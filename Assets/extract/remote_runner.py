import argparse
import os
import sys
import paramiko
from tqdm import tqdm
import laspy
import open3d as o3d
import numpy as np

def create_ssh_client():
    ssh = paramiko.SSHClient()
    ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    ssh.connect(
        hostname="connect.bjb1.seetacloud.com",
        port=26492,
        username="root",
        password="IQKb3+cMsCE1"
    )
    return ssh

def create_progress_callback(description, file_size):
    """创建进度条回调函数"""
    pbar = tqdm(
        total=file_size,
        unit='B',
        unit_scale=True,
        unit_divisor=1024,
        desc=description,
        ncols=80,
        file=sys.stdout  # 输出到stdout而不是stderr
    )
    
    def progress_callback(transferred, total):
        pbar.update(transferred - pbar.n)
        if transferred >= total:
            pbar.close()
    
    return progress_callback

def upload_with_progress(sftp, local_path, remote_path, description):
    """使用SFTP上传文件并显示进度条"""
    file_size = os.path.getsize(local_path)
    callback = create_progress_callback(description, file_size)
    sftp.put(local_path, remote_path, callback=callback)

def download_with_progress(sftp, remote_path, local_path, description):
    """使用SFTP下载文件并显示进度条"""
    # 如果本地文件已存在，删除它
    if os.path.exists(local_path):
        print(f"本地文件 {local_path} 已存在，正在删除...")
        os.remove(local_path)
        print(f"已删除原文件: {local_path}")
    
    # 先获取远程文件大小
    file_attrs = sftp.stat(remote_path)
    file_size = file_attrs.st_size
    callback = create_progress_callback(description, file_size)
    sftp.get(remote_path, local_path, callback=callback)

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
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True, help="Local input file (.las)")
    parser.add_argument("--output", required=True, help="Local output file (.las)")
    parser.add_argument("--visualize", action='store_true', help="Visualize the output file after download")
    args = parser.parse_args()

    local_input_path = args.input
    local_output_path = args.output

    if not os.path.exists(local_input_path):
        print(f"File not found: {local_input_path}")
        return

    ssh = create_ssh_client()
    sftp = ssh.open_sftp()

    filename = os.path.basename(local_input_path)
    remote_input_path = f"/root/autodl-tmp/input/{filename}"
    remote_output_path = f"/root/autodl-tmp/output/result_{filename}"

    # 确保远程目录存在
    try:
        sftp.makedirs("/root/autodl-tmp/input")
    except:
        pass  # 目录可能已存在
    
    try:
        sftp.makedirs("/root/autodl-tmp/output")
    except:
        pass

    # 上传文件 - 带进度条
    print(f"Uploading {local_input_path} → {remote_input_path}")
    upload_with_progress(sftp, local_input_path, remote_input_path, f"上传 {filename}")

    # 远程脚本只传入输入和输出文件路径
    command = (
        f"/root/miniconda3/bin/python3 /root/autodl-tmp/scripts/work.py "
        f"{remote_input_path} {remote_output_path}"
    )
    print(f"Running remote script: {command}")
    stdin, stdout, stderr = ssh.exec_command(command)

    print("".join(stdout.readlines()))
    err_msg = "".join(stderr.readlines())
    if err_msg:
        print("ERR:", err_msg)

    # 下载文件 - 带进度条
    result_filename = os.path.basename(local_output_path)
    print(f"Downloading {remote_output_path} → {local_output_path}")
    download_with_progress(sftp, remote_output_path, local_output_path, f"下载 {result_filename}")

    sftp.close()
    ssh.close()
    print("Done!")
    
    # 可视化下载的文件
    if args.visualize or input("是否要可视化下载的LAS文件? (y/N): ").lower().startswith('y'):
        visualize_las_file(local_output_path)

if __name__ == "__main__":
    main()
