# 统计大屏UI显示问题解决方案

## 问题描述

统计大屏页面中四个部分的静态测试数据都没有正常显示，UI元素引用失败，导致数据无法渲染。

## 问题分析

### 1. UI初始化时机问题
- UIDocument组件在Start()方法执行时可能尚未完全加载
- UI元素引用在元素创建完成之前就开始执行

### 2. 元素引用不匹配
- 代码中通过`Q()`方法查找UXML中定义的面板
- 可能存在引用名称不匹配或元素层级问题

### 3. 样式应用问题
- CSS样式可能没有正确应用到动态创建的元素上
- 文本元素可见性设置不当

## 解决方案

### 1. 延迟初始化UI
```csharp
void Start()
{
    InitializeLogSystem();
    // 延迟初始化UI，确保UIDocument完全加载
    Invoke("InitializeUI", 0.5f);
}
```

### 2. 添加UI初始化状态检查
```csharp
// UI初始化状态
private bool isUIInitialized = false;

private void InitializeUI()
{
    // 确保UIDocument已加载
    if (statisticsUIDocument.rootVisualElement == null)
    {
        WriteDebugLog("UIDocument尚未加载完成，等待下一帧...");
        Invoke("InitializeUI", 0.1f);
        return;
    }
    
    // 获取面板引用后设置初始化状态
    if (deviceStatsPanel != null && performancePanel != null && 
        inspectionPanel != null && dangerPanel != null)
    {
        isUIInitialized = true;
        WriteDebugLog("统计大屏UI已初始化 - 四图表布局");
        
        // 立即刷新一次面板
        RefreshAllPanels();
    }
    else
    {
        WriteErrorLog("部分面板引用获取失败，重试初始化...");
        Invoke("InitializeUI", 0.5f);
    }
}
```

### 3. 强制文本可见性
```csharp
private void AddStatItem(VisualElement container, string label, string value, string type)
{
    // 创建标签元素 - 完全独立，不依赖CSS
    var labelElement = new Label();
    
    // 强制设置文本内容
    labelElement.text = label;
    
    // 强制文本可见性
    labelElement.style.display = DisplayStyle.Flex;
    labelElement.style.visibility = Visibility.Visible;
    labelElement.style.opacity = 1f;
    
    // 强制文本颜色和背景
    labelElement.style.color = Color.white;
    labelElement.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    
    // 强制设置尺寸
    labelElement.style.minHeight = 20;
    labelElement.style.minWidth = 80;
    labelElement.style.height = 20;
    labelElement.style.width = 80;
}
```

### 4. 优化图表渲染
```csharp
public void CreateBarChart(VisualElement container, List<float> data, List<string> labels)
{
    // 减小图表高度以适应面板
    chartContainer.style.height = 100;
    chartContainer.style.minHeight = 100;
    
    // 确保柱子有最小高度
    float actualHeight = Mathf.Max(heightPercent, 10f);
    
    // 强制设置标签样式
    valueLabel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    valueLabel.style.paddingLeft = 2;
    valueLabel.style.paddingRight = 2;
    valueLabel.style.borderRadius = 2;
}
```

### 5. 创建统一的容器创建方法
```csharp
/// <summary>
/// 创建统计项容器
/// </summary>
private VisualElement CreateStatsContainer()
{
    var statsContainer = new VisualElement();
    statsContainer.AddToClassList("stats-container");
    statsContainer.style.flexDirection = FlexDirection.Row;
    statsContainer.style.flexWrap = Wrap.Wrap;
    statsContainer.style.marginBottom = 15;
    statsContainer.style.paddingTop = 10;
    statsContainer.style.paddingBottom = 10;
    statsContainer.style.backgroundColor = new Color(0.05f, 0.1f, 0.15f, 0.3f);
    statsContainer.style.minHeight = 60;
    statsContainer.style.position = Position.Relative;
    statsContainer.AddToClassList("rounded-container");
    statsContainer.style.alignItems = Align.Center;
    statsContainer.style.justifyContent = Justify.Center;
    
    return statsContainer;
}

/// <summary>
/// 创建图表容器
/// </summary>
private VisualElement CreateChartContainer()
{
    var chartContainer = new VisualElement();
    chartContainer.AddToClassList("chart-container");
    chartContainer.style.height = 120;
    chartContainer.style.backgroundColor = new Color(0.1f, 0.15f, 0.2f, 0.9f);
    chartContainer.AddToClassList("rounded-chart");
    chartContainer.style.paddingLeft = 15;
    chartContainer.style.paddingRight = 15;
    chartContainer.style.paddingTop = 15;
    chartContainer.style.paddingBottom = 15;
    chartContainer.style.marginTop = 15;
    chartContainer.style.minHeight = 120;
    chartContainer.style.minWidth = 200;
    chartContainer.style.position = Position.Relative;
    chartContainer.style.alignItems = Align.Center;
    chartContainer.style.justifyContent = Justify.Center;
    
    return chartContainer;
}
```

## 测试验证

### 1. 添加测试脚本
创建`StatisticsDashboardTest.cs`脚本来验证UI显示是否正常：

```csharp
public class StatisticsDashboardTest : MonoBehaviour
{
    [Header("测试配置")]
    public bool testOnStart = true;
    public float testInterval = 3f;
    
    public void RunTest()
    {
        Debug.Log("=== 统计大屏测试开始 ===");
        
        // 检查组件引用
        // 检查UI元素
        // 检查面板引用
        
        Debug.Log("=== 统计大屏测试完成 ===");
    }
}
```

### 2. 检查要点
- UIDocument组件是否正确挂载
- UXML文件中的元素名称是否与代码引用匹配
- 面板引用是否成功获取
- 内容区域是否正确创建
- 测试数据是否正确生成

## 常见问题排查

### 1. 面板引用为空
- 检查UXML文件中的元素名称
- 确认UIDocument已完全加载
- 查看控制台日志输出

### 2. 文本不可见
- 强制设置文本颜色为白色
- 添加背景色以便调试
- 确保文本元素尺寸正确

### 3. 图表不显示
- 检查ChartRenderer组件是否正确创建
- 确认数据格式正确
- 查看图表容器样式设置

## 参考实现

参考场景总览功能的实现思路：
- 使用延迟初始化确保组件完全加载
- 添加状态检查避免重复操作
- 强制设置样式确保元素可见
- 创建统一的容器创建方法

## 总结

通过以上解决方案，统计大屏的UI显示问题应该能够得到解决。关键是要确保：

1. UI初始化时机正确
2. 元素引用成功获取
3. 样式强制应用
4. 测试数据正确生成
5. 图表渲染器正常工作

如果问题仍然存在，请检查控制台日志输出，根据具体的错误信息进行进一步排查。
