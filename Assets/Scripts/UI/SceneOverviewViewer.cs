using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 场景总览弹窗查看器
/// 提供独立的场景总览弹窗功能
/// </summary>
public class SceneOverviewViewer : MonoBehaviour
{
    [Header("弹窗设置")]
    public bool showOnStart = false;
    
    [Header("数据设置")]
    private SceneInitializer sceneInitializer;
    
    // 组件引用
    private VisualElement overviewPanel;
    private UIDocument uiDocument;
    private bool isInitialized = false;
    private bool isVisible = false;
    
    // 单例模式
    private static SceneOverviewViewer instance;
    
    // 缩放和交互相关
    private float currentZoom = 1.0f;
    private float minZoom = 0.5f;
    private float maxZoom = 3.0f;
    private VisualElement mapCanvas;
    private VisualElement tooltipElement;
    private bool isDragging = false;
    private Vector2 lastMousePosition;
    private Vector2 mapOffset = Vector2.zero;
    private Label zoomLabel;
    
    // 电塔数据缓存
    private List<TowerMarkerData> towerMarkers = new List<TowerMarkerData>();
    
    /// <summary>
    /// 电塔标记数据结构
    /// </summary>
    private struct TowerMarkerData
    {
        public Vector2 position;
        public string name;
        public string status;
        public Vector3 worldPosition;
        public VisualElement element;
    }
    
    /// <summary>
    /// 电塔数据结构
    /// </summary>
    public struct TowerData
    {
        public Vector3 position;
        public string name;
        public string status; // "normal", "warning", "error"
    }
    
    /// <summary>
    /// 电线数据结构
    /// </summary>
    public struct WireData
    {
        public Vector3 start;
        public Vector3 end;
        public Color color;
    }
    
    public static SceneOverviewViewer Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SceneOverviewViewer>();
                if (instance == null)
                {
                    var go = new GameObject("SceneOverviewViewer");
                    instance = go.AddComponent<SceneOverviewViewer>();
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializeViewer();
        
        if (showOnStart)
        {
            ShowSceneOverview();
        }
    }

    void InitializeViewer()
    {
        if (isInitialized) return;
        
        Debug.Log("正在初始化SceneOverviewViewer...");
        
        // 改为使用UGUI系统，而不是UIElements
        CreateUGUIPanel();
        
        sceneInitializer = FindObjectOfType<SceneInitializer>();
        if (sceneInitializer != null)
        {
            Debug.Log("找到SceneInitializer");
        }
        else
        {
            Debug.LogWarning("未找到SceneInitializer，将使用模拟数据");
        }
        
        isInitialized = true;
        Debug.Log("SceneOverviewViewer初始化完成");
    }
    
    void CreateUGUIPanel()
    {
        // 创建Canvas GameObject
        var canvasObject = new GameObject("SceneOverviewCanvas");
        canvasObject.transform.SetParent(transform);
        
        // 添加Canvas组件
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 确保在最前面
        
        // 添加CanvasScaler
        var scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // 添加GraphicRaycaster
        canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // 保存canvas引用供后续使用
        gameObject.AddComponent<Canvas>();
        
        // 初始状态隐藏
        canvasObject.SetActive(false);
        
        Debug.Log("UGUI Canvas已创建");
    }
    
    System.Collections.IEnumerator DelayedInitialization()
    {
        yield return null; // 等待一帧
        
        if (uiDocument.rootVisualElement != null)
        {
            sceneInitializer = FindObjectOfType<SceneInitializer>();
            isInitialized = true;
            Debug.Log("延迟初始化完成");
        }
        else
        {
            Debug.LogError("UIDocument.rootVisualElement 仍然为空，无法显示场景总览");
        }
    }

    public void ShowSceneOverview()
    {
        Debug.Log("SceneOverviewViewer.ShowSceneOverview() 被调用");
        
        if (!isInitialized)
        {
            Debug.Log("初始化SceneOverviewViewer...");
            InitializeViewer();
        }
        
        if (isVisible) 
        {
            Debug.Log("场景总览已经显示，跳过");
            return;
        }
        
        Debug.Log("创建场景总览面板...");
        CreateOverviewPanel();
        isVisible = true;
        
        Debug.Log("场景总览显示完成");
    }
    
    void CreateOverviewPanel()
    {
        if (uiDocument == null)
        {
            Debug.LogError("uiDocument 为空，无法创建场景总览面板");
            return;
        }
        
        if (uiDocument.rootVisualElement == null)
        {
            Debug.LogError("uiDocument.rootVisualElement 为空，无法创建场景总览面板");
            return;
        }
        
        if (overviewPanel != null)
        {
            Debug.Log("移除现有的场景总览面板");
            overviewPanel.RemoveFromHierarchy();
        }
        
        Debug.Log("创建新的场景总览面板");
        
        // 创建主容器
        overviewPanel = new VisualElement();
        overviewPanel.style.position = Position.Absolute;
        overviewPanel.style.left = 0;
        overviewPanel.style.top = 0;
        overviewPanel.style.right = 0;
        overviewPanel.style.bottom = 0;
        overviewPanel.style.backgroundColor = new Color(0f, 0f, 0f, 0.7f);
        overviewPanel.style.justifyContent = Justify.Center;
        overviewPanel.style.alignItems = Align.Center;
        
        var container = new VisualElement();
        container.style.width = Length.Percent(90);
        container.style.height = Length.Percent(85);
        container.style.maxWidth = 1200;
        container.style.maxHeight = 800;
        container.style.backgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        container.style.borderTopLeftRadius = 12;
        container.style.borderTopRightRadius = 12;
        container.style.borderBottomLeftRadius = 12;
        container.style.borderBottomRightRadius = 12;
        
        try
        {
            Debug.Log("创建头部...");
            CreateHeader(container);
            
            Debug.Log("创建内容...");
            CreateContent(container);
            
            Debug.Log("创建底部...");
            CreateFooter(container);
            
            overviewPanel.Add(container);
            uiDocument.rootVisualElement.Add(overviewPanel);
            
            Debug.Log("场景总览面板已添加到UI");
            
            // 注册点击事件关闭弹窗
            overviewPanel.RegisterCallback<ClickEvent>(evt => {
                if (evt.target == overviewPanel)
                {
                    HideViewer();
                }
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError($"创建场景总览面板时出错: {e.Message}\n{e.StackTrace}");
        }
    }
    
    void CreateHeader(VisualElement parent)
    {
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.height = 60;
        header.style.backgroundColor = new Color(0.2f, 0.4f, 0.7f, 1f);
        header.style.paddingLeft = 25;
        header.style.paddingRight = 25;
        header.style.borderTopLeftRadius = 12;
        header.style.borderTopRightRadius = 12;
        
        // 标题
        var title = new Label("电力系统场景总览");
        title.style.color = Color.white;
        title.style.fontSize = 24;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        ApplyFont(title);
        header.Add(title);
        
        // 关闭按钮
        var closeButton = new Button(() => { HideViewer(); });
        closeButton.text = "×";
        closeButton.style.width = 40;
        closeButton.style.height = 40;
        closeButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        closeButton.style.color = Color.white;
        closeButton.style.fontSize = 28;
        closeButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        closeButton.style.borderTopLeftRadius = 20;
        closeButton.style.borderTopRightRadius = 20;
        closeButton.style.borderBottomLeftRadius = 20;
        closeButton.style.borderBottomRightRadius = 20;
        closeButton.style.borderBottomWidth = 0;
        closeButton.style.borderTopWidth = 0;
        closeButton.style.borderLeftWidth = 0;
        closeButton.style.borderRightWidth = 0;
        closeButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFontToButton(closeButton);
        header.Add(closeButton);
        
        parent.Add(header);
    }
    
    void CreateContent(VisualElement parent)
    {
        var content = new VisualElement();
        content.style.flexDirection = FlexDirection.Row;
        content.style.flexGrow = 1;
        content.style.paddingTop = 15;
        content.style.paddingBottom = 15;
        content.style.paddingLeft = 15;
        content.style.paddingRight = 15;
        
        // 主线路图区域（75%）
        CreateMainPanel(content);
        
        // 统计面板（25%）
        CreateStatsPanel(content);
        
        parent.Add(content);
    }
    
    void CreateMainPanel(VisualElement parent)
    {
        var mapContainer = new VisualElement();
        mapContainer.style.flexGrow = 1;
        mapContainer.style.backgroundColor = Color.white;
        mapContainer.style.marginRight = 15;
        mapContainer.style.borderTopLeftRadius = 8;
        mapContainer.style.borderTopRightRadius = 8;
        mapContainer.style.borderBottomLeftRadius = 8;
        mapContainer.style.borderBottomRightRadius = 8;
        mapContainer.style.paddingTop = 20;
        mapContainer.style.paddingBottom = 20;
        mapContainer.style.paddingLeft = 20;
        mapContainer.style.paddingRight = 20;
        
        // 标题
        var mapTitle = new Label("电力系统线路总览图");
        mapTitle.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
        mapTitle.style.fontSize = 20;
        mapTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        mapTitle.style.marginBottom = 15;
        mapTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(mapTitle);
        mapContainer.Add(mapTitle);
        
        // 缩放控制区域
        CreateZoomControls(mapContainer);
        
        // 线路图
        CreateMap(mapContainer);
        
        parent.Add(mapContainer);
    }
    
    void CreateZoomControls(VisualElement parent)
    {
        var controlsContainer = new VisualElement();
        controlsContainer.style.flexDirection = FlexDirection.Row;
        controlsContainer.style.justifyContent = Justify.SpaceBetween;
        controlsContainer.style.alignItems = Align.Center;
        controlsContainer.style.marginBottom = 10;
        controlsContainer.style.height = 30;
        
        // 左侧：缩放按钮
        var zoomContainer = new VisualElement();
        zoomContainer.style.flexDirection = FlexDirection.Row;
        zoomContainer.style.alignItems = Align.Center;
        
        var zoomOutButton = new Button(() => { ZoomMap(-0.2f); });
        zoomOutButton.text = "-";
        zoomOutButton.style.width = 30;
        zoomOutButton.style.height = 30;
        zoomOutButton.style.fontSize = 18;
        zoomOutButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        zoomOutButton.style.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        zoomOutButton.style.borderBottomWidth = 0;
        zoomOutButton.style.borderTopWidth = 0;
        zoomOutButton.style.borderLeftWidth = 0;
        zoomOutButton.style.borderRightWidth = 0;
        zoomOutButton.style.borderTopLeftRadius = 5;
        zoomOutButton.style.borderBottomLeftRadius = 5;
        ApplyFontToButton(zoomOutButton);
        
        zoomLabel = new Label($"{(currentZoom * 100):F0}%");
        zoomLabel.style.width = 60;
        zoomLabel.style.height = 30;
        zoomLabel.style.backgroundColor = Color.white;
        zoomLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        zoomLabel.style.fontSize = 12;
        zoomLabel.style.borderLeftWidth = 1;
        zoomLabel.style.borderRightWidth = 1;
        zoomLabel.style.borderTopWidth = 1;
        zoomLabel.style.borderBottomWidth = 1;
        zoomLabel.style.borderLeftColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        zoomLabel.style.borderRightColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        zoomLabel.style.borderTopColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        zoomLabel.style.borderBottomColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        ApplyFont(zoomLabel);
        
        var zoomInButton = new Button(() => { ZoomMap(0.2f); });
        zoomInButton.text = "+";
        zoomInButton.style.width = 30;
        zoomInButton.style.height = 30;
        zoomInButton.style.fontSize = 18;
        zoomInButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        zoomInButton.style.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        zoomInButton.style.borderBottomWidth = 0;
        zoomInButton.style.borderTopWidth = 0;
        zoomInButton.style.borderLeftWidth = 0;
        zoomInButton.style.borderRightWidth = 0;
        zoomInButton.style.borderTopRightRadius = 5;
        zoomInButton.style.borderBottomRightRadius = 5;
        ApplyFontToButton(zoomInButton);
        
        zoomContainer.Add(zoomOutButton);
        zoomContainer.Add(zoomLabel);
        zoomContainer.Add(zoomInButton);
        
        controlsContainer.Add(zoomContainer);
        
        // 右侧：重置按钮
        var resetButton = new Button(() => { ResetMapView(); });
        resetButton.text = "重置视图";
        resetButton.style.height = 30;
        resetButton.style.paddingLeft = 10;
        resetButton.style.paddingRight = 10;
        resetButton.style.fontSize = 12;
        resetButton.style.backgroundColor = new Color(0.2f, 0.6f, 0.8f, 1f);
        resetButton.style.color = Color.white;
        resetButton.style.borderBottomWidth = 0;
        resetButton.style.borderTopWidth = 0;
        resetButton.style.borderLeftWidth = 0;
        resetButton.style.borderRightWidth = 0;
        resetButton.style.borderTopLeftRadius = 5;
        resetButton.style.borderTopRightRadius = 5;
        resetButton.style.borderBottomLeftRadius = 5;
        resetButton.style.borderBottomRightRadius = 5;
        ApplyFontToButton(resetButton);
        
        controlsContainer.Add(resetButton);
        
        parent.Add(controlsContainer);
    }
    
    void CreateStatsPanel(VisualElement parent)
    {
        var statsPanel = new VisualElement();
        statsPanel.style.width = 280;
        statsPanel.style.backgroundColor = Color.white;
        statsPanel.style.paddingTop = 20;
        statsPanel.style.paddingBottom = 20;
        statsPanel.style.paddingLeft = 15;
        statsPanel.style.paddingRight = 15;
        statsPanel.style.borderTopLeftRadius = 8;
        statsPanel.style.borderTopRightRadius = 8;
        statsPanel.style.borderBottomLeftRadius = 8;
        statsPanel.style.borderBottomRightRadius = 8;
        
        // 标题
        var statsTitle = new Label("系统统计");
        statsTitle.style.color = new Color(0.1f, 0.4f, 0.8f, 1f);
        statsTitle.style.fontSize = 18;
        statsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        statsTitle.style.marginBottom = 15;
        statsTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(statsTitle);
        statsPanel.Add(statsTitle);
        
        // 获取统计数据
        var stats = GetSystemStats();
        
        // 创建统计卡片
        CreateStatCard(statsPanel, "电塔数量", $"{stats.Item1} 座", new Color(0.2f, 0.6f, 0.9f, 1f));
        CreateStatCard(statsPanel, "线路数量", $"{stats.Item2} 条", new Color(0.9f, 0.6f, 0.2f, 1f));
        CreateStatCard(statsPanel, "总长度", $"{stats.Item3:F1} 米", new Color(0.3f, 0.8f, 0.3f, 1f));
        CreateStatCard(statsPanel, "平均塔高", $"{stats.Item4:F1} 米", new Color(0.8f, 0.3f, 0.8f, 1f));
        
        // 状态统计
        var statusContainer = new VisualElement();
        statusContainer.style.marginTop = 20;
        
        var statusTitle = new Label("设备状态");
        statusTitle.style.fontSize = 14;
        statusTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        statusTitle.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        statusTitle.style.marginBottom = 10;
        ApplyFont(statusTitle);
        statusContainer.Add(statusTitle);
        
        var normalCount = Mathf.Max(stats.Item1 - 2, 0);
        var warningCount = Mathf.Min(1, stats.Item1);
        var errorCount = Mathf.Min(1, Mathf.Max(stats.Item1 - 6, 0));
        
        CreateStatusItem(statusContainer, "正常", normalCount, new Color(0.1f, 0.6f, 0.1f, 1f));
        CreateStatusItem(statusContainer, "警告", warningCount, new Color(1f, 0.8f, 0.2f, 1f));
        CreateStatusItem(statusContainer, "异常", errorCount, new Color(1f, 0.3f, 0.3f, 1f));
        
        statsPanel.Add(statusContainer);
        parent.Add(statsPanel);
    }
    
    void CreateMap(VisualElement parent)
    {
        var mapContainer = new VisualElement();
        mapContainer.style.height = 350;
        mapContainer.style.backgroundColor = new Color(0.98f, 0.98f, 1f, 1f);
        mapContainer.style.borderLeftWidth = 2;
        mapContainer.style.borderRightWidth = 2;
        mapContainer.style.borderTopWidth = 2;
        mapContainer.style.borderBottomWidth = 2;
        mapContainer.style.borderLeftColor = new Color(0.8f, 0.8f, 0.9f, 1f);
        mapContainer.style.borderRightColor = new Color(0.8f, 0.8f, 0.9f, 1f);
        mapContainer.style.borderTopColor = new Color(0.8f, 0.8f, 0.9f, 1f);
        mapContainer.style.borderBottomColor = new Color(0.8f, 0.8f, 0.9f, 1f);
        mapContainer.style.borderTopLeftRadius = 8;
        mapContainer.style.borderTopRightRadius = 8;
        mapContainer.style.borderBottomLeftRadius = 8;
        mapContainer.style.borderBottomRightRadius = 8;
        mapContainer.style.overflow = Overflow.Hidden;
        
        mapCanvas = new VisualElement();
        mapCanvas.style.position = Position.Absolute;
        mapCanvas.style.left = 0;
        mapCanvas.style.top = 0;
        mapCanvas.style.right = 0;
        mapCanvas.style.bottom = 0;
        
        // 注册交互事件
        RegisterMapInteractionEvents(mapContainer);
        
        // 创建工具提示
        CreateTooltip(mapContainer);
        
        // 绘制网格背景
        DrawGridBackground(mapCanvas);
        
        // 绘制电力线路图
        DrawPowerlineMap(mapCanvas);
        
        mapContainer.Add(mapCanvas);
        parent.Add(mapContainer);
    }
    
    void RegisterMapInteractionEvents(VisualElement mapContainer)
    {
        // 鼠标滚轮缩放
        mapContainer.RegisterCallback<WheelEvent>(evt => {
            float zoomDelta = evt.delta.y > 0 ? -0.1f : 0.1f;
            ZoomMap(zoomDelta);
            evt.StopPropagation();
        });
        
        // 鼠标拖拽
        mapContainer.RegisterCallback<MouseDownEvent>(evt => {
            if (evt.button == 0) // 左键
            {
                isDragging = true;
                lastMousePosition = evt.localMousePosition;
                evt.StopPropagation();
            }
        });
        
        mapContainer.RegisterCallback<MouseMoveEvent>(evt => {
            if (isDragging)
            {
                Vector2 deltaPosition = evt.localMousePosition - lastMousePosition;
                mapOffset += deltaPosition;
                lastMousePosition = evt.localMousePosition;
                UpdateMapTransform();
                evt.StopPropagation();
            }
        });
        
        mapContainer.RegisterCallback<MouseUpEvent>(evt => {
            if (evt.button == 0)
            {
                isDragging = false;
                evt.StopPropagation();
            }
        });
        
        // 防止拖拽出边界时鼠标释放
        mapContainer.RegisterCallback<MouseLeaveEvent>(evt => {
            isDragging = false;
        });
    }
    
    void CreateTooltip(VisualElement parent)
    {
        tooltipElement = new VisualElement();
        tooltipElement.style.position = Position.Absolute;
        tooltipElement.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        tooltipElement.style.color = Color.white;
        tooltipElement.style.paddingTop = 8;
        tooltipElement.style.paddingBottom = 8;
        tooltipElement.style.paddingLeft = 12;
        tooltipElement.style.paddingRight = 12;
        tooltipElement.style.borderTopLeftRadius = 6;
        tooltipElement.style.borderTopRightRadius = 6;
        tooltipElement.style.borderBottomLeftRadius = 6;
        tooltipElement.style.borderBottomRightRadius = 6;
        tooltipElement.style.display = DisplayStyle.None;
        
        parent.Add(tooltipElement);
    }
    
    void ZoomMap(float delta)
    {
        currentZoom = Mathf.Clamp(currentZoom + delta, minZoom, maxZoom);
        UpdateMapTransform();
        UpdateZoomLabel();
    }
    
    void ResetMapView()
    {
        currentZoom = 1.0f;
        mapOffset = Vector2.zero;
        UpdateMapTransform();
        UpdateZoomLabel();
    }
    
    void UpdateMapTransform()
    {
        if (mapCanvas != null)
        {
            mapCanvas.transform.scale = new Vector3(currentZoom, currentZoom, 1);
            mapCanvas.transform.position = new Vector3(mapOffset.x, mapOffset.y, 0);
        }
    }
    
    void UpdateZoomLabel()
    {
        if (zoomLabel != null)
        {
            zoomLabel.text = $"{(currentZoom * 100):F0}%";
        }
    }
    
    void ShowTowerTooltip(Vector2 mousePosition, TowerMarkerData towerData)
    {
        if (tooltipElement == null) return;
        
        tooltipElement.Clear();
        
        var towerInfo = GetTowerInfo(towerData.name, towerData.worldPosition);
        
        var nameLabel = new Label($"名称: {towerData.name}");
        nameLabel.style.fontSize = 12;
        nameLabel.style.color = Color.white;
        nameLabel.style.marginBottom = 2;
        ApplyFont(nameLabel);
        tooltipElement.Add(nameLabel);
        
        var positionLabel = new Label($"位置: ({towerData.worldPosition.x:F1}, {towerData.worldPosition.z:F1})");
        positionLabel.style.fontSize = 10;
        positionLabel.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        positionLabel.style.marginBottom = 2;
        ApplyFont(positionLabel);
        tooltipElement.Add(positionLabel);
        
        var statusLabel = new Label($"状态: {GetStatusText(towerData.status)}");
        statusLabel.style.fontSize = 10;
        statusLabel.style.color = GetStatusColor(towerData.status);
        statusLabel.style.marginBottom = 2;
        ApplyFont(statusLabel);
        tooltipElement.Add(statusLabel);
        
        var heightLabel = new Label($"高度: {towerInfo.Item1:F1}m");
        heightLabel.style.fontSize = 10;
        heightLabel.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        ApplyFont(heightLabel);
        tooltipElement.Add(heightLabel);
        
        // 设置工具提示位置
        tooltipElement.style.left = mousePosition.x + 10;
        tooltipElement.style.top = mousePosition.y - 20;
        tooltipElement.style.display = DisplayStyle.Flex;
    }
    
    void HideTooltip()
    {
        if (tooltipElement != null)
        {
            tooltipElement.style.display = DisplayStyle.None;
        }
    }
    
    void JumpToTower(Vector3 towerWorldPosition)
    {
        var cameraManager = FindObjectOfType<CameraManager>();
        if (cameraManager != null)
        {
            try
            {
                // 切换到上帝视角
                cameraManager.SwitchView(1); // 1 = 上帝视角
                
                // 延迟聚焦到电塔位置
                StartCoroutine(FocusOnTowerDelayed(towerWorldPosition));
                
                Debug.Log($"跳转到电塔位置: {towerWorldPosition}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"跳转到电塔时出错: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("未找到CameraManager组件，无法跳转到电塔位置");
        }
    }
    
    System.Collections.IEnumerator FocusOnTowerDelayed(Vector3 towerPosition)
    {
        yield return new WaitForSeconds(0.1f);
        
        var godViewCamera = FindObjectOfType<GodViewCamera>();
        if (godViewCamera != null)
        {
            try
            {
                var focusMethod = godViewCamera.GetType().GetMethod("FocusOnPoint");
                if (focusMethod != null)
                {
                    focusMethod.Invoke(godViewCamera, new object[] { towerPosition });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"聚焦电塔时出错: {e.Message}");
            }
        }
    }
    
    System.Tuple<float, string, float> GetTowerInfo(string towerName, Vector3 position)
    {
        // 尝试从场景中查找实际电塔（不依赖标签）
        var towerObjects = GameObject.FindObjectsOfType<GameObject>()
            .Where(go => go.name.Contains("Tower"))
            .ToArray();
            
        if (towerObjects != null && towerObjects.Length > 0)
        {
            var nearestTower = towerObjects.OrderBy(t => Vector3.Distance(t.transform.position, position)).FirstOrDefault();
            if (nearestTower != null && Vector3.Distance(nearestTower.transform.position, position) < 10f)
            {
                var renderer = nearestTower.GetComponent<Renderer>();
                if (renderer != null)
                {
                    float actualHeight = renderer.bounds.size.y;
                    return new System.Tuple<float, string, float>(actualHeight, "输电塔", 220f);
                }
                else
                {
                    // 如果没有Renderer，使用transform的缩放估算高度
                    float estimatedHeight = nearestTower.transform.localScale.y * 25f; // 假设基础高度25m
                    return new System.Tuple<float, string, float>(estimatedHeight, "输电塔", 220f);
                }
            }
        }
        
        // 根据名称推断电塔类型和参数
        if (towerName.Contains("1") || towerName.Contains("A"))
        {
            return new System.Tuple<float, string, float>(35f, "高压塔", 500f);
        }
        else if (towerName.Contains("2") || towerName.Contains("B"))
        {
            return new System.Tuple<float, string, float>(30f, "中压塔", 220f);
        }
        else
        {
            return new System.Tuple<float, string, float>(25f, "输电塔", 110f);
        }
    }
    
    string GetStatusText(string status)
    {
        switch (status)
        {
            case "normal": return "正常";
            case "warning": return "警告";
            case "error": return "异常";
            default: return "未知";
        }
    }
    
    Color GetStatusColor(string status)
    {
        switch (status)
        {
            case "normal": return new Color(0.2f, 0.8f, 0.2f, 1f);
            case "warning": return new Color(1f, 0.8f, 0.2f, 1f);
            case "error": return new Color(1f, 0.3f, 0.3f, 1f);
            default: return Color.white;
        }
    }
    
    void DrawGridBackground(VisualElement canvas)
    {
        // 创建网格容器
        var gridContainer = new VisualElement();
        gridContainer.style.position = Position.Absolute;
        gridContainer.style.left = 0;
        gridContainer.style.top = 0;
        gridContainer.style.right = 0;
        gridContainer.style.bottom = 0;
        
        // 绘制纵向网格线
        for (int i = 0; i <= 20; i++)
        {
            var vLine = new VisualElement();
            vLine.style.position = Position.Absolute;
            vLine.style.left = Length.Percent(i * 5f);
            vLine.style.top = 0;
            vLine.style.bottom = 0;
            vLine.style.width = 1;
            vLine.style.backgroundColor = new Color(0.9f, 0.95f, 1f, 0.6f);
            gridContainer.Add(vLine);
        }
        
        // 绘制横向网格线
        for (int i = 0; i <= 10; i++)
        {
            var hLine = new VisualElement();
            hLine.style.position = Position.Absolute;
            hLine.style.top = Length.Percent(i * 10f);
            hLine.style.left = 0;
            hLine.style.right = 0;
            hLine.style.height = 1;
            hLine.style.backgroundColor = new Color(0.9f, 0.95f, 1f, 0.6f);
            gridContainer.Add(hLine);
        }
        
        canvas.Add(gridContainer);
    }
    
    void ClearTowerMarkers()
    {
        if (towerMarkers != null)
        {
            foreach (var marker in towerMarkers)
            {
                if (marker.element != null && marker.element.parent != null)
                {
                    marker.element.RemoveFromHierarchy();
                }
            }
            towerMarkers.Clear();
        }
    }
    
    void DrawPowerlineMap(VisualElement canvas)
    {
        // 清理电塔标记缓存
        ClearTowerMarkers();
        
        // 获取场景拓扑数据
        var topologyData = GetSceneTopologyData();
        
        if (topologyData.Item1.Count == 0)
        {
            // 如果没有实际数据，绘制演示拓扑
            DrawDemoTopology(canvas);
            return;
        }
        
        // 计算地图边界
        var bounds = CalculateMapBounds(topologyData.Item1);
        
        // 计算缩放比例
        float scale = CalculateMapScale(bounds, 800, 350);
        
        // 绘制电线
        foreach (var wire in topologyData.Item2)
        {
            DrawWireLine(canvas, wire.start, wire.end, bounds, scale, wire.color);
        }
        
        // 绘制电塔
        foreach (var tower in topologyData.Item1)
        {
            DrawTowerMarker(canvas, tower.position, bounds, scale, tower.status, tower.name);
        }
        
        // 绘制图例
        DrawLegend(canvas);
    }
    
    void DrawDemoTopology(VisualElement canvas)
    {
        // 创建演示电塔位置
        var demoPositions = new Vector2[]
        {
            new Vector2(120, 180), new Vector2(260, 140), new Vector2(380, 200),
            new Vector2(520, 160), new Vector2(640, 220), new Vector2(760, 180),
            new Vector2(90, 280), new Vector2(200, 300), new Vector2(340, 320)
        };
        
        // 绘制连接线
        for (int i = 0; i < demoPositions.Length - 1; i++)
        {
            DrawWireLineDemo(canvas, demoPositions[i], demoPositions[i + 1], new Color(0.2f, 0.6f, 0.8f, 1f));
        }
        
        // 绘制电塔
        for (int i = 0; i < demoPositions.Length; i++)
        {
            string status = i == 0 ? "warning" : (i == 5 ? "error" : "normal");
            string name = $"塔{i + 1}";
            DrawTowerMarkerDemo(canvas, demoPositions[i], status, name);
        }
        
        DrawLegend(canvas);
    }
    
    System.Tuple<List<TowerData>, List<WireData>> GetSceneTopologyData()
    {
        var towers = new List<TowerData>();
        var wires = new List<WireData>();
        
        if (sceneInitializer != null && sceneInitializer.powerlines != null)
        {
            // 从场景初始化器获取实际数据
            foreach (var powerline in sceneInitializer.powerlines)
            {
                // 使用towerPositions数组获取电塔位置
                if (powerline.towerPositions != null)
                {
                    for (int i = 0; i < powerline.towerPositions.Length; i++)
                    {
                        towers.Add(new TowerData
                        {
                            position = powerline.towerPositions[i],
                            name = $"Tower_{i + 1}",
                            status = "normal"
                        });
                    }
                }
            }
        }
        
        // 如果没有从powerlines获取到数据，尝试查找场景中的电塔GameObject
        if (towers.Count == 0)
        {
            var towerObjects = GameObject.FindObjectsOfType<GameObject>()
                .Where(go => go.name.Contains("Tower") && go.transform.parent != null && 
                           go.transform.parent.name.Contains("Powerline"))
                .ToArray();
                
            foreach (var towerObj in towerObjects)
            {
                towers.Add(new TowerData
                {
                    position = towerObj.transform.position,
                    name = towerObj.name,
                    status = "normal"
                });
            }
        }
        
        return new System.Tuple<List<TowerData>, List<WireData>>(towers, wires);
    }
    
    Bounds CalculateMapBounds(List<TowerData> towers)
    {
        if (towers.Count == 0)
            return new Bounds(Vector3.zero, Vector3.one * 100);
        
        var bounds = new Bounds(towers[0].position, Vector3.zero);
        foreach (var tower in towers)
        {
            bounds.Encapsulate(tower.position);
        }
        
        return bounds;
    }
    
    float CalculateMapScale(Bounds bounds, float maxWidth, float maxHeight)
    {
        float scaleX = maxWidth / bounds.size.x;
        float scaleZ = maxHeight / bounds.size.z;
        return Mathf.Min(scaleX, scaleZ) * 0.8f;
    }
    
    void DrawWireLine(VisualElement canvas, Vector3 start, Vector3 end, Bounds bounds, float scale, Color color)
    {
        var startPos = WorldToMapPosition(start, bounds, scale, 800, 350);
        var endPos = WorldToMapPosition(end, bounds, scale, 800, 350);
        DrawWireLineDemo(canvas, startPos, endPos, color);
    }
    
    void DrawTowerMarker(VisualElement canvas, Vector3 worldPos, Bounds bounds, float scale, string status, string name)
    {
        var mapPos = WorldToMapPosition(worldPos, bounds, scale, 800, 350);
        DrawTowerMarkerDemo(canvas, mapPos, status, name, worldPos);
    }
    
    Vector2 WorldToMapPosition(Vector3 worldPos, Bounds bounds, float scale, float mapWidth, float mapHeight)
    {
        var relativePos = worldPos - bounds.min;
        var normalizedPos = new Vector2(relativePos.x / bounds.size.x, relativePos.z / bounds.size.z);
        return new Vector2(normalizedPos.x * mapWidth, normalizedPos.y * mapHeight);
    }
    
    void DrawWireLineDemo(VisualElement canvas, Vector2 start, Vector2 end, Color color)
    {
        var diff = end - start;
        var length = diff.magnitude;
        var angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        
        var line = new VisualElement();
        line.style.position = Position.Absolute;
        line.style.left = start.x;
        line.style.top = start.y - 1;
        line.style.width = length;
        line.style.height = 2;
        line.style.backgroundColor = color;
        line.style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));
        line.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(50));
        
        canvas.Add(line);
    }
    
    void DrawTowerMarkerDemo(VisualElement canvas, Vector2 position, string status, string name, Vector3 worldPosition = default)
    {
        var tower = new VisualElement();
        tower.style.position = Position.Absolute;
        tower.style.left = position.x - 8;
        tower.style.top = position.y - 8;
        tower.style.width = 16;
        tower.style.height = 16;
        tower.style.borderTopLeftRadius = 8;
        tower.style.borderTopRightRadius = 8;
        tower.style.borderBottomLeftRadius = 8;
        tower.style.borderBottomRightRadius = 8;
        tower.style.backgroundColor = GetStatusColor(status);
        tower.style.borderLeftWidth = 2;
        tower.style.borderRightWidth = 2;
        tower.style.borderTopWidth = 2;
        tower.style.borderBottomWidth = 2;
        tower.style.borderLeftColor = Color.white;
        tower.style.borderRightColor = Color.white;
        tower.style.borderTopColor = Color.white;
        tower.style.borderBottomColor = Color.white;
        
        // 创建电塔数据
        TowerMarkerData towerData = new TowerMarkerData
        {
            position = position,
            name = name,
            status = status,
            worldPosition = worldPosition,
            element = tower
        };
        
        towerMarkers.Add(towerData);
        
        // 注册鼠标事件
        tower.RegisterCallback<MouseEnterEvent>(evt => {
            ShowTowerTooltip(evt.localMousePosition, towerData);
            tower.style.borderLeftWidth = 3;
            tower.style.borderRightWidth = 3;
            tower.style.borderTopWidth = 3;
            tower.style.borderBottomWidth = 3;
            evt.StopPropagation();
        });
        
        tower.RegisterCallback<MouseMoveEvent>(evt => {
            if (tooltipElement != null && tooltipElement.style.display == DisplayStyle.Flex)
            {
                ShowTowerTooltip(evt.localMousePosition, towerData);
            }
            evt.StopPropagation();
        });
        
        tower.RegisterCallback<MouseLeaveEvent>(evt => {
            HideTooltip();
            tower.style.borderLeftWidth = 2;
            tower.style.borderRightWidth = 2;
            tower.style.borderTopWidth = 2;
            tower.style.borderBottomWidth = 2;
            evt.StopPropagation();
        });
        
        tower.RegisterCallback<ClickEvent>(evt => {
            if (worldPosition != default(Vector3))
            {
                JumpToTower(worldPosition);
            }
            evt.StopPropagation();
        });
        
        canvas.Add(tower);
    }
    
    void DrawLegend(VisualElement canvas)
    {
        var legend = new VisualElement();
        legend.style.position = Position.Absolute;
        legend.style.right = 15;
        legend.style.top = 15;
        legend.style.backgroundColor = new Color(1f, 1f, 1f, 0.9f);
        legend.style.paddingTop = 10;
        legend.style.paddingBottom = 10;
        legend.style.paddingLeft = 10;
        legend.style.paddingRight = 10;
        legend.style.borderTopLeftRadius = 5;
        legend.style.borderTopRightRadius = 5;
        legend.style.borderBottomLeftRadius = 5;
        legend.style.borderBottomRightRadius = 5;
        legend.style.borderLeftWidth = 1;
        legend.style.borderRightWidth = 1;
        legend.style.borderTopWidth = 1;
        legend.style.borderBottomWidth = 1;
        legend.style.borderLeftColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        legend.style.borderRightColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        legend.style.borderTopColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        legend.style.borderBottomColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        
        var title = new Label("图例");
        title.style.fontSize = 12;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 5;
        ApplyFont(title);
        legend.Add(title);
        
        AddLegendItem(legend, "正常", GetStatusColor("normal"));
        AddLegendItem(legend, "警告", GetStatusColor("warning"));
        AddLegendItem(legend, "异常", GetStatusColor("error"));
        
        canvas.Add(legend);
    }
    
    void AddLegendItem(VisualElement parent, string label, Color color)
    {
        var item = new VisualElement();
        item.style.flexDirection = FlexDirection.Row;
        item.style.alignItems = Align.Center;
        item.style.marginBottom = 3;
        
        var colorBox = new VisualElement();
        colorBox.style.width = 12;
        colorBox.style.height = 12;
        colorBox.style.backgroundColor = color;
        colorBox.style.marginRight = 5;
        colorBox.style.borderTopLeftRadius = 6;
        colorBox.style.borderTopRightRadius = 6;
        colorBox.style.borderBottomLeftRadius = 6;
        colorBox.style.borderBottomRightRadius = 6;
        item.Add(colorBox);
        
        var text = new Label(label);
        text.style.fontSize = 10;
        ApplyFont(text);
        item.Add(text);
        
        parent.Add(item);
    }
    
    void CreateStatCard(VisualElement parent, string label, string value, Color color)
    {
        var card = new VisualElement();
        card.style.backgroundColor = color;
        card.style.paddingTop = 12;
        card.style.paddingBottom = 12;
        card.style.paddingLeft = 15;
        card.style.paddingRight = 15;
        card.style.marginBottom = 10;
        card.style.borderTopLeftRadius = 6;
        card.style.borderTopRightRadius = 6;
        card.style.borderBottomLeftRadius = 6;
        card.style.borderBottomRightRadius = 6;
        
        var labelText = new Label(label);
        labelText.style.color = Color.white;
        labelText.style.fontSize = 12;
        labelText.style.marginBottom = 3;
        ApplyFont(labelText);
        card.Add(labelText);
        
        var valueText = new Label(value);
        valueText.style.color = Color.white;
        valueText.style.fontSize = 16;
        valueText.style.unityFontStyleAndWeight = FontStyle.Bold;
        ApplyFont(valueText);
        card.Add(valueText);
        
        parent.Add(card);
    }
    
    void CreateStatusItem(VisualElement parent, string label, int count, Color color)
    {
        var item = new VisualElement();
        item.style.flexDirection = FlexDirection.Row;
        item.style.justifyContent = Justify.SpaceBetween;
        item.style.alignItems = Align.Center;
        item.style.paddingTop = 5;
        item.style.paddingBottom = 5;
        item.style.paddingLeft = 10;
        item.style.paddingRight = 10;
        item.style.borderLeftWidth = 3;
        item.style.borderLeftColor = color;
        
        var labelText = new Label(label);
        labelText.style.fontSize = 12;
        ApplyFont(labelText);
        item.Add(labelText);
        
        var countText = new Label(count.ToString());
        countText.style.fontSize = 12;
        countText.style.unityFontStyleAndWeight = FontStyle.Bold;
        countText.style.color = color;
        ApplyFont(countText);
        item.Add(countText);
        
        parent.Add(item);
    }
    
    void CreateFooter(VisualElement parent)
    {
        var footer = new VisualElement();
        footer.style.height = 50;
        footer.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        footer.style.flexDirection = FlexDirection.Row;
        footer.style.justifyContent = Justify.Center;
        footer.style.alignItems = Align.Center;
        footer.style.borderBottomLeftRadius = 12;
        footer.style.borderBottomRightRadius = 12;
        
        var refreshButton = new Button(() => { RefreshMap(); });
        refreshButton.text = "刷新数据";
        refreshButton.style.width = 100;
        refreshButton.style.height = 30;
        refreshButton.style.backgroundColor = new Color(0.3f, 0.7f, 0.3f, 1f);
        refreshButton.style.color = Color.white;
        refreshButton.style.fontSize = 14;
        refreshButton.style.borderBottomWidth = 0;
        refreshButton.style.borderTopWidth = 0;
        refreshButton.style.borderLeftWidth = 0;
        refreshButton.style.borderRightWidth = 0;
        refreshButton.style.borderTopLeftRadius = 5;
        refreshButton.style.borderTopRightRadius = 5;
        refreshButton.style.borderBottomLeftRadius = 5;
        refreshButton.style.borderBottomRightRadius = 5;
        ApplyFontToButton(refreshButton);
        footer.Add(refreshButton);
        
        parent.Add(footer);
    }
    
    void RefreshMap()
    {
        // 清理电塔标记缓存
        towerMarkers.Clear();
        
        // 重新绘制地图
        if (mapCanvas != null)
        {
            mapCanvas.Clear();
            DrawGridBackground(mapCanvas);
            DrawPowerlineMap(mapCanvas);
            
            // 重新创建工具提示
            CreateTooltip(mapCanvas);
        }
    }
    
    System.Tuple<int, int, float, float> GetSystemStats()
    {
        if (sceneInitializer != null && sceneInitializer.powerlines != null && sceneInitializer.powerlines.Count > 0)
        {
            // 统计电塔数量
            int towerCount = 0;
            float totalLength = 0f;
            float totalHeight = 0f;
            
            foreach (var powerline in sceneInitializer.powerlines)
            {
                if (powerline.towerPositions != null)
                {
                    towerCount += powerline.towerPositions.Length;
                    totalHeight += powerline.towerPositions.Sum(pos => pos.y);
                }
                totalLength += powerline.length;
            }
            
            int wireCount = sceneInitializer.powerlines.Count;
            float avgHeight = towerCount > 0 ? totalHeight / towerCount + 20f : 25f; // 加上电塔高度估算
            
            return new System.Tuple<int, int, float, float>(towerCount, wireCount, totalLength, avgHeight);
        }
        else
        {
            // 尝试从场景中查找电塔对象
            var towerObjects = GameObject.FindObjectsOfType<GameObject>()
                .Where(go => go.name.Contains("Tower"))
                .ToArray();
                
            if (towerObjects.Length > 0)
            {
                float avgHeight = towerObjects.Average(t => t.transform.position.y) + 20f;
                return new System.Tuple<int, int, float, float>(towerObjects.Length, 3, 500f, avgHeight);
            }
            
            // 使用模拟数据
            return new System.Tuple<int, int, float, float>(8, 5, 1200.5f, 25.3f);
        }
    }
    
    public void HideViewer()
    {
        if (overviewPanel != null)
        {
            overviewPanel.RemoveFromHierarchy();
            overviewPanel = null;
        }
        isVisible = false;
    }
    
    void ApplyFont(Label label)
    {
        if (label != null)
        {
            // 优先使用FontManager
            if (FontManager.Instance != null)
            {
                var currentFont = FontManager.Instance.GetCurrentFont();
                if (currentFont != null)
                {
                    label.style.unityFont = currentFont;
                }
            }
            else
            {
                // 备用方案：使用Unity内建字体
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font != null)
                {
                    label.style.unityFont = font;
                }
            }
        }
    }
    
    void ApplyFontToButton(Button button)
    {
        if (button != null)
        {
            // 优先使用FontManager
            if (FontManager.Instance != null)
            {
                var currentFont = FontManager.Instance.GetCurrentFont();
                if (currentFont != null)
                {
                    button.style.unityFont = currentFont;
                }
            }
            else
            {
                // 备用方案：使用Unity内建字体
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font != null)
                {
                    button.style.unityFont = font;
                }
            }
            button.style.fontSize = 14;
        }
    }
    
    void OnDestroy()
    {
        HideViewer();
    }
}