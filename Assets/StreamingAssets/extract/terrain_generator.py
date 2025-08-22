# -*- coding: utf-8 -*-
"""
地形生成器 (最终精简&极速版) - terrain_generator.py

本脚本包含从LiDAR数据中提取地面点并生成地形高度图的核心函数。

核心技术:
1.  数据精简(Thinning): 在处理前，通过格网化只保留每个单元的最低点，
    从根本上减少需要处理的点云数量。
2.  无KD-Tree格网增长: 使用Numba加速的、超快速的地面点迭代提取算法。
3.  超快速空洞填充: 使用 `scipy.ndimage.distance_transform_edt` 替代
    `griddata`，对生成的地形格网进行极速空洞填充。

所需库:
- laspy[lazrs]
- numpy
- scipy
- numba
- open3d (仅用于可选的可视化)
"""

import laspy
import numpy as np
import scipy.ndimage
import open3d as o3d
import os
import numba
import time
import json
import argparse


# ==============================================================================
#  核心算法函数
# ==============================================================================

@numba.jit(nopython=True)
def grow_ground_from_grid_numba(
    points_to_check_indices: np.ndarray,
    points_xyz: np.ndarray,
    ground_elevation_grid: np.ndarray,
    is_ground_mask: np.ndarray,
    x_min: float, y_min: float,
    grid_res: float, grid_h: int, grid_w: int,
    height_threshold: float
) -> np.ndarray:
    """
    Numba加速的核心迭代函数：在格网上进行地面点增长。
    """
    newly_ground_indices = []

    for i in range(len(points_to_check_indices)):
        p_idx = points_to_check_indices[i]
        p = points_xyz[p_idx]

        col = int((p[0] - x_min) / grid_res)
        row = int((p[1] - y_min) / grid_res)

        if not (0 < row < grid_h - 1 and 0 < col < grid_w - 1):
            continue

        min_neighbor_z = np.inf
        for r_offset in range(-1, 2):
            for c_offset in range(-1, 2):
                neighbor_z = ground_elevation_grid[row + r_offset, col + c_offset]
                if not np.isnan(neighbor_z) and neighbor_z < min_neighbor_z:
                    min_neighbor_z = neighbor_z

        if min_neighbor_z != np.inf and p[2] < min_neighbor_z + height_threshold:
            newly_ground_indices.append(p_idx)
            if np.isnan(ground_elevation_grid[row, col]) or p[2] < ground_elevation_grid[row, col]:
                ground_elevation_grid[row, col] = p[2]

    return np.array(newly_ground_indices, dtype=np.int64)


def extract_ground_ultra_fast(
    input_las_path: str,
    thinning_resolution: float = 0.5,
    initial_grid_size: float = 15.0,
    iteration_thresholds: list = None
) -> laspy.LasData:
    """
    终极速度优化版地面提取：数据精简 + 无KD-Tree格网增长。
    """
    if iteration_thresholds is None:
        iteration_thresholds = [0.25, 0.5, 1.0, 1.5]
    
    print("--- 开始地面点提取 (终极速度版) ---")
    las = laspy.read(input_las_path)
    all_points_xyz = np.vstack((las.x, las.y, las.z)).transpose()
    print(f"原始点云数量: {len(all_points_xyz)}")

    print(f"\n步骤 1: 执行数据精简 (格网分辨率: {thinning_resolution}m)...")
    start_time = time.time()

    x_min, y_min = np.min(all_points_xyz[:, 0]), np.min(all_points_xyz[:, 1])
    x_max, y_max = np.max(all_points_xyz[:, 0]), np.max(all_points_xyz[:, 1])

    thin_cell_x = ((all_points_xyz[:, 0] - x_min) / thinning_resolution).astype(np.int32)
    thin_cell_y = ((all_points_xyz[:, 1] - y_min) / thinning_resolution).astype(np.int32)
    num_cells_x = int((x_max - x_min) / thinning_resolution) + 1
    thin_cell_id = thin_cell_y * num_cells_x + thin_cell_x

    sorted_indices = np.lexsort((all_points_xyz[:, 2], thin_cell_id))

    _, first_indices = np.unique(thin_cell_id[sorted_indices], return_index=True)
    thinned_indices = sorted_indices[first_indices]

    points = all_points_xyz[thinned_indices]
    original_points_subset = las.points[thinned_indices]
    n_points = len(points)

    print(f"数据精简完成，耗时: {time.time() - start_time:.4f} 秒。")
    print(f"精简后点云数量: {n_points} (减少了 {len(all_points_xyz) - n_points} 个点)")

    is_ground = np.zeros(n_points, dtype=bool)
    print("\n步骤 2: (向量化) 在粗糙网格中寻找最低点作为初始地面种子...")
    start_time = time.time()

    seed_cell_x = ((points[:, 0] - x_min) / initial_grid_size).astype(np.int32)
    seed_cell_y = ((points[:, 1] - y_min) / initial_grid_size).astype(np.int32)
    num_seed_cells_x = int((x_max - x_min) / initial_grid_size) + 1
    seed_cell_id = seed_cell_y * num_seed_cells_x + seed_cell_x

    sorted_indices_seed = np.lexsort((points[:, 2], seed_cell_id))
    _, first_indices_seed = np.unique(seed_cell_id[sorted_indices_seed], return_index=True)
    initial_ground_indices = sorted_indices_seed[first_indices_seed]

    is_ground[initial_ground_indices] = True
    print(f"提取完成，耗时: {time.time() - start_time:.4f} 秒。找到 {len(initial_ground_indices)} 个种子点。")

    print("\n步骤 3: 执行无KD-Tree的格网增长迭代...")
    grid_h = int((y_max - y_min) / thinning_resolution) + 1
    grid_w = num_cells_x
    ground_elevation_grid = np.full((grid_h, grid_w), np.nan, dtype=np.float32)

    initial_ground_points = points[initial_ground_indices]
    init_cols = ((initial_ground_points[:, 0] - x_min) / thinning_resolution).astype(np.int32)
    init_rows = ((initial_ground_points[:, 1] - y_min) / thinning_resolution).astype(np.int32)
    ground_elevation_grid[init_rows, init_cols] = initial_ground_points[:, 2]

    for i, height_threshold in enumerate(iteration_thresholds):
        start_iter_time = time.time()
        print(f"\n开始第 {i + 1}/{len(iteration_thresholds)} 轮迭代 (高差阈值: {height_threshold}m)...")

        points_to_check_indices = np.where(~is_ground)[0]
        if len(points_to_check_indices) == 0:
            print("所有点都已分类完毕。")
            break

        newly_ground_indices = grow_ground_from_grid_numba(
            points_to_check_indices, points, ground_elevation_grid, is_ground,
            x_min, y_min, thinning_resolution, grid_h, grid_w, height_threshold
        )

        if len(newly_ground_indices) > 0:
            is_ground[newly_ground_indices] = True
            print(f"本轮迭代新识别出 {len(newly_ground_indices)} 个地面点。耗时 {time.time() - start_iter_time:.4f} 秒。")
        else:
            print(f"本轮没有新的地面点。耗时 {time.time() - start_iter_time:.4f} 秒。")

    print("\n所有迭代完成，正在创建结果对象...")
    final_ground_indices = np.where(is_ground)[0]

    ground_las = laspy.LasData(las.header)
    ground_las.points = original_points_subset[final_ground_indices]

    return ground_las


@numba.jit(nopython=True)
def grid_lowest_point_numba(points_xyz, grid_resolution, x_min, y_min, grid_w, grid_h):
    """
    极速格网化函数：遍历所有点，将每个点的Z值填充到对应网格中，只保留最低值。
    """
    grid_z = np.full((grid_h, grid_w), np.nan, dtype=np.float32)

    for i in range(len(points_xyz)):
        p = points_xyz[i]
        col = int((p[0] - x_min) / grid_resolution)
        row = int((p[1] - y_min) / grid_resolution)

        if 0 <= row < grid_h and 0 <= col < grid_w:
            if np.isnan(grid_z[row, col]) or p[2] < grid_z[row, col]:
                grid_z[row, col] = p[2]

    return grid_z


def fill_holes_fast(grid_z: np.ndarray) -> np.ndarray:
    """
    使用 scipy.ndimage 进行超快速的空洞填充。
    """
    valid_mask = ~np.isnan(grid_z)

    if np.all(valid_mask) or not np.any(valid_mask):
        return grid_z

    indices = scipy.ndimage.distance_transform_edt(
        ~valid_mask, return_distances=False, return_indices=True
    )

    filled_grid = grid_z[tuple(indices)]

    return filled_grid


# ==============================================================================
#  作为独立脚本运行时的示例入口 (用于测试)
# ==============================================================================
if __name__ == '__main__':
    parser = argparse.ArgumentParser(description="从LiDAR数据生成地形高度图(.raw)和元数据。")
    parser.add_argument('--input', type=str, required=True, help="输入的.las或.laz文件路径。")
    parser.add_argument('--output_raw', type=str, required=True, help="输出的.raw高度图文件路径。")
    parser.add_argument('--thinning_res', type=float, default=0.5, help="数据精简的格网分辨率(米)。")
    parser.add_argument('--grid_size', type=float, default=15.0, help="提取初始种子的粗糙格网大小(米)。")
    parser.add_argument('--terrain_res', type=float, default=1.0, help="最终高度图的分辨率(米)。")
    parser.add_argument('--visualize', action='store_true', help="如果设置此项，则在脚本结束前显示提取出的地面点云。")

    args = parser.parse_args()

    # 1. 提取地面点
    ground_data = extract_ground_ultra_fast(
        input_las_path=args.input,
        thinning_resolution=args.thinning_res,
        initial_grid_size=args.grid_size,
        iteration_thresholds=[0.25, 0.5, 1.0, 1.5]
    )
    if not (ground_data and len(ground_data.points) > 0):
        print(json.dumps({"error": "未能提取任何地面点。"}))
        exit(1)

    print(f"--- 地面点提取成功，共 {len(ground_data.points)} 个点 ---")

    if args.visualize:
        print("\n--- 正在启动3D可视化窗口... (关闭窗口后脚本将继续执行) ---")
        pcd = o3d.geometry.PointCloud()
        pcd.points = o3d.utility.Vector3dVector(ground_data.xyz)
        pcd.paint_uniform_color([0.8, 0.2, 0.2])
        o3d.visualization.draw_geometries(
            [pcd],
            window_name="提取出的地面点 (Extracted Ground Points)",
            point_show_normal=False
        )
        print("--- 可视化窗口已关闭 ---")

    # 2. 生成稀疏高程格网
    ground_xyz = ground_data.xyz
    x_min, y_min, _ = np.min(ground_xyz, axis=0)
    x_max, y_max, _ = np.max(ground_xyz, axis=0)
    grid_w = int(np.ceil((x_max - x_min) / args.terrain_res))
    grid_h = int(np.ceil((y_max - y_min) / args.terrain_res))
    sparse_grid_z = grid_lowest_point_numba(ground_xyz, args.terrain_res, x_min, y_min, grid_w, grid_h)

    # 3. 填充空洞
    filled_grid_z = fill_holes_fast(sparse_grid_z)

    # 4. 准备导出高度图
    min_h, max_h = np.min(filled_grid_z), np.max(filled_grid_z)
    height_range = max_h - min_h
    if height_range > 0:
        normalized_h = (filled_grid_z - min_h) / height_range
    else:
        normalized_h = np.zeros_like(filled_grid_z)
    heightmap_16bit = (normalized_h * 65535).astype(np.uint16)

    with open(args.output_raw, 'wb') as f:
        f.write(heightmap_16bit.T.tobytes())

    # 5. 打印元数据
    metadata = {
        "success": True,
        "heightmapWidth": filled_grid_z.shape[1],
        "heightmapHeight": filled_grid_z.shape[0],
        "terrainWorldWidth": x_max - x_min,
        "terrainWorldLength": y_max - y_min,
        "terrainWorldHeight": height_range,
        "message": "高度图生成成功！"
    }
    print("\n--- 最终元数据 ---")
    print(json.dumps(metadata, indent=4))
    print("--- 脚本执行完毕 ---")