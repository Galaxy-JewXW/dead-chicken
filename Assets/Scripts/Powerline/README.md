# 电力线核心模块 (Powerline)

> 电力线可视化系统的核心业务逻辑，负责电力线的生成、渲染、物理模拟和交互功能

## 📋 目录

- [模块概述](#模块概述)
- [核心组件](#核心组件)
- [功能特性](#功能特性)
- [数据格式](#数据格式)
- [快速开始](#快速开始)
- [配置指南](#配置指南)
- [API参考](#api参考)
- [故障排除](#故障排除)

## 🎯 模块概述

### 设计目标
电力线模块是整个可视化系统的核心，实现了从简单CSV数据到复杂3D电力线系统的完整转换过程。

### 核心特性
- **🚀 简化输入**: 只需电塔位置+高度，自动生成完整的8根导线系统
- **🎯 精确连接**: 导线精确连接到电塔的8个引脚位置，支持动态缩放
- **⚡ 物理弧垂**: 真实的电线下垂效果，符合物理规律
- **🏗️ 智能电塔**: 根据高度自动缩放电塔模型，自动定位贴合地面
- **🌍 地形适配**: 自动适应地形高度变化，确保真实感

## 🧩 核心组件

### 1. SceneInitializer.cs (23KB, 656行)
**主控制器** - 电力线系统的核心引擎

#### 主要职责
- 从CSV文件加载电力线数据
- 创建电力线的三维模型和电塔
- 实现电力线的物理下垂效果
- 地形适配和高度调整
- 管理电力线分段和连接逻辑

#### 核心配置
```csharp
[Header("数据配置")]
public string csvFileName = "simple_towers";
public bool usePrecisePinConnection = true;
public float baseTowerHeight = 2f;

[Header("电塔配置")]
public GameObject towerPrefab;
public bool enableTowerScaling = true;

[Header("物理效果")]
public bool enableSag = true;
public float sagIntensity = 1.0f;
```

### 2. PowerlineInteraction.cs (19KB, 629行)
**交互控制器** - 处理电力线的用户交互

#### 主要功能
- 鼠标悬停高亮效果
- 点击选择电力线
- 电力线属性查询和显示
- 与UI系统的交互接口

#### 交互特性
```csharp
[Header("交互配置")]
public bool enableHighlight = true;
public bool enableClickInfo = true;
public bool enableHoverEffect = true;

[Header("高亮效果")]
public Color hoverColor = new Color(1f, 0.8f, 0.2f);
public Color selectedColor = new Color(0.2f, 0.8f, 1f);
public float highlightIntensity = 1.5f;
```

### 3. TowerPinpointSystem.cs (20KB, 535行)
**引脚连接系统** - 精确的引脚定位和连接

#### 核心功能
- 自动识别电塔的8个引脚位置（上层4个，下层4个）
- 支持动态电塔缩放的引脚位置计算
- 提供多种引脚布局方案

#### 引脚布局
```
引脚编号（0-7）：
上层: 0(左外) 1(左中) 2(右中) 3(右外) - 地线(GroundWire)
下层: 4(左外) 5(左中) 6(右中) 7(右外) - 主导线(Conductor)
```

### 4. MultiWireSystemSetup.cs (7.5KB, 234行)
**快速配置工具** - 一键配置整个电力线系统

#### 配置功能
- 自动配置所有相关组件
- 一键启用精确引脚连接
- 快速切换不同配置模式

### 5. GoodTowerSetup.cs (3.6KB, 122行)
**电塔配置助手** - 专门为GoodTower.prefab设计的配置工具

#### 功能特性
- 自动配置GoodTower电塔系统
- 修正电塔位置问题
- 重新生成电塔系统

## 🚀 功能特性

### 智能导线生成
```csharp
// 自动生成8根导线
// 4根地线连接到上层引脚(0-3)
// 4根主导线连接到下层引脚(4-7)
for (int wireIndex = 0; wireIndex < 8; wireIndex++)
{
    var wireInfo = new PowerlineInfo
    {
        wireType = wireIndex < 4 ? "GroundWire" : "Conductor",
        index = wireIndex,
        // 自动分配引脚连接
    };
}
```

### 精确引脚连接
```csharp
// 动态缩放引脚位置
Vector3 scaledPinPosition = originalPinPosition * scaleRatio;
Vector3 worldPinPosition = towerTransform.TransformPoint(scaledPinPosition);

// 确保导线精确连接到引脚
lineRenderer.SetPosition(pointIndex, worldPinPosition);
```

### 物理弧垂效果
```csharp
// 计算电线下垂
float distanceRatio = (float)i / (segments - 1);
float sagAmount = Mathf.Sin(distanceRatio * Mathf.PI) * sagIntensity;
Vector3 saggedPosition = Vector3.Lerp(startPoint, endPoint, distanceRatio);
saggedPosition.y -= sagAmount;
```

### 地形适配
```csharp
// 自动适应地形高度
if (terrainManager != null)
{
    float terrainHeight = terrainManager.GetTerrainHeight(position.x, position.z);
    position.y = Mathf.Max(position.y, terrainHeight);
}
```

## 支持的数据格式

### B.csv格式（分组连线）

- 列：group_id,order,x,y,z,line_count
- 同组内按order顺序连线，不同group不连线
- X,Y→Unity的X,Z，Z→Y（高度），自动缩放×10并居中
- 文件需放在Assets/Resources/

## 🚀 快速开始

### 方法一：使用MultiWireSystemSetup（推荐）

```csharp
// 1. 创建空物体，添加MultiWireSystemSetup脚本
GameObject setupObject = new GameObject("PowerlineSetup");
MultiWireSystemSetup setup = setupObject.AddComponent<MultiWireSystemSetup>();

// 2. 配置参数
setup.towerPrefab = Resources.Load<GameObject>("Prefabs/GoodTower");
setup.enablePinConnection = true;

// 3. 右键选择"配置简化输入模式"
```

### 方法二：手动配置

```csharp
// 1. 创建主控制器
GameObject mainController = new GameObject("SceneInitializer");
SceneInitializer initializer = mainController.AddComponent<SceneInitializer>();

// 2. 添加引脚系统
TowerPinpointSystem pinSystem = mainController.AddComponent<TowerPinpointSystem>();

// 3. 配置参数
initializer.csvFileName = "simple_towers";
initializer.usePrecisePinConnection = true;
initializer.pinpointSystem = pinSystem;
```

## ⚙️ 配置指南

### 基础配置
```csharp
[Header("数据配置")]
public string csvFileName = "simple_towers";        // CSV文件名（不含扩展名）
public bool usePrecisePinConnection = true;        // 启用精确引脚连接
public float baseTowerHeight = 2f;                 // Unity模型原始高度

[Header("电塔配置")]
public GameObject towerPrefab;                     // 电塔预制体
public bool enableTowerScaling = true;            // 启用电塔缩放
public Vector3 towerPositionOffset = Vector3.zero; // 位置偏移

[Header("导线配置")]
public Material wirelineMaterial;                  // 导线材质
public float wireWidth = 0.1f;                    // 导线宽度
public int sagSegments = 50;                       // 弧垂分段数
```

### 引脚系统配置
```csharp
[Header("引脚布局")]
public float debugUpperArmHeight = 1.0f;          // 上层横臂高度比例
public float debugLowerArmHeight = 0.65f;         // 下层横臂高度比例
public float debugArmWidth = 0.6f;                // 横臂宽度
public bool showPinMarkers = false;               // 显示引脚标记
```

### 物理效果配置
```csharp
[Header("物理效果")]
public bool enableSag = true;                      // 启用弧垂效果
public float sagIntensity = 1.0f;                  // 弧垂强度
public AnimationCurve sagCurve;                    // 弧垂曲线
```

## 🔌 API参考

### SceneInitializer 主要方法

```csharp
// 初始化系统
public void Initialize()

// 加载CSV数据
public void LoadPowerlineData(string fileName)

// 创建电力线系统
public void CreatePowerlineSystem()

// 创建单条电力线
public void CreatePowerline(List<Vector3> points, PowerlineInfo info)

// 创建电塔
public GameObject CreateTower(Vector3 position, float height)

// 获取电力线信息
public PowerlineInfo GetPowerlineInfo(int index)
```

### TowerPinpointSystem 主要方法

```csharp
// 获取引脚位置
public Vector3 GetPinPosition(int pinIndex, Transform towerTransform, float scaleRatio)

// 获取所有引脚位置
public Vector3[] GetAllPinPositions(Transform towerTransform, float scaleRatio)

// 测试引脚位置
[ContextMenu("测试引脚位置")]
public void TestPinPositions()

// 获取引脚布局信息
public string GetPinLayoutInfo()
```

### PowerlineInteraction 主要方法

```csharp
// 选择电力线
public void SelectPowerline()

// 取消选择
public void DeselectPowerline()

// 获取详细信息
public PowerlineDetailInfo GetDetailedInfo()

// 设置电力线信息
public void SetPowerlineInfo(SceneInitializer.PowerlineInfo info)
```

## 🔧 故障排除

### 常见问题解决

#### Q: 电塔位置不正确？
A: 
```csharp
// 解决方案1：使用GoodTowerSetup修正
// 右键GoodTowerSetup → "修正现有电塔位置"

// 解决方案2：检查配置
initializer.enableTowerScaling = true;
initializer.baseTowerHeight = 2f; // 确保与模型实际高度匹配
```

#### Q: 导线连接不准确？
A:
```csharp
// 解决方案：确保引脚系统正确配置
initializer.usePrecisePinConnection = true;
initializer.pinpointSystem = GetComponent<TowerPinpointSystem>();

// 调试引脚位置
pinpointSystem.showPinMarkers = true;
pinpointSystem.TestPinPositions(); // 在Context Menu中执行
```

#### Q: 电力线不显示？
A:
```csharp
// 检查数据文件
// 确保CSV文件位于Resources目录下
// 文件格式正确，无额外空格或特殊字符

// 检查材质配置
initializer.wirelineMaterial = Resources.Load<Material>("DefaultLineMaterial");
```

#### Q: 性能问题？
A:
```csharp
// 优化建议
initializer.sagSegments = 20; // 减少弧垂分段数
initializer.enableSag = false; // 关闭弧垂效果（如不需要）

// 使用LOD系统
// 远距离时减少细节
```

### 调试工具

#### 1. 引脚位置调试
```csharp
// 在TowerPinpointSystem组件上右键
[ContextMenu("测试引脚位置")]
public void TestPinPositions()

// 启用可视化标记
pinpointSystem.showPinMarkers = true;
```

#### 2. 数据验证
```csharp
// 验证CSV数据加载
public void ValidateCSVData()
{
    foreach (var line in csvData)
    {
        Debug.Log($"Tower: {line.position}, Height: {line.height}");
    }
}
```

#### 3. 性能监控
```csharp
// 监控电力线创建性能
System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
CreatePowerlineSystem();
stopwatch.Stop();
Debug.Log($"电力线创建耗时: {stopwatch.ElapsedMilliseconds}ms");
```

## 📚 技术细节

### 缩放算法
```csharp
// 计算电塔缩放比例
float requiredHeight = csvHeight; // CSV中指定的高度
float originalHeight = baseTowerHeight; // Unity模型原始高度
float scaleRatio = requiredHeight / originalHeight;

// 应用缩放
towerTransform.localScale = Vector3.one * scaleRatio;
```

### 引脚映射
```csharp
// 引脚到导线的映射关系
private static readonly Dictionary<int, string> PinToWireType = new Dictionary<int, string>
{
    {0, "GroundWire"}, {1, "GroundWire"}, {2, "GroundWire"}, {3, "GroundWire"},
    {4, "Conductor"}, {5, "Conductor"}, {6, "Conductor"}, {7, "Conductor"}
};
```

### 弧垂计算
```csharp
// 基于正弦函数的弧垂模拟
for (int i = 0; i < sagSegments; i++)
{
    float t = (float)i / (sagSegments - 1);
    Vector3 basePosition = Vector3.Lerp(startPoint, endPoint, t);
    float sagOffset = Mathf.Sin(t * Mathf.PI) * sagIntensity;
    Vector3 finalPosition = basePosition + Vector3.down * sagOffset;
    lineRenderer.SetPosition(i, finalPosition);
}
```

---

## 🤝 贡献指南

欢迎提交Issue和Pull Request来改进电力线模块！

1. 遵循现有的代码风格
2. 添加必要的注释和文档
3. 确保向后兼容性
4. 提供测试用例

## 📈 未来规划

- [ ] 支持更多电塔类型
- [ ] 动态载荷计算
- [ ] 风力效果模拟
- [ ] 实时电力参数显示
- [ ] 电力线故障模拟 