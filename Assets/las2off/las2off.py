#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
LAS点云文件转换为OFF格式的Python脚本
简化版本 - 直接运行即可
"""

import os
import sys
import numpy as np

# =============================================================================
# 配置区域 - 修改这里的文件名
# =============================================================================
INPUT_FILE = "B线路1.las"      # 输入LAS文件名
OUTPUT_FILE = "B线路1.off"     # 输出OFF文件名
PRECISION = 6                 # 坐标精度（小数位数）
# =============================================================================

try:
    import laspy
except ImportError:
    print("错误: 需要安装laspy库")
    print("请运行: pip install laspy")
    sys.exit(1)


def convert_las_to_off():
    """将LAS文件转换为OFF格式"""
    print("=== LAS点云文件转OFF格式转换工具 ===")
    print(f"输入文件: {INPUT_FILE}")
    print(f"输出文件: {OUTPUT_FILE}")
    
    # 检查输入文件
    if not os.path.exists(INPUT_FILE):
        print(f"错误: 输入文件不存在: {INPUT_FILE}")
        print("请将LAS文件放在脚本同一目录下")
        sys.exit(1)
    
    try:
        # 读取LAS文件
        print("正在读取LAS文件...")
        las = laspy.read(INPUT_FILE)
        
        # 提取坐标 (保持原始精度)
        x = np.array(las.x, dtype=np.float64)
        y = np.array(las.y, dtype=np.float64) 
        z = np.array(las.z, dtype=np.float64)
        
        points = np.column_stack([x, y, z])
        
        print(f"成功读取LAS文件: {len(points)} 个点")
        print(f"坐标范围: X[{x.min():.6f}, {x.max():.6f}], "
              f"Y[{y.min():.6f}, {y.max():.6f}], "
              f"Z[{z.min():.6f}, {z.max():.6f}]")
        
        # 写入OFF文件
        print("正在写入OFF文件...")
        with open(OUTPUT_FILE, 'w', encoding='utf-8') as f:
            # 写入OFF文件头
            f.write("OFF\n")
            f.write(f"{len(points)} 0 0\n")
            
            # 写入顶点坐标
            for i, point in enumerate(points):
                f.write(f"{point[0]:.{PRECISION}f} "
                       f"{point[1]:.{PRECISION}f} "
                       f"{point[2]:.{PRECISION}f}\n")
                
                # 显示进度
                if (i + 1) % 10000 == 0:
                    print(f"已写入 {i + 1}/{len(points)} 个顶点")
        
        print(f"[成功] 转换完成！")
        print(f"输出文件: {OUTPUT_FILE}")
        print(f"文件大小: {os.path.getsize(OUTPUT_FILE) / (1024*1024):.2f} MB")
        
    except Exception as e:
        print(f"[错误] 转换失败: {e}")
        sys.exit(1)


if __name__ == "__main__":
    convert_las_to_off() 