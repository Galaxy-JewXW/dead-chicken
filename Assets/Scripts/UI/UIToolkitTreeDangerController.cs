using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Linq;

/// <summary>
/// UI Toolkit树木危险监测控制器 - 简化版本
/// 专注于核心功能：参数设置、监测控制、统计显示和危险树木列表
/// </summary>
public class UIToolkitTreeDangerController : MonoBehaviour
{
    [Header("监测系统设置")]
    public bool enableAutoMonitoring = true;
    public float monitoringInterval = 5f;
    public float maxDetectionDistance = 100f;
    
    [Header("危险评估参数")]
    public float criticalDistance = 5f;   // 危险距离 - 降低到5米
    public float warningDistance = 15f;   // 警告距离 - 降低到15米
    public float safeDistance = 30f;      // 安全距离 - 降低到30米
    
    [Header("树木生长参数")]
    public float baseGrowthRate = 0.1f;
    
    // UI管理器引用
    private SimpleUIToolkitManager uiManager;
    
    // 监测系统引用
    private TreeDangerMonitor treeDangerMonitor;
    
    // UI元素引用
    private VisualElement treeDangerPanel;
    private VisualElement controlSection;
    private VisualElement statisticsSection;
    private VisualElement treeListSection;
    
    // 控制元素
    private Button startMonitoringButton;
    private Button clearMarkersButton;
    
    // 显示元素
    private Label statusLabel;
    // 删除不再需要的变量
    // private Label statisticsLabel;
    private VisualElement treeListContainer;
    
    // 监测状态
    private bool isMonitoring = false;
    
    void Start()
    {
        // 查找UI管理器
        uiManager = FindObjectOfType<SimpleUIToolkitManager>();
        if (uiManager == null)
        {
            Debug.LogError("未找到SimpleUIToolkitManager，UIToolkitTreeDangerController无法工作");
            return;
        }
        
        // 查找或创建监测系统
        treeDangerMonitor = FindObjectOfType<TreeDangerMonitor>();
        if (treeDangerMonitor == null)
        {
            var monitorObj = new GameObject("TreeDangerMonitor");
            treeDangerMonitor = monitorObj.AddComponent<TreeDangerMonitor>();
            Debug.Log("已创建TreeDangerMonitor组件");
        }
        
        // 确保参数值已设置
        Debug.Log($"参数初始化 - 危险距离: {criticalDistance}, 警告距离: {warningDistance}, 安全距离: {safeDistance}, 生长率: {baseGrowthRate}");
        
        // 同步参数
        SyncMonitoringParameters();
        
        Initialize();
        
        // 启动自动刷新协程
        StartCoroutine(AutoRefreshCoroutine());
        
        // 启动延迟刷新UI协程，确保参数值显示
        StartCoroutine(DelayedRefreshUI());
        
        // 启动场景检查协程
        StartCoroutine(CheckSceneObjects());
    }
    
    /// <summary>
    /// 延迟刷新UI的协程，确保参数值正确显示
    /// </summary>
    IEnumerator DelayedRefreshUI()
    {
        yield return new WaitForSeconds(0.5f);
        
        // 强制刷新显示
        if (treeDangerPanel != null)
        {
            RefreshDisplay();
            Debug.Log("延迟刷新UI完成");
        }
    }
    
    /// <summary>
    /// 检查场景中的对象情况
    /// </summary>
    IEnumerator CheckSceneObjects()
    {
        yield return new WaitForSeconds(1f);
        
        // 检查场景中的树木
        var trees = FindObjectsOfType<GameObject>().Where(obj => obj.name.ToLower().Contains("tree") || obj.name.ToLower().Contains("树")).ToArray();
        Debug.Log($"场景中找到 {trees.Length} 个树木对象");
        
        // 检查场景中的电力线
        var powerlines = FindObjectsOfType<PowerlineInteraction>();
        Debug.Log($"场景中找到 {powerlines.Length} 个电力线对象");
        
        // 检查距离情况
        if (trees.Length > 0 && powerlines.Length > 0)
        {
            var nearestTree = trees[0];
            var nearestPowerline = powerlines[0];
            var distance = Vector3.Distance(nearestTree.transform.position, nearestPowerline.transform.position);
            Debug.Log($"示例：树木 '{nearestTree.name}' 与电力线 '{nearestPowerline.name}' 的距离为 {distance:F2}m");
            
            if (distance <= criticalDistance)
            {
                Debug.Log($"⚠️ 发现危险情况！距离 {distance:F2}m <= 危险阈值 {criticalDistance}m");
            }
            else if (distance <= warningDistance)
            {
                Debug.Log($"⚠️ 发现警告情况！距离 {distance:F2}m <= 警告阈值 {warningDistance}m");
            }
            else if (distance <= safeDistance)
            {
                Debug.Log($"⚠️ 发现安全边界情况！距离 {distance:F2}m <= 安全阈值 {safeDistance}m");
            }
            else
            {
                Debug.Log($"✅ 距离安全，距离 {distance:F2}m > 安全阈值 {safeDistance}m");
            }
        }
    }
    
    public void Initialize()
    {
        if (uiManager == null) return;
        Debug.Log("UIToolkitTreeDangerController已初始化");
    }
    
    /// <summary>
    /// 创建树木危险监测面板UI
    /// </summary>
    public VisualElement CreateTreeDangerPanel()
    {
        treeDangerPanel = new VisualElement();
        treeDangerPanel.style.width = Length.Percent(100);
        treeDangerPanel.style.height = Length.Percent(100);
        treeDangerPanel.style.flexDirection = FlexDirection.Column;
        
        // 创建控制区域
        CreateControlSection();
        
        // 创建统计信息区域
        CreateStatisticsSection();
        
        // 创建所有树木距离信息区域
        CreateAllTreesDistanceSection();
        
        // 创建树木列表区域
        CreateTreeListSection();
        
        return treeDangerPanel;
    }
    
    void CreateControlSection()
    {
        controlSection = new VisualElement();
        controlSection.style.backgroundColor = new Color(0.95f, 0.98f, 1f, 1f);
        controlSection.style.marginBottom = 10;
        controlSection.style.paddingTop = 12;
        controlSection.style.paddingBottom = 12;
        controlSection.style.paddingLeft = 10; // 减少左侧内边距
        controlSection.style.paddingRight = 10; // 减少右侧内边距
        controlSection.style.borderTopLeftRadius = 8;
        controlSection.style.borderTopRightRadius = 8;
        controlSection.style.borderBottomLeftRadius = 8;
        controlSection.style.borderBottomRightRadius = 8;
        controlSection.style.borderLeftWidth = 2;
        controlSection.style.borderRightWidth = 2;
        controlSection.style.borderTopWidth = 2;
        controlSection.style.borderBottomWidth = 2;
        controlSection.style.borderLeftColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        controlSection.style.borderRightColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        controlSection.style.borderTopColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        controlSection.style.borderBottomColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        controlSection.style.flexShrink = 0;
        controlSection.style.width = Length.Percent(100); // 使用百分比宽度，适应父容器
        controlSection.style.maxWidth = Length.Percent(100); // 最大宽度不超过父容器
        
        // 标题
        var titleLabel = new Label("树木危险监测系统");
        titleLabel.style.color = new Color(0.1f, 0.5f, 0.1f, 1f);
        titleLabel.style.fontSize = 14; // 进一步减少字体大小
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 10; // 减少标题下方间距
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(titleLabel);
        controlSection.Add(titleLabel);
        
        // 参数控制区域
        CreateParameterControls();
        
        // 控制按钮区域和状态显示已在CreateParameterControls中创建
        
        treeDangerPanel.Add(controlSection);
    }
    
    void CreateParameterControls()
    {
        // 创建参数控制容器
        var paramContainer = new VisualElement();
        paramContainer.style.flexDirection = FlexDirection.Column;
        paramContainer.style.marginBottom = 15;
        
        // 调试信息
        Debug.Log($"创建参数控制 - 危险距离: {criticalDistance}, 警告距离: {warningDistance}, 安全距离: {safeDistance}, 生长率: {baseGrowthRate}");
        
        // 简化的参数设置 - 只保留核心参数
        CreateSimplifiedParameterRow("危险距离:", criticalDistance, (value) => {
            criticalDistance = value;
            if (treeDangerMonitor != null) treeDangerMonitor.criticalDistance = value;
        }, paramContainer);
        
        CreateSimplifiedParameterRow("警告距离:", warningDistance, (value) => {
            warningDistance = value;
            if (treeDangerMonitor != null) treeDangerMonitor.warningDistance = value;
        }, paramContainer);
        
        CreateSimplifiedParameterRow("安全距离:", safeDistance, (value) => {
            safeDistance = value;
            if (treeDangerMonitor != null) treeDangerMonitor.safeDistance = value;
        }, paramContainer);
        
        CreateSimplifiedParameterRow("基础生长率(m/年):", baseGrowthRate, (value) => {
            baseGrowthRate = value;
            if (treeDangerMonitor != null) treeDangerMonitor.baseGrowthRate = value;
        }, paramContainer);
        
        controlSection.Add(paramContainer);
        
        // 创建控制按钮和状态显示
        CreateControlButtons();
        
        // 强制刷新参数值显示
        StartCoroutine(ForceRefreshParameterValues(paramContainer));
    }
    
    /// <summary>
    /// 强制刷新参数值显示的协程
    /// </summary>
    IEnumerator ForceRefreshParameterValues(VisualElement paramContainer)
    {
        yield return new WaitForSeconds(0.1f);
        
        // 查找所有TextField并强制设置值
        var textFields = paramContainer.Query<TextField>().ToList();
        Debug.Log($"找到 {textFields.Count} 个TextField");
        
        foreach (var textField in textFields)
        {
            if (textField != null)
            {
                // 根据TextField的父级标签来确定应该设置什么值
                var parent = textField.parent;
                if (parent != null)
                {
                    var label = parent.Q<Label>();
                    if (label != null)
                    {
                        string labelText = label.text;
                        float value = 0f;
                        
                        if (labelText.Contains("危险距离"))
                            value = criticalDistance;
                        else if (labelText.Contains("警告距离"))
                            value = warningDistance;
                        else if (labelText.Contains("安全距离"))
                            value = safeDistance;
                        else if (labelText.Contains("基础生长率"))
                            value = baseGrowthRate;
                        
                        if (value > 0f)
                        {
                            textField.value = value.ToString("F1");
                            Debug.Log($"强制设置 {labelText}: {value}, TextField值: {textField.value}");
                        }
                    }
                }
            }
        }
        
        // 再次验证所有TextField的值
        yield return new WaitForSeconds(0.1f);
        foreach (var textField in textFields)
        {
            if (textField != null)
            {
                Debug.Log($"TextField最终值: {textField.value}");
            }
        }
    }
    
    void CreateSimplifiedParameterRow(string labelText, float defaultValue, Action<float> onValueChanged, VisualElement container)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 8;
        
        // 标签
        var label = new Label(labelText);
        label.style.fontSize = 11;
        label.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        label.style.minWidth = 120;
        uiManager?.ApplyFont(label);
        row.Add(label);
        
        // 数值输入框 - 参考其他UI的实现方式
        var textField = new TextField();
        
        // 确保默认值不为0或无效值
        float displayValue = defaultValue;
        if (displayValue <= 0f)
        {
            // 如果默认值无效，设置合理的默认值
            if (labelText.Contains("危险距离"))
                displayValue = 5f;  // 降低到5米
            else if (labelText.Contains("警告距离"))
                displayValue = 15f; // 降低到15米
            else if (labelText.Contains("安全距离"))
                displayValue = 30f; // 降低到30米
            else if (labelText.Contains("基础生长率"))
                displayValue = 0.1f;
        }
        
        // 直接设置初始值 - 参考其他UI的实现
        textField.value = displayValue.ToString("F1");
        
        // 设置样式 - 参考其他UI的样式设置
        textField.style.width = 80;
        textField.style.height = 25;
        textField.style.fontSize = 12;
        textField.style.color = Color.black;
        textField.style.backgroundColor = Color.white;
        textField.style.borderTopWidth = 1;
        textField.style.borderBottomWidth = 1;
        textField.style.borderLeftWidth = 1;
        textField.style.borderRightWidth = 1;
        textField.style.borderTopColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        textField.style.borderBottomColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        textField.style.borderLeftColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        textField.style.borderRightColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        textField.style.paddingTop = 4;
        textField.style.paddingBottom = 4;
        textField.style.paddingLeft = 6;
        textField.style.paddingRight = 6;
        textField.style.borderTopLeftRadius = 3;
        textField.style.borderTopRightRadius = 3;
        textField.style.borderBottomLeftRadius = 3;
        textField.style.borderBottomRightRadius = 3;
        
        // 应用字体
        uiManager?.ApplyFont(textField);
        
        // 添加调试信息
        Debug.Log($"创建参数行: {labelText}, 默认值: {defaultValue}, 显示值: {displayValue}, TextField值: {textField.value}");
        
        // 注册值改变回调
        textField.RegisterValueChangedCallback(evt => {
            Debug.Log($"参数值改变: {labelText} 从 {evt.previousValue} 到 {evt.newValue}");
            if (float.TryParse(evt.newValue, out float newValue))
            {
                onValueChanged(newValue);
                Debug.Log($"参数已更新: {labelText} = {newValue}");
            }
            else
            {
                Debug.LogWarning($"无法解析数值: {evt.newValue}");
            }
        });
        
        // 添加到行容器
        row.Add(textField);
        container.Add(row);
        
        // 验证TextField是否正确设置
        Debug.Log($"参数行创建完成: {labelText}, TextField最终值: {textField.value}");
    }
    
    void CreateControlButtons()
    {
        // 创建按钮容器
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.justifyContent = Justify.SpaceAround;
        buttonContainer.style.marginBottom = 15;
        
        // 开始监测按钮
        startMonitoringButton = new Button(() => StartMonitoring());
        startMonitoringButton.text = "开始监测";
        startMonitoringButton.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        startMonitoringButton.style.color = Color.white;
        startMonitoringButton.style.fontSize = 12;
        startMonitoringButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        startMonitoringButton.style.paddingTop = 8;
        startMonitoringButton.style.paddingBottom = 8;
        startMonitoringButton.style.paddingLeft = 12;
        startMonitoringButton.style.paddingRight = 12;
        startMonitoringButton.style.borderTopLeftRadius = 6;
        startMonitoringButton.style.borderTopRightRadius = 6;
        startMonitoringButton.style.borderBottomLeftRadius = 6;
        startMonitoringButton.style.borderBottomRightRadius = 6;
        startMonitoringButton.style.flexGrow = 1;
        startMonitoringButton.style.marginRight = 5;
        uiManager?.ApplyFont(startMonitoringButton);
        buttonContainer.Add(startMonitoringButton);
        
        // 清除标记按钮
        clearMarkersButton = new Button(() => ClearAllMarkers());
        clearMarkersButton.text = "清除标记";
        clearMarkersButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        clearMarkersButton.style.color = Color.white;
        clearMarkersButton.style.fontSize = 12;
        clearMarkersButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        clearMarkersButton.style.paddingTop = 8;
        clearMarkersButton.style.paddingBottom = 8;
        clearMarkersButton.style.paddingLeft = 12;
        clearMarkersButton.style.paddingRight = 12;
        clearMarkersButton.style.borderTopLeftRadius = 6;
        clearMarkersButton.style.borderTopRightRadius = 6;
        clearMarkersButton.style.borderBottomLeftRadius = 6;
        clearMarkersButton.style.borderBottomRightRadius = 6;
        clearMarkersButton.style.flexGrow = 1;
        clearMarkersButton.style.marginLeft = 5;
        uiManager?.ApplyFont(clearMarkersButton);
        buttonContainer.Add(clearMarkersButton);
        
        // 测试危险检测按钮
        var testButton = new Button(() => TestDangerDetection());
        testButton.text = "测试危险检测";
        testButton.style.backgroundColor = new Color(0.8f, 0.4f, 0.2f, 1f);
        testButton.style.color = Color.white;
        testButton.style.fontSize = 11;
        testButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        testButton.style.paddingTop = 6;
        testButton.style.paddingBottom = 6;
        testButton.style.paddingLeft = 10;
        testButton.style.paddingRight = 10;
        testButton.style.borderTopLeftRadius = 4;
        testButton.style.borderTopRightRadius = 4;
        testButton.style.borderBottomLeftRadius = 4;
        testButton.style.borderBottomRightRadius = 4;
        testButton.style.marginTop = 5;
        uiManager?.ApplyFont(testButton);
        buttonContainer.Add(testButton);
        
        controlSection.Add(buttonContainer);
        
        // 创建状态显示
        CreateStatusDisplay();
    }
    
    /// <summary>
    /// 测试危险检测功能
    /// </summary>
    void TestDangerDetection()
    {
        Debug.Log("=== 开始测试危险检测功能 ===");
        
        if (treeDangerMonitor == null)
        {
            Debug.LogError("TreeDangerMonitor未找到");
            return;
        }
        
        // 强制执行监测
        treeDangerMonitor.ManualMonitoring();
        
        // 获取监测结果
        var allDangerInfo = treeDangerMonitor.GetAllDangerInfo();
        Debug.Log($"监测完成，发现 {allDangerInfo.Count} 个危险情况");
        
        if (allDangerInfo.Count == 0)
        {
            Debug.LogWarning("⚠️ 没有检测到任何危险情况！");
            Debug.LogWarning("可能的原因：");
            Debug.LogWarning("1. 危险距离阈值设置过高");
            Debug.LogWarning("2. 场景中没有树木或电力线");
            Debug.LogWarning("3. 距离计算有问题");
            
            // 尝试调整参数
            Debug.Log("尝试调整危险判定参数...");
            treeDangerMonitor.criticalDistance = 2f;  // 进一步降低到2米
            treeDangerMonitor.warningDistance = 8f;   // 降低到8米
            treeDangerMonitor.safeDistance = 20f;     // 降低到20米
            
            // 再次监测
            treeDangerMonitor.ManualMonitoring();
            var retryDangerInfo = treeDangerMonitor.GetAllDangerInfo();
            Debug.Log($"调整参数后，发现 {retryDangerInfo.Count} 个危险情况");
        }
        else
        {
            foreach (var dangerInfo in allDangerInfo)
            {
                Debug.Log($"危险树木: {dangerInfo.tree.name}, 距离: {dangerInfo.currentDistance:F2}m, 等级: {dangerInfo.dangerLevel}");
            }
        }
        
        // 刷新显示
        UpdateDisplay();
        UpdateStatus($"测试完成，发现 {allDangerInfo.Count} 个危险情况");
    }
    
    void CreateStatusDisplay()
    {
        // 创建状态显示容器
        var statusContainer = new VisualElement();
        statusContainer.style.backgroundColor = new Color(1f, 1f, 0.8f, 1f);
        statusContainer.style.paddingTop = 8;
        statusContainer.style.paddingBottom = 8;
        statusContainer.style.paddingLeft = 10;
        statusContainer.style.paddingRight = 10;
        statusContainer.style.borderTopLeftRadius = 6;
        statusContainer.style.borderTopRightRadius = 6;
        statusContainer.style.borderBottomLeftRadius = 6;
        statusContainer.style.borderBottomRightRadius = 6;
        statusContainer.style.borderLeftWidth = 1;
        statusContainer.style.borderLeftColor = new Color(0.8f, 0.8f, 0.2f, 1f);
        
        // 状态标签
        statusLabel = new Label("系统就绪, 等待监测数据...");
        statusLabel.style.fontSize = 11;
        statusLabel.style.color = new Color(0.6f, 0.6f, 0.2f, 1f);
        statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(statusLabel);
        statusContainer.Add(statusLabel);
        
        controlSection.Add(statusContainer);
    }
    
    void CreateStatisticsSection()
    {
        statisticsSection = new VisualElement();
        statisticsSection.style.backgroundColor = new Color(1f, 0.95f, 0.9f, 1f);
        statisticsSection.style.marginBottom = 10;
        statisticsSection.style.paddingTop = 12;
        statisticsSection.style.paddingBottom = 12;
        statisticsSection.style.paddingLeft = 10;
        statisticsSection.style.paddingRight = 10;
        statisticsSection.style.borderTopLeftRadius = 8;
        statisticsSection.style.borderTopRightRadius = 8;
        statisticsSection.style.borderBottomLeftRadius = 8;
        statisticsSection.style.borderBottomRightRadius = 8;
        statisticsSection.style.borderLeftWidth = 2;
        statisticsSection.style.borderRightWidth = 2;
        statisticsSection.style.borderTopWidth = 2;
        statisticsSection.style.borderBottomWidth = 2;
        statisticsSection.style.borderLeftColor = new Color(1f, 0.6f, 0.2f, 1f);
        statisticsSection.style.borderRightColor = new Color(1f, 0.6f, 0.2f, 1f);
        statisticsSection.style.borderTopColor = new Color(1f, 0.6f, 0.2f, 1f);
        statisticsSection.style.borderBottomColor = new Color(1f, 0.6f, 0.2f, 1f);
        statisticsSection.style.flexShrink = 0;
        statisticsSection.style.width = Length.Percent(100);
        statisticsSection.style.maxWidth = Length.Percent(100);
        
        // 标题
        var titleLabel = new Label("监测统计");
        titleLabel.style.color = new Color(0.8f, 0.4f, 0.1f, 1f);
        titleLabel.style.fontSize = 14;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 10;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(titleLabel);
        statisticsSection.Add(titleLabel);
        
        // 简化的统计信息显示
        CreateSimplifiedStatisticsDisplay();
        
        treeDangerPanel.Add(statisticsSection);
    }
    
    void CreateSimplifiedStatisticsDisplay()
    {
        // 创建统计信息容器
        var statsContainer = new VisualElement();
        statsContainer.style.flexDirection = FlexDirection.Column;
        statsContainer.style.marginBottom = 10;
        
        // 获取监测统计信息
        string statsText = "暂无监测数据";
        string hintText = "点击'开始监测'按钮开始监测";
        
        if (treeDangerMonitor != null)
        {
            int treeCount = treeDangerMonitor.GetTreeCount();
            int dangerousTrees = treeDangerMonitor.GetAllDangerInfo().Count;
            
            if (treeCount > 0)
            {
                statsText = $"监测统计(总计: {treeCount}棵)";
                hintText = dangerousTrees > 0 ? 
                    $"发现{dangerousTrees}棵危险树木" : 
                    "所有树木都处于安全状态";
            }
        }
        
        // 统计信息标签
        var statsLabel = new Label(statsText);
        statsLabel.style.fontSize = 12;
        statsLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        statsLabel.style.marginBottom = 5;
        uiManager?.ApplyFont(statsLabel);
        statsContainer.Add(statsLabel);
        
        // 提示信息标签
        var hintLabel = new Label(hintText);
        hintLabel.style.fontSize = 12;
        hintLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        hintLabel.style.marginBottom = 5;
        uiManager?.ApplyFont(hintLabel);
        statsContainer.Add(hintLabel);
        
        statisticsSection.Add(statsContainer);
    }
    
    void CreateAllTreesDistanceSection()
    {
        var distanceSection = new VisualElement();
        distanceSection.style.backgroundColor = new Color(0.9f, 0.95f, 1f, 1f);
        distanceSection.style.marginBottom = 10;
        distanceSection.style.paddingTop = 12;
        distanceSection.style.paddingBottom = 12;
        distanceSection.style.paddingLeft = 10;
        distanceSection.style.paddingRight = 10;
        distanceSection.style.borderTopLeftRadius = 8;
        distanceSection.style.borderTopRightRadius = 8;
        distanceSection.style.borderBottomLeftRadius = 8;
        distanceSection.style.borderBottomRightRadius = 8;
        distanceSection.style.borderLeftWidth = 2;
        distanceSection.style.borderRightWidth = 2;
        distanceSection.style.borderTopWidth = 2;
        distanceSection.style.borderBottomWidth = 2;
        distanceSection.style.borderLeftColor = new Color(0.2f, 0.6f, 1f, 1f);
        distanceSection.style.borderRightColor = new Color(0.2f, 0.6f, 1f, 1f);
        distanceSection.style.borderTopColor = new Color(0.2f, 0.6f, 1f, 1f);
        distanceSection.style.borderBottomColor = new Color(0.2f, 0.6f, 1f, 1f);
        distanceSection.style.flexShrink = 0;
        distanceSection.style.width = Length.Percent(100);
        distanceSection.style.maxWidth = Length.Percent(100);

        var titleLabel = new Label("所有树木与电力线距离");
        titleLabel.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
        titleLabel.style.fontSize = 14;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 10;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(titleLabel);
        distanceSection.Add(titleLabel);

        // 创建距离信息容器
        var distanceContainer = new VisualElement();
        distanceContainer.style.flexDirection = FlexDirection.Column;
        distanceContainer.style.marginBottom = 10;
        
        // 添加刷新按钮
        var refreshButton = new Button(() => RefreshDistanceDisplay(distanceContainer));
        refreshButton.text = "刷新距离信息";
        refreshButton.style.backgroundColor = new Color(0.2f, 0.7f, 0.2f, 1f);
        refreshButton.style.color = Color.white;
        refreshButton.style.fontSize = 11;
        refreshButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        refreshButton.style.paddingTop = 6;
        refreshButton.style.paddingBottom = 6;
        refreshButton.style.paddingLeft = 10;
        refreshButton.style.paddingRight = 10;
        refreshButton.style.borderTopLeftRadius = 4;
        refreshButton.style.borderTopRightRadius = 4;
        refreshButton.style.borderBottomLeftRadius = 4;
        refreshButton.style.borderBottomRightRadius = 4;
        refreshButton.style.marginBottom = 10;
        refreshButton.style.alignSelf = Align.Center;
        uiManager?.ApplyFont(refreshButton);
        distanceContainer.Add(refreshButton);

        // 显示距离信息
        DisplayAllTreesDistance(distanceContainer);
        
        distanceSection.Add(distanceContainer);
        treeDangerPanel.Add(distanceSection);
    }
    
    /// <summary>
    /// 显示所有树木的距离信息
    /// </summary>
    void DisplayAllTreesDistance(VisualElement container)
    {
        // 清除旧内容
        var existingContent = container.Q("distanceContent");
        if (existingContent != null)
        {
            existingContent.RemoveFromHierarchy();
        }
        
        var distanceContent = new VisualElement();
        distanceContent.name = "distanceContent";
        distanceContent.style.flexDirection = FlexDirection.Column;
        
        if (treeDangerMonitor != null)
        {
            // 获取所有树木信息
            var allDangerInfo = treeDangerMonitor.GetAllDangerInfo();
            
            if (allDangerInfo.Count == 0)
            {
                // 尝试获取场景中的树木信息
                var sceneTrees = FindObjectsOfType<GameObject>().Where(obj => 
                    obj.name.ToLower().Contains("tree") || 
                    obj.name.ToLower().Contains("树") ||
                    obj.name.ToLower().Contains("plant") ||
                    obj.name.ToLower().Contains("vegetation")).ToArray();
                
                if (sceneTrees.Length > 0)
                {
                    // 显示场景中的树木基本信息
                    var infoLabel = new Label($"场景中找到 {sceneTrees.Length} 棵树木，但尚未执行监测");
                    infoLabel.style.fontSize = 12;
                    infoLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                    infoLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                    infoLabel.style.marginBottom = 10;
                    uiManager?.ApplyFont(infoLabel);
                    distanceContent.Add(infoLabel);
                    
                    // 创建表头
                    var headerRow = new VisualElement();
                    headerRow.style.flexDirection = FlexDirection.Row;
                    headerRow.style.justifyContent = Justify.SpaceBetween;
                    headerRow.style.alignItems = Align.Center;
                    headerRow.style.marginBottom = 8;
                    headerRow.style.backgroundColor = new Color(0.8f, 0.9f, 1f, 1f);
                    headerRow.style.paddingTop = 6;
                    headerRow.style.paddingBottom = 6;
                    headerRow.style.paddingLeft = 8;
                    headerRow.style.paddingRight = 8;
                    headerRow.style.borderTopLeftRadius = 4;
                    headerRow.style.borderTopRightRadius = 4;
                    headerRow.style.borderBottomLeftRadius = 4;
                    headerRow.style.borderBottomRightRadius = 4;

                    var treeNameLabel = new Label("树木名称");
                    treeNameLabel.style.fontSize = 12;
                    treeNameLabel.style.color = new Color(0.2f, 0.4f, 0.8f, 1f);
                    treeNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    treeNameLabel.style.minWidth = 200;
                    uiManager?.ApplyFont(treeNameLabel);
                    headerRow.Add(treeNameLabel);

                    var positionLabel = new Label("位置坐标");
                    positionLabel.style.fontSize = 12;
                    positionLabel.style.color = new Color(0.2f, 0.4f, 0.8f, 1f);
                    positionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    positionLabel.style.minWidth = 150;
                    uiManager?.ApplyFont(positionLabel);
                    headerRow.Add(positionLabel);
                    
                    var statusLabel = new Label("状态");
                    statusLabel.style.fontSize = 12;
                    statusLabel.style.color = new Color(0.2f, 0.4f, 0.8f, 1f);
                    statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    statusLabel.style.minWidth = 80;
                    uiManager?.ApplyFont(statusLabel);
                    headerRow.Add(statusLabel);

                    distanceContent.Add(headerRow);

                    // 显示前20棵树木的基本信息
                    int displayCount = Mathf.Min(sceneTrees.Length, 20);
                    for (int i = 0; i < displayCount; i++)
                    {
                        var tree = sceneTrees[i];
                        var row = new VisualElement();
                        row.style.flexDirection = FlexDirection.Row;
                        row.style.justifyContent = Justify.SpaceBetween;
                        row.style.alignItems = Align.Center;
                        row.style.marginBottom = 4;
                        row.style.paddingTop = 4;
                        row.style.paddingBottom = 4;
                        row.style.paddingLeft = 8;
                        row.style.paddingRight = 8;
                        row.style.borderTopLeftRadius = 3;
                        row.style.borderTopRightRadius = 3;
                        row.style.borderBottomLeftRadius = 3;
                        row.style.borderBottomRightRadius = 3;
                        row.style.backgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);

                        var treeName = new Label(tree.name);
                        treeName.style.fontSize = 11;
                        treeName.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                        treeName.style.minWidth = 200;
                        uiManager?.ApplyFont(treeName);
                        row.Add(treeName);

                        var position = new Label($"({tree.transform.position.x:F1}, {tree.transform.position.y:F1}, {tree.transform.position.z:F1})");
                        position.style.fontSize = 11;
                        position.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                        position.style.minWidth = 150;
                        uiManager?.ApplyFont(position);
                        row.Add(position);
                        
                        var status = new Label("待监测");
                        status.style.fontSize = 11;
                        status.style.color = new Color(0.8f, 0.6f, 0.2f, 1f);
                        status.style.unityFontStyleAndWeight = FontStyle.Bold;
                        status.style.minWidth = 80;
                        uiManager?.ApplyFont(status);
                        row.Add(status);

                        distanceContent.Add(row);
                    }
                    
                    if (sceneTrees.Length > 20)
                    {
                        var moreLabel = new Label($"... 还有 {sceneTrees.Length - 20} 棵树木未显示");
                        moreLabel.style.fontSize = 11;
                        moreLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                        moreLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                        moreLabel.style.marginTop = 8;
                        uiManager?.ApplyFont(moreLabel);
                        distanceContent.Add(moreLabel);
                    }
                    
                    // 添加提示信息
                    var hintLabel = new Label("点击'开始监测'按钮获取详细距离信息");
                    hintLabel.style.fontSize = 11;
                    hintLabel.style.color = new Color(0.4f, 0.6f, 0.8f, 1f);
                    hintLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                    hintLabel.style.marginTop = 10;
                    hintLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    uiManager?.ApplyFont(hintLabel);
                    distanceContent.Add(hintLabel);
                }
                else
                {
                    var noDataLabel = new Label("暂无距离数据，请先执行监测");
                    noDataLabel.style.fontSize = 12;
                    noDataLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                    noDataLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                    uiManager?.ApplyFont(noDataLabel);
                    distanceContent.Add(noDataLabel);
                }
            }
            else
            {
                // 显示监测后的详细距离信息
                // 创建表头
                var headerRow = new VisualElement();
                headerRow.style.flexDirection = FlexDirection.Row;
                headerRow.style.justifyContent = Justify.SpaceBetween;
                headerRow.style.alignItems = Align.Center;
                headerRow.style.marginBottom = 8;
                headerRow.style.backgroundColor = new Color(0.8f, 0.9f, 1f, 1f);
                headerRow.style.paddingTop = 6;
                headerRow.style.paddingBottom = 6;
                headerRow.style.paddingLeft = 8;
                headerRow.style.paddingRight = 8;
                headerRow.style.borderTopLeftRadius = 4;
                headerRow.style.borderTopRightRadius = 4;
                headerRow.style.borderBottomLeftRadius = 4;
                headerRow.style.borderBottomRightRadius = 4;

                var treeNameLabel = new Label("树木名称");
                treeNameLabel.style.fontSize = 12;
                treeNameLabel.style.color = new Color(0.2f, 0.4f, 0.8f, 1f);
                treeNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                treeNameLabel.style.minWidth = 150;
                uiManager?.ApplyFont(treeNameLabel);
                headerRow.Add(treeNameLabel);

                var distanceLabel = new Label("距离 (m)");
                distanceLabel.style.fontSize = 12;
                distanceLabel.style.color = new Color(0.2f, 0.4f, 0.8f, 1f);
                distanceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                distanceLabel.style.minWidth = 80;
                uiManager?.ApplyFont(distanceLabel);
                headerRow.Add(distanceLabel);
                
                var dangerLabel = new Label("危险等级");
                dangerLabel.style.fontSize = 12;
                dangerLabel.style.color = new Color(0.2f, 0.4f, 0.8f, 1f);
                dangerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                dangerLabel.style.minWidth = 80;
                uiManager?.ApplyFont(dangerLabel);
                headerRow.Add(dangerLabel);
                
                var heightLabel = new Label("树木高度");
                heightLabel.style.fontSize = 12;
                heightLabel.style.color = new Color(0.2f, 0.4f, 0.8f, 1f);
                heightLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                heightLabel.style.minWidth = 80;
                uiManager?.ApplyFont(heightLabel);
                headerRow.Add(heightLabel);

                distanceContent.Add(headerRow);

                // 按距离排序
                var sortedDangerInfo = allDangerInfo.OrderBy(info => info.currentDistance).ToList();
                
                // 显示每个树木的距离信息
                foreach (var dangerInfo in sortedDangerInfo)
                {
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.justifyContent = Justify.SpaceBetween;
                    row.style.alignItems = Align.Center;
                    row.style.marginBottom = 4;
                    row.style.paddingTop = 4;
                    row.style.paddingBottom = 4;
                    row.style.paddingLeft = 8;
                    row.style.paddingRight = 8;
                    row.style.borderTopLeftRadius = 3;
                    row.style.borderTopRightRadius = 3;
                    row.style.borderBottomLeftRadius = 3;
                    row.style.borderBottomRightRadius = 3;
                    
                    // 根据危险等级设置背景色
                    Color backgroundColor = GetDangerLevelColor(dangerInfo.dangerLevel);
                    backgroundColor.a = 0.1f; // 降低透明度
                    row.style.backgroundColor = backgroundColor;

                    var treeName = new Label(dangerInfo.tree.name);
                    treeName.style.fontSize = 11;
                    treeName.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                    treeName.style.minWidth = 150;
                    uiManager?.ApplyFont(treeName);
                    row.Add(treeName);

                    var distanceValue = new Label($"{dangerInfo.currentDistance:F1}");
                    distanceValue.style.fontSize = 11;
                    distanceValue.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                    distanceValue.style.minWidth = 80;
                    uiManager?.ApplyFont(distanceValue);
                    row.Add(distanceValue);
                    
                    var dangerLevel = new Label(GetDangerLevelText(dangerInfo.dangerLevel));
                    dangerLevel.style.fontSize = 11;
                    dangerLevel.style.color = GetDangerLevelColor(dangerInfo.dangerLevel);
                    dangerLevel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    dangerLevel.style.minWidth = 80;
                    uiManager?.ApplyFont(dangerLevel);
                    row.Add(dangerLevel);
                    
                    var treeHeight = new Label($"{dangerInfo.treeHeight:F1}m");
                    treeHeight.style.fontSize = 11;
                    treeHeight.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                    treeHeight.style.minWidth = 80;
                    uiManager?.ApplyFont(treeHeight);
                    row.Add(treeHeight);

                    distanceContent.Add(row);
                }
                
                // 添加统计信息
                var statsLabel = new Label($"总计: {allDangerInfo.Count} 棵树木");
                statsLabel.style.fontSize = 11;
                statsLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                statsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                statsLabel.style.marginTop = 8;
                uiManager?.ApplyFont(statsLabel);
                distanceContent.Add(statsLabel);
                
                // 添加距离范围信息
                if (allDangerInfo.Count > 0)
                {
                    var minDistance = allDangerInfo.Min(info => info.currentDistance);
                    var maxDistance = allDangerInfo.Max(info => info.currentDistance);
                    var avgDistance = allDangerInfo.Average(info => info.currentDistance);
                    
                    var rangeLabel = new Label($"距离范围: {minDistance:F1}m - {maxDistance:F1}m (平均: {avgDistance:F1}m)");
                    rangeLabel.style.fontSize = 11;
                    rangeLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                    rangeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                    rangeLabel.style.marginTop = 4;
                    uiManager?.ApplyFont(rangeLabel);
                    distanceContent.Add(rangeLabel);
                }
            }
        }
        else
        {
            var errorLabel = new Label("TreeDangerMonitor未找到");
            errorLabel.style.fontSize = 12;
            errorLabel.style.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            errorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            uiManager?.ApplyFont(errorLabel);
            distanceContent.Add(errorLabel);
        }
        
        container.Add(distanceContent);
    }
    
    /// <summary>
    /// 刷新距离显示
    /// </summary>
    void RefreshDistanceDisplay(VisualElement container)
    {
        DisplayAllTreesDistance(container);
        UpdateStatus("距离信息已刷新");
    }

    void CreateTreeListSection()
    {
        treeListSection = new VisualElement();
        treeListSection.style.backgroundColor = new Color(0.9f, 0.95f, 1f, 1f);
        treeListSection.style.marginBottom = 10;
        treeListSection.style.paddingTop = 12;
        treeListSection.style.paddingBottom = 12;
        treeListSection.style.paddingLeft = 10;
        treeListSection.style.paddingRight = 10;
        treeListSection.style.borderTopLeftRadius = 8;
        treeListSection.style.borderTopRightRadius = 8;
        treeListSection.style.borderBottomLeftRadius = 8;
        treeListSection.style.borderBottomRightRadius = 8;
        treeListSection.style.borderLeftWidth = 2;
        treeListSection.style.borderRightWidth = 2;
        treeListSection.style.borderTopWidth = 2;
        treeListSection.style.borderBottomWidth = 2;
        treeListSection.style.borderLeftColor = new Color(0.2f, 0.6f, 1f, 1f);
        treeListSection.style.borderRightColor = new Color(0.2f, 0.6f, 1f, 1f);
        treeListSection.style.borderTopColor = new Color(0.2f, 0.6f, 1f, 1f);
        treeListSection.style.borderBottomColor = new Color(0.2f, 0.6f, 1f, 1f);
        treeListSection.style.flexShrink = 0;
        treeListSection.style.width = Length.Percent(100);
        treeListSection.style.maxWidth = Length.Percent(100);
        
        // 标题
        var titleLabel = new Label("危险树木列表");
        titleLabel.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
        titleLabel.style.fontSize = 14;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 10;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(titleLabel);
        treeListSection.Add(titleLabel);
        
        // 简化的树木列表容器
        CreateSimplifiedTreeListContainer();
        
        treeDangerPanel.Add(treeListSection);
    }
    
    void CreateSimplifiedTreeListContainer()
    {
        // 创建树木列表容器
        treeListContainer = new VisualElement();
        treeListContainer.style.flexDirection = FlexDirection.Column;
        treeListContainer.style.marginBottom = 10;
        
        // 检查是否有危险树木
        if (treeDangerMonitor != null && treeDangerMonitor.GetAllDangerInfo().Count > 0)
        {
            // 显示危险树木列表
            DisplayDangerousTreesList();
        }
        else
        {
            // 显示无危险信息
            CreateNoDangerInfoDisplay();
        }
        
        treeListSection.Add(treeListContainer);
    }
    
    void CreateNoDangerInfoDisplay()
    {
        // 创建简化的系统状态信息
        var statusPanel = new VisualElement();
        statusPanel.style.backgroundColor = new Color(0.95f, 0.98f, 0.95f, 1f);
        statusPanel.style.marginBottom = 10;
        statusPanel.style.paddingTop = 10;
        statusPanel.style.paddingBottom = 10;
        statusPanel.style.paddingLeft = 10;
        statusPanel.style.paddingRight = 10;
        statusPanel.style.borderTopLeftRadius = 6;
        statusPanel.style.borderTopRightRadius = 6;
        statusPanel.style.borderBottomLeftRadius = 6;
        statusPanel.style.borderBottomRightRadius = 6;
        statusPanel.style.borderLeftWidth = 1;
        statusPanel.style.borderLeftColor = new Color(0.2f, 0.7f, 0.2f, 1f);
        
        // 简化的状态信息
        string statusText = "系统状态良好";
        string detailText = "当前场景中所有树木都处于安全状态";
        
        if (treeDangerMonitor != null)
        {
            int treeCount = treeDangerMonitor.GetTreeCount();
            int dangerousTrees = treeDangerMonitor.GetAllDangerInfo().Count;
            
            if (treeCount == 0)
            {
                statusText = "未找到树木";
                detailText = "场景中没有检测到树木对象";
            }
            else if (dangerousTrees > 0)
            {
                statusText = $"发现{dangerousTrees}棵危险树木";
                detailText = "需要关注这些危险树木";
            }
            else
            {
                statusText = $"已找到{treeCount}棵树木";
                detailText = "所有树木都处于安全状态";
            }
        }
        
        // 状态标签
        var statusLabel = new Label(statusText);
        statusLabel.style.fontSize = 12;
        statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        statusLabel.style.color = new Color(0.2f, 0.7f, 0.2f, 1f);
        statusLabel.style.marginBottom = 5;
        uiManager?.ApplyFont(statusLabel);
        statusPanel.Add(statusLabel);
        
        // 详细信息标签
        var detailLabel = new Label(detailText);
        detailLabel.style.fontSize = 11;
        detailLabel.style.color = new Color(0.4f, 0.6f, 0.4f, 1f);
        uiManager?.ApplyFont(detailLabel);
        statusPanel.Add(detailLabel);
        
        treeListContainer.Add(statusPanel);
    }
    
    // 控制方法
    void StartMonitoring()
    {
        if (treeDangerMonitor == null)
        {
            UpdateStatus("TreeDangerMonitor未找到");
            return;
        }
        
        // 同步参数
        SyncMonitoringParameters();
        
        // 开始监测
        treeDangerMonitor.ManualMonitoring();
        isMonitoring = true;
        
        // 更新按钮状态
        if (startMonitoringButton != null)
        {
            startMonitoringButton.text = "停止监测";
            startMonitoringButton.style.backgroundColor = new Color(0.8f, 0.4f, 0.2f, 1f);
        }
        
        UpdateStatus("监测已启动");
        UpdateDisplay();
    }
    
    /// <summary>
    /// 延迟刷新显示的协程
    /// </summary>
    IEnumerator DelayedRefreshDisplay()
    {
        yield return new WaitForSeconds(0.5f);
        RefreshDisplay();
    }
    
    void ClearAllMarkers()
    {
        if (treeDangerMonitor == null)
        {
            UpdateStatus("TreeDangerMonitor未找到");
            return;
        }
        
        // 清除所有危险标记
        treeDangerMonitor.ClearAllDangerMarkers();
        
        UpdateStatus("已清除所有危险标记");
        UpdateDisplay();
    }
    
    // 更新方法
    void UpdateStatus(string message)
    {
        if (statusLabel != null)
        {
            statusLabel.text = message;
        }
        
        Debug.Log($"[TreeDangerController] {message}");
    }
    
    void UpdateStatistics()
    {
        if (treeDangerMonitor == null) return;
        
        // 获取基本统计信息
        int totalTrees = treeDangerMonitor.GetTreeCount();
        int dangerousTrees = treeDangerMonitor.GetAllDangerInfo().Count;
        
        // 更新统计显示
        if (statisticsSection != null)
        {
            // 清除旧的统计信息
            statisticsSection.Clear();
            
            // 重新创建标题
            var titleLabel = new Label("监测统计");
            titleLabel.style.color = new Color(0.8f, 0.4f, 0.1f, 1f);
            titleLabel.style.fontSize = 14;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 10;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            uiManager?.ApplyFont(titleLabel);
            statisticsSection.Add(titleLabel);
            
            // 创建简化的统计信息
            var statsContainer = new VisualElement();
            statsContainer.style.flexDirection = FlexDirection.Column;
            statsContainer.style.marginBottom = 10;
            
            string statsText = $"监测统计(总计: {totalTrees}棵)";
            string hintText = dangerousTrees > 0 ? 
                $"发现{dangerousTrees}棵危险树木" : 
                "所有树木都处于安全状态";
            
            // 统计信息标签
            var statsLabel = new Label(statsText);
            statsLabel.style.fontSize = 12;
            statsLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            statsLabel.style.marginBottom = 5;
            uiManager?.ApplyFont(statsLabel);
            statsContainer.Add(statsLabel);
            
            // 提示信息标签
            var hintLabel = new Label(hintText);
            hintLabel.style.fontSize = 12;
            hintLabel.style.color = dangerousTrees > 0 ? 
                new Color(0.8f, 0.4f, 0.2f, 1f) : 
                new Color(0.8f, 0.8f, 0.8f, 1f);
            hintLabel.style.marginBottom = 5;
            uiManager?.ApplyFont(hintLabel);
            statsContainer.Add(hintLabel);
            
            statisticsSection.Add(statsContainer);
        }
    }
    
    void UpdateTreeList()
    {
        if (treeDangerMonitor == null || treeListContainer == null) return;
        
        // 清除旧的列表内容
        treeListContainer.Clear();
        
        var allDangerInfo = treeDangerMonitor.GetAllDangerInfo();
        
        if (allDangerInfo.Count == 0)
        {
            // 没有危险树木，显示安全信息
            CreateNoDangerInfoDisplay();
        }
        else
        {
            // 有危险树木，显示列表
            DisplayDangerousTreesList();
        }
    }
    
    void DisplayDangerousTreesList()
    {
        if (treeDangerMonitor == null) return;
        
        var allDangerInfo = treeDangerMonitor.GetAllDangerInfo();
        
        foreach (var dangerInfo in allDangerInfo)
        {
            CreateSimplifiedTreeListItem(dangerInfo);
        }
    }
    
    void CreateSimplifiedTreeListItem(TreeDangerMonitor.TreeDangerInfo dangerInfo)
    {
        // 创建简化的树木列表项
        var itemContainer = new VisualElement();
        itemContainer.style.backgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        itemContainer.style.marginBottom = 8;
        itemContainer.style.paddingTop = 8;
        itemContainer.style.paddingBottom = 8;
        itemContainer.style.paddingLeft = 8;
        itemContainer.style.paddingRight = 8;
        itemContainer.style.borderTopLeftRadius = 6;
        itemContainer.style.borderTopRightRadius = 6;
        itemContainer.style.borderBottomLeftRadius = 6;
        itemContainer.style.borderBottomRightRadius = 6;
        itemContainer.style.borderLeftWidth = 2;
        itemContainer.style.borderLeftColor = GetDangerLevelColor(dangerInfo.dangerLevel);
        
        // 危险等级标签
        var levelLabel = new Label($"危险等级: {GetDangerLevelText(dangerInfo.dangerLevel)}");
        levelLabel.style.fontSize = 12;
        levelLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        levelLabel.style.color = GetDangerLevelColor(dangerInfo.dangerLevel);
        levelLabel.style.marginBottom = 5;
        uiManager?.ApplyFont(levelLabel);
        itemContainer.Add(levelLabel);
        
        // 距离信息
        var distanceLabel = new Label($"当前距离: {dangerInfo.currentDistance:F1}m");
        distanceLabel.style.fontSize = 11;
        distanceLabel.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        distanceLabel.style.marginBottom = 3;
        uiManager?.ApplyFont(distanceLabel);
        itemContainer.Add(distanceLabel);
        
        // 树木高度
        var heightLabel = new Label($"树木高度: {dangerInfo.treeHeight:F1}m");
        heightLabel.style.fontSize = 11;
        heightLabel.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        heightLabel.style.marginBottom = 3;
        uiManager?.ApplyFont(heightLabel);
        itemContainer.Add(heightLabel);
        
        // 新增：高度比例信息（如果电塔高度信息可用）
        if (dangerInfo.powerline != null)
        {
            // 尝试获取电塔高度信息
            float powerlineHeight = 20f; // 默认值
            try
            {
                var powerlineInfo = dangerInfo.powerline.GetDetailedInfo();
                if (powerlineInfo != null && powerlineInfo.basicInfo != null && powerlineInfo.basicInfo.points != null && powerlineInfo.basicInfo.points.Count > 0)
                {
                    // 使用LINQ计算平均高度
                    powerlineHeight = powerlineInfo.basicInfo.points.Select(p => p.y).Average();
                }
            }
            catch
            {
                // 如果获取失败，使用默认值
            }
            
            float heightRatio = dangerInfo.treeHeight / powerlineHeight;
            if (heightRatio >= 0.5f)
            {
                var ratioLabel = new Label($"⚠️ 高度比例: {heightRatio * 100:F0}% (电塔高度: {powerlineHeight:F1}m)");
                ratioLabel.style.fontSize = 11;
                ratioLabel.style.color = new Color(0.8f, 0.4f, 0.2f, 1f);
                ratioLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                ratioLabel.style.marginBottom = 5;
                uiManager?.ApplyFont(ratioLabel);
                itemContainer.Add(ratioLabel);
            }
        }
        
        // 跳转按钮
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.justifyContent = Justify.FlexEnd;
        
        var jumpButton = new Button(() => JumpToTree(dangerInfo));
        jumpButton.text = "跳转";
        jumpButton.style.backgroundColor = new Color(0.2f, 0.7f, 0.2f, 1f);
        jumpButton.style.color = Color.white;
        jumpButton.style.fontSize = 10;
        jumpButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        jumpButton.style.paddingTop = 4;
        jumpButton.style.paddingBottom = 4;
        jumpButton.style.paddingLeft = 8;
        jumpButton.style.paddingRight = 8;
        jumpButton.style.borderTopLeftRadius = 4;
        jumpButton.style.borderTopRightRadius = 4;
        jumpButton.style.borderBottomLeftRadius = 4;
        jumpButton.style.borderBottomRightRadius = 4;
        uiManager?.ApplyFont(jumpButton);
        buttonContainer.Add(jumpButton);
        
        itemContainer.Add(buttonContainer);
        treeListContainer.Add(itemContainer);
    }
    
    // 辅助方法
    Color GetDangerLevelColor(TreeDangerMonitor.TreeDangerLevel level)
    {
        switch (level)
        {
            case TreeDangerMonitor.TreeDangerLevel.Safe:
                return new Color(0.2f, 0.7f, 0.2f, 1f);
            case TreeDangerMonitor.TreeDangerLevel.Warning:
                return new Color(1f, 0.6f, 0f, 1f);
            case TreeDangerMonitor.TreeDangerLevel.Critical:
                return new Color(1f, 0.4f, 0f, 1f);
            case TreeDangerMonitor.TreeDangerLevel.Emergency:
                return new Color(0.9f, 0.1f, 0.1f, 1f);
            default:
                return new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }
    
    string GetDangerLevelText(TreeDangerMonitor.TreeDangerLevel level)
    {
        switch (level)
        {
            case TreeDangerMonitor.TreeDangerLevel.Safe:
                return "安全";
            case TreeDangerMonitor.TreeDangerLevel.Warning:
                return "警告";
            case TreeDangerMonitor.TreeDangerLevel.Critical:
                return "危险";
            case TreeDangerMonitor.TreeDangerLevel.Emergency:
                return "紧急";
            default:
                return "未知";
        }
    }
    
    /// <summary>
    /// 获取当前FPS
    /// </summary>
    // float GetFPS()
    // {
    //     return 1.0f / Time.deltaTime;
    // }
    
    /// <summary>
    /// 获取内存使用量（MB）
    /// </summary>
    // float GetMemoryUsage()
    // {
    //     return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
    // }
    
    /// <summary>
    /// 获取场景中的对象数量
    /// </summary>
    // int GetSceneObjectCount()
    // {
    //     return FindObjectsOfType<GameObject>().Length;
    // }
    

    
    void SyncMonitoringParameters()
    {
        if (treeDangerMonitor == null) return;
        
        // 同步距离参数
        treeDangerMonitor.criticalDistance = criticalDistance;
        treeDangerMonitor.warningDistance = warningDistance;
        treeDangerMonitor.safeDistance = safeDistance;
        
        // 同步生长参数
        treeDangerMonitor.baseGrowthRate = baseGrowthRate;
    }
    
    // 公共接口方法
    public void RefreshDisplay()
    {
        if (treeDangerMonitor == null)
        {
            Debug.LogWarning("TreeDangerMonitor未找到，无法刷新显示");
            return;
        }
        
        // 更新显示
        UpdateDisplay();
        
        Debug.Log("显示刷新完成");
    }
    
    public void Hide()
    {
        this.enabled = false;
    }
    
    public void Show()
    {
        this.enabled = true;
        RefreshDisplay();
    }
    
    public void UpdateDisplay()
    {
        // 更新统计信息
        UpdateStatistics();
        
        // 更新树木列表
        UpdateTreeList();
        
        // 更新距离信息显示
        UpdateDistanceDisplay();
        
        // 更新状态显示
        if (statusLabel != null)
        {
            if (isMonitoring)
            {
                statusLabel.text = "监测进行中...";
                statusLabel.style.color = new Color(0.2f, 0.7f, 0.2f, 1f);
            }
            else
            {
                statusLabel.text = "系统就绪, 等待监测数据...";
                statusLabel.style.color = new Color(0.6f, 0.6f, 0.2f, 1f);
            }
        }
    }
    
    /// <summary>
    /// 更新距离信息显示
    /// </summary>
    void UpdateDistanceDisplay()
    {
        if (treeDangerPanel != null)
        {
            // 查找距离信息容器
            var distanceSection = treeDangerPanel.Q("distanceContent");
            if (distanceSection != null && distanceSection.parent != null)
            {
                DisplayAllTreesDistance(distanceSection.parent);
            }
        }
    }
    
    /// <summary>
    /// 自动刷新协程
    /// </summary>
    IEnumerator AutoRefreshCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            
            if (isMonitoring)
            {
                UpdateDisplay();
            }
        }
    }
    
    void JumpToTree(TreeDangerMonitor.TreeDangerInfo dangerInfo)
    {
        if (dangerInfo == null || dangerInfo.tree == null)
        {
            Debug.LogWarning("跳转失败：树木信息为空");
            return;
        }
        
        Vector3 treePosition = dangerInfo.tree.transform.position;
        var cameraManager = FindObjectOfType<CameraManager>();
        
        if (cameraManager != null && cameraManager.mainCamera != null)
        {
            Vector3 jumpPosition = treePosition + Vector3.up * 5f + Vector3.back * 10f;
            cameraManager.mainCamera.transform.position = jumpPosition;
            cameraManager.mainCamera.transform.LookAt(treePosition);
        }
        else
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.position = treePosition + Vector3.up * 5f + Vector3.back * 10f;
                mainCamera.transform.LookAt(treePosition);
            }
        }
        
        if (uiManager != null)
        {
            string treeInfo = $"已跳转到危险树木: {dangerInfo.tree.name}";
            uiManager.UpdateStatusBar(treeInfo);
        }
    }
}

