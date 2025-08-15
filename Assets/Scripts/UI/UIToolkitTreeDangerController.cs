using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// UI Toolkit树木危险监测控制器 - 简化版本
/// </summary>
public class UIToolkitTreeDangerController : MonoBehaviour
{
    [Header("监测系统设置")]
    public bool enableAutoMonitoring = true;
    public float monitoringInterval = 5f;
    public float maxDetectionDistance = 100f;
    
    [Header("危险评估参数")]
    public float criticalDistance = 10f;  // 危险距离
    public float warningDistance = 30f;   // 警告距离
    public float safeDistance = 50f;      // 安全距离
    
    [Header("树木生长参数")]
    public float baseGrowthRate = 0.1f;
    public float maxTreeHeight = 50f;
    public float seasonalGrowthFactor = 0.2f;
    
    [Header("电力线安全参数")]
    public float powerlineHeight = 20f;
    public float powerlineSag = 2f;
    public float windSwayFactor = 1.5f;
    
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
    private Label statisticsLabel;
    private VisualElement treeListContainer;
    
    // 新增：时间预测显示元素
    private Label oneYearPredictionLabel;
    private Label threeYearPredictionLabel;
    private Label trendAnalysisLabel;
    
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
        
        // 同步参数
        SyncMonitoringParameters();
        
        Initialize();
        
        // 启动自动刷新协程
        StartCoroutine(AutoRefreshCoroutine());
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
        
        // 控制按钮区域
        CreateControlButtons();
        
        // 状态显示
        statusLabel = new Label("系统就绪，等待监测数据...");
        statusLabel.style.color = new Color(0.7f, 0.4f, 0.1f, 1f);
        statusLabel.style.fontSize = 9; // 进一步减少字体大小
        statusLabel.style.marginTop = 6; // 减少状态标签上方间距
        statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        statusLabel.style.backgroundColor = new Color(1f, 1f, 0.8f, 1f);
        statusLabel.style.paddingTop = 4; // 减少状态标签内边距
        statusLabel.style.paddingBottom = 4;
        statusLabel.style.paddingLeft = 4;
        statusLabel.style.paddingRight = 4;
        statusLabel.style.borderTopLeftRadius = 4;
        statusLabel.style.borderTopRightRadius = 4;
        statusLabel.style.borderBottomLeftRadius = 4;
        statusLabel.style.borderBottomRightRadius = 4;
        statusLabel.style.borderLeftWidth = 2;
        statusLabel.style.borderLeftColor = new Color(0.8f, 0.6f, 0.2f, 1f);
        uiManager?.ApplyFont(statusLabel);
        controlSection.Add(statusLabel);
        
        treeDangerPanel.Add(controlSection);
    }
    
    void CreateParameterControls()
    {
        // 距离参数容器
        var distanceContainer = new VisualElement();
        distanceContainer.style.flexDirection = FlexDirection.Column; // 改为垂直布局，节省水平空间
        distanceContainer.style.alignItems = Align.Stretch; // 拉伸对齐，确保子元素占满宽度
        distanceContainer.style.marginBottom = 8; // 进一步减少底部间距
        distanceContainer.style.marginTop = 3;
        
        // 危险距离
        var criticalContainer = CreateParameterField("危险距离:", criticalDistance, 45, (value) => {
            criticalDistance = value;
            UpdateMonitoringParameters();
        });
        distanceContainer.Add(criticalContainer);
        
        // 警告距离
        var warningContainer = CreateParameterField("警告距离:", warningDistance, 45, (value) => {
            warningDistance = value;
            UpdateMonitoringParameters();
        });
        distanceContainer.Add(warningContainer);
        
        // 安全距离
        var safeContainer = CreateParameterField("安全距离:", safeDistance, 45, (value) => {
            safeDistance = value;
            UpdateMonitoringParameters();
        });
        distanceContainer.Add(safeContainer);
        
        controlSection.Add(distanceContainer);
        
        // 生长率参数
        var growthContainer = CreateParameterField("基础生长率 (m/年):", baseGrowthRate, 55, (value) => {
            baseGrowthRate = value;
            UpdateMonitoringParameters();
        });
        growthContainer.style.marginBottom = 10; // 进一步减少底部间距
        growthContainer.style.marginTop = 3;
        controlSection.Add(growthContainer);
    }
    
    VisualElement CreateParameterField(string labelText, float defaultValue, float fieldWidth, System.Action<float> onValueChanged)
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Row;
        container.style.alignItems = Align.Center; // 确保垂直居中对齐
        container.style.justifyContent = Justify.SpaceBetween; // 标签和输入框两端对齐
        container.style.marginRight = 4; // 进一步减少右侧间距
        container.style.marginLeft = 1; // 进一步减少左侧间距
        container.style.flexShrink = 0; // 防止被压缩
        container.style.width = Length.Percent(100); // 使用全宽度
        
        var label = new Label(labelText);
        label.style.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        label.style.fontSize = 9; // 进一步减少字体大小
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginRight = 4; // 进一步减少标签右侧间距
        label.style.minWidth = 60; // 进一步减少标签最小宽度
        label.style.flexShrink = 0; // 防止标签被压缩
        label.style.unityTextAlign = TextAnchor.MiddleLeft; // 确保标签文字左对齐
        uiManager?.ApplyFont(label);
        container.Add(label);
        
        var valueLabel = new Label(defaultValue.ToString("F1"));
        valueLabel.style.width = fieldWidth;
        valueLabel.style.height = 20; // 设置明确的高度
        valueLabel.style.marginLeft = 1; // 进一步减少输入框左侧间距
        valueLabel.style.flexShrink = 0; // 防止输入框被压缩
        valueLabel.style.backgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f); // 浅灰色背景
        valueLabel.style.color = new Color(0.2f, 0.2f, 0.2f, 1f); // 深色文字
        valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold; // 粗体显示数值
        valueLabel.style.borderLeftWidth = 1; // 添加边框使其更明显
        valueLabel.style.borderRightWidth = 1;
        valueLabel.style.borderTopWidth = 1;
        valueLabel.style.borderBottomWidth = 1;
        valueLabel.style.borderLeftColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        valueLabel.style.borderRightColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        valueLabel.style.borderTopColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        valueLabel.style.borderBottomColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        valueLabel.style.borderTopLeftRadius = 3;
        valueLabel.style.borderTopRightRadius = 3;
        valueLabel.style.borderBottomLeftRadius = 3;
        valueLabel.style.borderBottomRightRadius = 3;
        valueLabel.style.paddingLeft = 4;
        valueLabel.style.paddingRight = 4;
        valueLabel.style.paddingTop = 2;
        valueLabel.style.paddingBottom = 2;
        valueLabel.style.unityTextAlign = TextAnchor.MiddleCenter; // 数值居中对齐
        uiManager?.ApplyFont(valueLabel);
        container.Add(valueLabel);
        
        return container;
    }
    
    void CreateControlButtons()
    {
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.justifyContent = Justify.SpaceAround; // 改为SpaceAround
        buttonContainer.style.alignItems = Align.Center; // 垂直居中对齐
        buttonContainer.style.marginBottom = 8; // 进一步减少底部间距
        buttonContainer.style.marginTop = 3; // 进一步减少顶部间距
        buttonContainer.style.marginLeft = 5; // 增加左侧间距
        buttonContainer.style.marginRight = 5; // 增加右侧间距
        
        // 开始监测按钮
        startMonitoringButton = CreateStyledButton("开始监测", StartMonitoring, new Color(0.2f, 0.7f, 0.2f, 1f));
        buttonContainer.Add(startMonitoringButton);
        
        // 清除标记按钮
        clearMarkersButton = CreateStyledButton("清除标记", ClearAllMarkers, new Color(0.8f, 0.2f, 0.2f, 1f));
        buttonContainer.Add(clearMarkersButton);
        
        // 新增：生成时间预测报告按钮
        var reportButton = CreateStyledButton("预测报告", GenerateTimePredictionReport, new Color(0.2f, 0.5f, 0.8f, 1f));
        buttonContainer.Add(reportButton);
        
        controlSection.Add(buttonContainer);
    }
    
    Button CreateStyledButton(string text, System.Action onClick, Color backgroundColor)
    {
        var button = new Button(onClick);
        button.text = text;
        button.style.width = 70; // 进一步减少按钮宽度，适应侧栏
        button.style.height = 26; // 进一步减少按钮高度
        button.style.backgroundColor = backgroundColor;
        button.style.color = Color.white;
        button.style.fontSize = 9; // 进一步减少字体大小
        button.style.unityFontStyleAndWeight = FontStyle.Bold;
        button.style.borderTopLeftRadius = 5;
        button.style.borderTopRightRadius = 5;
        button.style.borderBottomLeftRadius = 5;
        button.style.borderBottomRightRadius = 5;
        button.style.marginLeft = 6; // 增加按钮间距
        button.style.marginRight = 6; // 增加按钮间距
        button.style.flexShrink = 0; // 防止按钮被压缩
        uiManager?.ApplyFont(button);
        return button;
    }
    
    void CreateStatisticsSection()
    {
        statisticsSection = new VisualElement();
        statisticsSection.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        statisticsSection.style.marginBottom = 10;
        statisticsSection.style.paddingTop = 15;
        statisticsSection.style.paddingBottom = 15;
        statisticsSection.style.paddingLeft = 15;
        statisticsSection.style.paddingRight = 15;
        statisticsSection.style.borderTopLeftRadius = 8;
        statisticsSection.style.borderTopRightRadius = 8;
        statisticsSection.style.borderBottomLeftRadius = 8;
        statisticsSection.style.borderBottomRightRadius = 8;
        statisticsSection.style.borderLeftWidth = 2;
        statisticsSection.style.borderRightWidth = 2;
        statisticsSection.style.borderTopWidth = 2;
        statisticsSection.style.borderBottomWidth = 2;
        statisticsSection.style.borderLeftColor = new Color(0.8f, 0.6f, 0.2f, 1f);
        statisticsSection.style.borderRightColor = new Color(0.8f, 0.6f, 0.2f, 1f);
        statisticsSection.style.borderTopColor = new Color(0.8f, 0.6f, 0.2f, 1f);
        statisticsSection.style.borderBottomColor = new Color(0.8f, 0.6f, 0.2f, 1f);
        statisticsSection.style.flexShrink = 0;
        
        var statsTitle = new Label("监测统计");
        statsTitle.style.color = new Color(0.6f, 0.4f, 0.1f, 1f);
        statsTitle.style.fontSize = 16;
        statsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        statsTitle.style.marginBottom = 10;
        uiManager?.ApplyFont(statsTitle);
        statisticsSection.Add(statsTitle);
        
        // 当前统计信息
        statisticsLabel = new Label("暂无数据");
        statisticsLabel.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        statisticsLabel.style.fontSize = 12;
        statisticsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(statisticsLabel);
        statisticsSection.Add(statisticsLabel);
        
        // 新增：时间预测统计区域
        CreateTimePredictionSection();
        
        treeDangerPanel.Add(statisticsSection);
    }
    
    void CreateTimePredictionSection()
    {
        // 时间预测标题
        var predictionTitle = new Label("时间预测分析");
        predictionTitle.style.color = new Color(0.4f, 0.3f, 0.1f, 1f);
        predictionTitle.style.fontSize = 14;
        predictionTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        predictionTitle.style.marginTop = 15;
        predictionTitle.style.marginBottom = 8;
        uiManager?.ApplyFont(predictionTitle);
        statisticsSection.Add(predictionTitle);
        
        // 一年后预测
        var oneYearContainer = CreatePredictionContainer("一年后预测", "一年后树木生长对电线的危险程度预测");
        oneYearContainer.style.marginBottom = 8;
        statisticsSection.Add(oneYearContainer);
        
        // 三年后预测
        var threeYearContainer = CreatePredictionContainer("三年后预测", "三年后树木生长对电线的危险程度预测");
        threeYearContainer.style.marginBottom = 8;
        statisticsSection.Add(threeYearContainer);
        
        // 趋势分析
        var trendContainer = CreateTrendAnalysisContainer();
        statisticsSection.Add(trendContainer);
    }
    
    VisualElement CreatePredictionContainer(string title, string description)
    {
        var container = new VisualElement();
        container.style.backgroundColor = new Color(0.95f, 0.95f, 0.98f, 1f);
        container.style.paddingTop = 8;
        container.style.paddingBottom = 8;
        container.style.paddingLeft = 8;
        container.style.paddingRight = 8;
        container.style.borderTopLeftRadius = 6;
        container.style.borderTopRightRadius = 6;
        container.style.borderBottomLeftRadius = 6;
        container.style.borderBottomRightRadius = 6;
        container.style.borderLeftWidth = 1;
        container.style.borderLeftColor = new Color(0.7f, 0.7f, 0.8f, 1f);
        
        // 标题
        var titleLabel = new Label(title);
        titleLabel.style.color = new Color(0.3f, 0.3f, 0.5f, 1f);
        titleLabel.style.fontSize = 12;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 4;
        uiManager?.ApplyFont(titleLabel);
        container.Add(titleLabel);
        
        // 描述
        var descLabel = new Label(description);
        descLabel.style.color = new Color(0.5f, 0.5f, 0.6f, 1f);
        descLabel.style.fontSize = 10;
        descLabel.style.marginBottom = 6;
        uiManager?.ApplyFont(descLabel);
        container.Add(descLabel);
        
        // 预测内容（将在UpdateStatistics中填充）
        var contentLabel = new Label("等待监测数据...");
        contentLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        contentLabel.style.fontSize = 11;
        contentLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        contentLabel.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);
        contentLabel.style.paddingTop = 4;
        contentLabel.style.paddingBottom = 4;
        contentLabel.style.paddingLeft = 6;
        contentLabel.style.paddingRight = 6;
        contentLabel.style.borderTopLeftRadius = 3;
        contentLabel.style.borderTopRightRadius = 3;
        contentLabel.style.borderBottomLeftRadius = 3;
        contentLabel.style.borderBottomRightRadius = 3;
        uiManager?.ApplyFont(contentLabel);
        container.Add(contentLabel);
        
        // 存储引用以便后续更新
        if (title.Contains("一年后"))
        {
            oneYearPredictionLabel = contentLabel;
        }
        else if (title.Contains("三年后"))
        {
            threeYearPredictionLabel = contentLabel;
        }
        
        return container;
    }
    
    VisualElement CreateTrendAnalysisContainer()
    {
        var container = new VisualElement();
        container.style.backgroundColor = new Color(0.98f, 0.95f, 0.9f, 1f);
        container.style.paddingTop = 8;
        container.style.paddingBottom = 8;
        container.style.paddingLeft = 8;
        container.style.paddingRight = 8;
        container.style.borderTopLeftRadius = 6;
        container.style.borderTopRightRadius = 6;
        container.style.borderBottomLeftRadius = 6;
        container.style.borderBottomRightRadius = 6;
        container.style.borderLeftWidth = 1;
        container.style.borderLeftColor = new Color(0.8f, 0.7f, 0.5f, 1f);
        
        // 标题
        var titleLabel = new Label("趋势分析");
        titleLabel.style.color = new Color(0.5f, 0.4f, 0.2f, 1f);
        titleLabel.style.fontSize = 12;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 4;
        uiManager?.ApplyFont(titleLabel);
        container.Add(titleLabel);
        
        // 趋势内容（将在UpdateStatistics中填充）
        var contentLabel = new Label("等待监测数据...");
        contentLabel.style.color = new Color(0.6f, 0.5f, 0.3f, 1f);
        contentLabel.style.fontSize = 11;
        contentLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        contentLabel.style.backgroundColor = new Color(0.95f, 0.92f, 0.85f, 0.5f);
        contentLabel.style.paddingTop = 4;
        contentLabel.style.paddingBottom = 4;
        contentLabel.style.paddingLeft = 6;
        contentLabel.style.paddingRight = 6;
        contentLabel.style.borderTopLeftRadius = 3;
        contentLabel.style.borderTopRightRadius = 3;
        contentLabel.style.borderBottomLeftRadius = 3;
        contentLabel.style.borderBottomRightRadius = 3;
        uiManager?.ApplyFont(contentLabel);
        container.Add(contentLabel);
        
        trendAnalysisLabel = contentLabel;
        
        return container;
    }
    
    void CreateTreeListSection()
    {
        treeListSection = new VisualElement();
        treeListSection.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        treeListSection.style.paddingTop = 15;
        treeListSection.style.paddingBottom = 15;
        treeListSection.style.paddingLeft = 15;
        treeListSection.style.paddingRight = 15;
        treeListSection.style.borderTopLeftRadius = 8;
        treeListSection.style.borderTopRightRadius = 8;
        treeListSection.style.borderBottomLeftRadius = 8;
        treeListSection.style.borderBottomRightRadius = 8;
        treeListSection.style.borderLeftWidth = 2;
        treeListSection.style.borderRightWidth = 2;
        treeListSection.style.borderTopWidth = 2;
        treeListSection.style.borderBottomWidth = 2;
        treeListSection.style.borderLeftColor = new Color(0.3f, 0.7f, 1f, 1f);
        treeListSection.style.borderRightColor = new Color(0.3f, 0.7f, 1f, 1f);
        treeListSection.style.borderTopColor = new Color(0.3f, 0.7f, 1f, 1f);
        treeListSection.style.borderBottomColor = new Color(0.3f, 0.7f, 1f, 1f);
        treeListSection.style.flexGrow = 1;
        
        var listTitle = new Label("危险树木列表");
        listTitle.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
        listTitle.style.fontSize = 16;
        listTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        listTitle.style.marginBottom = 8;
        uiManager?.ApplyFont(listTitle);
        treeListSection.Add(listTitle);
        
        var scrollView = new ScrollView();
        scrollView.style.minHeight = 300;
        scrollView.style.maxHeight = 1000;
        scrollView.style.flexGrow = 1;
        scrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
        
        treeListContainer = new VisualElement();
        treeListContainer.style.flexDirection = FlexDirection.Column;
        treeListContainer.style.flexShrink = 0;
        
        scrollView.Add(treeListContainer);
        treeListSection.Add(scrollView);
        
        treeDangerPanel.Add(treeListSection);
    }
    
    // 控制方法
    void StartMonitoring()
    {
        if (treeDangerMonitor == null) return;
        
        UpdateStatus("🔄 正在启动自动监测...");
        
        // 启用自动监测
        treeDangerMonitor.enableAutoMonitoring = true;
        
        // 同步监测参数
        SyncMonitoringParameters();
        
        // 强制刷新并执行一次监测
        treeDangerMonitor.ForceRefreshAndMonitor();
        
        UpdateStatus("✅ 自动监测已启动");
        
        // 延迟刷新显示，确保监测结果已更新
        StartCoroutine(DelayedRefreshDisplay());
    }
    
    /// <summary>
    /// 延迟刷新显示的协程
    /// </summary>
    System.Collections.IEnumerator DelayedRefreshDisplay()
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
        
        treeDangerMonitor.ClearAllDangerMarkers();
        UpdateStatus("已清除所有危险标记");
        UpdateStatistics();
        UpdateTreeList();
        
        if (uiManager != null)
        {
            uiManager.UpdateStatusBar("已清除所有树木危险标记");
        }
    }
    
    // 更新方法
    void UpdateStatus(string message)
    {
        if (statusLabel != null)
        {
            // 添加时间戳，删除状态图标
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string formattedMessage = $"[{timestamp}] {message}";
            
            statusLabel.text = formattedMessage;
            
            // 根据消息类型设置不同的样式
            if (message.Contains("错误") || message.Contains("失败"))
            {
                statusLabel.style.color = new Color(0.9f, 0.1f, 0.1f, 1f);
                statusLabel.style.backgroundColor = new Color(1f, 0.9f, 0.9f, 1f);
                statusLabel.style.borderLeftColor = new Color(0.9f, 0.1f, 0.1f, 1f);
            }
            else if (message.Contains("警告"))
            {
                statusLabel.style.color = new Color(1f, 0.6f, 0f, 1f);
                statusLabel.style.backgroundColor = new Color(1f, 0.98f, 0.9f, 1f);
                statusLabel.style.borderLeftColor = new Color(1f, 0.6f, 0f, 1f);
            }
            else if (message.Contains("成功") || message.Contains("完成"))
            {
                statusLabel.style.color = new Color(0.2f, 0.7f, 0.2f, 1f);
                statusLabel.style.backgroundColor = new Color(0.9f, 1f, 0.9f, 1f);
                statusLabel.style.borderLeftColor = new Color(0.2f, 0.7f, 0.2f, 1f);
            }
            else
            {
                statusLabel.style.color = new Color(0.7f, 0.4f, 0.1f, 1f);
                statusLabel.style.backgroundColor = new Color(1f, 1f, 0.8f, 1f);
                statusLabel.style.borderLeftColor = new Color(0.8f, 0.6f, 0.2f, 1f);
            }
        }
    }
    
    void UpdateStatistics()
    {
        if (treeDangerMonitor == null || statisticsLabel == null) return;
        
        var stats = treeDangerMonitor.GetDangerStatistics();
        int totalTrees = treeDangerMonitor.GetTreeCount(); // 获取实际找到的树木数量
        bool hasMonitoringResults = treeDangerMonitor.GetAllDangerInfo().Count > 0;
        
        if (totalTrees == 0)
        {
            // 没有找到树木
            statisticsLabel.text = "暂无监测数据\n" +
                                  "提示：请确保场景中有树木对象\n" +
                                  "建议：运行SceneInitializer创建树木\n" +
                                  "操作：点击'开始监测'按钮刷新";
            statisticsLabel.style.color = new Color(0.8f, 0.4f, 0f, 1f);
            statisticsLabel.style.backgroundColor = new Color(1f, 0.95f, 0.9f, 0.8f);
            return;
        }
        
        if (hasMonitoringResults)
        {
            // 有监测结果，显示完整统计
            int safeCount = stats.ContainsKey(TreeDangerMonitor.TreeDangerLevel.Safe) ? stats[TreeDangerMonitor.TreeDangerLevel.Safe] : 0;
            int warningCount = stats.ContainsKey(TreeDangerMonitor.TreeDangerLevel.Warning) ? stats[TreeDangerMonitor.TreeDangerLevel.Warning] : 0;
            int criticalCount = stats.ContainsKey(TreeDangerMonitor.TreeDangerLevel.Critical) ? stats[TreeDangerMonitor.TreeDangerLevel.Critical] : 0;
            int emergencyCount = stats.ContainsKey(TreeDangerMonitor.TreeDangerLevel.Emergency) ? stats[TreeDangerMonitor.TreeDangerLevel.Emergency] : 0;
            
            int monitoredTotal = stats.Values.Sum();
            float riskPercentage = monitoredTotal > 0 ? ((float)(warningCount + criticalCount + emergencyCount) / monitoredTotal) * 100f : 0f;
            
            string statsText = $"监测统计 (总计: {monitoredTotal}棵)\n" +
                              $"安全: {safeCount}棵 ({(monitoredTotal > 0 ? (float)safeCount / monitoredTotal * 100f : 0f):F1}%)\n" +
                              $"警告: {warningCount}棵 ({(monitoredTotal > 0 ? (float)warningCount / monitoredTotal * 100f : 0f):F1}%)\n" +
                              $"危险: {criticalCount}棵 ({(monitoredTotal > 0 ? (float)criticalCount / monitoredTotal * 100f : 0f):F1}%)\n" +
                              $"紧急: {emergencyCount}棵 ({(monitoredTotal > 0 ? (float)emergencyCount / monitoredTotal * 100f : 0f):F1}%)\n" +
                              $"总体风险: {riskPercentage:F1}%";
            
            statisticsLabel.text = statsText;
            
            // 根据风险等级设置不同的颜色
            if (emergencyCount > 0)
            {
                statisticsLabel.style.color = new Color(0.9f, 0.1f, 0.1f, 1f);
                statisticsLabel.style.backgroundColor = new Color(1f, 0.9f, 0.9f, 0.8f);
            }
            else if (criticalCount > 0)
            {
                statisticsLabel.style.color = new Color(1f, 0.4f, 0f, 1f);
                statisticsLabel.style.backgroundColor = new Color(1f, 0.95f, 0.9f, 0.8f);
            }
            else if (warningCount > 0)
            {
                statisticsLabel.style.color = new Color(1f, 0.6f, 0f, 1f);
                statisticsLabel.style.backgroundColor = new Color(1f, 0.98f, 0.9f, 0.8f);
            }
            else
            {
                statisticsLabel.style.color = new Color(0.2f, 0.7f, 0.2f, 1f);
                statisticsLabel.style.backgroundColor = new Color(0.9f, 1f, 0.9f, 0.8f);
            }
        }
        else
        {
            // 找到树木但未执行监测
            statisticsLabel.text = $"监测统计 (总计: {totalTrees}棵)\n" +
                                  "所有树木都处于安全状态\n" +
                                  "总体风险: 0.0%\n" +
                                  "提示：树木已找到，但尚未执行监测\n" +
                                  "建议：点击'开始监测'按钮开始监测";
            statisticsLabel.style.color = new Color(0.2f, 0.7f, 0.2f, 1f);
            statisticsLabel.style.backgroundColor = new Color(0.9f, 1f, 0.9f, 0.8f);
        }
        
        // 更新时间预测统计
        UpdateTimePredictionStatistics();
    }
    
    void UpdateTimePredictionStatistics()
    {
        if (treeDangerMonitor == null) return;
        
        var predictionStats = treeDangerMonitor.GetTimePredictionStatistics();
        
        if (!(bool)predictionStats["hasData"])
        {
            // 没有预测数据
            if (oneYearPredictionLabel != null)
            {
                oneYearPredictionLabel.text = "暂无监测数据";
                oneYearPredictionLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            }
            
            if (threeYearPredictionLabel != null)
            {
                threeYearPredictionLabel.text = "暂无监测数据";
                threeYearPredictionLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            }
            
            if (trendAnalysisLabel != null)
            {
                trendAnalysisLabel.text = "暂无监测数据";
                trendAnalysisLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            }
            return;
        }
        
        // 更新一年后预测
        if (oneYearPredictionLabel != null)
        {
            var oneYear = (Dictionary<string, object>)predictionStats["oneYear"];
            int critical = (int)oneYear["critical"];
            int emergency = (int)oneYear["emergency"];
            float riskPercentage = (float)oneYear["riskPercentage"];
            bool willBeDangerous = (bool)oneYear["willBeDangerous"];
            
            string oneYearText = $"危险: {critical}棵 | 紧急: {emergency}棵\n" +
                                $"总体风险: {riskPercentage:F1}%";
            
            if (willBeDangerous)
            {
                oneYearText += "\n⚠️ 一年后将出现危险情况";
                oneYearPredictionLabel.style.color = new Color(0.8f, 0.4f, 0.1f, 1f);
                oneYearPredictionLabel.style.backgroundColor = new Color(1f, 0.95f, 0.9f, 0.8f);
            }
            else
            {
                oneYearText += "\n✅ 一年后仍保持安全";
                oneYearPredictionLabel.style.color = new Color(0.2f, 0.6f, 0.2f, 1f);
                oneYearPredictionLabel.style.backgroundColor = new Color(0.9f, 1f, 0.9f, 0.8f);
            }
            
            oneYearPredictionLabel.text = oneYearText;
        }
        
        // 更新三年后预测
        if (threeYearPredictionLabel != null)
        {
            var threeYear = (Dictionary<string, object>)predictionStats["threeYear"];
            int critical = (int)threeYear["critical"];
            int emergency = (int)threeYear["emergency"];
            float riskPercentage = (float)threeYear["riskPercentage"];
            bool willBeDangerous = (bool)threeYear["willBeDangerous"];
            
            string threeYearText = $"危险: {critical}棵 | 紧急: {emergency}棵\n" +
                                  $"总体风险: {riskPercentage:F1}%";
            
            if (willBeDangerous)
            {
                threeYearText += "\n🚨 三年后将出现危险情况";
                threeYearPredictionLabel.style.color = new Color(0.9f, 0.2f, 0.2f, 1f);
                threeYearPredictionLabel.style.backgroundColor = new Color(1f, 0.9f, 0.9f, 0.8f);
            }
            else
            {
                threeYearText += "\n✅ 三年后仍保持安全";
                threeYearPredictionLabel.style.color = new Color(0.2f, 0.6f, 0.2f, 1f);
                threeYearPredictionLabel.style.backgroundColor = new Color(0.9f, 1f, 0.9f, 0.8f);
            }
            
            threeYearPredictionLabel.text = threeYearText;
        }
        
        // 更新趋势分析
        if (trendAnalysisLabel != null)
        {
            var trend = (Dictionary<string, object>)predictionStats["trend"];
            bool riskIncreasing = (bool)trend["riskIncreasing"];
            string maxRiskPeriod = (string)trend["maxRiskPeriod"];
            string recommendation = (string)trend["recommendation"];
            
            string trendText = $"风险趋势: {(riskIncreasing ? "上升" : "稳定")}\n" +
                              $"最大风险期: {maxRiskPeriod}\n" +
                              $"建议: {recommendation}";
            
            if (riskIncreasing)
            {
                trendAnalysisLabel.style.color = new Color(0.8f, 0.4f, 0.1f, 1f);
                trendAnalysisLabel.style.backgroundColor = new Color(1f, 0.95f, 0.9f, 0.8f);
            }
            else
            {
                trendAnalysisLabel.style.color = new Color(0.2f, 0.6f, 0.2f, 1f);
                trendAnalysisLabel.style.backgroundColor = new Color(0.9f, 1f, 0.9f, 0.8f);
            }
            
            trendAnalysisLabel.text = trendText;
        }
    }
    
    void UpdateTreeList()
    {
        if (treeDangerMonitor == null || treeListContainer == null) return;
        
        treeListContainer.Clear();
        
        var allDangerInfo = treeDangerMonitor.GetAllDangerInfo();
        if (allDangerInfo == null || allDangerInfo.Count == 0)
        {
            CreateNoDangerInfoDisplay();
            return;
        }
        
        var sortedDangerInfo = allDangerInfo.OrderByDescending(d => d.dangerLevel).ToList();
        
        for (int i = 0; i < sortedDangerInfo.Count; i++)
        {
            var dangerInfo = sortedDangerInfo[i];
            if (dangerInfo == null || dangerInfo.tree == null) continue;
            
            CreateTreeListItem(dangerInfo, i + 1);
        }
    }
    
    void CreateNoDangerInfoDisplay()
    {
        // 创建系统状态信息面板
        var statusPanel = new VisualElement();
        statusPanel.style.backgroundColor = new Color(0.95f, 0.98f, 0.95f, 1f);
        statusPanel.style.marginBottom = 15;
        statusPanel.style.paddingTop = 15;
        statusPanel.style.paddingBottom = 15;
        statusPanel.style.paddingLeft = 15;
        statusPanel.style.paddingRight = 15;
        statusPanel.style.borderTopLeftRadius = 8;
        statusPanel.style.borderTopRightRadius = 8;
        statusPanel.style.borderBottomLeftRadius = 8;
        statusPanel.style.borderBottomRightRadius = 8;
        statusPanel.style.borderLeftWidth = 2;
        statusPanel.style.borderLeftColor = new Color(0.2f, 0.7f, 0.2f, 1f);
        
        // 获取系统状态信息
        string statusText = "系统状态良好";
        string detailText = "当前场景中所有树木都处于安全状态, 与电力线保持安全距离。";
        
        if (treeDangerMonitor != null)
        {
            int treeCount = treeDangerMonitor.GetTreeCount();
            if (treeCount == 0)
            {
                statusText = "系统状态：未找到树木";
                detailText = "场景中没有检测到树木对象，请确保：\n1. 已运行SceneInitializer创建树木\n2. 树木对象名称包含'Tree'或'植物'\n3. 树木对象已启用且可见";
            }
            else
            {
                statusText = $"系统状态：已找到{treeCount}棵树木";
                detailText = $"场景中检测到{treeCount}棵树木，所有树木都处于安全状态。\n与电力线保持安全距离，无需担心。";
            }
        }
        
        // 系统状态标签
        var statusLabel = new Label(statusText);
        statusLabel.style.fontSize = 16;
        statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        statusLabel.style.color = new Color(0.2f, 0.7f, 0.2f, 1f);
        statusLabel.style.marginBottom = 8;
        statusPanel.Add(statusLabel);
        
        // 详细说明标签
        var detailLabel = new Label(detailText);
        detailLabel.style.fontSize = 12;
        detailLabel.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        detailLabel.style.whiteSpace = WhiteSpace.Normal;
        detailLabel.style.marginBottom = 10;
        statusPanel.Add(detailLabel);
        
        // 系统运行时间
        var uptimeLabel = new Label($"系统运行时间: {GetSystemRuntime()}");
        uptimeLabel.style.fontSize = 11;
        uptimeLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        uptimeLabel.style.marginBottom = 10;
        statusPanel.Add(uptimeLabel);
        
        // 监测信息面板
        var monitoringPanel = new VisualElement();
        monitoringPanel.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        monitoringPanel.style.paddingTop = 10;
        monitoringPanel.style.paddingBottom = 10;
        monitoringPanel.style.paddingLeft = 10;
        monitoringPanel.style.paddingRight = 10;
        monitoringPanel.style.borderTopLeftRadius = 6;
        monitoringPanel.style.borderTopRightRadius = 6;
        monitoringPanel.style.borderBottomLeftRadius = 6;
        monitoringPanel.style.borderBottomRightRadius = 6;
        monitoringPanel.style.borderTopWidth = 1;
        monitoringPanel.style.borderTopColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        
        var monitoringTitle = new Label("监测信息");
        monitoringTitle.style.fontSize = 12;
        monitoringTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        monitoringTitle.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        monitoringTitle.style.marginBottom = 8;
        monitoringPanel.Add(monitoringTitle);
        
        // 监测参数详情
        if (treeDangerMonitor != null)
        {
            var paramLabel = new Label($"距离参数: 危险({treeDangerMonitor.criticalDistance}m) | 警告({treeDangerMonitor.warningDistance}m) | 安全({treeDangerMonitor.safeDistance}m)");
            paramLabel.style.fontSize = 11;
            paramLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            paramLabel.style.marginBottom = 5;
            monitoringPanel.Add(paramLabel);
            
            var growthLabel = new Label($"生长参数: 基础生长率 {treeDangerMonitor.baseGrowthRate}m/年");
            growthLabel.style.fontSize = 11;
            growthLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            monitoringPanel.Add(growthLabel);
        }
        
        statusPanel.Add(monitoringPanel);
        
        // 操作建议
        var actionLabel = new Label("操作建议：点击'开始监测'按钮可立即执行监测，点击'清除标记'按钮可清理所有标记。");
        actionLabel.style.fontSize = 11;
        actionLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        actionLabel.style.marginTop = 10;
        actionLabel.style.whiteSpace = WhiteSpace.Normal;
        statusPanel.Add(actionLabel);
        
        treeListContainer.Add(statusPanel);
    }
    
    void CreateTreeListItem(TreeDangerMonitor.TreeDangerInfo dangerInfo, int index)
    {
        var itemContainer = new VisualElement();
        itemContainer.style.backgroundColor = new Color(1f, 1f, 1f, 0.9f);
        itemContainer.style.marginBottom = 8;
        itemContainer.style.paddingTop = 12;
        itemContainer.style.paddingBottom = 12;
        itemContainer.style.paddingLeft = 12;
        itemContainer.style.paddingRight = 12;
        itemContainer.style.borderTopLeftRadius = 6;
        itemContainer.style.borderTopRightRadius = 6;
        itemContainer.style.borderBottomLeftRadius = 6;
        itemContainer.style.borderBottomRightRadius = 6;
        itemContainer.style.borderLeftWidth = 3;
        itemContainer.style.borderRightWidth = 1;
        itemContainer.style.borderTopWidth = 1;
        itemContainer.style.borderBottomWidth = 1;
        
        Color borderColor = GetDangerLevelColor(dangerInfo.dangerLevel);
        itemContainer.style.borderLeftColor = borderColor;
        itemContainer.style.borderRightColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        itemContainer.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        itemContainer.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        
        // 标题行
        var titleRow = new VisualElement();
        titleRow.style.flexDirection = FlexDirection.Row;
        titleRow.style.justifyContent = Justify.SpaceBetween;
        titleRow.style.alignItems = Align.Center;
        titleRow.style.marginBottom = 8;
        
        var titleLabel = new Label($"危险树木 #{index}");
        titleLabel.style.color = new Color(0.2f, 0.5f, 0.8f, 1f);
        titleLabel.style.fontSize = 14;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        uiManager?.ApplyFont(titleLabel);
        titleRow.Add(titleLabel);
        
        // 按钮容器
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        
        var jumpButton = CreateStyledButton("跳转", () => JumpToTree(dangerInfo), new Color(0.2f, 0.7f, 0.2f, 1f));
        jumpButton.style.width = 60;
        jumpButton.style.height = 24;
        jumpButton.style.fontSize = 11;
        buttonContainer.Add(jumpButton);
        
        titleRow.Add(buttonContainer);
        itemContainer.Add(titleRow);
        
        // 信息网格
        var infoGrid = new VisualElement();
        infoGrid.style.flexDirection = FlexDirection.Column;
        
        // 树木名称和危险等级
        var nameRow = new VisualElement();
        nameRow.style.flexDirection = FlexDirection.Row;
        nameRow.style.justifyContent = Justify.SpaceBetween;
        nameRow.style.marginBottom = 4;
        
        var treeName = new Label($"树木: {dangerInfo.tree.name}");
        treeName.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        treeName.style.fontSize = 12;
        uiManager?.ApplyFont(treeName);
        nameRow.Add(treeName);
        
        var levelInfo = new Label($"等级: {GetDangerLevelString(dangerInfo.dangerLevel)}");
        levelInfo.style.color = borderColor;
        levelInfo.style.fontSize = 12;
        levelInfo.style.unityFontStyleAndWeight = FontStyle.Bold;
        uiManager?.ApplyFont(levelInfo);
        nameRow.Add(levelInfo);
        
        infoGrid.Add(nameRow);
        
        // 距离信息 - 增强显示
        var distanceContainer = new VisualElement();
        distanceContainer.style.flexDirection = FlexDirection.Column;
        distanceContainer.style.marginBottom = 4;
        
        var currentDistanceLabel = new Label($"当前距离: {dangerInfo.currentDistance:F1}m");
        currentDistanceLabel.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        currentDistanceLabel.style.fontSize = 12;
        uiManager?.ApplyFont(currentDistanceLabel);
        distanceContainer.Add(currentDistanceLabel);
        
        var projectedDistanceLabel = new Label($"预测距离: {dangerInfo.projectedDistance:F1}m");
        projectedDistanceLabel.style.color = new Color(0.5f, 0.3f, 0.3f, 1f);
        projectedDistanceLabel.style.fontSize = 12;
        uiManager?.ApplyFont(projectedDistanceLabel);
        distanceContainer.Add(projectedDistanceLabel);
        
        // 距离趋势分析
        if (dangerInfo.projectedDistance < dangerInfo.currentDistance)
        {
            var trendLabel = new Label("距离正在减少，风险增加中");
            trendLabel.style.color = new Color(0.8f, 0.4f, 0.1f, 1f);
            trendLabel.style.fontSize = 11;
            trendLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            uiManager?.ApplyFont(trendLabel);
            distanceContainer.Add(trendLabel);
        }
        else if (dangerInfo.projectedDistance > dangerInfo.currentDistance)
        {
            var trendLabel = new Label("距离正在增加，风险减少中");
            trendLabel.style.color = new Color(0.2f, 0.6f, 0.2f, 1f);
            trendLabel.style.fontSize = 11;
            uiManager?.ApplyFont(trendLabel);
            distanceContainer.Add(trendLabel);
        }
        
        infoGrid.Add(distanceContainer);
        
        // 树木信息 - 增强显示
        var treeInfoContainer = new VisualElement();
        treeInfoContainer.style.flexDirection = FlexDirection.Column;
        treeInfoContainer.style.marginBottom = 4;
        
        var heightLabel = new Label($"树木高度: {dangerInfo.treeHeight:F1}m");
        heightLabel.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        heightLabel.style.fontSize = 12;
        uiManager?.ApplyFont(heightLabel);
        treeInfoContainer.Add(heightLabel);
        
        var growthLabel = new Label($"生长率: {dangerInfo.growthRate:F3}m/年");
        growthLabel.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        growthLabel.style.fontSize = 12;
        uiManager?.ApplyFont(growthLabel);
        treeInfoContainer.Add(growthLabel);
        
        // 生长趋势分析
        if (dangerInfo.growthRate > 0.05f)
        {
            var growthTrendLabel = new Label("生长速度较快，需要密切关注");
            growthTrendLabel.style.color = new Color(0.7f, 0.5f, 0.1f, 1f);
            growthTrendLabel.style.fontSize = 11;
            uiManager?.ApplyFont(growthTrendLabel);
            treeInfoContainer.Add(growthTrendLabel);
        }
        
        infoGrid.Add(treeInfoContainer);
        
        // 电力线信息 - 新增显示
        if (dangerInfo.powerline != null)
        {
            var powerlineContainer = new VisualElement();
            powerlineContainer.style.flexDirection = FlexDirection.Column;
            powerlineContainer.style.marginBottom = 4;
            powerlineContainer.style.backgroundColor = new Color(0.95f, 0.95f, 1f, 0.5f);
            powerlineContainer.style.paddingTop = 4;
            powerlineContainer.style.paddingBottom = 4;
            powerlineContainer.style.paddingLeft = 6;
            powerlineContainer.style.paddingRight = 6;
            powerlineContainer.style.borderTopLeftRadius = 4;
            powerlineContainer.style.borderTopRightRadius = 4;
            powerlineContainer.style.borderBottomLeftRadius = 4;
            powerlineContainer.style.borderBottomRightRadius = 4;
            powerlineContainer.style.borderLeftWidth = 2;
            powerlineContainer.style.borderLeftColor = new Color(0.3f, 0.5f, 0.8f, 1f);
            
            var powerlineTitle = new Label("相关电力线信息");
            powerlineTitle.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
            powerlineTitle.style.fontSize = 11;
            powerlineTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            powerlineTitle.style.marginBottom = 2;
            uiManager?.ApplyFont(powerlineTitle);
            powerlineContainer.Add(powerlineTitle);
            
            var powerlineName = new Label($"电力线: {dangerInfo.powerline.name}");
            powerlineName.style.color = new Color(0.4f, 0.4f, 0.6f, 1f);
            powerlineName.style.fontSize = 10;
            uiManager?.ApplyFont(powerlineName);
            powerlineContainer.Add(powerlineName);
            
            if (!string.IsNullOrEmpty(dangerInfo.towerGroup) && !string.IsNullOrEmpty(dangerInfo.towerNumber))
            {
                var towerInfo = new Label($"电塔组: {dangerInfo.towerGroup}, 编号: {dangerInfo.towerNumber}");
                towerInfo.style.color = new Color(0.4f, 0.4f, 0.6f, 1f);
                towerInfo.style.fontSize = 10;
                uiManager?.ApplyFont(towerInfo);
                powerlineContainer.Add(towerInfo);
            }
            
            infoGrid.Add(powerlineContainer);
        }
        
        // 时间信息 - 新增显示
        var timeContainer = new VisualElement();
        timeContainer.style.flexDirection = FlexDirection.Column;
        timeContainer.style.marginBottom = 4;
        timeContainer.style.backgroundColor = new Color(0.98f, 0.95f, 0.9f, 0.5f);
        timeContainer.style.paddingTop = 4;
        timeContainer.style.paddingBottom = 4;
        timeContainer.style.paddingLeft = 6;
        timeContainer.style.paddingRight = 6;
        timeContainer.style.borderTopLeftRadius = 4;
        timeContainer.style.borderTopRightRadius = 4;
        timeContainer.style.borderBottomLeftRadius = 4;
        timeContainer.style.borderBottomRightRadius = 4;
        timeContainer.style.borderLeftWidth = 2;
        timeContainer.style.borderLeftColor = new Color(0.8f, 0.6f, 0.2f, 1f);
        
        var timeTitle = new Label("评估时间信息");
        timeTitle.style.color = new Color(0.6f, 0.4f, 0.1f, 1f);
        timeTitle.style.fontSize = 11;
        timeTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        timeTitle.style.marginBottom = 2;
        uiManager?.ApplyFont(timeTitle);
        timeContainer.Add(timeTitle);
        
        var lastAssessmentLabel = new Label($"最后评估: {dangerInfo.lastAssessment:yyyy-MM-dd HH:mm:ss}");
        lastAssessmentLabel.style.color = new Color(0.5f, 0.4f, 0.3f, 1f);
        lastAssessmentLabel.style.fontSize = 10;
        uiManager?.ApplyFont(lastAssessmentLabel);
        timeContainer.Add(lastAssessmentLabel);
        
        // 计算距离上次评估的时间
        var timeSinceAssessment = DateTime.Now - dangerInfo.lastAssessment;
        var timeAgoLabel = new Label($"距离上次评估: {GetTimeAgoString(timeSinceAssessment)}");
        timeAgoLabel.style.color = new Color(0.5f, 0.4f, 0.3f, 1f);
        timeAgoLabel.style.fontSize = 10;
        uiManager?.ApplyFont(timeAgoLabel);
        timeContainer.Add(timeAgoLabel);
        
        infoGrid.Add(timeContainer);
        
        // 风险描述 - 增强显示
        var riskContainer = new VisualElement();
        riskContainer.style.backgroundColor = new Color(1f, 0.95f, 0.95f, 0.5f);
        riskContainer.style.paddingTop = 4;
        riskContainer.style.paddingBottom = 4;
        riskContainer.style.paddingLeft = 6;
        riskContainer.style.paddingRight = 6;
        riskContainer.style.borderTopLeftRadius = 4;
        riskContainer.style.borderTopRightRadius = 4;
        riskContainer.style.borderBottomLeftRadius = 4;
        riskContainer.style.borderBottomRightRadius = 4;
        riskContainer.style.borderLeftWidth = 2;
        riskContainer.style.borderLeftColor = new Color(0.8f, 0.3f, 0.3f, 1f);
        
        var riskTitle = new Label("风险分析");
        riskTitle.style.color = new Color(0.7f, 0.2f, 0.2f, 1f);
        riskTitle.style.fontSize = 11;
        riskTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        riskTitle.style.marginBottom = 2;
        uiManager?.ApplyFont(riskTitle);
        riskContainer.Add(riskTitle);
        
        var riskInfo = new Label($"风险描述: {dangerInfo.riskDescription}");
        riskInfo.style.color = new Color(0.6f, 0.2f, 0.2f, 1f);
        riskInfo.style.fontSize = 10;
        riskInfo.style.whiteSpace = WhiteSpace.Normal;
        uiManager?.ApplyFont(riskInfo);
        riskContainer.Add(riskInfo);
        
        infoGrid.Add(riskContainer);
        
        // 新增：时间预测信息显示
        CreateTimePredictionDisplay(dangerInfo, infoGrid);
        
        // 位置信息 - 增强显示
        var posContainer = new VisualElement();
        posContainer.style.flexDirection = FlexDirection.Column;
        posContainer.style.marginBottom = 4;
        
        var posTitle = new Label("位置坐标");
        posTitle.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        posTitle.style.fontSize = 11;
        posTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        posTitle.style.marginBottom = 2;
        uiManager?.ApplyFont(posTitle);
        posContainer.Add(posTitle);
        
        var posInfo = new Label($"树木位置: ({dangerInfo.tree.transform.position.x:F1}, {dangerInfo.tree.transform.position.y:F1}, {dangerInfo.tree.transform.position.z:F1})");
        posInfo.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        posInfo.style.fontSize = 10;
        uiManager?.ApplyFont(posInfo);
        posContainer.Add(posInfo);
        
        if (dangerInfo.powerline != null)
        {
            var powerlinePosInfo = new Label($"电力线位置: ({dangerInfo.powerline.transform.position.x:F1}, {dangerInfo.powerline.transform.position.y:F1}, {dangerInfo.powerline.transform.position.z:F1})");
            powerlinePosInfo.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            powerlinePosInfo.style.fontSize = 10;
            uiManager?.ApplyFont(powerlinePosInfo);
            posContainer.Add(powerlinePosInfo);
        }
        
        infoGrid.Add(posContainer);
        
        itemContainer.Add(infoGrid);
        treeListContainer.Add(itemContainer);
    }
    
    void CreateTimePredictionDisplay(TreeDangerMonitor.TreeDangerInfo dangerInfo, VisualElement infoGrid)
    {
        // 一年后预测信息
        var oneYearContainer = new VisualElement();
        oneYearContainer.style.backgroundColor = new Color(0.95f, 0.95f, 1f, 0.5f);
        oneYearContainer.style.paddingTop = 4;
        oneYearContainer.style.paddingBottom = 4;
        oneYearContainer.style.paddingLeft = 6;
        oneYearContainer.style.paddingRight = 6;
        oneYearContainer.style.borderTopLeftRadius = 4;
        oneYearContainer.style.borderTopRightRadius = 4;
        oneYearContainer.style.borderBottomLeftRadius = 4;
        oneYearContainer.style.borderBottomRightRadius = 4;
        oneYearContainer.style.borderLeftWidth = 2;
        oneYearContainer.style.borderLeftColor = new Color(0.4f, 0.4f, 0.8f, 1f);
        oneYearContainer.style.marginBottom = 4;
        
        var oneYearTitle = new Label("一年后预测");
        oneYearTitle.style.color = new Color(0.2f, 0.2f, 0.6f, 1f);
        oneYearTitle.style.fontSize = 11;
        oneYearTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        oneYearTitle.style.marginBottom = 2;
        uiManager?.ApplyFont(oneYearTitle);
        oneYearContainer.Add(oneYearTitle);
        
        var oneYearInfo = new Label($"距离: {dangerInfo.oneYearDistance:F1}m | 等级: {GetDangerLevelString(dangerInfo.oneYearDangerLevel)}");
        oneYearInfo.style.color = new Color(0.4f, 0.4f, 0.6f, 1f);
        oneYearInfo.style.fontSize = 10;
        uiManager?.ApplyFont(oneYearInfo);
        oneYearContainer.Add(oneYearInfo);
        
        var oneYearRisk = new Label(dangerInfo.oneYearRiskDescription);
        oneYearRisk.style.color = new Color(0.5f, 0.5f, 0.7f, 1f);
        oneYearRisk.style.fontSize = 10;
        oneYearRisk.style.whiteSpace = WhiteSpace.Normal;
        uiManager?.ApplyFont(oneYearRisk);
        oneYearContainer.Add(oneYearRisk);
        
        infoGrid.Add(oneYearContainer);
        
        // 三年后预测信息
        var threeYearContainer = new VisualElement();
        threeYearContainer.style.backgroundColor = new Color(1f, 0.95f, 0.95f, 0.5f);
        threeYearContainer.style.paddingTop = 4;
        threeYearContainer.style.paddingBottom = 4;
        threeYearContainer.style.paddingLeft = 6;
        threeYearContainer.style.paddingRight = 6;
        threeYearContainer.style.borderTopLeftRadius = 4;
        threeYearContainer.style.borderTopRightRadius = 4;
        threeYearContainer.style.borderBottomLeftRadius = 4;
        threeYearContainer.style.borderBottomRightRadius = 4;
        threeYearContainer.style.borderLeftWidth = 2;
        threeYearContainer.style.borderLeftColor = new Color(0.8f, 0.4f, 0.4f, 1f);
        threeYearContainer.style.marginBottom = 4;
        
        var threeYearTitle = new Label("三年后预测");
        threeYearTitle.style.color = new Color(0.6f, 0.2f, 0.2f, 1f);
        threeYearTitle.style.fontSize = 11;
        threeYearTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        threeYearTitle.style.marginBottom = 2;
        uiManager?.ApplyFont(threeYearTitle);
        threeYearContainer.Add(threeYearTitle);
        
        var threeYearInfo = new Label($"距离: {dangerInfo.threeYearDistance:F1}m | 等级: {GetDangerLevelString(dangerInfo.threeYearDangerLevel)}");
        threeYearInfo.style.color = new Color(0.6f, 0.4f, 0.4f, 1f);
        threeYearInfo.style.fontSize = 10;
        uiManager?.ApplyFont(threeYearInfo);
        threeYearContainer.Add(threeYearInfo);
        
        var threeYearRisk = new Label(dangerInfo.threeYearRiskDescription);
        threeYearRisk.style.color = new Color(0.7f, 0.5f, 0.5f, 1f);
        threeYearRisk.style.fontSize = 10;
        threeYearRisk.style.whiteSpace = WhiteSpace.Normal;
        uiManager?.ApplyFont(threeYearRisk);
        threeYearContainer.Add(threeYearRisk);
        
        infoGrid.Add(threeYearContainer);
        
        // 趋势分析
        var trendContainer = new VisualElement();
        trendContainer.style.backgroundColor = new Color(0.98f, 0.95f, 0.9f, 0.5f);
        trendContainer.style.paddingTop = 4;
        trendContainer.style.paddingBottom = 4;
        trendContainer.style.paddingLeft = 6;
        trendContainer.style.paddingRight = 6;
        trendContainer.style.borderTopLeftRadius = 4;
        trendContainer.style.borderTopRightRadius = 4;
        trendContainer.style.borderBottomLeftRadius = 4;
        trendContainer.style.borderBottomRightRadius = 4;
        trendContainer.style.borderLeftWidth = 2;
        trendContainer.style.borderLeftColor = new Color(0.8f, 0.6f, 0.2f, 1f);
        trendContainer.style.marginBottom = 4;
        
        var trendTitle = new Label("趋势分析");
        trendTitle.style.color = new Color(0.6f, 0.4f, 0.1f, 1f);
        trendTitle.style.fontSize = 11;
        trendTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        trendTitle.style.marginBottom = 2;
        uiManager?.ApplyFont(trendTitle);
        trendContainer.Add(trendTitle);
        
        string trendText = "";
        if (dangerInfo.willBeDangerousInOneYear && dangerInfo.willBeDangerousInThreeYears)
        {
            trendText = "风险持续上升，需要立即制定管理计划";
            trendContainer.style.borderLeftColor = new Color(0.9f, 0.2f, 0.2f, 1f);
        }
        else if (dangerInfo.willBeDangerousInOneYear)
        {
            trendText = "一年后风险增加，建议提前处理";
            trendContainer.style.borderLeftColor = new Color(0.8f, 0.4f, 0.1f, 1f);
        }
        else if (dangerInfo.willBeDangerousInThreeYears)
        {
            trendText = "三年后可能出现风险，需要长期监测";
            trendContainer.style.borderLeftColor = new Color(1f, 0.6f, 0f, 1f);
        }
        else
        {
            trendText = "风险相对稳定，继续监测即可";
            trendContainer.style.borderLeftColor = new Color(0.2f, 0.7f, 0.2f, 1f);
        }
        
        var trendInfo = new Label(trendText);
        trendInfo.style.color = new Color(0.5f, 0.4f, 0.3f, 1f);
        trendInfo.style.fontSize = 10;
        trendInfo.style.whiteSpace = WhiteSpace.Normal;
        uiManager?.ApplyFont(trendInfo);
        trendContainer.Add(trendInfo);
        
        infoGrid.Add(trendContainer);
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
    
    string GetDangerLevelString(TreeDangerMonitor.TreeDangerLevel level)
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
    /// 将时间间隔转换为友好的字符串显示
    /// </summary>
    string GetTimeAgoString(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
        {
            return $"{(int)timeSpan.TotalDays}天前";
        }
        else if (timeSpan.TotalHours >= 1)
        {
            return $"{(int)timeSpan.TotalHours}小时前";
        }
        else if (timeSpan.TotalMinutes >= 1)
        {
            return $"{(int)timeSpan.TotalMinutes}分钟前";
        }
        else
        {
            return "刚刚";
        }
    }
    
    /// <summary>
    /// 获取系统运行时间
    /// </summary>
    string GetSystemRuntime()
    {
        var runtime = Time.time;
        int hours = (int)(runtime / 3600f);
        int minutes = (int)((runtime % 3600f) / 60f);
        int seconds = (int)(runtime % 60f);
        
        if (hours > 0)
        {
            return $"{hours}小时{minutes}分钟{seconds}秒";
        }
        else if (minutes > 0)
        {
            return $"{minutes}分钟{seconds}秒";
        }
        else
        {
            return $"{seconds}秒";
        }
    }
    
    /// <summary>
    /// 获取当前FPS
    /// </summary>
    float GetFPS()
    {
        return 1.0f / Time.deltaTime;
    }
    
    /// <summary>
    /// 获取内存使用量（MB）
    /// </summary>
    float GetMemoryUsage()
    {
        return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
    }
    
    /// <summary>
    /// 获取场景中的对象数量
    /// </summary>
    int GetSceneObjectCount()
    {
        return FindObjectsOfType<GameObject>().Length;
    }
    

    
    void SyncMonitoringParameters()
    {
        if (treeDangerMonitor == null) return;
        
        treeDangerMonitor.criticalDistance = criticalDistance;
        treeDangerMonitor.warningDistance = warningDistance;
        treeDangerMonitor.safeDistance = safeDistance;
        treeDangerMonitor.baseGrowthRate = baseGrowthRate;
        treeDangerMonitor.maxTreeHeight = maxTreeHeight;
        treeDangerMonitor.seasonalGrowthFactor = seasonalGrowthFactor;
        treeDangerMonitor.powerlineHeight = powerlineHeight;
        treeDangerMonitor.powerlineSag = powerlineSag;
        treeDangerMonitor.windSwayFactor = windSwayFactor;
    }
    
    void UpdateMonitoringParameters()
    {
        if (treeDangerMonitor == null) return;
        
        treeDangerMonitor.SetMonitoringParameters(criticalDistance, warningDistance, safeDistance, baseGrowthRate);
        UpdateStatus("监测参数已更新");
    }
    
    // 公共接口方法
    public void RefreshDisplay()
    {
        if (treeDangerMonitor == null)
        {
            Debug.LogWarning("TreeDangerMonitor未找到，无法刷新显示");
            return;
        }
        
        // 强制刷新树木列表和监测结果
        treeDangerMonitor.ForceRefreshAndMonitor();
        
        // 更新统计信息
        UpdateStatistics();
        
        // 更新树木列表
        UpdateTreeList();
        
        Debug.Log($"显示刷新完成 - 树木总数: {treeDangerMonitor.GetTreeCount()}");
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
        UpdateStatistics();
        UpdateTreeList();
    }
    
    /// <summary>
    /// 自动刷新协程
    /// </summary>
    System.Collections.IEnumerator AutoRefreshCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            
            if (isMonitoring && treeDangerMonitor != null)
            {
                UpdateDisplay();
            }
            
            // 实时更新性能信息
            UpdatePerformanceInfo();
        }
    }
    
    /// <summary>
    /// 更新性能信息显示
    /// </summary>
    void UpdatePerformanceInfo()
    {
        // 实时更新状态栏显示系统性能信息
        if (uiManager != null)
        {
            float fps = GetFPS();
            float memory = GetMemoryUsage();
            int objectCount = GetSceneObjectCount();
            
            string performanceInfo = $"FPS: {fps:F1} | 内存: {memory:F1}MB | 对象: {objectCount}";
            uiManager.UpdateStatusBar($"性能监控: {performanceInfo}");
        }
    }
    
    void GenerateTimePredictionReport()
    {
        if (treeDangerMonitor == null)
        {
            UpdateStatus("TreeDangerMonitor未找到");
            return;
        }
        
        string report = treeDangerMonitor.GetTreeGrowthTrendReport();
        
        // 在控制台输出报告
        Debug.Log("=== 树木生长趋势预测报告 ===");
        Debug.Log(report);
        
        // 更新状态显示
        UpdateStatus("已生成时间预测报告，请查看控制台");
        
        // 在UI中显示简要信息
        if (uiManager != null)
        {
            var oneYearDangerous = treeDangerMonitor.GetOneYearDangerousTrees();
            var threeYearDangerous = treeDangerMonitor.GetThreeYearDangerousTrees();
            
            string summary = $"预测报告: 一年后{oneYearDangerous.Count}棵危险，三年后{threeYearDangerous.Count}棵危险";
            uiManager.UpdateStatusBar(summary);
        }
        
        // 刷新显示
        UpdateStatistics();
        UpdateTreeList();
    }
}

