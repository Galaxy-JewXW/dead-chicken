import open3d as o3d
import numpy as np
import laspy
import os
import sys
from tqdm import tqdm
import cv2
from sklearn.cluster import DBSCAN, MeanShift, estimate_bandwidth
from sklearn.decomposition import PCA
from sklearn.linear_model import RANSACRegressor
import matplotlib.pyplot as plt
from scipy.spatial import KDTree

# 设置输出编码为UTF-8 - 使用更安全的方式
import os

# 设置环境变量确保UTF-8编码
os.environ['PYTHONIOENCODING'] = 'utf-8'

class PowerLineExtractor:
    """
    电力线提取器类
    用于从点云数据中提取电力线
    """

    def __init__(self, threshold=0.81, radius=1.5, height_min=0, height_max=20, eps=1.5, min_samples=5):
        """
        初始化电力线提取器

        :param threshold: 线特征阈值，默认0.81
        :param radius: 近邻点搜索半径，默认1.5
        :param height_min: 高程最小值，默认0
        :param height_max: 高程最大值，默认20
        :param eps: DBSCAN的邻域半径，默认1.5
        :param min_samples: DBSCAN的最小样本数，默认10
        """
        self.threshold = threshold
        self.radius = radius
        self.height_min = height_min
        self.height_max = height_max
        self.eps = eps
        self.min_samples = min_samples

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
        
        # 修复逻辑：正确分离低高程点和高程点
        low_ind = np.where(points[:, 2] < limit_min)[0]  # 低高程点
        high_ind = np.where(points[:, 2] > limit_max)[0]  # 高高程点
        valid_ind = np.where((points[:, 2] >= limit_min) & (points[:, 2] <= limit_max))[0]  # 有效高程范围内的点
        
        low_cloud = cloud.select_by_index(low_ind)
        high_cloud = cloud.select_by_index(high_ind)
        valid_cloud = cloud.select_by_index(valid_ind)
        
        # 返回有效高程范围内的点作为"低点"，高高程点作为"高点"
        # 这样电力线提取会处理高高程点，而有效范围内的点会被保留用于后续处理
        return valid_cloud, high_cloud

    def _pca_compute(self, data, sort=True):
        """
        SVD分解计算点云的特征值

        :param data: 输入数据
        :param sort: 是否将特征值进行排序
        :return: 特征值
        """
        average_data = np.mean(data, axis=0)
        decentration_matrix = data - average_data
        H = np.dot(decentration_matrix.T, decentration_matrix)
        _, eigenvalues, _ = np.linalg.svd(H)
        if sort:
            sort_idx = eigenvalues.argsort()[::-1]
            eigenvalues = eigenvalues[sort_idx]
        return eigenvalues

    def _power_line_segmentation(self, power_line_cloud, threshold=None):
        """
        计算每一个点的线性特征，并根据线性特征提取线点云

        :param power_line_cloud: 输入点云
        :param threshold: 线特征阈值，如果为None则使用实例变量
        :return: 线点云和线之外的点云
        """
        if threshold is None:
            threshold = self.threshold
        low, high = self._pass_through(power_line_cloud, self.height_min, self.height_max)
        
        # 简化输出：只显示关键信息
        original_points = np.asarray(power_line_cloud.points)
        valid_count = np.sum((original_points[:, 2] >= self.height_min) & (original_points[:, 2] <= self.height_max))
        high_count = np.sum(original_points[:, 2] > self.height_max)
        
        print(f"Height filter: {valid_count} valid points, {high_count} high points")
        
        points = np.asarray(high.points)
        kdtree = o3d.geometry.KDTreeFlann(high)
        num_points = len(high.points)
        linear = []

        for i in tqdm(range(num_points), desc="Computing linear features", ncols=80):
            k, idx, _ = kdtree.search_radius_vector_3d(high.points[i], self.radius)
            neighbors = points[idx, :]
            w = self._pca_compute(neighbors)
            l1, l2 = w[0], w[1]
            L = np.divide((l1 - l2), l1, out=np.zeros_like((l1 - l2)), where=l1 != 0)
            linear.append(L)

        linear = np.array(linear)
        idx = np.where(linear > threshold)[0]
        line_cloud_ = high.select_by_index(idx)
        out_line_cloud_ = high.select_by_index(idx, invert=True) + low
        
        # 检查是否成功提取到电力线点云
        line_points = np.asarray(line_cloud_.points)
        if line_points.shape[0] == 0:
            print(f"Warning: Linear feature threshold too high, trying lower threshold...")
            # 尝试降低阈值
            lower_threshold = threshold * 0.8
            idx = np.where(linear > lower_threshold)[0]
            line_cloud_ = high.select_by_index(idx)
            out_line_cloud_ = high.select_by_index(idx, invert=True) + low
            
            line_points = np.asarray(line_cloud_.points)
            if line_points.shape[0] == 0:
                print(f"Warning: No power line points extracted even with lower threshold")
                # 返回一个空的点云，避免后续DBSCAN出错
                empty_cloud = o3d.geometry.PointCloud()
                return empty_cloud, out_line_cloud_
            else:
                print(f"Successfully extracted {line_points.shape[0]} power line points with lower threshold")
        
        return line_cloud_, out_line_cloud_

    def _dbscan_clustering(self, points, eps=None, min_samples=None):
        """
        使用DBSCAN算法对点云进行聚类

        :param points: 输入点云
        :param eps: 邻域半径，如果为None则使用实例变量
        :param min_samples: 最小样本数，如果为None则使用实例变量
        :return: 聚类后的标签
        """
        if eps is None:
            eps = self.eps
        if min_samples is None:
            min_samples = self.min_samples
        clustering = DBSCAN(eps=eps, min_samples=min_samples).fit(points)
        return clustering.labels_

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
                distance = abs(a * x + b * y - rho) / np.sqrt(a**2 + b**2)
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
        使用PCA获取点云的主方向
        
        :param points: 点云数据
        :return: 主方向向量
        """
        pca = PCA(n_components=3)
        pca.fit(points)
        return pca.components_[0]  # 第一主成分方向
    
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
            distance = np.linalg.norm(sorted_points[i] - sorted_points[i-1])
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
            
            if length >= min_length:
                filtered_clouds.append(cloud)
            else:
                print(f"Removed power line {i}: length={length:.2f}m (below threshold {min_length}m)")
        
        return filtered_clouds, length_info
    
    def _print_length_statistics(self, length_info):
        """
        打印长度统计信息
        
        :param length_info: 长度信息列表
        """
        if not length_info:
            return
        
        kept_lengths = [info['length'] for info in length_info if info['kept']]
        removed_count = len(length_info) - len(kept_lengths)
        
        print(f"长度筛选: 保留 {len(kept_lengths)} 条, 移除 {removed_count} 条")
        
    def _validate_segments_by_catenary_fit(self, segments, projections, heights, split_points, plot_debug=False, max_rmse=0.5):
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
            end_idx = split_points[i+1] + 1 if i < len(split_points) - 2 else len(heights)
            
            segment_proj = projections[start_idx:end_idx]
            segment_heights = heights[start_idx:end_idx]
            
            try:
                # 初始参数估计
                v_guess = np.min(segment_heights)  # 最低点的高度
                a_guess = (np.max(segment_heights) - v_guess) / 2  # 尺度参数
                h_guess = segment_proj[np.argmin(segment_heights)]  # 最低点位置
                
                # 拟合悬链线
                popt, pcov = curve_fit(catenary, segment_proj, segment_heights, 
                                        p0=[a_guess, h_guess, v_guess], maxfev=10000)
                
                # 计算拟合误差
                z_fit = catenary(segment_proj, *popt)
                rmse = np.sqrt(np.mean((segment_heights - z_fit)**2))
                
                if plot_debug:
                    import matplotlib.pyplot as plt
                    plt.figure(figsize=(10, 6))
                    plt.plot(segment_proj, segment_heights, 'b.', alpha=0.5, label='点云数据')
                    x_fit = np.linspace(min(segment_proj), max(segment_proj), 100)
                    z_fit = catenary(x_fit, *popt)
                    plt.plot(x_fit, z_fit, 'r-', label=f'悬链线拟合 (RMSE={rmse:.3f})')
                    plt.xlabel('电力线方向的距离')
                    plt.ylabel('高度')
                    plt.title(f'分段 {i+1} 悬链线拟合')
                    plt.grid(True)
                    plt.legend()
                    plt.show()
                
                # 只有拟合误差小于阈值的段才认为是有效电力线段
                if rmse < max_rmse:
                    valid_segments.append(segment)
                else:
                    print(f"分段 {i+1} 拟合误差过大 (RMSE={rmse:.3f}), 可能不是有效电力线段")
            except Exception as e:
                print(f"分段 {i+1} 拟合失败: {str(e)}")

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
            end_idx = split_points[i+1] + 1 if i < len(split_points) - 2 else len(z_values)
            
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
            [1, 0.5, 0]   # 橙
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
        y = power_line_points[:, 2]   # z坐标
        
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
        
        # 简化输出：不显示详细的坐标信息
        
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
            
            # 简化输出：不显示每条电力线的详细信息
        
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
        
        return True
    
    def extract(self, input_file, save_line_cloud=None, save_out_cloud=None, 
                min_line_points=50, min_line_length=10.0, length_method='projection',
                reference_point_method='center'):
        """
        完整的电力线提取和可视化流程

        :param input_file: 输入文件路径
        :param save_line_cloud: 保存线点云的文件路径（可选）
        :param save_out_cloud: 保存非线点云的文件路径（可选）
        :param min_line_points: 聚类筛选的最小点数阈值，默认50
        :param min_line_length: 最小电力线长度阈值，默认10.0米
        :param length_method: 计算长度的方法，'projection'或'path'
        :param reference_point_method: 坐标变换的全局参考点选择方法
            - 'center': 使用所有电力线的中心点（默认）
            - 'min_z': 使用所有电力线中的最低点
            - 'max_z': 使用所有电力线中的最高点
            - 'start': 使用第一条电力线的起点作为全局参考点
            - 'end': 使用第一条电力线的终点作为全局参考点
        :return: 变换后的电力线点云列表
        """
        point_cloud = self._read_point_cloud(input_file)
        
        # 简化输出：只显示关键信息
        points = np.asarray(point_cloud.points)
        if points.shape[0] > 0:
            print(f"Reading point cloud: {points.shape[0]} points")
            
            # 根据实际点云数据调整高程范围
            z_min_percentile = np.percentile(points[:, 2], 25)
            z_max_percentile = np.percentile(points[:, 2], 75)
            
            # 设置更合理的高程范围
            self.height_min = z_min_percentile
            self.height_max = z_max_percentile
            
            print(f"Height range: [{self.height_min:.1f}, {self.height_max:.1f}]")
        else:
            print("Warning: Original point cloud is empty")
        
        line_cloud, out_line_cloud = self._power_line_segmentation(point_cloud)
        points = np.asarray(line_cloud.points)
        
        # 检查是否成功提取到电力线点云
        if points.shape[0] == 0:
            print("Error: No power line points extracted")
            return []
        
        print(f"Extracted {points.shape[0]} power line points")
        
        # DBSCAN聚类
        labels = self._dbscan_clustering(points)
        unique_labels = set(labels)
        single_line_clouds = []
        for label in unique_labels:
            if label == -1:
                continue
            single_line_points = points[labels == label]
            single_line_cloud = o3d.geometry.PointCloud()
            single_line_cloud.points = o3d.utility.Vector3dVector(single_line_points)
            single_line_clouds.append(single_line_cloud)
        
        print(f"DBSCAN clustering: {len(single_line_clouds)} clusters")
        
        # 聚类后筛选
        filtered_line_clouds = self._filter_clusters(single_line_clouds, min_line_points)
        print(f"After filtering: {len(filtered_line_clouds)} clusters")
        
        # 分离单独电力线
        individual_power_lines = []
        for cluster_cloud in filtered_line_clouds:
            individual_lines = self._separate_individual_power_lines(cluster_cloud)
            individual_power_lines.extend(individual_lines)
        
        print(f"Separated {len(individual_power_lines)} individual power lines")
        
        # 根据高度极大值点进一步分割
        refined_power_lines = []
        for line_cloud in individual_power_lines:
            segments = self._split_power_line_by_peaks(line_cloud, 
                                                    prominence=0.5, 
                                                    min_segment_points=20,
                                                    plot_debug=False)
            refined_power_lines.extend(segments)
        
        refined_power_lines = self._filter_clusters(refined_power_lines, min_line_points=min_line_points)
        filtered_power_lines, length_info = self._filter_power_lines_by_length(
            refined_power_lines, min_length=min_line_length, length_method=length_method
        )
        
        print(f"After length filtering: {len(filtered_power_lines)} power lines")
        
        # 坐标变换
        transformed_power_lines, transform_infos = self._transform_all_power_lines(
            filtered_power_lines, reference_point_method=reference_point_method
        )
        
        print(f"Coordinate transformation completed")
        
        # 合并所有变换后的电力线为一个点云用于保存
        if transformed_power_lines:
            all_points = np.vstack([np.asarray(cloud.points) for cloud in transformed_power_lines])
            line_cloud.points = o3d.utility.Vector3dVector(all_points)
        
        if save_line_cloud:
            self._save_point_cloud(line_cloud, save_line_cloud)
        if save_out_cloud:
            self._save_point_cloud(out_line_cloud, save_out_cloud)
        
        # 返回变换后的电力线（原始电力线信息可以通过transform_infos恢复）
        return transformed_power_lines

if __name__ == '__main__':
    extractor = PowerLineExtractor()
    input_file = "../data/A/A.las"
    
    # 提取电力线，设置最小长度阈值为15米，使用中心点作为坐标变换参考点
    individual_power_lines = extractor.extract(
        input_file, 
        min_line_points=50, 
        min_line_length=200.0,
        length_method='path',  # 可选 'projection' 或 'path'
        reference_point_method='center'  # 坐标变换参考点选择方法
    )
    
    # 可视化分离后的单独电力线，每条线不同颜色
    colored_power_lines = extractor._visualize_separate_power_lines(individual_power_lines)
    
    print(f"\nFinal result: {len(individual_power_lines)} valid power lines after length filtering and coordinate transformation")
    
    # 可视化所有筛选后的电力线
    if colored_power_lines:
        eval("o3d.visualization.draw_geometries(colored_power_lines)")
    else:
        print("No power lines to visualize after filtering.")