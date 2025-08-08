using UnityEngine;
using PowerlineSystem;

namespace UI
{
    /// <summary>
    /// 点云自动初始化器
    /// 确保点云管理器在场景中自动创建和配置
    /// </summary>
    public class PointCloudAutoInitializer : MonoBehaviour
    {
        [Header("自动初始化设置")]
        [Tooltip("是否在Start时自动创建点云管理器")]
        public bool autoCreateManager = true;
        
        [Tooltip("点云管理器的GameObject名称")]
        public string pointCloudManagerName = "PowerlinePointCloudManager";
        
        [Header("默认配置")]
        [Tooltip("默认点云数据路径")]
        public string defaultDataPath = "pointcloud/sample";
        
        [Tooltip("默认缩放比例")]
        public float defaultScale = 1f;
        
        [Tooltip("是否自动适配电力线")]
        public bool autoFitToPowerlines = false;
        
        private PowerlinePointCloudManager pointCloudManager;
        private bool isInitialized = false;
        
        void Start()
        {
            Debug.Log($"PointCloudAutoInitializer.Start() - autoCreateManager: {autoCreateManager}");
            if (autoCreateManager)
            {
                InitializePointCloudSystem();
            }
        }
        
        /// <summary>
        /// 初始化点云系统
        /// </summary>
        public void InitializePointCloudSystem()
        {
            if (isInitialized) return;
            
            // 查找现有的点云管理器
            pointCloudManager = FindObjectOfType<PowerlinePointCloudManager>();
            
            if (pointCloudManager == null)
            {
                // 创建新的点云管理器
                CreatePointCloudManager();
            }
            else
            {
                // 配置现有的点云管理器
                ConfigurePointCloudManager(pointCloudManager);
            }
            
            // 确保UI控制器也正确配置
            SetupUIController();
            
            isInitialized = true;
            Debug.Log("点云系统初始化完成");
        }
        
        /// <summary>
        /// 创建新的点云管理器
        /// </summary>
        void CreatePointCloudManager()
        {
            // 创建GameObject
            GameObject managerObj = new GameObject(pointCloudManagerName);
            
            // 添加点云管理器组件
            pointCloudManager = managerObj.AddComponent<PowerlinePointCloudManager>();
            
            // 配置管理器
            ConfigurePointCloudManager(pointCloudManager);
            
            Debug.Log($"已创建点云管理器: {pointCloudManagerName}");
        }
        
        /// <summary>
        /// 配置点云管理器
        /// </summary>
        void ConfigurePointCloudManager(PowerlinePointCloudManager manager)
        {
            if (manager == null) return;
            
            // 设置基本配置
            manager.dataPath = defaultDataPath;
            manager.scale = defaultScale;
            manager.autoFitToPowerlines = autoFitToPowerlines;
            
            // 查找场景初始化器
            var sceneInitializer = FindObjectOfType<SceneInitializer>();
            if (sceneInitializer != null)
            {
                manager.sceneInitializer = sceneInitializer;
                
                // 同时在场景初始化器中设置点云管理器引用
                sceneInitializer.pointCloudManager = manager;
                
                Debug.Log("已将点云管理器连接到场景初始化器");
            }
            
            // 设置性能优化参数
            manager.maxPointsPerMesh = 65000;
            manager.enableLOD = true;
            manager.showLoadingProgress = true;
            manager.enableUIControls = true;
            
            // 设置渲染参数
            manager.opacity = 1f;
            manager.invertYZ = false;
            manager.forceReload = false;
            
            Debug.Log($"点云管理器配置完成 - 数据路径: {manager.dataPath}");
        }
        
        /// <summary>
        /// 设置UI控制器
        /// </summary>
        void SetupUIController()
        {
            // 查找SimpleUIToolkitManager
            var uiManager = FindObjectOfType<SimpleUIToolkitManager>();
            if (uiManager != null)
            {
                // 确保UI管理器上有点云控制器
                var pointCloudController = uiManager.GetComponent<PointCloudUIController>();
                if (pointCloudController == null)
                {
                    pointCloudController = uiManager.gameObject.AddComponent<PointCloudUIController>();
                    Debug.Log("已为UI管理器添加点云控制器");
                }
                
                // 设置点云控制器的管理器引用
                if (pointCloudController.pointCloudManager == null)
                {
                    pointCloudController.pointCloudManager = pointCloudManager;
                    Debug.Log("已连接点云控制器和点云管理器");
                }
            }
            
            // 设置主相机的点云渲染支持
            SetupCameraForPointCloud();
        }
        
        /// <summary>
        /// 为主相机设置点云渲染支持
        /// </summary>
        void SetupCameraForPointCloud()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // 保存原始相机设置
                var originalClearFlags = mainCamera.clearFlags;
                var originalBackgroundColor = mainCamera.backgroundColor;
                var originalSkybox = RenderSettings.skybox;
                
                // 只添加点云渲染支持，不修改相机设置
                var enabler = mainCamera.GetComponent<PowerlinePointSizeEnabler>();
                if (enabler == null)
                {
                    enabler = mainCamera.gameObject.AddComponent<PowerlinePointSizeEnabler>();
                }
                
                // 确保主相机不包含点云层级（只在弹窗中显示）
                int pointCloudLayer = 31;
                int pointCloudLayerMask = 1 << pointCloudLayer;
                
                // 确保culling mask不包含点云层级
                if ((mainCamera.cullingMask & pointCloudLayerMask) != 0)
                {
                    mainCamera.cullingMask &= ~pointCloudLayerMask;
                    Debug.Log($"主相机culling mask已设置，排除点云层级 {pointCloudLayer}");
                }
                
                // 强制恢复原始设置，防止天空变黑
                mainCamera.clearFlags = originalClearFlags;
                mainCamera.backgroundColor = originalBackgroundColor;
                
                // 确保天空盒设置正确
                if (originalClearFlags == CameraClearFlags.Skybox && originalSkybox != null)
                {
                    RenderSettings.skybox = originalSkybox;
                }
                
                Debug.Log("已为主相机设置点云渲染支持（保持原始设置）");
            }
            else
            {
                Debug.LogWarning("未找到主相机，无法设置点云渲染支持");
            }
        }
        
        /// <summary>
        /// 手动初始化点云系统（可在Inspector中调用）
        /// </summary>
        [ContextMenu("手动初始化点云系统")]
        public void ManualInitialize()
        {
            isInitialized = false;
            InitializePointCloudSystem();
        }
        
        /// <summary>
        /// 恢复天空盒设置（如果天空变黑可以调用此方法）
        /// </summary>
        [ContextMenu("恢复天空盒设置")]
        public void RestoreSkyboxSettings()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // 强制设置为天空盒模式
                mainCamera.clearFlags = CameraClearFlags.Skybox;
                
                // 尝试恢复默认天空盒
                if (RenderSettings.skybox == null)
                {
                    // 查找默认天空盒材质
                    Material defaultSkybox = Resources.Load<Material>("Default-Skybox");
                    if (defaultSkybox != null)
                    {
                        RenderSettings.skybox = defaultSkybox;
                    }
                }
                
                Debug.Log("天空盒设置已恢复");
            }
        }
        
        /// <summary>
        /// 获取点云管理器引用
        /// </summary>
        public PowerlinePointCloudManager GetPointCloudManager()
        {
            return pointCloudManager;
        }
        
        /// <summary>
        /// 检查点云系统是否已初始化
        /// </summary>
        public bool IsInitialized()
        {
            return isInitialized && pointCloudManager != null;
        }
        
        void OnValidate()
        {
            // 在Inspector中修改参数时自动应用到点云管理器
            if (Application.isPlaying && pointCloudManager != null)
            {
                ConfigurePointCloudManager(pointCloudManager);
            }
        }
    }
} 