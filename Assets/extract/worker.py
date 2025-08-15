# -*- coding: utf-8 -*-

import numpy as np
import json
import argparse
import os
import open3d as o3d
import laspy
import matplotlib.pyplot as plt
import time

# --- 模块化导入 ---
# 确保 terrain_generator.py 和 powerline_extractor.py 与此脚本在同一目录下
# 或在Python的搜索路径 (PYTHONPATH) 中。
try:
    from terrain_generator import extract_ground_ultra_fast, grid_lowest_point_numba, fill_holes_fast
    from Extractor4 import PowerLineExtractor
except ImportError as e:
    print(f"错误: 无法导入必要的模块。请确保 'terrain_generator.py' 和 'Extractor4.py' 文件存在于当前目录中。")
    print(f"详细错误: {e}")
    exit(1)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description="从LiDAR数据统一生成地形高度图和提取电力线。")
    
    # --- 文件输入输出参数 ---
    io_group = parser.add_argument_group('文件输入输出')
    io_group.add_argument('--input', type=str, required=True, help="输入的.las或.laz文件路径。")
    io_group.add_argument('--output_raw', type=str, required=True, help="输出的.raw高度图文件路径。")

    # --- 地形提取参数 ---
    terrain_group = parser.add_argument_group('地形提取参数')
    terrain_group.add_argument('--terrain_res', type=float, default=1.0, help="最终高度图的分辨率(米)。")
    terrain_group.add_argument('--thinning_res', type=float, default=0.5, help="地形提取时数据精简的分辨率(米)。")

    # --- 电力线提取参数 (新增) ---
    pl_group = parser.add_argument_group('电力线提取参数')
    pl_group.add_argument('--pl_height_min', type=float, default=0, help='电力线高程最小值 (默认: 0)')
    pl_group.add_argument('--pl_height_max', type=float, default=60, help='电力线高程最大值 (默认: 60)')
    pl_group.add_argument('--pl_eps', type=float, default=1.8, help='DBSCAN邻域半径 (默认: 1.8)')
    pl_group.add_argument('--pl_min_samples', type=int, default=7, help='DBSCAN最小样本数 (默认: 7)')
    pl_group.add_argument('--pl_min_line_length', type=float, default=30.0, help='电力线最小长度阈值 (默认: 30.0)')
    
    # --- 调试与显示参数 ---
    debug_group = parser.add_argument_group('调试与显示')
    debug_group.add_argument('--visualize', action='store_true', help="如果设置，则在结束前显示对齐后的地形和电力线。")
    
    args = parser.parse_args()
    
    start_time = time.time()

    # --- 步骤 1: 执行电力线提取 ---
    # 使用从命令行传入的参数来初始化提取器
    powerline_extractor = PowerLineExtractor(
        height_min=args.pl_height_min,
        height_max=args.pl_height_max,
        eps=args.pl_eps,
        min_samples=args.pl_min_samples
    )
    
    # 调用extract方法，并传入相关参数
    transformed_powerlines = powerline_extractor.extract(
        input_file=args.input,
        min_line_length=args.pl_min_line_length,
        # 强制关闭Extractor内部的逐步可视化，由主脚本统一控制最终可视化
        visualize_steps=False 
    )
    
    # --- 步骤 2: 【核心】从实例中获取坐标变换信息 ---
    if hasattr(powerline_extractor, 'last_translation_vector'):
        translation_vector = powerline_extractor.last_translation_vector
    else:
        print("Error: missing last_translation_vector in PowerLineExtractor. Modify the class to set self.last_translation_vector.")
        exit(1)

    # --- 步骤 3: 执行地形提取 ---
    ground_data_original_coord = extract_ground_ultra_fast(
        input_las_path=args.input,
        thinning_resolution=args.thinning_res,
        initial_grid_size=15.0
    )
    if not (ground_data_original_coord and len(ground_data_original_coord.points) > 0):
        print(json.dumps({"error": "no ground points extracted"}))
        exit(1)

    # --- 步骤 4: 对地面点应用相同的平移向量 ---
    ground_xyz_original = ground_data_original_coord.xyz
    ground_xyz_transformed = ground_xyz_original - translation_vector

    # --- 步骤 5: 使用变换后的地面点生成高度图 ---
    x_min_t, y_min_t, _ = np.min(ground_xyz_transformed, axis=0)
    x_max_t, y_max_t, _ = np.max(ground_xyz_transformed, axis=0)
    
    grid_w = int(np.ceil((x_max_t - x_min_t) / args.terrain_res))
    grid_h = int(np.ceil((y_max_t - y_min_t) / args.terrain_res))
    
    sparse_grid_z = grid_lowest_point_numba(ground_xyz_transformed, args.terrain_res, x_min_t, y_min_t, grid_w, grid_h)
    filled_grid_z = fill_holes_fast(sparse_grid_z)
    
    min_h, max_h = np.min(filled_grid_z), np.max(filled_grid_z)
    height_range = max_h - min_h
    heightmap_16bit = ((filled_grid_z - min_h) / height_range * 65535).astype(np.uint16) if height_range > 0 else np.zeros_like(filled_grid_z, dtype=np.uint16)

    with open(args.output_raw, 'wb') as f:
        f.write(heightmap_16bit.T.tobytes())

    # --- 步骤 6: 【最终输出】生成统一的元数据JSON ---
    base_name = os.path.splitext(os.path.basename(args.input))[0]
    powerlines_las_path = f"{base_name}_extracted_powerlines.las"
    powerlines_json_path = f"{base_name}_powerline_endpoints.json"

    final_metadata = {
        "success": True,
        "terrain_metadata": {
            "heightmap_file": args.output_raw,
            "heightmapWidth": filled_grid_z.shape[1],
            "heightmapHeight": filled_grid_z.shape[0],
            "terrainWorldWidth": x_max_t - x_min_t,
            "terrainWorldLength": y_max_t - y_min_t,
            "terrainWorldHeight": height_range,
        },
        "powerline_metadata": {
            "line_count": len(transformed_powerlines),
            "las_file": powerlines_las_path,
            "endpoints_file": powerlines_json_path
        },
        "transform_info": {
            "translation_vector": translation_vector.tolist(),
            "comment": "这是从原始坐标系到当前新坐标系的平移向量。原始坐标 = 新坐标 + 平移向量"
        }
    }
    # 输出最终的 JSON 供外部系统消费
    print(json.dumps(final_metadata))
    
    # --- 步骤 7: 可选的可视化对齐结果 ---
    if args.visualize:
         ground_pcd = o3d.geometry.PointCloud()
         ground_pcd.points = o3d.utility.Vector3dVector(ground_xyz_transformed)
         ground_pcd.paint_uniform_color([0.5, 0.5, 0.5]) # 灰色地面

         colored_powerlines = powerline_extractor._visualize_separate_power_lines(transformed_powerlines)

         o3d.visualization.draw_geometries([ground_pcd] + colored_powerlines,
                                           window_name="对齐后的地形与电力线 (Aligned Terrain & Power Lines)")
 
    total_time = time.time() - start_time
    # 简要输出耗时（可选）
    print(f"{{\"success\": true, \"elapsed_seconds\": {total_time:.2f}}}")