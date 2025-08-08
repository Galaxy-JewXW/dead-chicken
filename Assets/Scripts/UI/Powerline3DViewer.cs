using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 电力线3D弹窗查看器
/// 用于展示电力线档距段的3D详细视图
/// </summary>
public class Powerline3DViewer : MonoBehaviour
{
    [Header("弹窗设置")]
    public GameObject viewerPanel;
    public Camera viewerCamera;
    public RawImage viewerImage;
    public Button closeButton;
    public Button resetViewButton;
    
    [Header("3D场景设置")]
    public Transform sceneContainer;
    public Light sceneLight;
    public float rotationSpeed = 100f;
    public float zoomSpeed = 10f;
    
    [Header("UI组件")]
    public Text titleText;
    public Text infoText;
    public Slider zoomSlider;
    
    // 私有变量
    private int viewerLayer = 31;
    private RenderTexture renderTexture;
    private GameObject currentTower1;
    private GameObject currentTower2;
    private Vector3 sceneCenter;
    private bool isDragging = false;
    private bool isPanning = false;
    private Vector3 lastMousePosition;
    private float currentZoom = 1f;
    
    // 网格和坐标轴设置
    private bool showGrid = true;
    private bool showAxes = true;
    private float gridSize = 10f;
    private GameObject gridObject;
    private GameObject axesObject;
    
    // 单例模式
    private static Powerline3DViewer instance;
    public static Powerline3DViewer Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<Powerline3DViewer>();
                if (instance == null)
                {
                    GameObject go = new GameObject("Powerline3DViewer");
                    instance = go.AddComponent<Powerline3DViewer>();
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
            InitializeViewer();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeViewer()
    {
        CreateViewerPanel();
        Create3DScene();
        CreateRenderTexture();
        SetupCamera();
        HideViewer();
    }
    
    void CreateViewerPanel()
    {
        if (viewerPanel != null) return;
        
        // 创建主面板
        viewerPanel = new GameObject("Powerline3DViewerPanel");
        viewerPanel.transform.SetParent(transform);
        
        // 设置Canvas
        Canvas canvas = viewerPanel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        
        CanvasScaler scaler = viewerPanel.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        viewerPanel.AddComponent<GraphicRaycaster>();
        
        // 创建背景
        GameObject background = CreateUIElement("Background", viewerPanel.transform);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);
        SetRectTransform(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        
        // 创建主容器
        GameObject container = CreateUIElement("Container", viewerPanel.transform);
        Image containerImage = container.AddComponent<Image>();
        containerImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        SetRectTransform(container, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero);
        
        // 创建子组件
        CreateTitle(container.transform);
        Create3DViewArea(container.transform);
        CreateControlButtons(container.transform);
        CreateInfoPanel(container.transform);
    }
    
    GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.AddComponent<RectTransform>();
        return obj;
    }
    
    void SetRectTransform(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
    
    void CreateTitle(Transform parent)
    {
        GameObject titleObj = CreateUIElement("Title", parent);
        titleText = titleObj.AddComponent<Text>();
        titleText.text = "电力线档距段3D视图";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 26; // 稍微增大标题字体
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        SetRectTransform(titleObj, new Vector2(0, 0.9f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
    }
    
    void Create3DViewArea(Transform parent)
    {
        GameObject viewArea = CreateUIElement("3DViewArea", parent);
        Image border = viewArea.AddComponent<Image>();
        border.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        SetRectTransform(viewArea, new Vector2(0.05f, 0.15f), new Vector2(0.7f, 0.85f), Vector2.zero, Vector2.zero);
        
        GameObject imageObj = CreateUIElement("RenderImage", viewArea.transform);
        viewerImage = imageObj.AddComponent<RawImage>();
        viewerImage.color = Color.white;
        SetRectTransform(imageObj, Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3)); // 稍微增加内边距
    }
    
    void CreateControlButtons(Transform parent)
    {
        closeButton = CreateButton("CloseButton", parent, "×", 32, new Color(0.8f, 0.2f, 0.2f, 1f),
            new Vector2(0.95f, 0.95f), new Vector2(1, 1), HideViewer);
        
        resetViewButton = CreateButton("ResetButton", parent, "重置视图", 15, new Color(0.2f, 0.6f, 0.8f, 1f), // 稍微增大字体
            new Vector2(0.75f, 0.05f), new Vector2(0.85f, 0.12f), ResetView);
        
        // 网格切换按钮
        CreateButton("GridButton", parent, "网格", 13, new Color(0.4f, 0.7f, 0.4f, 1f), // 稍微增大字体
            new Vector2(0.87f, 0.05f), new Vector2(0.95f, 0.12f), ToggleGrid);
        
        GameObject sliderObj = CreateUIElement("ZoomSlider", parent);
        zoomSlider = sliderObj.AddComponent<Slider>();
        SetRectTransform(sliderObj, new Vector2(0.75f, 0.25f), new Vector2(0.85f, 0.35f), Vector2.zero, Vector2.zero);
        zoomSlider.minValue = 0.1f;
        zoomSlider.maxValue = 8f; // 增加最大缩放倍数到8倍
        zoomSlider.value = 1f;
        zoomSlider.onValueChanged.AddListener(OnZoomChanged);
    }
    
    Button CreateButton(string name, Transform parent, string text, int fontSize, Color color, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = CreateUIElement(name, parent);
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;
        Button button = buttonObj.AddComponent<Button>();
        SetRectTransform(buttonObj, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        
        GameObject textObj = CreateUIElement($"{name}Text", buttonObj.transform);
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = fontSize;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        SetRectTransform(textObj, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        
        button.onClick.AddListener(onClick);
        return button;
    }
    
    void CreateInfoPanel(Transform parent)
    {
        GameObject infoPanel = CreateUIElement("InfoPanel", parent);
        Image infoBg = infoPanel.AddComponent<Image>();
        infoBg.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
        SetRectTransform(infoPanel, new Vector2(0.72f, 0.15f), new Vector2(0.95f, 0.85f), Vector2.zero, Vector2.zero);
        
        GameObject infoTextObj = CreateUIElement("InfoText", infoPanel.transform);
        infoText = infoTextObj.AddComponent<Text>();
        infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoText.fontSize = 13; // 稍微增大字体
        infoText.color = Color.white;
        infoText.alignment = TextAnchor.UpperLeft;
        infoText.lineSpacing = 1.2f; // 增加行间距
        SetRectTransform(infoTextObj, Vector2.zero, Vector2.one, new Vector2(8, 8), new Vector2(-8, -8)); // 增加内边距
    }
    
    void Create3DScene()
    {
        try
        {
            if (sceneContainer == null)
            {
                GameObject sceneObj = new GameObject("3DScene");
                sceneContainer = sceneObj.transform;
                sceneContainer.SetParent(transform);
                sceneContainer.localPosition = Vector3.zero;
                SetObjectLayer(sceneObj, viewerLayer);
            }
            
            if (viewerCamera == null)
            {
                GameObject cameraObj = new GameObject("ViewerCamera");
                cameraObj.transform.SetParent(sceneContainer);
                viewerCamera = cameraObj.AddComponent<Camera>();
                viewerCamera.clearFlags = CameraClearFlags.SolidColor;
                viewerCamera.backgroundColor = new Color(0.15f, 0.15f, 0.18f, 1f); // 专业深色背景
                viewerCamera.cullingMask = 1 << viewerLayer;
                viewerCamera.fieldOfView = 55f; // 更宽的初始视野以适应长档距段
                viewerCamera.farClipPlane = 1000f; // 更大的远裁剪面
                viewerCamera.nearClipPlane = 0.1f;
                viewerCamera.enabled = false; // 初始时禁用，等需要时再启用
                
                // 检测是否在exe中运行
                bool isInExe = !Application.isEditor;
                if (isInExe)
                {
                    viewerCamera.fieldOfView = 60f; // exe中更宽的视野
                    viewerCamera.farClipPlane = 800f; // 更大的远裁剪面
                }
                
                SetObjectLayer(cameraObj, viewerLayer);
                Debug.Log($"ViewerCamera创建完成: layer={viewerLayer}, enabled={viewerCamera.enabled}, FOV={viewerCamera.fieldOfView}, farClip={viewerCamera.farClipPlane}");
            }
            
            if (sceneLight == null)
            {
                GameObject lightObj = new GameObject("SceneLight");
                lightObj.transform.SetParent(sceneContainer);
                sceneLight = lightObj.AddComponent<Light>();
                sceneLight.type = LightType.Directional;
                sceneLight.intensity = 1.5f; // 增强亮度
                sceneLight.color = new Color(0.95f, 0.95f, 1f, 1f); // 冷色调照明
                sceneLight.cullingMask = 1 << viewerLayer;
                sceneLight.shadows = LightShadows.Soft;
                lightObj.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
                
                SetObjectLayer(lightObj, viewerLayer);
                Debug.Log("SceneLight创建完成");
            }
            
            // 创建专业网格和坐标轴
            CreateProfessionalGrid();
            CreateCoordinateAxes();
            
            Debug.Log("3D场景创建完成");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"创建3D场景时出错: {e.Message}");
        }
    }
    
    void CreateRenderTexture()
    {
        if (renderTexture == null)
        {
            try
            {
                // 检测是否在exe中运行，使用不同的设置
                bool isInExe = !Application.isEditor;
                
                if (isInExe)
                {
                    // exe中的优化设置
                    renderTexture = new RenderTexture(1280, 720, 16, RenderTextureFormat.ARGB32);
                    renderTexture.antiAliasing = 1; // 降低抗锯齿，提高兼容性
                    renderTexture.filterMode = FilterMode.Bilinear;
                    renderTexture.useMipMap = false; // 禁用mipmap，提高性能
                }
                else
                {
                    // 编辑器中的高质量设置
                    renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
                    renderTexture.antiAliasing = 4; // 4x抗锯齿
                    renderTexture.filterMode = FilterMode.Bilinear;
                }
                
                renderTexture.Create();
                
                if (!renderTexture.IsCreated())
                {
                    Debug.LogError("RenderTexture创建失败，尝试使用备用方案");
                    // 备用方案：使用更简单的设置
                    renderTexture = new RenderTexture(800, 600, 0, RenderTextureFormat.ARGB32);
                    renderTexture.Create();
                }
                
                Debug.Log($"RenderTexture创建成功: {renderTexture.width}x{renderTexture.height} (exe: {isInExe})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"创建RenderTexture时出错: {e.Message}");
                // 最后的备用方案
                renderTexture = new RenderTexture(640, 480, 0, RenderTextureFormat.ARGB32);
                renderTexture.Create();
            }
        }
    }
    
    void SetupCamera()
    {
        if (viewerCamera != null && renderTexture != null)
        {
            try
            {
                viewerCamera.targetTexture = renderTexture;
                
                // 确保相机设置正确
                viewerCamera.enabled = true;
                viewerCamera.clearFlags = CameraClearFlags.SolidColor;
                viewerCamera.backgroundColor = new Color(0.15f, 0.15f, 0.18f, 1f);
                
                // 关键：设置相机的层级遮罩，只渲染viewerLayer层级的对象
                viewerCamera.cullingMask = 1 << viewerLayer;
                
                // 设置相机本身的层级
                SetObjectLayer(viewerCamera.gameObject, viewerLayer);
                
                // 检测是否在exe中运行
                bool isInExe = !Application.isEditor;
                if (isInExe)
                {
                    // exe中的相机优化 - 适应长档距段
                    viewerCamera.fieldOfView = 60f; // 更宽的视野以适应长档距段
                    viewerCamera.farClipPlane = 800f; // 更大的远裁剪面
                }
                
                Debug.Log($"相机设置完成: targetTexture={renderTexture != null}, enabled={viewerCamera.enabled}, cullingMask={viewerCamera.cullingMask}, layer={viewerCamera.gameObject.layer}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"设置相机时出错: {e.Message}");
            }
        }
    }
    
    public void ShowPowerlineSegment(PowerlineInteraction powerline)
    {
        try
        {
            if (powerline?.powerlineInfo == null) return;
            
            var info = powerline.powerlineInfo;
            int towerSegmentIndex = info.index / 8;
            
            EnsureMainSceneIsolation();
            ClearCurrentScene();
            
            int wireIndexInSegment = info.index % 8;
            string wireTypeName = info.wireType == "GroundWire" ? "地线" : "主导线";
            
            UpdateUI(towerSegmentIndex, wireIndexInSegment, wireTypeName, info);
            CreateSegmentScene(info);
            ShowViewer();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"显示电力线3D视图时出错: {e.Message}");
        }
    }
    
    void UpdateUI(int towerSegmentIndex, int wireIndexInSegment, string wireTypeName, SceneInitializer.PowerlineInfo info)
    {
        if (titleText != null)
        {
            titleText.text = $"<b>电力线仿真分析系统</b> | 档距段 {towerSegmentIndex} - {wireTypeName} #{wireIndexInSegment} | {info.voltage}kV";
        }
        
        if (infoText != null)
        {
            float distance = Vector3.Distance(info.towerPositions[0], info.towerPositions[1]);
            float wireLength = distance * 1.02f; // 考虑悬垂增加的长度
            
            // 获取电力线的详细信息（如果存在）
            PowerlineInteraction powerlineInteraction = FindPowerlineInteraction(info.index);
            string status = "良好";
            string statusTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            float curvature = 0f;
            
            if (powerlineInteraction != null)
            {
                var detailInfo = powerlineInteraction.GetDetailedInfo();
                status = detailInfo.condition;
                statusTime = detailInfo.conditionSetTime.ToString("yyyy-MM-dd HH:mm");
                curvature = detailInfo.curvature;
            }
            
            infoText.text = $"<b><color=#4CAF50>▌导线基本信息</color></b>\n" +
                           $"<color=#E0E0E0>导线类型:</color> <color=#FF6B6B>{wireTypeName}</color>\n" +
                           $"<color=#E0E0E0>导线编号:</color> <color=#4ECDC4>#{wireIndexInSegment}</color>\n" +
                           $"<color=#E0E0E0>档距长度:</color> <color=#95E1D3>{distance:F2}m</color>\n" +
                           $"<color=#E0E0E0>导线长度:</color> <color=#95E1D3>{wireLength:F2}m</color>\n" +
                           $"<color=#E0E0E0>导线材质:</color> <color=#81C784>{GetWireMaterial(info.wireType)}</color>\n" +
                           $"<color=#E0E0E0>导线直径:</color> <color=#81C784>{GetWireDiameter(info.wireType):F1}mm</color>\n" +
                           $"<color=#E0E0E0>状态:</color> <color=#FFA726>{status} ({statusTime})</color>\n" +
                           $"<color=#E0E0E0>弯曲度:</color> <color=#FFA726>{curvature:F2}%</color>\n\n" +
                           $"<b><color=#2196F3>▌位置坐标</color></b>\n" +
                           $"<color=#E0E0E0>起始电塔:</color> <color=#4ECDC4>({info.towerPositions[0].x:F1}, {info.towerPositions[0].y:F1}, {info.towerPositions[0].z:F1})</color>\n" +
                           $"<color=#E0E0E0>终止电塔:</color> <color=#4ECDC4>({info.towerPositions[1].x:F1}, {info.towerPositions[1].y:F1}, {info.towerPositions[1].z:F1})</color>\n" +
                           $"<color=#E0E0E0>中心位置:</color> <color=#4ECDC4>({(info.towerPositions[0].x + info.towerPositions[1].x) / 2:F1}, {(info.towerPositions[0].y + info.towerPositions[1].y) / 2:F1}, {(info.towerPositions[0].z + info.towerPositions[1].z) / 2:F1})</color>\n\n" +
                           $"<b><color=#FF9800>▌档距信息</color></b>\n" +
                           $"<color=#E0E0E0>档距段编号:</color> <color=#FFCC02>{towerSegmentIndex}</color>\n" +
                           $"<color=#E0E0E0>电压等级:</color> <color=#FFCC02>{info.voltage}kV</color>\n" +
                           $"<color=#E0E0E0>安全距离:</color> <color=#FFCC02>{GetSafetyDistance(info.voltage):F1}m</color>\n\n" +
                           $"<b><color=#607D8B>▌操作说明</color></b>\n" +
                           $"<color=#90A4AE>• 左键拖拽:</color> <color=#E0E0E0>旋转视角</color>\n" +
                           $"<color=#90A4AE>• 中键拖拽:</color> <color=#E0E0E0>平移视角</color>\n" +
                           $"<color=#90A4AE>• 右键点击:</color> <color=#E0E0E0>重置视图</color>\n" +
                           $"<color=#90A4AE>• 滚轮:</color> <color=#E0E0E0>缩放视图</color>";
        }
    }
    
    /// <summary>
    /// 根据电力线索引查找对应的PowerlineInteraction组件
    /// </summary>
    private PowerlineInteraction FindPowerlineInteraction(int wireIndex)
    {
        PowerlineInteraction[] allPowerlines = FindObjectsOfType<PowerlineInteraction>();
        foreach (PowerlineInteraction powerline in allPowerlines)
        {
            if (powerline.powerlineInfo != null && powerline.powerlineInfo.index == wireIndex)
            {
                return powerline;
            }
        }
        return null;
    }
    
    void CreateSegmentScene(SceneInitializer.PowerlineInfo info)
    {
        try
        {
            if (info.towerPositions == null || info.towerPositions.Length < 2)
            {
                Debug.LogError("towerPositions为空或长度不足");
                return;
            }
            
            Debug.Log($"开始创建档距段场景: 起始电塔({info.towerPositions[0]}), 终止电塔({info.towerPositions[1]})");
            
            sceneCenter = (info.towerPositions[0] + info.towerPositions[1]) * 0.5f;
            Debug.Log($"场景中心: {sceneCenter}");
            
            CloneTargetTowers(info);
            AdjustCameraToScene();
            CleanupUnwantedObjects();
            
            // 验证场景中的对象
            ValidateSceneObjects();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"创建档距段场景时出错: {e.Message}");
        }
    }
    
    void ValidateSceneObjects()
    {
        if (sceneContainer == null)
        {
            Debug.LogError("sceneContainer为空");
            return;
        }
        
        int childCount = sceneContainer.childCount;
        Debug.Log($"场景容器中的子对象数量: {childCount}");
        
        for (int i = 0; i < childCount; i++)
        {
            Transform child = sceneContainer.GetChild(i);
            if (child != null)
            {
                Debug.Log($"子对象 {i}: {child.name} (layer: {child.gameObject.layer})");
                
                // 检查是否有渲染器
                Renderer[] renderers = child.GetComponentsInChildren<Renderer>();
                Debug.Log($"  - 渲染器数量: {renderers.Length}");
                
                // 检查是否在正确的层级
                if (child.gameObject.layer != viewerLayer)
                {
                    Debug.LogWarning($"  - 层级不匹配: 期望{viewerLayer}, 实际{child.gameObject.layer}");
                }
            }
        }
        
        // 检查相机设置
        if (viewerCamera != null)
        {
            Debug.Log($"相机设置: enabled={viewerCamera.enabled}, cullingMask={viewerCamera.cullingMask}, layer={viewerCamera.gameObject.layer}");
        }
    }
    
    void CloneTargetTowers(SceneInitializer.PowerlineInfo info)
    {
        try
        {
            string tower1Name = $"Tower_{info.towerPositions[0].x:F1}_{info.towerPositions[0].z:F1}";
            string tower2Name = $"Tower_{info.towerPositions[1].x:F1}_{info.towerPositions[1].z:F1}";
            
            Debug.Log($"查找电塔: {tower1Name}, {tower2Name}");
            
            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            Debug.Log($"场景根对象数量: {rootObjects.Length}");
            
            GameObject realTower1 = FindGameObjectInScene(rootObjects, tower1Name);
            GameObject realTower2 = FindGameObjectInScene(rootObjects, tower2Name);
            
            Debug.Log($"找到真实电塔: 电塔1={realTower1 != null}, 电塔2={realTower2 != null}");
            
            // 克隆或创建电塔
            if (realTower1 != null)
            {
                currentTower1 = CloneTower(realTower1, info.towerPositions[0], "起始电塔");
                Debug.Log($"电塔1克隆完成: {currentTower1 != null}");
            }
            else
            {
                currentTower1 = CreateTowerRepresentation(info.towerPositions[0], "起始电塔");
                Debug.Log($"电塔1创建完成: {currentTower1 != null}");
            }
            
            if (realTower2 != null)
            {
                currentTower2 = CloneTower(realTower2, info.towerPositions[1], "终止电塔");
                Debug.Log($"电塔2克隆完成: {currentTower2 != null}");
            }
            else
            {
                currentTower2 = CreateTowerRepresentation(info.towerPositions[1], "终止电塔");
                Debug.Log($"电塔2创建完成: {currentTower2 != null}");
            }
            
            // 克隆电力线
            CloneSegmentWires(info, rootObjects);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"克隆电塔时出错: {e.Message}");
        }
    }
    
    GameObject FindGameObjectInScene(GameObject[] rootObjects, string name)
    {
        if (rootObjects == null || string.IsNullOrEmpty(name)) return null;
        
        foreach (var root in rootObjects)
        {
            if (root?.name == name) return root;
            Transform found = root?.transform.Find(name);
            if (found != null) return found.gameObject;
        }
        
        return GameObject.Find(name);
    }
    
    GameObject CloneTower(GameObject originalTower, Vector3 targetPosition, string displayName)
    {
        try
        {
            if (originalTower == null || sceneContainer == null)
            {
                Debug.LogError($"克隆电塔失败: originalTower={originalTower != null}, sceneContainer={sceneContainer != null}");
                return null;
            }
            
            Debug.Log($"开始克隆电塔: {originalTower.name} -> {displayName}");
            
            GameObject clonedTower = Instantiate(originalTower);
            clonedTower.name = displayName;
            clonedTower.transform.SetParent(sceneContainer);
            clonedTower.transform.localPosition = targetPosition - sceneCenter;
            
            Debug.Log($"电塔位置设置: 目标={targetPosition}, 中心={sceneCenter}, 本地位置={clonedTower.transform.localPosition}");
            
            // 处理材质
            var renderers = clonedTower.GetComponentsInChildren<Renderer>();
            Debug.Log($"电塔渲染器数量: {renderers.Length}");
            foreach (var renderer in renderers)
            {
                if (renderer?.material != null)
                {
                    Material newMaterial = new Material(renderer.material);
                    renderer.material = newMaterial;
                    if (newMaterial.HasProperty("_Mode"))
                        newMaterial.SetFloat("_Mode", 0);
                    
                    Debug.Log($"材质设置完成: {renderer.name}");
                }
            }
            
            // 禁用碰撞器
            var colliders = clonedTower.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                if (collider != null) collider.enabled = false;
            }
            
            // 设置层级
            SetObjectLayer(clonedTower, viewerLayer);
            
            Debug.Log($"电塔克隆完成: {clonedTower.name}, layer={clonedTower.layer}");
            return clonedTower;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"克隆电塔时出错: {e.Message}");
            return null;
        }
    }
    
    void CloneSegmentWires(SceneInitializer.PowerlineInfo info, GameObject[] rootObjects)
    {
        try
        {
            Debug.Log("开始克隆电力线段");
            
            GameObject wireContainer = new GameObject("PowerlineContainer");
            wireContainer.transform.SetParent(sceneContainer);
            wireContainer.transform.localPosition = Vector3.zero;
            SetObjectLayer(wireContainer, viewerLayer);
            
            Debug.Log($"电力线容器创建完成: {wireContainer.name}, layer={wireContainer.layer}");
            
            bool wiresCreated = false;
            
            // 尝试克隆现有电力线
            if (CloneExistingWires(info, rootObjects, wireContainer))
            {
                Debug.Log("成功克隆现有电力线");
                wiresCreated = true;
            }
            else
            {
                Debug.Log("克隆现有电力线失败，尝试从引脚生成");
                
                // 尝试从引脚生成电力线
                if (GenerateWiresFromPins(info, wireContainer))
                {
                    Debug.Log("成功从引脚生成电力线");
                    wiresCreated = true;
                }
                else
                {
                    Debug.Log("从引脚生成电力线失败，创建默认电力线");
                    
                    // 创建默认电力线
                    CreateDefaultWires(wireContainer);
                    wiresCreated = true;
                }
            }
            
            if (wiresCreated)
            {
                int wireCount = wireContainer.transform.childCount;
                Debug.Log($"电力线创建完成，共{wireCount}根导线");
                
                // 验证电力线
                for (int i = 0; i < wireCount; i++)
                {
                    Transform wire = wireContainer.transform.GetChild(i);
                    if (wire != null)
                    {
                        LineRenderer lr = wire.GetComponent<LineRenderer>();
                        if (lr != null)
                        {
                            Debug.Log($"电力线 {i}: {wire.name}, 位置数={lr.positionCount}, layer={wire.gameObject.layer}");
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"克隆电力线段时出错: {e.Message}");
        }
    }
    
    bool CloneExistingWires(SceneInitializer.PowerlineInfo info, GameObject[] rootObjects, GameObject wireContainer)
    {
        SceneInitializer sceneInitializer = FindObjectOfType<SceneInitializer>();
        if (sceneInitializer == null) return false;
        
        int towerSegmentIndex = info.index / 8;
        List<GameObject> segmentWires = new List<GameObject>();
        
        for (int wireIndex = 0; wireIndex < 8; wireIndex++)
        {
            int globalWireIndex = towerSegmentIndex * 8 + wireIndex;
            GameObject wire = FindWireByIndex(globalWireIndex, rootObjects);
            if (wire != null) segmentWires.Add(wire);
        }
        
        if (segmentWires.Count == 8)
        {
            foreach (GameObject originalWire in segmentWires)
            {
                CloneWireToScene(originalWire, wireContainer);
            }
            return true;
        }
        
        return false;
    }
    
    GameObject FindWireByIndex(int wireIndex, GameObject[] rootObjects)
    {
        PowerlineInteraction[] allPowerlines = FindObjectsOfType<PowerlineInteraction>();
        foreach (PowerlineInteraction powerline in allPowerlines)
        {
            if (powerline.powerlineInfo != null && powerline.powerlineInfo.index == wireIndex)
            {
                return powerline.gameObject;
            }
        }
        return null;
    }
    
    void CloneWireToScene(GameObject originalWire, GameObject wireContainer)
    {
        GameObject clonedWire = Instantiate(originalWire);
        clonedWire.transform.SetParent(wireContainer.transform);
        clonedWire.transform.localPosition = Vector3.zero;
        clonedWire.transform.localRotation = Quaternion.identity;
        
        SetObjectLayer(clonedWire, viewerLayer);
        
        PowerlineInteraction interaction = clonedWire.GetComponent<PowerlineInteraction>();
        if (interaction != null && interaction.powerlineInfo != null)
        {
            RecalculateWirePath(clonedWire, interaction.powerlineInfo);
        }
    }
    
    void RecalculateWirePath(GameObject wireObj, SceneInitializer.PowerlineInfo wireInfo)
    {
        LineRenderer lr = wireObj.GetComponent<LineRenderer>();
        if (lr == null) return;
        
        int wireIndex = wireInfo.index % 8;
        Vector3 startPin = GetTowerPinPosition(currentTower1, wireIndex);
        Vector3 endPin = GetTowerPinPosition(currentTower2, wireIndex);
        
        Vector3[] wirePath = CreateSaggedWirePath(startPin, endPin, 20);
        lr.positionCount = wirePath.Length;
        lr.SetPositions(wirePath);
    }
    
    bool GenerateWiresFromPins(SceneInitializer.PowerlineInfo info, GameObject wireContainer)
    {
        if (currentTower1 == null || currentTower2 == null) return false;
        
        List<Vector3> tower1Pins = GetTowerPinPositions(currentTower1);
        List<Vector3> tower2Pins = GetTowerPinPositions(currentTower2);
        
        if (tower1Pins.Count != 8 || tower2Pins.Count != 8) return false;
        
        for (int i = 0; i < 8; i++)
        {
            CreateWireFromPins(tower1Pins[i], tower2Pins[i], i, info, wireContainer);
        }
        
        return true;
    }
    
    List<Vector3> GetTowerPinPositions(GameObject tower)
    {
        TowerPinpointSystem pinSystem = tower.GetComponent<TowerPinpointSystem>();
        if (pinSystem == null) return GetDefaultPinPositions(tower);
        
        List<Vector3> pinPositions = new List<Vector3>();
        var pins = pinSystem.GetTowerPins(tower);
        foreach (var pin in pins)
        {
            Vector3 worldPos = tower.transform.TransformPoint(pin.localPosition);
            pinPositions.Add(worldPos);
        }
        
        return pinPositions;
    }
    
    List<Vector3> GetDefaultPinPositions(GameObject tower)
    {
        List<Vector3> positions = new List<Vector3>();
        Vector3 towerPos = tower.transform.position;
        
        Renderer[] renderers = tower.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(towerPos, Vector3.zero);
        foreach (var renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        
        float height = bounds.size.y;
        float width = Mathf.Max(bounds.size.x, bounds.size.z);
        
        float[] layerHeights = {
            towerPos.y + height * 0.8f,
            towerPos.y + height * 0.65f,
            towerPos.y + height * 0.5f,
            towerPos.y + height * 0.35f
        };
        
        float frontZ = towerPos.z + width * 0.3f;
        float backZ = towerPos.z - width * 0.3f;
        
        for (int i = 0; i < 4; i++)
        {
            positions.Add(new Vector3(towerPos.x, layerHeights[i], frontZ));
        }
        
        for (int i = 0; i < 4; i++)
        {
            positions.Add(new Vector3(towerPos.x, layerHeights[i], backZ));
        }
        
        return positions;
    }
    
    void CreateWireFromPins(Vector3 startPin, Vector3 endPin, int wireIndex, SceneInitializer.PowerlineInfo info, GameObject wireContainer)
    {
        string wireType = (wireIndex < 4) ? "GroundWire" : "Conductor";
        string wireTypeName = (wireIndex < 4) ? "G" : "C";
        int wireIndexInType = (wireIndex < 4) ? wireIndex : (wireIndex - 4);
        
        GameObject wireObj = new GameObject($"Wire_{wireTypeName}{wireIndexInType}");
        wireObj.transform.SetParent(wireContainer.transform);
        
        SetObjectLayer(wireObj, viewerLayer);
        
        LineRenderer lr = wireObj.AddComponent<LineRenderer>();
        Material wireMaterial = CreateDefaultWireMaterial(wireType);
        lr.material = wireMaterial;
        lr.startWidth = 0.2f;
        lr.endWidth = 0.2f;
        lr.useWorldSpace = true;
        
        Vector3[] wirePath = CreateSaggedWirePath(startPin, endPin, 20);
        lr.positionCount = wirePath.Length;
        lr.SetPositions(wirePath);
        
        PowerlineInteraction interaction = wireObj.AddComponent<PowerlineInteraction>();
        SceneInitializer.PowerlineInfo wireInfo = new SceneInitializer.PowerlineInfo
        {
            index = info.index + wireIndex,
            wireType = wireType,
            wireIndex = wireIndexInType,
            start = startPin,
            end = endPin,
            length = Vector3.Distance(startPin, endPin),
            voltage = info.voltage,
            towerPositions = new Vector3[] { currentTower1.transform.position, currentTower2.transform.position }
        };
        
        wireInfo.points.Add(startPin);
        wireInfo.points.Add(endPin);
        interaction.SetPowerlineInfo(wireInfo);
    }
    
    Vector3 GetTowerPinPosition(GameObject tower, int pinIndex)
    {
        if (tower == null) return Vector3.zero;
        
        List<Vector3> pinPositions = GetTowerPinPositions(tower);
        if (pinIndex >= 0 && pinIndex < pinPositions.Count)
        {
            return pinPositions[pinIndex];
        }
        
        return tower.transform.position + Vector3.up * 10f;
    }
    
    void CreateDefaultWires(GameObject wireContainer)
    {
        if (currentTower1 == null || currentTower2 == null) return;
        
        Vector3 tower1Pos = currentTower1.transform.localPosition;
        Vector3 tower2Pos = currentTower2.transform.localPosition;
        Vector3[] pinOffsets = GetTowerPinOffsets();
        
        for (int i = 0; i < 8; i++)
        {
            string wireType = (i < 4) ? "GroundWire" : "Conductor";
            string wireTypeName = (i < 4) ? "G" : "C";
            int wireIndexInType = (i < 4) ? i : (i - 4);
            
            GameObject wire = new GameObject($"DefaultWire_{wireTypeName}{wireIndexInType}");
            wire.transform.SetParent(wireContainer.transform, false);
            
            LineRenderer lr = wire.AddComponent<LineRenderer>();
            lr.material = CreateDefaultWireMaterial(wireType);
            lr.startWidth = 0.2f;
            lr.endWidth = 0.2f;
            lr.useWorldSpace = true;
            
            Vector3 startPin = tower1Pos + pinOffsets[i];
            Vector3 endPin = tower2Pos + pinOffsets[i];
            
            Vector3[] wirePoints = CreateSaggedWirePath(startPin, endPin, 10);
            lr.positionCount = wirePoints.Length;
            lr.SetPositions(wirePoints);
            
            SetObjectLayer(wire, viewerLayer);
        }
    }
    
    Vector3[] GetTowerPinOffsets()
    {
        Vector3[] pinOffsets = new Vector3[8];
        
        // 地线引脚（4个，在电塔顶部）
        pinOffsets[0] = new Vector3(-3f, 14f, 0f);
        pinOffsets[1] = new Vector3(-1f, 14f, 0f);
        pinOffsets[2] = new Vector3(1f, 14f, 0f);
        pinOffsets[3] = new Vector3(3f, 14f, 0f);
        
        // 主导线引脚（4个，在电塔中部）
        pinOffsets[4] = new Vector3(-4f, 10f, 0f);
        pinOffsets[5] = new Vector3(-2f, 8f, 0f);
        pinOffsets[6] = new Vector3(2f, 8f, 0f);
        pinOffsets[7] = new Vector3(4f, 10f, 0f);
        
        return pinOffsets;
    }
    
    Vector3[] CreateSaggedWirePath(Vector3 startPoint, Vector3 endPoint, int segments)
    {
        Vector3[] points = new Vector3[segments];
        float distance = Vector3.Distance(startPoint, endPoint);
        
        // 根据距离动态调整下垂程度
        float maxSag = 2f;
        if (distance > 50f)
        {
            // 长距离时减少下垂程度
            maxSag = Mathf.Lerp(2f, 0.5f, (distance - 50f) / 100f);
            maxSag = Mathf.Max(maxSag, 0.2f); // 最小下垂程度
        }
        
        // 计算地面高度（取起点和终点的最低高度作为参考）
        float groundHeight = Mathf.Min(startPoint.y, endPoint.y) - 15f; // 假设电力线高度偏移为15米
        
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            Vector3 basePoint = Vector3.Lerp(startPoint, endPoint, t);
            float sag = Mathf.Sin(t * Mathf.PI) * maxSag;
            Vector3 point = new Vector3(basePoint.x, basePoint.y - sag, basePoint.z);
            
            // 确保点不会低于地面
            point.y = Mathf.Max(point.y, groundHeight);
            
            points[i] = point;
        }
        
        return points;
    }
    
    Material CreateDefaultWireMaterial(string wireType)
    {
        Material mat = new Material(Shader.Find("Standard"));
        
        if (wireType == "GroundWire" || wireType == "Ground")
        {
            mat.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            mat.name = "GroundWireMaterial";
        }
        else
        {
            mat.color = new Color(0.8f, 0.7f, 0.5f, 1f);
            mat.name = "ConductorMaterial";
        }
        
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0.8f);
        
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.6f);
        
        return mat;
    }
    
    void SetObjectLayer(GameObject obj, int layer)
    {
        if (obj == null) return;
        
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetObjectLayer(child.gameObject, layer);
        }
    }
    
    void EnsureMainSceneIsolation()
    {
        Camera[] allCameras = FindObjectsOfType<Camera>();
        
        foreach (Camera cam in allCameras)
        {
            if (cam == viewerCamera) continue;
            
            if ((cam.cullingMask & (1 << viewerLayer)) != 0)
            {
                cam.cullingMask &= ~(1 << viewerLayer);
            }
        }
    }
    
    GameObject CreateTowerRepresentation(Vector3 position, string name)
    {
        try
        {
            Debug.Log($"创建电塔表示: {name} at {position}");
            
            GameObject tower = new GameObject(name);
            tower.transform.SetParent(sceneContainer);
            tower.transform.localPosition = position - sceneCenter;
            
            Debug.Log($"电塔表示位置: 目标={position}, 中心={sceneCenter}, 本地位置={tower.transform.localPosition}");
            
            // 创建电塔主体
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.transform.SetParent(tower.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(2f, 15f, 2f);
            
            // 设置材质
            Material bodyMaterial = new Material(Shader.Find("Standard"));
            bodyMaterial.color = Color.gray;
            body.GetComponent<Renderer>().material = bodyMaterial;
            
            // 创建横担
            GameObject crossarm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossarm.transform.SetParent(tower.transform);
            crossarm.transform.localPosition = new Vector3(0, 8f, 0);
            crossarm.transform.localScale = new Vector3(8f, 0.5f, 0.5f);
            
            Material crossarmMaterial = new Material(Shader.Find("Standard"));
            crossarmMaterial.color = new Color(0.2f, 0.2f, 0.2f);
            crossarm.GetComponent<Renderer>().material = crossarmMaterial;
            
            // 设置层级
            SetObjectLayer(tower, viewerLayer);
            
            Debug.Log($"电塔表示创建完成: {tower.name}, layer={tower.layer}, 渲染器数量={tower.GetComponentsInChildren<Renderer>().Length}");
            return tower;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"创建电塔表示时出错: {e.Message}");
            return null;
        }
    }
    
    void AdjustCameraToScene()
    {
        try
        {
            if (viewerCamera == null)
            {
                Debug.LogError("ViewerCamera为空，无法调整相机");
                return;
            }
            
            Debug.Log("开始调整相机位置");
            
            // 计算档距段的实际长度
            float spanLength = 0f;
            if (currentTower1 != null && currentTower2 != null)
            {
                spanLength = Vector3.Distance(currentTower1.transform.localPosition, currentTower2.transform.localPosition);
                Debug.Log($"档距段长度: {spanLength}");
            }
            
            // 根据档距段长度动态调整相机参数
            float baseDistance = Mathf.Max(spanLength * 0.8f, 50f); // 至少50米，长档距段按80%计算
            float cameraHeight = baseDistance * 0.4f; // 相机高度为距离的40%
            float cameraDistance = baseDistance * 1.2f; // 相机距离为基准距离的120%
            
            // 设置相机位置
            Vector3 cameraPosition = new Vector3(0, cameraHeight, -cameraDistance);
            viewerCamera.transform.localPosition = cameraPosition;
            viewerCamera.transform.LookAt(Vector3.zero);
            
            // 根据档距段长度调整视野
            float targetFOV;
            if (spanLength > 200f) // 长档距段
            {
                targetFOV = 60f; // 更宽的视野
            }
            else if (spanLength > 100f) // 中等档距段
            {
                targetFOV = 50f;
            }
            else // 短档距段
            {
                targetFOV = 45f;
            }
            
            // 检测是否在exe中运行
            bool isInExe = !Application.isEditor;
            if (isInExe)
            {
                targetFOV = Mathf.Min(targetFOV + 5f, 70f); // exe中稍微增加视野
            }
            
            viewerCamera.fieldOfView = targetFOV;
            
            // 调整远裁剪面以适应长档距段
            float farClipPlane = Mathf.Max(cameraDistance * 2f, 500f);
            viewerCamera.farClipPlane = farClipPlane;
            
            Debug.Log($"相机调整完成: 档距长度={spanLength}, 位置={cameraPosition}, FOV={targetFOV}, 远裁剪面={farClipPlane}");
            
            // 验证场景边界
            if (sceneContainer != null)
            {
                Bounds sceneBounds = CalculateSceneBounds();
                Debug.Log($"场景边界: 中心={sceneBounds.center}, 大小={sceneBounds.size}");
                
                // 检查是否所有对象都在视野内
                bool allObjectsVisible = true;
                Renderer[] renderers = sceneContainer.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer != null && renderer.enabled)
                    {
                        Vector3 viewportPoint = viewerCamera.WorldToViewportPoint(renderer.bounds.center);
                        if (viewportPoint.x < -0.1f || viewportPoint.x > 1.1f || 
                            viewportPoint.y < -0.1f || viewportPoint.y > 1.1f || 
                            viewportPoint.z < 0f)
                        {
                            Debug.LogWarning($"对象 {renderer.name} 可能不在视野内: {viewportPoint}");
                            allObjectsVisible = false;
                        }
                    }
                }
                
                if (allObjectsVisible)
                {
                    Debug.Log("所有对象都在视野内");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"调整相机位置时出错: {e.Message}");
        }
    }
    
    Bounds CalculateSceneBounds()
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
        bool hasBounds = false;
        
        if (sceneContainer != null)
        {
            Renderer[] renderers = sceneContainer.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.enabled)
                {
                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }
        }
        
        return bounds;
    }
    
    void ClearCurrentScene()
    {
        currentTower1 = null;
        currentTower2 = null;
        
        if (sceneContainer != null)
        {
            List<GameObject> objectsToDestroy = new List<GameObject>();
            
            for (int i = 0; i < sceneContainer.childCount; i++)
            {
                GameObject child = sceneContainer.GetChild(i).gameObject;
                if (child != null && child.GetComponent<Camera>() == null && child.GetComponent<Light>() == null)
                {
                    objectsToDestroy.Add(child);
                }
            }
            
            foreach (var obj in objectsToDestroy)
            {
                if (obj != null) DestroyImmediate(obj);
            }
        }
    }
    
    void CleanupUnwantedObjects()
    {
        if (sceneContainer == null) return;
        
        List<string> preservedNames = new List<string>
        {
            "ViewerCamera", "SceneLight", "起始电塔", "终止电塔", "PowerlineContainer"
        };
        
        List<GameObject> objectsToDestroy = new List<GameObject>();
        
        for (int i = 0; i < sceneContainer.childCount; i++)
        {
            GameObject child = sceneContainer.GetChild(i).gameObject;
            
            bool shouldPreserve = child.GetComponent<Camera>() != null || 
                                child.GetComponent<Light>() != null ||
                                child == currentTower1 || 
                                child == currentTower2 ||
                                preservedNames.Contains(child.name);
            
            if (!shouldPreserve) objectsToDestroy.Add(child);
        }
        
        foreach (var obj in objectsToDestroy)
        {
            if (obj != null) DestroyImmediate(obj);
        }
    }
    
    void ShowViewer()
    {
        try
        {
            // 确保RenderTexture存在
            if (renderTexture == null)
            {
                Debug.LogWarning("RenderTexture为空，重新创建");
                CreateRenderTexture();
            }
            
            // 确保相机存在
            if (viewerCamera == null)
            {
                Debug.LogWarning("ViewerCamera为空，重新创建");
                Create3DScene();
            }
            
            // 显示面板
            if (viewerPanel != null)
            {
                viewerPanel.SetActive(true);
                Debug.Log("3D查看器面板已显示");
            }
            else
            {
                Debug.LogError("ViewerPanel为空");
            }
            
            // 设置相机和渲染纹理
            if (viewerCamera != null && renderTexture != null)
            {
                viewerCamera.targetTexture = renderTexture;
                viewerCamera.enabled = true;
                
                // 强制渲染一帧
                viewerCamera.Render();
                
                Debug.Log($"相机设置完成: enabled={viewerCamera.enabled}, targetTexture={viewerCamera.targetTexture != null}");
            }
            else
            {
                Debug.LogError($"相机或渲染纹理为空: camera={viewerCamera != null}, texture={renderTexture != null}");
            }
            
            // 设置UI图像
            if (viewerImage != null && renderTexture != null)
            {
                viewerImage.texture = renderTexture;
                Debug.Log("UI图像纹理已设置");
            }
            else
            {
                Debug.LogError($"UI图像或渲染纹理为空: image={viewerImage != null}, texture={renderTexture != null}");
            }
            
            // 检测是否在exe中运行
            bool isInExe = !Application.isEditor;
            if (isInExe)
            {
                Debug.Log("在exe中运行，应用特殊优化");
                // 在exe中延迟一帧再渲染，确保所有设置都生效
                StartCoroutine(DelayedRender());
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"显示3D查看器时出错: {e.Message}");
        }
    }
    
    System.Collections.IEnumerator DelayedRender()
    {
        yield return new WaitForEndOfFrame();
        
        if (viewerCamera != null && renderTexture != null)
        {
            // 强制刷新场景中的所有对象
            ForceRefreshSceneObjects();
            
            // 重新调整相机位置
            AdjustCameraToScene();
            
            // 强制渲染
            viewerCamera.Render();
            Debug.Log("延迟渲染完成");
        }
    }
    
    void ForceRefreshSceneObjects()
    {
        try
        {
            if (sceneContainer == null) return;
            
            Debug.Log("强制刷新场景对象");
            
            // 确保所有渲染器都启用
            Renderer[] renderers = sceneContainer.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                    // 强制更新材质
                    if (renderer.material != null)
                    {
                        renderer.material.SetPass(0);
                    }
                }
            }
            
            // 确保所有LineRenderer都启用
            LineRenderer[] lineRenderers = sceneContainer.GetComponentsInChildren<LineRenderer>();
            foreach (var lr in lineRenderers)
            {
                if (lr != null)
                {
                    lr.enabled = true;
                    // 强制更新材质
                    if (lr.material != null)
                    {
                        lr.material.SetPass(0);
                    }
                }
            }
            
            // 确保灯光正确设置
            if (sceneLight != null)
            {
                sceneLight.enabled = true;
                sceneLight.cullingMask = 1 << viewerLayer;
            }
            
            Debug.Log($"场景刷新完成: 渲染器={renderers.Length}, 线渲染器={lineRenderers.Length}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"强制刷新场景对象时出错: {e.Message}");
        }
    }
    
    public void HideViewer()
    {
        if (viewerPanel != null) viewerPanel.SetActive(false);
        if (viewerCamera != null)
        {
            viewerCamera.targetTexture = null;
            viewerCamera.enabled = false;
        }
        ClearCurrentScene();
    }
    
    void ResetView()
    {
        AdjustCameraToScene();
        currentZoom = 1f;
        if (zoomSlider != null) zoomSlider.value = 1f;
        isDragging = false;
        isPanning = false;
    }
    
    void OnZoomChanged(float value)
    {
        currentZoom = value;
        if (viewerCamera != null) viewerCamera.fieldOfView = 60f / value;
    }
    
    void Update()
    {
        if (!viewerPanel.activeSelf) return;
        HandleInput();
    }
    
    void HandleInput()
    {
        // 鼠标左键：旋转视角
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
        
        // 鼠标中键：平移视角
        if (Input.GetMouseButtonDown(2))
        {
            isPanning = true;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(2))
        {
            isPanning = false;
        }
        
        if (isDragging && viewerCamera != null)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 rotationCenter = Vector3.zero;
            viewerCamera.transform.RotateAround(rotationCenter, Vector3.up, delta.x * 0.5f);
            viewerCamera.transform.RotateAround(rotationCenter, viewerCamera.transform.right, -delta.y * 0.5f);
            lastMousePosition = Input.mousePosition;
        }
        
        if (isPanning && viewerCamera != null)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 move = viewerCamera.transform.right * (-delta.x * 0.01f) + viewerCamera.transform.up * (-delta.y * 0.01f);
            viewerCamera.transform.position += move;
            lastMousePosition = Input.mousePosition;
        }
        
        // 滚轮缩放
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0 && zoomSlider != null)
        {
            // 根据当前缩放级别调整步长，高倍数时步长更大
            float zoomStep = Mathf.Lerp(0.5f, 2f, zoomSlider.value / zoomSlider.maxValue);
            zoomSlider.value += scroll * zoomStep;
        }
        
        // 右键重置视图
        if (Input.GetMouseButtonDown(1))
        {
            ResetView();
        }
    }
    
    string GetWireMaterial(string wireType)
    {
        return wireType == "GroundWire" ? "钢芯铝绞线" : "铝合金导线";
    }
    
    float GetWireDiameter(string wireType)
    {
        return wireType == "GroundWire" ? 12.6f : 28.6f; // mm
    }
    
    float GetCurrentCapacity(string wireType)
    {
        return wireType == "GroundWire" ? 0f : 845f; // A，地线不载流
    }
    
    float GetResistance(string wireType)
    {
        return wireType == "GroundWire" ? 0.2890f : 0.0754f; // Ω/km
    }
    
    float GetLineDensity(string wireType)
    {
        return wireType == "GroundWire" ? 0.67f : 1.349f; // kg/m
    }
    
    float GetBreakingForce(string wireType)
    {
        return wireType == "GroundWire" ? 62700f : 108800f; // N
    }
    
    float CalculateSagHeight(float spanLength, float voltage)
    {
        // 简化的弧垂计算公式
        float baseRatio = voltage >= 220f ? 0.025f : 0.03f;
        return spanLength * baseRatio;
    }
    
    float CalculateTensionForce(float voltage, float wireLength)
    {
        // 简化的张力计算
        return voltage >= 220f ? 15000f : 12000f;
    }
    
    float GetSafetyDistance(float voltage)
    {
        if (voltage >= 500f) return 8.5f;
        if (voltage >= 220f) return 4.0f;
        if (voltage >= 110f) return 2.0f;
        return 1.0f;
    }
    
    void CreateProfessionalGrid()
    {
        if (gridObject == null && showGrid)
        {
            gridObject = new GameObject("ProfessionalGrid");
            gridObject.transform.SetParent(sceneContainer);
            gridObject.transform.localPosition = Vector3.zero;
            
            // 创建空间网格（3D网格）
            CreateSpatialGrid();
            SetObjectLayer(gridObject, viewerLayer);
        }
    }
    
    void CreateSpatialGrid()
    {
        List<Vector3> allGridPoints = new List<Vector3>();
        float halfGrid = gridSize * 8f; // 扩大网格范围
        float height = gridSize * 5f; // 增加网格高度
        
        // 地面网格 (XZ平面)
        for (int i = -8; i <= 8; i++)
        {
            float z = i * gridSize;
            allGridPoints.Add(new Vector3(-halfGrid, 0, z));
            allGridPoints.Add(new Vector3(halfGrid, 0, z));
        }
        for (int i = -8; i <= 8; i++)
        {
            float x = i * gridSize;
            allGridPoints.Add(new Vector3(x, 0, -halfGrid));
            allGridPoints.Add(new Vector3(x, 0, halfGrid));
        }
        
        // 垂直网格线 (Y轴方向)
        for (int i = -8; i <= 8; i++)
        {
            for (int j = -8; j <= 8; j++)
            {
                if (i % 3 == 0 && j % 3 == 0) // 适当减少垂直线密度
                {
                    float x = i * gridSize;
                    float z = j * gridSize;
                    allGridPoints.Add(new Vector3(x, 0, z));
                    allGridPoints.Add(new Vector3(x, height, z));
                }
            }
        }
        
        // 空中水平网格 (不同高度的XZ平面)
        for (int h = 1; h <= 5; h++)
        {
            float y = h * gridSize;
            // 主要网格线
            for (int i = -8; i <= 8; i += 2)
            {
                float z = i * gridSize;
                allGridPoints.Add(new Vector3(-halfGrid, y, z));
                allGridPoints.Add(new Vector3(halfGrid, y, z));
            }
            for (int i = -8; i <= 8; i += 2)
            {
                float x = i * gridSize;
                allGridPoints.Add(new Vector3(x, y, -halfGrid));
                allGridPoints.Add(new Vector3(x, y, halfGrid));
            }
        }
        
        // 创建单个LineRenderer来绘制所有网格线
        LineRenderer gridRenderer = gridObject.AddComponent<LineRenderer>();
        gridRenderer.material = CreateGridMaterial();
        gridRenderer.startWidth = 0.04f; // 稍微增加线宽，提高清晰度
        gridRenderer.endWidth = 0.04f;
        gridRenderer.useWorldSpace = false;
        gridRenderer.positionCount = allGridPoints.Count;
        gridRenderer.SetPositions(allGridPoints.ToArray());
    }
    
    void CreateCoordinateAxes()
    {
        if (axesObject == null && showAxes)
        {
            axesObject = new GameObject("CoordinateAxes");
            axesObject.transform.SetParent(sceneContainer);
            axesObject.transform.localPosition = Vector3.zero;
            
            // X轴 (红色)
            CreateAxis(axesObject.transform, Vector3.right, Color.red, "X");
            // Y轴 (绿色)
            CreateAxis(axesObject.transform, Vector3.up, Color.green, "Y");
            // Z轴 (蓝色)
            CreateAxis(axesObject.transform, Vector3.forward, Color.blue, "Z");
            
            SetObjectLayer(axesObject, viewerLayer);
        }
    }
    
    void CreateAxis(Transform parent, Vector3 direction, Color color, string label)
    {
        GameObject axis = new GameObject($"Axis_{label}");
        axis.transform.SetParent(parent);
        axis.transform.localPosition = Vector3.zero;
        
        LineRenderer axisRenderer = axis.AddComponent<LineRenderer>();
        axisRenderer.material = CreateAxisMaterial(color);
        axisRenderer.startWidth = 0.08f;
        axisRenderer.endWidth = 0.08f;
        axisRenderer.useWorldSpace = false;
        axisRenderer.positionCount = 2;
        axisRenderer.SetPositions(new Vector3[] { Vector3.zero, direction * 15f });
        
        // 创建箭头
        CreateArrowHead(axis.transform, direction, color);
    }
    
    void CreateArrowHead(Transform parent, Vector3 direction, Color color)
    {
        GameObject arrowHead = new GameObject("ArrowHead");
        arrowHead.transform.SetParent(parent);
        arrowHead.transform.localPosition = direction * 15f;
        
        LineRenderer arrowRenderer = arrowHead.AddComponent<LineRenderer>();
        arrowRenderer.material = CreateAxisMaterial(color);
        arrowRenderer.startWidth = 0.1f;
        arrowRenderer.endWidth = 0.02f;
        arrowRenderer.useWorldSpace = false;
        arrowRenderer.positionCount = 4;
        
        Vector3 perpendicular1 = Vector3.Cross(direction, Vector3.up).normalized;
        if (perpendicular1.magnitude < 0.1f)
            perpendicular1 = Vector3.Cross(direction, Vector3.right).normalized;
        Vector3 perpendicular2 = Vector3.Cross(direction, perpendicular1).normalized;
        
        Vector3[] arrowPoints = new Vector3[]
        {
            Vector3.zero,
            -direction * 2f + perpendicular1 * 0.5f,
            Vector3.zero,
            -direction * 2f + perpendicular2 * 0.5f
        };
        
        arrowRenderer.SetPositions(arrowPoints);
    }
    
    Material CreateGridMaterial()
    {
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(0.35f, 0.35f, 0.4f, 0.7f); // 稍微提高可见度和对比度
        return mat;
    }
    
    Material CreateAxisMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;
        return mat;
    }
    
    void ToggleGrid()
    {
        showGrid = !showGrid;
        if (gridObject != null)
        {
            gridObject.SetActive(showGrid);
        }
        else if (showGrid)
        {
            CreateProfessionalGrid();
        }
    }
    
    void ToggleAxes()
    {
        showAxes = !showAxes;
        if (axesObject != null)
        {
            axesObject.SetActive(showAxes);
        }
        else if (showAxes)
        {
            CreateCoordinateAxes();
        }
    }

    void OnDestroy()
    {
        if (viewerCamera != null)
        {
            viewerCamera.targetTexture = null;
            viewerCamera.enabled = false;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            DestroyImmediate(renderTexture);
            renderTexture = null;
        }
    }
} 

