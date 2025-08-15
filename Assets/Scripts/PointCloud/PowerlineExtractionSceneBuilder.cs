using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UI;

// 修复CS1626错误：Cannot yield a value in the body of a try block with a catch clause

namespace PowerlineSystem
{
    /// <summary>
    /// 电力线提取场景构建器
    /// 专门用于电力线提取模式，在空场景中自动创建电塔和电线
    /// </summary>
    public class PowerlineExtractionSceneBuilder : MonoBehaviour
    {
        [Header("模式配置")]
        [Tooltip("是否启用电力线提取模式")]
        public bool enableExtractionMode = true;
        
        [Tooltip("是否在提取完成后弹出点云查看器")]
        public bool showPointCloudViewer = false;
        
        [Tooltip("是否自动清空场景中现有的电塔")]
        public bool autoClearExistingTowers = true;
        
        [Header("电塔预制体")]
        [Tooltip("电塔预制体")]
        public GameObject towerPrefab;
        
        [Header("电线设置")]
        [Tooltip("电线材质")]
        public Material powerlineMaterial;
        
        [Tooltip("电线粗细")]
        public float powerlineThickness = 0.1f;
        
        [Header("场景配置")]
        [Tooltip("电塔父对象")]
        public Transform towerParent;
        
        [Tooltip("电线父对象")]
        public Transform powerlineParent;
        
        [Header("数据缩放配置")]
        [Tooltip("目标电塔平均高度范围")]
        public Vector2 targetTowerHeightRange = new Vector2(10f, 20f);
        
        [Tooltip("是否启用自动数据缩放（注意：JSON转CSV阶段已自动缩放到高度=10）")]
        public bool enableAutoScaling = false;
        
        [Tooltip("强制使用指定的缩放因子（当enableAutoScaling为false时）")]
        public float manualScaleFactor = 1f;
        
        [Header("相机控制配置")]
        [Tooltip("是否在电力线提取完成后自动跳转相机视角")]
        public bool enableAutoJumpToFirstTower = true;
        
        [Tooltip("相机跳转到第一个电塔的距离")]
        public float cameraDistanceFromTower = 100f;
        
        [Tooltip("相机跳转高度偏移")]
        public float cameraHeightOffset = 50f;
        
        [Header("树木配置")]
        [Tooltip("是否在建立电塔和电线的同时建立树木")]
        public bool enableTreePlacement = true;
        
        [Tooltip("树木预制体")]
        public GameObject treePrefab;
        
        [Tooltip("树木CSV文件名")]
        public string treeCsvFileName = "trees";
        
        [Tooltip("每个电塔附近的树木数量")]
        public int treesPerTower = 3;
        
        [Tooltip("树木距离电塔的最小距离")]
        public float minTreeDistanceFromTower = 8f;
        
        [Tooltip("树木距离电塔的最大距离")]
        public float maxTreeDistanceFromTower = 25f;
        
        [Tooltip("是否启用树木自动缩放")]
        public bool enableTreeAutoScaling = true;
        
        [Tooltip("树木高度相对于电塔高度的比例范围")]
        public Vector2 treeHeightRatioRange = new Vector2(0.2f, 0.8f);
        
        [Tooltip("树木高度随机变化范围（相对于计算出的高度）")]
        public Vector2 treeHeightVariationRange = new Vector2(0.7f, 1.3f);
        
        [Tooltip("是否启用树木高度随机变化")]
        public bool enableTreeHeightVariation = true;
        
        [Tooltip("树木基础缩放倍数")]
        public float treeBaseScale = 50f;
        
        // 内部组件引用
        private TowerPinpointSystem towerPinpointSystem;
        private SceneInitializer sceneInitializer;
        private SimpleUIToolkitManager uiManager;
        private CameraManager cameraManager;
        
        // 树木管理
        private List<GameObject> placedTrees = new List<GameObject>();
        private List<TreeData> treeDataList = new List<TreeData>();
        private TerrainManager terrainManager;
        
        // 电力线提取数据
        private string currentCsvPath = "";
        private List<Vector3> originalTowerPositions = new List<Vector3>(); // 原始电塔位置
        private List<Vector3> scaledTowerPositions = new List<Vector3>(); // 缩放后的电塔位置
        private float calculatedScaleFactor = 1f; // 计算出的缩放因子
        
        // 状态
        private bool isBuilding = false;
        
        [System.Serializable]
        public struct TreeData
        {
            public int treeId;
            public int groupId;
            public int towerId;
            public Vector3 position;
            public string treeType;
        }
        
        [System.Serializable]
        public class SimpleTreeData
        {
            public int treeId;
            public Vector3 position;
            public float height;
            public int groupId;
            public int towerId;
            public string treeType;
            public float scale;
            
            public SimpleTreeData(int id, Vector3 pos, float h, int group, int tower, string type, float s = 1.0f)
            {
                treeId = id;
                position = pos;
                height = h;
                groupId = group;
                towerId = tower;
                treeType = type;
                scale = s;
            }
        }
        
        /// <summary>
        /// 组件启动时的初始化
        /// </summary>
        void Start()
        {
            // 自动尝试创建Tower标签
            AutoCreateTowerTagIfNeeded();
            
            // 初始化组件
            Initialize();
        }
        
        /// <summary>
        /// 自动创建Tower标签（如果需要）
        /// </summary>
        void AutoCreateTowerTagIfNeeded()
        {
#if UNITY_EDITOR
            try
            {
                // 测试Tower标签是否存在
                GameObject.FindGameObjectsWithTag("Tower");
                Debug.Log("[PowerlineExtractionSceneBuilder] Tower标签已存在");
            }
            catch (UnityException)
            {
                Debug.LogWarning("[PowerlineExtractionSceneBuilder] Tower标签未定义，尝试自动创建...");
                
                // 查找TowerTagHelper组件
                TowerTagHelper tagHelper = FindObjectOfType<TowerTagHelper>();
                if (tagHelper == null)
                {
                    // 创建一个临时的TowerTagHelper
                    GameObject tempObj = new GameObject("TempTowerTagHelper");
                    tagHelper = tempObj.AddComponent<TowerTagHelper>();
                }
                
                // 尝试创建标签
                tagHelper.CreateTowerTag();
                
                // 如果是临时创建的，删除它
                if (tagHelper.gameObject.name == "TempTowerTagHelper")
                {
                    DestroyImmediate(tagHelper.gameObject);
                }
            }
#else
            Debug.Log("[PowerlineExtractionSceneBuilder] 运行时模式，跳过标签创建");
#endif
        }
        
        /// <summary>
        /// 初始化场景构建器
        /// </summary>
        void Initialize()
        {
            // 查找UI管理器
            if (uiManager == null)
            {
                uiManager = FindObjectOfType<SimpleUIToolkitManager>();
            }
            
            // 查找相机管理器
            if (cameraManager == null)
            {
                cameraManager = FindObjectOfType<CameraManager>();
                if (cameraManager != null)
                {
                    Debug.Log("[PowerlineExtractionSceneBuilder] 找到CameraManager");
                }
                else
                {
                    Debug.LogWarning("[PowerlineExtractionSceneBuilder] 未找到CameraManager，将使用基础相机控制");
                }
            }
            
            // 查找或创建电塔定位系统
            if (towerPinpointSystem == null)
            {
                towerPinpointSystem = FindObjectOfType<TowerPinpointSystem>();
                if (towerPinpointSystem == null)
                {
                    // 自动创建TowerPinpointSystem
                    GameObject towerSystemObj = new GameObject("TowerPinpointSystem");
                    towerPinpointSystem = towerSystemObj.AddComponent<TowerPinpointSystem>();
                    Debug.Log("[PowerlineExtractionSceneBuilder] 已自动创建TowerPinpointSystem组件");
                }
                else
                {
                    Debug.Log("[PowerlineExtractionSceneBuilder] 找到现有的TowerPinpointSystem组件");
                }
            }
            
            // 查找场景初始化器
            if (sceneInitializer == null)
            {
                sceneInitializer = FindObjectOfType<SceneInitializer>();
                if (sceneInitializer == null)
                {
                    Debug.LogWarning("[PowerlineExtractionSceneBuilder] 未找到SceneInitializer，某些功能可能受限");
                }
            }
            
            // 确保SceneInitializer的towerPrefab被正确设置
            if (sceneInitializer != null && sceneInitializer.towerPrefab == null)
            {
                if (towerPrefab != null)
                {
                    sceneInitializer.towerPrefab = towerPrefab;
                    Debug.Log("[PowerlineExtractionSceneBuilder] 已将towerPrefab设置到SceneInitializer");
                }
                else
                {
                    Debug.LogWarning("[PowerlineExtractionSceneBuilder] SceneInitializer的towerPrefab为空，可能影响电塔创建");
                }
            }
            
            // 查找地形管理器
            if (terrainManager == null)
            {
                terrainManager = FindObjectOfType<TerrainManager>();
                if (terrainManager != null)
                {
                    Debug.Log("[PowerlineExtractionSceneBuilder] 找到TerrainManager");
                }
                else
                {
                    Debug.LogWarning("[PowerlineExtractionSceneBuilder] 未找到TerrainManager，树木地形适配功能将不可用");
                }
            }
            
            // 查找电塔预制件
            if (towerPrefab == null)
            {
                towerPrefab = Resources.Load<GameObject>("Prefabs/GoodTower");
                if (towerPrefab == null)
                {
                    Debug.LogError("[PowerlineExtractionSceneBuilder] 未找到默认电塔预制件，请确保Resources/Prefabs/GoodTower.prefab存在");
                    Debug.LogError("[PowerlineExtractionSceneBuilder] 请手动指定towerPrefab或检查预制件路径");
                }
                else
                {
                    Debug.Log("[PowerlineExtractionSceneBuilder] 已自动加载电塔预制件: GoodTower");
                }
            }
            
            // 创建父对象
            CreateParentObjects();
            
            Debug.Log("[PowerlineExtractionSceneBuilder] 初始化完成");
        }
        
        /// <summary>
        /// 创建父对象用于组织场景
        /// </summary>
        void CreateParentObjects()
        {
            if (towerParent == null)
            {
                GameObject towerParentObj = GameObject.Find("电力塔");
                if (towerParentObj == null)
                {
                    towerParentObj = new GameObject("电力塔");
                }
                towerParent = towerParentObj.transform;
            }
            
            if (powerlineParent == null)
            {
                GameObject powerlineParentObj = GameObject.Find("电力线");
                if (powerlineParentObj == null)
                {
                    powerlineParentObj = new GameObject("电力线");
                }
                powerlineParent = powerlineParentObj.transform;
            }
        }
        
        /// <summary>
        /// 从CSV文件构建电力线提取场景
        /// 使用提取出来的电塔数据，并调用原来的方式重置场景为新的电塔和电线
        /// 工作流程：
        /// 1. 清空现有场景
        /// 2. 从CSV加载电塔数据并创建电塔实例
        /// 3. 设置电塔标签
        /// 4. 使用SceneInitializer.RegenerateAll()重新生成电力线连接
        /// 5. 优化相机视角
        /// </summary>
        /// <param name="csvPath">提取的电塔数据CSV文件路径</param>
        public void BuildSceneFromCsv(string csvPath)
        {
            if (isBuilding)
            {
                Debug.LogWarning("[PowerlineExtractionSceneBuilder] 正在构建场景，请等待完成");
                return;
            }
            
            if (!enableExtractionMode)
            {
                Debug.Log("[PowerlineExtractionSceneBuilder] 电力线提取模式未启用，跳过场景构建");
                return;
            }
            
            StartCoroutine(BuildSceneCoroutine(csvPath));
        }
        
        /// <summary>
        /// 构建场景的协程
        /// </summary>
        IEnumerator BuildSceneCoroutine(string csvPath)
        {
            isBuilding = true;
            System.Exception buildException = null;
            
            try
            {
                Debug.Log($"[PowerlineExtractionSceneBuilder] 开始构建场景，CSV路径: {csvPath}");
                
                // 验证文件
                if (!File.Exists(csvPath))
                {
                    throw new System.IO.FileNotFoundException($"CSV文件不存在: {csvPath}");
                }
            }
            catch (System.Exception ex)
            {
                buildException = ex;
            }
            
            // 如果初始化阶段有错误，直接返回
            if (buildException != null)
            {
                Debug.LogError($"[PowerlineExtractionSceneBuilder] 场景构建失败: {buildException.Message}");
                isBuilding = false;
                yield break;
            }
            
            // 执行实际的场景构建步骤
            yield return StartCoroutine(ExecuteBuildSteps(csvPath));
            
            isBuilding = false;
        }
        
        /// <summary>
        /// 执行构建步骤的协程
        /// </summary>
        IEnumerator ExecuteBuildSteps(string csvPath)
        {
            // 步骤1：清空现有场景（无yield的错误处理）
            bool clearSuccess = true;
            if (autoClearExistingTowers)
            {
                try
                {
                    ClearExistingScene();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[PowerlineExtractionSceneBuilder] 清空场景失败: {ex.Message}");
                    clearSuccess = false;
                }
            }
            
            // 如果清空失败，停止构建
            if (!clearSuccess)
            {
                yield break;
            }
            
            // 等待清空完成
            if (autoClearExistingTowers)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            // 步骤2：加载并创建电塔
            Debug.Log("[PowerlineExtractionSceneBuilder] 正在创建电力塔...");
            yield return StartCoroutine(CreateTowersFromCsv(csvPath));
            
            // 步骤3：创建电力线连接
            Debug.Log("[PowerlineExtractionSceneBuilder] 正在创建电力线连接...");
            yield return StartCoroutine(CreatePowerlineConnections());
            
            // 步骤3.5：加载树木数据
            if (enableTreePlacement)
            {
                Debug.Log("[PowerlineExtractionSceneBuilder] 正在加载树木数据...");
                // 使用新的简化树木构建方式
                LoadSimpleTreeData();
            }
            
            // 步骤4：优化相机视角（无yield的错误处理）
            try
            {
                OptimizeCameraView();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PowerlineExtractionSceneBuilder] 优化相机视角失败: {ex.Message}");
            }
            
            // 步骤5：自动跳转相机到第一个电塔（如果启用）
            if (enableAutoJumpToFirstTower)
            {
                try
                {
                    AutoJumpCameraToFirstTower();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[PowerlineExtractionSceneBuilder] 自动跳转相机失败: {ex.Message}");
                }
                
                // 等待相机跳转完成
                yield return new WaitForSeconds(0.2f);
            }
            
            // 步骤5.5：放置树木（如果启用）
            if (enableTreePlacement)
            {
                Debug.Log("[PowerlineExtractionSceneBuilder] 正在放置树木...");
                try
                {
                    // 使用新的简化树木构建方式
                    PlaceTreesFromSimplifiedInput();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[PowerlineExtractionSceneBuilder] 放置树木失败: {ex.Message}");
                }
                
                // 等待树木放置完成
                yield return new WaitForSeconds(0.1f);
            }
            
            // 步骤6：更新UI模式（无yield的错误处理）
            try
            {
                if (uiManager != null)
                {
                    uiManager.currentMode = SimpleUIToolkitManager.UIMode.Normal;
                    Debug.Log("[PowerlineExtractionSceneBuilder] 已切换到正常模式");
                }
                
                Debug.Log("[PowerlineExtractionSceneBuilder] 场景构建完成！");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PowerlineExtractionSceneBuilder] UI模式更新失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 清空现有场景
        /// </summary>
        void ClearExistingScene()
        {
            Debug.Log("[PowerlineExtractionSceneBuilder] 开始清空现有场景...");
            int clearedCount = 0;
            
            // 方法1：清空指定父对象下的所有子对象
            if (towerParent != null)
            {
                int towerChildCount = towerParent.childCount;
                for (int i = towerParent.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(towerParent.GetChild(i).gameObject);
                    clearedCount++;
                }
                Debug.Log($"[PowerlineExtractionSceneBuilder] 从towerParent清理了 {towerChildCount} 个对象");
            }
            
            if (powerlineParent != null)
            {
                int powerlineChildCount = powerlineParent.childCount;
                for (int i = powerlineParent.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(powerlineParent.GetChild(i).gameObject);
                }
                Debug.Log($"[PowerlineExtractionSceneBuilder] 从powerlineParent清理了 {powerlineChildCount} 个对象");
            }
            
            // 方法2：通过标签查找并清理（如果标签存在）
            try
            {
                GameObject[] taggedTowers = GameObject.FindGameObjectsWithTag("Tower");
                foreach (GameObject tower in taggedTowers)
                {
                    if (tower != null)
                    {
                        DestroyImmediate(tower);
                        clearedCount++;
                    }
                }
                Debug.Log($"[PowerlineExtractionSceneBuilder] 通过Tower标签清理了 {taggedTowers.Length} 个电塔");
            }
            catch (UnityException)
            {
                Debug.Log("[PowerlineExtractionSceneBuilder] Tower标签未定义，跳过标签清理");
            }
            
            // 清理已放置的树木
            if (enableTreePlacement)
            {
                ClearPlacedTrees();
                Debug.Log("[PowerlineExtractionSceneBuilder] 已清理所有树木");
            }
            
            // 方法3：通过名称全局查找并清理
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            int globalCleared = 0;
            foreach (GameObject obj in allObjects)
            {
                if (obj != null && 
                    (obj.name.Contains("Tower") || 
                     obj.name.Contains("GoodTower") || 
                     obj.name.Contains("Powerline") ||
                     obj.name.Contains("Wire")))
                {
                    // 避免删除重要的系统对象
                    if (obj.name.Contains("TowerPinpointSystem") || 
                        obj.name.Contains("SceneInitializer") ||
                        obj.name.Contains("PowerlineExtractionSceneBuilder"))
                    {
                        continue;
                    }
                    
                    DestroyImmediate(obj);
                    globalCleared++;
                }
            }
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] 全局搜索清理了 {globalCleared} 个对象");
            Debug.Log($"[PowerlineExtractionSceneBuilder] 场景清理完成，总共清理了 {clearedCount + globalCleared} 个对象");
        }
        
        /// <summary>
        /// 从CSV文件创建电塔 - 使用SceneInitializer的tower_centers构建方法
        /// </summary>
        IEnumerator CreateTowersFromCsv(string csvPath)
        {
            Debug.Log($"[PowerlineExtractionSceneBuilder] 开始从CSV创建电塔: {csvPath}");
            
            // 保存当前CSV路径供后续使用
            currentCsvPath = csvPath;
            
            // 检查SceneInitializer
            if (sceneInitializer == null)
            {
                Debug.LogError("[PowerlineExtractionSceneBuilder] SceneInitializer 未找到，尝试重新初始化...");
                Initialize(); // 重新初始化，应该会自动查找SceneInitializer
                
                if (sceneInitializer == null)
                {
                    Debug.LogError("[PowerlineExtractionSceneBuilder] 无法找到SceneInitializer，停止电塔创建");
                    yield break;
                }
            }
            
            // 检查CSV文件
            if (!System.IO.File.Exists(csvPath))
            {
                Debug.LogError($"[PowerlineExtractionSceneBuilder] CSV文件不存在: {csvPath}");
                yield break;
            }
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] 所有检查通过，开始使用SceneInitializer的tower_centers构建方法...");
            
            // 使用SceneInitializer的tower_centers构建方法
            yield return StartCoroutine(BuildSceneUsingSceneInitializer(csvPath));
        }
        
        /// <summary>
        /// 使用SceneInitializer的tower_centers构建方法构建场景
        /// </summary>
        IEnumerator BuildSceneUsingSceneInitializer(string csvPath)
        {
            Debug.Log("[PowerlineExtractionSceneBuilder] 使用SceneInitializer的tower_centers构建方法...");
            
            // 检查SceneInitializer的towerPrefab
            if (sceneInitializer.towerPrefab == null)
            {
                Debug.LogError("[PowerlineExtractionSceneBuilder] SceneInitializer的towerPrefab为空，无法创建电塔！");
                yield break;
            }
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] SceneInitializer的towerPrefab: {sceneInitializer.towerPrefab.name}");
            
            // 检查CSV文件是否存在
            if (!System.IO.File.Exists(csvPath))
            {
                Debug.LogError($"[PowerlineExtractionSceneBuilder] CSV文件不存在: {csvPath}");
                yield break;
            }
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] CSV文件存在，直接使用: {csvPath}");
            Debug.Log($"[PowerlineExtractionSceneBuilder] 文件大小: {new System.IO.FileInfo(csvPath).Length} 字节");
            
            // 获取文件名（不包含扩展名）
            string fileName = System.IO.Path.GetFileNameWithoutExtension(csvPath);
            
            // 检查文件是否已经在Resources目录中
            if (csvPath.Contains("Resources"))
            {
                Debug.Log($"[PowerlineExtractionSceneBuilder] CSV文件已在Resources目录中，直接使用: {csvPath}");
            }
            else
            {
                Debug.LogWarning($"[PowerlineExtractionSceneBuilder] CSV文件不在Resources目录中，但文件存在: {csvPath}");
                Debug.LogWarning($"[PowerlineExtractionSceneBuilder] 尝试继续构建场景，但可能存在问题");
            }
            
            // 步骤2：检测并配置SceneInitializer使用正确的格式
            sceneInitializer.SetCsvFileName(fileName);
            
            // 自动检测CSV格式
            SceneInitializer.CSVFormat detectedFormat = DetectCSVFormat(csvPath);
            sceneInitializer.csvFormat = detectedFormat;
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] 自动检测到CSV格式: {detectedFormat}");
            Debug.Log($"[PowerlineExtractionSceneBuilder] 设置SceneInitializer.csvFormat = {detectedFormat}");
            
            // 根据格式设置不同的参数
            if (detectedFormat == SceneInitializer.CSVFormat.B)
            {
                Debug.Log("[PowerlineExtractionSceneBuilder] 使用B.csv格式，支持group分组连线");
                sceneInitializer.towerScale = 3.0f; // B.csv格式使用较小的缩放
            }
            else
            {
                Debug.Log("[PowerlineExtractionSceneBuilder] 使用标准格式");
                sceneInitializer.towerScale = 5.0f; // 标准格式使用较大的缩放
            }
            sceneInitializer.baseTowerHeight = 2f;
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] 配置SceneInitializer: csvFileName='{sceneInitializer.csvFileName}', csvFormat={sceneInitializer.csvFormat}, towerScale={sceneInitializer.towerScale}");
            
            // 步骤3：清空现有场景（如果需要）
            if (autoClearExistingTowers)
            {
                ClearExistingScene();
                yield return new WaitForSeconds(0.1f);
            }
            
            // 步骤4：使用SceneInitializer的InitializeScene方法构建完整场景
            Debug.Log("[PowerlineExtractionSceneBuilder] 调用SceneInitializer.InitializeScene()构建场景...");
            Debug.Log($"[PowerlineExtractionSceneBuilder] SceneInitializer配置: towerScale={sceneInitializer.towerScale}, baseTowerHeight={sceneInitializer.baseTowerHeight}");
            
            try
            {
                sceneInitializer.InitializeScene();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PowerlineExtractionSceneBuilder] SceneInitializer.InitializeScene()失败: {ex.Message}");
                yield break;
            }
            
            // 等待场景构建完成
            yield return new WaitForSeconds(1.0f);
            
            // 步骤5：验证构建结果
            GameObject[] createdTowers = FindTowersSafely();
            int powerlineCount = sceneInitializer.powerlines != null ? sceneInitializer.powerlines.Count : 0;
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] 场景构建完成！创建了 {createdTowers.Length} 座电塔，{powerlineCount} 条电力线");
            
            if (createdTowers.Length == 0)
            {
                Debug.LogWarning("[PowerlineExtractionSceneBuilder] 警告：未创建任何电塔，请检查CSV文件格式");
            }
            
            if (powerlineCount == 0)
            {
                Debug.LogWarning("[PowerlineExtractionSceneBuilder] 警告：未生成任何电力线，可能是电塔数量不足");
            }
            
            // 步骤6：设置电塔标签
            SetTowerTagsSafely();
            
            // 步骤7：强制跳转相机到第一个电塔位置（因为坐标可能很大）
            if (enableAutoJumpToFirstTower)
            {
                Debug.Log("[PowerlineExtractionSceneBuilder] 强制跳转相机到电塔位置...");
                AutoJumpCameraToFirstTower();
            }
            
            Debug.Log("[PowerlineExtractionSceneBuilder] 使用SceneInitializer构建场景完成！");
        }
        

        
        /// <summary>
        /// 从CSV文件加载电塔数据，转换为SceneInitializer.SimpleTowerData格式
        /// 支持自动缩放功能
        /// </summary>

        

        
        /// <summary>
        /// 将包含中文的文件名转换为英文文件名
        /// 解决Unity Resources.Load对中文文件名支持不佳的问题
        /// </summary>
        string ConvertToEnglishFileName(string originalName)
        {
            // 如果文件名不包含中文，直接返回
            if (!ContainsChinese(originalName))
            {
                return originalName;
            }
            
            // 创建英文映射
            string englishName = originalName
                .Replace("tower_centers_", "tower_centers_")  // 保持前缀
                .Replace("A部分", "SectionA")
                .Replace("B部分", "SectionB")
                .Replace("C部分", "SectionC")
                .Replace("线路", "Line")
                .Replace("部分", "Section")
                .Replace("平坦", "Flat")
                .Replace("缺失", "Missing")
                .Replace("右侧", "Right")
                .Replace("左侧", "Left")
                .Replace("已抽稀", "Thinned")
                .Replace("最小段", "MinSegment");
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] 中文文件名转换: '{originalName}' -> '{englishName}'");
            return englishName;
        }
        
        /// <summary>
        /// 检查字符串是否包含中文字符
        /// </summary>
        bool ContainsChinese(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            
            foreach (char c in text)
            {
                if (c >= 0x4e00 && c <= 0x9fff) // 中文Unicode范围
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 检测CSV文件格式
        /// </summary>
        SceneInitializer.CSVFormat DetectCSVFormat(string csvPath)
        {
            try
            {
                string[] lines = System.IO.File.ReadAllLines(csvPath);
                if (lines.Length < 1) return SceneInitializer.CSVFormat.SimpleTowers;
                
                // 检查第一行数据，确定列数和格式
                string firstDataLine = lines[0].Trim();
                if (string.IsNullOrEmpty(firstDataLine)) return SceneInitializer.CSVFormat.SimpleTowers;
                
                Debug.Log($"[PowerlineExtractionSceneBuilder] 检测CSV格式，第一行数据: {firstDataLine}");
                
                // 如果第一行包含标题（文字），查看第二行数据
                // 但是要排除B.csv格式的标题行（group_id,order,x,y,z,line_count）
                if (firstDataLine.ToLower().Contains("tower_id") || 
                    (firstDataLine.ToLower().Contains("x") && firstDataLine.ToLower().Contains("y") && 
                     !firstDataLine.ToLower().Contains("group_id")))
                {
                    if (lines.Length > 1)
                    {
                        firstDataLine = lines[1].Trim();
                        Debug.Log($"[PowerlineExtractionSceneBuilder] 跳过标题行，使用第二行数据: {firstDataLine}");
                    }
                }
                
                // 分析数据行的列数
                string[] tokens = firstDataLine.Split(',');
                Debug.Log($"[PowerlineExtractionSceneBuilder] CSV列数: {tokens.Length}");
                
                // 检查是否为B.csv格式（6列：group_id,order,x,y,z,line_count）
                if (tokens.Length == 6)
                {
                    // 进一步检查是否为B.csv格式：第一列和第二列应该是整数（group_id和order）
                    if (int.TryParse(tokens[0], out _) && int.TryParse(tokens[1], out _))
                    {
                        Debug.Log("[PowerlineExtractionSceneBuilder] 检测为B.csv格式(group_id,order,x,y,z,line_count) -> 使用B格式，支持group分组连线");
                        return SceneInitializer.CSVFormat.B;
                    }
                    else
                    {
                        Debug.Log("[PowerlineExtractionSceneBuilder] 检测为6列格式但非B.csv格式 -> 使用SimpleTowers格式");
                        return SceneInitializer.CSVFormat.SimpleTowers;
                    }
                }
                // 对于提取的电力线CSV文件，格式实际是：
                // tower_centers_*.csv -> x,y,z 格式 (3列)
                // SceneInitializer.TowerCenters期望的是 x,z,height 格式
                // 我们需要将我们的x,y,z映射为x,z,height（y作为高度）
                else if (tokens.Length == 3)
                {
                    Debug.Log("[PowerlineExtractionSceneBuilder] 检测为3列格式(x,y,z) -> 使用TowerCenters格式，将数据映射为x,z,height");
                    return SceneInitializer.CSVFormat.TowerCenters;
                }
                else if (tokens.Length == 4)
                {
                    Debug.Log("[PowerlineExtractionSceneBuilder] 检测为4列格式(x,y,z,height) -> 使用SimpleTowers格式");
                    return SceneInitializer.CSVFormat.SimpleTowers;
                }
                else
                {
                    Debug.LogWarning($"[PowerlineExtractionSceneBuilder] 未知列数 {tokens.Length}，使用默认SimpleTowers格式");
                    return SceneInitializer.CSVFormat.SimpleTowers;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PowerlineExtractionSceneBuilder] 检测CSV格式失败: {ex.Message}，使用默认格式");
                return SceneInitializer.CSVFormat.SimpleTowers;
            }
        }
        
        /// <summary>
        /// 检查是否需要格式转换
        /// </summary>
        bool NeedsFormatConversion(string csvPath)
        {
            try
            {
                string[] lines = System.IO.File.ReadAllLines(csvPath);
                if (lines.Length < 2) return false;
                
                string firstLine = lines[0].ToLower();
                string secondLine = lines[1];
                
                // 如果包含Tower_ID列，需要转换
                if (firstLine.Contains("tower_id") || (secondLine.Split(',')[0].Contains("Tower")))
                {
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 专门为TowerCenters格式转换CSV数据
        /// 将提取的x,y,z格式转换为TowerCenters期望的x,z,height格式
        /// 支持数据缩放
        /// </summary>
        void ConvertCSVFormatForTowerCenters(string sourcePath, string targetPath, SceneInitializer.CSVFormat targetFormat)
        {
            try
            {
                string[] lines = System.IO.File.ReadAllLines(sourcePath);
                List<string> outputLines = new List<string>();
                
                Debug.Log($"[PowerlineExtractionSceneBuilder] 开始转换CSV格式为TowerCenters，源文件: {sourcePath}");
                
                // 如果启用了缩放，需要先计算缩放因子
                List<Vector3> rawPositions = new List<Vector3>();
                List<float> rawHeights = new List<float>();
                
                // 第一遍：读取原始数据
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.ToLower().Contains("x") && line.ToLower().Contains("z")) continue; // 跳过可能的标题行
                    
                    string[] tokens = line.Split(',');
                    if (tokens.Length == 3)
                    {
                        if (float.TryParse(tokens[0], out float x) &&
                            float.TryParse(tokens[1], out float y) &&
                            float.TryParse(tokens[2], out float z))
                        {
                            rawPositions.Add(new Vector3(x, y, z));
                            rawHeights.Add(y); // 使用y作为高度
                        }
                    }
                }
                
                // 计算缩放因子
                float scaleFactor = 1f;
                if (enableAutoScaling && rawPositions.Count > 0)
                {
                    // 简单的缩放因子计算：将平均高度缩放到目标范围
                    float averageHeight = rawHeights.Average();
                    float targetHeight = (targetTowerHeightRange.x + targetTowerHeightRange.y) * 0.5f;
                    scaleFactor = targetHeight / Mathf.Max(averageHeight, 1f);
                    scaleFactor = Mathf.Clamp(scaleFactor, 0.001f, 1000f);
                    Debug.Log($"[PowerlineExtractionSceneBuilder] 转换时应用自动缩放因子: {scaleFactor:F4}");
                }
                else if (!enableAutoScaling && manualScaleFactor != 1f)
                {
                    scaleFactor = manualScaleFactor;
                    Debug.Log($"[PowerlineExtractionSceneBuilder] 转换时应用手动缩放因子: {scaleFactor:F4}");
                }
                else
                {
                    Debug.Log($"[PowerlineExtractionSceneBuilder] 转换时不应用缩放");
                }
                
                // 第二遍：应用缩放并转换格式
                for (int i = 0; i < rawPositions.Count; i++)
                {
                    Vector3 originalPos = rawPositions[i];
                    Vector3 scaledPos = originalPos * scaleFactor;
                    
                    // 转换格式：我们的x,y,z -> TowerCenters的x,z,height
                    // 应用缩放后：scaledX, scaledZ, scaledY(作为高度)
                    outputLines.Add($"{scaledPos.x:F6},{scaledPos.z:F6},{scaledPos.y:F6}");
                    
                    if (scaleFactor != 1f)
                    {
                        Debug.Log($"[PowerlineExtractionSceneBuilder] 转换数据: ({originalPos.x:F2},{originalPos.y:F2},{originalPos.z:F2}) → 缩放({scaledPos.x:F2},{scaledPos.y:F2},{scaledPos.z:F2}) → TowerCenters({scaledPos.x:F2},{scaledPos.z:F2},{scaledPos.y:F2})");
                    }
                    else
                    {
                        Debug.Log($"[PowerlineExtractionSceneBuilder] 转换数据: ({originalPos.x:F2},{originalPos.y:F2},{originalPos.z:F2}) → TowerCenters({originalPos.x:F2},{originalPos.z:F2},{originalPos.y:F2})");
                    }
                }
                
                // 写入转换后的文件
                System.IO.File.WriteAllLines(targetPath, outputLines);
                Debug.Log($"[PowerlineExtractionSceneBuilder] CSV格式转换完成，输出 {outputLines.Count} 行数据到: {targetPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PowerlineExtractionSceneBuilder] CSV格式转换失败: {ex.Message}");
                // 如果转换失败，尝试直接复制原文件
                try
                {
                    System.IO.File.Copy(sourcePath, targetPath, true);
                    Debug.Log($"[PowerlineExtractionSceneBuilder] 已复制原文件作为备用: {targetPath}");
                }
                catch (System.Exception copyEx)
                {
                    Debug.LogError($"[PowerlineExtractionSceneBuilder] 复制原文件也失败: {copyEx.Message}");
                }
            }
        }
        
        /// <summary>
        /// 转换CSV格式为SceneInitializer支持的格式
        /// </summary>
        void ConvertCSVFormat(string sourcePath, string targetPath, SceneInitializer.CSVFormat targetFormat)
        {
            try
            {
                string[] lines = System.IO.File.ReadAllLines(sourcePath);
                List<string> outputLines = new List<string>();
                
                // 跳过标题行，从数据行开始
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;
                    
                    string[] tokens = line.Split(',');
                    if (tokens.Length >= 4)
                    {
                        // 假设格式是 Tower_ID,X,Y,Z
                        // 转换为 x,y,z,height 格式，默认height=30
                        if (float.TryParse(tokens[1], out float x) &&
                            float.TryParse(tokens[2], out float y) &&
                            float.TryParse(tokens[3], out float z))
                        {
                            // 使用z坐标作为基础高度，添加默认塔高
                            float height = 30.0f; // 默认电塔高度
                            outputLines.Add($"{x},{y},{z},{height}");
                        }
                    }
                }
                
                // 写入转换后的文件
                System.IO.File.WriteAllLines(targetPath, outputLines);
                Debug.Log($"[PowerlineExtractionSceneBuilder] 已转换CSV格式：{outputLines.Count} 行数据");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PowerlineExtractionSceneBuilder] CSV格式转换失败: {ex.Message}");
                // 如果转换失败，尝试直接复制原文件
                try
                {
                    System.IO.File.Copy(sourcePath, targetPath, true);
                    Debug.Log($"[PowerlineExtractionSceneBuilder] 已复制原文件作为备用: {targetPath}");
                }
                catch (System.Exception copyEx)
                {
                    Debug.LogError($"[PowerlineExtractionSceneBuilder] 复制原文件也失败: {copyEx.Message}");
                }
            }
        }
        
        /// <summary>
        /// 安全地为电塔设置标签
        /// </summary>
        void SetTowerTagsSafely()
        {
            // 查找所有新创建的电塔
            GameObject[] allTowers = FindTowersSafely();
            int taggedCount = 0;
            
            foreach (GameObject tower in allTowers)
            {
                try
                {
                    tower.tag = "Tower";
                    taggedCount++;
                }
                catch (UnityException)
                {
                    // Tower标签不存在，尝试创建标签或跳过
                    Debug.LogWarning($"[PowerlineExtractionSceneBuilder] 无法为 {tower.name} 设置Tower标签，标签可能未定义");
                }
            }
            
            if (taggedCount > 0)
            {
                Debug.Log($"[PowerlineExtractionSceneBuilder] 成功为 {taggedCount} 座电塔设置了Tower标签");
            }
            else if (allTowers.Length > 0)
            {
                Debug.LogWarning("[PowerlineExtractionSceneBuilder] 建议在Unity的Tags & Layers中添加'Tower'标签以获得更好的性能");
            }
        }
        
        /// <summary>
        /// 创建电力线连接
        /// </summary>
        IEnumerator CreatePowerlineConnections()
        {
            Debug.Log("[PowerlineExtractionSceneBuilder] 验证电力线连接...");
            
            // 由于电塔和电线都已经通过SceneInitializer.InitializeScene()方法创建了
            // 这里只需要验证和优化结果
            
            if (sceneInitializer != null)
            {
                // 验证电塔和电力线是否已正确创建
                GameObject[] createdTowers = FindTowersSafely();
                int powerlineCount = sceneInitializer.powerlines != null ? sceneInitializer.powerlines.Count : 0;
                
                Debug.Log($"[PowerlineExtractionSceneBuilder] 验证结果：{createdTowers.Length} 座电塔，{powerlineCount} 条电力线");
                
                if (createdTowers.Length == 0)
                {
                    Debug.LogWarning("[PowerlineExtractionSceneBuilder] 警告：未找到任何电塔");
                }
                
                if (powerlineCount == 0)
                {
                    Debug.LogWarning("[PowerlineExtractionSceneBuilder] 警告：未生成任何电力线");
                    
                    // 如果电力线未生成，尝试重新生成
                    if (createdTowers.Length >= 2)
                    {
                        Debug.Log("[PowerlineExtractionSceneBuilder] 尝试重新生成电力线...");
                        sceneInitializer.RegenerateAll();
                        yield return new WaitForSeconds(1.0f);
                        
                        powerlineCount = sceneInitializer.powerlines != null ? sceneInitializer.powerlines.Count : 0;
                        Debug.Log($"[PowerlineExtractionSceneBuilder] 重新生成后：{powerlineCount} 条电力线");
                    }
                }
                
                // 验证电力线质量
                if (powerlineCount > 0)
                {
                    Debug.Log("[PowerlineExtractionSceneBuilder] 电力线连接验证完成");
                    
                    // 可以在这里添加额外的电力线优化逻辑
                    // 例如：检查电力线的物理属性、视觉效果等
                }
            }
            else
            {
                Debug.LogWarning("[PowerlineExtractionSceneBuilder] 未找到SceneInitializer，无法验证电力线连接");
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        /// <summary>
        /// 优化相机视角
        /// </summary>
        void OptimizeCameraView()
        {
            // 安全地查找所有电塔
            GameObject[] towers = FindTowersSafely();
            
            if (towers.Length > 0)
            {
                // 计算边界
                Bounds bounds = new Bounds();
                bool boundsInitialized = false;
                
                foreach (GameObject tower in towers)
                {
                    Renderer[] renderers = tower.GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in renderers)
                    {
                        if (!boundsInitialized)
                        {
                            bounds = renderer.bounds;
                            boundsInitialized = true;
                        }
                        else
                        {
                            bounds.Encapsulate(renderer.bounds);
                        }
                    }
                }
                
                if (boundsInitialized)
                {
                    // 调整相机位置
                    Camera mainCamera = Camera.main;
                    if (mainCamera != null)
                    {
                        Vector3 center = bounds.center;
                        float distance = Mathf.Max(bounds.size.x, bounds.size.z) * 1.5f;
                        mainCamera.transform.position = center + Vector3.up * distance * 0.6f + Vector3.back * distance;
                        mainCamera.transform.LookAt(center);
                        
                        Debug.Log($"[PowerlineExtractionSceneBuilder] 已调整相机视角，聚焦于{towers.Length}座电塔");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[PowerlineExtractionSceneBuilder] 优化相机视角失败: 未找到电塔");
            }
        }
        
        /// <summary>
        /// 自动跳转相机到第一个电塔前
        /// </summary>
        void AutoJumpCameraToFirstTower()
        {
            Debug.Log("[PowerlineExtractionSceneBuilder] 开始自动跳转相机到第一个电塔前...");
            
            // 查找所有电塔
            GameObject[] towers = FindTowersSafely();
            
            if (towers.Length == 0)
            {
                Debug.LogWarning("[PowerlineExtractionSceneBuilder] 未找到电塔，无法跳转相机");
                return;
            }
            
            // 找到第一个电塔
            GameObject firstTower = towers[0];
            Vector3 towerPosition = firstTower.transform.position;
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] 找到第一个电塔: {firstTower.name}, 位置: {towerPosition}");
            
            // 计算相机位置：在电塔前方一定距离，稍微抬高
            Vector3 cameraPosition = towerPosition;
            cameraPosition.y += cameraHeightOffset; // 抬高相机
            cameraPosition.z -= cameraDistanceFromTower; // 往后退
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] 计算相机位置: {cameraPosition}, 距离电塔: {cameraDistanceFromTower}m");
            
            // 直接控制主相机（优先选择）
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Debug.Log("[PowerlineExtractionSceneBuilder] 直接控制主相机位置");
                
                mainCamera.transform.position = cameraPosition;
                mainCamera.transform.LookAt(towerPosition);
                
                Debug.Log($"[PowerlineExtractionSceneBuilder] 相机已跳转到位置: {cameraPosition}, 朝向: {towerPosition}");
                
                // 强制更新相机
                mainCamera.enabled = false;
                mainCamera.enabled = true;
            }
            else if (cameraManager != null)
            {
                Debug.Log("[PowerlineExtractionSceneBuilder] 使用CameraManager切换到第一人称视角");
                
                // 切换到第一人称视角（索引0）
                cameraManager.SwitchView(0);
                
                // 等待一帧后设置位置
                StartCoroutine(SetFirstPersonCameraPositionDelayed(cameraPosition, towerPosition));
            }
            else
            {
                Debug.LogWarning("[PowerlineExtractionSceneBuilder] 未找到主相机或CameraManager，无法跳转");
            }
            
            // 额外调试：显示所有电塔的位置
            Debug.Log($"[PowerlineExtractionSceneBuilder] 所有电塔位置信息:");
            for (int i = 0; i < Mathf.Min(towers.Length, 5); i++) // 只显示前5个
            {
                Debug.Log($"[PowerlineExtractionSceneBuilder] 电塔 {i+1}: {towers[i].name} 位置: {towers[i].transform.position}");
            }
            if (towers.Length > 5)
            {
                Debug.Log($"[PowerlineExtractionSceneBuilder] ... 还有 {towers.Length - 5} 座电塔");
            }
        }
        
        /// <summary>
        /// 延迟设置第一人称相机位置的协程
        /// </summary>
        IEnumerator SetFirstPersonCameraPositionDelayed(Vector3 cameraPosition, Vector3 lookAtTarget)
        {
            // 等待几帧，确保第一人称相机已经激活
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            
            // 查找第一人称相机组件
            FirstPersonCamera fpCamera = FindObjectOfType<FirstPersonCamera>();
            if (fpCamera != null)
            {
                Debug.Log($"[PowerlineExtractionSceneBuilder] 设置第一人称相机位置: {cameraPosition}");
                fpCamera.SetPlayerPosition(cameraPosition);
                
                // 设置朝向
                Vector3 direction = (lookAtTarget - cameraPosition).normalized;
                float yRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                fpCamera.transform.rotation = Quaternion.Euler(0, yRotation, 0);
                
                Debug.Log($"[PowerlineExtractionSceneBuilder] 第一人称相机已设置到第一个电塔前，朝向电塔");
            }
            else
            {
                // 如果没有第一人称相机，直接控制主相机
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    mainCamera.transform.position = cameraPosition;
                    mainCamera.transform.LookAt(lookAtTarget);
                    Debug.Log($"[PowerlineExtractionSceneBuilder] 主相机已跳转到第一个电塔前");
                }
            }
        }
        
        /// <summary>
        /// 安全地查找电塔对象
        /// </summary>
        GameObject[] FindTowersSafely()
        {
            List<GameObject> towers = new List<GameObject>();
            
            // 方法1：尝试通过标签查找
            try
            {
                GameObject[] taggedTowers = GameObject.FindGameObjectsWithTag("Tower");
                towers.AddRange(taggedTowers);
            }
            catch (UnityException)
            {
                Debug.LogWarning("[PowerlineExtractionSceneBuilder] Tower标签未定义，使用其他方法查找");
            }
            
            // 方法2：通过父对象查找
            if (towerParent != null)
            {
                foreach (Transform child in towerParent)
                {
                    if (child.name.Contains("Tower") && !towers.Contains(child.gameObject))
                    {
                        towers.Add(child.gameObject);
                    }
                }
            }
            
            // 方法3：通过名称在全局查找
            if (towers.Count == 0)
            {
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if ((obj.name.Contains("Tower") || obj.name.Contains("GoodTower")) && !towers.Contains(obj))
                    {
                        towers.Add(obj);
                    }
                }
            }
            
            return towers.ToArray();
        }
        
        /// <summary>
        /// 设置是否显示点云查看器
        /// </summary>
        public void SetShowPointCloudViewer(bool show)
        {
            showPointCloudViewer = show;
        }
        
        /// <summary>
        /// 检查是否应该显示点云查看器
        /// </summary>
        public bool ShouldShowPointCloudViewer()
        {
            return showPointCloudViewer && enableExtractionMode;
        }
        
        /// <summary>
        /// 等待CSV文件准备就绪
        /// </summary>
        IEnumerator WaitForCsvFileReady()
        {
            Debug.Log("[PowerlineExtractionSceneBuilder] 等待CSV文件准备就绪...");
            
            int maxRetries = 10;
            float waitInterval = 0.5f;
            
            for (int retry = 0; retry < maxRetries; retry++)
            {
                // 查找最新的提取CSV文件
                string latestCsvPath = FindLatestExtractionCsvFile();
                
                if (!string.IsNullOrEmpty(latestCsvPath) && System.IO.File.Exists(latestCsvPath))
                {
                    // 尝试读取文件以确保它可以访问
                    try
                    {
                        string[] lines = System.IO.File.ReadAllLines(latestCsvPath);
                        if (lines.Length > 0)
                        {
                            Debug.Log($"[PowerlineExtractionSceneBuilder] ✓ CSV文件准备就绪: {latestCsvPath}, 包含 {lines.Length} 行数据");
                            currentCsvPath = latestCsvPath;
                            yield break;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[PowerlineExtractionSceneBuilder] 尝试 {retry + 1}: CSV文件还未准备好 - {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[PowerlineExtractionSceneBuilder] 尝试 {retry + 1}: 未找到CSV文件");
                }
                
                yield return new WaitForSeconds(waitInterval);
            }
            
            Debug.LogError("[PowerlineExtractionSceneBuilder] ⚠️ 超时：无法找到或读取CSV文件");
        }
        
        /// <summary>
        /// 查找最新的电力线提取CSV文件
        /// </summary>
        string FindLatestExtractionCsvFile()
        {
            try
            {
                string resourcesDir = System.IO.Path.Combine(Application.dataPath, "Resources");
                if (!System.IO.Directory.Exists(resourcesDir))
                {
                    Debug.LogError($"[PowerlineExtractionSceneBuilder] Resources目录不存在: {resourcesDir}");
                    return "";
                }
                
                // 查找所有电力线提取生成的CSV文件
                string[] extractionCsvFiles = System.IO.Directory.GetFiles(resourcesDir, "tower_centers_*.csv");
                
                if (extractionCsvFiles.Length == 0)
                {
                    Debug.LogWarning("[PowerlineExtractionSceneBuilder] 未找到任何电力线提取生成的CSV文件");
                    return "";
                }
                
                // 按修改时间排序，返回最新的
                var latestFile = extractionCsvFiles.OrderByDescending(f => System.IO.File.GetLastWriteTime(f)).First();
                
                Debug.Log($"[PowerlineExtractionSceneBuilder] 找到最新的提取CSV文件: {latestFile}");
                Debug.Log($"[PowerlineExtractionSceneBuilder] 文件修改时间: {System.IO.File.GetLastWriteTime(latestFile)}");
                
                return latestFile;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PowerlineExtractionSceneBuilder] 查找最新CSV文件时出错: {ex.Message}");
                return "";
            }
        }
        
        /// <summary>
        /// 测试缩放功能（用于调试）
        /// </summary>
        [System.Obsolete("仅用于调试测试")]
        public void TestScaling()
        {
            Debug.Log($"[PowerlineExtractionSceneBuilder] === 缩放功能测试 ===");
            Debug.Log($"[PowerlineExtractionSceneBuilder] 启用自动缩放: {enableAutoScaling}");
            Debug.Log($"[PowerlineExtractionSceneBuilder] 手动缩放因子: {manualScaleFactor}");
            Debug.Log($"[PowerlineExtractionSceneBuilder] 目标高度范围: {targetTowerHeightRange.x} - {targetTowerHeightRange.y}");
            Debug.Log($"[PowerlineExtractionSceneBuilder] 当前计算的缩放因子: {calculatedScaleFactor}");
            
            // 测试一些示例数据
            List<Vector3> testPositions = new List<Vector3>
            {
                new Vector3(1000f, 500f, 2000f),
                new Vector3(1100f, 520f, 2100f),
                new Vector3(1200f, 480f, 2200f)
            };
            
            List<float> testHeights = new List<float> { 500f, 520f, 480f };
            
            // 简单的缩放因子计算
            float averageHeight = testHeights.Average();
            float targetHeight = (targetTowerHeightRange.x + targetTowerHeightRange.y) * 0.5f;
            float testScaleFactor = targetHeight / Mathf.Max(averageHeight, 1f);
            testScaleFactor = Mathf.Clamp(testScaleFactor, 0.001f, 1000f);
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] 示例数据的缩放因子: {testScaleFactor:F6}");
            
            // 显示缩放后的结果
            for (int i = 0; i < testPositions.Count; i++)
            {
                Vector3 original = testPositions[i];
                Vector3 scaled = original * testScaleFactor;
                Debug.Log($"[PowerlineExtractionSceneBuilder] 示例 {i+1}: ({original.x:F1}, {original.y:F1}, {original.z:F1}) → ({scaled.x:F2}, {scaled.y:F2}, {scaled.z:F2})");
            }
        }
        
        #region 树木管理
        
        /// <summary>
        /// 加载简化树木数据
        /// </summary>
        private List<SimpleTreeData> LoadSimpleTreeData()
        {
            List<SimpleTreeData> trees = new List<SimpleTreeData>();
            
            if (!enableTreePlacement) 
            {
                Debug.LogWarning("[PowerlineExtractionSceneBuilder] 树木放置功能未启用！");
                return trees;
            }
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] 开始加载树木数据，CSV文件名: {treeCsvFileName}");
            
            // 加载CSV文件
            TextAsset csvFile = Resources.Load<TextAsset>(treeCsvFileName);
            if (csvFile == null)
            {
                Debug.LogError($"[PowerlineExtractionSceneBuilder] 无法找到树木CSV文件 {treeCsvFileName}，跳过树木放置");
                Debug.LogError("[PowerlineExtractionSceneBuilder] 请确保CSV文件位于Resources文件夹中");
                return trees;
            }
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] 成功加载CSV文件，文件大小: {csvFile.text.Length} 字符");
            
            // 获取电塔高度信息
            Dictionary<int, float> towerHeights = GetTowerHeights();
            Debug.Log($"[PowerlineExtractionSceneBuilder] 获取到 {towerHeights.Count} 座电塔的高度信息");
            
            // 解析CSV数据
            string[] lines = csvFile.text.Split('\n');
            Debug.Log($"[PowerlineExtractionSceneBuilder] CSV文件包含 {lines.Length} 行数据");
            
            // 跳过标题行
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                string[] values = line.Split(',');
                if (values.Length >= 6)
                {
                    if (int.TryParse(values[0], out int treeId) &&
                        int.TryParse(values[1], out int groupId) &&
                        int.TryParse(values[2], out int towerId) &&
                        float.TryParse(values[3], out float x) &&
                        float.TryParse(values[4], out float y) &&
                        float.TryParse(values[5], out float z))
                    {
                        // 坐标转换：X,Y→Unity的X,Z，Z→Y（高度）
                        Vector3 position = new Vector3(x, z, y);
                        string treeType = values.Length > 6 ? values[6] : "Tree";
                        
                        // 计算树木高度（基于电塔高度的比例）
                        float treeHeight = CalculateTreeHeightBasedOnTower(towerId, towerHeights);
                        
                        // 计算缩放比例
                        float scale = UnityEngine.Random.Range(0.8f, 1.2f);
                        
                        SimpleTreeData treeData = new SimpleTreeData(treeId, position, treeHeight, groupId, towerId, treeType, scale);
                        trees.Add(treeData);
                        
                        // 每10棵树输出一次调试信息
                        if (trees.Count % 10 == 0)
                        {
                            Debug.Log($"[PowerlineExtractionSceneBuilder] 已加载 {trees.Count} 棵树，最新: ID={treeId}, 位置=({x},{y},{z}), 组={groupId}, 塔={towerId}, 高度={treeHeight:F2}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[PowerlineExtractionSceneBuilder] 第 {i} 行数据解析失败: {line}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[PowerlineExtractionSceneBuilder] 第 {i} 行数据列数不足: {line}");
                }
            }
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] 成功加载 {trees.Count} 棵简化树木数据");
            return trees;
        }
        
        /// <summary>
        /// 从简化数据放置树木（仿照电塔的PlaceTowersFromSimplifiedInput方式）
        /// </summary>
        private List<GameObject> PlaceTreesFromSimplifiedInput()
        {
            Debug.Log("[PowerlineExtractionSceneBuilder] 开始执行树木放置...");
            
            List<SimpleTreeData> trees = LoadSimpleTreeData();
            List<GameObject> createdTrees = new List<GameObject>();
            
            if (trees.Count == 0) 
            {
                Debug.LogWarning("[PowerlineExtractionSceneBuilder] 没有树木数据可供放置");
                return createdTrees;
            }
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] 准备放置 {trees.Count} 棵树");
            
            // 清理已放置的树木
            ClearPlacedTrees();
            
            // 如果没有指定树木预制件，尝试从Resources加载
            if (treePrefab == null)
            {
                Debug.Log("[PowerlineExtractionSceneBuilder] 树木预制件未指定，尝试从Resources加载...");
                treePrefab = Resources.Load<GameObject>("Prefabs/Tree");
                if (treePrefab == null)
                {
                    Debug.LogError("[PowerlineExtractionSceneBuilder] 无法找到Tree预制件，跳过树木放置");
                    Debug.LogError("[PowerlineExtractionSceneBuilder] 请确保Tree.prefab位于Resources/Prefabs/文件夹中");
                    return createdTrees;
                }
                Debug.Log("[PowerlineExtractionSceneBuilder] 成功加载Tree预制件");
            }
            else
            {
                Debug.Log("[PowerlineExtractionSceneBuilder] 使用已指定的树木预制件");
            }
            
            int successCount = 0;
            int failCount = 0;
            
            foreach (var treeData in trees)
            {
                GameObject tree = CreateTreeAtPosition(treeData);
                if (tree != null)
                {
                    createdTrees.Add(tree);
                    placedTrees.Add(tree);
                    successCount++;
                    
                    // 每10棵树输出一次进度
                    if (successCount % 10 == 0)
                    {
                        Debug.Log($"[PowerlineExtractionSceneBuilder] 已成功放置 {successCount} 棵树");
                    }
                }
                else
                {
                    failCount++;
                    Debug.LogWarning($"[PowerlineExtractionSceneBuilder] 第 {treeData.treeId} 棵树创建失败");
                }
            }
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] 树木放置完成！成功: {successCount}, 失败: {failCount}");
            Debug.Log($"[PowerlineExtractionSceneBuilder] 总共放置了 {placedTrees.Count} 棵树");
            
            return createdTrees;
        }
        
        /// <summary>
        /// 在指定位置创建树木（仿照电塔的CreateTowerAtPosition方式）
        /// </summary>
        private GameObject CreateTreeAtPosition(SimpleTreeData treeData)
        {
            if (treePrefab == null) 
            {
                Debug.LogError("[PowerlineExtractionSceneBuilder] 树木预制件为空，无法创建树木");
                return null;
            }
            
            Vector3 position = treeData.position;
            Debug.Log($"[PowerlineExtractionSceneBuilder] 创建树木 ID={treeData.treeId}, 原始位置=({position.x:F2}, {position.y:F2}, {position.z:F2})");
            
            // 地形适配：调整树木基座高度
            if (terrainManager != null)
            {
                float terrainHeight = terrainManager.GetTerrainHeight(position);
                position.y = Mathf.Max(position.y, terrainHeight);
                Debug.Log($"[PowerlineExtractionSceneBuilder] 地形高度: {terrainHeight:F2}, 调整后Y坐标: {position.y:F2}");
            }
            else
            {
                Debug.LogWarning("[PowerlineExtractionSceneBuilder] 地形管理器未找到，跳过地形适配");
            }
            
            // 添加随机偏移，避免树木完全重叠
            float randomOffsetX = UnityEngine.Random.Range(-2f, 2f);
            float randomOffsetZ = UnityEngine.Random.Range(-2f, 2f);
            position += new Vector3(randomOffsetX, 0, randomOffsetZ);
            Debug.Log($"[PowerlineExtractionSceneBuilder] 随机偏移: ({randomOffsetX:F2}, 0, {randomOffsetZ:F2}), 最终位置: ({position.x:F2}, {position.y:F2}, {position.z:F2})");
            
            // 实例化树木
            GameObject tree = Instantiate(treePrefab, position, Quaternion.identity);
            if (tree == null)
            {
                Debug.LogError("[PowerlineExtractionSceneBuilder] 树木实例化失败");
                return null;
            }
            
            // 设置名称
            tree.name = $"Tree_{treeData.treeId}_Group{treeData.groupId}_Tower{treeData.towerId}";
            
            // 随机旋转
            float randomRotation = UnityEngine.Random.Range(0f, 360f);
            tree.transform.rotation = Quaternion.Euler(0, randomRotation, 0);
            
            // 应用缩放
            tree.transform.localScale = Vector3.one * treeData.scale * treeBaseScale;
            Debug.Log($"[PowerlineExtractionSceneBuilder] 树木 {tree.name} 创建成功，缩放: {treeData.scale:F2}, 旋转: {randomRotation:F1}°");
            
            // 应用自动缩放（如果启用）
            if (enableTreeAutoScaling)
            {
                ApplyTreeAutoScaling(tree);
                Debug.Log($"[PowerlineExtractionSceneBuilder] 已应用自动缩放");
            }
            
            // 设置父对象
            if (towerParent != null)
            {
                tree.transform.SetParent(towerParent);
                Debug.Log($"[PowerlineExtractionSceneBuilder] 树木已设置父对象: {towerParent.name}");
            }
            else
            {
                Debug.LogWarning("[PowerlineExtractionSceneBuilder] 未设置树木父对象");
            }
            
            return tree;
        }
        
        /// <summary>
        /// 加载树木数据
        /// </summary>
        private void LoadTreeData()
        {
            if (!enableTreePlacement) return;
            
            treeDataList.Clear();
            
            // 加载CSV文件
            TextAsset csvFile = Resources.Load<TextAsset>(treeCsvFileName);
            if (csvFile == null)
            {
                Debug.LogWarning($"[PowerlineExtractionSceneBuilder] 无法找到树木CSV文件 {treeCsvFileName}，跳过树木放置");
                return;
            }
            
            // 解析CSV数据
            string[] lines = csvFile.text.Split('\n');
            
            // 跳过标题行
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                string[] values = line.Split(',');
                if (values.Length >= 6)
                {
                    TreeData treeData = new TreeData();
                    
                    if (int.TryParse(values[0], out treeData.treeId) &&
                        int.TryParse(values[1], out treeData.groupId) &&
                        int.TryParse(values[2], out treeData.towerId) &&
                        float.TryParse(values[3], out float x) &&
                        float.TryParse(values[4], out float y) &&
                        float.TryParse(values[5], out float z))
                    {
                        // 坐标转换：X,Y→Unity的X,Z，Z→Y（高度）
                        treeData.position = new Vector3(x, z, y);
                        treeData.treeType = values.Length > 6 ? values[6] : "Tree";
                        
                        treeDataList.Add(treeData);
                    }
                }
            }
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] 成功加载 {treeDataList.Count} 棵树的数据");
        }
        
        /// <summary>
        /// 在电塔附近放置树木
        /// </summary>
        private void PlaceTreesNearTowers(List<Vector3> towerPositions)
        {
            if (!enableTreePlacement || treeDataList.Count == 0) return;
            
            // 清理已放置的树木
            ClearPlacedTrees();
            
            // 如果没有指定树木预制件，尝试从Resources加载
            if (treePrefab == null)
            {
                treePrefab = Resources.Load<GameObject>("Prefabs/Tree");
                if (treePrefab == null)
                {
                    Debug.LogWarning("[PowerlineExtractionSceneBuilder] 无法找到Tree预制件，跳过树木放置");
                    return;
                }
            }
            
            int treesPlaced = 0;
            
            // 为每个电塔放置树木
            foreach (Vector3 towerPos in towerPositions)
            {
                // 放置CSV中定义的树木
                var treesForThisTower = treeDataList.Where(t => 
                    Vector3.Distance(t.position, towerPos) < 50f).ToList();
                
                foreach (var treeData in treesForThisTower)
                {
                    if (treesPlaced >= treeDataList.Count) break;
                    
                    PlaceSingleTree(treeData);
                    treesPlaced++;
                }
                
                // 在电塔附近添加额外的随机树木
                for (int i = 0; i < treesPerTower; i++)
                {
                    if (treesPlaced >= treeDataList.Count + treesPerTower * towerPositions.Count) break;
                    
                    PlaceRandomTreeNearTower(towerPos);
                    treesPlaced++;
                }
            }
            
            Debug.Log($"[PowerlineExtractionSceneBuilder] 成功放置了 {placedTrees.Count} 棵树");
        }
        
        /// <summary>
        /// 放置单棵树
        /// </summary>
        private void PlaceSingleTree(TreeData treeData)
        {
            Vector3 finalPosition = CalculateTreePosition(treeData.position);
            
            // 实例化树
            GameObject tree = Instantiate(treePrefab, finalPosition, Quaternion.identity);
            
            // 设置父对象
            if (towerParent != null)
            {
                tree.transform.SetParent(towerParent);
            }
            
            // 随机旋转
            tree.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
            
            // 随机缩放
            float scale = UnityEngine.Random.Range(0.8f, 1.2f);
            tree.transform.localScale = Vector3.one * scale * treeBaseScale;
            
            // 应用自动缩放
            if (enableTreeAutoScaling)
            {
                ApplyTreeAutoScaling(tree);
            }
            
            // 设置名称
            tree.name = $"Tree_{treeData.treeId}_Group{treeData.groupId}_Tower{treeData.towerId}";
            
            // 添加到已放置列表
            placedTrees.Add(tree);
        }
        
        /// <summary>
        /// 在电塔附近放置随机树木
        /// </summary>
        private void PlaceRandomTreeNearTower(Vector3 towerPos)
        {
            // 计算随机位置
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(minTreeDistanceFromTower, maxTreeDistanceFromTower);
            
            Vector3 treePos = towerPos + new Vector3(
                Mathf.Cos(angle) * distance,
                0,
                Mathf.Sin(angle) * distance
            );
            
            // 地形适配
            if (terrainManager != null)
            {
                float terrainHeight = terrainManager.GetTerrainHeight(treePos);
                treePos.y = Mathf.Max(treePos.y, terrainHeight);
            }
            
            // 检查是否与现有树木重叠
            if (IsTreePositionOccupied(treePos, 2f)) return;
            
            // 实例化树
            GameObject tree = Instantiate(treePrefab, treePos, Quaternion.identity);
            
            // 设置父对象
            if (towerParent != null)
            {
                tree.transform.SetParent(towerParent);
            }
            
            // 随机旋转和缩放
            tree.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
            float scale = UnityEngine.Random.Range(0.8f, 1.2f);
            tree.transform.localScale = Vector3.one * scale * treeBaseScale;
            
            // 应用自动缩放
            if (enableTreeAutoScaling)
            {
                ApplyTreeAutoScaling(tree);
            }
            
            // 设置名称
            tree.name = $"RandomTree_Tower_{placedTrees.Count}";
            
            // 添加到已放置列表
            placedTrees.Add(tree);
        }
        
        /// <summary>
        /// 计算树木的最终位置
        /// </summary>
        private Vector3 CalculateTreePosition(Vector3 basePosition)
        {
            // 添加随机偏移，避免树木完全重叠
            float randomOffsetX = UnityEngine.Random.Range(-2f, 2f);
            float randomOffsetZ = UnityEngine.Random.Range(-2f, 2f);
            
            Vector3 finalPosition = basePosition + new Vector3(randomOffsetX, 0, randomOffsetZ);
            
            // 如果启用了地形适配，调整高度
            if (terrainManager != null)
            {
                float terrainHeight = terrainManager.GetTerrainHeight(finalPosition);
                finalPosition.y = Mathf.Max(finalPosition.y, terrainHeight);
            }
            
            return finalPosition;
        }
        
        /// <summary>
        /// 应用树木自动缩放
        /// </summary>
        private void ApplyTreeAutoScaling(GameObject tree)
        {
            // 获取树的当前高度
            Bounds bounds = GetTreeBounds(tree);
            float currentHeight = bounds.size.y;
            
            if (currentHeight > 0)
            {
                // 尝试从树木名称中提取电塔ID，以获取电塔高度
                int towerId = ExtractTowerIdFromTreeName(tree.name);
                float targetHeight = 0f;
                
                                 if (towerId > 0)
                 {
                     // 获取电塔高度信息
                     Dictionary<int, float> towerHeights = GetTowerHeights();
                     if (towerHeights.TryGetValue(towerId, out float towerHeight))
                     {
                         // 基础比例：树木高度为电塔高度的20%-80%
                         float baseHeightRatio = UnityEngine.Random.Range(treeHeightRatioRange.x, treeHeightRatioRange.y);
                         float baseTargetHeight = towerHeight * baseHeightRatio;
                         
                         // 添加随机变化
                         if (enableTreeHeightVariation)
                         {
                             float variationRatio = UnityEngine.Random.Range(treeHeightVariationRange.x, treeHeightVariationRange.y);
                             targetHeight = baseTargetHeight * variationRatio;
                         }
                         else
                         {
                             targetHeight = baseTargetHeight;
                         }
                         
                         // 确保在合理范围内
                         targetHeight = Mathf.Clamp(targetHeight, 1f, towerHeight * 0.9f);
                         
                         Debug.Log($"[PowerlineExtractionSceneBuilder] 自动缩放: 电塔 {towerId} 高度={towerHeight:F2}, 基础目标高度={baseTargetHeight:F2}, 最终目标高度={targetHeight:F2}");
                     }
                 }
                
                // 如果无法获取电塔高度，使用默认范围
                if (targetHeight <= 0)
                {
                    targetHeight = UnityEngine.Random.Range(10f, 20f);
                    Debug.LogWarning($"[PowerlineExtractionSceneBuilder] 无法获取电塔高度，使用默认目标高度: {targetHeight:F2}");
                }
                
                // 计算缩放比例
                float scaleRatio = targetHeight / currentHeight;
                // 移除缩放限制，允许树木更大
                scaleRatio = Mathf.Clamp(scaleRatio, 0.1f, 10f); // 扩大缩放范围
                
                // 应用缩放：基于treeBaseScale重新计算，而不是乘以scaleRatio
                // 这样可以确保树木保持我们设置的基础大小
                Vector3 baseScale = Vector3.one * treeBaseScale;
                tree.transform.localScale = baseScale * scaleRatio;
                
                Debug.Log($"[PowerlineExtractionSceneBuilder] 自动缩放: 目标高度={targetHeight:F2}, 当前高度={currentHeight:F2}, 缩放比例={scaleRatio:F2}, 最终缩放={tree.transform.localScale}");
            }
        }
        
        /// <summary>
        /// 获取电塔高度信息
        /// </summary>
        private Dictionary<int, float> GetTowerHeights()
        {
            Dictionary<int, float> towerHeights = new Dictionary<int, float>();
            
            // 查找所有电塔
            GameObject[] towers = FindTowersSafely();
            
            foreach (GameObject tower in towers)
            {
                if (tower != null)
                {
                    // 尝试从电塔名称中提取ID
                    if (int.TryParse(ExtractTowerIdFromName(tower.name), out int towerId))
                    {
                        // 获取电塔的实际高度（从Renderer的bounds）
                        Renderer[] renderers = tower.GetComponentsInChildren<Renderer>();
                        float towerHeight = 0f;
                        
                        if (renderers.Length > 0)
                        {
                            Bounds bounds = renderers[0].bounds;
                            for (int i = 1; i < renderers.Length; i++)
                            {
                                bounds.Encapsulate(renderers[i].bounds);
                            }
                            towerHeight = bounds.size.y;
                        }
                        
                        if (towerHeight > 0)
                        {
                            towerHeights[towerId] = towerHeight;
                            Debug.Log($"[PowerlineExtractionSceneBuilder] 电塔 {towerId}: 高度 = {towerHeight:F2}");
                        }
                    }
                }
            }
            
            return towerHeights;
        }
        
        /// <summary>
        /// 从电塔名称中提取ID
        /// </summary>
        private string ExtractTowerIdFromName(string towerName)
        {
            // 尝试从名称中提取数字ID
            // 例如: "Tower_1", "GoodTower_2", "Tower1" 等
            string[] parts = towerName.Split('_');
            if (parts.Length > 1)
            {
                return parts[parts.Length - 1];
            }
            
            // 如果没有下划线，尝试提取末尾的数字
            string result = "";
            for (int i = towerName.Length - 1; i >= 0; i--)
            {
                if (char.IsDigit(towerName[i]))
                {
                    result = towerName[i] + result;
                }
                else
                {
                    break;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 从树木名称中提取电塔ID
        /// </summary>
        private int ExtractTowerIdFromTreeName(string treeName)
        {
            // 树木名称格式通常是: "Tree_1_Group1_Tower2" 或 "RandomTree_Tower_3"
            if (treeName.Contains("Tower"))
            {
                string[] parts = treeName.Split('_');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].ToLower() == "tower" && i + 1 < parts.Length)
                    {
                        // 下一个部分应该是电塔ID
                        if (int.TryParse(parts[i + 1], out int towerId))
                        {
                            return towerId;
                        }
                    }
                }
            }
            
            return -1; // 无法提取
        }
        
        /// <summary>
        /// 基于电塔高度计算树木高度
        /// </summary>
        private float CalculateTreeHeightBasedOnTower(int towerId, Dictionary<int, float> towerHeights)
        {
            if (towerHeights.TryGetValue(towerId, out float towerHeight))
            {
                // 基础比例：树木高度为电塔高度的20%-80%（扩大范围）
                float baseHeightRatio = UnityEngine.Random.Range(treeHeightRatioRange.x, treeHeightRatioRange.y);
                float baseTreeHeight = towerHeight * baseHeightRatio;
                
                // 添加随机变化：在基础高度上增加±30%的变化
                float finalTreeHeight = baseTreeHeight;
                if (enableTreeHeightVariation)
                {
                    float variationRatio = UnityEngine.Random.Range(treeHeightVariationRange.x, treeHeightVariationRange.y);
                    finalTreeHeight = baseTreeHeight * variationRatio;
                }
                
                // 确保树木高度在合理范围内（最小1米，最大不超过电塔高度的90%）
                finalTreeHeight = Mathf.Clamp(finalTreeHeight, 1f, towerHeight * 0.9f);
                
                Debug.Log($"[PowerlineExtractionSceneBuilder] 电塔 {towerId}: 高度={towerHeight:F2}, 基础比例={baseHeightRatio:F2}, 基础高度={baseTreeHeight:F2}, 最终高度={finalTreeHeight:F2}");
                return finalTreeHeight;
            }
            else
            {
                // 如果找不到电塔高度，使用更大的默认范围
                float defaultHeight = UnityEngine.Random.Range(5f, 25f);
                Debug.LogWarning($"[PowerlineExtractionSceneBuilder] 未找到电塔 {towerId} 的高度信息，使用默认高度: {defaultHeight:F2}");
                return defaultHeight;
            }
        }
        
        /// <summary>
        /// 获取树的边界
        /// </summary>
        private Bounds GetTreeBounds(GameObject tree)
        {
            Renderer renderer = tree.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds;
            }
            
            // 如果没有Renderer，尝试获取所有子对象的边界
            Renderer[] renderers = tree.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
                return bounds;
            }
            
            // 默认边界
            return new Bounds(tree.transform.position, Vector3.one);
        }
        
        /// <summary>
        /// 检查树木位置是否被占用
        /// </summary>
        private bool IsTreePositionOccupied(Vector3 position, float minDistance)
        {
            foreach (GameObject tree in placedTrees)
            {
                if (tree != null && Vector3.Distance(tree.transform.position, position) < minDistance)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 清理已放置的树木
        /// </summary>
        private void ClearPlacedTrees()
        {
            foreach (GameObject tree in placedTrees)
            {
                if (tree != null)
                {
                    DestroyImmediate(tree);
                }
            }
            placedTrees.Clear();
        }
        
        /// <summary>
        /// 重新加载和放置树木
        /// </summary>
        [ContextMenu("重新加载树木")]
        public void ReloadTrees()
        {
            // 使用新的简化树木构建方式
            PlaceTreesFromSimplifiedInput();
        }
        
        /// <summary>
        /// 清除所有树木
        /// </summary>
        [ContextMenu("清除所有树木")]
        public void ClearAllTrees()
        {
            ClearPlacedTrees();
        }
        

        
        /// <summary>
        /// 测试树木系统（用于调试）
        /// </summary>
        [ContextMenu("测试树木系统")]
        public void TestTreeSystem()
        {
            Debug.Log("[PowerlineExtractionSceneBuilder] === 树木系统测试 ===");
            Debug.Log($"[PowerlineExtractionSceneBuilder] 启用树木放置: {enableTreePlacement}");
            Debug.Log($"[PowerlineExtractionSceneBuilder] 树木CSV文件名: {treeCsvFileName}");
            Debug.Log($"[PowerlineExtractionSceneBuilder] 树木预制件: {(treePrefab != null ? treePrefab.name : "未设置")}");
            Debug.Log($"[PowerlineExtractionSceneBuilder] 树木高度比例范围: {treeHeightRatioRange.x * 100:F0}% - {treeHeightRatioRange.y * 100:F0}% (相对于电塔高度)");
            Debug.Log($"[PowerlineExtractionSceneBuilder] 树木高度变化范围: {treeHeightVariationRange.x * 100:F0}% - {treeHeightVariationRange.y * 100:F0}% (相对于计算高度)");
            Debug.Log($"[PowerlineExtractionSceneBuilder] 启用树木高度变化: {enableTreeHeightVariation}");
            
            // 测试加载树木数据
            var trees = LoadSimpleTreeData();
            Debug.Log($"[PowerlineExtractionSceneBuilder] 测试加载树木数据: {trees.Count} 棵");
            
            // 测试放置树木
            if (trees.Count > 0)
            {
                var createdTrees = PlaceTreesFromSimplifiedInput();
                Debug.Log($"[PowerlineExtractionSceneBuilder] 测试放置树木: {createdTrees.Count} 棵");
            }
        }
        
        /// <summary>
        /// 手动触发从CSV构建树木（用于测试）
        /// </summary>
        [ContextMenu("手动构建树木")]
        public void BuildTreesFromCsv()
        {
            Debug.Log("[PowerlineExtractionSceneBuilder] 手动触发树木构建...");
            
            if (!enableTreePlacement)
            {
                Debug.LogWarning("[PowerlineExtractionSceneBuilder] 树木放置功能未启用，请先启用enableTreePlacement");
                return;
            }
            
            var createdTrees = PlaceTreesFromSimplifiedInput();
            Debug.Log($"[PowerlineExtractionSceneBuilder] 手动构建完成，创建了 {createdTrees.Count} 棵树");
        }
        
        /// <summary>
        /// 检查树木系统状态（用于调试）
        /// </summary>
        [ContextMenu("检查树木系统状态")]
        public void CheckTreeSystemStatus()
        {
            Debug.Log("=== 树木系统状态检查 ===");
            Debug.Log($"enableTreePlacement: {enableTreePlacement}");
            Debug.Log($"treePrefab: {(treePrefab != null ? treePrefab.name : "null")}");
            Debug.Log($"treeCsvFileName: {treeCsvFileName}");
            Debug.Log($"terrainManager: {(terrainManager != null ? terrainManager.name : "null")}");
            Debug.Log($"towerParent: {(towerParent != null ? towerParent.name : "null")}");
            Debug.Log($"已放置树木数量: {placedTrees.Count}");
            
            // 检查CSV文件
            TextAsset csvFile = Resources.Load<TextAsset>(treeCsvFileName);
            if (csvFile != null)
            {
                Debug.Log($"CSV文件存在，大小: {csvFile.text.Length} 字符");
                string[] lines = csvFile.text.Split('\n');
                Debug.Log($"CSV文件行数: {lines.Length}");
            }
            else
            {
                Debug.LogError($"CSV文件不存在: {treeCsvFileName}");
            }
            
            // 检查预制件
            if (treePrefab == null)
            {
                GameObject loadedPrefab = Resources.Load<GameObject>("Prefabs/Tree");
                Debug.Log($"从Resources加载的预制件: {(loadedPrefab != null ? loadedPrefab.name : "null")}");
            }
            
            Debug.Log("=== 状态检查完成 ===");
        }
        
        #endregion
    }
} 