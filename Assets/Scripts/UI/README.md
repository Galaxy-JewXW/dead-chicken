# UI系统模块 (UI)

> 基于Unity UI Toolkit构建的现代化用户界面系统

## 📋 目录

- [系统概述](#系统概述)
- [核心组件](#核心组件)
- [功能模块](#功能模块)
- [快速开始](#快速开始)
- [API接口](#api接口)
- [样式配置](#样式配置)
- [扩展开发](#扩展开发)

## 🎯 系统概述

### 设计理念
现代化UI系统采用**模块化架构**，每个功能模块独立管理，确保高性能和易维护性。

### 技术特点
- **🚀 UI Toolkit**: 基于Unity最新UI框架构建
- **🏗️ 模块化设计**: 每个功能独立控制器，职责清晰
- **🎨 响应式布局**: 适配不同分辨率和屏幕比例
- **⚡ 高性能**: 按需更新，智能缓存机制
- **🔧 易于扩展**: 标准化接口，支持自定义插件

### 界面布局
```
┌─────────────────────────────────────────────────────────────┐
│ 顶部导航栏: [标题] [相机] [测量] [危险物] [电力线] [面板切换]      │
├─────────────────────────────────────────────────────────────┤
│ 左侧    │                                   │ 右侧侧栏      │
│ (预留)   │          主视图区域                  │ (功能面板)     │
│        │                                   │              │
├─────────────────────────────────────────────────────────────┤
│ 底部状态栏: [模式显示] [坐标信息] [操作提示]                    │
└─────────────────────────────────────────────────────────────┘
```

## 🧩 核心组件

### SimpleUIToolkitManager.cs (42KB, 1165行)
**主UI管理器** - 系统的核心控制中心

#### 主要职责
- 创建和管理UI文档结构
- 协调各个功能模块的UI控制器
- 处理模式切换和界面状态管理
- 提供统一的UI创建和样式接口

#### 核心功能
```csharp
public enum UIMode
{
    Normal,     // 正常模式
    Camera,     // 相机控制模式
    Measure,    // 测量模式
    Danger,     // 危险标记模式
    Powerline   // 电力线信息模式
}

// 主要方法
public void SwitchMode(UIMode mode)
public void ShowPowerlineInfo(PowerlineInteraction powerline)
public void UpdateMeasureInfo()
public void UpdateStatusBar(string message)
```

### UITheme.cs (4.8KB, 155行)
**主题配置系统** - 统一管理UI样式和主题

#### 配置选项
```csharp
[Header("颜色配置")]
public Color primaryColor;      // 主色调
public Color accentColor;       // 强调色
public Color dangerColor;       // 危险色
public Color successColor;      // 成功色

[Header("字体配置")]
public Font primaryFont;        // 主要字体
public int titleFontSize = 18;  // 标题字体大小
public int normalFontSize = 14; // 正常字体大小
```

## 🎮 功能模块

### 1. 测量控制器 (UIToolkitMeasureController.cs)

#### 功能特性
- ✅ 多点连续测量
- ✅ 实时距离计算和显示
- ✅ 可视化测量线条
- ✅ 详细的段落距离信息

#### 核心方法
```csharp
public void StartMeasuring()        // 开始测量
public void StopMeasuring()         // 停止测量
public void AddMeasurePoint(Vector3 point)  // 添加测量点
public void ClearMeasurements()     // 清除所有测量
```

#### 使用方式
1. 点击顶部"测量"按钮进入测量模式
2. 点击"开始测量"按钮
3. 在场景中点击添加测量点
4. 右键结束测量

### 2. 危险标记控制器 (UIToolkitDangerController.cs)

#### 功能特性
- ✅ 4种危险类型（建筑物、植被、设备、其他）
- ✅ 3个危险等级（低、中、高）
- ✅ 完整的标记管理系统
- ✅ 详细信息编辑和查看

#### 核心方法
```csharp
public void StartDangerMarking()    // 开始危险标记
public void StopDangerMarking()     // 停止危险标记
public void CreateDangerMarker(Vector3 position)  // 创建标记
public void ShowDangerInfo(DangerMarker marker)   // 显示详情
```

#### 危险类型配置
```csharp
public enum DangerType
{
    Building,    // 建筑物
    Vegetation,  // 植被
    Equipment,   // 设备
    Other        // 其他
}

public enum DangerLevel
{
    Low,         // 低危险
    Medium,      // 中等危险
    High         // 高危险
}
```

### 3. 相机UI控制器 (UIToolkitCameraController.cs)

#### 功能特性
- ✅ 相机模式切换界面
- ✅ 实时相机状态显示
- ✅ 快捷操作按钮
- ✅ 操作指南显示

#### 核心方法
```csharp
public void SwitchToFirstPerson()   // 切换到第一人称
public void SwitchToGodView()       // 切换到俯视视角
public void SwitchToFlyCamera()     // 切换到飞行视角
public void ResetCamera()           // 重置相机
```

## 🚀 快速开始

### 1. 基础设置

```csharp
// 在场景中创建UI管理器
GameObject uiManager = new GameObject("UIManager");
SimpleUIToolkitManager manager = uiManager.AddComponent<SimpleUIToolkitManager>();

// 配置主题（可选）
UITheme theme = Resources.Load<UITheme>("DefaultUITheme");
// 主题将自动应用
```

### 2. 启用功能模块

```csharp
// 各个控制器会自动初始化
// 无需手动配置，开箱即用
```

### 3. 模式切换

```csharp
// 代码方式切换模式
var uiManager = FindObjectOfType<SimpleUIToolkitManager>();
uiManager.SwitchMode(SimpleUIToolkitManager.UIMode.Measure);

// 或使用快捷键
// 1 - 相机模式
// 2 - 测量模式
// 3 - 危险标记模式
```

## 🔌 API接口

### 核心接口

#### SimpleUIToolkitManager
```csharp
// 模式管理
public void SwitchMode(UIMode mode)
public UIMode GetCurrentMode()

// 信息显示
public void ShowPowerlineInfo(PowerlineInteraction powerline)
public void HidePowerlineInfo()
public void UpdateMeasureInfo()

// 状态更新
public void UpdateStatusBar(string message)
public void UpdateCoordinates(Vector3 position)

// 样式应用
public void ApplyFont(Label label)
public void ApplyFont(Button button)
```

#### 功能控制器通用接口
```csharp
public interface IUIController
{
    void Initialize();
    void Show();
    void Hide();
    void UpdateUI();
}
```

### 事件系统
```csharp
// 模式切换事件
public event System.Action<UIMode> OnModeChanged;

// 测量事件
public event System.Action<Vector3> OnMeasurePointAdded;
public event System.Action OnMeasureComplete;

// 危险标记事件
public event System.Action<DangerMarker> OnDangerMarkerCreated;
```

## 🎨 样式配置

### UI样式文件
位置: `Resources/Styles/MaterialDesign.uss`

#### 主要样式类
```css
.panel {
    background-color: rgba(57, 61, 114, 0.95);
    border-radius: 8px;
    padding: 15px;
    margin: 10px;
}

.button-primary {
    background-color: rgb(56, 120, 255);
    color: white;
    border-radius: 5px;
    padding: 8px 16px;
}

.label-title {
    font-size: 20px;
    color: white;
    -unity-font-style: bold;
}
```

### 动态样式应用
```csharp
// 在代码中应用样式
element.AddToClassList("panel");
button.AddToClassList("button-primary");
label.AddToClassList("label-title");
```

### 主题自定义
```csharp
// 自定义主题颜色
var theme = ScriptableObject.CreateInstance<UITheme>();
theme.primaryColor = new Color(0.39f, 0.4f, 0.95f);
theme.accentColor = new Color(0.3f, 0.6f, 1f);

// 应用主题
uiManager.ApplyTheme(theme);
```

## 🔧 扩展开发

### 添加新的功能模块

#### 1. 创建控制器类
```csharp
public class CustomUIController : MonoBehaviour, IUIController
{
    private SimpleUIToolkitManager uiManager;
    private VisualElement panel;
    
    public void Initialize()
    {
        uiManager = FindObjectOfType<SimpleUIToolkitManager>();
        CreateUI();
    }
    
    public void Show()
    {
        panel?.SetDisplayed(true);
    }
    
    public void Hide()
    {
        panel?.SetDisplayed(false);
    }
    
    public void UpdateUI()
    {
        // 更新UI逻辑
    }
    
    private void CreateUI()
    {
        // 创建UI元素
    }
}
```

#### 2. 集成到主管理器
```csharp
// 在SimpleUIToolkitManager中添加
public CustomUIController customController;

void InitializeControllers()
{
    customController = GetOrAddComponent<CustomUIController>();
    customController.Initialize();
}
```

### 自定义UI元素
```csharp
// 创建自定义按钮
var customButton = new Button(() => {
    Debug.Log("Custom button clicked!");
});
customButton.text = "自定义按钮";
customButton.AddToClassList("button-primary");

// 添加到面板
panel.Add(customButton);
```

### 性能优化建议

#### 1. 按需更新
```csharp
// 避免每帧更新UI
private float lastUpdateTime;
private const float UPDATE_INTERVAL = 0.1f; // 100ms

void Update()
{
    if (Time.time - lastUpdateTime > UPDATE_INTERVAL)
    {
        UpdateUI();
        lastUpdateTime = Time.time;
    }
}
```

#### 2. 对象池管理
```csharp
// 重用UI元素而不是频繁创建销毁
private Queue<VisualElement> elementPool = new Queue<VisualElement>();

VisualElement GetPooledElement()
{
    if (elementPool.Count > 0)
        return elementPool.Dequeue();
    else
        return new VisualElement();
}
```

## 🐛 常见问题

### Q: UI不显示或显示异常？
A: 检查以下几点：
1. 确认UIDocument组件正确配置
2. 检查PanelSettings是否正确设置
3. 验证CSS样式文件是否加载

### Q: 字体显示问题？
A: 设置备用字体：
```csharp
public Font fallbackFont; // 在Inspector中设置
uiManager.fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
```

### Q: 性能问题？
A: 优化建议：
1. 减少UI更新频率
2. 使用对象池管理UI元素
3. 避免复杂的嵌套结构

### Q: 样式不生效？
A: 检查样式文件路径和类名是否正确：
```csharp
// 确保样式文件在Resources/Styles/目录下
var styleSheet = Resources.Load<StyleSheet>("Styles/MaterialDesign");
rootElement.styleSheets.Add(styleSheet);
```

## 点云预览功能

初始界面支持上传LAS点云文件后直接预览点云，无需进入主系统。预览窗口支持3D交互、统计信息显示、相机控制等。

### 使用流程
1. 选择LAS文件后，点击“预览点云”按钮
2. 独立窗口显示点云，可旋转、缩放、平移
3. 关闭后可继续提取或重新选择文件

### 技术实现
- 组件：InitialInterfaceManager、PointCloudViewer、PowerlinePointCloudManager
- 支持延迟加载、内存优化、LOD渲染

## 电力线信息显示优化

- 信息面板显示：电力线长度、宽度、弯曲度、状态（优秀/良好/需维护）
- 状态可设置，自动记录设置时间，颜色区分
- 弯曲度=弧垂高度/档距长度×100%，单位%（0-10%）
- 宽度：地线12.6mm，主导线28.6mm，默认20mm
- 支持状态设置对话框、状态持久化（生命周期内）

### 常见问题
- 信息不准确：检查路径点、类型、参数
- UI不显示：检查UIDocument、PanelSettings、样式文件

---

## 📚 相关文档

- [Unity UI Toolkit官方文档](https://docs.unity3d.com/Manual/UIElements.html)
- [CSS样式参考](https://docs.unity3d.com/Manual/UIE-USS.html)
- [UXML文件格式](https://docs.unity3d.com/Manual/UIE-UXML.html)

## 🤝 贡献指南

欢迎提交Issue和Pull Request来改进UI系统！

1. 遵循现有的代码风格
2. 添加必要的注释和文档
3. 确保向后兼容性
4. 提供测试用例 