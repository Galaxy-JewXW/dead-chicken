using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PowerlineSystem;
using System.Collections;
using UI;

namespace UI
{
    /// <summary>
    /// 独立点云查看器 v3.1 - 美化版
    /// 现代化Material Design风格界面，优化视觉效果
    /// 完整的相机控制功能：缩放、旋转、平移
    /// </summary>
    public class PointCloudViewer : MonoBehaviour
    {
        [Header("查看器设置")]
        [Tooltip("查看器窗口标题")]
        public string viewerTitle = "点云数据查看器";
        
        [Tooltip("查看器窗口大小")]
        public Vector2 windowSize = new Vector2(1400, 900);
        
        [Tooltip("点云相机设置")]
        public LayerMask pointCloudLayer = -1;
        
        [Header("层级设置")]
        [Tooltip("点云专用层级")]
        public int pointCloudLayerIndex = 31; // 使用最高层级避免冲突
        
        [Header("点云管理")]
        [Tooltip("选择的点云文件")]
        public string selectedPointCloudFile = "A部分";
        
        [Header("界面主题 - 优化配色")]
        [Tooltip("主要颜色 - 现代蓝")]
        public Color primaryColor = new Color(0.24f, 0.54f, 0.98f, 1f);
        [Tooltip("强调颜色 - 翡翠绿")]
        public Color accentColor = new Color(0.12f, 0.85f, 0.38f, 1f);
        [Tooltip("危险颜色 - 珊瑚红")]
        public Color dangerColor = new Color(1f, 0.36f, 0.31f, 1f);
        [Tooltip("背景颜色 - 深邃夜空")]
        public Color backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.98f);
        [Tooltip("面板颜色 - 石墨灰")]
        public Color panelColor = new Color(0.15f, 0.15f, 0.18f, 0.96f);
        [Tooltip("渐变叠加色 - 神秘紫")]
        public Color gradientOverlayColor = new Color(0.18f, 0.24f, 0.42f, 0.15f);
        
        // 查看器组件
        private GameObject viewerWindow;
        private GameObject originalViewerWindow; // 保存原始的Canvas对象
        private Canvas viewerCanvas;
        private Camera pointCloudCamera;
        private PowerlinePointCloudManager pointCloudManager;
        private RenderTexture renderTexture;
        
        // UI组件
        private GameObject titleBar;
        private GameObject controlPanel;
        private GameObject renderArea;
        private GameObject statsPanel;
        private Button closeButton;
        private Button resetCameraButton;
        private Text statusText;
        private Text statsText;
        private Text titleText;
        private Text cameraInfoText;
        private Text fileStatusText;
        
        // 相机控制变量
        private bool isDragging = false;
        private bool isPanning = false;
        private Vector3 lastMousePosition;
        private float cameraDistance = 500f;  // 初始距离更远，便于总览
        private Vector3 cameraRotation = new Vector3(20f, 0f, 0f);
        private Vector3 cameraTarget = Vector3.zero;
        private float mouseSensitivity = 0.5f;  // 旋转速度慢一点
        private float zoomSpeed = 0.5f;  // 缩放速度快一点
        private float panSpeed = 0.005f;  // 平移速度慢一点
        
        private bool isViewerActive = false;
        private bool isExternalManager = false; // 标记是否使用外部点云管理器
        
        /// <summary>
        /// 显示点云查看器
        /// </summary>
        public void ShowPointCloudViewer(string pointCloudFile = "")
        {
            if (!string.IsNullOrEmpty(pointCloudFile))
            {
                selectedPointCloudFile = pointCloudFile;
            }
            
            if (isViewerActive)
            {
                if (originalViewerWindow != null)
                {
                    originalViewerWindow.SetActive(true);
                    return;
                }
            }
            
            StartCoroutine(CreatePointCloudViewer());
        }
        
        /// <summary>
        /// 隐藏点云查看器
        /// </summary>
        public void HidePointCloudViewer()
        {
            if (originalViewerWindow != null)
            {
                originalViewerWindow.SetActive(false);
            }
            isViewerActive = false;
        }
        
        /// <summary>
        /// 关闭点云查看器
        /// </summary>
        public void ClosePointCloudViewer()
        {
            // 清理点云相机
            if (pointCloudCamera != null)
            {
                pointCloudCamera.targetTexture = null;
                DestroyImmediate(pointCloudCamera.gameObject);
                pointCloudCamera = null;
            }
            
            // 清理点云管理器 - 只销毁内部创建的管理器
            if (pointCloudManager != null && !isExternalManager)
            {
                DestroyImmediate(pointCloudManager.gameObject);
                pointCloudManager = null;
            }
            else if (isExternalManager)
            {
                // 外部管理器只取消引用，不销毁
                pointCloudManager = null;
                isExternalManager = false;
                Debug.Log("外部点云管理器已解除关联，但未销毁");
            }
            
            // 清理渲染纹理
            if (renderTexture != null)
            {
                renderTexture.Release();
                DestroyImmediate(renderTexture);
                renderTexture = null;
            }
            
            // 销毁整个查看器窗口（包括遮罩层）
            if (originalViewerWindow != null)
            {
                DestroyImmediate(originalViewerWindow);
                originalViewerWindow = null;
            }
            
            // 重置引用
            viewerWindow = null;
            viewerCanvas = null;
            
            // 重置状态
            isViewerActive = false;
            
            // 确保主相机正常工作
            RestoreMainCamera();
            
            Debug.Log("点云查看器已关闭，主场景已恢复");
        }
        
        /// <summary>
        /// 恢复主相机设置
        /// </summary>
        void RestoreMainCamera()
        {
            // 查找主相机
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // 如果没有主相机，查找第一个相机
                mainCamera = FindObjectOfType<Camera>();
            }
            
            if (mainCamera != null)
            {
                // 确保主相机是激活的
                mainCamera.gameObject.SetActive(true);
                mainCamera.enabled = true;
                
                // 重置相机的渲染纹理
                mainCamera.targetTexture = null;
                
                // 确保相机渲染到屏幕，并包含点云层级
                int allLayers = -1; // 渲染所有层
                mainCamera.cullingMask = allLayers;
                
                // 确保主相机不包含点云层级（只在弹窗中显示）
                if (pointCloudLayerIndex >= 0 && pointCloudLayerIndex < 32)
                {
                    int pointCloudLayerMask = 1 << pointCloudLayerIndex;
                    if ((mainCamera.cullingMask & pointCloudLayerMask) != 0)
                    {
                        mainCamera.cullingMask &= ~pointCloudLayerMask;
                        Debug.Log($"主相机culling mask已更新，排除点云层级 {pointCloudLayerIndex}");
                    }
                }
                
                Debug.Log($"主相机已恢复: {mainCamera.name}, culling mask: {mainCamera.cullingMask}");
            }
            else
            {
                Debug.LogWarning("未找到主相机，可能需要手动检查场景设置");
            }
        }
        
        IEnumerator CreatePointCloudViewer()
        {
            yield return new WaitForEndOfFrame();
            
            CreateViewerWindow();
            CreateMainBackground();
            CreateTitleBar();
            CreateRenderArea();
            CreatePointCloudCamera();
            CreatePointCloudManager();
            CreateControlPanel();
            CreateStatsPanel();
            
            isViewerActive = true;
            

        }
        
        void CreateViewerWindow()
        {
            // 创建主窗口
            originalViewerWindow = new GameObject("PointCloudViewerWindow");
            originalViewerWindow.layer = LayerMask.NameToLayer("UI");
            
            // 创建Canvas
            viewerCanvas = originalViewerWindow.AddComponent<Canvas>();
            viewerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            viewerCanvas.sortingOrder = 1000;
            
            // 添加Canvas Scaler
            var canvasScaler = originalViewerWindow.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            // 添加GraphicRaycaster
            originalViewerWindow.AddComponent<GraphicRaycaster>();
            
            Debug.Log("点云查看器v3.1美化版窗口已创建");
        }
        
        void CreateMainBackground()
        {
            // 创建全屏半透明遮罩
            var overlay = new GameObject("ScreenOverlay");
            overlay.transform.SetParent(originalViewerWindow.transform, false);
            
            var overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.6f);
            
            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            
            // 创建统一的弹窗主容器
            var modalContainer = new GameObject("ModalContainer");
            modalContainer.transform.SetParent(originalViewerWindow.transform, false);
            
            var containerImage = modalContainer.AddComponent<Image>();
            containerImage.color = new Color(0.12f, 0.12f, 0.16f, 0.98f);
            
            var containerRect = modalContainer.GetComponent<RectTransform>();
            // 更小的弹窗，更现代的比例
            containerRect.anchorMin = new Vector2(0.08f, 0.08f);
            containerRect.anchorMax = new Vector2(0.92f, 0.92f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            
            // 添加现代化阴影效果
            var containerShadow = modalContainer.AddComponent<Shadow>();
            containerShadow.effectColor = new Color(0, 0, 0, 0.6f);
            containerShadow.effectDistance = new Vector2(8, -8);
            
            // 添加边框光效
            var containerOutline = modalContainer.AddComponent<Outline>();
            containerOutline.effectColor = new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.4f);
            containerOutline.effectDistance = new Vector2(2, 2);
            
            // 将后续所有组件的父对象改为 modalContainer
            viewerWindow = modalContainer;
            
            // 添加内部渐变效果
            var gradientOverlay = new GameObject("GradientOverlay");
            gradientOverlay.transform.SetParent(viewerWindow.transform, false);
            
            var gradientImage = gradientOverlay.AddComponent<Image>();
            gradientImage.color = new Color(0.05f, 0.1f, 0.2f, 0.3f);
            
            var gradientRect = gradientOverlay.GetComponent<RectTransform>();
            gradientRect.anchorMin = Vector2.zero;
            gradientRect.anchorMax = Vector2.one;
            gradientRect.offsetMin = Vector2.zero;
            gradientRect.offsetMax = Vector2.zero;
        }
        
        void CreateTitleBar()
        {
            titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(viewerWindow.transform, false);
            
            var titleBarImage = titleBar.AddComponent<Image>();
            titleBarImage.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            
            var titleBarRect = titleBar.GetComponent<RectTransform>();
            titleBarRect.anchorMin = new Vector2(0f, 0.92f);
            titleBarRect.anchorMax = new Vector2(1f, 1f);
            titleBarRect.offsetMin = Vector2.zero;
            titleBarRect.offsetMax = Vector2.zero;
            
            // 添加底部分隔线
            var separator = new GameObject("TitleSeparator");
            separator.transform.SetParent(titleBar.transform, false);
            
            var sepImage = separator.AddComponent<Image>();
            sepImage.color = new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.6f);
            
            var sepRect = separator.GetComponent<RectTransform>();
            sepRect.anchorMin = new Vector2(0f, 0f);
            sepRect.anchorMax = new Vector2(1f, 0.05f);
            sepRect.offsetMin = Vector2.zero;
            sepRect.offsetMax = Vector2.zero;
            
            // 标题图标 - 增强视觉效果
            var iconObj = new GameObject("TitleIcon");
            iconObj.transform.SetParent(titleBar.transform, false);
            
            var iconText = iconObj.AddComponent<Text>();
            iconText.text = "PC";
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconText.fontSize = 20;
            iconText.color = primaryColor;
            iconText.alignment = TextAnchor.MiddleCenter;
            
            // 为图标添加光晕效果
            var iconShadow = iconObj.AddComponent<Shadow>();
            iconShadow.effectColor = new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.4f);
            iconShadow.effectDistance = new Vector2(1, -1);
            
            var iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.015f, 0.1f);
            iconRect.anchorMax = new Vector2(0.065f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            
            // 标题文本
            var titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(titleBar.transform, false);
            
            titleText = titleObj.AddComponent<Text>();
            titleText.text = viewerTitle;
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 22;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.fontStyle = FontStyle.Bold;
            
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.08f, 0.1f);
            titleRect.anchorMax = new Vector2(0.6f, 0.9f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            

            
            // 相机信息显示
            var cameraInfoObj = new GameObject("CameraInfo");
            cameraInfoObj.transform.SetParent(titleBar.transform, false);
            
            cameraInfoText = cameraInfoObj.AddComponent<Text>();
            cameraInfoText.text = "距离:500 | ∠:(20°,0°) | FOV:60°";
            cameraInfoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cameraInfoText.fontSize = 13;
            cameraInfoText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            cameraInfoText.alignment = TextAnchor.MiddleCenter;
            
            var cameraInfoRect = cameraInfoObj.GetComponent<RectTransform>();
            cameraInfoRect.anchorMin = new Vector2(0.6f, 0.1f);
            cameraInfoRect.anchorMax = new Vector2(0.9f, 0.9f);
            cameraInfoRect.offsetMin = Vector2.zero;
            cameraInfoRect.offsetMax = Vector2.zero;
            
            // 关闭按钮
            CreateCloseButton();
        }
        
        void CreateCloseButton()
        {
            var closeButtonObj = new GameObject("CloseButton");
            closeButtonObj.transform.SetParent(titleBar.transform, false);
            
            closeButton = closeButtonObj.AddComponent<Button>();
            var closeButtonImage = closeButtonObj.AddComponent<Image>();
            closeButtonImage.color = new Color(dangerColor.r, dangerColor.g, dangerColor.b, 0.8f);
            
            var closeButtonRect = closeButtonObj.GetComponent<RectTransform>();
            closeButtonRect.anchorMin = new Vector2(0.92f, 0.15f);
            closeButtonRect.anchorMax = new Vector2(0.98f, 0.85f);
            closeButtonRect.offsetMin = Vector2.zero;
            closeButtonRect.offsetMax = Vector2.zero;
            
            // 添加悬停效果的边框
            var hoverBorder = closeButtonObj.AddComponent<Outline>();
            hoverBorder.effectColor = new Color(1f, 1f, 1f, 0.2f);
            hoverBorder.effectDistance = new Vector2(1, 1);
            
            // 关闭图标
            var closeIconObj = new GameObject("CloseIcon");
            closeIconObj.transform.SetParent(closeButtonObj.transform, false);
            
            var closeIconText = closeIconObj.AddComponent<Text>();
            closeIconText.text = "✕";
            closeIconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeIconText.fontSize = 20;
            closeIconText.color = Color.white;
            closeIconText.alignment = TextAnchor.MiddleCenter;
            closeIconText.fontStyle = FontStyle.Bold;
            
            var closeIconRect = closeIconObj.GetComponent<RectTransform>();
            closeIconRect.anchorMin = Vector2.zero;
            closeIconRect.anchorMax = Vector2.one;
            closeIconRect.offsetMin = Vector2.zero;
            closeIconRect.offsetMax = Vector2.zero;
            
            closeButton.onClick.AddListener(ClosePointCloudViewer);
        }
        
        void CreateRenderArea()
        {
            renderArea = new GameObject("RenderArea");
            renderArea.transform.SetParent(viewerWindow.transform, false);
            
            var renderAreaImage = renderArea.AddComponent<Image>();
            renderAreaImage.color = new Color(0.06f, 0.06f, 0.09f, 1f);
            
            var renderAreaRect = renderArea.GetComponent<RectTransform>();
            renderAreaRect.anchorMin = new Vector2(0.24f, 0.15f);
            renderAreaRect.anchorMax = new Vector2(0.98f, 0.92f);
            renderAreaRect.offsetMin = Vector2.zero;
            renderAreaRect.offsetMax = Vector2.zero;
            
            // 添加内边框
            var innerBorder = new GameObject("InnerBorder");
            innerBorder.transform.SetParent(renderArea.transform, false);
            
            var borderImage = innerBorder.AddComponent<Image>();
            borderImage.color = Color.clear;
            
            var borderRect = innerBorder.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            
            // 添加边框效果
            var borderOutline = innerBorder.AddComponent<Outline>();
            borderOutline.effectColor = new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.5f);
            borderOutline.effectDistance = new Vector2(1, 1);
            
            // 渲染纹理显示
            var displayObj = new GameObject("PointCloudDisplay");
            displayObj.transform.SetParent(renderArea.transform, false);
            
            var rawImage = displayObj.AddComponent<RawImage>();
            
            var displayRect = displayObj.GetComponent<RectTransform>();
            displayRect.anchorMin = new Vector2(0.01f, 0.01f);
            displayRect.anchorMax = new Vector2(0.99f, 0.99f);
            displayRect.offsetMin = Vector2.zero;
            displayRect.offsetMax = Vector2.zero;
            
            // 创建渲染纹理
            renderTexture = new RenderTexture(1280, 720, 24);
            renderTexture.Create();
            rawImage.texture = renderTexture;
            
            // 添加操作提示
            CreateRenderAreaHints();
        }
        
        void CreateRenderAreaHints()
        {
            var hintsObj = new GameObject("ControlHints");
            hintsObj.transform.SetParent(renderArea.transform, false);
            
            var hintsText = hintsObj.AddComponent<Text>();
            hintsText.text = "左键:旋转  右键:平移  滚轮:缩放";
            hintsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hintsText.fontSize = 14;
            hintsText.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
            hintsText.alignment = TextAnchor.LowerCenter;
            
            var hintsRect = hintsObj.GetComponent<RectTransform>();
            hintsRect.anchorMin = new Vector2(0.1f, 0.02f);
            hintsRect.anchorMax = new Vector2(0.9f, 0.08f);
            hintsRect.offsetMin = Vector2.zero;
            hintsRect.offsetMax = Vector2.zero;
        }
        
        void CreatePointCloudCamera()
        {
            var cameraObj = new GameObject("PointCloudCamera");
            pointCloudCamera = cameraObj.AddComponent<Camera>();
            
            // 设置相机参数 - 优化背景色
            pointCloudCamera.clearFlags = CameraClearFlags.SolidColor;
            pointCloudCamera.backgroundColor = new Color(0.18f, 0.22f, 0.28f, 1f);
            pointCloudCamera.cullingMask = 1 << pointCloudLayerIndex;
            pointCloudCamera.targetTexture = renderTexture;
            
            // 根据是否在EXE中调整初始参数
            bool isInExe = !Application.isEditor;
            if (isInExe)
            {
                pointCloudCamera.fieldOfView = 80f; // EXE中使用更大的视野角度
                pointCloudCamera.farClipPlane = 8000f; // EXE中使用更大的远裁剪平面
            }
            else
            {
                pointCloudCamera.fieldOfView = 75f; // 编辑器中使用标准视野角度
                pointCloudCamera.farClipPlane = 5000f; // 编辑器中使用标准远裁剪平面
            }
            
            pointCloudCamera.nearClipPlane = 0.01f; // 减小近裁剪平面
            
            // 设置初始相机位置
            UpdateCameraPosition();
            
            // 添加点大小支持
            PowerlinePointSizeEnabler.SetupDedicatedPointCloudCamera(pointCloudCamera);
            
            Debug.Log($"点云专用相机已创建，层级：{pointCloudLayerIndex}，FOV：{pointCloudCamera.fieldOfView}°，远裁剪面：{pointCloudCamera.farClipPlane}");
        }
        
        void CreatePointCloudManager()
        {
            // 检查是否已经有点云管理器
            var existingManagers = FindObjectsOfType<PowerlinePointCloudManager>();
            PowerlinePointCloudManager integratedManager = null;
            
            if (existingManagers.Length > 0)
            {
                integratedManager = existingManagers[0];
                Debug.Log("找到现有的点云管理器，将使用它");
            }
            
            if (integratedManager != null)
            {
                // 使用现有的管理器
                pointCloudManager = integratedManager;
                
                // 更新数据路径以使用查看器的文件
                pointCloudManager.dataPath = "pointcloud/" + selectedPointCloudFile;
                Debug.Log($"使用现有的点云管理器，数据路径: {pointCloudManager.dataPath}");
                
                // 标记这是外部管理器，关闭时不销毁
                isExternalManager = true;
                
                // 设置管理器的相机为查看器相机
                // (已移除集成组件依赖)
            }
            else
            {
                // 创建新的点云管理器（原有逻辑）
                var managerObj = new GameObject("PointCloudManager");
                pointCloudManager = managerObj.AddComponent<PowerlinePointCloudManager>();
                
                // 设置点云管理器参数
                pointCloudManager.dataPath = "pointcloud/" + selectedPointCloudFile;
                pointCloudManager.scale = 1f;
                pointCloudManager.autoFitToPowerlines = false;
                pointCloudManager.showLoadingProgress = false;
                
                // 标记这是内部管理器，关闭时需要销毁
                isExternalManager = false;
                
                Debug.Log($"创建了新的点云管理器，数据路径: {pointCloudManager.dataPath}");
            }
            
            // 订阅事件
            pointCloudManager.OnLoadingProgress += OnPointCloudLoadingProgress;
            pointCloudManager.OnLoadingComplete += OnPointCloudLoadingComplete;
            pointCloudManager.OnLoadingError += OnPointCloudLoadingError;
            
            // 立即设置点云管理器的层级
            pointCloudManager.gameObject.layer = pointCloudLayerIndex;
            Debug.Log($"点云管理器层级已设置为: {pointCloudLayerIndex}");
            
            // 开始加载点云
            Debug.Log($"开始加载点云: {pointCloudManager.dataPath}");
            pointCloudManager.LoadPointCloudAsync();
            
            // 延迟设置点云层级
            StartCoroutine(SetPointCloudLayerDelayed());
        }
        
        void CreateControlPanel()
        {
            controlPanel = new GameObject("ControlPanel");
            controlPanel.transform.SetParent(viewerWindow.transform, false);
            
            var panelImage = controlPanel.AddComponent<Image>();
            panelImage.color = new Color(0.10f, 0.10f, 0.14f, 0.95f);
            
            var panelRect = controlPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.02f, 0.02f);
            panelRect.anchorMax = new Vector2(0.22f, 0.92f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            // 添加右侧分隔线
            var rightSeparator = new GameObject("RightSeparator");
            rightSeparator.transform.SetParent(controlPanel.transform, false);
            
            var rightSepImage = rightSeparator.AddComponent<Image>();
            rightSepImage.color = new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.4f);
            
            var rightSepRect = rightSeparator.GetComponent<RectTransform>();
            rightSepRect.anchorMin = new Vector2(0.98f, 0f);
            rightSepRect.anchorMax = new Vector2(1f, 1f);
            rightSepRect.offsetMin = Vector2.zero;
            rightSepRect.offsetMax = Vector2.zero;
            
            CreateControlPanelContent();
        }
        
        void CreateControlPanelContent()
        {
            float yPos = 0.95f;
            
            // 当前文件信息
            CreateFileInfo(yPos);
            yPos -= 0.12f;
            
            // 分隔线
            CreateSeparator(controlPanel, new Vector2(0.05f, yPos), new Vector2(0.95f, yPos + 0.01f));
            yPos -= 0.05f;
            
            // 相机控制
            CreateCameraControls(yPos);
            yPos -= 0.18f;
            
            // 状态显示
            CreateStatusDisplay(yPos);
        }
        
        void CreateFileInfo(float yPos)
        {
            // 文件名显示
            var fileNameObj = new GameObject("FileName");
            fileNameObj.transform.SetParent(controlPanel.transform, false);
            
            var fileNameText = fileNameObj.AddComponent<Text>();
            fileNameText.text = selectedPointCloudFile;
            fileNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fileNameText.fontSize = 16;
            fileNameText.color = primaryColor;
            fileNameText.alignment = TextAnchor.MiddleCenter;
            fileNameText.fontStyle = FontStyle.Bold;
            
            var fileNameRect = fileNameObj.GetComponent<RectTransform>();
            fileNameRect.anchorMin = new Vector2(0.05f, yPos - 0.06f);
            fileNameRect.anchorMax = new Vector2(0.95f, yPos);
            fileNameRect.offsetMin = Vector2.zero;
            fileNameRect.offsetMax = Vector2.zero;
            
            // 文件状态
            var statusObj = new GameObject("FileStatus");
            statusObj.transform.SetParent(controlPanel.transform, false);
            
            fileStatusText = statusObj.AddComponent<Text>();
            fileStatusText.text = "准备加载";
            fileStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fileStatusText.fontSize = 12;
            fileStatusText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            fileStatusText.alignment = TextAnchor.MiddleCenter;
            
            var statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.05f, yPos - 0.1f);
            statusRect.anchorMax = new Vector2(0.95f, yPos - 0.06f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;
        }
        

        

        

        
        void CreateCameraControls(float yPos)
        {
            // 相机控制标题
            var titleObj = new GameObject("CameraTitle");
            titleObj.transform.SetParent(controlPanel.transform, false);
            
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "相机控制";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 18;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.fontStyle = FontStyle.Bold;
            
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.08f, yPos - 0.05f);
            titleRect.anchorMax = new Vector2(0.92f, yPos);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            
            // 重置按钮
            var resetButtonObj = new GameObject("ResetButton");
            resetButtonObj.transform.SetParent(controlPanel.transform, false);
            
            resetCameraButton = resetButtonObj.AddComponent<Button>();
            var resetButtonImage = resetButtonObj.AddComponent<Image>();
            resetButtonImage.color = accentColor;
            
            var resetButtonRect = resetButtonObj.GetComponent<RectTransform>();
            resetButtonRect.anchorMin = new Vector2(0.06f, yPos - 0.14f);
            resetButtonRect.anchorMax = new Vector2(0.94f, yPos - 0.06f);
            resetButtonRect.offsetMin = Vector2.zero;
            resetButtonRect.offsetMax = Vector2.zero;
            
            // 添加增强的按钮阴影效果
            var resetButtonShadow = resetButtonObj.AddComponent<Shadow>();
            resetButtonShadow.effectColor = new Color(0, 0, 0, 0.4f);
            resetButtonShadow.effectDistance = new Vector2(3, -3);
            
            // 添加按钮边框光效
            var resetButtonOutline = resetButtonObj.AddComponent<Outline>();
            resetButtonOutline.effectColor = new Color(1f, 1f, 1f, 0.3f);
            resetButtonOutline.effectDistance = new Vector2(1, 1);
            
            // 重置按钮文本
            var resetTextObj = new GameObject("ResetText");
            resetTextObj.transform.SetParent(resetButtonObj.transform, false);
            
            var resetText = resetTextObj.AddComponent<Text>();
            resetText.text = "重置视角";
            resetText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            resetText.fontSize = 16;
            resetText.color = Color.white;
            resetText.alignment = TextAnchor.MiddleCenter;
            resetText.fontStyle = FontStyle.Bold;
            
            // 为重置按钮文字添加阴影
            var resetTextShadow = resetTextObj.AddComponent<Shadow>();
            resetTextShadow.effectColor = new Color(0, 0, 0, 0.5f);
            resetTextShadow.effectDistance = new Vector2(1, -1);
            
            var resetTextRect = resetTextObj.GetComponent<RectTransform>();
            resetTextRect.anchorMin = Vector2.zero;
            resetTextRect.anchorMax = Vector2.one;
            resetTextRect.offsetMin = Vector2.zero;
            resetTextRect.offsetMax = Vector2.zero;
            
            resetCameraButton.onClick.AddListener(ResetCamera);
        }
        
        void CreateStatusDisplay(float yPos)
        {
            // 状态标题
            var titleObj = new GameObject("StatusTitle");
            titleObj.transform.SetParent(controlPanel.transform, false);
            
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "状态信息";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 18;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.fontStyle = FontStyle.Bold;
            
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.08f, yPos - 0.05f);
            titleRect.anchorMax = new Vector2(0.92f, yPos);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            
            // 状态容器 - 现代化样式
            var containerObj = new GameObject("StatusContainer");
            containerObj.transform.SetParent(controlPanel.transform, false);
            
            var containerImage = containerObj.AddComponent<Image>();
            containerImage.color = new Color(0.06f, 0.06f, 0.09f, 0.8f);
            
            var containerRect = containerObj.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.06f, 0.05f);
            containerRect.anchorMax = new Vector2(0.94f, yPos - 0.08f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            
            // 为状态容器添加微妙边框
            var containerOutline = containerObj.AddComponent<Outline>();
            containerOutline.effectColor = new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.3f);
            containerOutline.effectDistance = new Vector2(1, 1);
            
            // 状态文本
            var statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(containerObj.transform, false);
            
            statusText = statusObj.AddComponent<Text>();
            statusText.text = "点云查看器已就绪\n\n统计信息:\n• 点数: 待加载\n• 内存: 待计算\n• 渲染: 待开始";
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 13;
            statusText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            statusText.alignment = TextAnchor.UpperLeft;
            
            var statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.05f, 0.05f);
            statusRect.anchorMax = new Vector2(0.95f, 0.95f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;
        }
        
        void CreateSeparator(GameObject parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var separatorObj = new GameObject("Separator");
            separatorObj.transform.SetParent(parent.transform, false);
            
            var separatorImage = separatorObj.AddComponent<Image>();
            separatorImage.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            
            var separatorRect = separatorObj.GetComponent<RectTransform>();
            separatorRect.anchorMin = anchorMin;
            separatorRect.anchorMax = anchorMax;
            separatorRect.offsetMin = Vector2.zero;
            separatorRect.offsetMax = Vector2.zero;
        }
        
        void CreateStatsPanel()
        {
            statsPanel = new GameObject("StatsPanel");
            statsPanel.transform.SetParent(viewerWindow.transform, false);
            
            var statsPanelImage = statsPanel.AddComponent<Image>();
            statsPanelImage.color = new Color(0.10f, 0.10f, 0.14f, 0.95f);
            
            var statsPanelRect = statsPanel.GetComponent<RectTransform>();
            statsPanelRect.anchorMin = new Vector2(0.24f, 0.02f);
            statsPanelRect.anchorMax = new Vector2(0.98f, 0.13f);
            statsPanelRect.offsetMin = Vector2.zero;
            statsPanelRect.offsetMax = Vector2.zero;
            
            // 添加顶部分隔线
            var topSeparator = new GameObject("TopSeparator");
            topSeparator.transform.SetParent(statsPanel.transform, false);
            
            var topSepImage = topSeparator.AddComponent<Image>();
            topSepImage.color = new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.6f);
            
            var topSepRect = topSeparator.GetComponent<RectTransform>();
            topSepRect.anchorMin = new Vector2(0f, 0.95f);
            topSepRect.anchorMax = new Vector2(1f, 1f);
            topSepRect.offsetMin = Vector2.zero;
            topSepRect.offsetMax = Vector2.zero;
            
            // 统计标题
            var titleObj = new GameObject("StatsTitle");
            titleObj.transform.SetParent(statsPanel.transform, false);
            
            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "点云统计信息";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 18;
            titleText.color = primaryColor;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.fontStyle = FontStyle.Bold;
            
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.02f, 0.65f);
            titleRect.anchorMax = new Vector2(0.5f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            
            // 统计文本
            var statsObj = new GameObject("StatsText");
            statsObj.transform.SetParent(statsPanel.transform, false);
            
            statsText = statsObj.AddComponent<Text>();
            statsText.text = "暂无数据 - 请先加载点云";
            statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statsText.fontSize = 14;
            statsText.color = Color.white;
            statsText.alignment = TextAnchor.UpperLeft;
            
            var statsRect = statsObj.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.02f, 0.05f);
            statsRect.anchorMax = new Vector2(0.98f, 0.6f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;
        }
        

        

        
        void ResetCamera()
        {
            cameraDistance = 500f;  // 与初始距离保持一致
            cameraRotation = new Vector3(20f, 0f, 0f);
            cameraTarget = Vector3.zero;
            UpdateCameraPosition();
            UpdateCameraInfo();
            
            if (statusText != null)
            {
                statusText.text = "相机视角已重置\n\n统计信息:\n• 点数: 待加载\n• 内存: 待计算\n• 渲染: 待开始";
            }
        }
        
        void OnPointCloudLoadingProgress(float progress)
        {
            if (statusText != null)
            {
                statusText.text = $"加载中... {(progress * 100):F0}%\n正在处理点云数据...\n\n统计信息:\n• 进度: {(progress * 100):F1}%\n• 状态: 读取中\n• 内存: 分配中";
            }
        }
        
        void OnPointCloudLoadingComplete()
        {
            // 延迟调整相机位置，确保点云对象已完全创建
            StartCoroutine(DelayedCameraAdjustment());
            
            // 更新统计信息
            UpdateStatsDisplay();
            
            if (statusText != null && pointCloudManager != null)
            {
                var stats = pointCloudManager.GetPointCloudStats();
                statusText.text = $"点云加载完成！\n\n统计信息:\n• 点数: {stats.totalPoints:N0}\n• 网格: {stats.totalMeshGroups}\n• 状态: 已就绪";
                
                if (fileStatusText != null)
                {
                    fileStatusText.text = "已加载";
                    fileStatusText.color = accentColor;
                }
            }
        }
        
        IEnumerator DelayedCameraAdjustment()
        {
            Debug.Log("开始延迟相机调整...");
            
            // 减少等待时间，提高响应速度
            yield return new WaitForSeconds(0.2f);
            
            // 检查点云管理器状态
            if (pointCloudManager == null)
            {
                Debug.LogError("点云管理器为空，无法进行相机调整");
                yield break;
            }
            
            // 等待点云完全加载 - 减少等待时间
            int maxWaitFrames = 15; // 减少到15帧
            int currentFrame = 0;
            while (currentFrame < maxWaitFrames)
            {
                var stats = pointCloudManager.GetPointCloudStats();
                if (stats.isLoaded && stats.totalPoints > 0)
                {
                    Debug.Log($"点云已完全加载: {stats.totalPoints} 个点");
                    break;
                }
                currentFrame++;
                yield return new WaitForEndOfFrame();
            }
            
            if (currentFrame >= maxWaitFrames)
            {
                Debug.LogWarning("等待点云加载超时，继续执行相机调整");
            }
            
            // 强制设置点云层级
            SetPointCloudObjectsLayer();
            
            // 等待层级设置生效
            yield return new WaitForEndOfFrame();
            
            // 再次检查点云对象是否存在
            if (pointCloudManager != null)
            {
                MeshRenderer[] renderers = pointCloudManager.GetComponentsInChildren<MeshRenderer>();
                Debug.Log($"找到 {renderers.Length} 个点云网格对象");
                
                // 确保所有渲染器都设置了正确的层级
                foreach (var renderer in renderers)
                {
                    if (renderer.gameObject.layer != pointCloudLayerIndex)
                    {
                        renderer.gameObject.layer = pointCloudLayerIndex;
                        Debug.Log($"修正网格对象层级: {renderer.gameObject.name}");
                    }
                }
            }
            
            // 调整相机位置
            AdjustCameraToPointCloud();
            
            // 强制刷新相机
            if (pointCloudCamera != null)
            {
                pointCloudCamera.enabled = false;
                yield return new WaitForEndOfFrame();
                pointCloudCamera.enabled = true;
                
                // 重新设置相机的culling mask，确保点云可见
                pointCloudCamera.cullingMask = 1 << pointCloudLayerIndex;
                
                Debug.Log("点云加载完成，相机已刷新，culling mask已重新设置");
            }
            
            // 延迟验证点云可见性
            yield return new WaitForSeconds(0.5f);
            ValidatePointCloudVisibility();
            
            // 最后再次检查并设置层级
            yield return new WaitForEndOfFrame();
            if (pointCloudManager != null)
            {
                SetPointCloudObjectsLayer();
                Debug.Log("最终层级设置完成");
            }
            
            // 自动执行重置视角
            ResetCamera();
            Debug.Log("点云加载完成，已自动重置视角");
        }
        
        void OnPointCloudLoadingError(string error)
        {
            if (statusText != null)
            {
                statusText.text = $"加载失败！\n错误: {error}\n\n统计信息:\n• 点数: 0\n• 状态: 错误\n• 文件: 未找到";
            }
            
            if (fileStatusText != null)
            {
                fileStatusText.text = "加载失败";
                fileStatusText.color = dangerColor;
            }
        }
        
        void UpdateStatsDisplay()
        {
            if (statsText == null || pointCloudManager == null) return;
            
            var stats = pointCloudManager.GetPointCloudStats();
            
            string statsInfo = $"总点数: {stats.totalPoints:N0}   网格组: {stats.totalMeshGroups}   渲染点数: {stats.renderedPoints:N0}\n";
            statsInfo += $"状态: {(stats.isLoaded ? "已加载" : stats.isLoading ? "加载中" : "未加载")}";
            
            if (stats.isLoaded)
            {
                statsInfo += $"   边界大小: {(stats.boundsMax - stats.boundsMin).magnitude:F1}";
                statsInfo += $"   中心点: ({stats.boundsCenter.x:F1}, {stats.boundsCenter.y:F1}, {stats.boundsCenter.z:F1})";
            }
            
            statsText.text = statsInfo;
        }
        
        void AdjustCameraToPointCloud()
        {
            if (pointCloudManager != null)
            {
                var stats = pointCloudManager.GetPointCloudStats();
                if (stats.isLoaded)
                {
                    // 计算点云边界和大小
                    Vector3 boundsSize = stats.boundsMax - stats.boundsMin;
                    float boundSize = boundsSize.magnitude;
                    float maxDimension = Mathf.Max(boundsSize.x, boundsSize.y, boundsSize.z);
                    
                    Debug.Log($"点云边界大小: {boundsSize}, 最大维度: {maxDimension}, 总大小: {boundSize}");
                    
                    // 根据点云大小动态调整相机参数
                    cameraTarget = stats.boundsCenter;
                    
                    // 动态计算相机距离
                    float baseDistance = Mathf.Max(maxDimension * 1.5f, 100f);
                    cameraDistance = Mathf.Clamp(baseDistance, 50f, 3000f);
                    
                    // 动态调整FOV和远裁剪平面
                    if (pointCloudCamera != null)
                    {
                        float targetFOV;
                        if (maxDimension > 1000f)
                        {
                            targetFOV = 80f; // 大型点云使用更大的FOV
                        }
                        else if (maxDimension > 500f)
                        {
                            targetFOV = 75f; // 中型点云
                        }
                        else if (maxDimension > 200f)
                        {
                            targetFOV = 70f; // 中小型点云
                        }
                        else
                        {
                            targetFOV = 65f; // 小型点云
                        }
                        
                        // 在EXE中稍微增大FOV
                        bool isInExe = !Application.isEditor;
                        if (isInExe)
                        {
                            targetFOV = Mathf.Min(targetFOV + 5f, 85f);
                        }
                        
                        pointCloudCamera.fieldOfView = targetFOV;
                        
                        // 动态调整远裁剪平面
                        float farClipPlane = Mathf.Max(cameraDistance * 3f, 1000f);
                        pointCloudCamera.farClipPlane = farClipPlane;
                        
                        Debug.Log($"相机参数调整: FOV={targetFOV}°, 远裁剪面={farClipPlane}, 距离={cameraDistance}");
                    }
                    
                    UpdateCameraPosition();
                    UpdateCameraInfo();
                    
                    // 验证点云可见性
                    ValidatePointCloudVisibility();
                    
                    Debug.Log($"相机已调整到点云中心: {cameraTarget}, 距离: {cameraDistance}, 边界大小: {boundSize}");
                }
                else
                {
                    Debug.LogWarning("点云统计信息显示未加载完成，使用默认相机位置");
                    // 使用默认位置
                    cameraTarget = Vector3.zero;
                    cameraDistance = 500f;
                    UpdateCameraPosition();
                }
            }
        }
        
        /// <summary>
        /// 验证点云可见性，检查是否有部分被遮挡
        /// </summary>
        void ValidatePointCloudVisibility()
        {
            if (pointCloudManager == null || pointCloudCamera == null) return;
            
            try
            {
                MeshRenderer[] renderers = pointCloudManager.GetComponentsInChildren<MeshRenderer>();
                int visibleCount = 0;
                int totalCount = renderers.Length;
                
                foreach (var renderer in renderers)
                {
                    if (renderer != null && renderer.enabled)
                    {
                        Vector3 viewportPoint = pointCloudCamera.WorldToViewportPoint(renderer.bounds.center);
                        if (viewportPoint.x >= -0.1f && viewportPoint.x <= 1.1f &&
                            viewportPoint.y >= -0.1f && viewportPoint.y <= 1.1f &&
                            viewportPoint.z > 0f)
                        {
                            visibleCount++;
                        }
                    }
                }
                
                float visibilityRatio = totalCount > 0 ? (float)visibleCount / totalCount : 0f;
                Debug.Log($"点云可见性检查: {visibleCount}/{totalCount} 可见 ({visibilityRatio:P1})");
                
                // 如果可见性太低，尝试调整相机
                if (visibilityRatio < 0.8f && totalCount > 0)
                {
                    Debug.LogWarning($"点云可见性较低 ({visibilityRatio:P1})，尝试调整相机参数");
                    
                    // 增大FOV和远裁剪平面
                    if (pointCloudCamera.fieldOfView < 85f)
                    {
                        pointCloudCamera.fieldOfView = Mathf.Min(pointCloudCamera.fieldOfView + 5f, 85f);
                    }
                    
                    if (pointCloudCamera.farClipPlane < 5000f)
                    {
                        pointCloudCamera.farClipPlane = Mathf.Min(pointCloudCamera.farClipPlane * 1.5f, 5000f);
                    }
                    
                    // 稍微增大相机距离
                    cameraDistance = Mathf.Min(cameraDistance * 1.2f, 3000f);
                    UpdateCameraPosition();
                    
                    Debug.Log($"已调整相机参数以提高可见性: FOV={pointCloudCamera.fieldOfView}°, 远裁剪面={pointCloudCamera.farClipPlane}, 距离={cameraDistance}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"验证点云可见性时出错: {e.Message}");
            }
        }
        
        void UpdateCameraPosition()
        {
            if (pointCloudCamera != null)
            {
                Vector3 rotation = cameraRotation;
                Quaternion quaternionRotation = Quaternion.Euler(rotation.x, rotation.y, rotation.z);
                Vector3 position = cameraTarget + quaternionRotation * Vector3.back * cameraDistance;
                
                pointCloudCamera.transform.position = position;
                pointCloudCamera.transform.rotation = quaternionRotation;
            }
        }
        
        void UpdateCameraInfo()
        {
            if (cameraInfoText != null)
            {
                float fov = pointCloudCamera != null ? pointCloudCamera.fieldOfView : 75f;
                cameraInfoText.text = $"距离:{cameraDistance:F1} | ∠:({cameraRotation.x:F0}°,{cameraRotation.y:F0}°) | FOV:{fov:F0}°";
            }
        }
        
        void Update()
        {
            if (!isViewerActive || pointCloudCamera == null) return;
            
            // 处理相机控制
            HandleCameraControl();
            
            // 定期更新信息
            if (Time.frameCount % 30 == 0)
            {
                UpdateStatsDisplay();
                UpdateCameraInfo();
            }
        }
        
        void HandleCameraControl()
        {
            Vector2 mousePos = Input.mousePosition;
            bool mouseInRenderArea = RectTransformUtility.RectangleContainsScreenPoint(
                renderArea.GetComponent<RectTransform>(), mousePos);
            
            if (!mouseInRenderArea) return;
            
            // 左键拖拽 - 旋转
            if (Input.GetMouseButtonDown(0))
            {
                isDragging = true;
                lastMousePosition = Input.mousePosition;
            }
            
            // 右键拖拽 - 平移
            if (Input.GetMouseButtonDown(1))
            {
                isPanning = true;
                lastMousePosition = Input.mousePosition;
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }
            
            if (Input.GetMouseButtonUp(1))
            {
                isPanning = false;
            }
            
            // 旋转控制
            if (isDragging)
            {
                Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
                cameraRotation.y += mouseDelta.x * mouseSensitivity * 0.5f;
                cameraRotation.x -= mouseDelta.y * mouseSensitivity * 0.5f;
                cameraRotation.x = Mathf.Clamp(cameraRotation.x, -89f, 89f);
                
                UpdateCameraPosition();
                lastMousePosition = Input.mousePosition;
            }
            
            // 平移控制 - 修复方向问题
            if (isPanning)
            {
                Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
                
                // 将鼠标移动转换为世界空间的平移
                Vector3 right = pointCloudCamera.transform.right;
                Vector3 up = pointCloudCamera.transform.up;
                
                float panAmount = cameraDistance * panSpeed;
                // 修复平移方向：鼠标向右时点云向右移动，鼠标向上时点云向上移动
                Vector3 panOffset = 0.2f * (-right * mouseDelta.x - up * mouseDelta.y) * panAmount;
                
                cameraTarget += panOffset;
                UpdateCameraPosition();
                lastMousePosition = Input.mousePosition;
            }
            
            // 滚轮缩放
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                cameraDistance -= scroll * cameraDistance * zoomSpeed;
                cameraDistance = Mathf.Clamp(cameraDistance, 10f, 5000f);  // 扩大缩放范围，防止点云消失
                UpdateCameraPosition();
            }
        }
        
        IEnumerator SetPointCloudLayerDelayed()
        {
            yield return new WaitForSeconds(2f); // 增加等待时间
            SetPointCloudObjectsLayer();
            
            // 额外检查和等待点云加载完成
            int maxRetries = 10;
            int retries = 0;
            
            while (retries < maxRetries)
            {
                if (pointCloudManager != null)
                {
                    // 检查是否有点云对象
                    MeshRenderer[] renderers = pointCloudManager.GetComponentsInChildren<MeshRenderer>();
                    if (renderers.Length > 0)
                    {
                        Debug.Log($"找到 {renderers.Length} 个点云网格，设置层级");
                        SetPointCloudObjectsLayer();
                        break;
                    }
                }
                
                retries++;
                yield return new WaitForSeconds(1f);
            }
            
            // 最后强制刷新一次相机
            if (pointCloudCamera != null)
            {
                pointCloudCamera.enabled = false;
                yield return new WaitForEndOfFrame();
                pointCloudCamera.enabled = true;
                
                // 重新设置相机的culling mask，确保点云可见
                pointCloudCamera.cullingMask = 1 << pointCloudLayerIndex;
                
                Debug.Log("强制刷新点云相机，culling mask已重新设置");
            }
        }
        
        void SetPointCloudObjectsLayer()
        {
            if (pointCloudManager != null)
            {
                SetLayerRecursively(pointCloudManager.gameObject, pointCloudLayerIndex);
                Debug.Log($"点云对象层级已设置为: {pointCloudLayerIndex}");
                
                // 确保所有子对象都设置了正确的层级
                MeshRenderer[] renderers = pointCloudManager.GetComponentsInChildren<MeshRenderer>();
                int correctedCount = 0;
                foreach (var renderer in renderers)
                {
                    if (renderer.gameObject.layer != pointCloudLayerIndex)
                    {
                        renderer.gameObject.layer = pointCloudLayerIndex;
                        correctedCount++;
                    }
                    // 确保渲染器是激活的
                    renderer.enabled = true;
                }
                
                Debug.Log($"已设置 {renderers.Length} 个网格渲染器的层级，修正了 {correctedCount} 个");
                
                // 如果使用外部管理器且已有点云数据，强制刷新层级
                if (isExternalManager)
                {
                    StartCoroutine(RefreshExternalPointCloudLayers());
                }
                
                // 确保相机能看到点云
                if (pointCloudCamera != null)
                {
                    pointCloudCamera.cullingMask = 1 << pointCloudLayerIndex;
                    Debug.Log($"相机culling mask已设置为: {pointCloudCamera.cullingMask}");
                }
            }
            else
            {
                Debug.LogWarning("点云管理器为空，无法设置层级");
            }
        }
        
        void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
        
        IEnumerator RefreshExternalPointCloudLayers()
        {
            // 等待一帧确保所有点云对象都已创建
            yield return new WaitForEndOfFrame();
            
            if (pointCloudManager != null)
            {
                // 查找所有点云网格对象
                MeshRenderer[] renderers = pointCloudManager.GetComponentsInChildren<MeshRenderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer.gameObject.name.Contains("PointCloudMesh") || 
                        renderer.gameObject.name.Contains("PointCloud"))
                    {
                        renderer.gameObject.layer = pointCloudLayerIndex;
                    }
                }
                
                Debug.Log($"已刷新外部点云管理器的 {renderers.Length} 个网格对象层级");
            }
        }
        
        void OnDestroy()
        {
            // 确保在销毁时清理所有资源
            if (originalViewerWindow != null)
            {
                DestroyImmediate(originalViewerWindow);
            }
            ClosePointCloudViewer();
        }
    }
} 