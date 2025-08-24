import argparse
import os
import paramiko
from scp import SCPClient

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

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True, help="Local input file (.las)")
    parser.add_argument("--output", required=True, help="Local output file (.las)")
    args = parser.parse_args()

    local_input_path = args.input
    local_output_path = args.output

    if not os.path.exists(local_input_path):
        print(f"❌ File not found: {local_input_path}")
        return

    ssh = create_ssh_client()
    scp = SCPClient(ssh.get_transport())

    filename = os.path.basename(local_input_path)
    remote_input_path = f"/root/autodl-tmp/input/{filename}"
    remote_output_path = f"/root/autodl-tmp/output/result_{filename}"

    print(f"📤 Uploading {local_input_path} → {remote_input_path} ...")
    scp.put(local_input_path, remote_input_path)

    # 远程脚本只传入输入和输出文件路径
    command = (
        f"/root/miniconda3/bin/python3 /root/autodl-tmp/scripts/work.py "
        f"{remote_input_path} {remote_output_path}"
    )
    print(f"🚀 Running remote script: {command}")
    stdin, stdout, stderr = ssh.exec_command(command)

    print("".join(stdout.readlines()))
    err_msg = "".join(stderr.readlines())
    if err_msg:
        print("ERR:", err_msg)

    print(f"📥 Downloading {remote_output_path} → {local_output_path} ...")
    scp.get(remote_output_path, local_output_path)

    scp.close()
    ssh.close()
    print("✅ Done!")

if __name__ == "__main__":
    main()
