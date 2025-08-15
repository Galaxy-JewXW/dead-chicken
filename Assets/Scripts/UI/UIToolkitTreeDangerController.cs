using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class UIToolkitTreeDangerController : MonoBehaviour
{
    private SimpleUIToolkitManager uiManager;
    private TreeDangerMonitor treeDangerMonitor;
    private VisualElement treeDangerPanel;
    private VisualElement treeListContainer;
    private Label statusLabel;
    private Label statisticsLabel;
    
    private float criticalDistance = 3f;
    private float warningDistance = 8f;
    private float safeDistance = 15f;
    private float growthRate = 0.1f;
    
    void Start()
    {
        uiManager = FindObjectOfType<SimpleUIToolkitManager>();
        treeDangerMonitor = FindObjectOfType<TreeDangerMonitor>();
        
        if (treeDangerMonitor == null)
        {
            treeDangerMonitor = gameObject.AddComponent<TreeDangerMonitor>();
            Debug.Log("已创建TreeDangerMonitor组件");
        }
        
        Initialize();
    }
    
    public void Initialize()
    {
        if (uiManager == null)
        {
            Debug.LogError("未找到SimpleUIToolkitManager");
            return;
        }
        Debug.Log("UIToolkitTreeDangerController已初始化");
    }
    
    public VisualElement CreateTreeDangerPanel()
    {
        treeDangerPanel = new VisualElement();
        treeDangerPanel.style.width = Length.Percent(100);
        treeDangerPanel.style.height = Length.Percent(100);
        treeDangerPanel.style.flexDirection = FlexDirection.Column;
        
        CreateControlSection();
        CreateStatisticsSection();
        CreateTreeListSection();
        
        return treeDangerPanel;
    }
    
    void CreateControlSection()
    {
        var controlContainer = new VisualElement();
        controlContainer.style.backgroundColor = new Color(0.95f, 0.98f, 1f, 1f);
        controlContainer.style.marginBottom = 10;
        controlContainer.style.paddingTop = 15;
        controlContainer.style.paddingBottom = 15;
        controlContainer.style.paddingLeft = 15;
        controlContainer.style.paddingRight = 15;
        controlContainer.style.borderTopLeftRadius = 8;
        controlContainer.style.borderTopRightRadius = 8;
        controlContainer.style.borderBottomLeftRadius = 8;
        controlContainer.style.borderBottomRightRadius = 8;
        controlContainer.style.borderLeftWidth = 2;
        controlContainer.style.borderRightWidth = 2;
        controlContainer.style.borderTopWidth = 2;
        controlContainer.style.borderBottomWidth = 2;
        controlContainer.style.borderLeftColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        controlContainer.style.borderRightColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        controlContainer.style.borderTopColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        controlContainer.style.borderBottomColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        controlContainer.style.flexShrink = 0;
        controlContainer.style.flexGrow = 0;
        
        var titleLabel = new Label("树木危险监测系统");
        titleLabel.style.color = new Color(0.1f, 0.5f, 0.1f, 1f);
        titleLabel.style.fontSize = 18;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 15;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(titleLabel);
        controlContainer.Add(titleLabel);
        
        CreateParameterControls(controlContainer);
        CreateControlButtons(controlContainer);
        
        statusLabel = new Label("系统就绪，等待监测数据...");
        statusLabel.style.color = new Color(0.7f, 0.4f, 0.1f, 1f);
        statusLabel.style.fontSize = 12;
        statusLabel.style.marginTop = 10;
        statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        statusLabel.style.backgroundColor = new Color(1f, 1f, 0.8f, 1f);
        statusLabel.style.paddingTop = 5;
        statusLabel.style.paddingBottom = 5;
        uiManager?.ApplyFont(statusLabel);
        controlContainer.Add(statusLabel);
        
        treeDangerPanel.Add(controlContainer);
    }
    
    void CreateParameterControls(VisualElement parent)
    {
        var distanceContainer = new VisualElement();
        distanceContainer.style.flexDirection = FlexDirection.Row;
        distanceContainer.style.justifyContent = Justify.SpaceBetween;
        distanceContainer.style.marginBottom = 10;
        
        var criticalContainer = new VisualElement();
        var criticalLabel = new Label("危险距离:");
        criticalLabel.style.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        criticalLabel.style.fontSize = 12;
        criticalLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        uiManager?.ApplyFont(criticalLabel);
        criticalContainer.Add(criticalLabel);
        
        var criticalField = new FloatField();
        criticalField.value = criticalDistance;
        criticalField.style.width = 60;
        criticalField.RegisterValueChangedCallback(evt => {
            criticalDistance = evt.newValue;
            UpdateMonitoringParameters();
        });
        criticalContainer.Add(criticalField);
        distanceContainer.Add(criticalContainer);
        
        var warningContainer = new VisualElement();
        var warningLabel = new Label("警告距离:");
        warningLabel.style.color = new Color(1f, 0.6f, 0f, 1f);
        warningLabel.style.fontSize = 12;
        warningLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        uiManager?.ApplyFont(warningLabel);
        warningContainer.Add(warningLabel);
        
        var warningField = new FloatField();
        warningField.value = warningDistance;
        warningField.style.width = 60;
        warningField.RegisterValueChangedCallback(evt => {
            warningDistance = evt.newValue;
            UpdateMonitoringParameters();
        });
        warningContainer.Add(warningField);
        distanceContainer.Add(warningContainer);
        
        var safeContainer = new VisualElement();
        var safeLabel = new Label("安全距离:");
        safeLabel.style.color = new Color(0.2f, 0.7f, 0.2f, 1f);
        safeLabel.style.fontSize = 12;
        safeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        uiManager?.ApplyFont(safeLabel);
        safeContainer.Add(safeLabel);
        
        var safeField = new FloatField();
        safeField.value = safeDistance;
        safeField.style.width = 60;
        safeField.RegisterValueChangedCallback(evt => {
            safeDistance = evt.newValue;
            UpdateMonitoringParameters();
        });
        safeContainer.Add(safeField);
        distanceContainer.Add(safeContainer);
        
        parent.Add(distanceContainer);
        
        var growthContainer = new VisualElement();
        growthContainer.style.flexDirection = FlexDirection.Row;
        growthContainer.style.justifyContent = Justify.SpaceBetween;
        growthContainer.style.marginBottom = 15;
        
        var growthLabel = new Label("基础生长率 (m/年):");
        growthLabel.style.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        growthLabel.style.fontSize = 12;
        growthLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        uiManager?.ApplyFont(growthLabel);
        growthContainer.Add(growthLabel);
        
        var growthField = new FloatField();
        growthField.value = growthRate;
        growthField.style.width = 80;
        growthField.RegisterValueChangedCallback(evt => {
            growthRate = evt.newValue;
            UpdateMonitoringParameters();
        });
        growthContainer.Add(growthField);
        
        parent.Add(growthContainer);
    }
    
    void CreateControlButtons(VisualElement parent)
    {
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.justifyContent = Justify.SpaceBetween;
        buttonContainer.style.marginBottom = 10;
        
        var startButton = new VisualElement();
        startButton.style.width = 100;
        startButton.style.height = 35;
        startButton.style.backgroundColor = new Color(0.2f, 0.7f, 0.2f, 1f);
        startButton.style.justifyContent = Justify.Center;
        startButton.style.alignItems = Align.Center;
        startButton.style.borderTopLeftRadius = 5;
        startButton.style.borderTopRightRadius = 5;
        startButton.style.borderBottomLeftRadius = 5;
        startButton.style.borderBottomRightRadius = 5;
        
        var startLabel = new Label("开始监测");
        startLabel.style.color = Color.white;
        startLabel.style.fontSize = 14;
        startLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(startLabel);
        startButton.Add(startLabel);
        startButton.RegisterCallback<ClickEvent>(evt => StartMonitoring());
        buttonContainer.Add(startButton);
        
        var manualButton = new VisualElement();
        manualButton.style.width = 100;
        manualButton.style.height = 35;
        manualButton.style.backgroundColor = new Color(0.2f, 0.6f, 0.8f, 1f);
        manualButton.style.justifyContent = Justify.Center;
        manualButton.style.alignItems = Align.Center;
        manualButton.style.borderTopLeftRadius = 5;
        manualButton.style.borderTopRightRadius = 5;
        manualButton.style.borderBottomLeftRadius = 5;
        manualButton.style.borderBottomRightRadius = 5;
        
        var manualLabel = new Label("手动监测");
        manualLabel.style.color = Color.white;
        manualLabel.style.fontSize = 14;
        manualLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(manualLabel);
        manualButton.Add(manualLabel);
        manualButton.RegisterCallback<ClickEvent>(evt => ManualMonitoring());
        buttonContainer.Add(manualButton);
        
        var clearButton = new VisualElement();
        clearButton.style.width = 100;
        clearButton.style.height = 35;
        clearButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        clearButton.style.justifyContent = Justify.Center;
        clearButton.style.alignItems = Align.Center;
        clearButton.style.borderTopLeftRadius = 5;
        clearButton.style.borderTopRightRadius = 5;
        clearButton.style.borderBottomLeftRadius = 5;
        clearButton.style.borderBottomRightRadius = 5;
        
        var clearLabel = new Label("清除标记");
        clearLabel.style.color = Color.white;
        clearLabel.style.fontSize = 14;
        clearLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(clearLabel);
        clearButton.Add(clearLabel);
        clearButton.RegisterCallback<ClickEvent>(evt => ClearAllMarkers());
        buttonContainer.Add(clearButton);
        
        parent.Add(buttonContainer);
    }
    
    void CreateStatisticsSection()
    {
        var statsContainer = new VisualElement();
        statsContainer.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        statsContainer.style.marginBottom = 10;
        statsContainer.style.paddingTop = 15;
        statsContainer.style.paddingBottom = 15;
        statsContainer.style.paddingLeft = 15;
        statsContainer.style.paddingRight = 15;
        statsContainer.style.borderTopLeftRadius = 8;
        statsContainer.style.borderTopRightRadius = 8;
        statsContainer.style.borderBottomLeftRadius = 8;
        statsContainer.style.borderBottomRightRadius = 8;
        statsContainer.style.borderLeftWidth = 2;
        statsContainer.style.borderRightWidth = 2;
        statsContainer.style.borderTopWidth = 2;
        statsContainer.style.borderBottomWidth = 2;
        statsContainer.style.borderLeftColor = new Color(0.8f, 0.6f, 0.2f, 1f);
        statsContainer.style.borderRightColor = new Color(0.8f, 0.6f, 0.2f, 1f);
        statsContainer.style.borderTopColor = new Color(0.8f, 0.6f, 0.2f, 1f);
        statsContainer.style.borderBottomColor = new Color(0.8f, 0.6f, 0.2f, 1f);
        statsContainer.style.flexShrink = 0;
        statsContainer.style.flexGrow = 0;
        
        var statsTitle = new Label("监测统计");
        statsTitle.style.color = new Color(0.6f, 0.4f, 0.1f, 1f);
        statsTitle.style.fontSize = 16;
        statsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        statsTitle.style.marginBottom = 10;
        uiManager?.ApplyFont(statsTitle);
        statsContainer.Add(statsTitle);
        
        statisticsLabel = new Label("暂无数据");
        statisticsLabel.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        statisticsLabel.style.fontSize = 12;
        statisticsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(statisticsLabel);
        statsContainer.Add(statisticsLabel);
        
        treeDangerPanel.Add(statsContainer);
    }
    
    void CreateTreeListSection()
    {
        var listContainer = new VisualElement();
        listContainer.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        listContainer.style.paddingTop = 15;
        listContainer.style.paddingBottom = 15;
        listContainer.style.paddingLeft = 15;
        listContainer.style.paddingRight = 15;
        listContainer.style.borderTopLeftRadius = 8;
        listContainer.style.borderTopRightRadius = 8;
        listContainer.style.borderBottomLeftRadius = 8;
        listContainer.style.borderBottomRightRadius = 8;
        listContainer.style.borderLeftWidth = 2;
        listContainer.style.borderRightWidth = 2;
        listContainer.style.borderTopWidth = 2;
        listContainer.style.borderBottomWidth = 2;
        listContainer.style.borderLeftColor = new Color(0.3f, 0.7f, 1f, 1f);
        listContainer.style.borderRightColor = new Color(0.3f, 0.7f, 1f, 1f);
        listContainer.style.borderTopColor = new Color(0.3f, 0.7f, 1f, 1f);
        listContainer.style.borderBottomColor = new Color(0.3f, 0.7f, 1f, 1f);
        listContainer.style.flexGrow = 1;
        listContainer.style.flexShrink = 1;
        
        var listTitle = new Label("危险树木列表");
        listTitle.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
        listTitle.style.fontSize = 16;
        listTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        listTitle.style.marginBottom = 8;
        uiManager?.ApplyFont(listTitle);
        listContainer.Add(listTitle);
        
        var scrollView = new ScrollView();
        scrollView.style.minHeight = 300;
        scrollView.style.maxHeight = 1000;
        scrollView.style.flexGrow = 1;
        scrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
        scrollView.style.overflow = Overflow.Hidden;
        scrollView.scrollDecelerationRate = 0.9f;
        
        scrollView.RegisterCallback<WheelEvent>(evt =>
        {
            scrollView.scrollOffset += new Vector2(0, evt.delta.y * 200f);
            evt.StopPropagation();
        });
        
        treeListContainer = new VisualElement();
        treeListContainer.style.flexDirection = FlexDirection.Column;
        treeListContainer.style.flexShrink = 0;
        
        scrollView.Add(treeListContainer);
        listContainer.Add(scrollView);
        
        treeDangerPanel.Add(listContainer);
    }
    
    void StartMonitoring()
    {
        if (treeDangerMonitor == null)
        {
            Debug.LogError("TreeDangerMonitor未找到");
            return;
        }
        
        treeDangerMonitor.enableAutoMonitoring = true;
        UpdateStatus("自动监测已启动");
        UpdateStatistics();
        
        if (uiManager != null)
        {
            uiManager.UpdateStatusBar("树木危险监测系统已启动");
        }
    }
    
    void ManualMonitoring()
    {
        if (treeDangerMonitor == null)
        {
            Debug.LogError("TreeDangerMonitor未找到");
            return;
        }
        
        treeDangerMonitor.ManualMonitoring();
        UpdateStatus("手动监测完成");
        UpdateStatistics();
        UpdateTreeList();
        
        if (uiManager != null)
        {
            uiManager.UpdateStatusBar("手动监测完成");
        }
    }
    
    void ClearAllMarkers()
    {
        if (treeDangerMonitor == null)
        {
            Debug.LogError("TreeDangerMonitor未找到");
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
    
    void UpdateMonitoringParameters()
    {
        if (treeDangerMonitor == null) return;
        
        treeDangerMonitor.SetMonitoringParameters(criticalDistance, warningDistance, safeDistance, growthRate);
        UpdateStatus("监测参数已更新");
    }
    
    void UpdateStatus(string message)
    {
        if (statusLabel != null)
        {
            statusLabel.text = message;
        }
    }
    
    void UpdateStatistics()
    {
        if (treeDangerMonitor == null || statisticsLabel == null) return;
        
        var stats = treeDangerMonitor.GetDangerStatistics();
        if (stats.Count > 0)
        {
            string statsText = $"安全: {stats[TreeDangerMonitor.TreeDangerLevel.Safe]}\n" +
                              $"警告: {stats[TreeDangerMonitor.TreeDangerLevel.Warning]}\n" +
                              $"危险: {stats[TreeDangerMonitor.TreeDangerLevel.Critical]}\n" +
                              $"紧急: {stats[TreeDangerMonitor.TreeDangerLevel.Emergency]}";
            
            statisticsLabel.text = statsText;
        }
        else
        {
            statisticsLabel.text = "暂无数据";
        }
    }
    
    void UpdateTreeList()
    {
        if (treeDangerMonitor == null || treeListContainer == null) return;
        
        treeListContainer.Clear();
        
        var allDangerInfo = treeDangerMonitor.GetAllDangerInfo();
        if (allDangerInfo == null || allDangerInfo.Count == 0)
        {
            var noDataLabel = new Label("暂无危险树木");
            noDataLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            noDataLabel.style.fontSize = 12;
            noDataLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            noDataLabel.style.paddingTop = 20;
            noDataLabel.style.paddingBottom = 20;
            uiManager?.ApplyFont(noDataLabel);
            treeListContainer.Add(noDataLabel);
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
        
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        
        var jumpButton = new VisualElement();
        jumpButton.style.width = 50;
        jumpButton.style.height = 24;
        jumpButton.style.marginRight = 5;
        jumpButton.style.backgroundColor = new Color(0.2f, 0.7f, 0.2f, 1f);
        jumpButton.style.justifyContent = Justify.Center;
        jumpButton.style.alignItems = Align.Center;
        jumpButton.style.borderTopLeftRadius = 3;
        jumpButton.style.borderTopRightRadius = 3;
        jumpButton.style.borderBottomLeftRadius = 3;
        jumpButton.style.borderBottomRightRadius = 3;
        
        var jumpButtonLabel = new Label("跳转");
        jumpButtonLabel.style.color = Color.white;
        jumpButtonLabel.style.fontSize = 11;
        jumpButtonLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        uiManager?.ApplyFont(jumpButtonLabel);
        jumpButton.Add(jumpButtonLabel);
        
        jumpButton.RegisterCallback<ClickEvent>(evt => JumpToTree(dangerInfo));
        buttonContainer.Add(jumpButton);
        
        titleRow.Add(buttonContainer);
        itemContainer.Add(titleRow);
        
        var infoGrid = new VisualElement();
        infoGrid.style.flexDirection = FlexDirection.Column;
        
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
        
        var distanceInfo = new Label($"当前距离: {dangerInfo.currentDistance:F1}m, 预测距离: {dangerInfo.projectedDistance:F1}m");
        distanceInfo.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        distanceInfo.style.fontSize = 12;
        distanceInfo.style.marginBottom = 4;
        uiManager?.ApplyFont(distanceInfo);
        infoGrid.Add(distanceInfo);
        
        var treeInfo = new Label($"高度: {dangerInfo.treeHeight:F1}m, 生长率: {dangerInfo.growthRate:F3}m/年");
        treeInfo.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        treeInfo.style.fontSize = 12;
        treeInfo.style.marginBottom = 4;
        uiManager?.ApplyFont(treeInfo);
        infoGrid.Add(treeInfo);
        
        var riskInfo = new Label($"风险: {dangerInfo.riskDescription}");
        riskInfo.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        riskInfo.style.fontSize = 11;
        riskInfo.style.whiteSpace = WhiteSpace.Normal;
        riskInfo.style.marginBottom = 4;
        uiManager?.ApplyFont(riskInfo);
        infoGrid.Add(riskInfo);
        
        var posInfo = new Label($"位置: ({dangerInfo.tree.transform.position.x:F1}, {dangerInfo.tree.transform.position.y:F1}, {dangerInfo.tree.transform.position.z:F1})");
        posInfo.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        posInfo.style.fontSize = 11;
        infoGrid.Add(posInfo);
        
        itemContainer.Add(infoGrid);
        treeListContainer.Add(itemContainer);
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
            // 使用主相机直接跳转
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
    
    public void RefreshDisplay()
    {
        UpdateStatistics();
        UpdateTreeList();
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
}
