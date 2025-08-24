import os
import paramiko
from scp import SCPClient
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

def visualize_point_cloud(input_file_path):
    """使用open3d可视化点云"""
    try:
        print(f"🔍 正在加载点云文件: {input_file_path}")
        
        # 检查文件是否存在
        if not os.path.exists(input_file_path):
            print(f"❌ 文件不存在: {input_file_path}")
            return False
            
        # 根据文件扩展名选择加载方式
        file_ext = os.path.splitext(input_file_path)[1].lower()
        
        if file_ext == '.las':
            # 使用laspy加载LAS文件
            try:
                import laspy
                las = laspy.read(input_file_path)
                points = np.vstack([las.x, las.y, las.z]).transpose()
                
                # 创建open3d点云对象
                pcd = o3d.geometry.PointCloud()
                pcd.points = o3d.utility.Vector3dVector(points)
                
                # 如果有颜色信息，添加颜色
                if hasattr(las, 'red') and hasattr(las, 'green') and hasattr(las, 'blue'):
                    colors = np.vstack([las.red, las.green, las.blue]).transpose() / 65535.0
                    pcd.colors = o3d.utility.Vector3dVector(colors)
                else:
                    # 如果没有颜色信息，使用高度作为颜色
                    heights = las.z
                    normalized_heights = (heights - heights.min()) / (heights.max() - heights.min())
                    colors = np.column_stack([normalized_heights, normalized_heights, normalized_heights])
                    pcd.colors = o3d.utility.Vector3dVector(colors)
                
                print(f"✅ 成功加载LAS文件，点云包含 {len(points)} 个点")
                
            except ImportError:
                print("❌ 未安装laspy库，无法加载LAS文件")
                return False
                
        elif file_ext in ['.ply', '.pcd', '.xyz']:
            # 使用open3d直接加载
            pcd = o3d.io.read_point_cloud(input_file_path)
            if not pcd.has_points():
                print(f"❌ 无法加载点云文件: {input_file_path}")
                return False
            print(f"✅ 成功加载点云文件，包含 {len(pcd.points)} 个点")
            
        else:
            print(f"❌ 不支持的文件格式: {file_ext}")
            return False
        
        # 显示点云
        print("🎨 正在显示点云可视化...")
        print("💡 提示：在可视化窗口中，您可以：")
        print("   - 使用鼠标左键旋转视角")
        print("   - 使用鼠标右键平移视角")
        print("   - 使用鼠标滚轮缩放")
        print("   - 按 'Q' 键退出可视化")
        
        o3d.visualization.draw_geometries([pcd], 
                                        window_name=f"点云可视化 - {os.path.basename(input_file_path)}",
                                        width=1200, 
                                        height=800)
        
        return True
        
    except Exception as e:
        print(f"❌ 点云可视化失败: {str(e)}")
        return False

def main():
    # 从环境变量或配置文件读取输入文件路径
    # 这里假设Unity会设置环境变量或创建配置文件
    input_file_path = os.environ.get('UNITY_INPUT_FILE')
    
    if not input_file_path:
        # 尝试从配置文件读取
        config_file = "unity_input_config.txt"
        if os.path.exists(config_file):
            try:
                with open(config_file, 'r', encoding='utf-8') as f:
                    input_file_path = f.read().strip()
                print(f"📖 从配置文件读取输入文件: {input_file_path}")
            except Exception as e:
                print(f"❌ 读取配置文件失败: {e}")
                return
        else:
            print("❌ 未找到输入文件路径，请设置环境变量UNITY_INPUT_FILE或创建unity_input_config.txt文件")
            return
    
    if not os.path.exists(input_file_path):
        print(f"❌ 输入文件不存在: {input_file_path}")
        return

    print(f"🚀 开始处理文件: {input_file_path}")
    
    # 创建SSH连接
    try:
        ssh = create_ssh_client()
        scp = SCPClient(ssh.get_transport())
        
        filename = os.path.basename(input_file_path)
        remote_input_path = f"/root/autodl-tmp/input/{filename}"
        
        print(f"📤 正在上传文件到远程服务器...")
        scp.put(input_file_path, remote_input_path)
        
        # 执行远程脚本（不需要输出文件）
        command = f"/root/miniconda3/bin/python3 /root/autodl-tmp/scripts/work.py {remote_input_path}"
        print(f"🚀 正在执行远程电力线提取脚本...")
        
        stdin, stdout, stderr = ssh.exec_command(command)
        
        # 读取输出
        print("📊 远程脚本输出:")
        output_lines = stdout.readlines()
        for line in output_lines:
            print(f"   {line.strip()}")
        
        error_lines = stderr.readlines()
        if error_lines:
            print("⚠️  错误信息:")
            for line in error_lines:
                print(f"   {line.strip()}")
        
        print("✅ 远程电力线提取完成！")
        
        # 关闭连接
        scp.close()
        ssh.close()
        
        # 在本地显示点云可视化
        print("\n🎨 开始本地点云可视化...")
        visualize_point_cloud(input_file_path)
        
    except Exception as e:
        print(f"❌ 处理失败: {str(e)}")

if __name__ == "__main__":
    main()
