# 通用组件模块 (Components)

本模块包含电力线可视化系统中的通用组件，提供基础功能和扩展能力。

## 核心组件

### 1. MarkerPrefab
- **功能**: 标记点预制件，用于在场景中标识重要位置
- **特性**: 支持自定义外观、交互响应、信息显示
- **使用**: 拖拽到场景中，配置标记属性

### 2. 树木系统集成
- **功能**: 在电塔附近自动放置树木，美化场景环境
- **特性**: 
  - 支持CSV数据驱动的树木位置
  - 自动地形适配
  - 随机旋转和缩放
  - 防止树木重叠
- **使用**: 通过PowerlineExtractionSceneBuilder组件配置和触发

## 树木系统详细说明

### 系统架构
树木系统采用与电塔系统相同的架构模式，使用 `SimpleTreeData` 类管理数据：

```csharp
[System.Serializable]
public class SimpleTreeData
{
    public Vector3 position;      // 树木位置
    public float height;          // 树木高度
    public int groupId;           // 分组ID
    public int towerId;           // 关联电塔ID
    public string treeType;       // 树木类型
    public float scale;           // 缩放比例
}
```

### 核心方法

#### 1. LoadSimpleTreeData()
- **功能**: 从CSV文件加载树木数据
- **返回**: `List<SimpleTreeData>` 树木数据列表
- **特点**: 自动解析CSV格式，支持坐标转换

#### 2. PlaceTreesFromSimplifiedInput()
- **功能**: 从简化数据放置树木
- **返回**: `List<GameObject>` 创建的树木对象列表
- **特点**: 仿照电塔的构建方式，更加可靠

#### 3. CreateTreeAtPosition()
- **功能**: 在指定位置创建单棵树木
- **参数**: `SimpleTreeData` 树木数据
- **返回**: `GameObject` 创建的树木对象
- **特点**: 支持地形适配、随机偏移、自动缩放

### 使用方法

#### 方法1: 自动构建（推荐）
在 `PowerlineExtractionSceneBuilder` 组件中：
1. 勾选 `Enable Tree Placement`
2. 设置 `Tree CSV File Name` 为 "trees"
3. 配置其他树木参数
4. 运行场景构建流程

#### 方法2: 手动触发
```csharp
// 获取组件引用
var builder = FindObjectOfType<PowerlineExtractionSceneBuilder>();

// 手动构建树木
builder.BuildTreesFromCsv();

// 重新加载树木
builder.ReloadTrees();

// 清除所有树木
builder.ClearAllTrees();
```

#### 方法3: 右键菜单
在Inspector中右键点击 `PowerlineExtractionSceneBuilder` 组件：
- `重新加载树木` - 重新加载和放置树木
- `清除所有树木` - 清除场景中的所有树木
- `测试树木系统` - 测试树木系统功能

### 配置参数

| 参数                           | 说明                   | 默认值              |
| ------------------------------ | ---------------------- | ------------------- |
| `Enable Tree Placement`        | 是否启用树木放置       | true                |
| `Tree Prefab`                  | 树木预制件             | 自动从Resources加载 |
| `Tree CSV File Name`           | 树木CSV文件名          | "trees"             |
| `Trees Per Tower`              | 每个电塔附近的树木数量 | 3                   |
| `Min Tree Distance From Tower` | 树木距离电塔的最小距离 | 8f                  |
| `Max Tree Distance From Tower` | 树木距离电塔的最大距离 | 25f                 |
| `Enable Tree Auto Scaling`     | 是否启用树木自动缩放   | true                |
| `Tree Height Range`            | 树木目标高度范围       | (3f, 8f)            |

### CSV文件格式

树木CSV文件应包含以下列：
```csv
tree_id,group_id,tower_id,x,y,z,tree_type
1,1,1,100.5,200.3,50.2,Oak
2,1,1,105.2,198.7,49.8,Pine
3,1,2,150.1,180.5,52.1,Maple
```

### 调试和测试

#### 控制台日志
系统会输出详细的日志信息：
- `[PowerlineExtractionSceneBuilder] 成功加载 X 棵简化树木数据`
- `[PowerlineExtractionSceneBuilder] 成功放置了 X 棵树`
- `[PowerlineExtractionSceneBuilder] 树木场景构建完成！`

#### 测试方法
使用 `TestTreeSystem()` 方法进行系统测试：
```csharp
builder.TestTreeSystem();
```

### 故障排除

#### 问题1: 树木不显示
- 检查 `Enable Tree Placement` 是否勾选
- 检查 `trees.csv` 文件是否存在且格式正确
- 检查Console日志中的错误信息

#### 问题2: 树木位置不正确
- 检查CSV文件中的坐标值
- 检查坐标转换逻辑
- 检查地形管理器是否正常工作

#### 问题3: 树木数量不足
- 检查CSV文件中的数据行数
- 检查 `Trees Per Tower` 设置
- 检查电塔数量是否足够

## 扩展功能

### 自定义树木类型
可以通过扩展 `SimpleTreeData` 类添加更多属性：
- 树木年龄
- 季节变化
- 生长状态
- 特殊效果

### 性能优化
- 支持LOD系统
- 批量渲染
- 视锥体剔除
- 动态加载

## 更新日志

### v2.0.0 (当前版本)
- 重构树木系统，采用与电塔系统相同的架构
- 新增 `SimpleTreeData` 类
- 新增 `PlaceTreesFromSimplifiedInput()` 方法
- 新增 `BuildTreesFromCsv()` 方法
- 新增 `TestTreeSystem()` 方法
- 改进错误处理和日志输出
- 优化地形适配逻辑

### v1.0.0
- 基础树木系统实现
- 支持CSV数据驱动
- 基本的地形适配
- 随机旋转和缩放 