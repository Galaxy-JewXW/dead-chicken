using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace UI
{
    /// <summary>
    /// 场景总览管理器 - 完整的场景总览功能
    /// 包含电力系统线路总览图、电塔交互、统计信息等
    /// </summary>
    public class SceneOverviewManager : MonoBehaviour
{
    [Header("场景总览配置")]
    public SceneInitializer sceneInitializer;
    public Font uiFont;
    

    
    [Header("UI颜色设置")]
    public Color primaryColor = new Color(0.2f, 0.4f, 0.7f, 1f);
    public Color dangerColor = new Color(1f, 0.2f, 0.2f, 1f);
    public Color towerColor = new Color(0.2f, 0.6f, 0.9f, 1f);
    public Color wireColor = new Color(0.8f, 0.7f, 0.5f, 1f);
    public Color groundWireColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    
    private bool isOverviewVisible = false;
    private VisualElement currentOverlay = null;
    private float mapZoom = 1.0f;
    private Vector2 mapOffset = Vector2.zero;
    private VisualElement mapContainer = null;
    private Label zoomLabel = null; // 添加缩放标签的引用
    private VisualElement miniMapIndicator = null; // 添加小地图指示器的引用
    
    // 测量工具相关变量
    private bool isMeasuring = false;
    private Vector3 measureStartPos;
    private Vector2 measureStartUIPos;
    private VisualElement measureLine = null;
    private VisualElement measureStartMarker = null;
    private VisualElement measureEndMarker = null;
    private Label measureDistanceLabel = null;
    private Button measureButton = null; // 添加测量按钮的引用
    private VisualElement sliderHandle = null; // 添加滑块手柄引用
    
    // 无人机巡检管理器
    private DronePatrolManager dronePatrolManager = null;
    
    // 拖拽相关变量
    private bool isDragging = false;
    private Vector2 lastMousePosition = Vector2.zero;
    private bool isMouseDown = false;
    private float clickStartTime = 0f;
    private Vector2 clickStartPosition = Vector2.zero;
    private const float CLICK_THRESHOLD_TIME = 0.3f;
    private const float CLICK_THRESHOLD_DISTANCE = 5f;
    
    [System.Serializable]
    public struct TowerData
    {
        public Vector3 position;
        public string name;
        public string status; // "normal", "warning", "error"
        public float height;
    }

    [System.Serializable]
    public struct DangerData
    {
        public Vector3 position;
        public string name;
        public DangerType dangerType;
        public DangerLevel dangerLevel;
        public string description;
    }
    
    void Start()
    {
        // 自动查找组件
        if (sceneInitializer == null)
        {
            sceneInitializer = FindObjectOfType<SceneInitializer>();
            if (sceneInitializer != null)
            {
                Debug.Log("SceneInitializer 组件已找到");
            }
            else
            {
                Debug.LogWarning("未找到 SceneInitializer 组件，将使用模拟数据");
            }
        }
        
        // 初始化无人机巡检管理器
        dronePatrolManager = FindObjectOfType<DronePatrolManager>();
        if (dronePatrolManager == null)
        {
            // 如果场景中没有DronePatrolManager，创建一个
            GameObject dronePatrolObj = new GameObject("DronePatrolManager");
            dronePatrolManager = dronePatrolObj.AddComponent<DronePatrolManager>();
            Debug.Log("创建了DronePatrolManager组件");
        }
    }
    
    /// <summary>
    /// 显示场景总览
    /// </summary>
    public void ShowSceneOverview()
    {
        if (isOverviewVisible)
        {
            HideSceneOverview();
            return;
        }
        
        Debug.Log("显示场景总览弹窗");
        ShowSceneOverviewPanel();
    }
    
    /// <summary>
    /// 隐藏场景总览
    /// </summary>
    public void HideSceneOverview()
    {
        if (currentOverlay != null && currentOverlay.parent != null)
        {
            currentOverlay.RemoveFromHierarchy();
            currentOverlay = null;
        }
        isOverviewVisible = false;
        mapContainer = null;
        zoomLabel = null; // 重置缩放标签引用
        miniMapIndicator = null; // 重置小地图指示器引用
        measureButton = null; // 重置测量按钮引用
        sliderHandle = null; // 重置滑块引用
        
        // 清除测量相关引用
        ClearMeasureElements();
        // 保持缩放状态，下次打开时继续使用
        Debug.Log("场景总览弹窗已隐藏");
    }
    
    /// <summary>
    /// 显示场景总览面板
    /// </summary>
    void ShowSceneOverviewPanel()
    {
        try
        {
            var uiDocument = FindObjectOfType<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("未找到UIDocument");
                return;
            }
            
            // 创建场景总览弹窗
            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.top = 0;
            overlay.style.right = 0;
            overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.7f);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;
            
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
            
            // 创建标题栏
            CreateHeader(container);
            
            // 创建内容区域
            CreateContent(container);
            
            overlay.Add(container);
            
            // 点击背景关闭
            overlay.RegisterCallback<ClickEvent>(evt => {
                if (evt.target == overlay)
                {
                    HideSceneOverview();
                }
            });
            
            uiDocument.rootVisualElement.Add(overlay);
            currentOverlay = overlay;
            isOverviewVisible = true;
            
            Debug.Log("场景总览弹窗创建成功");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"创建场景总览面板时出错: {e.Message}");
        }
    }
    
    /// <summary>
    /// 创建标题栏
    /// </summary>
    void CreateHeader(VisualElement container)
    {
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.height = 60;
        header.style.backgroundColor = primaryColor;
        header.style.paddingLeft = 25;
        header.style.paddingRight = 25;
        header.style.borderTopLeftRadius = 12;
        header.style.borderTopRightRadius = 12;
        
        var title = new Label("电力系统场景总览");
        title.style.color = Color.white;
        title.style.fontSize = 24;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        ApplyFont(title);
        header.Add(title);
        
        var closeButton = new Button(() => HideSceneOverview());
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
        ApplyFont(closeButton);
        header.Add(closeButton);
        
        container.Add(header);
    }
    
    /// <summary>
    /// 创建内容区域
    /// </summary>
    void CreateContent(VisualElement container)
    {
        var content = new VisualElement();
        content.style.flexDirection = FlexDirection.Row;
        content.style.flexGrow = 1;
        content.style.paddingTop = 15;
        content.style.paddingBottom = 15;
        content.style.paddingLeft = 15;
        content.style.paddingRight = 15;
        
        // 左侧：地图区域
        CreateMapArea(content);
        
        // 右侧：统计面板
        CreateStatsPanel(content);
        
        container.Add(content);
    }
    
    /// <summary>
    /// 创建地图区域
    /// </summary>
    void CreateMapArea(VisualElement content)
    {
        var mapArea = new VisualElement();
        mapArea.style.flexGrow = 1;
        mapArea.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        mapArea.style.marginTop = 10;
        mapArea.style.marginBottom = 10;
        mapArea.style.marginLeft = 10;
        mapArea.style.marginRight = 10;
        mapArea.style.borderTopLeftRadius = 8;
        mapArea.style.borderTopRightRadius = 8;
        mapArea.style.borderBottomLeftRadius = 8;
        mapArea.style.borderBottomRightRadius = 8;
        mapArea.style.flexDirection = FlexDirection.Column;
        
        // 创建工具栏
        var toolbar = CreateMapToolbar(mapArea);
        toolbar.name = "map-toolbar";
        mapArea.Add(toolbar);
        
        // 创建地图容器
        var mapContainer = new VisualElement();
        mapContainer.style.flexGrow = 1;
        mapContainer.style.position = Position.Relative;
        mapContainer.style.overflow = Overflow.Hidden;
        
        // 设置地图交互
        SetupMapInteraction(mapContainer);
        this.mapContainer = mapContainer;
        
        mapArea.Add(mapContainer);
        
        // 创建小地图/导航窗口
        var miniMap = CreateMiniMap();
        mapArea.Add(miniMap);
        
        // 绘制电力线路图
        DrawPowerlineMap(mapContainer);
        
        content.Add(mapArea);
    }
    
    /// <summary>
    /// 创建小地图/导航预览窗口
    /// </summary>
    VisualElement CreateMiniMap()
    {
        var miniMapContainer = new VisualElement();
        miniMapContainer.style.position = Position.Absolute;
        miniMapContainer.style.bottom = 15;
        miniMapContainer.style.right = 15;
        miniMapContainer.style.width = 180;
        miniMapContainer.style.height = 120;
        miniMapContainer.style.backgroundColor = new Color(1f, 1f, 1f, 0.95f);
        miniMapContainer.style.borderTopLeftRadius = 6;
        miniMapContainer.style.borderTopRightRadius = 6;
        miniMapContainer.style.borderBottomLeftRadius = 6;
        miniMapContainer.style.borderBottomRightRadius = 6;
        miniMapContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        miniMapContainer.style.borderBottomWidth = 1;
        miniMapContainer.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        miniMapContainer.style.borderTopWidth = 1;
        miniMapContainer.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        miniMapContainer.style.borderLeftWidth = 1;
        miniMapContainer.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        miniMapContainer.style.borderRightWidth = 1;
        
        // 小地图标题
        var titleBar = new VisualElement();
        titleBar.style.height = 25;
        titleBar.style.backgroundColor = new Color(0.2f, 0.4f, 0.7f, 1f);
        titleBar.style.borderTopLeftRadius = 5;
        titleBar.style.borderTopRightRadius = 5;
        titleBar.style.flexDirection = FlexDirection.Row;
        titleBar.style.alignItems = Align.Center;
        titleBar.style.justifyContent = Justify.Center;
        
        var titleLabel = new Label("导航");
        titleLabel.style.color = Color.white;
        titleLabel.style.fontSize = 11;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        ApplyFont(titleLabel);
        titleBar.Add(titleLabel);
        
        // 小地图内容区域
        var miniMapContent = new VisualElement();
        miniMapContent.style.flexGrow = 1;
        miniMapContent.style.position = Position.Relative;
        miniMapContent.style.marginTop = 5;
        miniMapContent.style.marginBottom = 5;
        miniMapContent.style.marginLeft = 5;
        miniMapContent.style.marginRight = 5;
        
        // 绘制小地图背景网格
        DrawMiniMapGrid(miniMapContent);
        
        // 绘制小地图内容
        DrawMiniMapContent(miniMapContent);
        
        // 绘制当前视图区域指示器
        var viewIndicator = CreateViewIndicator();
        miniMapContent.Add(viewIndicator);
        
        miniMapContainer.Add(titleBar);
        miniMapContainer.Add(miniMapContent);
        
        // 设置小地图交互
        SetupMiniMapInteraction(miniMapContent);
        
        // 存储指示器引用
        miniMapIndicator = viewIndicator;
        
        return miniMapContainer;
    }
    
    /// <summary>
    /// 绘制小地图网格背景
    /// </summary>
    void DrawMiniMapGrid(VisualElement container)
    {
        // 创建网格背景
        for (int i = 0; i <= 4; i++)
        {
            // 垂直线
            var vLine = new VisualElement();
            vLine.style.position = Position.Absolute;
            vLine.style.left = Length.Percent(i * 25);
            vLine.style.top = 0;
            vLine.style.bottom = 0;
            vLine.style.width = 1;
            vLine.style.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            container.Add(vLine);
            
            // 水平线
            var hLine = new VisualElement();
            hLine.style.position = Position.Absolute;
            hLine.style.top = Length.Percent(i * 25);
            hLine.style.left = 0;
            hLine.style.right = 0;
            hLine.style.height = 1;
            hLine.style.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            container.Add(hLine);
        }
    }
    
    /// <summary>
    /// 绘制小地图内容
    /// </summary>
    void DrawMiniMapContent(VisualElement container)
    {
        var towers = GetTowerData();
        var dangers = GetDangerData();
        var powerlines = GetPowerlineData();
        
        if (towers.Count == 0) return;
        
        var bounds = CalculateMapBounds(towers, dangers);
        float scale = CalculateMapScale(bounds, 160, 80); // 小地图尺寸
        
        // 绘制电力线
        foreach (var powerline in powerlines)
        {
            if (powerline.points != null && powerline.points.Count >= 2)
            {
                for (int i = 0; i < powerline.points.Count - 1; i++)
                {
                    Vector2 start = WorldToMiniMapPosition(powerline.points[i], bounds, 160, 80);
                    Vector2 end = WorldToMiniMapPosition(powerline.points[i + 1], bounds, 160, 80);
                    DrawMiniMapLine(container, start, end, new Color(0.8f, 0.7f, 0.5f, 1f));
                }
            }
        }
        
        // 绘制电塔
        foreach (var tower in towers)
        {
            Vector2 pos = WorldToMiniMapPosition(tower.position, bounds, 160, 80);
            DrawMiniMapPoint(container, pos, towerColor, 3);
        }
        
        // 绘制危险物
        foreach (var danger in dangers)
        {
            Vector2 pos = WorldToMiniMapPosition(danger.position, bounds, 160, 80);
            DrawMiniMapPoint(container, pos, GetDangerLevelColor(danger.dangerLevel), 2);
        }
    }
    
    /// <summary>
    /// 世界坐标转换为小地图位置
    /// </summary>
    Vector2 WorldToMiniMapPosition(Vector3 worldPos, Bounds bounds, float mapWidth, float mapHeight)
    {
        Vector3 localPos = worldPos - bounds.min;
        float normalizedX = localPos.x / bounds.size.x;
        float normalizedZ = localPos.z / bounds.size.z;
        
        float x = normalizedX * 100f;
        float y = normalizedZ * 100f;
        
        return new Vector2(x, y);
    }
    
    /// <summary>
    /// 在小地图上绘制线条
    /// </summary>
    void DrawMiniMapLine(VisualElement container, Vector2 start, Vector2 end, Color color)
    {
        var line = new VisualElement();
        line.style.position = Position.Absolute;
        line.style.left = Length.Percent(start.x);
        line.style.top = Length.Percent(start.y);
        line.style.width = 2;
        line.style.height = 2;
        line.style.backgroundColor = color;
        container.Add(line);
    }
    
    /// <summary>
    /// 在小地图上绘制点
    /// </summary>
    void DrawMiniMapPoint(VisualElement container, Vector2 position, Color color, float size)
    {
        var point = new VisualElement();
        point.style.position = Position.Absolute;
        point.style.left = Length.Percent(position.x - size/2);
        point.style.top = Length.Percent(position.y - size/2);
        point.style.width = size;
        point.style.height = size;
        point.style.backgroundColor = color;
        point.style.borderTopLeftRadius = size/2;
        point.style.borderTopRightRadius = size/2;
        point.style.borderBottomLeftRadius = size/2;
        point.style.borderBottomRightRadius = size/2;
        container.Add(point);
    }
    
    /// <summary>
    /// 创建当前视图区域指示器
    /// </summary>
    VisualElement CreateViewIndicator()
    {
        var indicator = new VisualElement();
        indicator.style.position = Position.Absolute;
        indicator.style.borderBottomColor = new Color(1f, 0f, 0f, 0.8f);
        indicator.style.borderBottomWidth = 2;
        indicator.style.borderTopColor = new Color(1f, 0f, 0f, 0.8f);
        indicator.style.borderTopWidth = 2;
        indicator.style.borderLeftColor = new Color(1f, 0f, 0f, 0.8f);
        indicator.style.borderLeftWidth = 2;
        indicator.style.borderRightColor = new Color(1f, 0f, 0f, 0.8f);
        indicator.style.borderRightWidth = 2;
        indicator.style.backgroundColor = new Color(1f, 0f, 0f, 0.1f);
        indicator.pickingMode = PickingMode.Ignore;
        
        UpdateViewIndicator(indicator);
        
        return indicator;
    }
    
    /// <summary>
    /// 更新视图指示器位置和大小
    /// </summary>
    void UpdateViewIndicator(VisualElement indicator)
    {
        if (indicator == null) return;
        
        // 计算当前视图在整体地图中的范围
        float viewWidth = 100f / mapZoom;
        float viewHeight = 100f / mapZoom;
        
        // 计算视图中心位置
        float centerX = 50f - mapOffset.x / mapZoom;
        float centerY = 50f - mapOffset.y / mapZoom;
        
        // 设置指示器位置和大小
        indicator.style.left = Length.Percent(centerX - viewWidth / 2);
        indicator.style.top = Length.Percent(centerY - viewHeight / 2);
        indicator.style.width = Length.Percent(viewWidth);
        indicator.style.height = Length.Percent(viewHeight);
    }
    
    /// <summary>
    /// 设置小地图交互
    /// </summary>
    void SetupMiniMapInteraction(VisualElement miniMapContent)
    {
        miniMapContent.RegisterCallback<ClickEvent>(evt =>
        {
            // 点击小地图跳转到对应位置
            var rect = miniMapContent.worldBound;
            Vector2 localPos = new Vector2(evt.position.x - rect.position.x, evt.position.y - rect.position.y);
            Vector2 normalizedPos = new Vector2(localPos.x / rect.width, localPos.y / rect.height);
            
            // 转换为地图百分比坐标
            float targetX = normalizedPos.x * 100f;
            float targetY = normalizedPos.y * 100f;
            
            // 设置偏移使目标点居中
            mapOffset.x = (50f - targetX) * mapZoom;
            mapOffset.y = (50f - targetY) * mapZoom;
            
            RefreshMapView();
            evt.StopPropagation();
        });
    }
    
    /// <summary>
    /// 设置地图交互
    /// </summary>
    void SetupMapInteraction(VisualElement mapContainer)
    {
        // 滚轮缩放
        mapContainer.RegisterCallback<WheelEvent>(evt =>
        {
            // 调整缩放敏感度，使其更容易控制
            float deltaZoom = -evt.delta.y * 1f;
            deltaZoom = Mathf.Clamp(deltaZoom, -0.3f, 0.3f);
            
            float newZoom = Mathf.Clamp(mapZoom + deltaZoom, 0.5f, 3.0f);
            
            if (newZoom != mapZoom)
            {
                // 计算缩放中心点（相对于鼠标位置）
                var containerRect = mapContainer.worldBound;
                Vector2 mousePos = new Vector2(evt.mousePosition.x - containerRect.position.x, evt.mousePosition.y - containerRect.position.y);
                Vector2 normalizedMousePos = new Vector2(
                    mousePos.x / containerRect.width,
                    mousePos.y / containerRect.height
                );
                
                // 调整偏移以实现以鼠标为中心的缩放
                Vector2 zoomCenter = normalizedMousePos - Vector2.one * 0.5f;
                float zoomDelta = newZoom - mapZoom;
                mapOffset -= zoomCenter * zoomDelta * 100f;
                
                mapZoom = newZoom;
                Debug.Log($"滚轮缩放: {mapZoom:F1}x");
                RefreshMapView();
            }
            
            evt.StopPropagation();
        });
        
        // 鼠标按下
        mapContainer.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button == 0) // 左键
            {
                isMouseDown = true;
                isDragging = false;
                lastMousePosition = evt.mousePosition;
                clickStartTime = Time.time;
                clickStartPosition = evt.mousePosition;
                evt.StopPropagation();
            }
        });
        
        // 鼠标移动
        mapContainer.RegisterCallback<MouseMoveEvent>(evt =>
        {
            if (isMouseDown)
            {
                Vector2 currentMousePosition = evt.mousePosition;
                Vector2 deltaPos = currentMousePosition - lastMousePosition;
                
                // 检查是否超过点击阈值，开始拖拽
                float distanceFromStart = Vector2.Distance(currentMousePosition, clickStartPosition);
                if (!isDragging && distanceFromStart > CLICK_THRESHOLD_DISTANCE)
                {
                    isDragging = true;
                }
                
                if (isDragging)
                {
                    // 更新地图偏移
                    var containerRect = mapContainer.worldBound;
                    Vector2 normalizedDelta = new Vector2(
                        deltaPos.x / containerRect.width * 100f,
                        deltaPos.y / containerRect.height * 100f
                    );
                    
                    mapOffset += normalizedDelta;
                    RefreshMapView();
                }
                
                lastMousePosition = currentMousePosition;
                evt.StopPropagation();
            }
        });
        
        // 鼠标抬起
        mapContainer.RegisterCallback<MouseUpEvent>(evt =>
        {
            if (evt.button == 0) // 左键
            {
                bool wasClick = !isDragging && 
                              (Time.time - clickStartTime) < CLICK_THRESHOLD_TIME &&
                              Vector2.Distance(evt.mousePosition, clickStartPosition) < CLICK_THRESHOLD_DISTANCE;
                
                isMouseDown = false;
                isDragging = false;
                
                // 如果是点击而不是拖拽
                if (wasClick)
                {
                    // 如果测量工具激活，处理测量点击
                    if (isMeasuring)
                    {
                        // 获取鼠标在地图容器中的本地坐标
                        var containerRect = mapContainer.worldBound;
                        Vector2 localPos = new Vector2(evt.mousePosition.x - containerRect.position.x, evt.mousePosition.y - containerRect.position.y);
                        
                        // 直接传递UI坐标给测量处理函数
                        HandleMeasureClickWithUIPos(localPos);
                        evt.StopPropagation();
                    }
                }
                else
                {
                    evt.StopPropagation();
                }
            }
        });
        
        // 鼠标离开容器
        mapContainer.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            isMouseDown = false;
            isDragging = false;
        });
    }

    /// <summary>
    /// 创建地图工具栏
    /// </summary>
    VisualElement CreateMapToolbar(VisualElement mapContainer)
    {
        var toolbar = new VisualElement();
        toolbar.style.flexDirection = FlexDirection.Row;
        toolbar.style.justifyContent = Justify.SpaceBetween;
        toolbar.style.alignItems = Align.Center;
        toolbar.style.height = 40;
        toolbar.style.paddingLeft = 10;
        toolbar.style.paddingRight = 10;
        toolbar.style.backgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        
        // 左侧：图例
        var legend = new VisualElement();
        legend.style.flexDirection = FlexDirection.Row;
        legend.style.alignItems = Align.Center;
        
        CreateLegendItem(legend, "电塔", towerColor);
        CreateLegendItem(legend, "导线", wireColor);
        CreateLegendItem(legend, "地线", groundWireColor);
        CreateLegendItem(legend, "危险物", dangerColor);
        
        toolbar.Add(legend);
        
        // 中间：搜索功能
        var searchContainer = new VisualElement();
        searchContainer.style.flexDirection = FlexDirection.Row;
        searchContainer.style.alignItems = Align.Center;
        searchContainer.style.marginLeft = 20;
        searchContainer.style.marginRight = 20;
        
        // 搜索输入框
        var searchField = new TextField();
        searchField.style.width = 150;
        searchField.style.height = 25;
        searchField.style.fontSize = 12;
        searchField.style.backgroundColor = Color.white;
        searchField.style.borderBottomColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        searchField.style.borderBottomWidth = 1;
        searchField.style.borderTopColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        searchField.style.borderTopWidth = 1;
        searchField.style.borderLeftColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        searchField.style.borderLeftWidth = 1;
        searchField.style.borderRightColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        searchField.style.borderRightWidth = 1;
        searchField.style.borderTopLeftRadius = 3;
        searchField.style.borderTopRightRadius = 0;
        searchField.style.borderBottomLeftRadius = 3;
        searchField.style.borderBottomRightRadius = 0;
        searchField.style.paddingLeft = 8;
        searchField.style.paddingRight = 8;
        searchField.value = "";
        ApplyFont(searchField);
        
        // 设置占位符文本效果
        var placeholderLabel = new Label("搜索电塔或危险物...");
        placeholderLabel.style.position = Position.Absolute;
        placeholderLabel.style.left = 10;
        placeholderLabel.style.top = 4;
        placeholderLabel.style.fontSize = 12;
        placeholderLabel.style.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        placeholderLabel.pickingMode = PickingMode.Ignore;
        ApplyFont(placeholderLabel);
        
        // 搜索输入事件处理
        searchField.RegisterCallback<ChangeEvent<string>>(evt =>
        {
            placeholderLabel.style.display = string.IsNullOrEmpty(evt.newValue) ? DisplayStyle.Flex : DisplayStyle.None;
        });
        
        searchField.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                PerformSearch(evt.target as TextField);
            }
        });
        
        var searchInputContainer = new VisualElement();
        searchInputContainer.style.position = Position.Relative;
        searchInputContainer.Add(searchField);
        searchInputContainer.Add(placeholderLabel);
        
        // 搜索按钮
        var searchButton = new Button(() => PerformSearch(searchField));
        searchButton.text = "🔍";
        searchButton.style.width = 30;
        searchButton.style.height = 25;
        searchButton.style.fontSize = 14;
        searchButton.style.backgroundColor = towerColor;
        searchButton.style.color = Color.white;
        searchButton.style.borderBottomWidth = 0;
        searchButton.style.borderTopWidth = 0;
        searchButton.style.borderLeftWidth = 0;
        searchButton.style.borderRightWidth = 0;
        searchButton.style.borderTopLeftRadius = 0;
        searchButton.style.borderTopRightRadius = 3;
        searchButton.style.borderBottomLeftRadius = 0;
        searchButton.style.borderBottomRightRadius = 3;
        
        searchContainer.Add(searchInputContainer);
        searchContainer.Add(searchButton);
        
        toolbar.Add(searchContainer);
        
        // 右侧：缩放和控制信息
        var controlsInfo = new VisualElement();
        controlsInfo.style.flexDirection = FlexDirection.Row;
        controlsInfo.style.alignItems = Align.Center;
        
        // 操作提示
        var hintLabel = new Label("滚轮缩放 | 左键拖拽移动");
        hintLabel.style.fontSize = 10;
        hintLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        hintLabel.style.marginRight = 15;
        ApplyFont(hintLabel);
        
        // 缩放控制区域
        var zoomControls = new VisualElement();
        zoomControls.style.flexDirection = FlexDirection.Row;
        zoomControls.style.alignItems = Align.Center;
        zoomControls.style.marginRight = 15;
        
        // 缩放减号按钮
        var zoomOutButton = new Button(() => {
            float newZoom = Mathf.Clamp(mapZoom - 0.2f, 0.5f, 3.0f);
            if (newZoom != mapZoom)
            {
                mapZoom = newZoom;
                RefreshMapView();
                UpdateZoomSlider();
            }
        });
        zoomOutButton.text = "−";
        zoomOutButton.style.width = 25;
        zoomOutButton.style.height = 25;
        zoomOutButton.style.fontSize = 16;
        zoomOutButton.style.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        zoomOutButton.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        zoomOutButton.style.borderBottomWidth = 0;
        zoomOutButton.style.borderTopWidth = 0;
        zoomOutButton.style.borderLeftWidth = 0;
        zoomOutButton.style.borderRightWidth = 0;
        zoomOutButton.style.borderTopLeftRadius = 3;
        zoomOutButton.style.borderTopRightRadius = 0;
        zoomOutButton.style.borderBottomLeftRadius = 3;
        zoomOutButton.style.borderBottomRightRadius = 0;
        ApplyFont(zoomOutButton);
        
        // 缩放滑块容器
        var sliderContainer = new VisualElement();
        sliderContainer.style.width = 80;
        sliderContainer.style.height = 25;
        sliderContainer.style.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        sliderContainer.style.flexDirection = FlexDirection.Row;
        sliderContainer.style.alignItems = Align.Center;
        sliderContainer.style.paddingLeft = 5;
        sliderContainer.style.paddingRight = 5;
        
        // 创建自定义滑块
        var sliderTrack = new VisualElement();
        sliderTrack.style.height = 3;
        sliderTrack.style.flexGrow = 1;
        sliderTrack.style.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        sliderTrack.style.borderTopLeftRadius = 2;
        sliderTrack.style.borderTopRightRadius = 2;
        sliderTrack.style.borderBottomLeftRadius = 2;
        sliderTrack.style.borderBottomRightRadius = 2;
        sliderTrack.style.position = Position.Relative;
        
        sliderHandle = new VisualElement();
        sliderHandle.style.width = 12;
        sliderHandle.style.height = 12;
        sliderHandle.style.backgroundColor = towerColor;
        sliderHandle.style.position = Position.Absolute;
        sliderHandle.style.top = -4; // 居中在轨道上
        sliderHandle.style.borderTopLeftRadius = 6;
        sliderHandle.style.borderTopRightRadius = 6;
        sliderHandle.style.borderBottomLeftRadius = 6;
        sliderHandle.style.borderBottomRightRadius = 6;
        sliderHandle.style.borderBottomColor = Color.white;
        sliderHandle.style.borderBottomWidth = 1;
        sliderHandle.style.borderTopColor = Color.white;
        sliderHandle.style.borderTopWidth = 1;
        sliderHandle.style.borderLeftColor = Color.white;
        sliderHandle.style.borderLeftWidth = 1;
        sliderHandle.style.borderRightColor = Color.white;
        sliderHandle.style.borderRightWidth = 1;
        
        // 设置滑块交互
        bool isDraggingSlider = false;
        sliderContainer.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button == 0)
            {
                isDraggingSlider = true;
                UpdateZoomFromSlider(evt.localMousePosition.x, sliderContainer.resolvedStyle.width);
                evt.StopPropagation();
            }
        });
        
        sliderContainer.RegisterCallback<MouseMoveEvent>(evt =>
        {
            if (isDraggingSlider)
            {
                UpdateZoomFromSlider(evt.localMousePosition.x, sliderContainer.resolvedStyle.width);
                evt.StopPropagation();
            }
        });
        
        sliderContainer.RegisterCallback<MouseUpEvent>(evt =>
        {
            if (evt.button == 0)
            {
                isDraggingSlider = false;
                evt.StopPropagation();
            }
        });
        
        sliderTrack.Add(sliderHandle);
        sliderContainer.Add(sliderTrack);
        
        // 缩放加号按钮
        var zoomInButton = new Button(() => {
            float newZoom = Mathf.Clamp(mapZoom + 0.2f, 0.5f, 3.0f);
            if (newZoom != mapZoom)
            {
                mapZoom = newZoom;
                RefreshMapView();
                UpdateZoomSlider();
            }
        });
        zoomInButton.text = "+";
        zoomInButton.style.width = 25;
        zoomInButton.style.height = 25;
        zoomInButton.style.fontSize = 16;
        zoomInButton.style.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        zoomInButton.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        zoomInButton.style.borderBottomWidth = 0;
        zoomInButton.style.borderTopWidth = 0;
        zoomInButton.style.borderLeftWidth = 0;
        zoomInButton.style.borderRightWidth = 0;
        zoomInButton.style.borderTopLeftRadius = 0;
        zoomInButton.style.borderTopRightRadius = 3;
        zoomInButton.style.borderBottomLeftRadius = 0;
        zoomInButton.style.borderBottomRightRadius = 3;
        ApplyFont(zoomInButton);
        
        // 缩放显示标签
        zoomLabel = new Label($"{mapZoom:F1}x");
        zoomLabel.style.fontSize = 12;
        zoomLabel.style.marginLeft = 8;
        zoomLabel.style.marginRight = 10;
        zoomLabel.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        zoomLabel.style.minWidth = 35;
        ApplyFont(zoomLabel);
        
        zoomControls.Add(zoomOutButton);
        zoomControls.Add(sliderContainer);
        zoomControls.Add(zoomInButton);
        zoomControls.Add(zoomLabel);
        
        // 测量工具按钮
        measureButton = new Button(() => ToggleMeasureTool());
        measureButton.text = "测距";
        measureButton.style.height = 28;
        measureButton.style.width = 45;
        measureButton.style.fontSize = 11;
        measureButton.style.paddingLeft = 6;
        measureButton.style.paddingRight = 6;
        measureButton.style.paddingTop = 4;
        measureButton.style.paddingBottom = 4;
        measureButton.style.backgroundColor = isMeasuring ? new Color(0.8f, 0.3f, 0.3f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);
        measureButton.style.color = Color.white;
        measureButton.style.borderBottomWidth = 0;
        measureButton.style.borderTopWidth = 0;
        measureButton.style.borderLeftWidth = 0;
        measureButton.style.borderRightWidth = 0;
        measureButton.style.borderTopLeftRadius = 4;
        measureButton.style.borderTopRightRadius = 4;
        measureButton.style.borderBottomLeftRadius = 4;
        measureButton.style.borderBottomRightRadius = 4;
        measureButton.style.marginRight = 5;
        measureButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(measureButton);
        
        // 重置按钮
        var resetButton = new Button(() => ResetMapView());
        resetButton.text = "重置";
        resetButton.style.height = 28;
        resetButton.style.width = 45;
        resetButton.style.fontSize = 11;
        resetButton.style.paddingLeft = 6;
        resetButton.style.paddingRight = 6;
        resetButton.style.paddingTop = 4;
        resetButton.style.paddingBottom = 4;
        resetButton.style.backgroundColor = towerColor;
        resetButton.style.color = Color.white;
        resetButton.style.borderBottomWidth = 0;
        resetButton.style.borderTopWidth = 0;
        resetButton.style.borderLeftWidth = 0;
        resetButton.style.borderRightWidth = 0;
        resetButton.style.borderTopLeftRadius = 4;
        resetButton.style.borderTopRightRadius = 4;
        resetButton.style.borderBottomLeftRadius = 4;
        resetButton.style.borderBottomRightRadius = 4;
        resetButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        ApplyFont(resetButton);
        
        controlsInfo.Add(hintLabel);
        controlsInfo.Add(zoomControls);
        controlsInfo.Add(measureButton);
        controlsInfo.Add(resetButton);
        
        // 存储滑块引用用于更新（添加到全局变量以便访问）
        sliderContainer.userData = sliderHandle;
        
        // 同时存储到mapContainer以保持兼容性
        mapContainer.userData = sliderHandle;
        
        toolbar.Add(controlsInfo);
        
        // 初始化滑块位置
        UpdateZoomSlider();
        
        return toolbar;
    }
    
    /// <summary>
    /// 执行搜索功能
    /// </summary>
    void PerformSearch(TextField searchField)
    {
        string searchText = searchField.value?.Trim().ToLower();
        if (string.IsNullOrEmpty(searchText))
        {
            return;
        }
        
        // 搜索电塔
        var towers = GetTowerData();
        var foundTower = towers.FirstOrDefault(t => t.name.ToLower().Contains(searchText));
        
        if (foundTower.name != null)
        {
            // 找到电塔，跳转到该位置
            FocusOnPosition(foundTower.position, $"找到电塔: {foundTower.name}");
            return;
        }
        
        // 搜索危险物
        var dangers = GetDangerData();
        var foundDanger = dangers.FirstOrDefault(d => 
            d.name.ToLower().Contains(searchText) ||
            GetDangerTypeString(d.dangerType).ToLower().Contains(searchText) ||
            GetDangerLevelString(d.dangerLevel).ToLower().Contains(searchText)
        );
        
        if (foundDanger.name != null)
        {
            // 找到危险物，跳转到该位置
            FocusOnPosition(foundDanger.position, $"找到危险物: {foundDanger.name}");
            return;
        }
        
        // 未找到结果
        ShowSearchResult("未找到匹配结果");
    }
    
    /// <summary>
    /// 聚焦到指定位置
    /// </summary>
    void FocusOnPosition(Vector3 worldPosition, string message)
    {
        // 计算地图边界
        var towers = GetTowerData();
        var dangers = GetDangerData();
        var bounds = CalculateMapBounds(towers, dangers);
        
        // 将世界坐标转换为地图百分比坐标
        Vector3 localPos = worldPosition - bounds.min;
        float normalizedX = localPos.x / bounds.size.x;
        float normalizedZ = localPos.z / bounds.size.z;
        
        // 计算需要的偏移量，使目标点居中
        float targetX = normalizedX * 100f;
        float targetY = normalizedZ * 100f;
        
        // 设置偏移使目标点居中
        mapOffset.x = (50f - targetX) * mapZoom;
        mapOffset.y = (50f - targetY) * mapZoom;
        
        // 设置合适的缩放级别以便查看
        if (mapZoom < 1.5f)
        {
            mapZoom = 1.5f;
        }
        
        RefreshMapView();
        ShowSearchResult(message);
    }
    
    /// <summary>
    /// 显示搜索结果消息
    /// </summary>
    void ShowSearchResult(string message)
    {
        // 创建临时消息提示
        if (mapContainer != null)
        {
            var messageLabel = new Label(message);
            messageLabel.style.position = Position.Absolute;
            messageLabel.style.top = 50;
            messageLabel.style.left = Length.Percent(50);
            messageLabel.style.translate = new Translate(Length.Percent(-50), 0);
            messageLabel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            messageLabel.style.color = Color.white;
            messageLabel.style.fontSize = 12;
            messageLabel.style.paddingTop = 8;
            messageLabel.style.paddingBottom = 8;
            messageLabel.style.paddingLeft = 15;
            messageLabel.style.paddingRight = 15;
            messageLabel.style.borderTopLeftRadius = 4;
            messageLabel.style.borderTopRightRadius = 4;
            messageLabel.style.borderBottomLeftRadius = 4;
            messageLabel.style.borderBottomRightRadius = 4;
            ApplyFont(messageLabel);
            
            mapContainer.Add(messageLabel);
            
            // 2秒后自动移除消息
            StartCoroutine(RemoveMessageAfterDelay(messageLabel, 2.0f));
        }
    }
    
    /// <summary>
    /// 延迟移除消息
    /// </summary>
    System.Collections.IEnumerator RemoveMessageAfterDelay(VisualElement element, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (element != null && element.parent != null)
        {
            element.RemoveFromHierarchy();
        }
    }
    
    // 添加滑块相关的辅助方法
    void UpdateZoomFromSlider(float mouseX, float containerWidth)
    {
        float normalizedPos = Mathf.Clamp01(mouseX / containerWidth);
        float newZoom = Mathf.Lerp(0.5f, 3.0f, normalizedPos);
        
        if (Mathf.Abs(newZoom - mapZoom) > 0.05f)
        {
            mapZoom = newZoom;
            RefreshMapView();
        }
    }
    
    void UpdateZoomSlider()
    {
        if (sliderHandle != null)
        {
            float normalizedZoom = (mapZoom - 0.5f) / (3.0f - 0.5f);
            float trackWidth = 70f; // 滑块轨道宽度减去边距
            float handlePos = normalizedZoom * trackWidth - 6f; // 减去手柄宽度的一半
            sliderHandle.style.left = Mathf.Clamp(handlePos, 0f, trackWidth - 12f);
            Debug.Log($"更新滑块位置: {handlePos:F1}px, 缩放: {mapZoom:F1}x");
        }
    }
    
    /// <summary>
    /// 创建统计面板
    /// </summary>
    void CreateStatsPanel(VisualElement content)
    {
        var statsPanel = new VisualElement();
        statsPanel.style.width = 300;
        statsPanel.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f);
        statsPanel.style.paddingTop = 20;
        statsPanel.style.paddingBottom = 20;
        statsPanel.style.paddingLeft = 15;
        statsPanel.style.paddingRight = 15;
        statsPanel.style.borderTopLeftRadius = 8;
        statsPanel.style.borderTopRightRadius = 8;
        statsPanel.style.borderBottomLeftRadius = 8;
        statsPanel.style.borderBottomRightRadius = 8;
        statsPanel.style.borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        statsPanel.style.borderBottomWidth = 1;
        statsPanel.style.borderTopColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        statsPanel.style.borderTopWidth = 1;
        statsPanel.style.borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        statsPanel.style.borderLeftWidth = 1;
        statsPanel.style.borderRightColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        statsPanel.style.borderRightWidth = 1;
        
        // 标题区域
        var titleContainer = new VisualElement();
        titleContainer.style.backgroundColor = primaryColor;
        titleContainer.style.marginBottom = 15;
        titleContainer.style.marginLeft = -15;
        titleContainer.style.marginRight = -15;
        titleContainer.style.marginTop = -20;
        titleContainer.style.paddingTop = 15;
        titleContainer.style.paddingBottom = 15;
        titleContainer.style.borderTopLeftRadius = 8;
        titleContainer.style.borderTopRightRadius = 8;
        titleContainer.style.alignItems = Align.Center;
        
        var statsTitle = new Label("系统统计");
        statsTitle.style.color = Color.white;
        statsTitle.style.fontSize = 18;
        statsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        ApplyFont(statsTitle);
        titleContainer.Add(statsTitle);
        
        // 添加图标
        var iconLabel = new Label("📊");
        iconLabel.style.fontSize = 20;
        iconLabel.style.marginBottom = 5;
        titleContainer.Add(iconLabel);
        
        statsPanel.Add(titleContainer);
        
        // 获取实际数据
        var statsData = GetSystemStats();
        
        // 创建概览卡片容器
        var overviewContainer = new VisualElement();
        overviewContainer.style.flexDirection = FlexDirection.Row;
        overviewContainer.style.flexWrap = Wrap.Wrap;
        overviewContainer.style.justifyContent = Justify.SpaceBetween;
        overviewContainer.style.marginBottom = 15;
        
        // 创建小型统计卡片
        CreateMiniStatCard(overviewContainer, "电塔", $"{statsData.towerCount}", towerColor, "🗼");
        CreateMiniStatCard(overviewContainer, "线路", $"{statsData.wireCount}", new Color(0.9f, 0.6f, 0.2f, 1f), "⚡");
        CreateMiniStatCard(overviewContainer, "危险物", $"{statsData.dangerCount}", dangerColor, "⚠️");
        
        statsPanel.Add(overviewContainer);
        
        // 危险物等级详细统计（带图表）
        if (statsData.dangerCount > 0)
        {
            CreateDangerLevelStatsWithChart(statsPanel, statsData.lowDangerCount, statsData.mediumDangerCount, statsData.highDangerCount);
        }
        
        // 系统性能指标
        var performanceSection = new VisualElement();
        performanceSection.style.backgroundColor = Color.white;
        performanceSection.style.borderTopLeftRadius = 6;
        performanceSection.style.borderTopRightRadius = 6;
        performanceSection.style.borderBottomLeftRadius = 6;
        performanceSection.style.borderBottomRightRadius = 6;
        performanceSection.style.paddingTop = 12;
        performanceSection.style.paddingBottom = 12;
        performanceSection.style.paddingLeft = 12;
        performanceSection.style.paddingRight = 12;
        performanceSection.style.marginBottom = 10;
        
        var performanceTitle = new Label("系统指标");
        performanceTitle.style.fontSize = 14;
        performanceTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        performanceTitle.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        performanceTitle.style.marginBottom = 8;
        ApplyFont(performanceTitle);
        performanceSection.Add(performanceTitle);
        
        CreateStatRow(performanceSection, "总长度", $"{statsData.totalLength:F1} 米", new Color(0.3f, 0.8f, 0.3f, 1f));
        CreateStatRow(performanceSection, "平均塔高", $"{statsData.avgHeight:F1} 米", new Color(0.8f, 0.3f, 0.8f, 1f));
        CreateStatRow(performanceSection, "覆盖范围", "12.5 km²", new Color(0.2f, 0.6f, 0.9f, 1f));
        
        statsPanel.Add(performanceSection);
        
        // 系统状态指示器
        var statusSection = new VisualElement();
        statusSection.style.backgroundColor = Color.white;
        statusSection.style.borderTopLeftRadius = 6;
        statusSection.style.borderTopRightRadius = 6;
        statusSection.style.borderBottomLeftRadius = 6;
        statusSection.style.borderBottomRightRadius = 6;
        statusSection.style.paddingTop = 12;
        statusSection.style.paddingBottom = 12;
        statusSection.style.paddingLeft = 12;
        statusSection.style.paddingRight = 12;
        
        var statusTitle = new Label("系统状态");
        statusTitle.style.fontSize = 14;
        statusTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        statusTitle.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        statusTitle.style.marginBottom = 8;
        ApplyFont(statusTitle);
        statusSection.Add(statusTitle);
        
        // 状态指示器
        var statusIndicator = new VisualElement();
        statusIndicator.style.flexDirection = FlexDirection.Row;
        statusIndicator.style.alignItems = Align.Center;
        statusIndicator.style.backgroundColor = new Color(0.9f, 0.95f, 0.9f, 1f);
        statusIndicator.style.borderTopLeftRadius = 4;
        statusIndicator.style.borderTopRightRadius = 4;
        statusIndicator.style.borderBottomLeftRadius = 4;
        statusIndicator.style.borderBottomRightRadius = 4;
        statusIndicator.style.paddingTop = 8;
        statusIndicator.style.paddingBottom = 8;
        statusIndicator.style.paddingLeft = 10;
        statusIndicator.style.paddingRight = 10;
        
        var statusDot = new VisualElement();
        statusDot.style.width = 8;
        statusDot.style.height = 8;
        statusDot.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        statusDot.style.borderTopLeftRadius = 4;
        statusDot.style.borderTopRightRadius = 4;
        statusDot.style.borderBottomLeftRadius = 4;
        statusDot.style.borderBottomRightRadius = 4;
        statusDot.style.marginRight = 8;
        
        var statusText = new Label("系统正常运行");
        statusText.style.fontSize = 12;
        statusText.style.color = new Color(0.2f, 0.6f, 0.2f, 1f);
        statusText.style.unityFontStyleAndWeight = FontStyle.Bold;
        ApplyFont(statusText);
        
        statusIndicator.Add(statusDot);
        statusIndicator.Add(statusText);
        statusSection.Add(statusIndicator);
        
        statsPanel.Add(statusSection);
        
        content.Add(statsPanel);
    }
    
    /// <summary>
    /// 创建小型统计卡片
    /// </summary>
    void CreateMiniStatCard(VisualElement parent, string label, string value, Color color, string icon)
    {
        var card = new VisualElement();
        card.style.width = Length.Percent(30);
        card.style.backgroundColor = Color.white;
        card.style.borderTopLeftRadius = 6;
        card.style.borderTopRightRadius = 6;
        card.style.borderBottomLeftRadius = 6;
        card.style.borderBottomRightRadius = 6;
        card.style.marginBottom = 8;
        card.style.paddingTop = 12;
        card.style.paddingBottom = 12;
        card.style.paddingLeft = 8;
        card.style.paddingRight = 8;
        card.style.alignItems = Align.Center;
        card.style.borderBottomColor = color;
        card.style.borderBottomWidth = 3;
        
        var iconLabel = new Label(icon);
        iconLabel.style.fontSize = 16;
        iconLabel.style.marginBottom = 4;
        card.Add(iconLabel);
        
        var valueElement = new Label(value);
        valueElement.style.fontSize = 16;
        valueElement.style.unityFontStyleAndWeight = FontStyle.Bold;
        valueElement.style.color = color;
        valueElement.style.marginBottom = 2;
        ApplyFont(valueElement);
        card.Add(valueElement);
        
        var labelElement = new Label(label);
        labelElement.style.fontSize = 10;
        labelElement.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        ApplyFont(labelElement);
        card.Add(labelElement);
        
        parent.Add(card);
    }
    
    /// <summary>
    /// 创建统计行
    /// </summary>
    void CreateStatRow(VisualElement parent, string label, string value, Color color)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 6;
        
        var labelElement = new Label(label);
        labelElement.style.fontSize = 12;
        labelElement.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        ApplyFont(labelElement);
        
        var valueElement = new Label(value);
        valueElement.style.fontSize = 12;
        valueElement.style.unityFontStyleAndWeight = FontStyle.Bold;
        valueElement.style.color = color;
        ApplyFont(valueElement);
        
        row.Add(labelElement);
        row.Add(valueElement);
        parent.Add(row);
    }
    
    /// <summary>
    /// 获取系统统计数据
    /// </summary>
    (int towerCount, int wireCount, int dangerCount, int lowDangerCount, int mediumDangerCount, int highDangerCount, float totalLength, float avgHeight) GetSystemStats()
    {
        int towerCount = 8;
        int wireCount = 5;
        int dangerCount = 0;
        int lowDangerCount = 0;
        int mediumDangerCount = 0;
        int highDangerCount = 0;
        float totalLength = 1200.5f;
        float avgHeight = 25.3f;
        
        // 获取实际数据
        var towers = GetTowerData();
        var powerlines = GetPowerlineData();
        var dangers = GetDangerData();
        
        towerCount = towers.Count;
        wireCount = powerlines.Count;
        dangerCount = dangers.Count;
        
        // 统计不同危险等级的数量
        foreach (var danger in dangers)
        {
            switch (danger.dangerLevel)
            {
                case DangerLevel.Low:
                    lowDangerCount++;
                    break;
                case DangerLevel.Medium:
                    mediumDangerCount++;
                    break;
                case DangerLevel.High:
                    highDangerCount++;
                    break;
            }
        }
        
        if (powerlines.Count > 0)
        {
            totalLength = powerlines.Sum(p => p.length);
        }
        
        if (towers.Count > 0)
        {
            avgHeight = towers.Average(t => t.height);
        }
        
        return (towerCount, wireCount, dangerCount, lowDangerCount, mediumDangerCount, highDangerCount, totalLength, avgHeight);
    }
    
    /// <summary>
    /// 绘制电力线路图
    /// </summary>
    void DrawPowerlineMap(VisualElement mapContainer)
    {
        try
        {
            // 清除现有内容（保留工具栏和背景）
            var itemsToRemove = new List<VisualElement>();
            foreach (VisualElement child in mapContainer.Children())
            {
                if (child.name != "map-toolbar")
                {
                    itemsToRemove.Add(child);
                }
            }
            foreach (var item in itemsToRemove)
            {
                item.RemoveFromHierarchy();
            }

            // 绘制网格背景
            DrawGridBackground(mapContainer);

            // 获取数据
            var towers = GetTowerData();
            var powerlines = GetPowerlineData();
            var dangers = GetDangerData();

            if (towers.Count == 0)
            {
                var noDataLabel = new Label("暂无电塔数据");
                noDataLabel.style.position = Position.Absolute;
                noDataLabel.style.left = Length.Percent(50);
                noDataLabel.style.top = Length.Percent(50);
                noDataLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                noDataLabel.style.fontSize = 16;
                noDataLabel.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
                ApplyFont(noDataLabel);
                mapContainer.Add(noDataLabel);
                return;
            }

            // 计算地图边界（包含电塔和危险物）
            var bounds = CalculateMapBounds(towers, dangers);
            float baseScale = CalculateMapScale(bounds, 1000f, 600f);
            float finalScale = baseScale * mapZoom;

            // 绘制电力线
            foreach (var powerline in powerlines)
            {
                DrawPowerline(mapContainer, powerline, bounds, finalScale, 1000f, 600f);
            }

            // 绘制电塔
            foreach (var tower in towers)
            {
                DrawTowerMarker(mapContainer, tower, bounds, finalScale, 1000f, 600f);
            }

            // 绘制危险物标记
            foreach (var danger in dangers)
            {
                DrawDangerMarker(mapContainer, danger, bounds, finalScale, 1000f, 600f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"绘制电力线路图时出错: {e.Message}");
        }
    }
    
    /// <summary>
    /// 绘制网格背景
    /// </summary>
    void DrawGridBackground(VisualElement container)
    {
        var gridColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        
        // 绘制垂直线
        for (int i = 0; i <= 10; i++)
        {
            float x = i * 10f; // 百分比位置
            var line = new VisualElement();
            line.style.position = Position.Absolute;
            line.style.left = Length.Percent(x);
            line.style.top = 0;
            line.style.width = 1;
            line.style.height = Length.Percent(100);
            line.style.backgroundColor = gridColor;
            container.Add(line);
        }
        
        // 绘制水平线
        for (int i = 0; i <= 6; i++)
        {
            float y = i * 16.67f; // 百分比位置
            var line = new VisualElement();
            line.style.position = Position.Absolute;
            line.style.left = 0;
            line.style.top = Length.Percent(y);
            line.style.width = Length.Percent(100);
            line.style.height = 1;
            line.style.backgroundColor = gridColor;
            container.Add(line);
        }
    }
    
    /// <summary>
    /// 获取电塔数据
    /// </summary>
    public List<TowerData> GetTowerData()
    {
        var towers = new List<TowerData>();
        
        if (sceneInitializer != null)
        {
            var towerData = sceneInitializer.LoadSimpleTowerData();
            for (int i = 0; i < towerData.Count; i++)
            {
                var tower = towerData[i];
                towers.Add(new TowerData
                {
                    position = tower.position,
                    name = $"电塔-{i + 1:D2}",
                    status = "normal",
                    height = tower.height
                });
            }
        }
        
        // 如果没有数据，创建模拟数据
        if (towers.Count == 0)
        {
            for (int i = 0; i < 8; i++)
            {
                towers.Add(new TowerData
                {
                    position = new Vector3(i * 150f, 0, i * 100f),
                    name = $"模拟电塔-{i + 1:D2}",
                    status = i % 3 == 0 ? "warning" : "normal",
                    height = 10f + (i % 3) * 2f
                });
            }
        }
        
        return towers;
    }
    
    /// <summary>
    /// 获取电力线数据
    /// </summary>
    List<SceneInitializer.PowerlineInfo> GetPowerlineData()
    {
        if (sceneInitializer != null && sceneInitializer.powerlines != null && sceneInitializer.powerlines.Count > 0)
        {
            return sceneInitializer.powerlines.ToList();
        }
        
        // 如果没有真实电力线数据，根据电塔位置生成连接线
        var towers = GetTowerData();
        var powerlines = new List<SceneInitializer.PowerlineInfo>();
        
        if (towers.Count >= 2)
        {
            for (int i = 0; i < towers.Count - 1; i++)
            {
                var powerline = new SceneInitializer.PowerlineInfo
                {
                    start = towers[i].position,
                    end = towers[i + 1].position,
                    wireType = "Conductor",
                    length = Vector3.Distance(towers[i].position, towers[i + 1].position),
                    points = new List<Vector3> { towers[i].position, towers[i + 1].position }
                };
                
                powerlines.Add(powerline);
            }
        }
        
        return powerlines;
    }

    /// <summary>
    /// 获取危险物数据
    /// </summary>
    List<DangerData> GetDangerData()
    {
        var dangers = new List<DangerData>();
        
        // 查找UIToolkitDangerController
        var dangerController = FindObjectOfType<UIToolkitDangerController>();
        if (dangerController != null)
        {
            // 获取危险物标记列表
            var dangerMarkers = dangerController.GetDangerMarkers();
            for (int i = 0; i < dangerMarkers.Count; i++)
            {
                var marker = dangerMarkers[i];
                if (marker != null)
                {
                    dangers.Add(new DangerData
                    {
                        position = marker.transform.position,
                        name = $"危险物-{i + 1:D2}",
                        dangerType = marker.dangerType,
                        dangerLevel = marker.dangerLevel,
                        description = marker.description
                    });
                }
            }
        }
        
        return dangers;
    }
    
    /// <summary>
    /// 计算地图边界（包含电塔和危险物）
    /// </summary>
    Bounds CalculateMapBounds(List<TowerData> towers, List<DangerData> dangers)
    {
        var allPositions = new List<Vector3>();
        
        // 添加电塔位置
        foreach (var tower in towers)
        {
            allPositions.Add(tower.position);
        }
        
        // 添加危险物位置
        foreach (var danger in dangers)
        {
            allPositions.Add(danger.position);
        }
        
        if (allPositions.Count == 0)
            return new Bounds(Vector3.zero, Vector3.one * 100f);
        
        Vector3 min = allPositions[0];
        Vector3 max = allPositions[0];
        
        foreach (var pos in allPositions)
        {
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }
        
        // 添加边距
        Vector3 size = max - min;
        size.x = Mathf.Max(size.x, 50f) * 1.2f;
        size.z = Mathf.Max(size.z, 50f) * 1.2f;
        
        Vector3 center = (min + max) * 0.5f;
        return new Bounds(center, size);
    }
    
    /// <summary>
    /// 计算地图缩放比例
    /// </summary>
    float CalculateMapScale(Bounds bounds, float maxWidth, float maxHeight)
    {
        float scaleX = maxWidth / bounds.size.x;
        float scaleZ = maxHeight / bounds.size.z;
        return Mathf.Min(scaleX, scaleZ);
    }
    
    /// <summary>
    /// 世界坐标转地图坐标
    /// </summary>
    Vector2 WorldToMapPosition(Vector3 worldPos, Bounds bounds, float scale, float mapWidth, float mapHeight)
    {
        // 计算相对于边界的位置
        Vector3 localPos = worldPos - bounds.min;
        
        // 归一化到0-1范围，然后转换为百分比
        float normalizedX = localPos.x / bounds.size.x;
        float normalizedZ = localPos.z / bounds.size.z;
        
        // 转换为百分比坐标（0-100）
        float x = normalizedX * 100f;
        float y = normalizedZ * 100f;
        
        // 应用缩放和偏移变换
        x = (x - 50f) * mapZoom + 50f + mapOffset.x;
        y = (y - 50f) * mapZoom + 50f + mapOffset.y;
        
        return new Vector2(x, y);
    }
    
    /// <summary>
    /// 绘制电力线
    /// </summary>
    void DrawPowerline(VisualElement container, SceneInitializer.PowerlineInfo powerline, Bounds bounds, float scale, float mapWidth, float mapHeight)
    {
        if (powerline.points == null || powerline.points.Count < 2) return;
        
        Color lineColor = powerline.wireType == "ground" ? groundWireColor : wireColor;
        
        for (int i = 0; i < powerline.points.Count - 1; i++)
        {
            Vector2 start = WorldToMapPosition(powerline.points[i], bounds, scale, mapWidth, mapHeight);
            Vector2 end = WorldToMapPosition(powerline.points[i + 1], bounds, scale, mapWidth, mapHeight);
            
            DrawLine(container, start, end, lineColor, 2f);
        }
    }
    
    /// <summary>
    /// 绘制线条（优化版本）
    /// </summary>
    void DrawLine(VisualElement container, Vector2 start, Vector2 end, Color color, float width)
    {
        // 优化：减少分段数以提高性能，但保持视觉效果
        float distance = Vector2.Distance(start, end);
        int segments = Mathf.Max(1, Mathf.RoundToInt(distance * 1.5f)); // 减少分段数
        segments = Mathf.Min(segments, 20); // 限制最大分段数
        
        for (int i = 0; i <= segments; i++)
        {
            float t = segments > 0 ? (float)i / segments : 0f;
            Vector2 pos = Vector2.Lerp(start, end, t);
            
            var dot = new VisualElement();
            dot.style.position = Position.Absolute;
            dot.style.left = Length.Percent(pos.x - 0.1f);
            dot.style.top = Length.Percent(pos.y - 0.1f);
            dot.style.width = Length.Percent(0.2f);
            dot.style.height = Length.Percent(0.2f);
            dot.style.backgroundColor = color;
            dot.style.borderTopLeftRadius = Length.Percent(50);
            dot.style.borderTopRightRadius = Length.Percent(50);
            dot.style.borderBottomLeftRadius = Length.Percent(50);
            dot.style.borderBottomRightRadius = Length.Percent(50);
            dot.style.opacity = 1f;
            
            container.Add(dot);
        }
    }
    
    /// <summary>
    /// 绘制电塔标记
    /// </summary>
    void DrawTowerMarker(VisualElement container, TowerData tower, Bounds bounds, float scale, float mapWidth, float mapHeight)
    {
        Vector2 mapPos = WorldToMapPosition(tower.position, bounds, scale, mapWidth, mapHeight);
        
        var towerElement = new VisualElement();
        towerElement.style.position = Position.Absolute;
        towerElement.style.left = Length.Percent(mapPos.x - 1); // 2%宽度，居中
        towerElement.style.top = Length.Percent(mapPos.y - 1);  // 2%高度，居中
        towerElement.style.width = Length.Percent(2);
        towerElement.style.height = Length.Percent(2);
        towerElement.style.backgroundColor = GetTowerStatusColor(tower.status);
        towerElement.style.borderTopLeftRadius = Length.Percent(50);
        towerElement.style.borderTopRightRadius = Length.Percent(50);
        towerElement.style.borderBottomLeftRadius = Length.Percent(50);
        towerElement.style.borderBottomRightRadius = Length.Percent(50);
        towerElement.style.borderBottomColor = Color.black;
        towerElement.style.borderBottomWidth = 1;
        towerElement.style.borderTopColor = Color.black;
        towerElement.style.borderTopWidth = 1;
        towerElement.style.borderLeftColor = Color.black;
        towerElement.style.borderLeftWidth = 1;
        towerElement.style.borderRightColor = Color.black;
        towerElement.style.borderRightWidth = 1;
        
        // 添加电塔名称标签
        var label = new Label(tower.name);
        label.style.position = Position.Absolute;
        label.style.left = Length.Percent(110); // 在圆点右侧
        label.style.top = Length.Percent(-25);  // 垂直居中
        label.style.fontSize = 10;
        label.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.backgroundColor = new Color(1f, 1f, 1f, 0.8f);
        label.style.paddingLeft = 2;
        label.style.paddingRight = 2;
        label.style.paddingTop = 1;
        label.style.paddingBottom = 1;
        label.style.borderTopLeftRadius = 2;
        label.style.borderTopRightRadius = 2;
        label.style.borderBottomLeftRadius = 2;
        label.style.borderBottomRightRadius = 2;
        ApplyFont(label);
        towerElement.Add(label);
        
        // 设置电塔交互
        SetupTowerInteraction(towerElement, tower, container);
        
        container.Add(towerElement);
    }
    
    /// <summary>
    /// 绘制危险物标记
    /// </summary>
    void DrawDangerMarker(VisualElement container, DangerData danger, Bounds bounds, float scale, float mapWidth, float mapHeight)
    {
        Vector2 mapPos = WorldToMapPosition(danger.position, bounds, scale, mapWidth, mapHeight);
        
        var dangerElement = new VisualElement();
        dangerElement.style.position = Position.Absolute;
        dangerElement.style.left = Length.Percent(mapPos.x - 0.6f); // 1.2%宽度，居中
        dangerElement.style.top = Length.Percent(mapPos.y - 0.6f);  // 1.2%高度，居中
        dangerElement.style.width = Length.Percent(1.2f);
        dangerElement.style.height = Length.Percent(1.2f);
        
        // 根据危险等级设置颜色
        Color markerColor = GetDangerLevelColor(danger.dangerLevel);
        dangerElement.style.backgroundColor = markerColor;
        
        // 三角形样式
        dangerElement.style.borderTopLeftRadius = 0;
        dangerElement.style.borderTopRightRadius = 0;
        dangerElement.style.borderBottomLeftRadius = Length.Percent(50);
        dangerElement.style.borderBottomRightRadius = Length.Percent(50);
        
        // 边框
        dangerElement.style.borderBottomColor = Color.black;
        dangerElement.style.borderBottomWidth = 1;
        dangerElement.style.borderTopColor = Color.black;
        dangerElement.style.borderTopWidth = 1;
        dangerElement.style.borderLeftColor = Color.black;
        dangerElement.style.borderLeftWidth = 1;
        dangerElement.style.borderRightColor = Color.black;
        dangerElement.style.borderRightWidth = 1;
        
        // 移除危险物名称标签，只在悬停时显示详细信息
        
        // 设置危险物交互
        SetupDangerInteraction(dangerElement, danger, container);
        
        container.Add(dangerElement);
    }
    
    /// <summary>
    /// 设置电塔交互功能
    /// </summary>
    void SetupTowerInteraction(VisualElement towerElement, TowerData tower, VisualElement mapContainer)
    {
        VisualElement tooltip = null;
        
        // 鼠标悬停
        towerElement.RegisterCallback<MouseEnterEvent>(evt =>
        {
            // 高亮电塔
            towerElement.style.scale = new Scale(Vector3.one * 1.5f);
            towerElement.style.borderBottomWidth = 3;
            towerElement.style.borderTopWidth = 3;
            towerElement.style.borderLeftWidth = 3;
            towerElement.style.borderRightWidth = 3;
            
            // 显示工具提示
            tooltip = CreateTowerTooltip(tower);
            
            // 获取鼠标相对于地图容器的位置
            var mousePos = evt.mousePosition;
            var containerRect = mapContainer.worldBound;
            var relativePos = mousePos - containerRect.position;
            
            // 计算工具提示位置（避免超出边界）
            float tooltipX = relativePos.x + 10;
            float tooltipY = relativePos.y - 10;
            
            // 边界检查
            if (tooltipX + 200 > containerRect.width) tooltipX = relativePos.x - 210;
            if (tooltipY < 0) tooltipY = relativePos.y + 20;
            
            tooltip.style.left = tooltipX;
            tooltip.style.top = tooltipY;
            
            mapContainer.Add(tooltip);
        });
        
        // 鼠标离开
        towerElement.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            // 恢复电塔样式
            towerElement.style.scale = new Scale(Vector3.one);
            towerElement.style.borderBottomWidth = 1;
            towerElement.style.borderTopWidth = 1;
            towerElement.style.borderLeftWidth = 1;
            towerElement.style.borderRightWidth = 1;
            
            // 隐藏工具提示
            if (tooltip != null && tooltip.parent != null)
            {
                tooltip.RemoveFromHierarchy();
                tooltip = null;
            }
        });
        
        // 处理点击跳转（需要与拖拽区分）
        bool clickStarted = false;
        Vector2 clickStartPos = Vector2.zero;
        float clickStartTimestamp = 0f;
        
        towerElement.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button == 0)
            {
                clickStarted = true;
                clickStartPos = evt.mousePosition;
                clickStartTimestamp = Time.time;
                evt.StopPropagation();
            }
        });
        
        towerElement.RegisterCallback<MouseUpEvent>(evt =>
        {
            if (evt.button == 0 && clickStarted)
            {
                float timeDiff = Time.time - clickStartTimestamp;
                float distance = Vector2.Distance(evt.mousePosition, clickStartPos);
                
                // 如果是短时间内的小距离移动，认为是点击
                if (timeDiff < CLICK_THRESHOLD_TIME && distance < CLICK_THRESHOLD_DISTANCE)
                {
                    JumpToTower(tower.position);
                    HideSceneOverview(); // 跳转后关闭弹窗
                }
                
                clickStarted = false;
                evt.StopPropagation();
            }
        });
    }

    /// <summary>
    /// 设置危险物交互功能
    /// </summary>
    void SetupDangerInteraction(VisualElement dangerElement, DangerData danger, VisualElement mapContainer)
    {
        VisualElement tooltip = null;
        
        // 鼠标悬停
        dangerElement.RegisterCallback<MouseEnterEvent>(evt =>
        {
            // 高亮危险物
            dangerElement.style.scale = new Scale(Vector3.one * 1.5f);
            dangerElement.style.borderBottomWidth = 3;
            dangerElement.style.borderTopWidth = 3;
            dangerElement.style.borderLeftWidth = 3;
            dangerElement.style.borderRightWidth = 3;
            
            // 显示工具提示
            tooltip = CreateDangerTooltip(danger);
            
            // 获取鼠标相对于地图容器的位置
            var mousePos = evt.mousePosition;
            var containerRect = mapContainer.worldBound;
            var relativePos = mousePos - containerRect.position;
            
            // 计算工具提示位置（避免超出边界）
            float tooltipX = relativePos.x + 10;
            float tooltipY = relativePos.y - 10;
            
            // 边界检查
            if (tooltipX + 200 > containerRect.width) tooltipX = relativePos.x - 210;
            if (tooltipY < 0) tooltipY = relativePos.y + 20;
            
            tooltip.style.left = tooltipX;
            tooltip.style.top = tooltipY;
            
            mapContainer.Add(tooltip);
        });
        
        // 鼠标离开
        dangerElement.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            // 恢复危险物样式
            dangerElement.style.scale = new Scale(Vector3.one);
            dangerElement.style.borderBottomWidth = 1;
            dangerElement.style.borderTopWidth = 1;
            dangerElement.style.borderLeftWidth = 1;
            dangerElement.style.borderRightWidth = 1;
            
            // 隐藏工具提示
            if (tooltip != null && tooltip.parent != null)
            {
                tooltip.RemoveFromHierarchy();
                tooltip = null;
            }
        });
        
        // 处理点击跳转（需要与拖拽区分）
        bool clickStarted = false;
        Vector2 clickStartPos = Vector2.zero;
        float clickStartTimestamp = 0f;
        
        dangerElement.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button == 0)
            {
                clickStarted = true;
                clickStartPos = evt.mousePosition;
                clickStartTimestamp = Time.time;
                evt.StopPropagation();
            }
        });
        
        dangerElement.RegisterCallback<MouseUpEvent>(evt =>
        {
            if (evt.button == 0 && clickStarted)
            {
                float timeDiff = Time.time - clickStartTimestamp;
                float distance = Vector2.Distance(evt.mousePosition, clickStartPos);
                
                // 如果是短时间内的小距离移动，认为是点击
                if (timeDiff < CLICK_THRESHOLD_TIME && distance < CLICK_THRESHOLD_DISTANCE)
                {
                    JumpToDanger(danger.position);
                    HideSceneOverview(); // 跳转后关闭弹窗
                }
                
                clickStarted = false;
                evt.StopPropagation();
            }
        });
    }
    
    /// <summary>
    /// 创建电塔工具提示
    /// </summary>
    VisualElement CreateTowerTooltip(TowerData tower)
    {
        var tooltip = new VisualElement();
        tooltip.style.position = Position.Absolute;
        tooltip.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        tooltip.style.borderTopLeftRadius = 5;
        tooltip.style.borderTopRightRadius = 5;
        tooltip.style.borderBottomLeftRadius = 5;
        tooltip.style.borderBottomRightRadius = 5;
        tooltip.style.paddingTop = 8;
        tooltip.style.paddingBottom = 8;
        tooltip.style.paddingLeft = 10;
        tooltip.style.paddingRight = 10;
        tooltip.style.minWidth = 180;
        
        var nameLabel = new Label(tower.name);
        nameLabel.style.color = Color.white;
        nameLabel.style.fontSize = 14;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.marginBottom = 4;
        ApplyFont(nameLabel);
        tooltip.Add(nameLabel);
        
        var posLabel = new Label($"位置: ({tower.position.x:F1}, {tower.position.z:F1})");
        posLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        posLabel.style.fontSize = 12;
        posLabel.style.marginBottom = 2;
        ApplyFont(posLabel);
        tooltip.Add(posLabel);
        
        var heightLabel = new Label($"高度: {tower.height:F1}m");
        heightLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        heightLabel.style.fontSize = 12;
        heightLabel.style.marginBottom = 2;
        ApplyFont(heightLabel);
        tooltip.Add(heightLabel);
        
        var statusLabel = new Label($"状态: {GetStatusText(tower.status)}");
        statusLabel.style.color = GetTowerStatusColor(tower.status);
        statusLabel.style.fontSize = 12;
        statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        ApplyFont(statusLabel);
        tooltip.Add(statusLabel);
        
        var hintLabel = new Label("点击跳转到电塔位置");
        hintLabel.style.color = new Color(0.6f, 0.8f, 1f, 1f);
        hintLabel.style.fontSize = 10;
        hintLabel.style.marginTop = 4;
        hintLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
        ApplyFont(hintLabel);
        tooltip.Add(hintLabel);
        
        return tooltip;
    }

    /// <summary>
    /// 创建危险物工具提示
    /// </summary>
    VisualElement CreateDangerTooltip(DangerData danger)
    {
        var tooltip = new VisualElement();
        tooltip.style.position = Position.Absolute;
        tooltip.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        tooltip.style.borderTopLeftRadius = 5;
        tooltip.style.borderTopRightRadius = 5;
        tooltip.style.borderBottomLeftRadius = 5;
        tooltip.style.borderBottomRightRadius = 5;
        tooltip.style.paddingTop = 8;
        tooltip.style.paddingBottom = 8;
        tooltip.style.paddingLeft = 10;
        tooltip.style.paddingRight = 10;
        tooltip.style.minWidth = 180;
        
        var nameLabel = new Label(danger.name);
        nameLabel.style.color = Color.white;
        nameLabel.style.fontSize = 14;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.marginBottom = 4;
        ApplyFont(nameLabel);
        tooltip.Add(nameLabel);
        
        var typeLabel = new Label($"类型: {GetDangerTypeString(danger.dangerType)}");
        typeLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        typeLabel.style.fontSize = 12;
        typeLabel.style.marginBottom = 2;
        ApplyFont(typeLabel);
        tooltip.Add(typeLabel);
        
        var levelLabel = new Label($"等级: {GetDangerLevelString(danger.dangerLevel)}");
        levelLabel.style.color = GetDangerLevelColor(danger.dangerLevel);
        levelLabel.style.fontSize = 12;
        levelLabel.style.marginBottom = 2;
        levelLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        ApplyFont(levelLabel);
        tooltip.Add(levelLabel);
        
        var posLabel = new Label($"位置: ({danger.position.x:F1}, {danger.position.z:F1})");
        posLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        posLabel.style.fontSize = 12;
        posLabel.style.marginBottom = 2;
        ApplyFont(posLabel);
        tooltip.Add(posLabel);
        
        var descLabel = new Label($"描述: {danger.description}");
        descLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        descLabel.style.fontSize = 12;
        descLabel.style.marginBottom = 2;
        ApplyFont(descLabel);
        tooltip.Add(descLabel);
        
        var hintLabel = new Label("点击跳转到危险物位置");
        hintLabel.style.color = new Color(1f, 0.6f, 0.6f, 1f);
        hintLabel.style.fontSize = 10;
        hintLabel.style.marginTop = 4;
        hintLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
        ApplyFont(hintLabel);
        tooltip.Add(hintLabel);
        
        return tooltip;
    }
    
    /// <summary>
    /// 根据状态获取颜色
    /// </summary>
    Color GetTowerStatusColor(string status)
    {
        switch (status.ToLower())
        {
            case "normal": return towerColor;
            case "warning": return new Color(1f, 0.7f, 0f, 1f);
            case "error": return new Color(1f, 0.2f, 0.2f, 1f);
            default: return towerColor;
        }
    }

    /// <summary>
    /// 获取危险等级颜色
    /// </summary>
    Color GetDangerLevelColor(DangerLevel level)
    {
        switch (level)
        {
            case DangerLevel.Low:
                return new Color(1f, 0.8f, 0f, 1f); // 金黄色
            case DangerLevel.Medium:
                return new Color(1f, 0.4f, 0f, 1f); // 橙红色
            case DangerLevel.High:
                return new Color(0.9f, 0.1f, 0.1f, 1f); // 深红色
            default:
                return new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }

    /// <summary>
    /// 获取状态文本
    /// </summary>
    string GetStatusText(string status)
    {
        switch (status.ToLower())
        {
            case "normal": return "正常";
            case "warning": return "警告";
            case "error": return "故障";
            default: return "未知";
        }
    }

    /// <summary>
    /// 获取危险类型字符串
    /// </summary>
    string GetDangerTypeString(DangerType type)
    {
        switch (type)
        {
            case DangerType.Building: return "建筑危险";
            case DangerType.Vegetation: return "植被危险";
            case DangerType.Equipment: return "设备危险";
            case DangerType.Other: return "其他危险";
            default: return "未知";
        }
    }

    /// <summary>
    /// 获取危险等级字符串
    /// </summary>
    string GetDangerLevelString(DangerLevel level)
    {
        switch (level)
        {
            case DangerLevel.Low: return "低危险";
            case DangerLevel.Medium: return "中等危险";
            case DangerLevel.High: return "高危险";
            default: return "未知";
        }
    }
    
    /// <summary>
    /// 跳转到电塔
    /// </summary>
    void JumpToTower(Vector3 towerPosition)
    {
        try
        {
            // 优先使用TowerOverviewManager的跳转功能
            var towerManager = FindObjectOfType<TowerOverviewManager>();
            if (towerManager != null)
            {
                // 从TowerOverviewManager中查找最接近的电塔
                var allTowers = towerManager.GetAllTowers();
                TowerOverviewManager.TowerOverviewInfo closestTower = null;
                float minDistance = float.MaxValue;
                
                foreach (var tower in allTowers)
                {
                    float distance = Vector3.Distance(tower.position, towerPosition);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestTower = tower;
                    }
                }
                
                if (closestTower != null)
                {
                    towerManager.JumpToTower(closestTower);
                    Debug.Log($"使用TowerOverviewManager跳转到电塔: {closestTower.name}");
                    return;
                }
            }
            
            // 备用方案：手动实现保持当前视角的跳转
            var cameraManager = FindObjectOfType<CameraManager>();
            if (cameraManager == null)
            {
                Debug.LogError("未找到CameraManager组件");
                return;
            }
            
            // 不切换视角，保持当前视角进行跳转
            int currentView = cameraManager.GetCurrentView();
            StartCoroutine(SmoothJumpToTowerPosition(towerPosition, currentView));
            
            Debug.Log($"跳转到电塔位置: {towerPosition}，保持视角: {currentView}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"跳转到电塔时出错: {e.Message}");
        }
    }
    
    /// <summary>
    /// 平滑跳转到电塔位置（备用方案）
    /// </summary>
    IEnumerator SmoothJumpToTowerPosition(Vector3 towerPosition, int currentView)
    {
        var cameraManager = FindObjectOfType<CameraManager>();
        Camera mainCamera = cameraManager != null ? cameraManager.mainCamera : Camera.main;
        
        if (mainCamera == null)
        {
            Debug.LogError("跳转失败：未找到可用的摄像机");
            yield break;
        }
        
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        
        // 根据当前视角计算目标位置和旋转
        Vector3 targetPos;
        Vector3 lookAtPos = towerPosition;
        Quaternion targetRot;
        
        switch (currentView)
        {
            case 1: // 上帝视角 - 在电塔上方俯视
                targetPos = towerPosition + new Vector3(5f, 80f, 5f);
                Vector3 downDirection = (lookAtPos - targetPos).normalized;
                downDirection.y = Mathf.Min(downDirection.y, -0.7f); // 强制向下
                targetRot = Quaternion.LookRotation(downDirection);
                break;
                
            case 2: // 飞行视角 - 简单的飞行视角位置
                targetPos = towerPosition + new Vector3(20f, 35f, 20f);
                lookAtPos.y += 10f;
                Vector3 flyDirection = (lookAtPos - targetPos).normalized;
                targetRot = Quaternion.LookRotation(flyDirection);
                break;
                
            default: // 第一人称视角 - 近距离观察
                targetPos = towerPosition + new Vector3(15f, 5f, 15f);
                lookAtPos.y += 10f;
                Vector3 defaultDirection = (lookAtPos - targetPos).normalized;
                targetRot = Quaternion.LookRotation(defaultDirection);
                break;
        }
        
        // 确保摄像机位置在地面之上
        targetPos.y = Mathf.Max(targetPos.y, 5f);
        
        float elapsedTime = 0f;
        float duration = 0.5f; // 跳转动画时间
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            // 使用平滑曲线
            t = Mathf.SmoothStep(0f, 1f, t);
            
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            mainCamera.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            
            yield return null;
        }
        
        // 确保最终位置准确
        mainCamera.transform.position = targetPos;
        mainCamera.transform.rotation = targetRot;
        
        Debug.Log($"相机已跳转到电塔位置: {towerPosition}，视角: {currentView}");
    }

    /// <summary>
    /// 跳转到危险物位置
    /// </summary>
    void JumpToDanger(Vector3 dangerPosition)
    {
        var dangerController = FindObjectOfType<UIToolkitDangerController>();
        if (dangerController != null)
        {
            // 使用危险物控制器的跳转功能
            var dangerMarkers = dangerController.GetDangerMarkers();
            foreach (var marker in dangerMarkers)
            {
                if (Vector3.Distance(marker.transform.position, dangerPosition) < 1f)
                {
                    dangerController.JumpToSpecificMarker(marker);
                    return;
                }
            }
        }
        
        // 如果没有找到对应的危险物控制器，使用通用跳转方法
        StartCoroutine(SmoothJumpToDangerPosition(dangerPosition));
    }

    /// <summary>
    /// 平滑跳转到危险物位置
    /// </summary>
    IEnumerator SmoothJumpToDangerPosition(Vector3 dangerPosition)
    {
        var cameraManager = FindObjectOfType<CameraManager>();
        if (cameraManager == null)
        {
            Debug.LogWarning("未找到CameraManager组件");
            yield break;
        }

        Camera targetCamera = cameraManager.mainCamera;
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError("跳转失败：未找到摄像机");
            yield break;
        }

        int currentView = cameraManager.GetCurrentView();
        Vector3 startPos = targetCamera.transform.position;
        Vector3 targetPos = dangerPosition + new Vector3(0, 5f, -10f); // 在危险物后方上方

        // 根据视角调整目标位置
        switch (currentView)
        {
            case 1: // 上帝视角
                targetPos = dangerPosition + new Vector3(0, 30f, 0);
                break;
            case 2: // 飞行视角
                targetPos = dangerPosition + new Vector3(-5f, 2f, -8f);
                break;
            default: // 第一人称视角
                targetPos = dangerPosition + new Vector3(0, 2f, -5f);
                break;
        }

        Quaternion startRot = targetCamera.transform.rotation;
        Vector3 lookDirection = (dangerPosition - targetPos).normalized;
        Quaternion targetRot = Quaternion.LookRotation(lookDirection);

        float duration = 1.5f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);

            targetCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            targetCamera.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);

            yield return null;
        }

        // 确保最终位置准确
        targetCamera.transform.position = targetPos;
        targetCamera.transform.rotation = targetRot;
    }
    
    /// <summary>
    /// 创建危险等级分布统计（带图表）
    /// </summary>
    void CreateDangerLevelStatsWithChart(VisualElement parent, int lowCount, int mediumCount, int highCount)
    {
        var dangerStatsCard = new VisualElement();
        dangerStatsCard.style.backgroundColor = Color.white;
        dangerStatsCard.style.borderTopLeftRadius = 6;
        dangerStatsCard.style.borderTopRightRadius = 6;
        dangerStatsCard.style.borderBottomLeftRadius = 6;
        dangerStatsCard.style.borderBottomRightRadius = 6;
        dangerStatsCard.style.marginBottom = 10;
        dangerStatsCard.style.paddingTop = 12;
        dangerStatsCard.style.paddingBottom = 12;
        dangerStatsCard.style.paddingLeft = 12;
        dangerStatsCard.style.paddingRight = 12;
        
        var title = new Label("危险等级分布");
        title.style.fontSize = 14;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        title.style.marginBottom = 10;
        ApplyFont(title);
        dangerStatsCard.Add(title);
        
        // 创建图表容器
        var chartContainer = new VisualElement();
        chartContainer.style.height = 60;
        chartContainer.style.flexDirection = FlexDirection.Row;
        chartContainer.style.alignItems = Align.FlexEnd;
        chartContainer.style.marginBottom = 10;
        chartContainer.style.paddingLeft = 10;
        chartContainer.style.paddingRight = 10;
        
        int totalCount = lowCount + mediumCount + highCount;
        if (totalCount > 0)
        {
            // 计算比例
            float lowRatio = (float)lowCount / totalCount;
            float mediumRatio = (float)mediumCount / totalCount;
            float highRatio = (float)highCount / totalCount;
            
            // 创建柱状图
            CreateDangerBar(chartContainer, "低", lowCount, lowRatio, GetDangerLevelColor(DangerLevel.Low));
            CreateDangerBar(chartContainer, "中", mediumCount, mediumRatio, GetDangerLevelColor(DangerLevel.Medium));
            CreateDangerBar(chartContainer, "高", highCount, highRatio, GetDangerLevelColor(DangerLevel.High));
        }
        
        dangerStatsCard.Add(chartContainer);
        
        // 详细数值列表
        CreateDangerStatRow(dangerStatsCard, "低危险", lowCount, GetDangerLevelColor(DangerLevel.Low));
        CreateDangerStatRow(dangerStatsCard, "中等危险", mediumCount, GetDangerLevelColor(DangerLevel.Medium));
        CreateDangerStatRow(dangerStatsCard, "高危险", highCount, GetDangerLevelColor(DangerLevel.High));
        
        parent.Add(dangerStatsCard);
    }
    
    /// <summary>
    /// 创建危险物柱状图条
    /// </summary>
    void CreateDangerBar(VisualElement parent, string label, int count, float ratio, Color color)
    {
        var barContainer = new VisualElement();
        barContainer.style.flexGrow = 1;
        barContainer.style.alignItems = Align.Center;
        barContainer.style.marginLeft = 5;
        barContainer.style.marginRight = 5;
        
        // 柱子
        var bar = new VisualElement();
        bar.style.width = 20;
        bar.style.height = ratio * 40 + 5; // 最小高度5，最大45
        bar.style.backgroundColor = color;
        bar.style.borderTopLeftRadius = 2;
        bar.style.borderTopRightRadius = 2;
        bar.style.marginBottom = 5;
        
        // 数值标签
        var countLabel = new Label(count.ToString());
        countLabel.style.fontSize = 10;
        countLabel.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        countLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        countLabel.style.marginBottom = 2;
        ApplyFont(countLabel);
        
        // 类别标签
        var typeLabel = new Label(label);
        typeLabel.style.fontSize = 9;
        typeLabel.style.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        ApplyFont(typeLabel);
        
        barContainer.Add(countLabel);
        barContainer.Add(bar);
        barContainer.Add(typeLabel);
        parent.Add(barContainer);
    }
    
    /// <summary>
    /// 创建危险物统计行
    /// </summary>
    void CreateDangerStatRow(VisualElement parent, string label, int count, Color color)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.alignItems = Align.Center;
        row.style.marginBottom = 4;
        
        var leftContainer = new VisualElement();
        leftContainer.style.flexDirection = FlexDirection.Row;
        leftContainer.style.alignItems = Align.Center;
        
        var colorDot = new VisualElement();
        colorDot.style.width = 8;
        colorDot.style.height = 8;
        colorDot.style.backgroundColor = color;
        colorDot.style.borderTopLeftRadius = 4;
        colorDot.style.borderTopRightRadius = 4;
        colorDot.style.borderBottomLeftRadius = 4;
        colorDot.style.borderBottomRightRadius = 4;
        colorDot.style.marginRight = 8;
        
        var labelElement = new Label(label);
        labelElement.style.fontSize = 11;
        labelElement.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        ApplyFont(labelElement);
        
        leftContainer.Add(colorDot);
        leftContainer.Add(labelElement);
        
        var valueElement = new Label($"{count} 个");
        valueElement.style.fontSize = 11;
        valueElement.style.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        valueElement.style.unityFontStyleAndWeight = FontStyle.Bold;
        ApplyFont(valueElement);
        
        row.Add(leftContainer);
        row.Add(valueElement);
        parent.Add(row);
    }
    
    /// <summary>
    /// 创建统计卡片
    /// </summary>
    void CreateStatCard(VisualElement parent, string label, string value, Color color)
    {
        var card = new VisualElement();
        card.style.backgroundColor = Color.white;
        card.style.borderTopLeftRadius = 8;
        card.style.borderTopRightRadius = 8;
        card.style.borderBottomLeftRadius = 8;
        card.style.borderBottomRightRadius = 8;
        card.style.marginBottom = 10;
        card.style.paddingTop = 15;
        card.style.paddingBottom = 15;
        card.style.paddingLeft = 15;
        card.style.paddingRight = 15;
        card.style.borderBottomColor = color;
        card.style.borderBottomWidth = 3;
        
        var labelElement = new Label(label);
        labelElement.style.fontSize = 12;
        labelElement.style.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        labelElement.style.marginBottom = 5;
        ApplyFont(labelElement);
        card.Add(labelElement);
        
        var valueElement = new Label(value);
        valueElement.style.fontSize = 20;
        valueElement.style.unityFontStyleAndWeight = FontStyle.Bold;
        valueElement.style.color = color;
        ApplyFont(valueElement);
        card.Add(valueElement);
        
        parent.Add(card);
    }
    
    /// <summary>
    /// 创建图例项
    /// </summary>
    void CreateLegendItem(VisualElement parent, string label, Color color)
    {
        var item = new VisualElement();
        item.style.flexDirection = FlexDirection.Row;
        item.style.alignItems = Align.Center;
        item.style.marginRight = 15;
        item.style.paddingLeft = 6;
        item.style.paddingRight = 6;
        item.style.paddingTop = 3;
        item.style.paddingBottom = 3;
        item.style.backgroundColor = new Color(1f, 1f, 1f, 0.8f);
        item.style.borderTopLeftRadius = 4;
        item.style.borderTopRightRadius = 4;
        item.style.borderBottomLeftRadius = 4;
        item.style.borderBottomRightRadius = 4;
        item.style.borderBottomColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        item.style.borderBottomWidth = 1;
        item.style.borderTopColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        item.style.borderTopWidth = 1;
        item.style.borderLeftColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        item.style.borderLeftWidth = 1;
        item.style.borderRightColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        item.style.borderRightWidth = 1;
        
        VisualElement icon = null;
        
        // 根据标签类型创建不同的图标
        switch (label)
        {
            case "电塔":
                icon = CreateTowerIcon(color);
                break;
            case "导线":
                icon = CreateWireIcon(color);
                break;
            case "地线":
                icon = CreateGroundWireIcon(color);
                break;
            case "危险物":
                icon = CreateDangerIcon(color);
                break;
            default:
                // 默认圆点
                icon = new VisualElement();
                icon.style.width = 12;
                icon.style.height = 12;
                icon.style.backgroundColor = color;
                icon.style.borderTopLeftRadius = 6;
                icon.style.borderTopRightRadius = 6;
                icon.style.borderBottomLeftRadius = 6;
                icon.style.borderBottomRightRadius = 6;
                break;
        }
        
        icon.style.marginRight = 6;
        
        var labelElement = new Label(label);
        labelElement.style.fontSize = 11;
        labelElement.style.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
        ApplyFont(labelElement);
        
        item.Add(icon);
        item.Add(labelElement);
        parent.Add(item);
    }
    
    /// <summary>
    /// 创建电塔图标
    /// </summary>
    VisualElement CreateTowerIcon(Color color)
    {
        var container = new VisualElement();
        container.style.width = 14;
        container.style.height = 14;
        container.style.flexDirection = FlexDirection.Column;
        container.style.alignItems = Align.Center;
        container.style.justifyContent = Justify.Center;
        
        // 电塔顶部
        var top = new VisualElement();
        top.style.width = 3;
        top.style.height = 6;
        top.style.backgroundColor = color;
        
        // 电塔底部
        var bottom = new VisualElement();
        bottom.style.width = 8;
        bottom.style.height = 6;
        bottom.style.backgroundColor = color;
        
        container.Add(top);
        container.Add(bottom);
        
        return container;
    }
    
    /// <summary>
    /// 创建导线图标
    /// </summary>
    VisualElement CreateWireIcon(Color color)
    {
        var container = new VisualElement();
        container.style.width = 16;
        container.style.height = 12;
        container.style.flexDirection = FlexDirection.Row;
        container.style.alignItems = Align.Center;
        
        // 创建波浪线效果
        for (int i = 0; i < 4; i++)
        {
            var segment = new VisualElement();
            segment.style.width = 3;
            segment.style.height = 2;
            segment.style.backgroundColor = color;
            segment.style.marginRight = 1;
            segment.style.borderTopLeftRadius = 1;
            segment.style.borderTopRightRadius = 1;
            segment.style.borderBottomLeftRadius = 1;
            segment.style.borderBottomRightRadius = 1;
            container.Add(segment);
        }
        
        return container;
    }
    
    /// <summary>
    /// 创建地线图标
    /// </summary>
    VisualElement CreateGroundWireIcon(Color color)
    {
        var container = new VisualElement();
        container.style.width = 16;
        container.style.height = 12;
        container.style.flexDirection = FlexDirection.Row;
        container.style.alignItems = Align.Center;
        
        // 创建虚线效果
        for (int i = 0; i < 3; i++)
        {
            var dash = new VisualElement();
            dash.style.width = 4;
            dash.style.height = 2;
            dash.style.backgroundColor = color;
            dash.style.marginRight = 2;
            container.Add(dash);
        }
        
        return container;
    }
    
    /// <summary>
    /// 创建危险物图标
    /// </summary>
    VisualElement CreateDangerIcon(Color color)
    {
        var container = new VisualElement();
        container.style.width = 14;
        container.style.height = 14;
        container.style.alignItems = Align.Center;
        container.style.justifyContent = Justify.Center;
        
        // 三角形警告标志
        var triangle = new VisualElement();
        triangle.style.width = 0;
        triangle.style.height = 0;
        triangle.style.borderBottomWidth = 10;
        triangle.style.borderLeftWidth = 6;
        triangle.style.borderRightWidth = 6;
        triangle.style.borderBottomColor = color;
        triangle.style.borderLeftColor = Color.clear;
        triangle.style.borderRightColor = Color.clear;
        
        container.Add(triangle);
        
        return container;
    }
    

    
    /// <summary>
    /// 重置地图视图
    /// </summary>
    void ResetMapView()
    {
        mapZoom = 1.0f;
        mapOffset = Vector2.zero;
        if (mapContainer != null)
        {
            RefreshMapView();
        }
    }
    
    /// <summary>
    /// 刷新地图视图
    /// </summary>
    void RefreshMapView()
    {
        if (mapContainer != null)
        {
            Debug.Log($"刷新地图视图，缩放：{mapZoom:F1}x");
            DrawPowerlineMap(mapContainer);
            
            // 更新缩放标签显示
            if (zoomLabel != null)
            {
                zoomLabel.text = $"{mapZoom:F1}x";
            }
            
            // 更新滑块位置
            UpdateZoomSlider();
            
            // 更新小地图视图指示器
            UpdateMiniMapIndicator();
        }
    }
    
    /// <summary>
    /// 更新小地图视图指示器
    /// </summary>
    void UpdateMiniMapIndicator()
    {
        if (miniMapIndicator != null)
        {
            UpdateViewIndicator(miniMapIndicator);
        }
    }
    
    /// <summary>
    /// 应用字体
    /// </summary>
    public void ApplyFont(VisualElement element)
    {
        if (element != null)
        {
            // 优先使用FontManager
            if (FontManager.Instance != null)
            {
                var currentFont = FontManager.Instance.GetCurrentFont();
                if (currentFont != null)
                {
                    element.style.unityFont = currentFont;
                }
            }
            else
            {
                // 备用方案：使用自定义字体
                if (uiFont != null)
                {
                    element.style.unityFont = uiFont;
                }
                else
                {
                    // 使用Unity内建字体确保文本可见
                    var defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (defaultFont != null)
                    {
                        element.style.unityFont = defaultFont;
                    }
                }
            }
            
            // 确保字体大小设置正确
            if (element is Label label)
            {
                if (label.style.fontSize.value.value <= 0)
                {
                    label.style.fontSize = 14;
                }
                // 确保文本颜色可见
                if (label.style.color.value.a < 0.1f)
                {
                    label.style.color = Color.black;
                }
            }
            else if (element is Button button)
            {
                if (button.style.fontSize.value.value <= 0)
                {
                    button.style.fontSize = 14;
                }
                // 确保按钮文本颜色可见
                if (button.style.color.value.a < 0.1f)
                {
                    button.style.color = Color.white;
                }
            }
        }
    }
    
    /// <summary>
    /// 切换测量工具状态
    /// </summary>
    void ToggleMeasureTool()
    {
        isMeasuring = !isMeasuring;
        
        // 更新按钮样式
        if (measureButton != null)
        {
            measureButton.style.backgroundColor = isMeasuring ? new Color(0.8f, 0.3f, 0.3f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);
        }
        
        if (!isMeasuring)
        {
            // 清除所有测量元素
            ClearMeasureElements();
            ShowSearchResult("测量工具已关闭");
        }
        else
        {
            ShowSearchResult("测量工具已启用，点击地图上两个点进行距离测量");
        }
        
        Debug.Log($"测量工具状态: {(isMeasuring ? "开启" : "关闭")}");
    }
    
    /// <summary>
    /// 处理测量点击（使用UI坐标）- 简化版本
    /// </summary>
    void HandleMeasureClickWithUIPos(Vector2 localUIPos)
    {
        if (!isMeasuring || mapContainer == null) return;
        
        Debug.Log($"测量点击位置: {localUIPos}");
        
        if (measureStartMarker == null)
        {
            // 第一次点击 - 设置起点
            measureStartUIPos = localUIPos;
            CreateSimpleMeasureMarker(localUIPos, "起点", Color.green);
            ShowSearchResult("已选择起点，请点击终点");
        }
        else if (measureEndMarker == null)
        {
            // 第二次点击 - 设置终点并完成测量
            Vector2 endUIPos = localUIPos;
            CreateSimpleMeasureMarker(endUIPos, "终点", Color.red);
            CreateMeasureLine(measureStartUIPos, endUIPos);
            
            // 计算并显示距离
            float uiDistance = Vector2.Distance(measureStartUIPos, endUIPos);
            float realDistance = CalculateRealDistance(uiDistance);
            CreateSimpleDistanceLabel((measureStartUIPos + endUIPos) / 2, realDistance);
            
            ShowSearchResult($"测量完成！距离: {realDistance:F1} 米");
        }
        else
        {
            // 第三次点击 - 清除上次结果，开始新测量
            ClearMeasureElements();
            measureStartUIPos = localUIPos;
            CreateSimpleMeasureMarker(localUIPos, "起点", Color.green);
            ShowSearchResult("开始新测量，请点击终点");
        }
    }
    
    /// <summary>
    /// 处理测量点击（保持原有接口兼容性）
    /// </summary>
    void HandleMeasureClick(Vector3 worldPosition)
    {
        if (!isMeasuring || mapContainer == null) return;
        
        // 获取地图的边界和缩放信息
        var towers = GetTowerData();
        var dangers = GetDangerData();
        var bounds = CalculateMapBounds(towers, dangers);
        var containerRect = mapContainer.worldBound;
        float scale = CalculateMapScale(bounds, containerRect.width - 20, containerRect.height - 20);
        
        // 将世界坐标转换为UI坐标
        Vector2 uiPos = WorldToMapPosition(worldPosition, bounds, scale, containerRect.width - 20, containerRect.height - 20);
        uiPos += new Vector2(10, 10); // 添加边距偏移
        
        // 调用UI坐标版本的方法
        HandleMeasureClickWithUIPos(uiPos);
    }
    
    /// <summary>
    /// 创建简化的测量标记点
    /// </summary>
    void CreateSimpleMeasureMarker(Vector2 position, string label, Color color)
    {
        var marker = new VisualElement();
        marker.style.position = Position.Absolute;
        marker.style.left = position.x - 6;
        marker.style.top = position.y - 6;
        marker.style.width = 12;
        marker.style.height = 12;
        marker.style.backgroundColor = color;
        marker.style.borderTopLeftRadius = 6;
        marker.style.borderTopRightRadius = 6;
        marker.style.borderBottomLeftRadius = 6;
        marker.style.borderBottomRightRadius = 6;
        marker.style.borderBottomColor = Color.white;
        marker.style.borderBottomWidth = 1;
        marker.style.borderTopColor = Color.white;
        marker.style.borderTopWidth = 1;
        marker.style.borderLeftColor = Color.white;
        marker.style.borderLeftWidth = 1;
        marker.style.borderRightColor = Color.white;
        marker.style.borderRightWidth = 1;
        
        // 简化的标签
        var labelElement = new Label(label);
        labelElement.style.position = Position.Absolute;
        labelElement.style.left = 15;
        labelElement.style.top = -8;
        labelElement.style.fontSize = 10;
        labelElement.style.color = color;
        labelElement.style.backgroundColor = Color.white;
        labelElement.style.paddingLeft = 2;
        labelElement.style.paddingRight = 2;
        labelElement.style.paddingTop = 1;
        labelElement.style.paddingBottom = 1;
        ApplyFont(labelElement);
        
        marker.Add(labelElement);
        mapContainer.Add(marker);
        
        // 根据标签确定是起点还是终点
        if (label == "起点")
        {
            measureStartMarker = marker;
        }
        else
        {
            measureEndMarker = marker;
        }
        
        Debug.Log($"创建简化标记 {label}: 中心位置{position}");
    }
    
        /// <summary>
    /// 创建测量线 - 简化版本，使用多个小方块连接成线
    /// </summary>
    void CreateMeasureLine(Vector2 start, Vector2 end)
    {
        // 创建容器
        measureLine = new VisualElement();
        measureLine.style.position = Position.Absolute;
        measureLine.style.left = 0;
        measureLine.style.top = 0;
        measureLine.style.width = Length.Percent(100);
        measureLine.style.height = Length.Percent(100);
        measureLine.pickingMode = PickingMode.Ignore;
        
        // 计算需要绘制的点数量
        Vector2 direction = end - start;
        float distance = direction.magnitude;
        int pointCount = Mathf.RoundToInt(distance / 3); // 每3像素一个点
        pointCount = Mathf.Max(2, pointCount); // 至少2个点
        
        // 绘制连线上的点
        for (int i = 0; i <= pointCount; i++)
        {
            float t = (float)i / pointCount;
            Vector2 position = Vector2.Lerp(start, end, t);
            
            var dot = new VisualElement();
            dot.style.position = Position.Absolute;
            dot.style.left = position.x - 1;
            dot.style.top = position.y - 1;
            dot.style.width = 2;
            dot.style.height = 2;
            dot.style.backgroundColor = new Color(1f, 0.5f, 0f, 1f);
            
            measureLine.Add(dot);
        }
        
        mapContainer.Add(measureLine);
        
        Debug.Log($"简化测量线: 起点{start}, 终点{end}, 距离{distance:F1}px, 绘制{pointCount + 1}个点");
    }
    
    /// <summary>
    /// 创建简化的距离标签
    /// </summary>
    void CreateSimpleDistanceLabel(Vector2 position, float distance)
    {
        measureDistanceLabel = new Label($"{distance:F1}m");
        measureDistanceLabel.style.position = Position.Absolute;
        measureDistanceLabel.style.left = position.x - 20;
        measureDistanceLabel.style.top = position.y - 20;
        measureDistanceLabel.style.fontSize = 12;
        measureDistanceLabel.style.color = Color.black;
        measureDistanceLabel.style.backgroundColor = new Color(1f, 0.8f, 0.2f, 0.9f);
        measureDistanceLabel.style.paddingLeft = 4;
        measureDistanceLabel.style.paddingRight = 4;
        measureDistanceLabel.style.paddingTop = 2;
        measureDistanceLabel.style.paddingBottom = 2;
        measureDistanceLabel.style.borderTopLeftRadius = 3;
        measureDistanceLabel.style.borderTopRightRadius = 3;
        measureDistanceLabel.style.borderBottomLeftRadius = 3;
        measureDistanceLabel.style.borderBottomRightRadius = 3;
        measureDistanceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        ApplyFont(measureDistanceLabel);
        
        mapContainer.Add(measureDistanceLabel);
        
        Debug.Log($"创建距离标签: {distance:F1}m 在位置 {position}");
    }
    
    /// <summary>
    /// 计算真实距离
    /// </summary>
    float CalculateRealDistance(float uiDistance)
    {
        var towers = GetTowerData();
        var dangers = GetDangerData();
        var bounds = CalculateMapBounds(towers, dangers);
        var containerRect = mapContainer.worldBound;
        
        // 简化的距离计算
        float worldUnitsPerPixel = Mathf.Max(bounds.size.x, bounds.size.z) / Mathf.Max(containerRect.width - 20, containerRect.height - 20);
        float realDistance = uiDistance * worldUnitsPerPixel / mapZoom;
        
        Debug.Log($"距离计算: UI距离{uiDistance:F1}px, 真实距离{realDistance:F1}m, 缩放{mapZoom:F2}");
        return realDistance;
    }
    
    /// <summary>
    /// 清除所有测量元素
    /// </summary>
    void ClearMeasureElements()
    {
        if (measureStartMarker != null)
        {
            measureStartMarker.RemoveFromHierarchy();
            measureStartMarker = null;
            Debug.Log("已清除起点标记");
        }
        if (measureEndMarker != null)
        {
            measureEndMarker.RemoveFromHierarchy();
            measureEndMarker = null;
            Debug.Log("已清除终点标记");
        }
        if (measureLine != null)
        {
            measureLine.RemoveFromHierarchy();
            measureLine = null;
            Debug.Log("已清除测量连线");
        }
        if (measureDistanceLabel != null)
        {
            measureDistanceLabel.RemoveFromHierarchy();
            measureDistanceLabel = null;
            Debug.Log("已清除距离标签");
        }
    }
    }
}