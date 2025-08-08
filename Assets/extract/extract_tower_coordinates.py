#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
电力塔坐标提取脚本
从电力线端点JSON文件中提取电力塔坐标，生成符合B.csv格式的CSV文件

使用方法:
    python extract_tower_coordinates.py <input_json> <output_csv>

参数:
    input_json: 输入的电力线端点JSON文件路径
    output_csv: 输出的电力塔坐标CSV文件路径

输出格式:
    group_id,order,x,y,z,line_count
"""

import json
import numpy as np
from sklearn.cluster import DBSCAN
import csv
import networkx as nx
import sys
import os

def normalize_coordinates_to_target_z_mean(coordinates, target_z_mean=10.0):
    """
    将坐标的z平均值归一化到目标值，并相应缩放xy坐标
    
    Args:
        coordinates: numpy数组，形状为(N, 3)，包含x,y,z坐标
        target_z_mean: 目标z坐标平均值，默认10.0
    
    Returns:
        normalized_coordinates: 归一化后的坐标数组
        scale_factor: 使用的缩放因子
    """
    if len(coordinates) == 0:
        return coordinates, 1.0
    
    # 计算当前z坐标的平均值
    current_z_mean = np.mean(coordinates[:, 2])
    
    if current_z_mean == 0:
        # 如果z平均值为0，使用一个小的非零值
        current_z_mean = 1.0
    
    # 计算缩放因子
    scale_factor = target_z_mean / current_z_mean
    
    # 应用缩放
    normalized_coordinates = coordinates * scale_factor
    
    print(f"坐标归一化: z平均值 {current_z_mean:.2f} -> {target_z_mean:.2f}, 缩放因子: {scale_factor:.4f}")
    
    return normalized_coordinates, scale_factor

def extract_tower_coordinates(input_json_path, output_csv_path, eps=120.0, min_samples=1, target_z_mean=10.0):
    """
    从电力线端点JSON文件中提取电力塔坐标
    
    Args:
        input_json_path: 输入的JSON文件路径
        output_csv_path: 输出的CSV文件路径
        eps: DBSCAN聚类半径，默认120米
        min_samples: DBSCAN最小样本数，默认1
        target_z_mean: 目标z坐标平均值，默认10.0
    """
    
    print(f"开始处理文件: {input_json_path}")
    
    # 检查输入文件是否存在
    if not os.path.exists(input_json_path):
        raise FileNotFoundError(f"输入文件不存在: {input_json_path}")
    
    # 读取端点JSON文件
    try:
        with open(input_json_path, 'r', encoding='utf-8') as f:
            endpoints_data = json.load(f)
        print(f"成功读取JSON文件，包含 {len(endpoints_data)} 条电力线")
    except Exception as e:
        raise Exception(f"读取JSON文件失败: {str(e)}")
    
    # 收集所有端点及其来源线索引
    points = []
    endpoint_to_line = []  # (line_index, 'start'/'end')
    
    for item in endpoints_data:
        if 'start' in item and 'end' in item and 'index' in item:
            points.append(item['start'])
            endpoint_to_line.append((item['index'], 'start'))
            points.append(item['end'])
            endpoint_to_line.append((item['index'], 'end'))
        else:
            print(f"警告：跳过格式不正确的条目: {item}")
    
    if len(points) == 0:
        raise Exception("没有找到有效的端点数据")
    
    points = np.array(points)  # shape (2*N, 3)
    print(f"收集到 {len(points)} 个端点")
    
    # 端点聚类
    print(f"开始DBSCAN聚类 (eps={eps}, min_samples={min_samples})...")
    db = DBSCAN(eps=eps, min_samples=min_samples).fit(points[:, :2])
    labels = db.labels_
    
    unique_labels = set(labels)
    n_clusters = len(unique_labels) - (1 if -1 in labels else 0)
    print(f"DBSCAN聚类完成，发现 {n_clusters} 个电力塔")
    
    # 统计每个塔的坐标
    tower_dict = {}
    all_tower_coords = []  # 收集所有塔的坐标用于归一化
    
    for label in unique_labels:
        if label == -1:  # 跳过噪声点
            continue
        cluster_points = points[labels == label]
        x = float(np.mean(cluster_points[:, 0]))
        y = float(np.mean(cluster_points[:, 1]))
        z = float(np.max(cluster_points[:, 2]))  # 使用最高点作为塔高
        tower_dict[label] = {'x': x, 'y': y, 'z': z, 'members': np.where(labels == label)[0].tolist()}
        all_tower_coords.append([x, y, z])
    
    # 应用坐标归一化
    if len(all_tower_coords) > 0:
        all_tower_coords = np.array(all_tower_coords)
        normalized_coords, scale_factor = normalize_coordinates_to_target_z_mean(all_tower_coords, target_z_mean)
        
        # 更新塔的坐标
        for i, label in enumerate([l for l in unique_labels if l != -1]):
            if label in tower_dict:
                tower_dict[label]['x'] = float(normalized_coords[i, 0])
                tower_dict[label]['y'] = float(normalized_coords[i, 1])
                tower_dict[label]['z'] = float(normalized_coords[i, 2])
    
    # 建立塔-线-塔连通关系
    print("建立电力塔连通关系...")
    G = nx.Graph()
    for i in range(0, len(endpoint_to_line), 2):
        if i + 1 < len(endpoint_to_line):
            line_idx = endpoint_to_line[i][0]
            tower_a = labels[i]
            tower_b = labels[i+1]
            if tower_a != -1 and tower_b != -1:  # 跳过噪声点
                G.add_node(tower_a)
                G.add_node(tower_b)
                G.add_edge(tower_a, tower_b, line_index=line_idx)
    
    # 分组（连通分量）
    groups = list(nx.connected_components(G))
    print(f"发现 {len(groups)} 个连通组")
    
    # 每组内排序（从度为1的塔出发，按物理顺序遍历）
    def sort_towers_in_group(subgraph):
        # 获取所有塔的label和坐标
        tower_labels = list(subgraph.nodes())
        coords = np.array([[tower_dict[label]['x'], tower_dict[label]['y'], tower_dict[label]['z']] for label in tower_labels])
        n = len(tower_labels)
        if n == 0:
            return []
        
        # 以y坐标最高的点为起点
        current = np.argmax(coords[:, 1])
        visited = [False] * n
        order = []
        order.append(tower_labels[current])
        visited[current] = True
        
        for _ in range(n - 1):
            dists = np.linalg.norm(coords - coords[current], axis=1)
            dists = np.where(visited, np.inf, dists)
            next_idx = np.argmin(dists)
            if dists[next_idx] == np.inf:
                break
            order.append(tower_labels[next_idx])
            visited[next_idx] = True
            current = next_idx
        return order
    
    # 输出到CSV
    print(f"生成CSV文件: {output_csv_path}")
    with open(output_csv_path, 'w', newline='', encoding='utf-8') as f:
        writer = csv.writer(f)
        writer.writerow(['group_id', 'order', 'x', 'y', 'z', 'line_count'])
        
        total_towers = 0
        for group_id, group in enumerate(groups):
            subgraph = G.subgraph(group)
            order_list = sort_towers_in_group(subgraph)
            
            for order, tower_label in enumerate(order_list):
                tower = tower_dict[tower_label]
                line_count = subgraph.degree[tower_label]
                writer.writerow([group_id, order, tower['x'], tower['y'], tower['z'], line_count])
                total_towers += 1
            
            # 在每组之间添加空行（可选，保持与B.csv格式一致）
            if group_id < len(groups) - 1:  # 不在最后一组后添加空行
                writer.writerow([])
    
    print(f"处理完成！")
    print(f"- 输入文件: {input_json_path}")
    print(f"- 输出文件: {output_csv_path}")
    print(f"- 总电力塔数: {total_towers}")
    print(f"- 连通组数: {len(groups)}")
    
    return {
        'total_towers': total_towers,
        'total_groups': len(groups),
        'total_lines': len(endpoints_data)
    }

def main():
    """主函数"""
    if len(sys.argv) < 3:
        print("使用方法: python extract_tower_coordinates.py <input_json> <output_csv> [eps] [min_samples] [target_z_mean]")
        print("示例: python extract_tower_coordinates.py B_powerline_endpoints.json B_tower_coordinates.csv")
        print("参数说明:")
        print("  eps: DBSCAN聚类半径，默认120")
        print("  min_samples: DBSCAN最小样本数，默认1")
        print("  target_z_mean: 目标z坐标平均值，默认10.0")
        sys.exit(1)
    
    input_json_path = sys.argv[1]
    output_csv_path = sys.argv[2]
    
    # 可选参数
    eps = 120.0
    min_samples = 1
    target_z_mean = 10.0
    
    if len(sys.argv) > 3:
        try:
            eps = float(sys.argv[3])
        except ValueError:
            print(f"警告：无效的eps参数，使用默认值 {eps}")
    
    if len(sys.argv) > 4:
        try:
            min_samples = int(sys.argv[4])
        except ValueError:
            print(f"警告：无效的min_samples参数，使用默认值 {min_samples}")
    
    if len(sys.argv) > 5:
        try:
            target_z_mean = float(sys.argv[5])
        except ValueError:
            print(f"警告：无效的target_z_mean参数，使用默认值 {target_z_mean}")
    
    try:
        stats = extract_tower_coordinates(input_json_path, output_csv_path, eps, min_samples, target_z_mean)
        print(f"\n统计信息: {stats['total_lines']}条电力线, {stats['total_towers']}个电力塔, {stats['total_groups']}个连通组")
    except Exception as e:
        print(f"错误: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main() 