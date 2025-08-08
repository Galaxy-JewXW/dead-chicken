import json
import numpy as np
import sys
from Extractor import PowerLineExtractor
from tqdm import tqdm
from scipy.optimize import curve_fit
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D

# 设置输出编码为UTF-8 - 使用更安全的方式
import os

# 设置环境变量确保UTF-8编码
os.environ['PYTHONIOENCODING'] = 'utf-8'

class Generator:
    def __init__(self, file_path, json_path, *args, **kwargs):
        self.file_path = file_path
        self.json_path = json_path
        self.individual_power_lines = None
        self.extractor = PowerLineExtractor(*args, **kwargs)

    def generate(self, *args, **kwargs):
        self.individual_power_lines = self.extractor.extract(self.file_path, *args, **kwargs)

    def _fit_catenary(self, points, projections):
        """
        对电力线进行悬链线拟合
        
        :param points: 电力线点云数据
        :param projections: 投影到主方向的距离
        :return: 拟合参数和误差信息
        """
        # 悬链线方程: z = a * cosh((x - h) / a) + v
        def catenary(x, a, h, v):
            return a * np.cosh((x - h) / a) + v
        
        heights = points[:, 2]  # Z坐标作为高度
        
        try:
            # 初始参数估计
            v_guess = np.min(heights)  # 最低点的高度
            a_guess = (np.max(heights) - v_guess) / 2  # 尺度参数
            h_guess = projections[np.argmin(heights)]  # 最低点位置
            
            # 拟合悬链线
            popt, pcov = curve_fit(catenary, projections, heights, 
                                    p0=[a_guess, h_guess, v_guess], maxfev=10000)
            
            # 计算拟合误差
            z_fit = catenary(projections, *popt)
            rmse = np.sqrt(np.mean((heights - z_fit)**2))
            r_squared = 1 - np.sum((heights - z_fit)**2) / np.sum((heights - np.mean(heights))**2)
            
            return {
                "method": "catenary",
                "equation": "z = a * cosh((x - h) / a) + v",
                "parameters": {
                    "a": float(popt[0]),  # 尺度参数
                    "h": float(popt[1]),  # 最低点位置
                    "v": float(popt[2])   # 最低点高度
                },
                "fit_quality": {
                    "rmse": float(rmse),
                    "r_squared": float(r_squared)
                },
                "success": True
            }
        except Exception as e:
            return {
                "method": "catenary",
                "equation": "z = a * cosh((x - h) / a) + v",
                "parameters": None,
                "fit_quality": None,
                "success": False,
                "error": str(e)
            }

    def _fit_parabola(self, points, projections):
        """
        对电力线进行抛物线拟合（悬链线的近似）
        
        :param points: 电力线点云数据
        :param projections: 投影到主方向的距离
        :return: 拟合参数和误差信息
        """
        # 抛物线方程: z = ax² + bx + c
        def parabola(x, a, b, c):
            return a * x**2 + b * x + c
        
        heights = points[:, 2]  # Z坐标作为高度
        
        try:
            # 初始参数估计
            c_guess = np.min(heights)  # 最低点的高度
            a_guess = (np.max(heights) - c_guess) / (np.max(projections) - np.min(projections))**2  # 二次项系数
            b_guess = 0  # 一次项系数
            
            # 拟合抛物线
            popt, pcov = curve_fit(parabola, projections, heights, 
                                    p0=[a_guess, b_guess, c_guess], maxfev=10000)
            
            # 计算拟合误差
            z_fit = parabola(projections, *popt)
            rmse = np.sqrt(np.mean((heights - z_fit)**2))
            r_squared = 1 - np.sum((heights - z_fit)**2) / np.sum((heights - np.mean(heights))**2)
            
            # 计算顶点位置
            vertex_x = -popt[1] / (2 * popt[0]) if popt[0] != 0 else 0
            vertex_z = parabola(vertex_x, *popt)
            
            return {
                "method": "parabola",
                "equation": "z = ax² + bx + c",
                "parameters": {
                    "a": float(popt[0]),  # 二次项系数
                    "b": float(popt[1]),  # 一次项系数
                    "c": float(popt[2])   # 常数项
                },
                "vertex": {
                    "x": float(vertex_x),
                    "z": float(vertex_z)
                },
                "fit_quality": {
                    "rmse": float(rmse),
                    "r_squared": float(r_squared)
                },
                "success": True
            }
        except Exception as e:
            return {
                "method": "parabola",
                "equation": "z = ax² + bx + c",
                "parameters": None,
                "fit_quality": None,
                "success": False,
                "error": str(e)
            }

    def _fit_polynomial(self, points, projections, degree=3):
        """
        对电力线进行多项式拟合
        
        :param points: 电力线点云数据
        :param projections: 投影到主方向的距离
        :param degree: 多项式次数，默认3次
        :return: 拟合参数和误差信息
        """
        heights = points[:, 2]  # Z坐标作为高度
        
        try:
            # 多项式拟合
            coeffs = np.polyfit(projections.astype(float), heights.astype(float), degree)
            
            # 计算拟合值
            z_fit = np.polyval(coeffs, projections.astype(float))
            
            # 计算拟合误差
            rmse = np.sqrt(np.mean((heights - z_fit)**2))
            r_squared = 1 - np.sum((heights - z_fit)**2) / np.sum((heights - np.mean(heights))**2)
            
            # 找到极值点
            if degree >= 2:
                # 求导数的根
                derivative_coeffs = np.polyder(coeffs)
                critical_points = np.roots(derivative_coeffs)
                # 只保留实数根
                critical_points = critical_points[np.isreal(critical_points)].real
                # 在投影范围内找极值点
                valid_points = critical_points[(critical_points >= np.min(projections)) & 
                                             (critical_points <= np.max(projections))]
                if len(valid_points) > 0:
                    extrema_z = np.polyval(coeffs, valid_points)
                    min_idx = np.argmin(extrema_z)
                    min_point = {"x": float(valid_points[min_idx]), "z": float(extrema_z[min_idx])}
                else:
                    min_point = None
            else:
                min_point = None
            
            return {
                "method": f"polynomial_{degree}",
                "equation": f"z = {coeffs[0]:.6f}x^{degree}" + "".join([f" + {coeffs[i]:.6f}x^{degree-i}" for i in range(1, len(coeffs))]),
                "parameters": {
                    "coefficients": coeffs.tolist(),
                    "degree": degree
                },
                "extrema": min_point,
                "fit_quality": {
                    "rmse": float(rmse),
                    "r_squared": float(r_squared)
                },
                "success": True
            }
        except Exception as e:
            return {
                "method": f"polynomial_{degree}",
                "equation": f"z = ax^{degree} + ...",
                "parameters": None,
                "fit_quality": None,
                "success": False,
                "error": str(e)
            }

    def _fit_linear(self, points, projections):
        """
        对电力线进行线性拟合（直线）
        
        :param points: 电力线点云数据
        :param projections: 投影到主方向的距离
        :return: 拟合参数和误差信息
        """
        heights = points[:, 2]  # Z坐标作为高度
        
        try:
            # 线性拟合
            coeffs = np.polyfit(projections, heights, 1)
            slope, intercept = coeffs
            
            # 计算拟合值
            z_fit = np.polyval(coeffs, projections.astype(float))
            
            # 计算拟合误差
            rmse = np.sqrt(np.mean((heights - z_fit)**2))
            r_squared = 1 - np.sum((heights - z_fit)**2) / np.sum((heights - np.mean(heights))**2)
            
            return {
                "method": "linear",
                "equation": f"z = {slope:.6f}x + {intercept:.6f}",
                "parameters": {
                    "slope": float(slope),
                    "intercept": float(intercept)
                },
                "fit_quality": {
                    "rmse": float(rmse),
                    "r_squared": float(r_squared)
                },
                "success": True
            }
        except Exception as e:
            return {
                "method": "linear",
                "equation": "z = mx + b",
                "parameters": None,
                "fit_quality": None,
                "success": False,
                "error": str(e)
            }

    def dump(self, fit_method="all"):
        """
        将每条电力线的两个端点坐标和拟合参数保存到JSON文件中
        
        :param fit_method: 拟合方法选择
            - "all": 使用所有拟合方法（默认）
            - "catenary": 只使用悬链线拟合
            - "parabola": 只使用抛物线拟合
            - "polynomial": 只使用多项式拟合
            - "linear": 只使用线性拟合
        :param show_sorting_info: 是否显示端点排序信息，默认False
        """
        if self.individual_power_lines is None:
            print("请先调用 generate() 方法提取电力线")
            return
        
        # 验证拟合方法参数
        valid_methods = ["all", "catenary", "parabola", "polynomial", "linear"]
        if fit_method not in valid_methods:
            print(f"错误：无效的拟合方法 '{fit_method}'。有效方法：{valid_methods}")
            return
        
        power_lines_data = []
        endpoints_with_lineid = []  # 新增：收集所有端点及其电力线id
        
        for i, line_cloud in tqdm(enumerate(self.individual_power_lines), desc="Fitting power lines", ncols=80):
            # 获取电力线的所有点坐标
            points = np.asarray(line_cloud.points)
            
            if len(points) < 2:
                print(f"Warning: Power line {i} has insufficient points, skipping")
                continue
            
            # 计算电力线的主方向
            # 使用PCA找到主方向
            centroid = np.mean(points, axis=0)
            centered_points = points - centroid
            cov_matrix = np.cov(centered_points.T)
            eigenvalues, eigenvectors = np.linalg.eigh(cov_matrix)
            
            # 主方向是最大特征值对应的特征向量
            main_direction = eigenvectors[:, np.argmax(eigenvalues)]
            
            # 将点投影到主方向上
            projections = np.dot(centered_points, main_direction)
            
            # 找到投影值最小和最大的点，即两个端点
            min_idx = np.argmin(projections)
            max_idx = np.argmax(projections)
            
            # 获取两个端点的坐标
            endpoint1 = points[min_idx].tolist()  # [x, y, z]
            endpoint2 = points[max_idx].tolist()  # [x, y, z]
            
            # 使用Python的元组比较进行排序
            if (endpoint1[0], endpoint1[1], endpoint1[2]) > (endpoint2[0], endpoint2[1], endpoint2[2]):
                endpoint1, endpoint2 = endpoint2, endpoint1
            
            # 新增：收集端点及其电力线id
            endpoints_with_lineid.append({"coord": endpoint1, "line_id": i})
            endpoints_with_lineid.append({"coord": endpoint2, "line_id": i})
            
            # 根据选择的拟合方法进行拟合
            fits = {}
            
            if fit_method in ["all", "catenary"]:
                fits["catenary"] = self._fit_catenary(points, projections)
            
            if fit_method in ["all", "parabola"]:
                fits["parabola"] = self._fit_parabola(points, projections)
            
            if fit_method in ["all", "polynomial"]:
                fits["polynomial_3"] = self._fit_polynomial(points, projections, degree=3)
            
            if fit_method in ["all", "linear"]:
                fits["linear"] = self._fit_linear(points, projections)
            
            # 保存电力线数据
            power_line_data = {
                "id": i,
                "endpoint1": {
                    "x": endpoint1[0],
                    "y": endpoint1[1], 
                    "z": endpoint1[2]
                },
                "endpoint2": {
                    "x": endpoint2[0],
                    "y": endpoint2[1],
                    "z": endpoint2[2]
                },
                "point_count": len(points),
                "fits": fits
            }
            
            power_lines_data.append(power_line_data)
        
        # ====== 新增：电力塔聚类 ======
        from sklearn.cluster import DBSCAN
        if len(endpoints_with_lineid) > 0:
            endpoint_coords = np.array([e["coord"] for e in endpoints_with_lineid])
            # DBSCAN参数可根据实际点云密度调整
            db = DBSCAN(eps=30, min_samples=2).fit(endpoint_coords)
            labels = db.labels_  # 聚类标签
            unique_labels = set(labels)
            noise_label = labels.dtype.type(-1)
            if noise_label in unique_labels:
                unique_labels.remove(noise_label)  # -1为噪声点，不算塔
            towers = []
            for tower_id in unique_labels:
                indices = np.where(labels == tower_id)[0]
                if len(indices) == 0:
                    continue  # 跳过空聚类
                tower_points = endpoint_coords[indices]
                center = tower_points.mean(axis=0)
                line_ids = list(set([endpoints_with_lineid[idx]["line_id"] for idx in indices]))
                towers.append({
                    "id": int(tower_id),
                    "position": {"x": float(center[0]), "y": float(center[1]), "z": float(center[2])},
                    "connected_power_line_ids": line_ids,
                    "endpoint_count": len(indices)
                })
            n_towers = len(towers)
        else:
            towers = []
            n_towers = 0
        # ====== 电力塔聚类结束 ======
        
        # 保存到JSON文件
        output_data = {
            "file_path": self.file_path,
            "total_power_lines": len(power_lines_data),
            "fit_method": fit_method,
            "power_lines": power_lines_data,
            # 新增：电力塔信息
            "total_towers": n_towers,
            "towers": towers
        }
        
        with open(self.json_path, 'w', encoding='utf-8') as f:
            json.dump(output_data, f, indent=2, ensure_ascii=False)
        
        print(f"Processing completed: {len(power_lines_data)} power lines, {n_towers} towers")
if __name__ == "__main__":
    generator = Generator(file_path="../data/A/A.las", json_path="A.json")
    generator.generate(min_line_points=50, min_line_length=200.0, length_method='path', reference_point_method='center')
    generator.dump(fit_method="catenary")