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
    public float criticalDistance = 3f;
    public float warningDistance = 8f;
    public float safeDistance = 15f;
    
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
        
        public TreeDangerInfo(GameObject treeObj, PowerlineInteraction powerlineObj)
        {
            tree = treeObj;
            powerline = powerlineObj;
            lastAssessment = DateTime.Now;
        }
    }
    
    void Start()
    {
        InitializeMonitoring();
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
        var foundPowerlines = FindObjectsOfType<PowerlineInteraction>();
        foreach (var powerline in foundPowerlines)
        {
            if (powerline != null && powerline.enabled)
            {
                powerlines.Add(powerline);
            }
        }
        Debug.Log($"找到 {powerlines.Count} 条电力线");
    }
    
    void FindTrees()
    {
        trees.Clear();
        try
        {
            GameObject[] taggedTrees = GameObject.FindGameObjectsWithTag("Tree");
            trees.AddRange(taggedTrees);
        }
        catch (UnityException)
        {
            Debug.LogWarning("Tree标签未定义，尝试通过名称查找");
        }
        
        if (trees.Count == 0)
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.ToLower().Contains("tree") || 
                    obj.name.ToLower().Contains("植物") ||
                    obj.name.ToLower().Contains("vegetation"))
                {
                    trees.Add(obj);
                }
            }
        }
        
        var numenaPlants = FindObjectsOfType<GameObject>().Where(obj => 
            obj.name.Contains("Lemon") || 
            obj.name.Contains("Tree") ||
            obj.transform.parent != null && obj.transform.parent.name.Contains("Plants"));
        
        foreach (var plant in numenaPlants)
        {
            if (!trees.Contains(plant))
            {
                trees.Add(plant);
            }
        }
        
        Debug.Log($"找到 {trees.Count} 棵树木/植物");
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
        
        float effectiveDangerDistance = criticalDistance + powerlineSag + windSwayFactor;
        
        dangerInfo.currentDistance = Mathf.Sqrt(horizontalDistance * horizontalDistance + verticalDistance * verticalDistance);
        dangerInfo.treeHeight = treeHeight;
        dangerInfo.growthRate = CalculateTreeGrowthRate(tree);
        
        float timeToAssessment = 30f;
        float projectedHeight = treeHeight + (dangerInfo.growthRate * timeToAssessment / 365f);
        float projectedVerticalDistance = Mathf.Abs(projectedHeight - powerlineHeight);
        dangerInfo.projectedDistance = Mathf.Sqrt(horizontalDistance * horizontalDistance + projectedVerticalDistance * projectedVerticalDistance);
        
        dangerInfo.dangerLevel = DetermineDangerLevel(dangerInfo.currentDistance, dangerInfo.projectedDistance, effectiveDangerDistance);
        dangerInfo.dangerPoint = CalculateDangerPoint(treePos, powerlinePos, treeHeight, powerlineHeight);
        dangerInfo.riskDescription = GenerateRiskDescription(dangerInfo);
        
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
    
    TreeDangerLevel DetermineDangerLevel(float currentDistance, float projectedDistance, float effectiveDangerDistance)
    {
        if (currentDistance <= effectiveDangerDistance || projectedDistance <= effectiveDangerDistance)
        {
            return TreeDangerLevel.Emergency;
        }
        else if (currentDistance <= warningDistance || projectedDistance <= warningDistance)
        {
            return TreeDangerLevel.Critical;
        }
        else if (currentDistance <= safeDistance || projectedDistance <= safeDistance)
        {
            return TreeDangerLevel.Warning;
        }
        else
        {
            return TreeDangerLevel.Safe;
        }
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
    
    public Dictionary<TreeDangerLevel, int> GetDangerStatistics()
    {
        var stats = new Dictionary<TreeDangerLevel, int>();
        
        foreach (TreeDangerLevel level in Enum.GetValues(typeof(TreeDangerLevel)))
        {
            stats[level] = treeDangerList.Count(t => t.dangerLevel == level);
        }
        
        return stats;
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
}
