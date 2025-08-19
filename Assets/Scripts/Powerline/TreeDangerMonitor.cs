using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// 树木危险监测系统
/// 自动检测树木对电力线的危险，考虑树木生长速度、高度等因素
/// </summary>
public class TreeDangerMonitor : MonoBehaviour
{
    [Header("监测设置")]
    public bool enableAutoMonitoring = true;
    public float monitoringInterval = 5f;
    public float maxDetectionDistance = 100f;
    
    [Header("危险评估参数")]
    public float criticalDistance = 10f;  // 从1f改为10f - 危险距离
    public float warningDistance = 30f;   // 从3f改为30f - 警告距离
    public float safeDistance = 50f;      // 从5f改为50f - 安全距离
    
    [Header("树木生长参数")]
    public float baseGrowthRate = 0.1f;
    public float maxTreeHeight = 50f;
    public float seasonalGrowthFactor = 0.2f;
    
    [Header("电力线安全参数")]
    public float powerlineHeight = 20f;
    public float powerlineSag = 2f;
    public float windSwayFactor = 1.5f;
    
    private List<PowerlineInteraction> powerlines = new List<PowerlineInteraction>();
    private List<GameObject> trees = new List<GameObject>();
    private List<TreeDangerInfo> treeDangerList = new List<TreeDangerInfo>();
    private float lastMonitoringTime = 0f;
    
    public enum TreeDangerLevel
    {
        Safe = 0,
        Warning = 1,
        Critical = 2,
        Emergency = 3
    }
    
    [System.Serializable]
    public class TreeDangerInfo
    {
        public GameObject tree;
        public PowerlineInteraction powerline;
        public float currentDistance;
        public float projectedDistance;
        public TreeDangerLevel dangerLevel;
        public float treeHeight;
        public float growthRate;
        public DateTime lastAssessment;
        public Vector3 dangerPoint;
        public string riskDescription;
        
        // 新增：位置记录字段
        public Vector3 treePosition;
        public Vector3 powerlinePosition;
        public string treeName;
        public string powerlineName;
        public string towerGroup;
        public string towerNumber;
        
        // 新增：时间预测字段
        public float oneYearDistance;        // 一年后的距离
        public float threeYearDistance;      // 三年后的距离
        public TreeDangerLevel oneYearDangerLevel;    // 一年后的危险等级
        public TreeDangerLevel threeYearDangerLevel;  // 三年后的危险等级
        public string oneYearRiskDescription;         // 一年后的风险描述
        public string threeYearRiskDescription;       // 三年后的风险描述
        public bool willBeDangerousInOneYear;         // 一年后是否危险
        public bool willBeDangerousInThreeYears;      // 三年后是否危险
        
        public TreeDangerInfo(GameObject treeObj, PowerlineInteraction powerlineObj)
        {
            tree = treeObj;
            powerline = powerlineObj;
            lastAssessment = DateTime.Now;
            
            // 记录位置信息
            if (treeObj != null)
            {
                treePosition = treeObj.transform.position;
                treeName = treeObj.name;
                ParseTreeName(treeObj.name);
            }
            
            if (powerlineObj != null)
            {
                powerlinePosition = powerlineObj.transform.position;
                powerlineName = powerlineObj.name;
            }
        }
        
        /// <summary>
        /// 解析树木名称，提取组别和电塔编号
        /// </summary>
        private void ParseTreeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            
            // 解析格式：Tree_[编号]_Group10_Tower[编号]
            if (name.Contains("Group") && name.Contains("Tower"))
            {
                try
                {
                    var parts = name.Split('_');
                    if (parts.Length >= 4)
                    {
                        towerGroup = parts[2]; // Group10
                        towerNumber = parts[3]; // Tower[编号]
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"解析树木名称失败: {name}, 错误: {e.Message}");
                }
            }
        }
    }
    
    void Start()
    {
        // 延迟启动，确保场景已完全加载
        Invoke(nameof(InitializeMonitoring), 2f);
        
        // 测试时间预测功能
        Invoke(nameof(TestTimePrediction), 5f);
    }
    
    void TestTimePrediction()
    {
        Debug.Log("=== 测试时间预测功能 ===");
        if (treeDangerList.Count > 0)
        {
            var predictionStats = GetTimePredictionStatistics();
            Debug.Log($"时间预测统计: {predictionStats.Count} 项");
            
            var oneYearDangerous = GetOneYearDangerousTrees();
            var threeYearDangerous = GetThreeYearDangerousTrees();
            
            Debug.Log($"一年后危险树木: {oneYearDangerous.Count}棵");
            Debug.Log($"三年后危险树木: {threeYearDangerous.Count}棵");
            
            string report = GetTreeGrowthTrendReport();
            Debug.Log("生长趋势报告:");
            Debug.Log(report);
        }
        else
        {
            Debug.Log("暂无监测数据，无法测试时间预测功能");
        }
    }
    
    void Update()
    {
        if (enableAutoMonitoring && Time.time - lastMonitoringTime >= monitoringInterval)
        {
            PerformMonitoring();
            lastMonitoringTime = Time.time;
        }
    }
    
    void InitializeMonitoring()
    {
        Debug.Log("初始化树木危险监测系统...");
        FindPowerlines();
        FindTrees();
        Debug.Log($"监测系统初始化完成 - 电力线: {powerlines.Count}, 树木: {trees.Count}");
    }
    
    void FindPowerlines()
    {
        powerlines.Clear();
        
        Debug.Log("=== 开始查找电力线 ===");
        
        // 方法1：查找所有PowerlineInteraction组件（最可靠）
        Debug.Log("方法1：查找PowerlineInteraction组件...");
        var foundPowerlines = FindObjectsOfType<PowerlineInteraction>();
        Debug.Log($"找到 {foundPowerlines.Length} 个PowerlineInteraction组件");
        
        int validCount = 0;
        foreach (var powerline in foundPowerlines)
        {
            if (powerline != null && powerline.enabled && powerline.gameObject.activeInHierarchy)
            {
                powerlines.Add(powerline);
                validCount++;
                Debug.Log($"有效电力线: {powerline.name} 在位置 {powerline.transform.position}");
            }
            else
            {
                Debug.LogWarning($"无效电力线: {powerline?.name ?? "null"} - enabled: {powerline?.enabled}, active: {powerline?.gameObject.activeInHierarchy}");
            }
        }
        
        Debug.Log($"有效PowerlineInteraction电力线数量: {validCount}");
        
        // 方法2：通过父对象查找电力线（参考电塔查找逻辑）
        Debug.Log("方法2：通过父对象查找电力线...");
        int parentFindCount = 0;
        
        // 查找PowerlineParent下的电力线
        GameObject powerlineParent = GameObject.Find("PowerlineParent");
        if (powerlineParent != null)
        {
            foreach (Transform child in powerlineParent.transform)
            {
                if (child != null && child.gameObject.activeInHierarchy)
                {
                    if (child.name.Contains("Powerline") || 
                        child.name.Contains("Wire") ||
                        child.name.Contains("Line"))
                    {
                        var powerlineComponent = child.GetComponent<PowerlineInteraction>();
                        if (powerlineComponent != null && powerlineComponent.enabled && !powerlines.Contains(powerlineComponent))
                        {
                            powerlines.Add(powerlineComponent);
                            parentFindCount++;
                            Debug.Log($"通过PowerlineParent找到电力线: {child.name} 在位置 {child.position}");
                        }
                    }
                }
            }
        }
        
        // 查找电力线父对象下的电力线
        GameObject powerlineParentObj = GameObject.Find("电力线");
        if (powerlineParentObj != null)
        {
            foreach (Transform child in powerlineParentObj.transform)
            {
                if (child != null && child.gameObject.activeInHierarchy)
                {
                    if (child.name.Contains("Powerline") || 
                        child.name.Contains("Wire") ||
                        child.name.Contains("Line"))
                    {
                        var powerlineComponent = child.GetComponent<PowerlineInteraction>();
                        if (powerlineComponent != null && powerlineComponent.enabled && !powerlines.Contains(powerlineComponent))
                        {
                            powerlines.Add(powerlineComponent);
                            parentFindCount++;
                            Debug.Log($"通过电力线父对象找到电力线: {child.name} 在位置 {child.position}");
                        }
                    }
                }
            }
        }
        
        Debug.Log($"通过父对象找到 {parentFindCount} 条电力线");
        
        // 方法3：查找所有LineRenderer组件（可能包含电力线）
        Debug.Log("方法3：查找LineRenderer组件...");
        var lineRenderers = FindObjectsOfType<LineRenderer>();
        Debug.Log($"找到 {lineRenderers.Length} 个LineRenderer组件");
        
        int powerlineLineCount = 0;
        foreach (var lr in lineRenderers)
        {
            if (lr != null && lr.gameObject.activeInHierarchy)
            {
                if (lr.name.ToLower().Contains("powerline") || 
                    lr.name.ToLower().Contains("wire") ||
                    lr.name.ToLower().Contains("line"))
                {
                    var powerlineComponent = lr.GetComponent<PowerlineInteraction>();
                    if (powerlineComponent != null && powerlineComponent.enabled && !powerlines.Contains(powerlineComponent))
                    {
                        powerlines.Add(powerlineComponent);
                        powerlineLineCount++;
                        Debug.Log($"通过LineRenderer找到电力线: {lr.name} 在位置 {lr.transform.position}, 点数: {lr.positionCount}");
                    }
                }
            }
        }
        Debug.Log($"通过LineRenderer找到 {powerlineLineCount} 条电力线");
        
        // 方法4：通过标签查找（参考电塔查找逻辑）
        Debug.Log("方法4：通过Powerline标签查找...");
        try
        {
            GameObject[] taggedPowerlines = GameObject.FindGameObjectsWithTag("Powerline");
            int taggedCount = 0;
            foreach (var taggedPowerline in taggedPowerlines)
            {
                if (taggedPowerline != null && taggedPowerline.activeInHierarchy)
                {
                    var powerlineComponent = taggedPowerline.GetComponent<PowerlineInteraction>();
                    if (powerlineComponent != null && powerlineComponent.enabled && !powerlines.Contains(powerlineComponent))
                    {
                        powerlines.Add(powerlineComponent);
                        taggedCount++;
                        Debug.Log($"通过Powerline标签找到电力线: {taggedPowerline.name} 在位置 {taggedPowerline.transform.position}");
                    }
                }
            }
            Debug.Log($"通过Powerline标签找到 {taggedCount} 条电力线");
        }
        catch (UnityException e)
        {
            Debug.LogWarning($"Powerline标签未定义: {e.Message}");
        }
        
        // 方法5：通过SceneInitializer查找已创建的电力线
        Debug.Log("方法5：通过SceneInitializer查找已创建的电力线...");
        var sceneInitializer = FindObjectOfType<SceneInitializer>();
        if (sceneInitializer != null)
        {
            // 使用反射获取私有字段powerlines
            var powerlinesField = sceneInitializer.GetType().GetField("powerlines", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (powerlinesField != null)
            {
                var scenePowerlines = powerlinesField.GetValue(sceneInitializer) as List<SceneInitializer.PowerlineInfo>;
                if (scenePowerlines != null)
                {
                    Debug.Log($"SceneInitializer中有 {scenePowerlines.Count} 条电力线信息");
                }
            }
        }
        
        // 最终统计
        Debug.Log($"=== 电力线查找完成 ===");
        Debug.Log($"总共找到 {powerlines.Count} 条有效电力线");
        
        if (powerlines.Count == 0)
        {
            Debug.LogWarning("⚠️ 没有找到电力线！可能的原因：");
            Debug.LogWarning("1. 场景中没有电力线对象");
            Debug.LogWarning("2. 电力线对象没有PowerlineInteraction组件");
            Debug.LogWarning("3. 电力线对象被禁用或隐藏");
            Debug.LogWarning("4. 需要先运行SceneInitializer创建电力线");
            Debug.LogWarning("5. 电力线对象在不可见的父对象下");
            
            // 显示场景中所有对象的名称（前30个）
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            Debug.Log("场景中的对象名称（前30个）:");
            for (int i = 0; i < Mathf.Min(allObjects.Length, 30); i++)
            {
                var obj = allObjects[i];
                if (obj != null)
                {
                    string parentInfo = obj.transform.parent != null ? $" (父对象: {obj.transform.parent.name})" : "";
                    Debug.Log($"  {i}: {obj.name}{parentInfo} - 激活状态: {obj.activeInHierarchy}");
                }
            }
        }
        else
        {
            Debug.Log("找到的电力线列表:");
            for (int i = 0; i < Mathf.Min(powerlines.Count, 10); i++)
            {
                var powerline = powerlines[i];
                if (powerline != null)
                {
                    string parentInfo = powerline.transform.parent != null ? $" (父对象: {powerline.transform.parent.name})" : "";
                    Debug.Log($"  {i}: {powerline.name} 在 {powerline.transform.position}{parentInfo}");
                }
            }
            if (powerlines.Count > 10)
            {
                Debug.Log($"  ... 还有 {powerlines.Count - 10} 条电力线");
            }
        }
    }
    
    void FindTrees()
    {
        trees.Clear();
        
        Debug.Log("=== 开始查找树木 ===");
        
        // 方法1：通过Tree标签查找（最可靠）
        Debug.Log("方法1：通过Tree标签查找树木...");
        try
        {
            GameObject[] taggedTrees = GameObject.FindGameObjectsWithTag("Tree");
            int taggedCount = 0;
            foreach (var taggedTree in taggedTrees)
            {
                if (taggedTree != null && taggedTree.activeInHierarchy)
                {
                    trees.Add(taggedTree);
                    taggedCount++;
                    Debug.Log($"通过Tree标签找到树木: {taggedTree.name} 在位置 {taggedTree.transform.position}");
                }
            }
            Debug.Log($"通过Tree标签找到 {taggedCount} 棵树木");
        }
        catch (UnityException e)
        {
            Debug.LogWarning($"Tree标签未定义: {e.Message}");
        }
        
        // 方法2：通过Plant标签查找
        Debug.Log("方法2：通过Plant标签查找植物...");
        try
        {
            GameObject[] taggedPlants = GameObject.FindGameObjectsWithTag("Plant");
            int plantTagCount = 0;
            foreach (var taggedPlant in taggedPlants)
            {
                if (taggedPlant != null && taggedPlant.activeInHierarchy && !trees.Contains(taggedPlant))
                {
                    trees.Add(taggedPlant);
                    plantTagCount++;
                    Debug.Log($"通过Plant标签找到植物: {taggedPlant.name} 在位置 {taggedPlant.transform.position}");
                }
            }
            Debug.Log($"通过Plant标签找到 {plantTagCount} 棵植物");
        }
        catch (UnityException e)
        {
            Debug.LogWarning($"Plant标签未定义: {e.Message}");
        }
        
        // 方法3：通过精确的命名格式查找（针对Tree_XXX_Group1_TowerYY格式）
        Debug.Log("方法3：通过精确命名格式查找树木...");
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        Debug.Log($"场景中总对象数量: {allObjects.Length}");
        
        int exactNameMatchCount = 0;
        int generalNameMatchCount = 0;
        
        foreach (var obj in allObjects)
        {
            if (obj != null && obj.activeInHierarchy && !trees.Contains(obj))
            {
                string objName = obj.name;
                
                // 精确匹配：Tree_XXX_Group1_TowerYY格式
                if (objName.StartsWith("Tree_") && objName.Contains("_Group") && objName.Contains("_Tower"))
                {
                    trees.Add(obj);
                    exactNameMatchCount++;
                    Debug.Log($"通过精确命名格式找到树木: {objName} 在位置 {obj.transform.position}");
                }
                // 一般匹配：包含Tree关键词
                else if (objName.Contains("Tree") || 
                         objName.Contains("tree") ||
                         objName.Contains("植物") ||
                         objName.Contains("vegetation") ||
                         objName.Contains("Lemon") ||
                         objName.Contains("lemon"))
                {
                    trees.Add(obj);
                    generalNameMatchCount++;
                    Debug.Log($"通过一般名称找到树木: {objName} 在位置 {obj.transform.position}");
                }
            }
        }
        
        Debug.Log($"通过精确命名格式找到 {exactNameMatchCount} 棵树木");
        Debug.Log($"通过一般名称找到 {generalNameMatchCount} 棵树木");
        
        // 方法4：通过父对象查找（参考电塔查找逻辑）
        Debug.Log("方法4：通过父对象查找树木...");
        int parentFindCount = 0;
        
        // 查找PowerlineParent下的树木
        GameObject powerlineParent = GameObject.Find("PowerlineParent");
        if (powerlineParent != null)
        {
            foreach (Transform child in powerlineParent.transform)
            {
                if (child != null && child.gameObject.activeInHierarchy && !trees.Contains(child.gameObject))
                {
                    if (child.name.StartsWith("Tree_") || 
                        child.name.Contains("Tree") ||
                        child.name.Contains("tree") ||
                        child.name.Contains("植物") ||
                        child.name.Contains("vegetation"))
                    {
                        trees.Add(child.gameObject);
                        parentFindCount++;
                        Debug.Log($"通过PowerlineParent找到树木: {child.name} 在位置 {child.position}");
                    }
                }
            }
        }
        
        // 查找Plants父对象下的树木
        GameObject plantsParent = GameObject.Find("Plants");
        if (plantsParent != null)
        {
            foreach (Transform child in plantsParent.transform)
            {
                if (child != null && child.gameObject.activeInHierarchy && !trees.Contains(child.gameObject))
                {
                    if (child.name.StartsWith("Tree_") || 
                        child.name.Contains("Tree") ||
                        child.name.Contains("tree") ||
                        child.name.Contains("植物") ||
                        child.name.Contains("vegetation") ||
                        child.name.Contains("Lemon") ||
                        child.name.Contains("lemon"))
                    {
                        trees.Add(child.gameObject);
                        parentFindCount++;
                        Debug.Log($"通过Plants父对象找到树木: {child.name} 在位置 {child.position}");
                    }
                }
            }
        }
        
        Debug.Log($"通过父对象找到 {parentFindCount} 棵树木");
        
        // 方法5：通过组件查找（参考电塔查找逻辑）
        Debug.Log("方法5：通过组件查找树木...");
        var treeComponents = FindObjectsOfType<MonoBehaviour>().Where(mb => 
            mb != null && mb.gameObject.activeInHierarchy &&
            (mb.GetType().Name.ToLower().Contains("tree") ||
             mb.GetType().Name.ToLower().Contains("plant")));
        
        int componentCount = 0;
        foreach (var component in treeComponents)
        {
            if (component != null && !trees.Contains(component.gameObject))
            {
                trees.Add(component.gameObject);
                componentCount++;
                Debug.Log($"通过组件找到对象: {component.gameObject.name} (组件: {component.GetType().Name}) 在位置 {component.transform.position}");
            }
        }
        Debug.Log($"通过组件找到 {componentCount} 个对象");
        
        // 方法6：通过Resources目录查找树木预制件（参考电塔查找逻辑）
        Debug.Log("方法6：查找Resources中的树木预制件...");
        var treePrefabs = Resources.LoadAll<GameObject>("Prefabs");
        int prefabCount = 0;
        foreach (var prefab in treePrefabs)
        {
            if (prefab != null && 
                (prefab.name.ToLower().Contains("tree") || 
                 prefab.name.ToLower().Contains("植物")))
            {
                Debug.Log($"找到树木预制件: {prefab.name}");
                prefabCount++;
            }
        }
        Debug.Log($"在Resources/Prefabs中找到 {prefabCount} 个树木相关预制件");
        
        // 方法7：通过SceneInitializer查找已放置的树木
        Debug.Log("方法7：通过SceneInitializer查找已放置的树木...");
        var sceneInitializer = FindObjectOfType<SceneInitializer>();
        if (sceneInitializer != null)
        {
            // 使用反射获取私有字段placedTrees
            var placedTreesField = sceneInitializer.GetType().GetField("placedTrees", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (placedTreesField != null)
            {
                var placedTrees = placedTreesField.GetValue(sceneInitializer) as List<GameObject>;
                if (placedTrees != null)
                {
                    int placedCount = 0;
                    foreach (var placedTree in placedTrees)
                    {
                        if (placedTree != null && placedTree.activeInHierarchy && !trees.Contains(placedTree))
                        {
                            trees.Add(placedTree);
                            placedCount++;
                            Debug.Log($"通过SceneInitializer找到已放置树木: {placedTree.name} 在位置 {placedTree.transform.position}");
                        }
                    }
                    Debug.Log($"通过SceneInitializer找到 {placedCount} 棵已放置树木");
                }
            }
        }
        
        // 最终统计
        Debug.Log($"=== 树木查找完成 ===");
        Debug.Log($"总共找到 {trees.Count} 棵树木/植物");
        
        // 分析找到的树木命名格式
        if (trees.Count > 0)
        {
            var exactFormatTrees = trees.Where(t => t.name.StartsWith("Tree_") && t.name.Contains("_Group") && t.name.Contains("_Tower")).ToList();
            var otherFormatTrees = trees.Where(t => !(t.name.StartsWith("Tree_") && t.name.Contains("_Group") && t.name.Contains("_Tower"))).ToList();
            
            Debug.Log($"命名格式分析:");
            Debug.Log($"  - 标准格式 (Tree_XXX_Group1_TowerYY): {exactFormatTrees.Count} 棵");
            Debug.Log($"  - 其他格式: {otherFormatTrees.Count} 棵");
            
            if (exactFormatTrees.Count > 0)
            {
                Debug.Log($"标准格式树木示例:");
                for (int i = 0; i < Mathf.Min(exactFormatTrees.Count, 5); i++)
                {
                    var tree = exactFormatTrees[i];
                    Debug.Log($"  {i + 1}: {tree.name}");
                }
            }
        }
        
        if (trees.Count == 0)
        {
            Debug.LogWarning("⚠️ 未找到任何树木！可能的原因：");
            Debug.LogWarning("1. 场景中没有树木对象");
            Debug.LogWarning("2. 树木对象名称不符合查找规则");
            Debug.LogWarning("3. 树木对象被禁用或隐藏");
            Debug.LogWarning("4. 需要先运行SceneInitializer创建树木");
            Debug.LogWarning("5. 树木对象在不可见的父对象下");
            Debug.LogWarning("6. 期望的命名格式: Tree_XXX_Group1_TowerYY");
            
            // 显示场景中所有对象的名称（前30个）
            Debug.Log("场景中的对象名称（前30个）:");
            for (int i = 0; i < Mathf.Min(allObjects.Length, 30); i++)
            {
                var obj = allObjects[i];
                if (obj != null)
                {
                    string parentInfo = obj.transform.parent != null ? $" (父对象: {obj.transform.parent.name})" : "";
                    Debug.Log($"  {i}: {obj.name}{parentInfo} - 激活状态: {obj.activeInHierarchy}");
                }
            }
            
            // 显示所有父对象
            Debug.Log("场景中的父对象:");
            var parentObjects = allObjects.Where(obj => obj.transform.childCount > 0).ToArray();
            for (int i = 0; i < Mathf.Min(parentObjects.Length, 20); i++)
            {
                var parent = parentObjects[i];
                Debug.Log($"  父对象 {i}: {parent.name} - 子对象数量: {parent.transform.childCount}");
            }
        }
        else
        {
            Debug.Log("找到的树木列表:");
            for (int i = 0; i < Mathf.Min(trees.Count, 15); i++)
            {
                var tree = trees[i];
                if (tree != null)
                {
                    string parentInfo = tree.transform.parent != null ? $" (父对象: {tree.transform.parent.name})" : "";
                    Debug.Log($"  {i}: {tree.name} 在 {tree.transform.position}{parentInfo}");
                }
            }
            if (trees.Count > 15)
            {
                Debug.Log($"  ... 还有 {trees.Count - 15} 棵树木");
            }
        }
    }
    
    void PerformMonitoring()
    {
        if (powerlines.Count == 0 || trees.Count == 0) return;
        
        Debug.Log("开始执行树木危险监测...");
        treeDangerList.Clear();
        
        foreach (var tree in trees)
        {
            if (tree == null) continue;
            
            PowerlineInteraction nearestPowerline = FindNearestPowerline(tree);
            if (nearestPowerline != null)
            {
                TreeDangerInfo dangerInfo = AssessTreeDanger(tree, nearestPowerline);
                if (dangerInfo != null)
                {
                    treeDangerList.Add(dangerInfo);
                }
            }
        }
        
        ProcessDangerousSituations();
        UpdateDangerDisplay();
        
        Debug.Log($"监测完成，发现 {treeDangerList.Count} 个危险情况");
    }
    
    PowerlineInteraction FindNearestPowerline(GameObject tree)
    {
        if (powerlines.Count == 0) return null;
        
        PowerlineInteraction nearest = null;
        float minDistance = float.MaxValue;
        
        foreach (var powerline in powerlines)
        {
            if (powerline == null) continue;
            
            float distance = Vector3.Distance(tree.transform.position, powerline.transform.position);
            if (distance <= maxDetectionDistance && distance < minDistance)
            {
                minDistance = distance;
                nearest = powerline;
            }
        }
        
        return nearest;
    }
    
    TreeDangerInfo AssessTreeDanger(GameObject tree, PowerlineInteraction powerline)
    {
        TreeDangerInfo dangerInfo = new TreeDangerInfo(tree, powerline);
        
        Vector3 treePos = tree.transform.position;
        Vector3 powerlinePos = powerline.transform.position;
        
        float treeHeight = GetTreeHeight(tree);
        float powerlineHeight = GetPowerlineHeight(powerline);
        
        Vector3 horizontalDiff = new Vector3(treePos.x - powerlinePos.x, 0, treePos.z - powerlinePos.z);
        float horizontalDistance = horizontalDiff.magnitude;
        float verticalDistance = Mathf.Abs(treeHeight - powerlineHeight);
        
        // 计算有效危险距离（考虑电力线弧垂和风力摇摆）
        float effectiveDangerDistance = criticalDistance + powerlineSag + windSwayFactor;
        
        // 当前距离计算
        dangerInfo.currentDistance = Mathf.Sqrt(horizontalDistance * horizontalDistance + verticalDistance * verticalDistance);
        dangerInfo.treeHeight = treeHeight;
        dangerInfo.growthRate = CalculateTreeGrowthRate(tree);
        
        // 新增：基于高度比例的判定逻辑
        float heightRatio = treeHeight / powerlineHeight; // 树木高度与电塔高度比例
        bool heightBasedDanger = heightRatio >= 0.5f; // 树木高度达到电塔50%时考虑危险
        
        // 计算基于高度的危险距离阈值
        float heightBasedCriticalDistance = 0f;
        float heightBasedWarningDistance = 0f;
        float heightBasedSafeDistance = 0f;
        
        if (heightBasedDanger)
        {
            // 树木高度达到电塔50%以上时，根据高度比例调整危险距离
            if (heightRatio >= 0.8f)
            {
                // 树木高度达到电塔80%以上，非常危险
                heightBasedCriticalDistance = 5f;  // 5米内为危险
                heightBasedWarningDistance = 15f;  // 15米内为警告
                heightBasedSafeDistance = 25f;     // 25米内为安全
            }
            else if (heightRatio >= 0.6f)
            {
                // 树木高度达到电塔60%以上，危险
                heightBasedCriticalDistance = 8f;  // 8米内为危险
                heightBasedWarningDistance = 20f;  // 20米内为警告
                heightBasedSafeDistance = 30f;     // 30米内为安全
            }
            else // heightRatio >= 0.5f
            {
                // 树木高度达到电塔50%以上，需要注意
                heightBasedCriticalDistance = 12f; // 12米内为危险
                heightBasedWarningDistance = 25f;  // 25米内为警告
                heightBasedSafeDistance = 35f;     // 35米内为安全
            }
        }
        else
        {
            // 树木高度较低时，使用原有的距离阈值
            heightBasedCriticalDistance = criticalDistance;
            heightBasedWarningDistance = warningDistance;
            heightBasedSafeDistance = safeDistance;
        }
        
        // 30天后的预测距离
        float timeToAssessment = 30f;
        float projectedHeight = treeHeight + (dangerInfo.growthRate * timeToAssessment / 365f);
        float projectedVerticalDistance = Mathf.Abs(projectedHeight - powerlineHeight);
        dangerInfo.projectedDistance = Mathf.Sqrt(horizontalDistance * horizontalDistance + projectedVerticalDistance * projectedVerticalDistance);
        
        // 一年后的预测
        float oneYearHeight = treeHeight + (dangerInfo.growthRate * 1f);
        float oneYearVerticalDistance = Mathf.Abs(oneYearHeight - powerlineHeight);
        dangerInfo.oneYearDistance = Mathf.Sqrt(horizontalDistance * horizontalDistance + oneYearVerticalDistance * oneYearVerticalDistance);
        
        // 三年后的预测
        float threeYearHeight = treeHeight + (dangerInfo.growthRate * 3f);
        float threeYearVerticalDistance = Mathf.Abs(threeYearHeight - powerlineHeight);
        dangerInfo.threeYearDistance = Mathf.Sqrt(horizontalDistance * horizontalDistance + threeYearVerticalDistance * threeYearVerticalDistance);
        
        // 使用基于高度的距离阈值进行危险等级判定
        dangerInfo.dangerLevel = DetermineDangerLevelWithHeightRatio(
            dangerInfo.currentDistance, 
            dangerInfo.projectedDistance, 
            heightBasedCriticalDistance,
            heightBasedWarningDistance,
            heightBasedSafeDistance,
            heightRatio
        );
        
        // 预测未来危险等级
        dangerInfo.oneYearDangerLevel = DetermineDangerLevelWithHeightRatio(
            dangerInfo.oneYearDistance, 
            dangerInfo.oneYearDistance, 
            heightBasedCriticalDistance,
            heightBasedWarningDistance,
            heightBasedSafeDistance,
            heightRatio
        );
        
        dangerInfo.threeYearDangerLevel = DetermineDangerLevelWithHeightRatio(
            dangerInfo.threeYearDistance, 
            dangerInfo.threeYearDistance, 
            heightBasedCriticalDistance,
            heightBasedWarningDistance,
            heightBasedSafeDistance,
            heightRatio
        );
        
        // 设置其他属性
        dangerInfo.oneYearRiskDescription = GenerateTimeBasedRiskDescription(dangerInfo.oneYearDistance, dangerInfo.oneYearDangerLevel, 1);
        dangerInfo.threeYearRiskDescription = GenerateTimeBasedRiskDescription(dangerInfo.threeYearDistance, dangerInfo.threeYearDangerLevel, 3);
        dangerInfo.willBeDangerousInOneYear = (dangerInfo.oneYearDangerLevel == TreeDangerLevel.Critical || 
                                              dangerInfo.oneYearDangerLevel == TreeDangerLevel.Emergency);
        dangerInfo.willBeDangerousInThreeYears = (dangerInfo.threeYearDangerLevel == TreeDangerLevel.Critical || 
                                                  dangerInfo.threeYearDangerLevel == TreeDangerLevel.Emergency);
        
        dangerInfo.dangerPoint = CalculateDangerPoint(treePos, powerlinePos, treeHeight, powerlineHeight);
        dangerInfo.riskDescription = GenerateRiskDescriptionWithHeightRatio(dangerInfo, heightRatio, heightBasedDanger);
        
        return dangerInfo;
    }
    
    float GetTreeHeight(GameObject tree)
    {
        if (tree == null) return 0f;
        
        Renderer renderer = tree.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.size.y;
        }
        
        Renderer[] childRenderers = tree.GetComponentsInChildren<Renderer>();
        if (childRenderers.Length > 0)
        {
            Bounds totalBounds = childRenderers[0].bounds;
            foreach (var childRenderer in childRenderers)
            {
                totalBounds.Encapsulate(childRenderer.bounds);
            }
            return totalBounds.size.y;
        }
        
        return 15f;
    }
    
    float GetPowerlineHeight(PowerlineInteraction powerline)
    {
        if (powerline == null) return this.powerlineHeight;
        
        var info = powerline.GetDetailedInfo();
        if (info != null && info.basicInfo != null && info.basicInfo.points != null && info.basicInfo.points.Count > 0)
        {
            float avgHeight = info.basicInfo.points.Average(p => p.y);
            return avgHeight;
        }
        
        return this.powerlineHeight;
    }
    
    float CalculateTreeGrowthRate(GameObject tree)
    {
        if (tree == null) return baseGrowthRate;
        
        float currentHeight = GetTreeHeight(tree);
        float heightFactor = Mathf.Clamp01(1f - (currentHeight / maxTreeHeight));
        float seasonalFactor = 1f + Mathf.Sin(Time.time * 0.1f) * seasonalGrowthFactor;
        
        float speciesFactor = 1f;
        string treeName = tree.name.ToLower();
        if (treeName.Contains("lemon"))
        {
            speciesFactor = 0.8f;
        }
        else if (treeName.Contains("pine") || treeName.Contains("松"))
        {
            speciesFactor = 1.2f;
        }
        else if (treeName.Contains("oak") || treeName.Contains("橡"))
        {
            speciesFactor = 0.6f;
        }
        
        return baseGrowthRate * heightFactor * seasonalFactor * speciesFactor;
    }
    
    /// <summary>
    /// 基于高度比例的危险等级判定
    /// </summary>
    TreeDangerLevel DetermineDangerLevelWithHeightRatio(float currentDistance, float projectedDistance, 
        float criticalDist, float warningDist, float safeDist, float heightRatio)
    {
        // 如果当前距离或预测距离达到危险阈值，判定为相应等级
        if (currentDistance <= criticalDist || projectedDistance <= criticalDist)
        {
            if (heightRatio >= 0.8f)
            {
                return TreeDangerLevel.Emergency; // 树木高度达到电塔80%以上，非常危险
            }
            else
            {
                return TreeDangerLevel.Critical;  // 危险
            }
        }
        else if (currentDistance <= warningDist || projectedDistance <= warningDist)
        {
            return TreeDangerLevel.Warning;       // 警告
        }
        else if (currentDistance <= safeDist || projectedDistance <= safeDist)
        {
            return TreeDangerLevel.Warning;       // 仍然为警告，因为距离较近
        }
        else
        {
            return TreeDangerLevel.Safe;          // 安全
        }
    }
    
    /// <summary>
    /// 基于高度比例的风险描述生成
    /// </summary>
    string GenerateRiskDescriptionWithHeightRatio(TreeDangerInfo dangerInfo, float heightRatio, bool heightBasedDanger)
    {
        string description = "";
        
        // 添加高度比例信息
        if (heightBasedDanger)
        {
            description += $"⚠️ 树木高度已达到电塔高度的 {heightRatio * 100:F0}%，需要特别关注！\n";
        }
        
        switch (dangerInfo.dangerLevel)
        {
            case TreeDangerLevel.Safe:
                description += "安全状态，树木与电力线距离充足";
                break;
            case TreeDangerLevel.Warning:
                if (heightBasedDanger)
                {
                    description += $"警告：树木高度较高({heightRatio * 100:F0}%)，当前距离电力线 {dangerInfo.currentDistance:F1}m，建议立即监测";
                }
                else
                {
                    description += $"警告：树木当前距离电力线 {dangerInfo.currentDistance:F1}m，建议定期监测";
                }
                break;
            case TreeDangerLevel.Critical:
                if (heightRatio >= 0.8f)
                {
                    description += $"紧急危险：树木高度已达到电塔高度的 {heightRatio * 100:F0}%，距离电力线仅 {dangerInfo.currentDistance:F1}m，需要立即处理！";
                }
                else
                {
                    description += $"危险：树木距离电力线过近 ({dangerInfo.currentDistance:F1}m)，需要立即处理";
                }
                break;
            case TreeDangerLevel.Emergency:
                description += $"紧急：树木高度已达到电塔高度的 {heightRatio * 100:F0}%，已接触或即将接触电力线！当前距离：{dangerInfo.currentDistance:F1}m";
                break;
        }
        
        // 添加生长预测信息
        if (dangerInfo.growthRate > 0 && heightBasedDanger)
        {
            float daysToDanger = (dangerInfo.currentDistance - criticalDistance) / (dangerInfo.growthRate / 365f);
            if (daysToDanger > 0 && daysToDanger < 365)
            {
                description += $"\n⚠️ 预计 {daysToDanger:F0} 天后可能达到危险距离！";
            }
        }
        
        // 添加高度建议
        if (heightBasedDanger)
        {
            if (heightRatio >= 0.8f)
            {
                description += "\n🚨 建议：立即修剪或移除，防止电力线接触！";
            }
            else if (heightRatio >= 0.6f)
            {
                description += "\n⚠️ 建议：制定修剪计划，控制树木高度增长";
            }
            else
            {
                description += "\n💡 建议：定期监测，预防高度增长带来的风险";
            }
        }
        
        return description;
    }
    
    Vector3 CalculateDangerPoint(Vector3 treePos, Vector3 powerlinePos, float treeHeight, float powerlineHeight)
    {
        Vector3 midPoint = (treePos + powerlinePos) * 0.5f;
        midPoint.y = powerlineHeight;
        return midPoint;
    }
    
    string GenerateRiskDescription(TreeDangerInfo dangerInfo)
    {
        string description = "";
        
        switch (dangerInfo.dangerLevel)
        {
            case TreeDangerLevel.Safe:
                description = "安全状态，树木与电力线距离充足";
                break;
            case TreeDangerLevel.Warning:
                description = $"警告：树木当前距离电力线 {dangerInfo.currentDistance:F1}m，建议定期监测";
                break;
            case TreeDangerLevel.Critical:
                description = $"危险：树木距离电力线过近 ({dangerInfo.currentDistance:F1}m)，需要立即处理";
                break;
            case TreeDangerLevel.Emergency:
                description = $"紧急：树木已接触或即将接触电力线！当前距离：{dangerInfo.currentDistance:F1}m";
                break;
        }
        
        if (dangerInfo.growthRate > 0)
        {
            float daysToDanger = (dangerInfo.currentDistance - criticalDistance) / (dangerInfo.growthRate / 365f);
            if (daysToDanger > 0 && daysToDanger < 365)
            {
                description += $"\n预计 {daysToDanger:F0} 天后可能达到危险距离";
            }
        }
        
        return description;
    }

    string GenerateTimeBasedRiskDescription(float distance, TreeDangerLevel level, int years)
    {
        string description = "";
        switch (level)
        {
            case TreeDangerLevel.Safe:
                description = $"在 {years} 年后，树木与电力线距离充足";
                break;
            case TreeDangerLevel.Warning:
                description = $"在 {years} 年后，树木距离电力线 {distance:F1}m，建议定期监测";
                break;
            case TreeDangerLevel.Critical:
                description = $"在 {years} 年后，树木距离电力线过近 ({distance:F1}m)，需要立即处理";
                break;
            case TreeDangerLevel.Emergency:
                description = $"在 {years} 年后，树木已接触或即将接触电力线！当前距离：{distance:F1}m";
                break;
        }
        return description;
    }
    
    void ProcessDangerousSituations()
    {
        var criticalTrees = treeDangerList.Where(t => t.dangerLevel >= TreeDangerLevel.Critical).ToList();
        
        foreach (var criticalTree in criticalTrees)
        {
            CreateDangerMarker(criticalTree);
            Debug.LogWarning($"发现危险树木: {criticalTree.tree.name} - {criticalTree.riskDescription}");
            SendDangerNotification(criticalTree);
        }
    }
    
    void CreateDangerMarker(TreeDangerInfo dangerInfo)
    {
        if (dangerInfo == null || dangerInfo.tree == null) return;
        
        if (dangerInfo.tree.GetComponent<DangerMarker>() != null) return;
        
        GameObject markerObj = new GameObject("TreeDangerMarker");
        markerObj.transform.position = dangerInfo.dangerPoint;
        markerObj.transform.SetParent(dangerInfo.tree.transform);
        
        DangerMarker marker = markerObj.AddComponent<DangerMarker>();
        DangerType dangerType = DangerType.Vegetation;
        DangerLevel dangerLevel = (DangerLevel)dangerInfo.dangerLevel;
        
        marker.SetDangerInfo(dangerType, dangerLevel, dangerInfo.riskDescription, "自动监测系统");
        
        Debug.Log($"已为危险树木 {dangerInfo.tree.name} 创建标记");
    }
    
    void SendDangerNotification(TreeDangerInfo dangerInfo)
    {
        Debug.LogWarning($"危险通知: {dangerInfo.tree.name} - {dangerInfo.riskDescription}");
    }
    
    void UpdateDangerDisplay()
    {
        if (treeDangerList.Count > 0)
        {
            var criticalCount = treeDangerList.Count(t => t.dangerLevel >= TreeDangerLevel.Critical);
            var warningCount = treeDangerList.Count(t => t.dangerLevel == TreeDangerLevel.Warning);
            
            Debug.Log($"危险统计 - 紧急: {criticalCount}, 警告: {warningCount}, 安全: {treeDangerList.Count - criticalCount - warningCount}");
        }
    }
    
    [ContextMenu("手动触发监测")]
    public void ManualMonitoring()
    {
        Debug.Log("手动触发树木危险监测...");
        PerformMonitoring();
    }
    
    /// <summary>
    /// 刷新树木列表（供外部调用）
    /// </summary>
    public void RefreshTreeList()
    {
        Debug.Log("刷新树木列表...");
        FindTrees();
        Debug.Log($"刷新完成，当前树木数量: {trees.Count}");
    }
    
    /// <summary>
    /// 获取危险统计信息
    /// </summary>
    public Dictionary<TreeDangerLevel, int> GetDangerStatistics()
    {
        var stats = new Dictionary<TreeDangerLevel, int>();
        
        // 初始化所有危险等级为0
        foreach (TreeDangerLevel level in Enum.GetValues(typeof(TreeDangerLevel)))
        {
            stats[level] = 0;
        }
        
        if (treeDangerList.Count > 0)
        {
            // 有监测结果，统计各危险等级的数量
            foreach (var dangerInfo in treeDangerList)
            {
                if (dangerInfo != null)
                {
                    TreeDangerLevel level = dangerInfo.dangerLevel;
                    if (stats.ContainsKey(level))
                    {
                        stats[level]++;
                    }
                    else
                    {
                        stats[level] = 1;
                    }
                }
            }
        }
        else if (trees.Count > 0)
        {
            // 找到树木但未执行监测，设置为安全状态
            int foundTreeCount = trees.Count;
            stats[TreeDangerLevel.Safe] = foundTreeCount;
            Debug.Log($"树木已找到但未监测，设置 {foundTreeCount} 棵为安全状态");
        }
        
        return stats;
    }
    
    /// <summary>
    /// 获取时间预测的危险统计信息
    /// </summary>
    public Dictionary<string, object> GetTimePredictionStatistics()
    {
        var predictionStats = new Dictionary<string, object>();
        
        if (treeDangerList.Count == 0)
        {
            predictionStats["hasData"] = false;
            predictionStats["message"] = "暂无监测数据";
            return predictionStats;
        }
        
        predictionStats["hasData"] = true;
        predictionStats["totalTrees"] = treeDangerList.Count;
        
        // 一年后的预测统计
        int oneYearCritical = 0;
        int oneYearEmergency = 0;
        int oneYearTotalDangerous = 0;
        
        // 三年后的预测统计
        int threeYearCritical = 0;
        int threeYearEmergency = 0;
        int threeYearTotalDangerous = 0;
        
        // 当前危险统计
        int currentCritical = 0;
        int currentEmergency = 0;
        int currentTotalDangerous = 0;
        
        foreach (var dangerInfo in treeDangerList)
        {
            if (dangerInfo == null) continue;
            
            // 当前危险统计
            if (dangerInfo.dangerLevel == TreeDangerLevel.Critical) currentCritical++;
            if (dangerInfo.dangerLevel == TreeDangerLevel.Emergency) currentEmergency++;
            if (dangerInfo.dangerLevel == TreeDangerLevel.Critical || dangerInfo.dangerLevel == TreeDangerLevel.Emergency)
                currentTotalDangerous++;
            
            // 一年后预测统计
            if (dangerInfo.oneYearDangerLevel == TreeDangerLevel.Critical) oneYearCritical++;
            if (dangerInfo.oneYearDangerLevel == TreeDangerLevel.Emergency) oneYearEmergency++;
            if (dangerInfo.willBeDangerousInOneYear) oneYearTotalDangerous++;
            
            // 三年后预测统计
            if (dangerInfo.threeYearDangerLevel == TreeDangerLevel.Critical) threeYearCritical++;
            if (dangerInfo.threeYearDangerLevel == TreeDangerLevel.Emergency) threeYearEmergency++;
            if (dangerInfo.willBeDangerousInThreeYears) threeYearTotalDangerous++;
        }
        
        // 当前状态
        predictionStats["current"] = new Dictionary<string, object>
        {
            ["critical"] = currentCritical,
            ["emergency"] = currentEmergency,
            ["totalDangerous"] = currentTotalDangerous,
            ["riskPercentage"] = treeDangerList.Count > 0 ? (float)currentTotalDangerous / treeDangerList.Count * 100f : 0f
        };
        
        // 一年后预测
        predictionStats["oneYear"] = new Dictionary<string, object>
        {
            ["critical"] = oneYearCritical,
            ["emergency"] = oneYearEmergency,
            ["totalDangerous"] = oneYearTotalDangerous,
            ["riskPercentage"] = treeDangerList.Count > 0 ? (float)oneYearTotalDangerous / treeDangerList.Count * 100f : 0f,
            ["willBeDangerous"] = oneYearTotalDangerous > 0
        };
        
        // 三年后预测
        predictionStats["threeYear"] = new Dictionary<string, object>
        {
            ["critical"] = threeYearCritical,
            ["emergency"] = threeYearEmergency,
            ["totalDangerous"] = threeYearTotalDangerous,
            ["riskPercentage"] = treeDangerList.Count > 0 ? (float)threeYearTotalDangerous / treeDangerList.Count * 100f : 0f,
            ["willBeDangerous"] = threeYearTotalDangerous > 0
        };
        
        // 趋势分析
        bool riskIncreasing = oneYearTotalDangerous > currentTotalDangerous || threeYearTotalDangerous > oneYearTotalDangerous;
        predictionStats["trend"] = new Dictionary<string, object>
        {
            ["riskIncreasing"] = riskIncreasing,
            ["maxRiskPeriod"] = threeYearTotalDangerous > oneYearTotalDangerous ? "三年后" : "一年后",
            ["recommendation"] = riskIncreasing ? "建议立即制定树木管理计划" : "风险相对稳定，继续监测"
        };
        
        return predictionStats;
    }
    
    /// <summary>
    /// 获取一年后将有危险的树木列表
    /// </summary>
    public List<TreeDangerInfo> GetOneYearDangerousTrees()
    {
        return treeDangerList.Where(t => t != null && t.willBeDangerousInOneYear).ToList();
    }
    
    /// <summary>
    /// 获取三年后将有危险的树木列表
    /// </summary>
    public List<TreeDangerInfo> GetThreeYearDangerousTrees()
    {
        return treeDangerList.Where(t => t != null && t.willBeDangerousInThreeYears).ToList();
    }
    
    /// <summary>
    /// 获取所有时间预测的危险树木（一年后或三年后）
    /// </summary>
    public List<TreeDangerInfo> GetAllTimePredictionDangerousTrees()
    {
        return treeDangerList.Where(t => t != null && (t.willBeDangerousInOneYear || t.willBeDangerousInThreeYears)).ToList();
    }
    
    /// <summary>
    /// 获取树木生长趋势报告
    /// </summary>
    public string GetTreeGrowthTrendReport()
    {
        if (treeDangerList.Count == 0)
            return "暂无监测数据";
        
        var oneYearDangerous = GetOneYearDangerousTrees();
        var threeYearDangerous = GetThreeYearDangerousTrees();
        
        string report = $"=== 树木生长趋势报告 ===\n";
        report += $"监测树木总数: {treeDangerList.Count}棵\n\n";
        
        report += $"一年后预测:\n";
        report += $"  危险树木: {oneYearDangerous.Count}棵\n";
        if (oneYearDangerous.Count > 0)
        {
            report += $"  风险等级分布:\n";
            var oneYearLevels = oneYearDangerous.GroupBy(t => t.oneYearDangerLevel);
            foreach (var level in oneYearLevels)
            {
                report += $"    {GetDangerLevelString(level.Key)}: {level.Count()}棵\n";
            }
        }
        
        report += $"\n三年后预测:\n";
        report += $"  危险树木: {threeYearDangerous.Count}棵\n";
        if (threeYearDangerous.Count > 0)
        {
            report += $"  风险等级分布:\n";
            var threeYearLevels = threeYearDangerous.GroupBy(t => t.threeYearDangerLevel);
            foreach (var level in threeYearLevels)
            {
                report += $"    {GetDangerLevelString(level.Key)}: {level.Count()}棵\n";
            }
        }
        
        // 趋势分析
        bool riskIncreasing = threeYearDangerous.Count > oneYearDangerous.Count;
        report += $"\n趋势分析:\n";
        report += $"  风险趋势: {(riskIncreasing ? "上升" : "稳定")}\n";
        report += $"  最大风险期: {(threeYearDangerous.Count > oneYearDangerous.Count ? "三年后" : "一年后")}\n";
        report += $"  建议: {(riskIncreasing ? "建议立即制定树木管理计划" : "风险相对稳定，继续监测")}\n";
        
        return report;
    }
    
    /// <summary>
    /// 获取危险等级的中文描述
    /// </summary>
    private string GetDangerLevelString(TreeDangerLevel level)
    {
        switch (level)
        {
            case TreeDangerLevel.Safe:
                return "安全";
            case TreeDangerLevel.Warning:
                return "警告";
            case TreeDangerLevel.Critical:
                return "危险";
            case TreeDangerLevel.Emergency:
                return "紧急";
            default:
                return "未知";
        }
    }
    
    public List<TreeDangerInfo> GetAllDangerInfo()
    {
        return new List<TreeDangerInfo>(treeDangerList);
    }
    
    public void ClearAllDangerMarkers()
    {
        foreach (var dangerInfo in treeDangerList)
        {
            if (dangerInfo.tree != null)
            {
                var marker = dangerInfo.tree.GetComponent<DangerMarker>();
                if (marker != null)
                {
                    DestroyImmediate(marker.gameObject);
                }
            }
        }
        
        treeDangerList.Clear();
        Debug.Log("已清除所有树木危险标记");
    }
    
    public void SetMonitoringParameters(float criticalDist, float warningDist, float safeDist, float growthRate)
    {
        criticalDistance = criticalDist;
        warningDistance = warningDist;
        safeDistance = safeDist;
        baseGrowthRate = growthRate;
        
        Debug.Log($"监测参数已更新 - 危险: {criticalDistance}m, 警告: {warningDistance}m, 安全: {safeDistance}m, 生长率: {baseGrowthRate}m/年");
    }
    
    /// <summary>
    /// 导出危险树木位置记录到CSV文件
    /// </summary>
    [ContextMenu("导出危险树木位置记录")]
    public void ExportDangerousTreesToCSV()
    {
        if (treeDangerList.Count == 0)
        {
            Debug.Log("暂无危险树木记录可导出");
            return;
        }
        
        try
        {
            string csvContent = "树木名称,电塔组别,电塔编号,树木位置,电力线位置,当前距离,预测距离,危险等级,树木高度,生长率,风险描述\n";
            
            foreach (var dangerInfo in treeDangerList)
            {
                if (dangerInfo.tree == null || dangerInfo.powerline == null) continue;
                
                string line = $"{dangerInfo.treeName}," +
                             $"{dangerInfo.towerGroup}," +
                             $"{dangerInfo.towerNumber}," +
                             $"{dangerInfo.treePosition}," +
                             $"{dangerInfo.powerlinePosition}," +
                             $"{dangerInfo.currentDistance:F2}," +
                             $"{dangerInfo.projectedDistance:F2}," +
                             $"{dangerInfo.dangerLevel}," +
                             $"{dangerInfo.treeHeight:F2}," +
                             $"{dangerInfo.growthRate:F3}," +
                             $"\"{dangerInfo.riskDescription}\"\n";
                
                csvContent += line;
            }
            
            Debug.Log($"=== 危险树木位置记录 ===\n{csvContent}");
            Debug.Log("CSV内容已输出到控制台，请复制保存");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"导出CSV失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 获取危险树木位置报告
    /// </summary>
    public string GetDangerousTreesLocationReport()
    {
        if (treeDangerList.Count == 0)
        {
            return "暂无危险树木记录";
        }
        
        string report = "=== 危险树木位置报告 ===\n";
        foreach (var dangerInfo in treeDangerList)
        {
            if (dangerInfo.tree == null || dangerInfo.powerline == null) continue;
            
            report += $"树木: {dangerInfo.treeName}\n";
            report += $"位置: {dangerInfo.treePosition}\n";
            report += $"电塔: {dangerInfo.towerGroup} - {dangerInfo.towerNumber}\n";
            report += $"当前距离: {dangerInfo.currentDistance:F2}m\n";
            report += $"危险等级: {dangerInfo.dangerLevel}\n";
            report += "---\n";
        }
        
        Debug.Log(report);
        return report;
    }
    
    /// <summary>
    /// 获取场景中的树木总数
    /// </summary>
    public int GetTreeCount()
    {
        return trees.Count;
    }
    
    /// <summary>
    /// 调试树木监测系统状态
    /// </summary>
    public void DebugTreeStatus()
    {
        Debug.Log($"=== 树木监测系统状态 ===");
        Debug.Log($"找到的树木数量: {trees.Count}");
        Debug.Log($"电力线数量: {powerlines.Count}");
        Debug.Log($"危险情况数量: {treeDangerList.Count}");
        
        if (trees.Count == 0)
        {
            Debug.LogWarning("⚠️ 没有找到树木，可能的原因：");
            Debug.LogWarning("1. 场景中没有树木对象");
            Debug.LogWarning("2. 树木对象名称不符合查找规则");
            Debug.LogWarning("3. 需要先运行SceneInitializer创建树木");
        }
        
        if (powerlines.Count == 0)
        {
            Debug.LogWarning("⚠️ 没有找到电力线，可能的原因：");
            Debug.LogWarning("1. 场景中没有电力线对象");
            Debug.LogWarning("2. 电力线对象没有PowerlineInteraction组件");
            Debug.LogWarning("3. 需要先运行SceneInitializer创建电力线");
        }
    }
    
    /// <summary>
    /// 强制重新查找并监测所有对象
    /// </summary>
    public void ForceRefreshAndMonitor()
    {
        Debug.Log("强制刷新并监测所有对象...");
        FindPowerlines();
        FindTrees();
        PerformMonitoring();
        Debug.Log($"刷新完成 - 树木: {trees.Count}, 电力线: {powerlines.Count}, 危险情况: {treeDangerList.Count}");
    }
    
    /// <summary>
    /// 系统诊断方法 - 供外部调用进行问题诊断
    /// </summary>
    [ContextMenu("系统诊断")]
    public void DiagnoseSystem()
    {
        Debug.Log("=== 树木危险监测系统诊断 ===");
        
        // 检查组件状态
        Debug.Log($"组件状态:");
        Debug.Log($"  - enableAutoMonitoring: {enableAutoMonitoring}");
        Debug.Log($"  - monitoringInterval: {monitoringInterval}秒");
        Debug.Log($"  - maxDetectionDistance: {maxDetectionDistance}米");
        Debug.Log($"  - 危险距离: {criticalDistance}米");
        Debug.Log($"  - 警告距离: {warningDistance}米");
        Debug.Log($"  - 安全距离: {safeDistance}米");
        
        // 检查对象列表
        Debug.Log($"对象列表状态:");
        Debug.Log($"  - 电力线数量: {powerlines.Count}");
        Debug.Log($"  - 树木数量: {trees.Count}");
        Debug.Log($"  - 危险情况数量: {treeDangerList.Count}");
        
        // 重新查找对象
        Debug.Log("重新查找对象...");
        FindPowerlines();
        FindTrees();
        
        // 检查监测状态
        Debug.Log($"监测状态:");
        Debug.Log($"  - 上次监测时间: {lastMonitoringTime:F1}秒前");
        Debug.Log($"  - 距离下次监测: {Mathf.Max(0, monitoringInterval - (Time.time - lastMonitoringTime)):F1}秒");
        
        // 如果对象不足，提供建议
        if (powerlines.Count == 0)
        {
            Debug.LogError("❌ 问题诊断: 没有找到电力线");
            Debug.LogError("建议:");
            Debug.LogError("1. 检查场景中是否有PowerlineInteraction组件");
            Debug.LogError("2. 运行SceneInitializer创建电力线");
            Debug.LogError("3. 确保电力线对象已启用");
        }
        
        if (trees.Count == 0)
        {
            Debug.LogError("❌ 问题诊断: 没有找到树木");
            Debug.LogError("建议:");
            Debug.LogError("1. 检查场景中是否有树木对象");
            Debug.LogError("2. 运行SceneInitializer创建树木");
            Debug.LogError("3. 确保树木对象名称包含'tree'、'植物'等关键词");
            Debug.LogError("4. 检查树木对象是否被禁用或隐藏");
        }
        
        if (powerlines.Count > 0 && trees.Count > 0)
        {
            Debug.Log("✅ 系统状态正常，可以进行监测");
            
            // 执行一次监测
            Debug.Log("执行测试监测...");
            PerformMonitoring();
            
            Debug.Log($"测试监测完成，发现 {treeDangerList.Count} 个危险情况");
        }
        
        Debug.Log("=== 诊断完成 ===");
    }
    
    /// <summary>
    /// 强制刷新系统状态
    /// </summary>
    [ContextMenu("强制刷新")]
    public void ForceRefresh()
    {
        Debug.Log("强制刷新树木危险监测系统...");
        
        // 清除所有列表
        powerlines.Clear();
        trees.Clear();
        treeDangerList.Clear();
        
        // 重新初始化
        InitializeMonitoring();
        
        // 如果启用了自动监测，立即执行一次
        if (enableAutoMonitoring)
        {
            PerformMonitoring();
        }
        
        Debug.Log("强制刷新完成");
    }
    
    /// <summary>
    /// 记录危险树木位置到控制台
    /// </summary>
    [ContextMenu("记录危险树木位置")]
    public void LogDangerousTreesLocations()
    {
        string report = GetDangerousTreesLocationReport();
        Debug.Log(report);
    }

    /// <summary>
    /// 调试显示基于高度比例的危险判定信息
    /// </summary>
    [ContextMenu("调试高度比例判定")]
    public void DebugHeightBasedAssessment()
    {
        Debug.Log("=== 基于高度比例的危险判定调试信息 ===");
        
        if (treeDangerList.Count == 0)
        {
            Debug.Log("暂无监测数据，请先执行监测");
            return;
        }
        
        foreach (var dangerInfo in treeDangerList)
        {
            if (dangerInfo == null || dangerInfo.powerline == null) continue;
            
            float powerlineHeight = GetPowerlineHeight(dangerInfo.powerline);
            float heightRatio = dangerInfo.treeHeight / powerlineHeight;
            bool heightBasedDanger = heightRatio >= 0.5f;
            
            Debug.Log($"树木: {dangerInfo.tree.name}");
            Debug.Log($"  树木高度: {dangerInfo.treeHeight:F1}m");
            Debug.Log($"  电塔高度: {powerlineHeight:F1}m");
            Debug.Log($"  高度比例: {heightRatio * 100:F1}%");
            Debug.Log($"  是否基于高度判定: {heightBasedDanger}");
            Debug.Log($"  当前距离: {dangerInfo.currentDistance:F1}m");
            Debug.Log($"  危险等级: {dangerInfo.dangerLevel}");
            Debug.Log($"  风险描述: {dangerInfo.riskDescription}");
            Debug.Log("  ---");
        }
        
        Debug.Log("=== 调试信息结束 ===");
    }
}
