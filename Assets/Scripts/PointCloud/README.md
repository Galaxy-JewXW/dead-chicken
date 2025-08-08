# 点云处理模块 (PointCloud)

> 📖 **完整使用指南请查看：[电力线可视化系统完整指南.md](../../电力线可视化系统完整指南.md#点云处理模块)**

## 概述
负责处理点云数据的加载、显示、转换等功能，包括LAS文件转换、点云渲染、三维重建等。

## 新增功能 ✨

### PowerlineExtractionSceneBuilder 增强功能

#### 🔧 数据自动缩放功能
- **目标**: 将处理得到的数据进行智能缩放，确保电塔高度在合理范围内
- **配置参数**:
  - `enableAutoScaling`: 是否启用自动数据缩放
  - `targetTowerHeightRange`: 目标电塔平均高度范围（默认10-20）
- **工作原理**:
  1. 分析原始电塔数据，计算平均高度
  2. 根据目标高度范围计算最优缩放因子
  3. 对所有坐标数据统一应用缩放因子
  4. 保持电塔间的相对位置关系不变

#### 📷 相机自动跳转功能
- **目标**: 在电力线提取完成后自动将视角跳转到第一个电塔前
- **配置参数**:
  - `enableAutoJumpToFirstTower`: 是否启用自动相机跳转
  - `cameraDistanceFromTower`: 相机距离电塔的距离（默认30米）
  - `cameraHeightOffset`: 相机高度偏移（默认10米）
- **工作原理**:
  1. 在场景构建完成后自动执行
  2. 查找第一个创建的电塔
  3. 计算合适的相机位置（电塔前方、适当高度）
  4. 如果有CameraManager，切换到第一人称视角
  5. 设置相机位置和朝向，面向电塔

## 文件说明

### PowerlineExtractionSceneBuilder.cs ⭐
- **功能**: 电力线提取场景构建器
- **新增特性**:
  - ✅ 智能数据缩放：自动将电塔缩放到合理高度
  - ✅ 相机自动跳转：完成后自动切换到第一人称视角
  - ✅ 缩放因子计算：基于平均高度的智能缩放算法
  - ✅ 相机管理集成：与CameraManager深度集成
- **职责**:
  - 专门用于电力线提取模式的场景构建
  - 从CSV文件加载电塔数据并应用缩放
  - 在空场景中自动创建电塔和电线
  - 提供沉浸式的场景查看体验

### 使用示例

```csharp
// 在Inspector中配置PowerlineExtractionSceneBuilder
var builder = GetComponent<PowerlineExtractionSceneBuilder>();

// 启用数据缩放
builder.enableAutoScaling = true;
builder.targetTowerHeightRange = new Vector2(15f, 25f); // 目标高度15-25米

// 启用相机自动跳转
builder.enableAutoJumpToFirstTower = true;
builder.cameraDistanceFromTower = 40f; // 距离电塔40米
builder.cameraHeightOffset = 8f; // 相机高度偏移8米

// 构建场景
builder.BuildSceneFromCsv("path/to/tower_centers.csv");
```

## 其他文件说明

### LasToOffConverter.cs
- **功能**: LAS文件转换器
- **职责**: 将LAS格式的点云文件转换为OFF格式，支持高性能处理

### PointCloudViewer.cs
- **功能**: 点云查看器
- **职责**: 提供交互式的点云查看界面，支持缩放、平移、旋转

### PointCloudUIController.cs
- **功能**: 点云UI控制器
- **职责**: 
  - 管理点云相关的UI界面
  - 处理用户交互事件
  - 协调点云加载和显示流程

### PowerlinePointCloudManager.cs
- **功能**: 电力线点云管理器
- **职责**: 
  - 专门处理电力线相关的点云数据
  - 管理点云的加载、显示和优化
  - 与电力线系统集成

### PowerlinePointSizeEnabler.cs
- **功能**: 电力线点大小启用器
- **职责**: 启用和管理点云中点的大小渲染

### PointCloudAutoInitializer.cs
- **功能**: 点云自动初始化器
- **职责**: 自动检测和初始化点云相关组件

### SkyboxRestorer.cs
- **功能**: 天空盒恢复器
- **职责**: 在点云场景切换时恢复天空盒设置

## 依赖关系
- 依赖Unity的Mesh和Material系统用于点云渲染
- 与Powerline模块协作进行电力线三维重建
- 与Camera模块集成提供最佳查看体验
- 与UI模块协作提供用户界面
- 依赖Python脚本进行LAS文件处理

## 技术特点

### 🎯 缩放算法优势
1. **智能计算**: 基于实际数据特征计算最优缩放因子
2. **比例保持**: 保持所有空间关系的一致性
3. **边界保护**: 防止极端缩放值导致的显示问题
4. **用户可控**: 提供灵活的目标范围配置

### 🎮 相机控制优势
1. **无缝集成**: 与现有相机系统完美配合
2. **智能定位**: 自动计算最佳观察位置
3. **平滑过渡**: 提供自然的视角切换体验
4. **错误处理**: 完善的异常处理和降级方案

## 注意事项
- 确保在使用缩放功能前备份原始数据
- 相机跳转功能需要场景中存在CameraManager组件才能获得最佳体验
- 建议在大型点云数据处理时启用缩放功能以获得合理的显示效果
- 点云文件较大时建议使用高性能模式 

## 点云预览功能

- 支持初始界面上传LAS文件后直接预览点云
- 预览窗口支持旋转、缩放、平移、统计信息显示
- 技术实现：PointCloudViewer、PowerlinePointCloudManager
- 支持大数据量点云的高效渲染与交互 