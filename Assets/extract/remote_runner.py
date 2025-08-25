import argparse
import os
import sys
import paramiko
import subprocess  # 引入subprocess模块
from tqdm import tqdm
import laspy
import open3d as o3d
import numpy as np

# SSH连接信息
# 将这些信息放在脚本顶部，方便管理
SSH_CONFIG = {
    "hostname": "connect.bjb1.seetacloud.com",
    "port": 26492,
    "username": "root",
}

def create_ssh_client():
    """
    创建一个SSH客户端。
    通过 key_filename 参数明确指定要使用的私钥文件。
    """
    ssh = paramiko.SSHClient()
    ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())

    # 使用 os.path.expanduser 来正确处理 "~" 符号
    key_filepath = os.path.expanduser("~/.ssh/id_rsa_rjb")
    
    print(f"Attempting to connect using key: {key_filepath}")

    ssh.connect(
        hostname=SSH_CONFIG["hostname"],
        port=SSH_CONFIG["port"],
        username=SSH_CONFIG["username"],
        key_filename=key_filepath  # <--- 关键改动在这里！
    )
    return ssh

def upload_with_rsync(local_path, remote_dir):
    """
    使用rsync高效上传文件，并实时显示原生进度。
    """
    filename = os.path.basename(local_path)
    print(f"--- Starting upload for {filename} via rsync ---")

    # 构建rsync命令
    # -a: 归档模式，保留文件属性
    # -v: 详细模式
    # -z: 启用压缩传输
    # --progress: 显示详细的传输进度
    # -e: 指定要使用的SSH命令，包括端口号
    command = [
        "rsync",
        "-avz",
        "--progress",
        "-e", f"ssh -p {SSH_CONFIG['port']}",
        local_path,
        f"{SSH_CONFIG['username']}@{SSH_CONFIG['hostname']}:{remote_dir}"
    ]

    print(f"Executing command: {' '.join(command)}")

    # 使用subprocess执行命令，并实时打印输出
    # 这使得rsync的进度条可以直接显示在终端中
    process = subprocess.Popen(
        command, 
        stdout=subprocess.PIPE, 
        stderr=subprocess.STDOUT, 
        text=True, 
        bufsize=1,
        encoding='utf-8'
    )

    # 实时读取并打印输出流
    for line in iter(process.stdout.readline, ''):
        sys.stdout.write(line)
        sys.stdout.flush()

    process.wait()  # 等待命令执行完成

    if process.returncode == 0:
        print(f"--- Rsync upload for {filename} completed successfully ---\n")
    else:
        # 如果rsync失败，抛出异常以中断程序
        error_message = f"Rsync upload failed with return code {process.returncode}."
        print(f"ERROR: {error_message}")
        raise subprocess.CalledProcessError(process.returncode, command)

def download_with_progress(sftp, remote_path, local_path, description):
    """
    使用SFTP下载文件并显示tqdm进度条 (此函数保持不变)。
    """
    if os.path.exists(local_path):
        print(f"Local file {local_path} already exists. Deleting it...")
        os.remove(local_path)
        print(f"Deleted old file: {local_path}")
    
    try:
        file_attrs = sftp.stat(remote_path)
        file_size = file_attrs.st_size
        
        with tqdm(total=file_size, unit='B', unit_scale=True, unit_divisor=1024, desc=description, ncols=80) as pbar:
            def progress_callback(transferred, total):
                pbar.update(transferred - pbar.n)
            
            sftp.get(remote_path, local_path, callback=progress_callback)
            
    except FileNotFoundError:
        print(f"ERROR: Remote file not found at {remote_path}")
        sys.exit(1) # 退出程序，因为无法下载结果

def download_with_rsync(remote_path, local_dir):
    """
    使用rsync高效下载文件，并实时显示原生进度。
    """
    filename = os.path.basename(remote_path)
    print(f"--- Starting download for {filename} via rsync ---")

    # 注意：rsync的源和目标位置与上传相反
    command = [
        "rsync",
        "-avz",
        "--progress",
        "-e", f"ssh -p {SSH_CONFIG['port']}",
        f"{SSH_CONFIG['username']}@{SSH_CONFIG['hostname']}:{remote_path}", # 源：远程文件
        local_dir  # 目标：本地目录
    ]

    print(f"Executing command: {' '.join(command)}")
    
    process = subprocess.Popen(
        command, 
        stdout=subprocess.PIPE, 
        stderr=subprocess.STDOUT, 
        text=True, 
        bufsize=1,
        encoding='utf-8'
    )

    for line in iter(process.stdout.readline, ''):
        sys.stdout.write(line)
        sys.stdout.flush()

    process.wait()

    if process.returncode == 0:
        print(f"--- Rsync download for {filename} completed successfully ---\n")
    else:
        error_message = f"Rsync download failed with return code {process.returncode}."
        print(f"ERROR: {error_message}")
        raise subprocess.CalledProcessError(process.returncode, command)


def visualize_las_file(las_file_path):
    """使用laspy读取las文件并用open3d可视化"""
    print(f"正在读取LAS文件: {las_file_path}")
    
    try:
        # 使用laspy读取las文件
        las = laspy.read(las_file_path)
        
        # 提取xyz坐标
        points = np.vstack((las.x, las.y, las.z)).transpose()
        
        print(f"点云包含 {len(points)} 个点")
        print(f"点云范围:")
        print(f"  X: {points[:, 0].min():.2f} ~ {points[:, 0].max():.2f}")
        print(f"  Y: {points[:, 1].min():.2f} ~ {points[:, 1].max():.2f}")
        print(f"  Z: {points[:, 2].min():.2f} ~ {points[:, 2].max():.2f}")
        
        # 创建open3d点云对象
        pcd = o3d.geometry.PointCloud()
        pcd.points = o3d.utility.Vector3dVector(points)
        
        # 尝试提取颜色信息
        colors = None
        try:
            if hasattr(las, 'red') and hasattr(las, 'green') and hasattr(las, 'blue'):
                # RGB颜色信息存在
                colors = np.vstack((las.red, las.green, las.blue)).transpose()
                pcd.colors = o3d.utility.Vector3dVector(colors)
                print("检测到RGB颜色信息")
            elif hasattr(las, 'intensity'):
                # 使用强度信息生成颜色
                intensity = las.intensity
                intensity_normalized = (intensity - intensity.min()) / (intensity.max() - intensity.min())
                colors = np.column_stack([intensity_normalized, intensity_normalized, intensity_normalized])
                pcd.colors = o3d.utility.Vector3dVector(colors)
                print("使用强度信息生成灰度颜色")
            else:
                print("未找到颜色或强度信息，使用默认颜色")
                
        except Exception as e:
            print(f"处理颜色信息时出错: {e}")
            print("使用默认颜色")
        
        # 计算法向量（可选，用于更好的可视化效果）
        print("计算法向量...")
        pcd.estimate_normals(
            search_param=o3d.geometry.KDTreeSearchParamHybrid(radius=0.1, max_nn=30)
        )
        
        # 可视化点云
        
        o3d.visualization.draw_geometries([pcd], 
                                        window_name="LAS点云可视化",
                                        width=1200, 
                                        height=800,
                                        left=50, 
                                        top=50)
        
    except Exception as e:
        print(f"可视化LAS文件时出错: {e}")
        import traceback
        traceback.print_exc()

def main():
    parser = argparse.ArgumentParser(description="Upload, process, and download a LAS file.")
    parser.add_argument("--input", required=True, help="Local input LAS file path.")
    parser.add_argument("--output", required=True, help="Local output LAS file path.")
    parser.add_argument("--visualize", action='store_true', help="Visualize the output file automatically after download.")
    args = parser.parse_args()

    local_input_path = args.input
    local_output_path = args.output

    if not os.path.exists(local_input_path):
        print(f"Error: Input file not found at {local_input_path}")
        return

    ssh = None
    try:
        ssh = create_ssh_client()
        sftp = ssh.open_sftp()
        print("Successfully connected to remote server.")

        filename = os.path.basename(local_input_path)
        remote_input_dir = "/root/autodl-tmp/input"
        remote_output_dir = "/root/autodl-tmp/output"
        remote_input_path = f"{remote_input_dir}/{filename}"
        remote_output_path = f"{remote_output_dir}/result_{filename}"

        # 确保远程目录存在 (仍然使用sftp来操作目录)
        for directory in [remote_input_dir, remote_output_dir]:
            try:
                sftp.stat(directory)
            except FileNotFoundError:
                print(f"Remote directory {directory} not found, creating it...")
                sftp.mkdir(directory)

        # 步骤 1: 使用rsync上传文件
        upload_with_rsync(local_input_path, remote_input_dir)

        # 步骤 2: 远程执行处理脚本
        command = (
            f"/root/miniconda3/bin/python3 /root/autodl-tmp/scripts/work.py "
            f"{remote_input_path} {remote_output_path}"
        )
        print(f"Running remote script: {command}")
        stdin, stdout, stderr = ssh.exec_command(command)

        # 实时打印远程脚本的输出
        for line in iter(stdout.readline, ""):
            print(line, end="")
        
        err_msg = "".join(stderr.readlines())
        if err_msg:
            print("--- REMOTE SCRIPT ERROR ---", file=sys.stderr)
            print(err_msg, file=sys.stderr)
            print("---------------------------", file=sys.stderr)

        # 步骤 3: 下载处理结果文件
        print(f"Downloading {remote_output_path} to {local_output_path}")
        # 获取本地输出目录
        local_output_dir = os.path.dirname(local_output_path)
        # 确保本地输出目录存在
        if not os.path.exists(local_output_dir):
            os.makedirs(local_output_dir)
        download_with_progress(sftp, remote_output_path, local_output_path, f"Downloading result")
        download_with_rsync(remote_output_path, local_output_dir)

        sftp.close()
        print("\nAll operations completed!")
        
        # 步骤 4: 可视化下载的文件
        if args.visualize or input("Do you want to visualize the downloaded LAS file? (y/N): ").lower().startswith('y'):
            visualize_las_file(local_output_path)

    except Exception as e:
        print(f"\nAn error occurred: {e}")
    finally:
        if ssh:
            ssh.close()
            print("SSH connection closed.")

if __name__ == "__main__":
    main()