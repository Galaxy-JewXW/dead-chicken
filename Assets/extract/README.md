# 电力线提取和电力塔坐标计算系统

本项目实现了从LAS点云数据中提取电力线并计算电力塔坐标的完整流程。

## 功能概述

### 第一阶段：电力线提取 (PowerLineExtractor)
- 从LAS点云文件中提取电力线
- 使用线性特征分析和DBSCAN聚类
- 支持动态参数调整，适应复杂地形
- 生成电力线端点JSON文件

### 第二阶段：电力塔坐标提取 (TowerCoordinateExtractor)
- 从电力线端点JSON文件中提取电力塔坐标
- 使用DBSCAN聚类识别电力塔位置
- 建立塔-线-塔连通关系图
- 对电力塔进行分组和排序
- 生成电力塔坐标CSV文件

## 支持的数据格式

### B.csv格式说明（支持分组连线）

B.csv格式是一个新的电力线数据格式，支持按group分组连线。每个group内的电塔会相互连线，但不同group之间不会连线。

#### 数据格式

```
group_id,order,x,y,z,line_count
```

- `group_id`: 电塔所属的组ID（整数）
- `order`: 在组内的顺序（整数，从0开始）
- `x`: 水平X坐标
- `y`: 水平Y坐标  
- `z`: 高度坐标（用作电塔高度）
- `line_count`: 线路数量（信息字段）

##### 示例数据

```
group_id,order,x,y,z,line_count
0,0,293.72,336.85,4.93,2
0,1,291.10,327.31,7.75,2
0,2,304.29,320.29,10.78,1
1,0,-366.53,76.75,5.99,4
1,1,-379.84,60.06,5.84,1
```

**坐标转换说明**：
- B.csv中的X,Y → Unity中的X,Z（水平平面，自动缩放×10并居中）
- B.csv中的Z → Unity中的Y（高度）
- 处理流程：原始坐标 → 缩放(×10) → 居中 → Unity坐标
- Unity坐标：`new Vector3(x - centerX, 0f, y - centerY)`，高度使用z值

#### 使用方法

1. 将B.csv文件放在`Assets/Resources/`目录下
2. 通过SceneInitializer或PowerlineExtractionSceneBuilder加载
3. 每个group内的电塔按order顺序依次连线，不同group之间不会连线

#### 连线规则
- **同组连线**: 每个group内的电塔按order顺序依次连线
- **跨组隔离**: 不同group之间的电塔不会连线
- **顺序连线**: 每个group内，电塔按照order字段的顺序连线（0->1->2->...）

#### 注意事项
- order字段每组内必须连续（0,1,2...）
- group_id建议从0开始
- B.csv文件必须放在`Assets/Resources/`目录下

---

## 文件结构

```
bbb/
├── data/
│   └── B.las                    # 输入的点云数据文件
├── Extractor4.py                # 电力线提取器类
├── tower_coordinate_extractor.py # 电力塔坐标提取器类
├── main_pipeline.py             # 主流程脚本
├── example_usage.py             # 使用示例脚本
├── extract_tower_coordinates.py # 原始脚本（已重构为类）
└── README.md                    # 说明文档
```

## 安装依赖

```bash
# 需要Python 3.11版本
pip install open3d numpy laspy scikit-learn networkx matplotlib scipy tqdm opencv-python
```

## 使用方法

### 方法1：使用主流程脚本（推荐）

```bash
# 基本使用
python main_pipeline.py data/B.las

# 自定义参数
python main_pipeline.py data/B.las \
    --threshold 0.81 \
    --radius 1.5 \
    --height_min 0 \
    --height_max 20 \
    --eps 120 \
    --min_samples 1 \
    --output_dir ./results

# 关闭可视化
python main_pipeline.py data/B.las --no_visualization
```

### 方法2：使用示例脚本

```bash
python example_usage.py
```

### 方法3：编程调用

```python
from Extractor4 import PowerLineExtractor
from tower_coordinate_extractor import TowerCoordinateExtractor

# 第一阶段：电力线提取
extractor = PowerLineExtractor(
    threshold=0.81,
    radius=1.5,
    height_min=0,
    height_max=20,
    enable_visualization=True
)

power_lines = extractor.extract(
    input_file="data/B.las",
    min_line_points=50,
    min_line_length=10.0,
    use_dynamic_params=True
)

# 第二阶段：电力塔坐标提取
tower_extractor = TowerCoordinateExtractor(eps=120, min_samples=1)
csv_file = tower_extractor.process("B_powerline_endpoints.json")
```

## 参数说明

### PowerLineExtractor 参数
- `threshold`: 线特征阈值，默认0.81
- `radius`: 近邻点搜索半径，默认1.5米
- `height_min`: 高程最小值，默认0米
- `height_max`: 高程最大值，默认20米
- `min_line_points`: 聚类筛选的最小点数阈值，默认50
- `min_line_length`: 最小电力线长度阈值，默认10.0米
- `enable_visualization`: 是否启用可视化功能，默认True

### TowerCoordinateExtractor 参数
- `eps`: DBSCAN聚类半径，默认120米
- `min_samples`: DBSCAN最小样本数，默认1

## 输出文件

### 电力线端点JSON文件
```json
[
  {
    "index": 0,
    "start": [x1, y1, z1],
    "end": [x2, y2, z2],
    "count": 点数
  }
]
```

### 电力塔坐标CSV文件
```csv
group_id,order,x,y,z,line_count
0,0,1234.56,5678.90,15.2,2
0,1,1235.67,5679.01,15.1,2
...
```

## 算法流程

### 电力线提取流程
1. 读取LAS点云数据
2. 高程滤波，提取高空点云
3. 计算线性特征，识别电力线点
4. DBSCAN聚类，分离不同电力线
5. 分离单独电力线
6. 基于高度峰值分割
7. 智能断裂线段合并
8. 长度筛选
9. 坐标变换
10. 输出端点JSON文件

### 电力塔坐标提取流程
1. 加载电力线端点JSON文件
2. 端点聚类，识别电力塔位置
3. 建立塔-线-塔连通关系图
4. 分组（连通分量）
5. 组内电力塔排序
6. 输出坐标CSV文件

## 注意事项

1. **输入文件格式**: 仅支持LAS格式的点云文件
2. **内存要求**: 大型点云文件可能需要较多内存
3. **计算时间**: 处理时间取决于点云大小和复杂度
4. **参数调优**: 不同地形和点云质量可能需要调整参数
5. **可视化**: 开启可视化会显示处理过程，但会降低处理速度

## 故障排除

### 常见问题

1. **导入错误**: 确保已安装所有依赖包
2. **文件不存在**: 检查输入文件路径是否正确
3. **内存不足**: 尝试关闭可视化或使用更小的参数
4. **提取效果差**: 调整threshold、radius等参数

### 调试建议

1. 使用`--no_visualization`参数关闭可视化以提高速度
2. 调整`eps`参数来优化电力塔聚类效果
3. 调整`threshold`参数来优化电力线提取效果
4. 查看生成的中间文件来诊断问题

## 许可证

本项目仅供学习和研究使用。

## 联系方式

如有问题或建议，请联系项目维护者。 