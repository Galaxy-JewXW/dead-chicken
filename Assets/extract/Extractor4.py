import open3d as o3d
import numpy as np
import laspy
import os
from tqdm import tqdm
import cv2
from sklearn.cluster import DBSCAN, MeanShift, estimate_bandwidth
from sklearn.decomposition import PCA
from sklearn.linear_model import RANSACRegressor
import matplotlib.pyplot as plt
from scipy.spatial import KDTree
import time
import copy
import json
import multiprocessing


class PowerLineExtractor:
    """
    电力线提取器类
    用于从点云数据中提取电力线
    """

    def __init__(self, threshold=0.81, radius=1.5, height_min=0, height_max=20, eps=1.5, min_samples=5, 
                 enable_visualization=True):
        """
        初始化电力线提取器
        
        :param threshold: 线特征阈值
        :param radius: 邻域搜索半径
        :param height_min: 最小高程
        :param height_max: 最大高程
        :param eps: DBSCAN邻域半径
        :param min_samples: DBSCAN最小样本数
        :param enable_visualization: 是否启用可视化
        """
        self.threshold = threshold
        self.radius = radius
        self.height_min = height_min
        self.height_max = height_max
        self.eps = eps
        self.min_samples = min_samples
        self.enable_visualization = enable_visualization
        
        # 添加缓存机制
        self._direction_cache = {}  # 缓存主方向计算结果
        self._endpoint_cache = {}   # 缓存端点计算结果
        self._radius_cache = {}    # 缓存动态半径计算结果
        
        # 并行计算设置
        self.n_jobs = min(multiprocessing.cpu_count(), 8)  # 最多使用8个核心

    def _clear_caches(self):
        """清理缓存以释放内存"""
        self._direction_cache.clear()
        self._endpoint_cache.clear()
        self._radius_cache.clear()

    def _read_point_cloud(self, file_path):
        """
        读取点云文件，支持.las格式

        :param file_path: 文件路径
        :return: open3d点云对象
        """
        las_data = laspy.read(file_path)
        x = np.array(las_data.x)
        y = np.array(las_data.y)
        z = np.array(las_data.z)
        points = np.column_stack((x, y, z))
        cloud = o3d.geometry.PointCloud()
        cloud.points = o3d.utility.Vector3dVector(points)
        return cloud

    def _save_point_cloud(self, cloud, file_path):
        """
        保存点云文件

        :param cloud: open3d点云对象
        :param file_path: 保存路径
        """
        points = np.asarray(cloud.points)
        header = laspy.LasHeader(point_format=0, version="1.2")
        header.offsets = np.array([0, 0, 0])
        header.scales = np.array([0.001, 0.001, 0.001])
        las_data = laspy.LasData(header)
        las_data.x = points[:, 0]
        las_data.y = points[:, 1]
        las_data.z = points[:, 2]
        las_data.write(file_path)

    def _pass_through(self, cloud, limit_min=None, limit_max=None):
        """
        高程滤波

        :param cloud: 输入点云
        :param limit_min: 高程最小值，如果为None则使用实例变量
        :param limit_max: 高程最大值，如果为None则使用实例变量
        :return: 高程较低点，高程较高点
        """
        if limit_min is None:
            limit_min = self.height_min
        if limit_max is None:
            limit_max = self.height_max
        points = np.asarray(cloud.points)
        ind = np.where((points[:, 2] >= limit_min) & (points[:, 2] <= limit_max))[0]
        low_cloud = cloud.select_by_index(ind)
        high_cloud = cloud.select_by_index(ind, invert=True)
        return low_cloud, high_cloud

    def _pca_compute(self, data, sort=True):
        """
        SVD分解计算点云的特征值（优化版本）

        :param data: 输入数据
        :param sort: 是否将特征值进行排序
        :return: 特征值
        """
        if len(data) < 3:
            return np.array([1.0, 0.0, 0.0])
        
        # 使用更快的协方差矩阵计算方法
        # 直接计算协方差矩阵，避免额外的矩阵乘法
        centered_data = data - np.mean(data, axis=0)
        cov_matrix = np.cov(centered_data.T)
        
        # 使用eigh而不是svd，对于对称矩阵更快
        eigenvalues = np.linalg.eigh(cov_matrix)[0]
        
        if sort:
            eigenvalues = np.sort(eigenvalues)[::-1]  # 降序排列
        
        return eigenvalues

    def _calculate_dynamic_radius_by_terrain(self, point, points, kdtree, base_radius=1.5, min_radius=0.8, max_radius=3.0):
        """
        根据地形复杂度动态计算邻域半径（优化版本）
        
        :param point: 当前点
        :param points: 所有点云
        :param kdtree: KD树对象
        :param base_radius: 基础半径
        :param min_radius: 最小半径
        :param max_radius: 最大半径
        :return: 动态半径
        """
        # 创建缓存键
        cache_key = (hash(str(point[:2])), base_radius)  # 只使用x,y坐标和基础半径作为键
        
        if hasattr(self, '_radius_cache') and cache_key in self._radius_cache:
            return self._radius_cache[cache_key]
        
        # 先用基础半径搜索邻域
        k, idx, _ = kdtree.search_radius_vector_3d(point, base_radius)
        neighbors = points[idx]
        
        if len(neighbors) < 5:
            result = min(base_radius * 1.5, max_radius)  # 点太少，适当增大半径
        else:
            # 简化地形复杂度计算：只计算z值的范围
            z_values = neighbors[:, 2]
            z_range = np.max(z_values) - np.min(z_values)
            
            # 使用更简单的判断逻辑
            if z_range > 5.0:  # 地形起伏大
                result = max(base_radius * 0.6, min_radius)
            elif z_range < 1.0:  # 地形平坦
                result = min(base_radius * 1.2, max_radius)
            else:  # 中等起伏
                result = base_radius
        
        # 缓存结果
        if not hasattr(self, '_radius_cache'):
            self._radius_cache = {}
        self._radius_cache[cache_key] = result
        
        return result

    def _calculate_dynamic_threshold_by_percentile(self, linear_features, percentile=90, min_threshold=0.6, max_threshold=0.9):
        """
        基于百分位数计算动态阈值
        
        :param linear_features: 线性度数组
        :param percentile: 百分位数（默认85，即取前15%）
        :param min_threshold: 最小阈值
        :param max_threshold: 最大阈值
        :return: 动态阈值
        """
        if len(linear_features) == 0:
            return min_threshold
        
        # 过滤掉异常值
        valid_features = linear_features[linear_features > 0.1]  # 过滤掉太小的值
        
        if len(valid_features) < 10:
            return min_threshold
        
        # 计算百分位数阈值
        threshold = np.percentile(valid_features, percentile)
        
        # 限制在合理范围内
        threshold = np.clip(threshold, min_threshold, max_threshold)
        
        return threshold

    def _power_line_segmentation(self, power_line_cloud, threshold=None, use_dynamic_params=True):
        """
        计算每一个点的线性特征，并根据线性特征提取线点云
        支持动态参数调整，提升复杂地形下的识别效果

        :param power_line_cloud: 输入点云
        :param threshold: 线特征阈值，如果为None则使用实例变量
        :param use_dynamic_params: 是否使用动态参数，默认True
        :return: 线点云和线之外的点云
        """
        if threshold is None:
            threshold = self.threshold
        low, high = self._pass_through(power_line_cloud, self.height_min, self.height_max)
        points = np.asarray(high.points)
        kdtree = o3d.geometry.KDTreeFlann(high)
        num_points = len(high.points)
        linear = []

        if use_dynamic_params:
            print("使用动态参数模式，提升复杂地形识别效果...")
            
            # 优化采样策略：根据点云密度自适应调整采样率
            if num_points > 8000:
                print(f"激进加速模式：大幅减少计算，共{num_points}个点")
                
                # 自适应采样率：点越多，采样率越低
                if num_points > 10000000:  # 超过1000万点
                    sample_ratio = 0.1  # 只计算10%
                elif num_points > 5000000:  # 超过500万点
                    sample_ratio = 0.15  # 只计算15%
                else:
                    sample_ratio = 0.2  # 只计算20%
                
                sample_step = int(1 / sample_ratio)
                print(f"自适应采样：每{sample_step}个点计算一次（采样率: {sample_ratio*100:.0f}%）")
                
                # 预分配数组
                linear = np.zeros(num_points)
                
                # 计算采样点的线性特征
                sample_indices = list(range(0, num_points, sample_step))
                sample_values = []
                
                for i in tqdm(sample_indices, desc="动态参数采样计算", ncols=100):
                    # 使用动态半径
                    dynamic_radius = self._calculate_dynamic_radius_by_terrain(
                        high.points[i], points, kdtree, base_radius=self.radius
                    )
                    
                    k, idx, _ = kdtree.search_radius_vector_3d(high.points[i], dynamic_radius)
                    if len(idx) >= 3:
                        neighbors = points[idx, :]
                        w = self._pca_compute(neighbors)
                        l1, l2 = w[0], w[1]
                        L = np.divide((l1 - l2), l1, out=np.zeros_like((l1 - l2)), where=l1 != 0)
                        sample_values.append(L)
                    else:
                        sample_values.append(0.0)
                
                # 使用线性插值填充未计算的点
                for i in range(len(sample_indices) - 1):
                    start_idx = sample_indices[i]
                    end_idx = sample_indices[i + 1]
                    start_val = sample_values[i]
                    end_val = sample_values[i + 1]
                    
                    # 线性插值
                    for j in range(start_idx, end_idx):
                        if end_idx > start_idx:
                            t = (j - start_idx) / (end_idx - start_idx)
                            linear[j] = start_val * (1 - t) + end_val * t
                        else:
                            linear[j] = start_val
                
                # 处理最后一个采样点之后的部分
                if sample_indices:
                    last_idx = sample_indices[-1]
                    last_val = sample_values[-1]
                    linear[last_idx:] = last_val
                
                # 转换为list格式（保持原接口）
                linear = list(linear)
            else:
                # 标准逐点计算，使用动态半径
                linear = []
                for i in tqdm(range(num_points), desc="动态参数标准计算", ncols=100):
                    # 使用动态半径
                    dynamic_radius = self._calculate_dynamic_radius_by_terrain(
                        high.points[i], points, kdtree, base_radius=self.radius
                    )
                    
                    k, idx, _ = kdtree.search_radius_vector_3d(high.points[i], dynamic_radius)
                    if len(idx) >= 3:
                        neighbors = points[idx, :]
                        w = self._pca_compute(neighbors)
                        l1, l2 = w[0], w[1]
                        L = np.divide((l1 - l2), l1, out=np.zeros_like((l1 - l2)), where=l1 != 0)
                        linear.append(L)
                    else:
                        linear.append(0.0)

            linear = np.array(linear)
            
            # 使用动态阈值
            dynamic_threshold = self._calculate_dynamic_threshold_by_percentile(linear)
            print(f"动态阈值: {dynamic_threshold:.3f} (原阈值: {threshold:.3f})")
            
            idx = np.where(linear > dynamic_threshold)[0]
            line_cloud_ = high.select_by_index(idx)
            out_line_cloud_ = high.select_by_index(idx, invert=True) + low
            return line_cloud_, out_line_cloud_
        else:
            # 原始固定参数模式（保持向后兼容）
            print("使用固定参数模式...")
            
            # 优化采样策略：根据点云密度自适应调整采样率
            if num_points > 8000:
                print(f"激进加速模式：大幅减少计算，共{num_points}个点")
                
                # 自适应采样率：点越多，采样率越低
                if num_points > 10000000:  # 超过1000万点
                    sample_ratio = 0.1  # 只计算10%
                elif num_points > 5000000:  # 超过500万点
                    sample_ratio = 0.15  # 只计算15%
                else:
                    sample_ratio = 0.2  # 只计算20%
                
                sample_step = int(1 / sample_ratio)
                print(f"自适应采样：每{sample_step}个点计算一次（采样率: {sample_ratio*100:.0f}%）")
                
                # 预分配数组
                linear = np.zeros(num_points)
                
                # 计算采样点的线性特征
                sample_indices = list(range(0, num_points, sample_step))
                sample_values = []
                
                for i in tqdm(sample_indices, desc="采样计算", ncols=100):
                    k, idx, _ = kdtree.search_radius_vector_3d(high.points[i], self.radius)
                    if len(idx) >= 3:
                        neighbors = points[idx, :]
                        w = self._pca_compute(neighbors)
                        l1, l2 = w[0], w[1]
                        L = np.divide((l1 - l2), l1, out=np.zeros_like((l1 - l2)), where=l1 != 0)
                        sample_values.append(L)
                    else:
                        sample_values.append(0.0)
                
                # 使用线性插值填充未计算的点
                for i in range(len(sample_indices) - 1):
                    start_idx = sample_indices[i]
                    end_idx = sample_indices[i + 1]
                    start_val = sample_values[i]
                    end_val = sample_values[i + 1]
                    
                    # 线性插值
                    for j in range(start_idx, end_idx):
                        if end_idx > start_idx:
                            t = (j - start_idx) / (end_idx - start_idx)
                            linear[j] = start_val * (1 - t) + end_val * t
                        else:
                            linear[j] = start_val
                
                # 处理最后一个采样点之后的部分
                if sample_indices:
                    last_idx = sample_indices[-1]
                    last_val = sample_values[-1]
                    linear[last_idx:] = last_val
                
                # 转换为list格式（保持原接口）
                linear = list(linear)

            linear = np.array(linear)
            idx = np.where(linear > threshold)[0]
            line_cloud_ = high.select_by_index(idx)
            out_line_cloud_ = high.select_by_index(idx, invert=True) + low
            return line_cloud_, out_line_cloud_

    def _dbscan_clustering(self, points, eps=None, min_samples=None):
        """
        使用DBSCAN算法对点云进行聚类，使用算法优化提升性能

        :param points: 输入点云
        :param eps: 邻域半径，如果为None则使用实例变量
        :param min_samples: 最小样本数，如果为None则使用实例变量
        :return: 聚类后的标签
        """
        if eps is None:
            eps = self.eps
        if min_samples is None:
            min_samples = self.min_samples
            
        num_points = len(points)
        print(f"开始DBSCAN聚类，点数: {num_points}, eps: {eps}, min_samples: {min_samples}")
        
        # 使用优化的DBSCAN算法，不进行采样，保证数据完整性
        clustering = DBSCAN(
            eps=eps, 
            min_samples=min_samples,
            algorithm='kd_tree',    # 使用KD树算法，比球树和暴力搜索更快
            leaf_size=50,           # 增大叶子节点大小，减少树深度
            n_jobs=-1              # 使用所有CPU核心并行计算
        ).fit(points)
        
        labels = clustering.labels_
        
        # 统计聚类结果
        unique_labels = np.unique(labels)
        n_clusters = len(unique_labels) - (1 if -1 in labels else 0)
        n_noise = list(labels).count(-1)
        
        print(f"DBSCAN聚类完成: {n_clusters}个聚类, {n_noise}个噪声点")
        return labels

    def _hough_transform(self, points_2d, img_shape=(1000, 1000), threshold=100):
        """
        使用霍夫变换检测直线

        :param points_2d: 二维点云数据
        :param img_shape: 图像大小
        :param threshold: 霍夫变换阈值
        :return: 检测到的直线参数
        """
        img = np.zeros(img_shape, dtype=np.uint8)
        for point in points_2d:
            x, y = int(point[0]), int(point[1])
            if 0 <= x < img_shape[1] and 0 <= y < img_shape[0]:
                img[y, x] = 255
        lines = cv2.HoughLines(img, rho=1, theta=np.pi / 180, threshold=threshold)
        return lines

    def _filter_points_by_lines(self, points_2d, lines, distance_threshold=1.5):
        """
        根据检测到的直线过滤点云

        :param points_2d: 二维点云数据
        :param lines: 检测到的直线参数
        :param distance_threshold: 点到直线的距离阈值
        :return: 过滤后的点云
        """
        filtered_points = []
        for point in points_2d:
            x, y = point
            min_distance = float('inf')
            for line in lines:
                rho, theta = line[0]
                a = np.cos(theta)
                b = np.sin(theta)
                distance = abs(a * x + b * y - rho) / np.sqrt(a ** 2 + b ** 2)
                min_distance = min(min_distance, distance)
            if min_distance < distance_threshold:
                filtered_points.append(point)
        return np.array(filtered_points)

    def _filter_clusters(self, single_line_clouds, min_line_points=50):
        """
        筛选聚类结果，剔除点数较少的聚类

        :param single_line_clouds: 聚类后的点云列表
        :param min_line_points: 最小点数阈值，默认50
        :return: 筛选后的点云列表
        """
        filtered_line_clouds = []
        for cloud in single_line_clouds:
            if np.asarray(cloud.points).shape[0] > min_line_points:
                filtered_line_clouds.append(cloud)
        return filtered_line_clouds

    def _get_main_direction(self, points):
        """
        使用PCA获取点云的主方向（带缓存优化）
        :param points: 点云数据
        :return: 主方向向量
        """
        # 创建缓存键（使用点的数量和中心点作为标识）
        if points.shape[0] < 2:
            return np.array([1, 0, 0])
        
        # 使用点的数量和前几个点的哈希作为缓存键
        cache_key = (points.shape[0], hash(str(points[:3].flatten())))
        
        if cache_key in self._direction_cache:
            return self._direction_cache[cache_key]
        
        if points.shape[0] == 2:
            # 两点，返回连线方向
            v = points[1] - points[0]
            norm = np.linalg.norm(v)
            if norm == 0:
                result = np.array([1, 0, 0])
            else:
                result = v / norm
        else:
            from sklearn.decomposition import PCA
            pca = PCA(n_components=3)
            pca.fit(points)
            result = pca.components_[0]  # 第一主成分方向
        
        # 缓存结果
        self._direction_cache[cache_key] = result
        return result

    def _project_points_to_plane(self, points, normal_vector):
        """
        将点云投影到垂直于给定法向量的平面上

        :param points: 点云数据
        :param normal_vector: 法向量
        :return: 投影后的2D点云
        """
        # 确保法向量是单位向量
        normal_vector = normal_vector / np.linalg.norm(normal_vector)

        # 找到两个与法向量垂直的基向量
        if abs(normal_vector[0]) < abs(normal_vector[1]) and abs(normal_vector[0]) < abs(normal_vector[2]):
            v1 = np.array([0, -normal_vector[2], normal_vector[1]])
        elif abs(normal_vector[1]) < abs(normal_vector[2]):
            v1 = np.array([-normal_vector[2], 0, normal_vector[0]])
        else:
            v1 = np.array([-normal_vector[1], normal_vector[0], 0])

        v1 = v1 / np.linalg.norm(v1)
        v2 = np.cross(normal_vector, v1)

        # 投影到由v1和v2构成的平面上
        projected_points = np.zeros((points.shape[0], 2))
        for i, point in enumerate(points):
            projected_points[i, 0] = np.dot(point, v1)
            projected_points[i, 1] = np.dot(point, v2)

        return projected_points, (v1, v2)

    def _separate_individual_power_lines(self, cluster_cloud, eps_projection=0.5, min_samples_projection=5):
        """
        将电力线簇分离为单独的电力线

        :param cluster_cloud: 电力线簇点云
        :param eps_projection: 投影平面上DBSCAN的eps参数
        :param min_samples_projection: 投影平面上DBSCAN的min_samples参数
        :return: 分离后的单独电力线点云列表
        """
        points = np.asarray(cluster_cloud.points)
        if points.shape[0] < 10:  # 如果点太少则不处理
            return [cluster_cloud]

        # 获取电力线的主方向
        main_direction = self._get_main_direction(points)

        # 将点云投影到垂直于主方向的平面上
        projected_points, (v1, v2) = self._project_points_to_plane(points, main_direction)

        # 在投影平面上使用DBSCAN聚类
        projection_labels = DBSCAN(eps=eps_projection, min_samples=min_samples_projection).fit(projected_points).labels_
        unique_labels = np.unique(projection_labels)

        # 根据聚类结果分离电力线
        individual_power_lines = []
        for label in unique_labels:
            if label == -1:  # 跳过噪声点
                continue

            # 提取当前电力线的点
            mask = projection_labels == label
            line_points = points[mask]

            # 创建点云对象
            line_cloud = o3d.geometry.PointCloud()
            line_cloud.points = o3d.utility.Vector3dVector(line_points)

            individual_power_lines.append(line_cloud)

        return individual_power_lines

    def _calculate_power_line_length(self, power_line_cloud):
        """
        计算电力线的长度

        :param power_line_cloud: 电力线点云
        :return: 电力线的长度
        """
        points = np.asarray(power_line_cloud.points)
        if points.shape[0] < 2:
            return 0.0

        # 方法1：使用主方向投影计算长度
        main_direction = self._get_main_direction(points)

        # 将所有点投影到主方向上
        center = np.mean(points, axis=0)
        centered_points = points - center
        projections = np.dot(centered_points, main_direction)

        # 计算投影的最大和最小值之间的距离
        length = np.max(projections) - np.min(projections)

        return length

    def _calculate_power_line_length_path(self, power_line_cloud):
        """
        通过路径长度计算电力线的长度（更精确的方法）

        :param power_line_cloud: 电力线点云
        :return: 电力线的路径长度
        """
        points = np.asarray(power_line_cloud.points)
        if points.shape[0] < 2:
            return 0.0

        # 获取主方向
        main_direction = self._get_main_direction(points)

        # 将点投影到主方向上并排序
        center = np.mean(points, axis=0)
        centered_points = points - center
        projections = np.dot(centered_points, main_direction)

        # 按投影值排序
        sorted_indices = np.argsort(projections)
        sorted_points = points[sorted_indices]

        # 计算相邻点之间的距离并求和
        total_length = 0.0
        for i in range(1, len(sorted_points)):
            distance = np.linalg.norm(sorted_points[i] - sorted_points[i - 1])
            total_length += distance

        return total_length

    def _filter_power_lines_by_length(self, power_line_clouds, min_length=10.0, length_method='projection'):
        """
        根据长度筛选电力线，删除长度过短的电力线

        :param power_line_clouds: 电力线点云列表
        :param min_length: 最小长度阈值，默认10.0米
        :param length_method: 计算长度的方法，'projection'或'path'
        :return: 筛选后的电力线点云列表和长度信息
        """
        filtered_clouds = []
        length_info = []

        for i, cloud in tqdm(enumerate(power_line_clouds), desc="Filtering power lines by length", ncols=100):
            if length_method == 'projection':
                length = self._calculate_power_line_length(cloud)
            else:  # path method
                length = self._calculate_power_line_length_path(cloud)

            length_info.append({
                'index': i,
                'length': length,
                'points_count': np.asarray(cloud.points).shape[0],
                'kept': length >= min_length
            })

            points_count = np.asarray(cloud.points).shape[0]
            
            if length >= min_length and points_count >= 30:  # 同时检查长度和点数
                filtered_clouds.append(cloud)
            else:
                reason = []
                if length < min_length:
                    reason.append(f"length={length:.2f}m < {min_length}m")
                if points_count < 30:
                    reason.append(f"points={points_count} < 30")
                # print(f"Removed power line {i}: {', '.join(reason)}")

        return filtered_clouds, length_info

    def _validate_segments_by_catenary_fit(self, segments, projections, heights, split_points, plot_debug=False,
                                           max_rmse=0.5):
        """
        通过悬链线拟合验证分段的合理性

        :param segments: 分段后的电力线点云列表
        :param projections: 原始投影距离
        :param heights: 原始高度值
        :param split_points: 分割点索引
        :param plot_debug: 是否绘制调试图
        :param max_rmse: 悬链线拟合的最大允许误差
        :return: 验证后的有效分段列表
        """
        valid_segments = []

        # 悬链线方程: z = a * cosh((x - h) / a) + v
        def catenary(x, a, h, v):
            return a * np.cosh((x - h) / a) + v

        from scipy.optimize import curve_fit

        for i, segment in enumerate(segments):
            start_idx = split_points[i] + 1 if i > 0 else 0
            end_idx = split_points[i + 1] + 1 if i < len(split_points) - 2 else len(heights)

            segment_proj = projections[start_idx:end_idx]
            segment_heights = heights[start_idx:end_idx]

            try:
                # 初始参数估计
                v_guess = np.min(segment_heights)  # 最低点的高度
                a_guess = (np.max(segment_heights) - v_guess) / 2  # 尺度参数
                h_guess = segment_proj[np.argmin(segment_heights)]  # 最低点位置

                # 拟合悬链线
                result = curve_fit(catenary, segment_proj, segment_heights,
                                   p0=[a_guess, h_guess, v_guess], maxfev=10000)
                popt = result[0]

                # 计算拟合误差
                z_fit = catenary(segment_proj, *popt)
                rmse = np.sqrt(np.mean((segment_heights - z_fit) ** 2))

                if plot_debug:
                    import matplotlib.pyplot as plt
                    plt.figure(figsize=(10, 6))
                    plt.plot(segment_proj, segment_heights, 'b.', alpha=0.5, label='点云数据')
                    x_fit = np.linspace(min(segment_proj), max(segment_proj), 100)
                    z_fit = catenary(x_fit, *popt)
                    plt.plot(x_fit, z_fit, 'r-', label=f'悬链线拟合 (RMSE={rmse:.3f})')
                    plt.xlabel('电力线方向的距离')
                    plt.ylabel('高度')
                    plt.title(f'分段 {i + 1} 悬链线拟合')
                    plt.grid(True)
                    plt.legend()
                    plt.show()

                # 只有拟合误差小于阈值的段才认为是有效电力线段
                if rmse < max_rmse:
                    valid_segments.append(segment)
                else:
                    # print(f"分段 {i + 1} 拟合误差过大 (RMSE={rmse:.3f}), 可能不是有效电力线段")
                    continue
            except Exception as e:
                # print(f"分段 {i + 1} 拟合失败: {str(e)}")
                continue

        return valid_segments

    def _split_power_line_by_peaks(self, power_line_cloud, prominence=1.0, min_segment_points=20,
                                   window_size=15, smooth=True, plot_debug=False):
        """
        通过寻找Z方向（高度）的极大值点来分割电力线

        :param power_line_cloud: 电力线点云
        :param prominence: 极大值点的显著性阈值（越高要求峰越明显）
        :param min_segment_points: 分割后每段的最小点数
        :param window_size: 平滑窗口大小
        :param smooth: 是否对高度曲线进行平滑处理
        :param plot_debug: 是否绘制调试图
        :return: 分割后的电力线点云列表
        """
        points = np.asarray(power_line_cloud.points)
        if points.shape[0] < min_segment_points * 2:  # 如果点太少则不分割
            return [power_line_cloud]

        # 获取电力线的主方向
        main_direction = self._get_main_direction(points)

        # 将点投影到主方向上并排序
        center = np.mean(points, axis=0)
        centered_points = points - center
        projections = np.dot(centered_points, main_direction)

        # 按投影值排序
        sorted_indices = np.argsort(projections)
        sorted_points = points[sorted_indices]
        sorted_projections = projections[sorted_indices] - np.min(projections)  # 归一化投影值

        # 提取Z值序列
        z_values = sorted_points[:, 2]

        # 平滑Z值序列减少噪声影响
        if smooth and len(z_values) > window_size:
            from scipy.signal import savgol_filter
            z_smooth = savgol_filter(z_values, window_size, 3)  # 使用Savitzky-Golay滤波
        else:
            z_smooth = z_values

        # 使用scipy.signal.find_peaks寻找极大值点
        from scipy.signal import find_peaks
        peaks, properties = find_peaks(z_smooth, prominence=prominence)

        # 绘制调试图
        if plot_debug:
            import matplotlib.pyplot as plt
            plt.figure(figsize=(12, 6))
            plt.plot(sorted_projections, z_values, 'b.', alpha=0.5, label='原始高度')
            plt.plot(sorted_projections, z_smooth, 'r-', label='平滑后高度')
            plt.plot(sorted_projections[peaks], z_smooth[peaks], 'go', label='极大值点')
            plt.xlabel('电力线方向的距离')
            plt.ylabel('高度')
            plt.legend()
            plt.title(f'电力线高度剖面 (Prominence={prominence})')
            plt.grid(True)
            plt.show()

        # 如果没有找到内部极大值点，则返回原始点云
        if len(peaks) == 0:
            return [power_line_cloud]

        # 根据极大值点分割电力线
        segments = []

        # 将极大值点和端点组合成分割点
        split_points = [-1] + list(peaks) + [len(z_values)]

        # 按照分割点创建线段
        for i in range(len(split_points) - 1):
            start_idx = split_points[i] + 1 if i > 0 else 0
            end_idx = split_points[i + 1] + 1 if i < len(split_points) - 2 else len(z_values)

            if end_idx - start_idx > min_segment_points:
                segment_points = sorted_points[start_idx:end_idx]
                segment_cloud = o3d.geometry.PointCloud()
                segment_cloud.points = o3d.utility.Vector3dVector(segment_points)
                segments.append(segment_cloud)

        # 验证分段合理性：拟合悬链线模型
        valid_segments = self._validate_segments_by_catenary_fit(segments, sorted_projections, z_values,
                                                                 split_points, plot_debug)

        return valid_segments if valid_segments else [power_line_cloud]

    def _visualize_separate_power_lines(self, individual_power_lines):
        """
        可视化分离后的电力线，每条线赋予不同颜色

        :param individual_power_lines: 分离后的电力线点云列表
        :return: 带颜色的电力线点云列表
        """
        colors = [
            [1, 0, 0],  # 红
            [0, 1, 0],  # 绿
            [0, 0, 1],  # 蓝
            [1, 1, 0],  # 黄
            [1, 0, 1],  # 洋红
            [0, 1, 1],  # 青
            [0.5, 0.5, 0],  # 橄榄
            [0.5, 0, 0.5],  # 紫
            [0, 0.5, 0.5],  # 蓝绿
            [1, 0.5, 0]  # 橙
        ]

        colored_lines = []
        for i, line_cloud in enumerate(individual_power_lines):
            color = colors[i % len(colors)]
            num_points = np.asarray(line_cloud.points).shape[0]
            line_cloud.colors = o3d.utility.Vector3dVector(np.tile(color, (num_points, 1)))
            colored_lines.append(line_cloud)

        return colored_lines

    def _fit_power_line_model(self, power_line_points):
        """
        对每条电力线拟合3D曲线模型

        :param power_line_points: 电力线点云数据
        :return: 拟合的模型参数
        """
        # 简化为对x,y坐标拟合z的函数
        # 实际应用中可能需要更复杂的曲线拟合方法
        X = power_line_points[:, :2]  # x, y坐标
        y = power_line_points[:, 2]  # z坐标

        try:
            ransac = RANSACRegressor().fit(X, y)
            return ransac
        except:
            return None

    def _transform_coordinates(self, power_line_cloud, reference_point_method='center'):
        """
        对电力线点云进行坐标变换，将参考点的x,y坐标设为0，其他点顺势平移

        :param power_line_cloud: 电力线点云
        :param reference_point_method: 参考点选择方法
            - 'center': 使用点云中心点
            - 'min_z': 使用最低点
            - 'max_z': 使用最高点
            - 'start': 使用起点（按主方向排序后的第一个点）
            - 'end': 使用终点（按主方向排序后的最后一个点）
        :return: 变换后的点云和变换参数
        """
        points = np.asarray(power_line_cloud.points)
        if points.shape[0] == 0:
            return power_line_cloud, None

        # 根据方法选择参考点
        if reference_point_method == 'center':
            # 使用点云中心点
            reference_point = np.mean(points, axis=0)
        elif reference_point_method == 'min_z':
            # 使用最低点
            min_z_idx = np.argmin(points[:, 2])
            reference_point = points[min_z_idx]
        elif reference_point_method == 'max_z':
            # 使用最高点
            max_z_idx = np.argmax(points[:, 2])
            reference_point = points[max_z_idx]
        elif reference_point_method in ['start', 'end']:
            # 使用起点或终点（按主方向排序）
            main_direction = self._get_main_direction(points)
            center = np.mean(points, axis=0)
            centered_points = points - center
            projections = np.dot(centered_points, main_direction)

            if reference_point_method == 'start':
                # 使用投影值最小的点（起点）
                start_idx = np.argmin(projections)
                reference_point = points[start_idx]
            else:
                # 使用投影值最大的点（终点）
                end_idx = np.argmax(projections)
                reference_point = points[end_idx]
        else:
            # 默认使用中心点
            reference_point = np.mean(points, axis=0)

        # 计算平移向量（只平移x,y坐标，z坐标保持不变）
        translation_vector = np.array([reference_point[0], reference_point[1], 0])

        # 应用平移变换
        transformed_points = points - translation_vector

        # 创建新的点云对象
        transformed_cloud = o3d.geometry.PointCloud()
        transformed_cloud.points = o3d.utility.Vector3dVector(transformed_points)

        # 如果有颜色信息，也复制过来
        if power_line_cloud.has_colors():
            transformed_cloud.colors = power_line_cloud.colors

        # 返回变换后的点云和变换参数
        transform_info = {
            'reference_point_method': reference_point_method,
            'original_reference_point': reference_point.tolist(),
            'translation_vector': translation_vector.tolist(),
            'transformed_reference_point': [0, 0, reference_point[2]]  # x,y设为0，z保持不变
        }

        return transformed_cloud, transform_info

    def _transform_all_power_lines(self, power_line_clouds, reference_point_method='center'):
        """
        对所有电力线进行坐标变换，确保所有电力线共用一个坐标系

        :param power_line_clouds: 电力线点云列表
        :param reference_point_method: 参考点选择方法
        :return: 变换后的电力线点云列表和变换信息列表
        """
        if not power_line_clouds:
            return [], []

        # 方法1：计算全局参考点（所有电力线的中心点）
        all_points = []
        for cloud in power_line_clouds:
            points = np.asarray(cloud.points)
            all_points.append(points)

        all_points_combined = np.vstack(all_points)

        # 根据方法选择全局参考点
        if reference_point_method == 'center':
            # 使用所有电力线的中心点
            global_reference_point = np.mean(all_points_combined, axis=0)
        elif reference_point_method == 'min_z':
            # 使用所有电力线中的最低点
            min_z_idx = np.argmin(all_points_combined[:, 2])
            global_reference_point = all_points_combined[min_z_idx]
        elif reference_point_method == 'max_z':
            # 使用所有电力线中的最高点
            max_z_idx = np.argmax(all_points_combined[:, 2])
            global_reference_point = all_points_combined[max_z_idx]
        elif reference_point_method in ['start', 'end']:
            # 使用第一条电力线的起点或终点作为全局参考点
            first_cloud_points = np.asarray(power_line_clouds[0].points)
            main_direction = self._get_main_direction(first_cloud_points)
            center = np.mean(first_cloud_points, axis=0)
            centered_points = first_cloud_points - center
            projections = np.dot(centered_points, main_direction)

            if reference_point_method == 'start':
                start_idx = np.argmin(projections)
                global_reference_point = first_cloud_points[start_idx]
            else:
                end_idx = np.argmax(projections)
                global_reference_point = first_cloud_points[end_idx]
        else:
            # 默认使用所有电力线的中心点
            global_reference_point = np.mean(all_points_combined, axis=0)

        # 计算全局平移向量（只平移x,y坐标，z坐标保持不变）
        global_translation_vector = np.array([global_reference_point[0], global_reference_point[1], 0])

        print(
            f"全局参考点: ({global_reference_point[0]:.2f}, {global_reference_point[1]:.2f}, {global_reference_point[2]:.2f})")
        print(
            f"全局平移向量: [{global_translation_vector[0]:.2f}, {global_translation_vector[1]:.2f}, {global_translation_vector[2]:.2f}]")

        # 对所有电力线应用相同的全局变换
        transformed_clouds = []
        transform_infos = []

        for i, cloud in enumerate(power_line_clouds):
            points = np.asarray(cloud.points)

            # 应用全局平移变换
            transformed_points = points - global_translation_vector

            # 创建新的点云对象
            transformed_cloud = o3d.geometry.PointCloud()
            transformed_cloud.points = o3d.utility.Vector3dVector(transformed_points)

            # 如果有颜色信息，也复制过来
            if cloud.has_colors():
                transformed_cloud.colors = cloud.colors

            transformed_clouds.append(transformed_cloud)

            # 记录变换信息
            transform_info = {
                'power_line_index': i,
                'reference_point_method': reference_point_method,
                'global_reference_point': global_reference_point.tolist(),
                'global_translation_vector': global_translation_vector.tolist(),
                'transformed_reference_point': [0, 0, global_reference_point[2]],  # x,y设为0，z保持不变
                'point_count': len(points)
            }
            transform_infos.append(transform_info)

            # print(f"电力线 {i}: {len(points)} 个点，变换后参考点 (0.00, 0.00, {global_reference_point[2]:.2f})")

        return transformed_clouds, transform_infos

    def _verify_coordinate_transformation(self, original_clouds, transformed_clouds, transform_infos):
        """
        验证坐标变换的正确性

        :param original_clouds: 原始电力线点云列表
        :param transformed_clouds: 变换后的电力线点云列表
        :param transform_infos: 变换信息列表
        :return: 验证结果
        """
        if not transform_infos:
            return True

        # 获取全局平移向量（所有电力线应该使用相同的平移向量）
        global_translation_vector = np.array(transform_infos[0]['global_translation_vector'])

        print(f"全局平移向量: {global_translation_vector}")

        # 验证所有电力线是否使用相同的平移向量
        for i, transform_info in enumerate(transform_infos):
            current_translation = np.array(transform_info['global_translation_vector'])
            if not np.allclose(current_translation, global_translation_vector):
                print(f"错误：电力线 {i} 使用了不同的平移向量")
                return False

        # 验证变换后的点云
        for i, (original_cloud, transformed_cloud) in enumerate(zip(original_clouds, transformed_clouds)):
            original_points = np.asarray(original_cloud.points)
            transformed_points = np.asarray(transformed_cloud.points)

            # 验证变换公式：transformed_points = original_points - global_translation_vector
            expected_transformed = original_points - global_translation_vector

            if not np.allclose(transformed_points, expected_transformed, atol=1e-6):
                print(f"错误：电力线 {i} 的坐标变换不正确")
                return False

            # 验证参考点是否被正确移动到(0,0,z)
            reference_point = np.array(transform_infos[i]['global_reference_point'])
            expected_reference = reference_point - global_translation_vector
            if not np.allclose(expected_reference[:2], [0, 0], atol=1e-6):
                print(f"错误：电力线 {i} 的参考点未正确移动到(0,0,z)")
                return False

        print("所有电力线共用一个坐标系")
        return True

    def _merge_broken_lines(self, power_line_clouds, distance_threshold=2.0, angle_threshold_deg=20):
        """
        自动拼接断裂的线段：端点距离+方向一致性

        :param power_line_clouds: 电力线点云列表
        :param distance_threshold: 端点距离阈值（米）
        :param angle_threshold_deg: 方向夹角阈值（度）
        :return: 拼接后的电力线点云列表
        """
        import math
        merged = [False] * len(power_line_clouds)
        lines = []
        # 先提取每条线的端点和主方向
        endpoints = []
        directions = []
        for cloud in power_line_clouds:
            points = np.asarray(cloud.points)
            if len(points) < 2:
                endpoints.append((points[0], points[0]))
                directions.append(np.array([1,0,0]))
                continue
            # 按主方向排序
            main_dir = self._get_main_direction(points)
            center = np.mean(points, axis=0)
            projections = np.dot(points - center, main_dir)
            idx_sort = np.argsort(projections)
            sorted_points = points[idx_sort]
            start, end = sorted_points[0], sorted_points[-1]
            endpoints.append((start, end))
            directions.append(main_dir)
            lines.append(sorted_points)

        # 标记已合并
        used = [False] * len(lines)
        merged_lines = []
        for i in range(len(lines)):
            if used[i]:
                continue
            cur_line = lines[i]
            cur_dir = directions[i]
            cur_start, cur_end = endpoints[i]
            changed = True
            while changed:
                changed = False
                for j in range(len(lines)):
                    if i == j or used[j]:
                        continue
                    other_line = lines[j]
                    other_dir = directions[j]
                    other_start, other_end = endpoints[j]
                    # 计算端点距离
                    dist_end2start = np.linalg.norm(cur_end - other_start)
                    dist_end2end = np.linalg.norm(cur_end - other_end)
                    dist_start2start = np.linalg.norm(cur_start - other_start)
                    dist_start2end = np.linalg.norm(cur_start - other_end)
                    # 计算方向夹角
                    angle = lambda v1, v2: np.degrees(np.arccos(np.clip(np.dot(v1, v2) / (np.linalg.norm(v1)*np.linalg.norm(v2)), -1, 1)))
                    # 只考虑end->start拼接
                    if dist_end2start < distance_threshold and angle(cur_dir, other_dir) < angle_threshold_deg:
                        # 插值补点
                        if dist_end2start > 2.0:
                            num_interp = int(dist_end2start // 1.0)
                            interp_points = [cur_end + (other_start - cur_end) * (k / (num_interp + 1)) for k in range(1, num_interp + 1)]
                            cur_line = np.vstack([cur_line, interp_points, other_line])
                        else:
                            cur_line = np.vstack([cur_line, other_line])
                        cur_end = other_end
                        cur_dir = self._get_main_direction(cur_line)
                        used[j] = True
                        changed = True
                        break
                    # 也可以考虑end->end, start->start, start->end
                    elif dist_end2end < distance_threshold and angle(cur_dir, -other_dir) < angle_threshold_deg:
                        # 反转other_line
                        if dist_end2end > 2.0:
                            num_interp = int(dist_end2end // 1.0)
                            interp_points = [cur_end + (other_end - cur_end) * (k / (num_interp + 1)) for k in range(1, num_interp + 1)]
                            cur_line = np.vstack([cur_line, interp_points, other_line[::-1]])
                        else:
                            cur_line = np.vstack([cur_line, other_line[::-1]])
                        cur_end = other_start
                        cur_dir = self._get_main_direction(cur_line)
                        used[j] = True
                        changed = True
                        break
                    elif dist_start2start < distance_threshold and angle(-cur_dir, other_dir) < angle_threshold_deg:
                        # 反转cur_line
                        if dist_start2start > 2.0:
                            num_interp = int(dist_start2start // 1.0)
                            interp_points = [cur_start + (other_start - cur_start) * (k / (num_interp + 1)) for k in range(1, num_interp + 1)]
                            cur_line = np.vstack([cur_line[::-1], interp_points, other_line])
                        else:
                            cur_line = np.vstack([cur_line[::-1], other_line])
                        cur_start = other_end
                        cur_dir = self._get_main_direction(cur_line)
                        used[j] = True
                        changed = True
                        break
                    elif dist_start2end < distance_threshold and angle(-cur_dir, -other_dir) < angle_threshold_deg:
                        # 反转cur_line和other_line
                        if dist_start2end > 2.0:
                            num_interp = int(dist_start2end // 1.0)
                            interp_points = [cur_start + (other_end - cur_start) * (k / (num_interp + 1)) for k in range(1, num_interp + 1)]
                            cur_line = np.vstack([cur_line[::-1], interp_points, other_line[::-1]])
                        else:
                            cur_line = np.vstack([cur_line[::-1], other_line[::-1]])
                        cur_start = other_start
                        cur_dir = self._get_main_direction(cur_line)
                        used[j] = True
                        changed = True
                        break
            used[i] = True
            # 合并后生成新的点云对象
            merged_cloud = o3d.geometry.PointCloud()
            merged_cloud.points = o3d.utility.Vector3dVector(cur_line)
            merged_lines.append(merged_cloud)
        return merged_lines

    def _iterative_merge_broken_lines(self, power_line_clouds, initial_distance=2.0, initial_angle=15, max_rounds=3, distance_step=1.0, angle_step=5):
        """
        智能断裂线段合并，基于空间线性聚类理论
        :param power_line_clouds: 电力线点云列表
        :param initial_distance: 初始端点距离阈值
        :param initial_angle: 初始方向夹角阈值  
        :param max_rounds: 最大迭代轮数
        :param distance_step: 每轮距离阈值递增
        :param angle_step: 每轮角度阈值递增
        :return: 拼接后的电力线点云列表
        """
        import time
        start_time = time.time()
        print(f"开始智能合并断裂线段，共{len(power_line_clouds)}条...")
        
        merged_lines = power_line_clouds
        
        # 第一步：基于共线性的预合并
        step1_start = time.time()
        merged_lines = self._collinearity_based_merge(merged_lines)
        step1_time = time.time() - step1_start
        print(f"共线性预合并完成，耗时{step1_time:.2f}秒")
        
        # # 第二步：多轮渐进式合并
        # step2_start = time.time()
        # for round in range(max_rounds):
        #     distance = initial_distance + round * distance_step
        #     angle = initial_angle + round * angle_step
        #     before = len(merged_lines)
            
        #     # 使用改进的合并策略
        #     round_start = time.time()
        #     merged_lines = self._enhanced_merge_broken_lines(merged_lines, 
        #                                                    distance_threshold=distance, 
        #                                                    angle_threshold_deg=angle,
        #                                                    round_num=round)
        #     round_time = time.time() - round_start
        #     after = len(merged_lines)
        #     print(f"[合并轮次 {round+1}] 距离阈值: {distance:.1f}m, 角度阈值: {angle}°，线段数: {before} → {after}，耗时{round_time:.2f}秒")
            
        #     # 如果连续两轮无变化，提前退出
        #     if after == before and round > 0:
        #         print(f"第{round+1}轮无变化，合并完成")
        #         break
        # step2_time = time.time() - step2_start
        
        # # 第三步：基于距离统计的最终整理
        # step3_start = time.time()
        # merged_lines = self._statistical_distance_refinement(merged_lines)
        # step3_time = time.time() - step3_start
        
        # # 第四步：针对明显断裂的长线段进行二次合并
        # step4_start = time.time()
        # final_merged_lines = self._secondary_merge_for_long_lines(merged_lines)
        # step4_time = time.time() - step4_start
        
        # total_time = time.time() - start_time
        # print(f"断裂线段合并完成，最终保留{len(final_merged_lines)}条电力线")
        # print(f"时间统计 - 共线性预合并: {step1_time:.2f}s，渐进式合并: {step2_time:.2f}s，距离整理: {step3_time:.2f}s，二次合并: {step4_time:.2f}s，总耗时: {total_time:.2f}s")
        # return final_merged_lines
        return merged_lines
    
    def _collinearity_based_merge(self, power_line_clouds, max_distance=50.0, angle_threshold_deg=15.0, prefer_closest=False):
        """
        基于共线性的预合并：合并明显共线的短线段（优化版本）
        
        :param power_line_clouds: 电力线点云列表
        :param max_distance: 最大合并距离阈值
        :param angle_threshold_deg: 角度阈值（度）
        :param prefer_closest: 是否优先合并距离最近的线段（True=二次合并模式，False=常规合并模式）
        """
        if len(power_line_clouds) <= 1:
            return power_line_clouds
            
        merged = []
        used = [False] * len(power_line_clouds)
        
        # 预计算所有线段的端点和方向（避免重复计算）
        line_info = []
        for i, cloud in enumerate(power_line_clouds):
            points = np.asarray(cloud.points)
            if len(points) < 2:
                line_info.append({
                    'start': points[0] if len(points) > 0 else np.array([0, 0, 0]),
                    'end': points[0] if len(points) > 0 else np.array([0, 0, 0]),
                    'direction': np.array([1, 0, 0]),
                    'valid': False
                })
                continue
            
            start_point = points[0]
            end_point = points[-1]
            direction = self._get_main_direction(points)
            
            line_info.append({
                'start': start_point,
                'end': end_point,
                'direction': direction,
                'valid': True
            })
        
        # print("共线性预合并...")
        for i, cloud_i in enumerate(tqdm(power_line_clouds, desc="共线性检查", ncols=100)):
            if used[i]:
                continue
                
            if not line_info[i]['valid']:
                merged.append(cloud_i)
                used[i] = True
                continue
                
            start_i = line_info[i]['start']
            end_i = line_info[i]['end']
            dir_i = line_info[i]['direction']
            
            if prefer_closest:
                # 二次合并模式：查找共线的线段，并记录距离信息，优先合并最近的
                collinear_candidates = []
                for j, cloud_j in enumerate(power_line_clouds):
                    if used[j] or i == j or not line_info[j]['valid']:
                        continue
                        
                    start_j = line_info[j]['start']
                    end_j = line_info[j]['end']
                    
                    # 向量化计算端点距离
                    endpoints1 = np.array([start_i, end_i])
                    endpoints2 = np.array([start_j, end_j])
                    distances = np.linalg.norm(endpoints1[:, np.newaxis, :] - endpoints2[np.newaxis, :, :], axis=2)
                    min_dist = float(np.min(distances))
                    
                    # 只有端点距离满足条件才检查角度共线性
                    if min_dist <= max_distance:
                        # 检查角度是否共线
                        cos_angle = np.abs(np.dot(dir_i, line_info[j]['direction']))
                        angle_deg = np.degrees(np.arccos(np.clip(cos_angle, 0, 1)))
                        
                        if angle_deg < angle_threshold_deg:
                            collinear_candidates.append((j, min_dist))
                
                # 如果有多个共线候选，按距离排序，优先合并距离最近的
                if collinear_candidates:
                    # 按距离排序，距离近的优先
                    collinear_candidates.sort(key=lambda x: x[1])
                    
                    # 只选择距离最近的一条进行合并
                    best_match_idx = collinear_candidates[0][0]
                    collinear_group = [i, best_match_idx]
                    
                    all_points = []
                    for idx in collinear_group:
                        all_points.extend(np.asarray(power_line_clouds[idx].points))
                        used[idx] = True
                    
                    # 创建合并后的点云
                    merged_cloud = o3d.geometry.PointCloud()
                    merged_cloud.points = o3d.utility.Vector3dVector(np.array(all_points))
                    merged.append(merged_cloud)
                    
                    # 输出合并信息
                    min_distance = collinear_candidates[0][1]
                    # print(f"  合并线段 {i} 和 {best_match_idx}，距离: {min_distance:.2f}m（共有{len(collinear_candidates)}个候选）")
                else:
                    merged.append(cloud_i)
                    used[i] = True
            else:
                # 常规合并模式：查找所有共线的线段并全部合并
                collinear_group = [i]
                for j, cloud_j in enumerate(power_line_clouds):
                    if used[j] or i == j or not line_info[j]['valid']:
                        continue
                        
                    start_j = line_info[j]['start']
                    end_j = line_info[j]['end']
                    
                    # 向量化计算端点距离
                    endpoints1 = np.array([start_i, end_i])
                    endpoints2 = np.array([start_j, end_j])
                    distances = np.linalg.norm(endpoints1[:, np.newaxis, :] - endpoints2[np.newaxis, :, :], axis=2)
                    min_dist = float(np.min(distances))
                    
                    # 检查距离和角度
                    if min_dist <= max_distance:
                        cos_angle = np.abs(np.dot(dir_i, line_info[j]['direction']))
                        angle_deg = np.degrees(np.arccos(np.clip(cos_angle, 0, 1)))
                        
                        if angle_deg < angle_threshold_deg:
                            collinear_group.append(j)
                
                # 合并共线线段
                if len(collinear_group) > 1:
                    all_points = []
                    for idx in collinear_group:
                        all_points.extend(np.asarray(power_line_clouds[idx].points))
                        used[idx] = True
                    
                    # 创建合并后的点云
                    merged_cloud = o3d.geometry.PointCloud()
                    merged_cloud.points = o3d.utility.Vector3dVector(np.array(all_points))
                    merged.append(merged_cloud)
                else:
                    merged.append(cloud_i)
                    used[i] = True
        
        return merged

    def _are_collinear(self, points1, points2, threshold=0.5, max_distance=30.0, angle_threshold_deg=20.0):
        """
        检查两组点是否共线且距离合理（优化版本）
        
        :param points1: 第一组点
        :param points2: 第二组点
        :param threshold: 共线性阈值
        :param max_distance: 最大距离阈值
        :param angle_threshold_deg: 角度阈值（度）
        """
        # 首先进行快速距离检查（避免不必要的方向计算）
        start1, end1 = points1[0], points1[-1]
        start2, end2 = points2[0], points2[-1]
        
        # 向量化计算所有端点间距离
        endpoints1 = np.array([start1, end1])
        endpoints2 = np.array([start2, end2])
        
        # 计算所有端点组合的距离
        distances = np.linalg.norm(endpoints1[:, np.newaxis, :] - endpoints2[np.newaxis, :, :], axis=2)
        min_dist = float(np.min(distances))
        
        # 如果距离太远，直接返回False
        if min_dist > max_distance:
            return False
        
        # 获取两条线的主方向（使用缓存）
        dir1 = self._get_main_direction(points1)
        dir2 = self._get_main_direction(points2)
        
        # 计算方向夹角（使用向量化操作）
        cos_angle = np.abs(np.dot(dir1, dir2))
        angle_deg = np.degrees(np.arccos(np.clip(cos_angle, 0, 1)))
        
        return angle_deg < angle_threshold_deg  # 方向夹角小于阈值认为共线
    
    def _enhanced_merge_broken_lines(self, power_line_clouds, distance_threshold, angle_threshold_deg, round_num):
        """
        增强的断裂线段合并，使用改进策略
        """
        # 第一轮使用较严格的标准，后续轮次逐渐放宽
        if round_num == 0:
            # 严格合并：只合并明显断裂的线段
            return self._merge_broken_lines(power_line_clouds, distance_threshold, angle_threshold_deg)
        else:
            # 宽松合并：考虑更多可能的连接
            return self._flexible_merge_broken_lines(power_line_clouds, distance_threshold, angle_threshold_deg)
    
    def _flexible_merge_broken_lines(self, power_line_clouds, distance_threshold, angle_threshold_deg):
        """
        灵活的断裂线段合并，允许更多连接可能性
        """
        if len(power_line_clouds) <= 1:
            return power_line_clouds
        
        # 使用原有的合并逻辑，但稍微放宽参数
        return self._merge_broken_lines(power_line_clouds, 
                                      distance_threshold * 1.2, 
                                      angle_threshold_deg * 1.3)
    
    def _statistical_distance_refinement(self, power_line_clouds):
        """
        基于距离统计的最终整理
        """
        if len(power_line_clouds) <= 2:
            return power_line_clouds
        
        # 计算所有线段间的最小距离
        distances = []
        for i in range(len(power_line_clouds)):
            for j in range(i+1, len(power_line_clouds)):
                points_i = np.asarray(power_line_clouds[i].points)
                points_j = np.asarray(power_line_clouds[j].points)
                
                min_dist = float('inf')
                for p1 in [points_i[0], points_i[-1]]:
                    for p2 in [points_j[0], points_j[-1]]:
                        dist = np.linalg.norm(p1 - p2)
                        min_dist = min(min_dist, dist)
                distances.append(min_dist)
        
        if not distances:
            return power_line_clouds
        
        # 使用统计方法确定合并阈值
        mean_dist = np.mean(distances)
        std_dist = np.std(distances)
        adaptive_threshold = mean_dist - 0.5 * std_dist
        
        if adaptive_threshold > 0 and adaptive_threshold < 3.0:
            # 使用自适应阈值进行最终合并
            return self._merge_broken_lines(power_line_clouds, 
                                          float(adaptive_threshold), 
                                          angle_threshold_deg=10)
        else:
            return power_line_clouds
    
    def _secondary_merge_for_long_lines(self, power_line_clouds):
        """
        针对明显断裂的长线段进行二次合并（更激进但保持谨慎）
        """
        if len(power_line_clouds) <= 1:
            return power_line_clouds
        
        # print(f"开始二次合并，当前{len(power_line_clouds)}条线段...")
        
        # 第一步：识别潜在的长线段（基于长度和方向一致性）
        long_line_candidates = self._identify_long_line_candidates(power_line_clouds)
        
        # 第二步：对长线段候选进行更激进的合并
        merged_lines = self._aggressive_merge_candidates(power_line_clouds, long_line_candidates)
        
        # # print(f"二次合并完成：{len(power_line_clouds)}条 → {len(merged_lines)}条")
        return merged_lines
    
    def _identify_long_line_candidates(self, power_line_clouds):
        """
        识别潜在的长线段候选（基于长度和空间分布）
        """
        candidates = []
        
        # 计算每条线的基本信息
        line_info = []
        # print("分析候选长线段...")
        for i, cloud in enumerate(tqdm(power_line_clouds, desc="分析线段", ncols=100)):
            points = np.asarray(cloud.points)
            if len(points) < 5:
                continue
                
            length = self._calculate_power_line_length(cloud)
            direction = self._get_main_direction(points)
            center = np.mean(points, axis=0)
            
            # 计算端点
            projections = np.dot(points - center, direction)
            min_idx = np.argmin(projections)
            max_idx = np.argmax(projections)
            start_point = points[min_idx]
            end_point = points[max_idx]
            
            line_info.append({
                'index': i,
                'length': length,
                'direction': direction,
                'center': center,
                'start': start_point,
                'end': end_point,
                'points': points
            })
        
        # 按长度排序，优先处理较长的线段
        line_info.sort(key=lambda x: x['length'], reverse=True)
        
        # 寻找可能断裂的长线段
        used = set()
        for info in line_info:
            if info['index'] in used or info['length'] < 20:  # 只考虑长度>=20米的线段
                continue
                
            # 寻找与当前线段方向一致且距离较近的其他线段
            candidate_group = [info['index']]
            
            for other_info in line_info:
                if (other_info['index'] in used or 
                    other_info['index'] == info['index'] or 
                    other_info['length'] < 10):  # 其他线段至少10米
                    continue
                
                # 检查方向一致性（允许更大的角度差异）
                angle = np.degrees(np.arccos(np.clip(
                    np.abs(np.dot(info['direction'], other_info['direction'])), 0, 1)))
                
                if angle > 15:  # 方向差异不能超过15度
                    continue
                
                # 检查空间距离（端点之间的最近距离）
                min_dist = float('inf')
                for p1 in [info['start'], info['end']]:
                    for p2 in [other_info['start'], other_info['end']]:
                        dist = np.linalg.norm(p1 - p2)
                        min_dist = min(min_dist, dist)
                
                if min_dist < 5.0:  # 端点距离小于5米
                    candidate_group.append(other_info['index'])
            
            if len(candidate_group) > 1:
                candidates.append(candidate_group)
                used.update(candidate_group)
        
        return candidates
    
    def _aggressive_merge_candidates(self, power_line_clouds, candidates):
        """
        对长线段候选进行更激进的合并
        """
        merged_lines = []
        used = set()
        
        # print("合并候选线段组...")
        for candidate_group in tqdm(candidates, desc="合并候选组", ncols=100):
            if len(candidate_group) <= 1:
                continue
                
            # 合并候选组中的所有线段
            all_points = []
            for idx in candidate_group:
                if idx < len(power_line_clouds):
                    points = np.asarray(power_line_clouds[idx].points)
                    all_points.extend(points)
                    used.add(idx)
            
            if all_points:
                # 按主方向排序所有点
                all_points = np.array(all_points)
                main_dir = self._get_main_direction(all_points)
                center = np.mean(all_points, axis=0)
                projections = np.dot(all_points - center, main_dir)
                sorted_indices = np.argsort(projections)
                sorted_points = all_points[sorted_indices]
                
                # 创建合并后的点云
                merged_cloud = o3d.geometry.PointCloud()
                merged_cloud.points = o3d.utility.Vector3dVector(sorted_points)
                merged_lines.append(merged_cloud)
                
                # print(f"合并了{len(candidate_group)}条线段，总长度: {self._calculate_power_line_length(merged_cloud):.1f}米")
        
        # 添加未参与合并的线段
        for i, cloud in enumerate(power_line_clouds):
            if i not in used:
                merged_lines.append(cloud)
        
        return merged_lines

    def _align_parallel_lines(self, power_line_clouds, direction_angle_threshold=8):
        """
        温和的端点对齐，让并行电力线看起来更整齐
        :param power_line_clouds: 电力线点云列表
        :param direction_angle_threshold: 主方向夹角阈值（度）
        :return: 端点对齐后的电力线点云列表
        """
        # print(f"正在进行端点对齐，共{len(power_line_clouds)}条电力线...")
        
        if len(power_line_clouds) <= 1:
            return power_line_clouds
            
        # 1. 计算每条线的主方向和端点
        line_info = []
        for i, cloud in enumerate(power_line_clouds):
            points = np.asarray(cloud.points)
            if len(points) < 3:
                line_info.append(None)
                continue
                
            main_dir = self._get_main_direction(points)
            # 统一方向（避免方向相反的情况）
            if main_dir[0] < 0:
                main_dir = -main_dir
                
            # 计算端点
            center = np.mean(points, axis=0)
            projections = np.dot(points - center, main_dir)
            min_idx = np.argmin(projections)
            max_idx = np.argmax(projections)
            start_point = points[min_idx]
            end_point = points[max_idx]
            
            line_info.append({
                'points': points,
                'direction': main_dir,
                'start': start_point,
                'end': end_point,
                'center': center
            })
        
        # 2. 按方向分组（只有方向非常接近的才分组）
        groups = []
        used = [False] * len(line_info)
        
        for i, info_i in enumerate(line_info):
            if used[i] or info_i is None:
                continue
                
            group = [i]
            used[i] = True
            
            for j, info_j in enumerate(line_info):
                if used[j] or info_j is None or i == j:
                    continue
                    
                # 计算方向夹角
                angle = np.degrees(np.arccos(np.clip(
                    np.dot(info_i['direction'], info_j['direction']), -1, 1)))
                
                # 只有方向非常接近的才分组
                if angle < direction_angle_threshold:
                    group.append(j)
                    used[j] = True
            
            groups.append(group)
        
        # 3. 对每组进行温和的端点对齐
        aligned_lines = []
        
        for group in groups:
            if len(group) == 1:
                # 单独的线不需要对齐
                aligned_lines.append(power_line_clouds[group[0]])
                continue
                
                # print(f"对齐组：{len(group)}条并行电力线")
            
            # 计算组平均方向
            group_directions = [line_info[idx]['direction'] for idx in group]
            avg_direction = np.mean(group_directions, axis=0)
            avg_direction = avg_direction / np.linalg.norm(avg_direction)
            
            # 收集所有端点
            all_starts = []
            all_ends = []
            
            for idx in group:
                info = line_info[idx]
                # 重新计算在平均方向上的投影
                start_proj = np.dot(info['start'] - info['center'], avg_direction)
                end_proj = np.dot(info['end'] - info['center'], avg_direction)
                
                if start_proj < end_proj:
                    all_starts.append(info['start'])
                    all_ends.append(info['end'])
                else:
                    all_starts.append(info['end']) 
                    all_ends.append(info['start'])
            
            # 计算对齐的端点（使用中位数，更稳定）
            aligned_start = np.median(all_starts, axis=0)
            aligned_end = np.median(all_ends, axis=0)
            
            # 对每条线进行温和的端点调整
            for idx in group:
                points = line_info[idx]['points'].copy()
                
                # 找到当前线的端点
                center = line_info[idx]['center']
                projections = np.dot(points - center, avg_direction)
                min_idx = np.argmin(projections)
                max_idx = np.argmax(projections)
                
                # 温和地调整端点（只调整最前面和最后面的几个点）
                adjust_ratio = 0.3  # 调整强度，较小的值保持原形状
                
                points[min_idx] = points[min_idx] * (1 - adjust_ratio) + aligned_start * adjust_ratio
                points[max_idx] = points[max_idx] * (1 - adjust_ratio) + aligned_end * adjust_ratio
                
                # 创建对齐后的点云
                aligned_cloud = o3d.geometry.PointCloud()
                aligned_cloud.points = o3d.utility.Vector3dVector(points)
                
                # 保持原有颜色
                if power_line_clouds[idx].has_colors():
                    aligned_cloud.colors = power_line_clouds[idx].colors
                    
                aligned_lines.append(aligned_cloud)
        
        # print(f"端点对齐完成，保持了所有{len(aligned_lines)}条电力线")
        return aligned_lines

    def _safe_visualize(self, geometries, window_name="Point Cloud", width=1200, height=800):
        """
        安全的可视化函数，处理可视化模块不可用的情况
        """
        try:
            # print(f"正在显示可视化窗口: {window_name}")
            # print(f"几何对象数量: {len(geometries)}")
            # 检查几何对象是否有效
            # for i, geom in enumerate(geometries):
            #     if hasattr(geom, 'points'):
            #         print(f"  对象 {i}: {len(geom.points)} 个点")
            #     else:
            #         print(f"  对象 {i}: 无点云数据")
            
            # 直接调用Open3D可视化
            o3d.visualization.draw_geometries(geometries, window_name=window_name, width=width, height=height)
            # print(f"可视化窗口已关闭: {window_name}")
        except Exception as e:
            print(f"可视化出现错误: {window_name}")
            print(f"错误详情: {str(e)}")
            print(f"错误类型: {type(e).__name__}")
            import traceback
            traceback.print_exc()

    def extract(self, input_file, save_line_cloud=None, save_out_cloud=None,
                min_line_points=50, min_line_length=10.0, length_method='projection',
                reference_point_method='center', visualize_steps=None, use_dynamic_params=True):
        """
        完整的电力线提取和可视化流程

        :param input_file: 输入文件路径
        :param save_line_cloud: 保存线点云的文件路径（可选）
        :param save_out_cloud: 保存非线点云的文件路径（可选）
        :param min_line_points: 聚类筛选的最小点数阈值，默认50
        :param min_line_length: 最小电力线长度阈值，默认10.0米
        :param length_method: 计算长度的方法，'projection'或'path'
        :param reference_point_method: 坐标变换的全局参考点选择方法
        :param visualize_steps: 是否在每步后进行可视化，None表示使用类属性enable_visualization
        :return: 变换后的电力线点云列表
        """
        # 记录程序开始时间
        import time
        program_start_time = time.time()
        
        # 确定是否开启可视化
        if visualize_steps is None:
            visualize_steps = self.enable_visualization
        
        print("=" * 60)
        print("开始电力线提取流程")
        print(f"可视化模式: {'开启' if visualize_steps else '关闭'}")
        print(f"开始时间: {time.strftime('%Y-%m-%d %H:%M:%S')}")
        print("=" * 60)
        
        # 步骤1: 读取点云数据
        print("\n步骤1：读取原始点云...")
        step1_start = time.time()
        point_cloud = self._read_point_cloud(input_file)
        step1_time = time.time() - step1_start
        print(f"原始点云包含 {len(point_cloud.points)} 个点，耗时{step1_time:.2f}秒")
        
        # 步骤2: 线性特征提取
        print("\n步骤2：计算线性特征并分割...")
        step2_start = time.time()
        line_cloud, out_line_cloud = self._power_line_segmentation(point_cloud, use_dynamic_params=use_dynamic_params)
        step2_time = time.time() - step2_start
        print(f"线性特征点云: {len(line_cloud.points)} 个点")
        print(f"非线性特征点云: {len(out_line_cloud.points)} 个点，耗时{step2_time:.2f}秒")
        
        # 步骤3: DBSCAN聚类
        print("\n步骤3：DBSCAN聚类分组...")
        step3_start = time.time()
        points = np.asarray(line_cloud.points)
        labels = self._dbscan_clustering(points)
        step3_time = time.time() - step3_start
        unique_labels = set(labels)
        single_line_clouds = []
        
        # 为每个聚类创建不同颜色的点云
        colors = [[1, 0, 0], [0, 1, 0], [0, 0, 1], [1, 1, 0], [1, 0, 1], 
                  [0, 1, 1], [0.5, 0.5, 0], [0.5, 0, 0.5], [0, 0.5, 0.5], [1, 0.5, 0]]
        
        for i, label in enumerate(unique_labels):
            if label == -1:  # 跳过噪声点
                continue
            single_line_points = points[labels == label]
            
            # 移除点数过滤，保留所有聚类
            # if len(single_line_points) < min_line_points:
            #     continue
                
            single_line_cloud = o3d.geometry.PointCloud()
            single_line_cloud.points = o3d.utility.Vector3dVector(single_line_points)
            color = colors[i % len(colors)]
            single_line_cloud.paint_uniform_color(color)
            single_line_clouds.append(single_line_cloud)
        
        print(f"DBSCAN聚类得到 {len(single_line_clouds)} 个有效聚类，耗时{step3_time:.2f}秒")
        
        # 步骤4: 分离每个簇中的单独电力线
        print("\n步骤4：分离单独电力线...")
        step4_start = time.time()
        individual_power_lines = []
        for cluster_cloud in single_line_clouds:
            individual_lines = self._separate_individual_power_lines(cluster_cloud)
            individual_power_lines.extend(individual_lines)
        
        step4_time = time.time() - step4_start
        print(f"分离得到 {len(individual_power_lines)} 条单独电力线，耗时{step4_time:.2f}秒")
        
        # 步骤5: 根据高度极大值点进一步分割电力线
        print("\n步骤5：基于高度峰值分割...")
        step5_start = time.time()
        refined_power_lines = []
        for line_cloud in individual_power_lines:
            # 🚀 加速峰值分割参数
            segments = self._split_power_line_by_peaks(line_cloud,
                                                       prominence=0.8,     # 提高突出度，减少分割
                                                       min_segment_points=30,  # 提高最小点数，减少碎片
                                                       plot_debug=False)
            refined_power_lines.extend(segments)
        
        step5_time = time.time() - step5_start
        print(f"峰值分割得到 {len(refined_power_lines)} 个线段，耗时{step5_time:.2f}秒")
        
        # 步骤6: 智能断裂线段合并
        print("\n步骤6：智能断裂线段合并...")
        step6_start = time.time()
        # 🚀 高速模式参数 - 大幅加速智能合并
        merged_power_lines = self._iterative_merge_broken_lines(refined_power_lines, 
                                                               initial_distance=1.8, 
                                                               initial_angle=15, 
                                                               max_rounds=1, 
                                                               distance_step=0.8, 
                                                               angle_step=8)
        step6_time = time.time() - step6_start
        print(f"合并后得到 {len(merged_power_lines)} 条电力线，耗时{step6_time:.2f}秒")
        
        # 步骤7: 合并后再过滤（已跳过）
        print("\n步骤7：跳过聚类过滤...")
        # merged_power_lines = self._filter_clusters(merged_power_lines, min_line_points=min_line_points)
        print(f"跳过过滤，保持 {len(merged_power_lines)} 条电力线")
        
        # 步骤8: 温和的端点对齐
        print("\n步骤8：端点对齐...")
        # aligned_power_lines = self._align_parallel_lines(merged_power_lines, direction_angle_threshold=6)
        aligned_power_lines = merged_power_lines
        print(f"对齐后保持 {len(aligned_power_lines)} 条电力线")
        
        # 步骤9: 长度筛选
        print("\n步骤9：长度筛选...")
        filtered_power_lines, length_info = self._filter_power_lines_by_length(
            aligned_power_lines, min_length=min_line_length, length_method=length_method
        )
        print(f"长度筛选后保留 {len(filtered_power_lines)} 条电力线")

        # 步骤10: 坐标变换
        print("\n步骤10：坐标变换...")
        transformed_power_lines, transform_infos = self._transform_all_power_lines(
            filtered_power_lines, reference_point_method=reference_point_method
        )
        print(f"坐标变换完成，保持 {len(transformed_power_lines)} 条电力线")

        # 验证坐标变换的正确性
        self._verify_coordinate_transformation(filtered_power_lines, transformed_power_lines, transform_infos)

        # 跳过步骤11：不进行最终主干线拼接，直接使用坐标变换结果
        print("\n跳过步骤11：不进行最终主干线拼接...")
        final_power_lines = transformed_power_lines  # 直接使用坐标变换的结果
        print(f"跳过拼接，保持 {len(final_power_lines)} 条电力线")

        # 最终过滤：移除点数过少的杂质对象
        print("\n最终过滤：移除点数过少的杂质对象...")
        min_points_threshold = 100  # 最少100个点
        filtered_final_lines = []
        
        # print(f"过滤前对象统计:")
        for i, cloud in enumerate(final_power_lines):
            points_count = len(np.asarray(cloud.points))
            # print(f"  对象 {i}: {points_count} 个点")
            
            if points_count >= min_points_threshold:
                filtered_final_lines.append(cloud)
            # else:
            #     print(f"    -> 过滤掉 (少于{min_points_threshold}个点)")
        
        print(f"过滤结果: {len(final_power_lines)} -> {len(filtered_final_lines)} 条电力线")
        # print(f"移除了 {len(final_power_lines) - len(filtered_final_lines)} 个杂质对象")
        
        # 更新最终结果
        final_power_lines = filtered_final_lines

        # 为最终结果着色
        final_colored_power_lines = self._visualize_separate_power_lines(final_power_lines)
        
        # 显示最终结果（无论可视化开关如何都显示）
        if final_colored_power_lines:
            # print("\n显示最终提取结果")
            self._safe_visualize(final_colored_power_lines, "最终结果: 电力线提取完成")
        
        # 合并所有变换后的电力线为一个点云用于保存
        if final_power_lines:
            all_points = np.vstack([np.asarray(cloud.points) for cloud in final_power_lines])
            all_colors = np.vstack([np.asarray(cloud.colors) for cloud in final_colored_power_lines])
            line_cloud.points = o3d.utility.Vector3dVector(all_points)
            line_cloud.colors = o3d.utility.Vector3dVector(all_colors)

        # 保存最终提取的电力线点云文件
        if final_power_lines:
            import os
            # 生成输出文件名
            base_name = os.path.splitext(os.path.basename(input_file))[0]
            final_output_file = f"{base_name}_extracted_powerlines.las"
            
            print(f"\n保存最终提取结果到: {final_output_file}")
            
            # 创建带颜色的LAS文件
            header = laspy.LasHeader(point_format=2, version="1.2")  # 格式2支持RGB颜色
            # 自动设置offset为点云的最小值，避免坐标溢出
            min_xyz = np.min(all_points, axis=0)
            header.offsets = min_xyz
            header.scales = np.array([0.001, 0.001, 0.001])
            las_data = laspy.LasData(header)
            las_data.x = all_points[:, 0]
            las_data.y = all_points[:, 1]
            las_data.z = all_points[:, 2]
            
            # 添加RGB颜色信息
            las_data.red = (all_colors[:, 0] * 65535).astype(np.uint16)
            las_data.green = (all_colors[:, 1] * 65535).astype(np.uint16)
            las_data.blue = (all_colors[:, 2] * 65535).astype(np.uint16)
            
            las_data.write(final_output_file)
            print(f"最终提取结果保存成功，包含 {len(all_points)} 个点")

        if save_line_cloud:
            self._save_point_cloud(line_cloud, save_line_cloud)
        if save_out_cloud:
            self._save_point_cloud(out_line_cloud, save_out_cloud)

        # 新增：递归合并短线段，确保所有电力线长度≥20米
        print("\n递归合并短线段，确保所有电力线长度≥20米...")
        final_power_lines = self._merge_short_neighbor_lines(final_power_lines, min_length=200.0, visualize=visualize_steps)
        print(f"递归合并后剩余 {len(final_power_lines)} 条电力线")
        if visualize_steps and final_power_lines:
            colored_final_lines = self._visualize_separate_power_lines(final_power_lines)
            # print("显示递归合并后的电力线")
            self._safe_visualize(colored_final_lines, "递归合并后的电力线")

        # 计算总体运行时间
        program_end_time = time.time()
        total_runtime = program_end_time - program_start_time
        
        print(f"\n电力线提取完成！最终得到 {len(final_power_lines)} 条有效电力线")
        print(f"总耗时: {total_runtime:.2f}秒")

        # 返回最终拼接后的电力线前，输出首尾端点到json
        output_json = []
        for idx, cloud in enumerate(final_power_lines):
            points = np.asarray(cloud.points)
            if len(points) == 0:
                continue
            # 按主方向排序后的首尾点
            main_direction = self._get_main_direction(points)
            center = np.mean(points, axis=0)
            projections = np.dot(points - center, main_direction)
            sorted_indices = np.argsort(projections)
            sorted_points = points[sorted_indices]
            start_point = sorted_points[0].tolist()
            end_point = sorted_points[-1].tolist()
            output_json.append({
                "index": idx,
                "start": start_point,
                "end": end_point,
                "count": len(points)
            })
        base_name = os.path.splitext(os.path.basename(input_file))[0]
        json_file = f"{base_name}_powerline_endpoints.json"
        with open(json_file, "w", encoding="utf-8") as f:
            json.dump(output_json, f, ensure_ascii=False, indent=2)
        # print(f"首尾端点已输出到: {json_file}")
        
        # 清理缓存以释放内存
        self._clear_caches()
        
        # 返回最终拼接后的电力线
        return final_power_lines

    def compare_dynamic_vs_fixed_params(self, input_file, save_comparison=True):
        """
        对比动态参数和固定参数的效果
        
        :param input_file: 输入文件路径
        :param save_comparison: 是否保存对比结果
        :return: 对比结果字典
        """
        print("=" * 60)
        print("开始动态参数 vs 固定参数对比测试")
        print("=" * 60)
        
        # 读取点云
        point_cloud = self._read_point_cloud(input_file)
        print(f"原始点云: {len(point_cloud.points)} 个点")
        
        # 测试固定参数
        print("\n--- 测试固定参数模式 ---")
        fixed_start = time.time()
        fixed_line_cloud, fixed_out_cloud = self._power_line_segmentation(
            point_cloud, use_dynamic_params=False
        )
        fixed_time = time.time() - fixed_start
        
        print(f"固定参数结果:")
        print(f"  线性特征点: {len(fixed_line_cloud.points)} 个")
        print(f"  非线性特征点: {len(fixed_out_cloud.points)} 个")
        print(f"  耗时: {fixed_time:.2f}秒")
        
        # 测试动态参数
        print("\n--- 测试动态参数模式 ---")
        dynamic_start = time.time()
        dynamic_line_cloud, dynamic_out_cloud = self._power_line_segmentation(
            point_cloud, use_dynamic_params=True
        )
        dynamic_time = time.time() - dynamic_start
        
        print(f"动态参数结果:")
        print(f"  线性特征点: {len(dynamic_line_cloud.points)} 个")
        print(f"  非线性特征点: {len(dynamic_out_cloud.points)} 个")
        print(f"  耗时: {dynamic_time:.2f}秒")
        
        # 计算改进效果
        linear_improvement = len(dynamic_line_cloud.points) - len(fixed_line_cloud.points)
        linear_improvement_ratio = linear_improvement / len(fixed_line_cloud.points) * 100 if len(fixed_line_cloud.points) > 0 else 0
        
        print(f"\n--- 对比结果 ---")
        print(f"线性特征点改进: {linear_improvement:+d} 个 ({linear_improvement_ratio:+.1f}%)")
        print(f"时间变化: {dynamic_time - fixed_time:+.2f}秒")
        
        # 可视化对比
        if self.enable_visualization:
            print("\n显示对比结果...")
            
            # 固定参数结果
            fixed_line_cloud.paint_uniform_color([1, 0, 0])  # 红色
            fixed_out_cloud.paint_uniform_color([0, 0, 1])   # 蓝色
            self._safe_visualize([fixed_line_cloud, fixed_out_cloud], "固定参数结果")
            
            # 动态参数结果
            dynamic_line_cloud.paint_uniform_color([1, 0, 0])  # 红色
            dynamic_out_cloud.paint_uniform_color([0, 0, 1])   # 蓝色
            self._safe_visualize([dynamic_line_cloud, dynamic_out_cloud], "动态参数结果")
        
        # 保存对比结果
        if save_comparison:
            import os
            base_name = os.path.splitext(os.path.basename(input_file))[0]
            
            # 保存固定参数结果
            fixed_output = f"{base_name}_fixed_params.las"
            self._save_point_cloud(fixed_line_cloud, fixed_output)
            print(f"固定参数结果保存到: {fixed_output}")
            
            # 保存动态参数结果
            dynamic_output = f"{base_name}_dynamic_params.las"
            self._save_point_cloud(dynamic_line_cloud, dynamic_output)
            print(f"动态参数结果保存到: {dynamic_output}")
        
        # 返回对比结果
        comparison_result = {
            'fixed_params': {
                'linear_points': len(fixed_line_cloud.points),
                'nonlinear_points': len(fixed_out_cloud.points),
                'time': fixed_time
            },
            'dynamic_params': {
                'linear_points': len(dynamic_line_cloud.points),
                'nonlinear_points': len(dynamic_out_cloud.points),
                'time': dynamic_time
            },
            'improvement': {
                'linear_points_change': linear_improvement,
                'linear_points_change_ratio': linear_improvement_ratio,
                'time_change': dynamic_time - fixed_time
            }
        }
        
        return comparison_result

    def _merge_short_neighbor_lines(self, power_line_clouds, min_length=20.0, visualize=True):
        """
        合并所有物理距离上相邻且长度低于min_length的电力线，直到所有线段长度都不低于min_length
        """
        import copy
        import numpy as np
        import open3d as o3d
        clouds = copy.deepcopy(power_line_clouds)
        merged_flags = [False] * len(clouds)
        result_lines = []
        while True:
            changed = False
            for i, cloud in enumerate(clouds):
                if merged_flags[i]:
                    continue
                points_i = np.asarray(cloud.points)
                if len(points_i) == 0:
                    merged_flags[i] = True
                    continue
                length_i = self._calculate_power_line_length(cloud)
                if length_i >= min_length:
                    result_lines.append(cloud)
                    merged_flags[i] = True
                    continue
                # 找最近的线段
                min_dist = float('inf')
                min_j = -1
                for j, cloud_j in enumerate(clouds):
                    if i == j or merged_flags[j]:
                        continue
                    points_j = np.asarray(cloud_j.points)
                    if len(points_j) == 0:
                        continue
                    length_j = self._calculate_power_line_length(cloud_j)
                    if length_j >= min_length:
                        continue
                    # 计算端点距离
                    d = min(
                        np.linalg.norm(points_i[0] - points_j[0]),
                        np.linalg.norm(points_i[0] - points_j[-1]),
                        np.linalg.norm(points_i[-1] - points_j[0]),
                        np.linalg.norm(points_i[-1] - points_j[-1])
                    )
                    if d < min_dist:
                        min_dist = d
                        min_j = j
                if min_j != -1 and min_dist < min_length:
                    # 合并i和min_j
                    merged_points = np.vstack([points_i, np.asarray(clouds[min_j].points)])
                    merged_cloud = o3d.geometry.PointCloud()
                    merged_cloud.points = o3d.utility.Vector3dVector(merged_points)
                    clouds[i] = merged_cloud
                    merged_flags[min_j] = True
                    changed = True
            if not changed:
                break
        # 收集剩余未合并的线段
        for i, flag in enumerate(merged_flags):
            if not flag:
                result_lines.append(clouds[i])
        # 可视化
        if visualize and result_lines:
            colored_lines = self._visualize_separate_power_lines(result_lines)
            self._safe_visualize(colored_lines, "最终短线段递归合并后电力线")
        return result_lines


if __name__ == '__main__':
    import sys
    import argparse
    
    # 解析命令行参数
    parser = argparse.ArgumentParser(description='电力线提取脚本')
    parser.add_argument('input_file', help='输入的LAS文件路径')
    parser.add_argument('--threshold', type=float, default=0.8, help='线特征阈值 (默认: 0.8)')
    parser.add_argument('--radius', type=float, default=2.0, help='邻域搜索半径 (默认: 2.0)')
    parser.add_argument('--height_min', type=float, default=0, help='最小高程 (默认: 0)')
    parser.add_argument('--height_max', type=float, default=60, help='最大高程 (默认: 60)')
    parser.add_argument('--eps', type=float, default=1.8, help='DBSCAN邻域半径 (默认: 1.8)')
    parser.add_argument('--min_samples', type=int, default=7, help='DBSCAN最小样本数 (默认: 7)')
    parser.add_argument('--min_line_points', type=int, default=50, help='最小点数阈值 (默认: 50)')
    parser.add_argument('--min_line_length', type=float, default=30.0, help='最小长度阈值 (默认: 30.0)')
    parser.add_argument('--length_method', choices=['projection', 'path'], default='projection', help='长度计算方法 (默认: projection)')
    parser.add_argument('--reference_point_method', choices=['center', 'min_z', 'max_z', 'start', 'end'], default='center', help='坐标变换参考点 (默认: center)')
    parser.add_argument('--enable_visualization', action='store_true', help='启用可视化')
    parser.add_argument('--use_dynamic_params', action='store_true', default=True, help='启用动态参数 (默认: True)')
    
    args = parser.parse_args()
    
    # 检查输入文件是否存在
    if not os.path.exists(args.input_file):
        print(f"错误：输入文件不存在: {args.input_file}")
        sys.exit(1)
    
    print(f"开始处理文件: {args.input_file}")
    print(f"参数配置:")
    print(f"  - 线特征阈值: {args.threshold}")
    print(f"  - 邻域搜索半径: {args.radius}")
    print(f"  - 高程范围: [{args.height_min}, {args.height_max}]")
    print(f"  - DBSCAN参数: eps={args.eps}, min_samples={args.min_samples}")
    print(f"  - 最小点数: {args.min_line_points}")
    print(f"  - 最小长度: {args.min_line_length}")
    print(f"  - 长度计算方法: {args.length_method}")
    print(f"  - 参考点方法: {args.reference_point_method}")
    print(f"  - 可视化: {'启用' if args.enable_visualization else '禁用'}")
    print(f"  - 动态参数: {'启用' if args.use_dynamic_params else '禁用'}")
    
    # 创建电力线提取器
    extractor = PowerLineExtractor(
        threshold=args.threshold,
        radius=args.radius,
        height_min=args.height_min,
        height_max=args.height_max,
        eps=args.eps,
        min_samples=args.min_samples,
        enable_visualization=args.enable_visualization
    )

    # 执行电力线提取
    try:
        individual_power_lines = extractor.extract(
            args.input_file,
            min_line_points=args.min_line_points,
            min_line_length=args.min_line_length,
            length_method=args.length_method,
            reference_point_method=args.reference_point_method,
            visualize_steps=args.enable_visualization,
            use_dynamic_params=args.use_dynamic_params
        )
        
        print(f"\n电力线提取完成！")
        print(f"成功提取 {len(individual_power_lines)} 条电力线")
        
        # 生成输出文件名
        base_name = os.path.splitext(os.path.basename(args.input_file))[0]
        json_file = f"{base_name}_powerline_endpoints.json"
        las_file = f"{base_name}_extracted_powerlines.las"
        
        print(f"输出文件:")
        print(f"  - 端点JSON: {json_file}")
        print(f"  - 提取点云: {las_file}")
        
    except Exception as e:
        print(f"错误：电力线提取失败: {str(e)}")
        sys.exit(1)
