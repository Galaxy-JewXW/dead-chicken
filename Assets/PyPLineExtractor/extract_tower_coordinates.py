import json
import numpy as np
import csv
from collections import defaultdict

def calculate_distance(point1, point2):
    """计算两点之间的欧几里得距离"""
    return np.sqrt((point1['x'] - point2['x'])**2 + 
                   (point1['y'] - point2['y'])**2 + 
                   (point1['z'] - point2['z'])**2)

def extract_tower_coordinates(json_file_path):
    """
    从JSON文件中提取电力塔坐标
    使用基于空间距离的智能分组，每组固定16个端点
    确保每组内的端点距离相近，组间距离较远
    """
    
    # 读取JSON文件
    with open(json_file_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    # 提取所有端点坐标
    all_endpoints = []
    
    for power_line in data['power_lines']:
        # 添加endpoint1
        endpoint1 = power_line['endpoint1']
        all_endpoints.append({
            'x': endpoint1['x'],
            'y': endpoint1['y'],
            'z': endpoint1['z']
        })
        
        # 添加endpoint2
        endpoint2 = power_line['endpoint2']
        all_endpoints.append({
            'x': endpoint2['x'],
            'y': endpoint2['y'],
            'z': endpoint2['z']
        })
    
    print(f"总共提取了 {len(all_endpoints)} 个端点坐标")
    
    # 使用基于距离的智能分组
    group_size = 16
    tower_coordinates = []
    remaining_endpoints = all_endpoints.copy()
    
    while len(remaining_endpoints) >= group_size:
        # 选择第一个未处理的端点作为种子
        seed = remaining_endpoints[0]
        group = [seed]
        remaining_endpoints.remove(seed)
        
        # 找到距离种子最近的15个端点
        distances = []
        for i, point in enumerate(remaining_endpoints):
            dist = calculate_distance(seed, point)
            distances.append((dist, i, point))
        
        # 按距离排序，选择最近的15个点
        distances.sort()
        for _, idx, point in distances[:group_size-1]:
            group.append(point)
            remaining_endpoints.remove(point)
        
        # 计算组的中心坐标和最大高度
        x_coords = [point['x'] for point in group]
        y_coords = [point['y'] for point in group]
        z_coords = [point['z'] for point in group]
        
        center_x = np.mean(x_coords)
        center_y = np.mean(y_coords)
        max_z = max(z_coords)
        
        tower_coordinates.append({
            'x': center_x,
            'y': center_y,
            'z': max_z
        })
        
        print(f"第 {len(tower_coordinates)} 组:")
        print(f"  中心坐标: ({center_x:.2f}, {center_y:.2f})")
        print(f"  最大高度: {max_z:.2f}")
        print(f"  包含端点数量: {len(group)}")
        
        # 计算组内平均距离和组间距离
        avg_internal_distance = calculate_group_internal_distance(group)
        print(f"  组内平均距离: {avg_internal_distance:.2f}")
        print()
    
    # 处理剩余的端点（如果不足16个）
    if remaining_endpoints:
        print(f"剩余 {len(remaining_endpoints)} 个端点（不足16个，丢弃）")
    
    return tower_coordinates

def calculate_group_internal_distance(group):
    """计算组内端点的平均距离"""
    if len(group) < 2:
        return 0
    
    total_distance = 0
    count = 0
    
    for i in range(len(group)):
        for j in range(i+1, len(group)):
            dist = calculate_distance(group[i], group[j])
            total_distance += dist
            count += 1
    
    return total_distance / count if count > 0 else 0

def save_tower_coordinates_json(tower_coordinates, output_file):
    """保存电力塔坐标到JSON文件"""
    output_data = {
        "total_towers": len(tower_coordinates),
        "tower_coordinates": tower_coordinates
    }
    
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(output_data, f, indent=2, ensure_ascii=False)
    
    print(f"电力塔坐标已保存到JSON文件: {output_file}")

def save_tower_coordinates_csv(tower_coordinates, output_file):
    """保存电力塔坐标到CSV文件"""
    with open(output_file, 'w', newline='', encoding='utf-8') as f:
        writer = csv.writer(f)
        
        # 写入表头
        writer.writerow(['Tower_ID', 'X', 'Y', 'Z'])
        
        # 写入数据
        for i, coord in enumerate(tower_coordinates, 1):
            writer.writerow([
                f'Tower_{i}',
                f"{coord['x']:.6f}",
                f"{coord['y']:.6f}",
                f"{coord['z']:.6f}"
            ])
    
    print(f"电力塔坐标已保存到CSV文件: {output_file}")

if __name__ == "__main__":
    # 提取电力塔坐标
    tower_coords = extract_tower_coordinates("A.json")
    
    # 保存结果到JSON文件
    save_tower_coordinates_json(tower_coords, "tower_coordinates.json")
    
    # 保存结果到CSV文件
    save_tower_coordinates_csv(tower_coords, "tower_coordinates.csv")
    
    print(f"\n总共提取了 {len(tower_coords)} 个电力塔坐标:")
    for i, coord in enumerate(tower_coords, 1):
        print(f"  电力塔 {i}: ({coord['x']:.2f}, {coord['y']:.2f}, {coord['z']:.2f})") 