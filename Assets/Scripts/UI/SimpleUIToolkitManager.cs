using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections;
using UI;
using PowerlineSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 简化的UI Toolkit管理器 - 电力线可视化系统
/// </summary>
public class SimpleUIToolkitManager : MonoBehaviour
{
    [Header("UI设置")]
    public bool enableUI = true;
    
    [Header("字体设置")]
    [Tooltip("备用字体资源，拖拽字体到此处确保文字正常显示")]
    public Font fallbackFont;
    
    [Header("颜色主题")]
    public Color primaryColor = new Color(0.39f, 0.4f, 0.95f, 1f); // 蓝紫色主题
    
    [Header("界面控制")]
    public bool showSidePanel = true; // 是否显示侧边面板
    
    private UIDocument uiDocument;
    private VisualElement rootElement;
    private VisualElement topBar;
    private VisualElement mainContent;

    private VisualElement sidebar;
    private Label centerLabel;
    private VisualElement powerlineInfoPanel;
    private Label powerlineInfoLabel;
    
    public enum UIMode
    {
        Normal,
        Camera,
        Measure,
        Danger,
        Powerline,
        TowerOverview,
        SceneOverview, // 添加场景总览模式
        PointCloud, // 添加点云模式
        StatisticsDashboard // 添加统计大屏模式
    }
    
    public UIMode currentMode = UIMode.Normal;
    
    // 无人机巡检按钮引用
    private VisualElement dronePatrolButtonContainer = null;
    private Label dronePatrolButtonLabel = null;
    private float lastButtonUpdateTime = 0f;
    private const float BUTTON_UPDATE_INTERVAL = 0.1f; // 每0.1秒更新一次按钮状态
    
    // 面板切换按钮引用
    private Label toggleButtonLabel = null;
    
    // 点云控制器
    private PointCloudUIController pointCloudController;
    
    // 电力线标记系统
    private PowerlineMarkingSystem powerlineMarkingSystem;
    
    // 统计大屏管理器
    private StatisticsDashboardManager statisticsDashboardManager;


    
    [Header("初始界面设置")]
    [Tooltip("是否在启动时显示初始界面")]
    public bool showInitialInterfaceOnStart = true;
    
    // 初始界面管理器
    private InitialInterfaceManager initialInterfaceManager;
    
    void Start()
    {
        // 初始化初始界面管理器
        InitializeInitialInterfaceManager();
        
        // 如果显示初始界面，则完全隐藏主UI
        if (showInitialInterfaceOnStart && initialInterfaceManager != null)
        {
            // 延迟显示初始界面，完全隐藏主UI
            StartCoroutine(ShowInitialInterfaceDelayed());
            return; // 不初始化主UI，等待用户选择
        }
        
        // 只有在不显示初始界面时才初始化主UI
        InitializeMainUI();
    }
    
    /// <summary>
    /// 初始化主UI（只在用户选择后调用）
    /// </summary>
    public void InitializeMainUI()
    {
        showSidePanel = true; // 强制显示侧边栏，防止Inspector设置失误
        
        if (enableUI)
        {
            InitializeUI();
        }
        HidePowerlineInfo(); // 启动时隐藏属性面板
        
        var towerManager = GetComponent<TowerOverviewManager>();
        // 初始化电塔总览UI
        if (towerManager == null)
        {
            towerManager = gameObject.AddComponent<TowerOverviewManager>();
        }
        
        // 初始化场景总览管理器，使用和TowerOverviewManager相同的方式
        var sceneManager = GetComponent<SceneOverviewManager>();
        if (sceneManager == null)
        {
            // 延迟添加组件，避免编译顺序问题
            StartCoroutine(AddSceneOverviewManagerDelayed());
        }
        
        // 初始化点云控制器
        InitializePointCloudController();
        
        // 初始化电力线标记系统
        InitializePowerlineMarkingSystem();
        
        // 初始化统计大屏系统
        InitializeStatisticsDashboard();
        
        // 初始化天空盒恢复器
        InitializeSkyboxRestorer();
        
        // 初始化UI清理器
        InitializeUICleanup();
        
        // 初始化地形系统
        InitializeTerrainSystem();
        
        // 强制初始化地形（确保在打包的exe中正常工作）
        ForceInitializeTerrain();
        
        // 切换到正常模式
        SwitchMode(UIMode.Normal);
    }
    
    /// <summary>
    /// 延迟显示初始界面
    /// </summary>
    System.Collections.IEnumerator ShowInitialInterfaceDelayed()
    {
        // 等待一帧，确保所有UI组件都已初始化
        yield return new WaitForEndOfFrame();
        
        // 显示初始界面（使用独立的UIDocument）
        if (initialInterfaceManager != null)
        {
            initialInterfaceManager.ShowInitialInterface();
            Debug.Log("初始界面已显示");
        }
    }
    
    /// <summary>
    /// 初始化初始界面管理器
    /// </summary>
    void InitializeInitialInterfaceManager()
    {
        // 查找现有的初始界面管理器
        initialInterfaceManager = FindObjectOfType<InitialInterfaceManager>();
        
        if (initialInterfaceManager == null)
        {
            // 创建初始界面管理器
            GameObject initialManagerObj = new GameObject("InitialInterfaceManager");
            initialInterfaceManager = initialManagerObj.AddComponent<InitialInterfaceManager>();
            Debug.Log("已创建初始界面管理器");
        }
        
        // 配置初始界面管理器
        if (initialInterfaceManager != null)
        {
            initialInterfaceManager.uiManager = this;
            initialInterfaceManager.sceneInitializer = FindObjectOfType<SceneInitializer>();
            initialInterfaceManager.powerLineExtractorManager = FindObjectOfType<PowerLineExtractorManager>();
        }
    }
    
    /// <summary>
    /// 延迟初始化场景总览查看器
    /// </summary>
    System.Collections.IEnumerator AddSceneOverviewManagerDelayed()
    {
        yield return new WaitForEndOfFrame();
        
        // 确保当前GameObject上有SceneOverviewManager组件
        var sceneOverviewManager = GetComponent<SceneOverviewManager>();
        if (sceneOverviewManager == null)
        {
            sceneOverviewManager = gameObject.AddComponent<SceneOverviewManager>();
            Debug.Log("SceneOverviewManager 组件已添加");
        }
        else
        {
            Debug.Log("SceneOverviewManager 组件已存在");
        }
    }
    
    /// <summary>
    /// 延迟更新切换按钮文字
    /// </summary>
    System.Collections.IEnumerator UpdateToggleButtonTextDelayed()
    {
        yield return new WaitForEndOfFrame();
        
        // 更新切换按钮文字
        if (toggleButtonLabel != null)
        {
            toggleButtonLabel.text = showSidePanel ? "隐藏侧栏" : "显示侧栏";
        }
    }
    
    /// <summary>
    /// 初始化点云控制器
    /// </summary>
    void InitializePointCloudController()
    {
        // 查找或创建点云控制器
        pointCloudController = GetComponent<PointCloudUIController>();
        if (pointCloudController == null)
        {
            pointCloudController = gameObject.AddComponent<PointCloudUIController>();
            Debug.Log("PointCloudUIController 组件已添加");
        }
        
        // 设置UI字体
        if (fallbackFont != null)
        {
            pointCloudController.uiFont = fallbackFont;
        }
        
        // 确保点云管理器已初始化
        EnsurePointCloudManagerExists();
    }
    
    /// <summary>
    /// 确保点云管理器存在
    /// </summary>
    void EnsurePointCloudManagerExists()
    {
        // 查找现有的点云管理器
        var pointCloudManager = FindObjectOfType<PowerlineSystem.PowerlinePointCloudManager>();
        
        if (pointCloudManager == null)
        {
            // 查找或创建点云自动初始化器
            var autoInitializer = FindObjectOfType<PointCloudAutoInitializer>();
            if (autoInitializer == null)
            {
                // 创建自动初始化器
                GameObject initializerObj = new GameObject("PointCloudAutoInitializer");
                autoInitializer = initializerObj.AddComponent<PointCloudAutoInitializer>();
                Debug.Log("已创建点云自动初始化器");
            }
            
            // 触发初始化
            autoInitializer.InitializePointCloudSystem();
            pointCloudManager = autoInitializer.GetPointCloudManager();
        }
        
        // 连接点云控制器和管理器
        if (pointCloudController != null && pointCloudManager != null)
        {
            pointCloudController.pointCloudManager = pointCloudManager;
            Debug.Log("已连接点云控制器和点云管理器");
        }
    }
    
    /// <summary>
    /// 初始化电力线标记系统
    /// </summary>
    void InitializePowerlineMarkingSystem()
    {
        // 查找现有的电力线标记系统
        powerlineMarkingSystem = FindObjectOfType<PowerlineMarkingSystem>();
        
        if (powerlineMarkingSystem == null)
        {
            // 创建电力线标记系统
            GameObject markingSystemObj = new GameObject("PowerlineMarkingSystem");
            powerlineMarkingSystem = markingSystemObj.AddComponent<PowerlineMarkingSystem>();
            Debug.Log("已创建电力线标记系统");
        }
    }
    
    /// <summary>
    /// 初始化统计大屏系统
    /// </summary>
    void InitializeStatisticsDashboard()
    {
        // 查找现有的统计大屏管理器
        statisticsDashboardManager = FindObjectOfType<StatisticsDashboardManager>();
        
        if (statisticsDashboardManager == null)
        {
            // 创建统计大屏管理器
            GameObject statisticsObj = new GameObject("StatisticsDashboardManager");
            statisticsDashboardManager = statisticsObj.AddComponent<StatisticsDashboardManager>();
            Debug.Log("已创建统计大屏管理器");
        }
    }
    
    /// <summary>
    /// 初始化天空盒恢复器
    /// </summary>
    void InitializeSkyboxRestorer()
    {
        // 查找现有的天空盒恢复器
        var skyboxRestorer = FindObjectOfType<SkyboxRestorer>();
        
        if (skyboxRestorer == null)
        {
            // 创建天空盒恢复器
            GameObject restorerObj = new GameObject("SkyboxRestorer");
            skyboxRestorer = restorerObj.AddComponent<SkyboxRestorer>();
            Debug.Log("已创建天空盒恢复器");
        }
        
        // 立即恢复天空盒
        skyboxRestorer.RestoreSkybox();
    }
    
    /// <summary>
    /// 初始化UI清理器
    /// </summary>
    void InitializeUICleanup()
    {
        // 查找现有的UI清理器
        var uiCleanup = FindObjectOfType<UICleanupHelper>();
        
        if (uiCleanup == null)
        {
            // 创建UI清理器
            GameObject cleanupObj = new GameObject("UICleanupHelper");
            uiCleanup = cleanupObj.AddComponent<UICleanupHelper>();
            Debug.Log("已创建UI清理器");
        }
    }
    
    /// <summary>
    /// 初始化地形系统
    /// </summary>
    void InitializeTerrainSystem()
    {
        Debug.Log("初始化地形系统...");
        
        // 查找现有的地形自动初始化器
        var terrainAutoInitializer = FindObjectOfType<TerrainAutoInitializer>();
        
        if (terrainAutoInitializer == null)
        {
            // 创建地形自动初始化器
            GameObject terrainInitializerObj = new GameObject("TerrainAutoInitializer");
            terrainAutoInitializer = terrainInitializerObj.AddComponent<TerrainAutoInitializer>();
            Debug.Log("已创建地形自动初始化器");
        }
        
        // 初始化地形系统
        terrainAutoInitializer.InitializeTerrainSystem();
        
        Debug.Log("地形系统初始化完成");
    }
    
    /// <summary>
    /// 强制初始化地形（确保在打包的exe中正常工作）
    /// </summary>
    void ForceInitializeTerrain()
    {
        Debug.Log("强制初始化地形...");
        
        // 查找现有的地形强制初始化器
        var terrainForceInitializer = FindObjectOfType<TerrainForceInitializer>();
        
        if (terrainForceInitializer == null)
        {
            // 创建地形强制初始化器
            GameObject forceInitializerObj = new GameObject("TerrainForceInitializer");
            terrainForceInitializer = forceInitializerObj.AddComponent<TerrainForceInitializer>();
            Debug.Log("已创建地形强制初始化器");
        }
        
        // 强制初始化地形
        terrainForceInitializer.ForceInitializeTerrain();
        
        Debug.Log("地形强制初始化完成");
    }
    
    void InitializeUI()
    {
        // 获取或创建UI Document
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            uiDocument = gameObject.AddComponent<UIDocument>();
        }
        
        // 创建Panel Settings
        if (uiDocument.panelSettings == null)
        {
            CreatePanelSettings();
        }
        
        CreateRootStructure();
        SwitchMode(UIMode.Normal);
        
        // 延迟更新切换按钮文字，确保UI完全初始化
        StartCoroutine(UpdateToggleButtonTextDelayed());
    }
    
    void CreatePanelSettings()
    {
        var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
        panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panelSettings.scale = 1.0f;
        panelSettings.referenceResolution = new Vector2Int(1920, 1080);
        panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        panelSettings.match = 0.5f;
        panelSettings.sortingOrder = 1;
        panelSettings.clearDepthStencil = true;
        panelSettings.clearColor = false;
        
        uiDocument.panelSettings = panelSettings;
        
        // 确保UI Document可以接收输入
        if (uiDocument.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.focusable = false;
            uiDocument.rootVisualElement.pickingMode = PickingMode.Position;
        }
    }
    
    void CreateRootStructure()
    {
        rootElement = new VisualElement();
        rootElement.style.flexDirection = FlexDirection.Column;
        rootElement.style.width = Length.Percent(100);
        rootElement.style.height = Length.Percent(100);
        rootElement.style.flexGrow = 1;
        rootElement.style.backgroundColor = StyleKeyword.None; // 透明无色
        // 确保根元素可以传递输入事件
        rootElement.focusable = false;
        rootElement.pickingMode = PickingMode.Position;
        
        CreateTopBar();
        CreateMainContent();
        
        uiDocument.rootVisualElement.Clear();
        uiDocument.rootVisualElement.style.flexDirection = FlexDirection.Column;
        uiDocument.rootVisualElement.style.width = Length.Percent(100);
        uiDocument.rootVisualElement.style.height = Length.Percent(100);
        uiDocument.rootVisualElement.style.flexGrow = 1;
        uiDocument.rootVisualElement.style.backgroundColor = StyleKeyword.None; // 透明无色
        uiDocument.rootVisualElement.Add(rootElement);
    }
    
    void CreateTopBar()
    {
        topBar = new VisualElement();
        topBar.style.flexDirection = FlexDirection.Row;
        topBar.style.justifyContent = Justify.SpaceBetween;
        topBar.style.alignItems = Align.Center;
        topBar.style.height = 60;
        topBar.style.backgroundColor = primaryColor;
        topBar.style.paddingLeft = 20;
        topBar.style.paddingRight = 20;
        
        // 标题
        var titleLabel = new Label("基于机载LiDAR点云的电力线提取与三维重建系统");
        titleLabel.style.color = Color.white;
        titleLabel.style.fontSize = 22;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.whiteSpace = WhiteSpace.Normal;
        ApplyFont(titleLabel);
        topBar.Add(titleLabel);
        
        // 功能按钮容器
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        
        // 首页按钮 - 回到初始信息界面
        CreateStyledButton("首页", () => SwitchMode(UIMode.Normal), buttonContainer);
        
        CreateStyledButton("相机", () => SwitchMode(UIMode.Camera), buttonContainer);
        CreateStyledButton("测量", () => SwitchMode(UIMode.Measure), buttonContainer);
        CreateStyledButton("危险物", () => SwitchMode(UIMode.Danger), buttonContainer);
        CreateStyledButton("电力线", () => SwitchMode(UIMode.Powerline), buttonContainer);
        CreateStyledButton("点云", () => SwitchMode(UIMode.PointCloud), buttonContainer);
        CreateStyledButton("统计大屏", () => SwitchMode(UIMode.StatisticsDashboard), buttonContainer);
        CreateDronePatrolButton(buttonContainer); // 创建特殊的无人机巡检按钮
        
        // 总览按钮容器
        var overviewContainer = new VisualElement();
        overviewContainer.style.flexDirection = FlexDirection.Row;
        overviewContainer.style.marginLeft = 5;
        
        // 场景总览按钮
        var sceneOverviewButton = new Button(() => {
            ToggleSceneOverview();
        });
        sceneOverviewButton.text = "场景总览";
        sceneOverviewButton.style.marginRight = 3;
        sceneOverviewButton.style.width = 100; // 增加宽度到100px以确保文字不换行
        sceneOverviewButton.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        sceneOverviewButton.style.color = Color.black;
        sceneOverviewButton.style.borderBottomLeftRadius = 5;
        sceneOverviewButton.style.borderBottomRightRadius = 5;
        sceneOverviewButton.style.borderTopLeftRadius = 5;
        sceneOverviewButton.style.borderTopRightRadius = 5;
        sceneOverviewButton.style.borderBottomWidth = 1;
        sceneOverviewButton.style.borderTopWidth = 1;
        sceneOverviewButton.style.borderLeftWidth = 1;
        sceneOverviewButton.style.borderRightWidth = 1;
        sceneOverviewButton.style.borderBottomColor = primaryColor;
        sceneOverviewButton.style.borderTopColor = primaryColor;
        sceneOverviewButton.style.borderLeftColor = primaryColor;
        sceneOverviewButton.style.borderRightColor = primaryColor;
        sceneOverviewButton.style.paddingLeft = 8;
        sceneOverviewButton.style.paddingRight = 8;
        sceneOverviewButton.style.paddingTop = 6;
        sceneOverviewButton.style.paddingBottom = 6;
        sceneOverviewButton.style.height = 40;
        sceneOverviewButton.style.fontSize = 13; // 稍微减小字体大小以确保文字不换行
        sceneOverviewButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        sceneOverviewButton.style.whiteSpace = WhiteSpace.NoWrap; // 强制文字不换行
        ApplyFont(sceneOverviewButton);
        
        // 添加悬停效果
        sceneOverviewButton.RegisterCallback<MouseEnterEvent>(evt => {
            sceneOverviewButton.style.backgroundColor = primaryColor;
            sceneOverviewButton.style.color = Color.white;
        });
        
        sceneOverviewButton.RegisterCallback<MouseLeaveEvent>(evt => {
            sceneOverviewButton.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            sceneOverviewButton.style.color = Color.black;
        });
        
        // 电塔总览按钮
        CreateStyledButton("电塔总览", () => SwitchMode(UIMode.TowerOverview), overviewContainer);
        
        overviewContainer.Add(sceneOverviewButton);
        
        var mainButtonContainer = new VisualElement();
        mainButtonContainer.style.flexDirection = FlexDirection.Row;
        mainButtonContainer.Add(buttonContainer);
        mainButtonContainer.Add(overviewContainer);
        
        // 添加退出程序按钮（放在最右侧）
        var exitButton = new Button(() => {
            ShowExitConfirmationDialog();
        });
        exitButton.text = "退出程序";
        exitButton.style.marginLeft = 10;
        exitButton.style.width = 100;
        exitButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f); // 红色
        exitButton.style.color = Color.white;
        exitButton.style.borderBottomLeftRadius = 5;
        exitButton.style.borderBottomRightRadius = 5;
        exitButton.style.borderTopLeftRadius = 5;
        exitButton.style.borderTopRightRadius = 5;
        exitButton.style.borderBottomWidth = 1;
        exitButton.style.borderTopWidth = 1;
        exitButton.style.borderLeftWidth = 1;
        exitButton.style.borderRightWidth = 1;
        exitButton.style.borderBottomColor = new Color(0.7f, 0.1f, 0.1f, 1f);
        exitButton.style.borderTopColor = new Color(0.7f, 0.1f, 0.1f, 1f);
        exitButton.style.borderLeftColor = new Color(0.7f, 0.1f, 0.1f, 1f);
        exitButton.style.borderRightColor = new Color(0.7f, 0.1f, 0.1f, 1f);
        exitButton.style.paddingLeft = 8;
        exitButton.style.paddingRight = 8;
        exitButton.style.paddingTop = 6;
        exitButton.style.paddingBottom = 6;
        exitButton.style.height = 40;
        exitButton.style.fontSize = 14;
        exitButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        exitButton.style.whiteSpace = WhiteSpace.NoWrap;
        ApplyFont(exitButton);
        
        // 添加悬停效果
        exitButton.RegisterCallback<MouseEnterEvent>(evt => {
            exitButton.style.backgroundColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        });
        
        exitButton.RegisterCallback<MouseLeaveEvent>(evt => {
            exitButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        });
        
        mainButtonContainer.Add(exitButton);
        
        // 添加切换面板显示的按钮到右侧
        CreateToggleButton("", mainButtonContainer);
        
        topBar.Add(mainButtonContainer);
        rootElement.Add(topBar);
    }
    
    void CreateStyledButton(string text, System.Action onClick, VisualElement parent)
    {
        var buttonContainer = new VisualElement();
        buttonContainer.style.height = 40;
        buttonContainer.style.width = 100; // 增加宽度到100px以确保文字不换行
        buttonContainer.style.marginLeft = 3;
        buttonContainer.style.marginRight = 3;
        buttonContainer.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        buttonContainer.style.justifyContent = Justify.Center;
        buttonContainer.style.alignItems = Align.Center;
        buttonContainer.style.borderTopLeftRadius = 5;
        buttonContainer.style.borderTopRightRadius = 5;
        buttonContainer.style.borderBottomLeftRadius = 5;
        buttonContainer.style.borderBottomRightRadius = 5;
        buttonContainer.style.borderLeftWidth = 1;
        buttonContainer.style.borderRightWidth = 1;
        buttonContainer.style.borderTopWidth = 1;
        buttonContainer.style.borderBottomWidth = 1;
        buttonContainer.style.borderLeftColor = primaryColor;
        buttonContainer.style.borderRightColor = primaryColor;
        buttonContainer.style.borderTopColor = primaryColor;
        buttonContainer.style.borderBottomColor = primaryColor;
        
        var label = new Label(text);
        label.style.color = Color.black;
        label.style.fontSize = 13; // 稍微减小字体大小以确保文字不换行
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.whiteSpace = WhiteSpace.NoWrap; // 强制文字不换行
        ApplyFont(label);
        buttonContainer.Add(label);
        
        // 鼠标悬停效果
        buttonContainer.RegisterCallback<MouseEnterEvent>(evt => {
            buttonContainer.style.backgroundColor = primaryColor;
            label.style.color = Color.white;
        });
        
        buttonContainer.RegisterCallback<MouseLeaveEvent>(evt => {
            buttonContainer.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            label.style.color = Color.black;
        });
        
        // 点击事件
        buttonContainer.RegisterCallback<ClickEvent>(evt => onClick?.Invoke());
        
        parent.Add(buttonContainer);
    }
    
    void CreateToggleButton(string text, VisualElement parent)
    {
        var buttonContainer = new VisualElement();
        buttonContainer.style.height = 40;
        buttonContainer.style.width = 100; // 增加宽度到100px以确保文字不换行
        buttonContainer.style.marginLeft = 3;
        buttonContainer.style.marginRight = 3;
        buttonContainer.style.justifyContent = Justify.Center;
        buttonContainer.style.alignItems = Align.Center;
        buttonContainer.style.borderTopLeftRadius = 5;
        buttonContainer.style.borderTopRightRadius = 5;
        buttonContainer.style.borderBottomLeftRadius = 5;
        buttonContainer.style.borderBottomRightRadius = 5;
        buttonContainer.style.borderLeftWidth = 1;
        buttonContainer.style.borderRightWidth = 1;
        buttonContainer.style.borderTopWidth = 1;
        buttonContainer.style.borderBottomWidth = 1;
        buttonContainer.style.borderLeftColor = primaryColor;
        buttonContainer.style.borderRightColor = primaryColor;
        buttonContainer.style.borderTopColor = primaryColor;
        buttonContainer.style.borderBottomColor = primaryColor;
        
        var label = new Label();
        label.style.fontSize = 13; // 稍微减小字体大小以确保文字不换行
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.whiteSpace = WhiteSpace.NoWrap; // 强制文字不换行
        ApplyFont(label);
        buttonContainer.Add(label);
        
        // 保存标签引用以便后续更新
        toggleButtonLabel = label;
        
        // 更新按钮样式和文字以反映当前状态
        UpdateToggleButtonStyle(buttonContainer, label);
        
        // 鼠标悬停效果
        buttonContainer.RegisterCallback<MouseEnterEvent>(evt => {
            if (showSidePanel)
            {
                buttonContainer.style.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            }
            else
            {
                buttonContainer.style.backgroundColor = new Color(0.5f, 0.5f, 0.9f, 1f);
            }
        });
        
        buttonContainer.RegisterCallback<MouseLeaveEvent>(evt => {
            UpdateToggleButtonStyle(buttonContainer, label);
        });
        
        // 点击事件 - 切换面板显示
        buttonContainer.RegisterCallback<ClickEvent>(evt => {
            showSidePanel = !showSidePanel;
            UpdateToggleButtonStyle(buttonContainer, label);
            UpdateSidePanelVisibility();
        });
        
        parent.Add(buttonContainer);
    }
    
    void UpdateToggleButtonStyle(VisualElement buttonContainer, Label label)
    {
        if (showSidePanel)
        {
            // 面板显示时 - 浅色样式，显示"隐藏侧栏"
            buttonContainer.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            label.style.color = Color.black;
            label.text = "隐藏侧栏";
        }
        else
        {
            // 面板隐藏时 - 激活样式，显示"显示侧栏"
            buttonContainer.style.backgroundColor = primaryColor;
            label.style.color = Color.white;
            label.text = "显示侧栏";
        }
    }
    
    public void UpdateSidePanelVisibility()
    {
        if (sidebar != null)
        {
            if (showSidePanel)
            {
                sidebar.style.display = DisplayStyle.Flex;
                sidebar.style.width = 300;
            }
            else
            {
                sidebar.style.display = DisplayStyle.None;
                sidebar.style.width = 0;
            }
        }

        if (centerLabel != null)
        {
            centerLabel.style.display = showSidePanel ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // 更新切换按钮文字
        if (toggleButtonLabel != null)
        {
            toggleButtonLabel.text = showSidePanel ? "隐藏侧栏" : "显示侧栏";
        }

        // 更新状态栏显示
        UpdateStatusBar(showSidePanel ? $"模式: {currentMode}" : "面板已隐藏");
    }
    
    /// <summary>
    /// 设置侧边栏显示状态
    /// </summary>
    /// <param name="visible">是否显示侧边栏</param>
    public void SetSidePanelVisibility(bool visible)
    {
        showSidePanel = visible;
        UpdateSidePanelVisibility();
    }
    
    /// <summary>
    /// 显示主UI（从初始界面切换到主界面时调用）
    /// </summary>
    public void ShowMainUI()
    {
        // 初始化主UI（如果还没初始化）
        if (uiDocument == null)
        {
            InitializeMainUI();
        }
        else
        {
            // 如果已初始化，只切换到正常模式
            SwitchMode(UIMode.Normal);
        }
        
        Debug.Log("主UI已显示");
    }
    
    /// <summary>
    /// 应用字体到标签
    /// </summary>
    public void ApplyFont(Label label)
    {
        // 优先使用FontManager
        if (FontManager.Instance != null)
        {
            FontManager.Instance.ApplyFont(label, FontSize.Body);
        }
        else
        {
            // 备用方案
            if (fallbackFont != null)
            {
                label.style.unityFont = fallbackFont;
            }
            else
            {
                var builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (builtinFont != null)
                {
                    label.style.unityFont = builtinFont;
                }
            }
        }
    }
    
    /// <summary>
    /// 应用字体到下拉列表
    /// </summary>
    public void ApplyFont(DropdownField dropdown)
    {
        // 优先使用FontManager
        if (FontManager.Instance != null)
        {
            var currentFont = FontManager.Instance.GetCurrentFont();
            if (currentFont != null)
            {
                dropdown.style.unityFont = currentFont;
            }
        }
        else
        {
            // 备用方案
            if (fallbackFont != null)
            {
                dropdown.style.unityFont = fallbackFont;
            }
            else
            {
                var builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (builtinFont != null)
                {
                    dropdown.style.unityFont = builtinFont;
                }
            }
        }
    }
    
    /// <summary>
    /// 应用字体到文本输入框
    /// </summary>
    public void ApplyFont(TextField textField)
    {
        // 优先使用FontManager
        if (FontManager.Instance != null)
        {
            var currentFont = FontManager.Instance.GetCurrentFont();
            if (currentFont != null)
            {
                textField.style.unityFont = currentFont;
            }
        }
        else
        {
            // 备用方案
            if (fallbackFont != null)
            {
                textField.style.unityFont = fallbackFont;
            }
            else
            {
                var builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (builtinFont != null)
                {
                    textField.style.unityFont = builtinFont;
                }
            }
        }
    }
    
    /// <summary>
    /// 强制应用字体到所有UI元素
    /// </summary>
    public void ForceApplyFontToAllUI()
    {
        Debug.Log("强制应用字体到所有UI元素...");
        
        if (rootElement != null)
        {
            ApplyFontToAllElements(rootElement);
        }
        
        Debug.Log("字体应用完成");
    }
    
    /// <summary>
    /// 递归应用字体到所有UI元素
    /// </summary>
    private void ApplyFontToAllElements(VisualElement element)
    {
        // 应用字体到当前元素
        if (element is Label label)
        {
            ApplyFont(label);
        }
        else if (element is Button button)
        {
            ApplyFont(button);
        }
        else if (element is TextField textField)
        {
            ApplyFont(textField);
        }
        else if (element is DropdownField dropdown)
        {
            ApplyFont(dropdown);
        }
        
        // 递归应用到子元素
        foreach (var child in element.Children())
        {
            ApplyFontToAllElements(child);
        }
    }
    
    /// <summary>
    /// 应用字体到按钮
    /// </summary>
    public void ApplyFont(Button button)
    {
        // 优先使用FontManager
        if (FontManager.Instance != null)
        {
            FontManager.Instance.ApplyFont(button, FontSize.Body);
        }
        else
        {
            // 备用方案
            if (fallbackFont != null)
            {
                button.style.unityFont = fallbackFont;
            }
            else
            {
                var builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (builtinFont != null)
                {
                    button.style.unityFont = builtinFont;
                }
            }
        }
    }
    
    void CreateMainContent()
    {
        mainContent = new VisualElement();
        mainContent.style.flexDirection = FlexDirection.Row;
        mainContent.style.flexGrow = 1;
        mainContent.style.height = Length.Percent(100);
        mainContent.style.backgroundColor = StyleKeyword.None; // 透明无色

        // 侧边栏
        sidebar = new VisualElement();
        sidebar.style.width = 300;
        sidebar.style.backgroundColor = Color.white;
        sidebar.style.paddingTop = 20;
        sidebar.style.paddingLeft = 20;
        sidebar.style.paddingRight = 20;
        sidebar.style.paddingBottom = 20;
        sidebar.style.height = Length.Percent(100);
        sidebar.style.flexGrow = 0;
        
        // 添加强制事件阻挡，防止点击穿透到场景
        sidebar.pickingMode = PickingMode.Position; // 确保可以接收事件
        sidebar.RegisterCallback<MouseDownEvent>(evt => {
            evt.StopPropagation();
            evt.PreventDefault();
        });
        sidebar.RegisterCallback<MouseUpEvent>(evt => {
            evt.StopPropagation();
            evt.PreventDefault();
        });
        sidebar.RegisterCallback<ClickEvent>(evt => {
            evt.StopPropagation();
            evt.PreventDefault();
        });
        sidebar.RegisterCallback<MouseMoveEvent>(evt => {
            evt.StopPropagation();
        });
        sidebar.RegisterCallback<WheelEvent>(evt => {
            evt.StopPropagation();
        });

        mainContent.Add(sidebar);
        rootElement.Add(mainContent);
    }
    

    
    public void SwitchMode(UIMode mode)
    {
        currentMode = mode;
        
        if (showSidePanel)
        {
            sidebar.Clear();
            
            switch (mode)
            {
                case UIMode.Camera:
                    ShowCameraPanel();
                    break;
                case UIMode.Measure:
                    ShowMeasurePanel();
                    break;
                case UIMode.Danger:
                    ShowDangerPanel();
                    break;
                case UIMode.Powerline:
                    ShowPowerlinePanel();
                    break;
                case UIMode.TowerOverview:
                    ShowTowerOverviewPanel();
                    break;
                case UIMode.PointCloud:
                    ShowPointCloudPanel();
                    break;
                case UIMode.SceneOverview:
                    // 场景总览使用独立弹窗，不需要侧边栏
                    break;
                case UIMode.StatisticsDashboard:
                    ShowStatisticsDashboardPanel();
                    break;
                default:
                    ShowNormalPanel();
                    break;
            }
            
            UpdateStatusBar($"模式: {mode}");
        }
        else
        {
            // 如果面板隐藏，只更新状态，不显示内容
            UpdateStatusBar($"模式: {mode} (面板已隐藏)");
        }
    }
    
    void ShowNormalPanel()
    {
        sidebar.Clear(); // 确保每次切换都清空sidebar
        var panel = CreatePanel("基于机载LiDAR点云的电力线提取与三维重建系统");
        
        // 欢迎语
        var welcomeText = new Label("欢迎使用！");
        welcomeText.style.color = Color.black;
        welcomeText.style.fontSize = 18;
        welcomeText.style.marginBottom = 15;
        welcomeText.style.backgroundColor = Color.yellow;
        welcomeText.style.paddingTop = 5;
        welcomeText.style.paddingBottom = 5;
        ApplyFont(welcomeText);
        panel.Add(welcomeText);
        
        // 功能说明容器
        var infoContainer = new VisualElement();
        infoContainer.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        infoContainer.style.paddingTop = 15;
        infoContainer.style.paddingBottom = 15;
        infoContainer.style.paddingLeft = 15;
        infoContainer.style.paddingRight = 15;
        infoContainer.style.borderTopLeftRadius = 8;
        infoContainer.style.borderTopRightRadius = 8;
        infoContainer.style.borderBottomLeftRadius = 8;
        infoContainer.style.borderBottomRightRadius = 8;
        infoContainer.style.borderLeftWidth = 2;
        infoContainer.style.borderRightWidth = 2;
        infoContainer.style.borderTopWidth = 2;
        infoContainer.style.borderBottomWidth = 2;
        infoContainer.style.borderLeftColor = new Color(0.3f, 0.7f, 1f, 1f);
        infoContainer.style.borderRightColor = new Color(0.3f, 0.7f, 1f, 1f);
        infoContainer.style.borderTopColor = new Color(0.3f, 0.7f, 1f, 1f);
        infoContainer.style.borderBottomColor = new Color(0.3f, 0.7f, 1f, 1f);
        
        // 功能说明标题
        var infoTitle = new Label("系统功能：");
        infoTitle.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
        infoTitle.style.fontSize = 16;
        infoTitle.style.unityFontStyleAndWeight = FontStyle.Normal;
        infoTitle.style.marginBottom = 10;
        ApplyFont(infoTitle);
        infoContainer.Add(infoTitle);
        
        // 核心功能列表
        var coreFunctions = new Label("• 相机控制 - 多视角观察\n• 距离测量 - 精确测距\n• 危险物标记 - 安全隐患标识\n• 电力线信息 - 详细参数查看\n• 电塔总览 - 设备状态监控\n• 点云数据 - 3D点云可视化\n• 场景总览 - 全局地图视图\n• 无人机巡检 - 自动巡检功能");
        coreFunctions.style.color = Color.black;
        coreFunctions.style.fontSize = 14;
        coreFunctions.style.whiteSpace = WhiteSpace.Normal;
        coreFunctions.style.marginBottom = 15;
        ApplyFont(coreFunctions);
        infoContainer.Add(coreFunctions);
        
        // 操作提示
        var operationHint = new Label("操作提示：\n点击顶部按钮切换功能模式\n使用鼠标滚轮缩放视角\n左键拖拽旋转，右键平移");
        operationHint.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        operationHint.style.fontSize = 12;
        operationHint.style.whiteSpace = WhiteSpace.Normal;
        operationHint.style.marginBottom = 10;
        ApplyFont(operationHint);
        infoContainer.Add(operationHint);
        
        // 快捷键说明
        var hotkeyInfo = new Label("快捷键：\nH - 首页  M - 测量模式  C - 相机模式\nX - 危险物模式  P - 电力线模式\nT - 电塔总览  Tab - 切换面板");
        hotkeyInfo.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        hotkeyInfo.style.fontSize = 12;
        hotkeyInfo.style.whiteSpace = WhiteSpace.Normal;
        ApplyFont(hotkeyInfo);
        infoContainer.Add(hotkeyInfo);
        
        panel.Add(infoContainer);
        sidebar.Add(panel);
    }
    
    void ShowCameraPanel()
    {
        var panel = CreatePanel("相机控制");
        CreatePanelButton("第一人称视角", panel, () => {
            var cameraManager = FindObjectOfType<CameraManager>();
            if (cameraManager != null)
            {
                cameraManager.SwitchView(0); // 第一人称视角
                UpdateStatusBar("已切换到第一人称视角");
            }
        });
        CreatePanelButton("上帝视角", panel, () => {
            var cameraManager = FindObjectOfType<CameraManager>();
            if (cameraManager != null)
            {
                cameraManager.SwitchView(1); // 上帝视角
                UpdateStatusBar("已切换到上帝视角");
            }
        });
        CreatePanelButton("飞行视角", panel, () => {
            var cameraManager = FindObjectOfType<CameraManager>();
            if (cameraManager != null)
            {
                cameraManager.SwitchView(2); // 飞行视角
                UpdateStatusBar("已切换到飞行视角");
            }
        });
        sidebar.Add(panel);
    }
    
    void ShowMeasurePanel()
    {
        var panel = CreatePanel("距离测量");
        
        var measureController = FindObjectOfType<UIToolkitMeasureController>();
        
        // 如果没有找到测量控制器，创建一个
        if (measureController == null)
        {
            var measureObj = new GameObject("UIToolkitMeasureController");
            measureController = measureObj.AddComponent<UIToolkitMeasureController>();
            measureController.Initialize();
            // UIToolkitMeasureController会在Start方法中自动初始化LineRenderer ，所以这里不需要手动初始化
        }
        
        CreatePanelButton("开始测量", panel, () => {
            if (measureController != null)
            {
                measureController.StartMeasuring();
                UpdateStatusBar("点击场景添加测量点");
            }
        });
        
        CreatePanelButton("清除测量", panel, () => {
            if (measureController != null)
            {
                measureController.ClearMeasurements();
                UpdateStatusBar("已清除所有测量标记");
            }
        });
        
        // 创建动态更新的测量信息显示区域
        if (measureController != null)
        {
            CreateDynamicMeasureInfoDisplay(panel, measureController);
        }
        
        sidebar.Add(panel);
    }
    
    void CreateDynamicMeasureInfoDisplay(VisualElement parent, UIToolkitMeasureController measureController)
    {
        var infoContainer = new VisualElement();
        infoContainer.name = "measure-info-container";
        infoContainer.style.backgroundColor = new Color(0.95f, 0.98f, 1f, 1f);
        infoContainer.style.marginTop = 10;
        infoContainer.style.paddingTop = 15;
        infoContainer.style.paddingBottom = 15;
        infoContainer.style.paddingLeft = 15;
        infoContainer.style.paddingRight = 15;
        infoContainer.style.borderTopLeftRadius = 8;
        infoContainer.style.borderTopRightRadius = 8;
        infoContainer.style.borderBottomLeftRadius = 8;
        infoContainer.style.borderBottomRightRadius = 8;
        infoContainer.style.borderLeftWidth = 2;
        infoContainer.style.borderRightWidth = 2;
        infoContainer.style.borderTopWidth = 2;
        infoContainer.style.borderBottomWidth = 2;
        infoContainer.style.borderLeftColor = new Color(0.3f, 0.7f, 1f, 1f);
        infoContainer.style.borderRightColor = new Color(0.3f, 0.7f, 1f, 1f);
        infoContainer.style.borderTopColor = new Color(0.3f, 0.7f, 1f, 1f);
        infoContainer.style.borderBottomColor = new Color(0.3f, 0.7f, 1f, 1f);
        
        // 总距离显示
        var distanceLabel = new Label("总距离: 0.00m");
        distanceLabel.name = "measure-distance-label";
        distanceLabel.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
        distanceLabel.style.fontSize = 16;
        distanceLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
        distanceLabel.style.marginBottom = 8;
        ApplyFont(distanceLabel);
        infoContainer.Add(distanceLabel);
        
        // 点数显示
        var pointsLabel = new Label("点数: 0");
        pointsLabel.name = "measure-points-label";
        pointsLabel.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
        pointsLabel.style.fontSize = 16;
        pointsLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
        pointsLabel.style.marginBottom = 12;
        ApplyFont(pointsLabel);
        infoContainer.Add(pointsLabel);
        
        // 分隔线
        var separator = new VisualElement();
        separator.style.height = 1;
        separator.style.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        separator.style.marginBottom = 8;
        infoContainer.Add(separator);
        
        // 详细信息滚动视图
        var scrollView = new ScrollView();
        scrollView.name = "measure-detail-scroll";
        scrollView.style.minHeight = 250;
        scrollView.style.maxHeight = 1000;
        scrollView.style.flexGrow = 1; 
        scrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
        scrollView.style.overflow = Overflow.Hidden;
        scrollView.scrollDecelerationRate = 0.9f;

        scrollView.RegisterCallback<WheelEvent>(evt =>
        {
            scrollView.scrollOffset += new Vector2(0, evt.delta.y * 200f); // 放大滚动量
            evt.StopPropagation();
        });


        var detailContainer = new VisualElement();
        detailContainer.name = "measure-detail-container";
        detailContainer.style.flexDirection = FlexDirection.Column;
        detailContainer.style.flexShrink = 0; 
        
        
        scrollView.Add(detailContainer);
        infoContainer.Add(scrollView);
        parent.Add(infoContainer);
    }
    
    // 新增：更新测量信息的公共方法
    public void UpdateMeasureInfo()
    {
        if (currentMode != UIMode.Measure || !showSidePanel) return;
        
        var measureController = FindObjectOfType<UIToolkitMeasureController>();
        if (measureController == null) return;
        
        try
        {
            // 获取测量数据
            var measurePointsField = measureController.GetType().GetField("measurePoints", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var measurePoints = measurePointsField?.GetValue(measureController) as System.Collections.Generic.List<Vector3>;
            
            if (measurePoints == null) return;
            
            // 计算总距离
            float totalDistance = 0f;
            if (measurePoints.Count > 1)
            {
                for (int i = 1; i < measurePoints.Count; i++)
                {
                    totalDistance += Vector3.Distance(measurePoints[i - 1], measurePoints[i]);
                }
            }
            
            // 更新总距离标签
            var distanceLabel = sidebar?.Q<Label>("measure-distance-label");
            if (distanceLabel != null)
            {
                distanceLabel.text = $"总距离: {totalDistance:F2}m";
            }
            
            // 更新点数标签
            var pointsLabel = sidebar?.Q<Label>("measure-points-label");
            if (pointsLabel != null)
            {
                pointsLabel.text = $"点数: {measurePoints.Count}";
            }
            
            // 更新详细点信息
            UpdateDetailedPointInfo(measurePoints);
        }
        catch (System.Exception ex)
        {
            // 静默处理异常，避免影响主程序运行
            UnityEngine.Debug.LogWarning($"更新测量信息时出错: {ex.Message}");
        }
    }
    
    // 新增：更新详细点信息的方法
    void UpdateDetailedPointInfo(System.Collections.Generic.List<Vector3> measurePoints)
    {
        var detailContainer = sidebar?.Q<VisualElement>("measure-detail-container");
        if (detailContainer == null) return;
        
        detailContainer.Clear();
        
        if (measurePoints == null || measurePoints.Count == 0)
        {
            var noPointsLabel = new Label("暂无测量点");
            noPointsLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            noPointsLabel.style.fontSize = 12;
            noPointsLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            noPointsLabel.style.paddingTop = 20;
            noPointsLabel.style.paddingBottom = 20;
            ApplyFont(noPointsLabel);
            detailContainer.Add(noPointsLabel);
            return;
        }
        
        // 为每个点创建简化的信息卡片
        for (int i = 0; i < measurePoints.Count; i++)
        {
            var point = measurePoints[i];
            
            var pointContainer = new VisualElement();
            pointContainer.style.backgroundColor = new Color(1f, 1f, 1f, 0.9f);
            pointContainer.style.marginBottom = 5;
            pointContainer.style.paddingTop = 8;
            pointContainer.style.paddingBottom = 8;
            pointContainer.style.paddingLeft = 8;
            pointContainer.style.paddingRight = 8;
            pointContainer.style.borderTopLeftRadius = 4;
            pointContainer.style.borderTopRightRadius = 4;
            pointContainer.style.borderBottomLeftRadius = 4;
            pointContainer.style.borderBottomRightRadius = 4;
            pointContainer.style.borderLeftWidth = 1;
            pointContainer.style.borderRightWidth = 1;
            pointContainer.style.borderTopWidth = 1;
            pointContainer.style.borderBottomWidth = 1;
            pointContainer.style.borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            pointContainer.style.borderRightColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            pointContainer.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            pointContainer.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            
            // 点标题
            var pointTitle = new Label($"测量点 {i + 1}");
            pointTitle.style.color = new Color(0.2f, 0.5f, 0.8f, 1f);
            pointTitle.style.fontSize = 13;
            pointTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            pointTitle.style.marginBottom = 3;
            ApplyFont(pointTitle);
            pointContainer.Add(pointTitle);
            
            // 坐标信息
            var coordLabel = new Label($"坐标: ({point.x:F2}, {point.y:F2}, {point.z:F2})");
            coordLabel.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            coordLabel.style.fontSize = 11;
            ApplyFont(coordLabel);
            pointContainer.Add(coordLabel);
            
            // 距离信息
            if (i > 0)
            {
                float segmentDistance = Vector3.Distance(measurePoints[i - 1], point);
                var distanceInfo = new Label($"距离上一点: {segmentDistance:F2}m");
                distanceInfo.style.color = new Color(0.1f, 0.7f, 0.1f, 1f);
                distanceInfo.style.fontSize = 11;
                distanceInfo.style.unityFontStyleAndWeight = FontStyle.Bold;
                ApplyFont(distanceInfo);
                pointContainer.Add(distanceInfo);
            }
            else
            {
                var startLabel = new Label("起始点");
                startLabel.style.color = new Color(0.8f, 0.4f, 0.1f, 1f);
                startLabel.style.fontSize = 11;
                startLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                ApplyFont(startLabel);
                pointContainer.Add(startLabel);
            }
            
            detailContainer.Add(pointContainer);
        }
    }
    
    void ShowDangerPanel()
    {
        // 获取或创建危险物控制器
        var dangerController = FindObjectOfType<UIToolkitDangerController>();
        if (dangerController == null)
        {
            var dangerObj = new GameObject("UIToolkitDangerController");
            dangerController = dangerObj.AddComponent<UIToolkitDangerController>();
            
            // 等待一帧让Start方法执行完毕
            StartCoroutine(ShowDangerPanelDelayed(dangerController));
            return;
        }
        
        CreateDangerPanelContent(dangerController);
    }
    
    System.Collections.IEnumerator ShowDangerPanelDelayed(UIToolkitDangerController dangerController)
    {
        yield return null; // 等待一帧
        CreateDangerPanelContent(dangerController);
    }
    
    void CreateDangerPanelContent(UIToolkitDangerController dangerController)
    {
        // 创建危险物面板
        try
        {
            var dangerPanel = dangerController.CreateDangerPanel();
            if (dangerPanel != null)
            {
                sidebar.Add(dangerPanel);
                
                // 更新标记列表
                dangerController.UpdateMarkerList();
            }
            else
            {
                CreateBackupDangerPanel();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"创建危险物面板时出错: {ex.Message}");
            CreateBackupDangerPanel();
        }
    }
    
    void CreateBackupDangerPanel()
    {
        // 备用危险物面板
        var panel = CreatePanel("危险物标记");
        CreatePanelButton("添加标记", panel, () => {
            // DangerMarkingSystem已移除，使用UIToolkitDangerController
            UpdateStatusBar("请使用危险物控制器创建标记");
        });
        sidebar.Add(panel);
    }
    
    void ShowPowerlinePanel()
    {
        var panel = CreatePanel("电力线标记管理");
        
        // 统计信息容器
        var statsContainer = new VisualElement();
        statsContainer.style.backgroundColor = new Color(0.95f, 0.98f, 1f, 1f);
        statsContainer.style.marginBottom = 15;
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
        statsContainer.style.borderLeftColor = new Color(0.3f, 0.7f, 1f, 1f);
        statsContainer.style.borderRightColor = new Color(0.3f, 0.7f, 1f, 1f);
        statsContainer.style.borderTopColor = new Color(0.3f, 0.7f, 1f, 1f);
        statsContainer.style.borderBottomColor = new Color(0.3f, 0.7f, 1f, 1f);
        
        // 统计标题
        var statsTitle = new Label("标记统计");
        statsTitle.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
        statsTitle.style.fontSize = 16;
        statsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        statsTitle.style.marginBottom = 10;
        ApplyFont(statsTitle);
        statsContainer.Add(statsTitle);
        
        // 统计信息
        int totalMarks = powerlineMarkingSystem != null ? powerlineMarkingSystem.GetTotalMarkCount() : 0;
        var statsLabel = new Label($"总标记数量: {totalMarks}");
        statsLabel.style.color = Color.black;
        statsLabel.style.fontSize = 14;
        ApplyFont(statsLabel);
        statsContainer.Add(statsLabel);
        
        panel.Add(statsContainer);
        
        // 标记列表容器
        var marksContainer = new VisualElement();
        marksContainer.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        marksContainer.style.paddingTop = 15;
        marksContainer.style.paddingBottom = 15;
        marksContainer.style.paddingLeft = 15;
        marksContainer.style.paddingRight = 15;
        marksContainer.style.borderTopLeftRadius = 8;
        marksContainer.style.borderTopRightRadius = 8;
        marksContainer.style.borderBottomLeftRadius = 8;
        marksContainer.style.borderBottomRightRadius = 8;
        marksContainer.style.borderLeftWidth = 1;
        marksContainer.style.borderRightWidth = 1;
        marksContainer.style.borderTopWidth = 1;
        marksContainer.style.borderBottomWidth = 1;
        marksContainer.style.borderLeftColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        marksContainer.style.borderRightColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        marksContainer.style.borderTopColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        marksContainer.style.borderBottomColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        
        // 列表标题
        var listTitle = new Label("所有标记");
        listTitle.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
        listTitle.style.fontSize = 16;
        listTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        listTitle.style.marginBottom = 15;
        ApplyFont(listTitle);
        marksContainer.Add(listTitle);
        
        // 创建滚动视图
        var scrollView = new ScrollView();
        scrollView.style.height = 300;
        
        // 显示所有标记
        if (powerlineMarkingSystem != null)
        {
            var allMarks = powerlineMarkingSystem.GetAllMarks();
            if (allMarks.Count > 0)
            {
                foreach (var mark in allMarks)
                {
                    CreatePowerlineMarkItem(scrollView, mark);
                }
            }
            else
            {
                var noMarksLabel = new Label("暂无标记信息\n点击电力线添加标记");
                noMarksLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                noMarksLabel.style.fontSize = 14;
                noMarksLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                noMarksLabel.style.whiteSpace = WhiteSpace.Normal;
                noMarksLabel.style.marginTop = 50;
                ApplyFont(noMarksLabel);
                scrollView.Add(noMarksLabel);
            }
        }
        else
        {
            var errorLabel = new Label("标记系统未初始化");
            errorLabel.style.color = Color.red;
            errorLabel.style.fontSize = 14;
            errorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            ApplyFont(errorLabel);
            scrollView.Add(errorLabel);
        }
        
        marksContainer.Add(scrollView);
        panel.Add(marksContainer);
        
        // 添加按钮容器 - 参考测距面板的按钮样式
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.justifyContent = Justify.Center;
        buttonContainer.style.alignItems = Align.Center;
        // 按钮之间的间距通过margin设置
        
        // 清空所有标记按钮
        var clearBtn = new VisualElement();
        clearBtn.style.width = 100;
        clearBtn.style.height = 32;
        clearBtn.style.backgroundColor = new Color(0.8f, 0.3f, 0.3f, 1f); // 红色
        clearBtn.style.justifyContent = Justify.Center;
        clearBtn.style.alignItems = Align.Center;
        clearBtn.style.borderTopLeftRadius = 4;
        clearBtn.style.borderTopRightRadius = 4;
        clearBtn.style.borderBottomLeftRadius = 4;
        clearBtn.style.borderBottomRightRadius = 4;
        
        var clearLabel = new Label("清空所有");
        clearLabel.style.color = Color.white;
        clearLabel.style.fontSize = 14;
        clearLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(clearLabel);
        clearBtn.Add(clearLabel);
        
        clearBtn.RegisterCallback<ClickEvent>(evt => {
            if (powerlineMarkingSystem != null)
            {
                powerlineMarkingSystem.ClearAllMarks();
                UpdateStatusBar("已清空所有标记");
                // 刷新面板
                SwitchMode(UIMode.Powerline);
            }
        });
        buttonContainer.Add(clearBtn);
        
        panel.Add(buttonContainer);
        sidebar.Add(panel);
    }
    
    void ShowTowerOverviewPanel()
    {
        sidebar.Clear();
        
        var towerManager = FindObjectOfType<TowerOverviewManager>();
        if (towerManager != null)
        {
            CreateTowerOverviewSidebar(towerManager);
        }
        else
        {
            // 如果没有找到TowerOverviewManager组件，显示备用面板
            var panel = CreatePanel("电塔总览");
            var errorText = new Label("电塔总览组件未找到，请检查配置");
            errorText.style.color = Color.red;
            errorText.style.fontSize = 14;
            errorText.style.whiteSpace = WhiteSpace.Normal;
            errorText.style.marginBottom = 15;
            ApplyFont(errorText);
            panel.Add(errorText);
            sidebar.Add(panel);
        }
    }
    
    void ShowPointCloudPanel()
    {
        sidebar.Clear();
        
        // 创建有标题的面板容器
        var panel = CreatePanel("点云控制");
        
        if (pointCloudController != null)
        {
            // 创建点云控制面板内容
            var pointCloudContent = pointCloudController.CreatePointCloudPanel();
            if (pointCloudContent != null)
            {
                // 移除面板内容的外部样式，只保留内容
                pointCloudContent.style.backgroundColor = StyleKeyword.None;
                pointCloudContent.style.borderLeftWidth = 0;
                pointCloudContent.style.borderRightWidth = 0;
                pointCloudContent.style.borderTopWidth = 0;
                pointCloudContent.style.borderBottomWidth = 0;
                pointCloudContent.style.paddingTop = 0;
                pointCloudContent.style.paddingBottom = 0;
                pointCloudContent.style.paddingLeft = 0;
                pointCloudContent.style.paddingRight = 0;
                pointCloudContent.style.marginBottom = 0;
                
                panel.Add(pointCloudContent);
                pointCloudController.ShowPointCloudPanel();
            }
        }
        else
        {
            // 如果没有找到点云控制器，显示错误信息
            var errorText = new Label("点云控制器未找到，请检查配置");
            errorText.style.color = Color.red;
            errorText.style.fontSize = 14;
            errorText.style.whiteSpace = WhiteSpace.Normal;
            errorText.style.marginBottom = 15;
            ApplyFont(errorText);
            panel.Add(errorText);
        }
        
        sidebar.Add(panel);
    }
    
    VisualElement CreatePanel(string title)
    {
        var panel = new VisualElement();
        panel.style.backgroundColor = Color.white;
        panel.style.paddingTop = 15;
        panel.style.paddingBottom = 15;
        panel.style.paddingLeft = 15;
        panel.style.paddingRight = 15;
        panel.style.marginBottom = 10;
        panel.style.borderTopLeftRadius = 8;
        panel.style.borderTopRightRadius = 8;
        panel.style.borderBottomLeftRadius = 8;
        panel.style.borderBottomRightRadius = 8;
        panel.style.borderLeftWidth = 2;
        panel.style.borderRightWidth = 2;
        panel.style.borderTopWidth = 2;
        panel.style.borderBottomWidth = 2;
        panel.style.borderLeftColor = primaryColor;
        panel.style.borderRightColor = primaryColor;
        panel.style.borderTopColor = primaryColor;
        panel.style.borderBottomColor = primaryColor;
        
        // 添加强制事件阻挡，防止点击穿透到后面的场景
        panel.pickingMode = PickingMode.Position; // 确保可以接收事件
        panel.RegisterCallback<MouseDownEvent>(evt => {
            evt.StopPropagation();
            evt.PreventDefault();
        });
        panel.RegisterCallback<MouseUpEvent>(evt => {
            evt.StopPropagation();
            evt.PreventDefault();
        });
        panel.RegisterCallback<ClickEvent>(evt => {
            evt.StopPropagation();
            evt.PreventDefault();
        });
        panel.RegisterCallback<MouseMoveEvent>(evt => {
            evt.StopPropagation();
        });
        panel.RegisterCallback<WheelEvent>(evt => {
            evt.StopPropagation();
        });
        
        var titleLabel = new Label(title);
        titleLabel.style.color = Color.white;
        titleLabel.style.fontSize = 16;
        titleLabel.style.marginBottom = 10;
        titleLabel.style.backgroundColor = primaryColor;
        titleLabel.style.paddingTop = 5;
        titleLabel.style.paddingBottom = 5;
        titleLabel.style.paddingLeft = 10;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
        ApplyFont(titleLabel);
        panel.Add(titleLabel);
        
        return panel;
    }
    
    void CreateTowerOverviewSidebar(TowerOverviewManager towerManager)
    {
        // 初始化电塔数据
        towerManager.InitializeTowerData();
        var towers = towerManager.GetAllTowers();
        
        // 创建主面板
        var panel = CreatePanel("电塔总览");
        
        // 创建统计信息容器
        var statsContainer = new VisualElement();
        statsContainer.style.backgroundColor = new Color(0.95f, 0.98f, 1f, 1f);
        statsContainer.style.marginBottom = 10;
        statsContainer.style.paddingTop = 10;
        statsContainer.style.paddingBottom = 10;
        statsContainer.style.paddingLeft = 10;
        statsContainer.style.paddingRight = 10;
        statsContainer.style.borderTopLeftRadius = 8;
        statsContainer.style.borderTopRightRadius = 8;
        statsContainer.style.borderBottomLeftRadius = 8;
        statsContainer.style.borderBottomRightRadius = 8;
        statsContainer.style.borderLeftWidth = 2;
        statsContainer.style.borderRightWidth = 2;
        statsContainer.style.borderTopWidth = 2;
        statsContainer.style.borderBottomWidth = 2;
        statsContainer.style.borderLeftColor = new Color(0.3f, 0.7f, 1f, 1f);
        statsContainer.style.borderRightColor = new Color(0.3f, 0.7f, 1f, 1f);
        statsContainer.style.borderTopColor = new Color(0.3f, 0.7f, 1f, 1f);
        statsContainer.style.borderBottomColor = new Color(0.3f, 0.7f, 1f, 1f);
        
        // 电塔总数
        var totalLabel = new Label($"电塔总数: {towers.Count}");
        totalLabel.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
        totalLabel.style.fontSize = 16;
        totalLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
        totalLabel.style.marginBottom = 5;
        ApplyFont(totalLabel);
        statsContainer.Add(totalLabel);
        
        // 状态统计
        var normalCount = towers.Count(t => t.status == "正常");
        var warningCount = towers.Count(t => t.status == "警告");
        var errorCount = towers.Count(t => t.status == "异常");
        
        var statusLabel = new Label($"正常: {normalCount} | 警告: {warningCount} | 异常: {errorCount}");
        statusLabel.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
        statusLabel.style.fontSize = 12;
        statusLabel.style.marginBottom = 5;
        ApplyFont(statusLabel);
        statsContainer.Add(statusLabel);
        
        // 高度统计
        if (towers.Count > 0)
        {
            var avgHeight = towers.Average(t => t.height);
            var maxHeight = towers.Max(t => t.height);
            var minHeight = towers.Min(t => t.height);
            
            var heightLabel = new Label($"高度: 平均 {avgHeight:F1}m | 最高 {maxHeight:F1}m | 最低 {minHeight:F1}m");
            heightLabel.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
            heightLabel.style.fontSize = 12;
            ApplyFont(heightLabel);
            statsContainer.Add(heightLabel);
        }
        
        panel.Add(statsContainer);
        
        // 创建电塔列表滚动视图
        var scrollView = new ScrollView();
        scrollView.style.minHeight = 300;
        scrollView.style.maxHeight = 600;
        scrollView.style.flexGrow = 1;
        scrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
        scrollView.style.overflow = Overflow.Hidden;
        scrollView.scrollDecelerationRate = 0.9f;
        
        scrollView.RegisterCallback<WheelEvent>(evt =>
        {
            scrollView.scrollOffset += new Vector2(0, evt.delta.y * 300f); // 增加滚动速度
            evt.StopPropagation();
        });
        
        var towerListContainer = new VisualElement();
        towerListContainer.style.flexDirection = FlexDirection.Column;
        towerListContainer.style.flexShrink = 0;
        
        // 创建电塔列表项，按ID排序
        var sortedTowers = towers.OrderBy(t => t.id).ToList();
        CreateTowerListItems(towerListContainer, sortedTowers, towerManager);
        
        scrollView.Add(towerListContainer);
        panel.Add(scrollView);
        
        sidebar.Add(panel);
    }
    
    void CreateTowerListItems(VisualElement container, System.Collections.Generic.List<TowerOverviewManager.TowerOverviewInfo> towers, TowerOverviewManager towerManager)
    {
        if (towers == null || towers.Count == 0)
        {
            var noTowersLabel = new Label("未找到匹配的电塔");
            noTowersLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            noTowersLabel.style.fontSize = 12;
            noTowersLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            noTowersLabel.style.paddingTop = 20;
            noTowersLabel.style.paddingBottom = 20;
            ApplyFont(noTowersLabel);
            container.Add(noTowersLabel);
            return;
        }
        
        // 为每个电塔创建信息卡片
        for (int i = 0; i < towers.Count; i++)
        {
            var tower = towers[i];
            
            var towerContainer = new VisualElement();
            towerContainer.style.backgroundColor = new Color(1f, 1f, 1f, 0.9f);
            towerContainer.style.marginBottom = 8;
            towerContainer.style.paddingTop = 12;
            towerContainer.style.paddingBottom = 12;
            towerContainer.style.paddingLeft = 12;
            towerContainer.style.paddingRight = 12;
            towerContainer.style.borderTopLeftRadius = 6;
            towerContainer.style.borderTopRightRadius = 6;
            towerContainer.style.borderBottomLeftRadius = 6;
            towerContainer.style.borderBottomRightRadius = 6;
            towerContainer.style.borderLeftWidth = 2;
            towerContainer.style.borderRightWidth = 2;
            towerContainer.style.borderTopWidth = 2;
            towerContainer.style.borderBottomWidth = 2;
            
            // 根据状态设置边框颜色
            var borderColor = GetTowerStatusColor(tower.status);
            towerContainer.style.borderLeftColor = borderColor;
            towerContainer.style.borderRightColor = borderColor;
            towerContainer.style.borderTopColor = borderColor;
            towerContainer.style.borderBottomColor = borderColor;
            
            // 电塔标题
            var titleContainer = new VisualElement();
            titleContainer.style.flexDirection = FlexDirection.Row;
            titleContainer.style.justifyContent = Justify.SpaceBetween;
            titleContainer.style.alignItems = Align.Center;
            titleContainer.style.marginBottom = 8;
            
            var towerTitle = new Label($"电塔 {tower.id}: {tower.name}");
            towerTitle.style.color = new Color(0.2f, 0.5f, 0.8f, 1f);
            towerTitle.style.fontSize = 14;
            towerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            towerTitle.style.flexGrow = 1;
            ApplyFont(towerTitle);
            titleContainer.Add(towerTitle);
            
            // 状态标签
            var statusLabel = new Label(tower.status);
            statusLabel.style.color = Color.white;
            statusLabel.style.fontSize = 10;
            statusLabel.style.backgroundColor = borderColor;
            statusLabel.style.paddingLeft = 6;
            statusLabel.style.paddingRight = 6;
            statusLabel.style.paddingTop = 2;
            statusLabel.style.paddingBottom = 2;
            statusLabel.style.borderTopLeftRadius = 3;
            statusLabel.style.borderTopRightRadius = 3;
            statusLabel.style.borderBottomLeftRadius = 3;
            statusLabel.style.borderBottomRightRadius = 3;
            ApplyFont(statusLabel);
            titleContainer.Add(statusLabel);
            
            towerContainer.Add(titleContainer);
            
            // 坐标信息
            var coordContainer = new VisualElement();
            coordContainer.style.flexDirection = FlexDirection.Row;
            coordContainer.style.justifyContent = Justify.SpaceBetween;
            coordContainer.style.alignItems = Align.Center;
            coordContainer.style.marginBottom = 6;
            
            var coordLabel = new Label($"坐标: ({tower.position.x:F1}, {tower.position.y:F1}, {tower.position.z:F1})");
            coordLabel.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            coordLabel.style.fontSize = 11;
            coordLabel.style.flexGrow = 1;
            ApplyFont(coordLabel);
            coordContainer.Add(coordLabel);
            
            // 跳转按钮
            var jumpButton = new VisualElement();
            jumpButton.style.backgroundColor = new Color(0.2f, 0.6f, 0.8f, 1f);
            jumpButton.style.paddingLeft = 8;
            jumpButton.style.paddingRight = 8;
            jumpButton.style.paddingTop = 4;
            jumpButton.style.paddingBottom = 4;
            jumpButton.style.borderTopLeftRadius = 4;
            jumpButton.style.borderTopRightRadius = 4;
            jumpButton.style.borderBottomLeftRadius = 4;
            jumpButton.style.borderBottomRightRadius = 4;
            
            var jumpLabel = new Label("跳转");
            jumpLabel.style.color = Color.white;
            jumpLabel.style.fontSize = 10;
            jumpLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            ApplyFont(jumpLabel);
            jumpButton.Add(jumpLabel);
            
            // 跳转按钮点击事件
            jumpButton.RegisterCallback<ClickEvent>(evt => 
            {
                towerManager.JumpToTower(tower);
                UpdateStatusBar($"已跳转到电塔 {tower.id}: {tower.name}");
            });
            
            // 悬停效果
            jumpButton.RegisterCallback<MouseEnterEvent>(evt => 
            {
                jumpButton.style.backgroundColor = new Color(0.3f, 0.7f, 0.9f, 1f);
            });
            
            jumpButton.RegisterCallback<MouseLeaveEvent>(evt => 
            {
                jumpButton.style.backgroundColor = new Color(0.2f, 0.6f, 0.8f, 1f);
            });
            
            coordContainer.Add(jumpButton);
            towerContainer.Add(coordContainer);
            
            // 高度信息
            var heightLabel = new Label($"高度: {tower.height:F1}m");
            heightLabel.style.color = new Color(0.1f, 0.7f, 0.1f, 1f);
            heightLabel.style.fontSize = 11;
            heightLabel.style.marginBottom = 4;
            ApplyFont(heightLabel);
            towerContainer.Add(heightLabel);
            
            // 距离信息（如果有摄像机）
            if (Camera.main != null)
            {
                var distance = Vector3.Distance(Camera.main.transform.position, tower.position);
                var distanceLabel = new Label($"距离: {distance:F1}m");
                distanceLabel.style.color = new Color(0.6f, 0.4f, 0.1f, 1f);
                distanceLabel.style.fontSize = 11;
                ApplyFont(distanceLabel);
                towerContainer.Add(distanceLabel);
            }
            
            container.Add(towerContainer);
        }
    }
    
    Color GetTowerStatusColor(string status)
    {
        switch (status)
        {
            case "正常": return new Color(0.2f, 0.8f, 0.2f, 1f);
            case "警告": return new Color(1f, 0.6f, 0.2f, 1f);
            case "异常": return new Color(0.9f, 0.2f, 0.2f, 1f);
            default: return new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }
    
    void CreatePanelButton(string text, VisualElement parent, System.Action onClick)
    {
        var button = new VisualElement();
        button.style.height = 35;
        button.style.backgroundColor = primaryColor;
        button.style.marginBottom = 5;
        button.style.justifyContent = Justify.Center;
        button.style.alignItems = Align.Center;
        button.style.borderTopLeftRadius = 5;
        button.style.borderTopRightRadius = 5;
        button.style.borderBottomLeftRadius = 5;
        button.style.borderBottomRightRadius = 5;
        
        var label = new Label(text);
        label.style.color = Color.white;
        label.style.fontSize = 14;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(label);
        button.Add(label);
        
        button.RegisterCallback<ClickEvent>(evt => onClick?.Invoke());
        parent.Add(button);
    }
    
    public void UpdateStatusBar(string message)
    {
        // 底栏已删除，状态消息将通过其他方式显示
        // 保留此方法以避免现有调用出错
    }
    
    public void UpdateCoordinates(Vector3 position)
    {
        // 底栏已删除，坐标信息将通过其他方式显示
        // 保留此方法以避免现有调用出错
    }
    
    void Update()
    {
        // 更新坐标显示
        if (Camera.main != null)
        {
            Vector3 cameraPosition = Camera.main.transform.position;
            UpdateCoordinates(cameraPosition);
        }
        
        // 定期更新无人机巡检按钮状态（降低频率）
        if (dronePatrolButtonContainer != null && Time.time - lastButtonUpdateTime > BUTTON_UPDATE_INTERVAL)
        {
            UpdateDronePatrolButtonStyle();
            lastButtonUpdateTime = Time.time;
        }
        
        // 处理键盘输入
        if (Input.GetKeyDown(KeyCode.H))
        {
            // H键回到首页
            SwitchMode(UIMode.Normal);
        }
        
        if (Input.GetKeyDown(KeyCode.M))
        {
            // M键切换测量模式
            if (currentMode == UIMode.Measure)
            {
                SwitchMode(UIMode.Normal);
            }
            else
            {
                SwitchMode(UIMode.Measure);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            // C键切换相机模式
            if (currentMode == UIMode.Camera)
            {
                SwitchMode(UIMode.Normal);
            }
            else
            {
                SwitchMode(UIMode.Camera);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.X))
        {
            // X键切换危险物模式
            if (currentMode == UIMode.Danger)
            {
                SwitchMode(UIMode.Normal);
            }
            else
            {
                SwitchMode(UIMode.Danger);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            // P键切换电力线模式
            if (currentMode == UIMode.Powerline)
            {
                SwitchMode(UIMode.Normal);
            }
            else
            {
                SwitchMode(UIMode.Powerline);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            // T键切换电塔总览模式
            if (currentMode == UIMode.TowerOverview)
            {
                SwitchMode(UIMode.Normal);
            }
            else
            {
                SwitchMode(UIMode.TowerOverview);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Tab键切换面板显示
            showSidePanel = !showSidePanel;
            UpdateSidePanelVisibility();
        }
        
        // 处理ESC键
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // ESC键隐藏面板
            if (showSidePanel)
            {
                showSidePanel = false;
                UpdateSidePanelVisibility();
            }
        }
        
        // F键强制应用字体
        if (Input.GetKeyDown(KeyCode.F))
        {
            ForceApplyFontToAllUI();
        }
    }

    public void ShowPowerlineInfo(global::PowerlineInteraction powerline)
    {
        if (powerline == null) return;
        
        // 如果当前是测量模式，不显示电力线信息
        if (currentMode == UIMode.Measure)
        {
            return;
        }
        
        // 强制显示侧边栏
        showSidePanel = true;
        UpdateSidePanelVisibility();
        
        // 切换到电力线模式
        if (currentMode != UIMode.Powerline)
        {
            SwitchMode(UIMode.Powerline);
        }
        
        // 获取电力线信息
        var info = powerline.GetDetailedInfo();
        
        // 清空sidebar
        sidebar.Clear();
        
        // 使用统一的CreatePanel方法创建面板
        var panel = CreatePanel("电力线信息");
        
        // 添加内容容器 - 参考危险物面板的样式
        var contentContainer = new VisualElement();
        contentContainer.style.backgroundColor = new Color(0.95f, 0.98f, 1f, 1f);
        contentContainer.style.marginBottom = 10;
        contentContainer.style.paddingTop = 15;
        contentContainer.style.paddingBottom = 15;
        contentContainer.style.paddingLeft = 15;
        contentContainer.style.paddingRight = 15;
        contentContainer.style.borderTopLeftRadius = 8;
        contentContainer.style.borderTopRightRadius = 8;
        contentContainer.style.borderBottomLeftRadius = 8;
        contentContainer.style.borderBottomRightRadius = 8;
        contentContainer.style.borderLeftWidth = 2;
        contentContainer.style.borderRightWidth = 2;
        contentContainer.style.borderTopWidth = 2;
        contentContainer.style.borderBottomWidth = 2;
        contentContainer.style.borderLeftColor = new Color(0.3f, 0.7f, 1f, 1f);
        contentContainer.style.borderRightColor = new Color(0.3f, 0.7f, 1f, 1f);
        contentContainer.style.borderTopColor = new Color(0.3f, 0.7f, 1f, 1f);
        contentContainer.style.borderBottomColor = new Color(0.3f, 0.7f, 1f, 1f);
        contentContainer.style.flexDirection = FlexDirection.Column;
        
        // 分行添加信息 - 使用统一的AddInfoRow方法
        if (info != null && info.basicInfo != null)
        {
            AddInfoRow(contentContainer, "类型", info.basicInfo.wireType ?? "无");
            AddInfoRow(contentContainer, "电压等级", info.voltage ?? "无");
            AddInfoRow(contentContainer, "材质", info.material ?? "无");
            AddInfoRow(contentContainer, "安全距离", info.safetyDistance + "m");
            AddInfoRow(contentContainer, "电力线长度", info.wireLength.ToString("F2") + "m");
            AddInfoRow(contentContainer, "电力线宽度", info.wireWidth.ToString("F1") + "mm");
            AddInfoRow(contentContainer, "弯曲度", info.curvature.ToString("F2") + "%");
            
            // 状态行 - 可点击设置
            AddConditionRow(contentContainer, "状态", info.condition ?? "无", info.conditionSetTime, powerline);
        }
        else
        {
            var noDataLabel = new Label("无数据");
            noDataLabel.style.color = Color.red;
            noDataLabel.style.fontSize = 14;
            noDataLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            noDataLabel.style.paddingTop = 10;
            noDataLabel.style.paddingBottom = 10;
            ApplyFont(noDataLabel);
            contentContainer.Add(noDataLabel);
        }
        
        panel.Add(contentContainer);
        
        // 添加标记信息容器
        var marksContainer = new VisualElement();
        marksContainer.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        marksContainer.style.marginTop = 10;
        marksContainer.style.marginBottom = 10;
        marksContainer.style.paddingTop = 15;
        marksContainer.style.paddingBottom = 15;
        marksContainer.style.paddingLeft = 15;
        marksContainer.style.paddingRight = 15;
        marksContainer.style.borderTopLeftRadius = 8;
        marksContainer.style.borderTopRightRadius = 8;
        marksContainer.style.borderBottomLeftRadius = 8;
        marksContainer.style.borderBottomRightRadius = 8;
        marksContainer.style.borderLeftWidth = 1;
        marksContainer.style.borderRightWidth = 1;
        marksContainer.style.borderTopWidth = 1;
        marksContainer.style.borderBottomWidth = 1;
        marksContainer.style.borderLeftColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        marksContainer.style.borderRightColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        marksContainer.style.borderTopColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        marksContainer.style.borderBottomColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        
        // 标记标题
        var marksTitle = new Label("标记信息");
        marksTitle.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
        marksTitle.style.fontSize = 16;
        marksTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        marksTitle.style.marginBottom = 10;
        ApplyFont(marksTitle);
        marksContainer.Add(marksTitle);
        
        // 显示该电力线的标记
        if (powerlineMarkingSystem != null)
        {
            var powerlineMarks = powerlineMarkingSystem.GetPowerlineMarks(powerline);
            if (powerlineMarks.Count > 0)
            {
                // 创建滚动视图
                var scrollView = new ScrollView();
                scrollView.style.height = 150;
                scrollView.style.maxHeight = 150;
                
                foreach (var mark in powerlineMarks)
                {
                    CreatePowerlineMarkItem(scrollView, mark);
                }
                
                marksContainer.Add(scrollView);
            }
            else
            {
                var noMarksLabel = new Label("暂无标记信息");
                noMarksLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                noMarksLabel.style.fontSize = 14;
                noMarksLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                noMarksLabel.style.paddingTop = 20;
                noMarksLabel.style.paddingBottom = 20;
                ApplyFont(noMarksLabel);
                marksContainer.Add(noMarksLabel);
            }
        }
        else
        {
            var errorLabel = new Label("标记系统未初始化");
            errorLabel.style.color = Color.red;
            errorLabel.style.fontSize = 14;
            errorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            ApplyFont(errorLabel);
            marksContainer.Add(errorLabel);
        }
        
        panel.Add(marksContainer);
        
        // 添加按钮容器 - 参考测距面板的按钮样式
        var buttonContainer = new VisualElement();
        buttonContainer.style.paddingTop = 10;
        buttonContainer.style.paddingBottom = 15;
        buttonContainer.style.justifyContent = Justify.Center;
        buttonContainer.style.alignItems = Align.Center;
        buttonContainer.style.flexDirection = FlexDirection.Column;
        
        // 第一行按钮容器
        var firstRowContainer = new VisualElement();
        firstRowContainer.style.flexDirection = FlexDirection.Row;
        firstRowContainer.style.justifyContent = Justify.Center; // 改为居中对齐
        firstRowContainer.style.marginBottom = 10;
        
        // 3D视图按钮 - 参考CreatePanelButton的实现方式
        var view3DBtn = new VisualElement();
        view3DBtn.style.width = 100;
        view3DBtn.style.height = 32;
        view3DBtn.style.marginRight = 20; // 添加右边距，增加与下一个按钮的间距
        view3DBtn.style.backgroundColor = primaryColor;
        view3DBtn.style.justifyContent = Justify.Center;
        view3DBtn.style.alignItems = Align.Center;
        view3DBtn.style.borderTopLeftRadius = 4;
        view3DBtn.style.borderTopRightRadius = 4;
        view3DBtn.style.borderBottomLeftRadius = 4;
        view3DBtn.style.borderBottomRightRadius = 4;
        
        var view3DLabel = new Label("档距段查看");
        view3DLabel.style.color = Color.white;
        view3DLabel.style.fontSize = 14;
        view3DLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(view3DLabel);
        view3DBtn.Add(view3DLabel);
        
        view3DBtn.RegisterCallback<ClickEvent>(evt => {
            Powerline3DViewer.Instance.ShowPowerlineSegment(powerline);
        });
        firstRowContainer.Add(view3DBtn);
        
        // 标记按钮
        var markBtn = new VisualElement();
        markBtn.style.width = 100;
        markBtn.style.height = 32;
        markBtn.style.backgroundColor = new Color(0.3f, 0.7f, 0.3f, 1f); // 绿色
        markBtn.style.justifyContent = Justify.Center;
        markBtn.style.alignItems = Align.Center;
        markBtn.style.borderTopLeftRadius = 4;
        markBtn.style.borderTopRightRadius = 4;
        markBtn.style.borderBottomLeftRadius = 4;
        markBtn.style.borderBottomRightRadius = 4;
        
        var markLabel = new Label("添加标记");
        markLabel.style.color = Color.white;
        markLabel.style.fontSize = 14;
        markLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(markLabel);
        markBtn.Add(markLabel);
        
        markBtn.RegisterCallback<ClickEvent>(evt => {
            ShowMarkInputDialog(powerline);
        });
        firstRowContainer.Add(markBtn);
        
        buttonContainer.Add(firstRowContainer);
        
        // 第二行按钮容器
        var secondRowContainer = new VisualElement();
        secondRowContainer.style.flexDirection = FlexDirection.Row;
        secondRowContainer.style.justifyContent = Justify.Center;
        
        // 关闭按钮 - 参考CreatePanelButton的实现方式
        var closeBtn = new VisualElement();
        closeBtn.style.width = 100;
        closeBtn.style.height = 32;
        closeBtn.style.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        closeBtn.style.justifyContent = Justify.Center;
        closeBtn.style.alignItems = Align.Center;
        closeBtn.style.borderTopLeftRadius = 4;
        closeBtn.style.borderTopRightRadius = 4;
        closeBtn.style.borderBottomLeftRadius = 4;
        closeBtn.style.borderBottomRightRadius = 4;
        
        var closeLabel = new Label("关闭");
        closeLabel.style.color = Color.black;
        closeLabel.style.fontSize = 14;
        closeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(closeLabel);
        closeBtn.Add(closeLabel);
        
        closeBtn.RegisterCallback<ClickEvent>(evt => {
            sidebar.Clear();
            ShowNormalPanel();
        });
        secondRowContainer.Add(closeBtn);
        
        buttonContainer.Add(secondRowContainer);
        
        panel.Add(buttonContainer);
        sidebar.Add(panel);
    }

    // 添加信息行 - 统一风格
    private void AddInfoRow(VisualElement parent, string label, string value)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.paddingTop = 4;
        row.style.paddingBottom = 4;
        row.style.borderBottomWidth = 1;
        row.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        
        var labelElement = new Label(label + ":");
        labelElement.style.color = new Color(0.2f, 0.4f, 0.8f, 1f);
        labelElement.style.fontSize = 12;
        labelElement.style.minWidth = 80;
        ApplyFont(labelElement);
        
        var valueElement = new Label(value);
        valueElement.style.color = Color.black;
        valueElement.style.fontSize = 12;
        valueElement.style.unityTextAlign = TextAnchor.MiddleRight;
        valueElement.style.flexGrow = 1;
        valueElement.style.marginRight = 15; // 增加右边距，保持与其他行一致
        ApplyFont(valueElement);
        
        row.Add(labelElement);
        row.Add(valueElement);
        parent.Add(row);
    }
    
    /// <summary>
    /// 从测量面板中清除电力线信息
    /// </summary>
    private void ClearPowerlineInfoFromMeasurePanel()
    {
        if (currentMode != UIMode.Measure) return;
        
        // 查找测量信息容器
        var measureInfoContainer = sidebar?.Q<VisualElement>("measure-info-container");
        if (measureInfoContainer == null) return;
        
        // 查找并移除电力线信息容器
        var powerlineInfoContainer = measureInfoContainer.Q<VisualElement>("powerline-info-container");
        if (powerlineInfoContainer != null)
        {
            powerlineInfoContainer.RemoveFromHierarchy();
        }
        
        // 查找并移除电力线信息标题
        var powerlineTitle = measureInfoContainer.Q<Label>("电力线信息");
        if (powerlineTitle != null)
        {
            powerlineTitle.RemoveFromHierarchy();
        }
        
        // 查找并移除分隔线（在电力线信息之前的橙色分隔线）
        var separators = measureInfoContainer.Query<VisualElement>().ToList();
        foreach (var separator in separators)
        {
            if (separator.style.backgroundColor.value == new Color(0.8f, 0.4f, 0.2f, 1f) && 
                separator.style.height.value.value == 2)
            {
                separator.RemoveFromHierarchy();
                break; // 只移除第一个橙色分隔线
            }
        }
    }

    private void AddConditionRow(VisualElement parent, string label, string value, System.DateTime setTime, PowerlineInteraction powerline)
    {
        // 第一行：标签和状态值
        var topRow = new VisualElement();
        topRow.style.flexDirection = FlexDirection.Row;
        topRow.style.justifyContent = Justify.SpaceBetween;
        topRow.style.alignItems = Align.Center;
        topRow.style.paddingTop = 4;
        topRow.style.paddingBottom = 2;
        topRow.style.borderBottomWidth = 1;
        topRow.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        
        var labelElement = new Label(label + ":");
        labelElement.style.color = new Color(0.2f, 0.4f, 0.8f, 1f);
        labelElement.style.fontSize = 12;
        labelElement.style.minWidth = 80;
        ApplyFont(labelElement);
        
        // 状态值和时间
        var valueElement = new Label($"{value} ({setTime:yyyy-MM-dd HH:mm})");
        valueElement.style.color = GetConditionColor(value);
        valueElement.style.fontSize = 12;
        valueElement.style.unityTextAlign = TextAnchor.MiddleRight;
        valueElement.style.unityFontStyleAndWeight = FontStyle.Bold;
        valueElement.style.flexGrow = 1;
        ApplyFont(valueElement);
        
        topRow.Add(labelElement);
        topRow.Add(valueElement);
        parent.Add(topRow);
        
        // 第二行：设置按钮
        var bottomRow = new VisualElement();
        bottomRow.style.flexDirection = FlexDirection.Row;
        bottomRow.style.justifyContent = Justify.FlexEnd;
        bottomRow.style.alignItems = Align.Center;
        bottomRow.style.paddingTop = 2;
        bottomRow.style.paddingBottom = 4;
        bottomRow.style.paddingRight = 10;
        
        // 设置按钮
        var setButton = new Button(() => ShowConditionSettingDialog(powerline));
        setButton.text = "设置";
        setButton.style.width = 50;
        setButton.style.height = 24;
        setButton.style.fontSize = 10;
        setButton.style.backgroundColor = new Color(0.3f, 0.7f, 0.3f, 1f);
        setButton.style.color = Color.white;
        setButton.style.unityTextAlign = TextAnchor.MiddleCenter; // 文字居中
        setButton.style.flexShrink = 0;
        ApplyFont(setButton);
        
        bottomRow.Add(setButton);
        parent.Add(bottomRow);
    }
    
    private Color GetConditionColor(string condition)
    {
        switch (condition)
        {
            case "优秀": return new Color(0.2f, 0.8f, 0.2f, 1f); // 绿色
            case "良好": return new Color(0.8f, 0.6f, 0.2f, 1f); // 橙色
            case "需要维护": return new Color(0.8f, 0.2f, 0.2f, 1f); // 红色
            default: return Color.black;
        }
    }
    
    private void ShowConditionSettingDialog(PowerlineInteraction powerline)
    {
        // 创建状态设置对话框
        var dialog = new VisualElement();
        dialog.style.position = Position.Absolute;
        dialog.style.left = 0;
        dialog.style.top = 0;
        dialog.style.right = 0;
        dialog.style.bottom = 0;
        dialog.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        dialog.style.justifyContent = Justify.Center;
        dialog.style.alignItems = Align.Center;
        
        var dialogContent = new VisualElement();
        dialogContent.style.width = 300;
        dialogContent.style.backgroundColor = Color.white;
        dialogContent.style.paddingTop = 20;
        dialogContent.style.paddingBottom = 20;
        dialogContent.style.paddingLeft = 20;
        dialogContent.style.paddingRight = 20;
        dialogContent.style.borderTopLeftRadius = 8;
        dialogContent.style.borderTopRightRadius = 8;
        dialogContent.style.borderBottomLeftRadius = 8;
        dialogContent.style.borderBottomRightRadius = 8;
        
        // 标题
        var title = new Label("设置电力线状态");
        title.style.fontSize = 16;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        title.style.marginBottom = 20;
        ApplyFont(title);
        dialogContent.Add(title);
        
        // 状态选项
        string[] conditions = { "优秀", "良好", "需要维护" };
        string currentCondition = powerline.GetCurrentCondition();
        
        foreach (string condition in conditions)
        {
            var conditionButton = new Button(() => {
                powerline.SetCondition(condition);
                rootElement.Remove(dialog);
                // 刷新显示
                ShowPowerlineInfo(powerline);
            });
            conditionButton.text = condition;
            conditionButton.style.width = 200;
            conditionButton.style.height = 35;
            conditionButton.style.marginBottom = 10;
            conditionButton.style.backgroundColor = condition == currentCondition ? 
                new Color(0.3f, 0.7f, 0.3f, 1f) : new Color(0.8f, 0.8f, 0.8f, 1f);
            conditionButton.style.color = condition == currentCondition ? Color.white : Color.black;
            ApplyFont(conditionButton);
            dialogContent.Add(conditionButton);
        }
        
        // 取消按钮
        var cancelButton = new Button(() => rootElement.Remove(dialog));
        cancelButton.text = "取消";
        cancelButton.style.width = 200;
        cancelButton.style.height = 35;
        cancelButton.style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        cancelButton.style.color = Color.white;
        ApplyFont(cancelButton);
        dialogContent.Add(cancelButton);
        
        dialog.Add(dialogContent);
        rootElement.Add(dialog);
    }
    
    public void HidePowerlineInfo()
    {
        if (powerlineInfoPanel != null)
            powerlineInfoPanel.style.display = DisplayStyle.None;
    }
    
    /// <summary>
    /// 在测量面板中添加电力线信息
    /// </summary>
    private void AddPowerlineInfoToMeasurePanel(global::PowerlineInteraction powerline)
    {
        if (powerline == null || currentMode != UIMode.Measure) return;
        
        // 获取电力线信息
        var info = powerline.GetDetailedInfo();
        
        // 查找测量信息容器
        var measureInfoContainer = sidebar?.Q<VisualElement>("measure-info-container");
        if (measureInfoContainer == null) return;
        
        // 检查是否已经添加了电力线信息，如果已添加则移除
        var existingPowerlineInfo = measureInfoContainer.Q<VisualElement>("powerline-info-container");
        if (existingPowerlineInfo != null)
        {
            existingPowerlineInfo.RemoveFromHierarchy();
        }
        
        // 移除之前的电力线信息标题
        var existingTitle = measureInfoContainer.Q<Label>("电力线信息");
        if (existingTitle != null)
        {
            existingTitle.RemoveFromHierarchy();
        }
        
        // 移除之前的橙色分隔线
        var separators = measureInfoContainer.Query<VisualElement>().ToList();
        foreach (var existingSeparator in separators)
        {
            if (existingSeparator.style.backgroundColor.value == new Color(0.8f, 0.4f, 0.2f, 1f) && 
                existingSeparator.style.height.value.value == 2)
            {
                existingSeparator.RemoveFromHierarchy();
                break; // 只移除第一个橙色分隔线
            }
        }
        
        // 添加分隔线
        var newSeparator = new VisualElement();
        newSeparator.style.height = 2;
        newSeparator.style.backgroundColor = new Color(0.8f, 0.4f, 0.2f, 1f); // 橙色分隔线
        newSeparator.style.marginTop = 15;
        newSeparator.style.marginBottom = 15;
        measureInfoContainer.Add(newSeparator);
        
        // 添加电力线信息标题
        var powerlineTitle = new Label("电力线信息");
        powerlineTitle.style.color = new Color(0.8f, 0.4f, 0.2f, 1f);
        powerlineTitle.style.fontSize = 16;
        powerlineTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        powerlineTitle.style.marginBottom = 10;
        powerlineTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(powerlineTitle);
        measureInfoContainer.Add(powerlineTitle);
        
        // 创建电力线信息容器
        var powerlineInfoContainer = new VisualElement();
        powerlineInfoContainer.name = "powerline-info-container";
        powerlineInfoContainer.style.backgroundColor = new Color(1f, 0.95f, 0.9f, 1f); // 浅橙色背景
        powerlineInfoContainer.style.paddingTop = 10;
        powerlineInfoContainer.style.paddingBottom = 10;
        powerlineInfoContainer.style.paddingLeft = 10;
        powerlineInfoContainer.style.paddingRight = 10;
        powerlineInfoContainer.style.borderTopLeftRadius = 6;
        powerlineInfoContainer.style.borderTopRightRadius = 6;
        powerlineInfoContainer.style.borderBottomLeftRadius = 6;
        powerlineInfoContainer.style.borderBottomRightRadius = 6;
        powerlineInfoContainer.style.borderLeftWidth = 1;
        powerlineInfoContainer.style.borderRightWidth = 1;
        powerlineInfoContainer.style.borderTopWidth = 1;
        powerlineInfoContainer.style.borderBottomWidth = 1;
        powerlineInfoContainer.style.borderLeftColor = new Color(0.8f, 0.4f, 0.2f, 1f);
        powerlineInfoContainer.style.borderRightColor = new Color(0.8f, 0.4f, 0.2f, 1f);
        powerlineInfoContainer.style.borderTopColor = new Color(0.8f, 0.4f, 0.2f, 1f);
        powerlineInfoContainer.style.borderBottomColor = new Color(0.8f, 0.4f, 0.2f, 1f);
        powerlineInfoContainer.style.marginBottom = 10;
        
        // 添加电力线基本信息
        if (info != null && info.basicInfo != null)
        {
            AddCompactInfoRow(powerlineInfoContainer, "类型", info.basicInfo.wireType ?? "无");
            AddCompactInfoRow(powerlineInfoContainer, "电压", info.voltage ?? "无");
            AddCompactInfoRow(powerlineInfoContainer, "长度", info.wireLength.ToString("F1") + "m");
            AddCompactInfoRow(powerlineInfoContainer, "状态", info.condition ?? "无");
        }
        else
        {
            var noDataLabel = new Label("无电力线数据");
            noDataLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            noDataLabel.style.fontSize = 12;
            noDataLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            noDataLabel.style.paddingTop = 5;
            noDataLabel.style.paddingBottom = 5;
            ApplyFont(noDataLabel);
            powerlineInfoContainer.Add(noDataLabel);
        }
        
        measureInfoContainer.Add(powerlineInfoContainer);
        
        // 更新状态栏
        UpdateStatusBar($"已显示电力线信息 - {info?.basicInfo?.wireType ?? "未知类型"}");
    }
    
    /// <summary>
    /// 添加紧凑的信息行（用于测量面板中的电力线信息）
    /// </summary>
    private void AddCompactInfoRow(VisualElement parent, string label, string value)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 4;
        
        var labelElement = new Label(label + ":");
        labelElement.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        labelElement.style.fontSize = 12;
        labelElement.style.minWidth = 40;
        ApplyFont(labelElement);
        
        var valueElement = new Label(value);
        valueElement.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        valueElement.style.fontSize = 12;
        valueElement.style.unityFontStyleAndWeight = FontStyle.Bold;
        valueElement.style.unityTextAlign = TextAnchor.MiddleRight;
        ApplyFont(valueElement);
        
        row.Add(labelElement);
        row.Add(valueElement);
        parent.Add(row);
    }
    
    /// <summary>
    /// 显示标记输入对话框
    /// </summary>
    void ShowMarkInputDialog(PowerlineInteraction powerline)
    {
        // 创建遮罩层
        var overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0;
        overlay.style.left = 0;
        overlay.style.right = 0;
        overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        overlay.style.justifyContent = Justify.Center;
        overlay.style.alignItems = Align.Center;
        
        // 创建对话框
        var dialog = new VisualElement();
        dialog.style.width = 400;
        dialog.style.backgroundColor = Color.white;
        dialog.style.borderTopLeftRadius = 8;
        dialog.style.borderTopRightRadius = 8;
        dialog.style.borderBottomLeftRadius = 8;
        dialog.style.borderBottomRightRadius = 8;
        dialog.style.borderLeftWidth = 2;
        dialog.style.borderRightWidth = 2;
        dialog.style.borderTopWidth = 2;
        dialog.style.borderBottomWidth = 2;
        dialog.style.borderLeftColor = primaryColor;
        dialog.style.borderRightColor = primaryColor;
        dialog.style.borderTopColor = primaryColor;
        dialog.style.borderBottomColor = primaryColor;
        dialog.style.paddingTop = 20;
        dialog.style.paddingBottom = 20;
        dialog.style.paddingLeft = 20;
        dialog.style.paddingRight = 20;
        
        // 标题
        var titleLabel = new Label("添加电力线标记");
        titleLabel.style.color = primaryColor;
        titleLabel.style.fontSize = 16;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        titleLabel.style.marginBottom = 20;
        ApplyFont(titleLabel);
        dialog.Add(titleLabel);
        
        // 输入框标签
        var inputLabel = new Label("标记内容");
        inputLabel.style.color = Color.black;
        inputLabel.style.fontSize = 14;
        inputLabel.style.marginBottom = 5;
        ApplyFont(inputLabel);
        dialog.Add(inputLabel);
        
        // 输入框
        var inputField = new TextField();
        inputField.style.marginBottom = 20;
        inputField.style.fontSize = 20; // 进一步增大字体到20px
        inputField.style.height = 40; // 相应增加高度
        inputField.style.paddingLeft = 8;
        inputField.style.paddingRight = 8;
        inputField.style.paddingTop = 6;
        inputField.style.paddingBottom = 6;
        
        // 设置光标样式，使其更明显
        inputField.style.unityTextAlign = TextAnchor.MiddleLeft;
        inputField.style.color = Color.black; // 确保文字颜色明显
        inputField.style.backgroundColor = Color.white; // 白色背景
        inputField.style.borderBottomColor = primaryColor;
        inputField.style.borderBottomWidth = 2;
        inputField.style.borderTopColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        inputField.style.borderTopWidth = 1;
        inputField.style.borderLeftColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        inputField.style.borderLeftWidth = 1;
        inputField.style.borderRightColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        inputField.style.borderRightWidth = 1;
        inputField.style.borderTopLeftRadius = 4;
        inputField.style.borderTopRightRadius = 4;
        inputField.style.borderBottomLeftRadius = 4;
        inputField.style.borderBottomRightRadius = 4;
        
        ApplyFont(inputField);
        dialog.Add(inputField);
        
        // 按钮容器
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Column;
        buttonContainer.style.justifyContent = Justify.Center;
        
        // 第一行按钮容器
        var firstRowContainer = new VisualElement();
        firstRowContainer.style.flexDirection = FlexDirection.Row;
        firstRowContainer.style.justifyContent = Justify.SpaceBetween;
        firstRowContainer.style.marginBottom = 10;
        
        // 确定按钮
        var confirmBtn = new VisualElement();
        confirmBtn.style.width = 80;
        confirmBtn.style.height = 32;
        confirmBtn.style.backgroundColor = primaryColor;
        confirmBtn.style.justifyContent = Justify.Center;
        confirmBtn.style.alignItems = Align.Center;
        confirmBtn.style.borderTopLeftRadius = 4;
        confirmBtn.style.borderTopRightRadius = 4;
        confirmBtn.style.borderBottomLeftRadius = 4;
        confirmBtn.style.borderBottomRightRadius = 4;
        
        var confirmLabel = new Label("确定");
        confirmLabel.style.color = Color.white;
        confirmLabel.style.fontSize = 14;
        confirmLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(confirmLabel);
        confirmBtn.Add(confirmLabel);
        
        confirmBtn.RegisterCallback<ClickEvent>(evt => {
            string markText = inputField.value;
            if (!string.IsNullOrEmpty(markText))
            {
                if (powerlineMarkingSystem != null)
                {
                    bool success = powerlineMarkingSystem.AddMark(powerline, markText);
                    if (success)
                    {
                        UpdateStatusBar("标记添加成功");
                        // 刷新当前电力线信息页面，显示新添加的标记
                        ShowPowerlineInfo(powerline);
                    }
                    else
                    {
                        UpdateStatusBar("标记添加失败，可能已达到数量限制");
                    }
                }
            }
            overlay.RemoveFromHierarchy();
        });
        firstRowContainer.Add(confirmBtn);
        
        // 取消按钮
        var cancelBtn = new VisualElement();
        cancelBtn.style.width = 80;
        cancelBtn.style.height = 32;
        cancelBtn.style.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cancelBtn.style.justifyContent = Justify.Center;
        cancelBtn.style.alignItems = Align.Center;
        cancelBtn.style.borderTopLeftRadius = 4;
        cancelBtn.style.borderTopRightRadius = 4;
        cancelBtn.style.borderBottomLeftRadius = 4;
        cancelBtn.style.borderBottomRightRadius = 4;
        
        var cancelLabel = new Label("取消");
        cancelLabel.style.color = Color.black;
        cancelLabel.style.fontSize = 14;
        cancelLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(cancelLabel);
        cancelBtn.Add(cancelLabel);
        
        cancelBtn.RegisterCallback<ClickEvent>(evt => {
            overlay.RemoveFromHierarchy();
        });
        firstRowContainer.Add(cancelBtn);
        
        buttonContainer.Add(firstRowContainer);
        
        dialog.Add(buttonContainer);
        overlay.Add(dialog);
        
        // 添加到根元素
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.Add(overlay);
        }
    }
    
    /// <summary>
    /// 创建电力线标记项（用于电力线信息页面）
    /// </summary>
    void CreatePowerlineMarkItem(VisualElement parent, PowerlineMark mark)
    {
        var markItem = new VisualElement();
        markItem.style.backgroundColor = Color.white;
        markItem.style.marginBottom = 6;
        markItem.style.paddingTop = 8;
        markItem.style.paddingBottom = 8;
        markItem.style.paddingLeft = 10;
        markItem.style.paddingRight = 10;
        markItem.style.borderTopLeftRadius = 4;
        markItem.style.borderTopRightRadius = 4;
        markItem.style.borderBottomLeftRadius = 4;
        markItem.style.borderBottomRightRadius = 4;
        markItem.style.borderLeftWidth = 1;
        markItem.style.borderRightWidth = 1;
        markItem.style.borderTopWidth = 1;
        markItem.style.borderBottomWidth = 1;
        markItem.style.borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        markItem.style.borderRightColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        markItem.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        markItem.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        
        // 标记内容
        var contentLabel = new Label(mark.markText);
        contentLabel.style.color = Color.black;
        contentLabel.style.fontSize = 13;
        contentLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        contentLabel.style.marginBottom = 3;
        contentLabel.style.whiteSpace = WhiteSpace.Normal;
        ApplyFont(contentLabel);
        markItem.Add(contentLabel);
        
        // 标记信息行
        var infoRow = new VisualElement();
        infoRow.style.flexDirection = FlexDirection.Row;
        infoRow.style.justifyContent = Justify.SpaceBetween;
        
        // 创建时间
        var timeInfo = new Label(mark.createTime.ToString("MM-dd HH:mm"));
        timeInfo.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        timeInfo.style.fontSize = 11;
        ApplyFont(timeInfo);
        infoRow.Add(timeInfo);
        
        // 删除按钮
        var deleteBtn = new VisualElement();
        deleteBtn.style.width = 40;
        deleteBtn.style.height = 20;
        deleteBtn.style.backgroundColor = new Color(0.8f, 0.3f, 0.3f, 1f);
        deleteBtn.style.justifyContent = Justify.Center;
        deleteBtn.style.alignItems = Align.Center;
        deleteBtn.style.borderTopLeftRadius = 2;
        deleteBtn.style.borderTopRightRadius = 2;
        deleteBtn.style.borderBottomLeftRadius = 2;
        deleteBtn.style.borderBottomRightRadius = 2;
        deleteBtn.style.alignSelf = Align.FlexEnd;
        
        var deleteLabel = new Label("×");
        deleteLabel.style.color = Color.white;
        deleteLabel.style.fontSize = 12;
        deleteLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        deleteLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        ApplyFont(deleteLabel);
        deleteBtn.Add(deleteLabel);
        
        deleteBtn.RegisterCallback<ClickEvent>(evt => {
            if (powerlineMarkingSystem != null)
            {
                // 这里需要根据标记找到对应的电力线ID和索引
                // 简化处理：直接清空所有标记（实际应用中需要更精确的删除逻辑）
                powerlineMarkingSystem.ClearAllMarks();
                UpdateStatusBar("标记已删除");
                // 刷新面板 - 需要重新获取当前选中的电力线
                var currentPowerline = FindObjectOfType<PowerlineInteraction>();
                if (currentPowerline != null)
                {
                    ShowPowerlineInfo(currentPowerline);
                }
            }
        });
        infoRow.Add(deleteBtn);
        
        markItem.Add(infoRow);
        parent.Add(markItem);
    }
    
    /// <summary>
    /// 创建标记项（用于标记管理页面）
    /// </summary>
    void CreateMarkItem(VisualElement parent, PowerlineMark mark)
    {
        var markItem = new VisualElement();
        markItem.style.backgroundColor = Color.white;
        markItem.style.marginBottom = 8;
        markItem.style.paddingTop = 10;
        markItem.style.paddingBottom = 10;
        markItem.style.paddingLeft = 12;
        markItem.style.paddingRight = 12;
        markItem.style.borderTopLeftRadius = 6;
        markItem.style.borderTopRightRadius = 6;
        markItem.style.borderBottomLeftRadius = 6;
        markItem.style.borderBottomRightRadius = 6;
        markItem.style.borderLeftWidth = 1;
        markItem.style.borderRightWidth = 1;
        markItem.style.borderTopWidth = 1;
        markItem.style.borderBottomWidth = 1;
        markItem.style.borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        markItem.style.borderRightColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        markItem.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        markItem.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        
        // 标记内容
        var contentLabel = new Label(mark.markText);
        contentLabel.style.color = Color.black;
        contentLabel.style.fontSize = 14;
        contentLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        contentLabel.style.marginBottom = 5;
        contentLabel.style.whiteSpace = WhiteSpace.Normal;
        ApplyFont(contentLabel);
        markItem.Add(contentLabel);
        
        // 标记信息行
        var infoRow = new VisualElement();
        infoRow.style.flexDirection = FlexDirection.Row;
        infoRow.style.justifyContent = Justify.SpaceBetween;
        infoRow.style.marginBottom = 5;
        
        // 电力线信息
        var powerlineInfo = new Label($"{mark.powerlineType} | {mark.voltage}");
        powerlineInfo.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        powerlineInfo.style.fontSize = 12;
        ApplyFont(powerlineInfo);
        infoRow.Add(powerlineInfo);
        
        // 创建时间
        var timeInfo = new Label(mark.createTime.ToString("MM-dd HH:mm"));
        timeInfo.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        timeInfo.style.fontSize = 12;
        ApplyFont(timeInfo);
        infoRow.Add(timeInfo);
        
        markItem.Add(infoRow);
        
        // 删除按钮
        var deleteBtn = new VisualElement();
        deleteBtn.style.width = 60;
        deleteBtn.style.height = 24;
        deleteBtn.style.backgroundColor = new Color(0.8f, 0.3f, 0.3f, 1f);
        deleteBtn.style.justifyContent = Justify.Center;
        deleteBtn.style.alignItems = Align.Center;
        deleteBtn.style.borderTopLeftRadius = 3;
        deleteBtn.style.borderTopRightRadius = 3;
        deleteBtn.style.borderBottomLeftRadius = 3;
        deleteBtn.style.borderBottomRightRadius = 3;
        deleteBtn.style.alignSelf = Align.FlexEnd;
        
        var deleteLabel = new Label("删除");
        deleteLabel.style.color = Color.white;
        deleteLabel.style.fontSize = 12;
        deleteLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(deleteLabel);
        deleteBtn.Add(deleteLabel);
        
        deleteBtn.RegisterCallback<ClickEvent>(evt => {
            if (powerlineMarkingSystem != null)
            {
                // 这里需要根据标记找到对应的电力线ID和索引
                // 简化处理：直接清空所有标记（实际应用中需要更精确的删除逻辑）
                powerlineMarkingSystem.ClearAllMarks();
                UpdateStatusBar("标记已删除");
                // 刷新面板
                SwitchMode(UIMode.Powerline);
            }
        });
        markItem.Add(deleteBtn);
        
        parent.Add(markItem);
    }
    
    /// <summary>
    /// 更新电塔总览中的距离信息
    /// </summary>
    void UpdateTowerOverviewDistances()
    {
        if (currentMode != UIMode.TowerOverview || !showSidePanel || Camera.main == null) return;
        
        var towerManager = FindObjectOfType<TowerOverviewManager>();
        if (towerManager == null) return;
        
        // 注意：此方法已被移除自动调用，以避免列表刷新导致的滚动位置重置
        // 如果需要更新距离信息，可以在特定事件（如跳转完成后）手动调用
        
        // 可以在这里实现只更新距离标签的逻辑，而不重新创建整个列表
        // 这需要保存对距离标签的引用，目前暂时禁用自动更新
    }

    /// <summary>
    /// 切换场景总览
    /// </summary>
    void ToggleSceneOverview()
    {
        Debug.Log("场景总览按钮被点击");
        
        // 使用SceneOverviewManager处理场景总览
        var sceneOverviewManager = FindObjectOfType<SceneOverviewManager>();
        if (sceneOverviewManager != null)
        {
            sceneOverviewManager.ShowSceneOverview();
        }
        else
        {
            Debug.LogError("未找到SceneOverviewManager组件");
        }
    }
    
    /// <summary>
    /// 切换无人机巡检
    /// </summary>
    void ToggleDronePatrol()
    {
        Debug.Log("无人机巡检按钮被点击");
        
        // 查找无人机巡检管理器
        var dronePatrolManager = FindObjectOfType<DronePatrolManager>();
        if (dronePatrolManager != null)
        {
            dronePatrolManager.ToggleDronePatrol();
        }
        else
        {
            Debug.LogError("未找到DronePatrolManager组件");
        }
    }
    
    /// <summary>
    /// 停止无人机巡检（右键功能）
    /// </summary>
    void StopDronePatrol()
    {
        Debug.Log("停止无人机巡检");
        
        // 查找无人机巡检管理器
        var dronePatrolManager = FindObjectOfType<DronePatrolManager>();
        if (dronePatrolManager != null)
        {
            dronePatrolManager.StopDronePatrol();
        }
        else
        {
            Debug.LogError("未找到DronePatrolManager组件");
        }
    }
    
    /// <summary>
    /// 创建无人机巡检按钮（支持左键切换，右键停止）
    /// </summary>
    void CreateDronePatrolButton(VisualElement parent)
    {
        dronePatrolButtonContainer = new VisualElement();
        dronePatrolButtonContainer.style.height = 40;
        dronePatrolButtonContainer.style.width = 100; // 增加宽度到100px以确保文字不换行
        dronePatrolButtonContainer.style.marginLeft = 3;
        dronePatrolButtonContainer.style.marginRight = 3;
        dronePatrolButtonContainer.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        dronePatrolButtonContainer.style.justifyContent = Justify.Center;
        dronePatrolButtonContainer.style.alignItems = Align.Center;
        dronePatrolButtonContainer.style.borderTopLeftRadius = 5;
        dronePatrolButtonContainer.style.borderTopRightRadius = 5;
        dronePatrolButtonContainer.style.borderBottomLeftRadius = 5;
        dronePatrolButtonContainer.style.borderBottomRightRadius = 5;
        dronePatrolButtonContainer.style.borderLeftWidth = 1;
        dronePatrolButtonContainer.style.borderRightWidth = 1;
        dronePatrolButtonContainer.style.borderTopWidth = 1;
        dronePatrolButtonContainer.style.borderBottomWidth = 1;
        dronePatrolButtonContainer.style.borderLeftColor = primaryColor;
        dronePatrolButtonContainer.style.borderRightColor = primaryColor;
        dronePatrolButtonContainer.style.borderTopColor = primaryColor;
        dronePatrolButtonContainer.style.borderBottomColor = primaryColor;
        
        dronePatrolButtonLabel = new Label("无人机巡检");
        dronePatrolButtonLabel.style.color = Color.black;
        dronePatrolButtonLabel.style.fontSize = 13; // 稍微减小字体大小以确保文字不换行
        dronePatrolButtonLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        dronePatrolButtonLabel.style.whiteSpace = WhiteSpace.NoWrap; // 强制文字不换行
        ApplyFont(dronePatrolButtonLabel);
        dronePatrolButtonContainer.Add(dronePatrolButtonLabel);
        
        // 鼠标悬停效果
        dronePatrolButtonContainer.RegisterCallback<MouseEnterEvent>(evt => {
            dronePatrolButtonContainer.style.backgroundColor = primaryColor;
            dronePatrolButtonLabel.style.color = Color.white;
        });
        
        dronePatrolButtonContainer.RegisterCallback<MouseLeaveEvent>(evt => {
            UpdateDronePatrolButtonStyle(); // 恢复正确的状态样式
        });
        
        // 鼠标按下事件 - 区分左键和右键
        dronePatrolButtonContainer.RegisterCallback<MouseDownEvent>(evt => {
            if (evt.button == 0) // 左键
            {
                ToggleDronePatrol();
            }
            else if (evt.button == 1) // 右键
            {
                StopDronePatrol();
            }
            evt.StopPropagation();
        });
        
        parent.Add(dronePatrolButtonContainer);
    }
    
    /// <summary>
    /// 更新无人机巡检按钮样式
    /// </summary>
    public void UpdateDronePatrolButtonStyle()
    {
        if (dronePatrolButtonContainer == null || dronePatrolButtonLabel == null) return;
        
        // 查找无人机巡检管理器获取状态
        var dronePatrolManager = FindObjectOfType<DronePatrolManager>();
        if (dronePatrolManager == null) 
        {
            // 默认状态
            dronePatrolButtonLabel.text = "无人机巡检";
            dronePatrolButtonContainer.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            dronePatrolButtonLabel.style.color = Color.black;
            return;
        }
        
        bool isPatrolling = dronePatrolManager.IsPatrolling;
        bool isPaused = dronePatrolManager.IsPaused;
        
        if (isPatrolling)
        {
            if (isPaused)
            {
                dronePatrolButtonLabel.text = "继续巡检";
                dronePatrolButtonContainer.style.backgroundColor = new Color(0.3f, 0.3f, 0.8f, 1f); // 蓝色表示暂停
                dronePatrolButtonLabel.style.color = Color.white;
            }
            else
            {
                dronePatrolButtonLabel.text = "暂停巡检";
                dronePatrolButtonContainer.style.backgroundColor = new Color(0.8f, 0.6f, 0.3f, 1f); // 橙色表示可暂停
                dronePatrolButtonLabel.style.color = Color.white;
            }
        }
        else
        {
            dronePatrolButtonLabel.text = "无人机巡检";
            dronePatrolButtonContainer.style.backgroundColor = new Color(0.3f, 0.7f, 0.3f, 1f); // 绿色表示开始
            dronePatrolButtonLabel.style.color = Color.white;
        }
    }
    
    /// <summary>
    /// 切换电塔总览
    /// </summary>
    void ToggleTowerOverview()
    {
        // 现有的电塔总览功能
        Debug.Log("切换电塔总览模式");
        
        if (currentMode == UIMode.TowerOverview)
        {
            SetUIMode(UIMode.Normal);
            return;
        }
        
        SetUIMode(UIMode.TowerOverview);
        
        // 清除之前的侧边栏内容
        if (sidebar != null)
        {
            sidebar.Clear();
            
            // 使用现有的电塔总览功能
            var towerManager = GetComponent<TowerOverviewManager>();
            if (towerManager != null)
            {
                CreateTowerOverviewSidebar(towerManager);
            }
        }
    }

    /// <summary>
    /// 设置UI模式
    /// </summary>
    void SetUIMode(UIMode mode)
    {
        currentMode = mode;
        SwitchMode(mode);
    }


    /// <summary>
    /// 检查鼠标是否在UI侧栏区域上方
    /// </summary>
    public bool IsMouseOverSidebar()
    {
        if (!showSidePanel || sidebar == null) return false;
        
        Vector2 mousePosition = Input.mousePosition;
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        
        // 侧栏区域：左侧400px宽度，覆盖除顶栏和底栏以外的区域
        float sidebarWidth = 400f; 
        float sidebarLeftBound = 0f;
        float sidebarRightBound = sidebarWidth;
        
        float topBarHeight = 60f;
        float sidebarTopBound = screenHeight - (topBarHeight - 20f);
        float sidebarBottomBound = 0f; // 底部边界设为0，因为没有底栏了
        
        bool isInSidebar = mousePosition.x >= sidebarLeftBound && 
                          mousePosition.x <= sidebarRightBound && 
                          mousePosition.y >= sidebarBottomBound && 
                          mousePosition.y <= sidebarTopBound;
        
        return isInSidebar;
    }

    /// <summary>
    /// 检查鼠标是否在任何UI元素上方
    /// </summary>
    public bool IsMouseOverUI()
    {
        Vector2 mousePosition = Input.mousePosition;
        float screenHeight = Screen.height;
        
        // 检查顶栏和侧栏
        bool isOverTopBar = mousePosition.y >= screenHeight - 60f;
        bool isOverSidebar = IsMouseOverSidebar();
        
        return isOverTopBar || isOverSidebar;
    }
    
    /// <summary>
    /// 显示退出确认对话框
    /// </summary>
    void ShowExitConfirmationDialog()
    {
        Debug.Log("显示退出确认对话框");
        
        // 创建遮罩层
        var overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0;
        overlay.style.left = 0;
        overlay.style.right = 0;
        overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        overlay.style.justifyContent = Justify.Center;
        overlay.style.alignItems = Align.Center;
        
        // 创建对话框
        var dialog = new VisualElement();
        dialog.style.width = 400;
        dialog.style.backgroundColor = Color.white;
        dialog.style.borderTopLeftRadius = 8;
        dialog.style.borderTopRightRadius = 8;
        dialog.style.borderBottomLeftRadius = 8;
        dialog.style.borderBottomRightRadius = 8;
        dialog.style.borderLeftWidth = 2;
        dialog.style.borderRightWidth = 2;
        dialog.style.borderTopWidth = 2;
        dialog.style.borderBottomWidth = 2;
        dialog.style.borderLeftColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        dialog.style.borderRightColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        dialog.style.borderTopColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        dialog.style.borderBottomColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        dialog.style.paddingTop = 20;
        dialog.style.paddingBottom = 20;
        dialog.style.paddingLeft = 20;
        dialog.style.paddingRight = 20;
        
        // 标题
        var titleLabel = new Label("确认退出");
        titleLabel.style.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        titleLabel.style.fontSize = 18;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        titleLabel.style.marginBottom = 20;
        ApplyFont(titleLabel);
        dialog.Add(titleLabel);
        
        // 确认信息
        var confirmLabel = new Label("您确定要退出程序吗？\n所有未保存的数据将会丢失。");
        confirmLabel.style.color = Color.black;
        confirmLabel.style.fontSize = 14;
        confirmLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        confirmLabel.style.marginBottom = 30;
        confirmLabel.style.whiteSpace = WhiteSpace.Normal;
        ApplyFont(confirmLabel);
        dialog.Add(confirmLabel);
        
        // 按钮容器
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.justifyContent = Justify.Center; // 改为居中对齐
        buttonContainer.style.alignItems = Align.Center;
        
        // 取消按钮
        var cancelButton = new Button(() => {
            rootElement.Remove(overlay);
        });
        cancelButton.text = "取消";
        cancelButton.style.width = 120;
        cancelButton.style.height = 35;
        cancelButton.style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        cancelButton.style.color = Color.white;
        cancelButton.style.borderTopLeftRadius = 5;
        cancelButton.style.borderTopRightRadius = 5;
        cancelButton.style.borderBottomLeftRadius = 5;
        cancelButton.style.borderBottomRightRadius = 5;
        cancelButton.style.marginRight = 20; // 按钮之间的间距
        cancelButton.style.unityTextAlign = TextAnchor.MiddleCenter; // 文字居中
        ApplyFont(cancelButton);
        buttonContainer.Add(cancelButton);
        
        // 确认退出按钮
        var confirmButton = new Button(() => {
            ExitApplication();
        });
        confirmButton.text = "确认退出";
        confirmButton.style.width = 120;
        confirmButton.style.height = 35;
        confirmButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        confirmButton.style.color = Color.white;
        confirmButton.style.borderTopLeftRadius = 5;
        confirmButton.style.borderTopRightRadius = 5;
        confirmButton.style.borderBottomLeftRadius = 5;
        confirmButton.style.borderBottomRightRadius = 5;
        confirmButton.style.marginLeft = 0; // 确保左边距为0
        confirmButton.style.unityTextAlign = TextAnchor.MiddleCenter; // 文字居中
        ApplyFont(confirmButton);
        buttonContainer.Add(confirmButton);
        
        dialog.Add(buttonContainer);
        overlay.Add(dialog);
        rootElement.Add(overlay);
    }
    
    /// <summary>
    /// 退出应用程序
    /// </summary>
    void ExitApplication()
    {
        Debug.Log("用户确认退出程序");
        
        #if UNITY_EDITOR
        // 在编辑器中停止播放
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // 在构建的应用程序中退出
        Application.Quit();
        #endif
    }
    
    /// <summary>
    /// 显示统计大屏面板
    /// </summary>
    void ShowStatisticsDashboardPanel()
    {
        Debug.Log("=== ShowStatisticsDashboardPanel方法开始执行 ===");
        
        // 清空主要内容区域，准备显示统计大屏
        mainContent.Clear();
        
        // 查找现有的StatisticsDashboardController
        var existingController = FindObjectOfType<StatisticsDashboardController>();
        StatisticsDashboardController dashboardController = null;
        
        if (existingController != null)
        {
            Debug.Log("找到现有的StatisticsDashboardController，使用现有实例");
            dashboardController = existingController;
            
            // 重要：设置正确的UIDocument引用！
            if (uiDocument != null)
            {
                dashboardController.statisticsUIDocument = uiDocument;
                Debug.Log("已设置正确的UIDocument引用到StatisticsDashboardController");
            }
            else
            {
                Debug.LogError("SimpleUIToolkitManager的uiDocument为null");
            }
        }
        else
        {
            Debug.Log("未找到现有的StatisticsDashboardController，创建新实例");
            // 创建统计大屏控制器GameObject
            var dashboardGameObject = new GameObject("StatisticsDashboardController");
            dashboardController = dashboardGameObject.AddComponent<StatisticsDashboardController>();
            
            // 设置引用
            if (uiDocument != null)
            {
                dashboardController.statisticsUIDocument = uiDocument;
                Debug.Log("已设置UIDocument引用到新创建的StatisticsDashboardController");
            }
            else
            {
                Debug.LogError("SimpleUIToolkitManager的uiDocument为null");
            }
            
            if (statisticsDashboardManager != null)
            {
                dashboardController.statisticsManager = statisticsDashboardManager;
                Debug.Log("已设置StatisticsDashboardManager引用");
            }
            else
            {
                Debug.LogWarning("StatisticsDashboardManager为null");
            }
        }
        
        // 调用统计大屏的显示方法
        if (dashboardController != null)
        {
            Debug.Log("开始调用StatisticsDashboardController.ShowStatisticsDashboard()");
            dashboardController.ShowStatisticsDashboard();
            Debug.Log("StatisticsDashboardController.ShowStatisticsDashboard()调用完成");
        }
        else
        {
            Debug.LogError("StatisticsDashboardController创建失败");
        }
        
        // 在侧边栏添加返回主界面按钮
        sidebar.Clear();
        var returnButton = new Button(() => {
            Debug.Log("用户点击返回主界面按钮");
            SwitchMode(UIMode.Normal);
        });
        returnButton.text = "返回主界面";
        returnButton.style.width = 200;
        returnButton.style.height = 40;
        returnButton.style.backgroundColor = primaryColor;
        returnButton.style.color = Color.white;
        returnButton.style.marginTop = 10;
        ApplyFont(returnButton);
        sidebar.Add(returnButton);
        
        Debug.Log("=== ShowStatisticsDashboardPanel方法执行完成 ===");
    }
} 