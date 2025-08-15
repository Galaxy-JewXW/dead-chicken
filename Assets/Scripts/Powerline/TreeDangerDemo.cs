using UnityEngine;
using System.Collections;
using System.Linq;

/// <summary>
/// 树木危险监测系统演示脚本
/// 展示如何使用和配置树木危险监测功能
/// </summary>
public class TreeDangerDemo : MonoBehaviour
{
    [Header("演示设置")]
    public bool enableDemo = true;
    public bool createSampleTrees = true;
    public bool createSamplePowerlines = true;
    
    [Header("示例树木设置")]
    public GameObject treePrefab;
    public int sampleTreeCount = 10;
    public float treeAreaRadius = 50f;
    public float minTreeHeight = 5f;
    public float maxTreeHeight = 25f;
    
    [Header("示例电力线设置")]
    public GameObject powerlinePrefab;
    public int samplePowerlineCount = 3;
    public float powerlineAreaRadius = 30f;
    public float powerlineHeight = 20f;
    
    [Header("演示控制")]
    public bool autoStartMonitoring = true;
    public float demoUpdateInterval = 10f;
    
    private TreeDangerMonitor treeDangerMonitor;
    private bool demoInitialized = false;
    
    void Start()
    {
        if (!enableDemo) return;
        
        StartCoroutine(InitializeDemo());
    }
    
    /// <summary>
    /// 初始化演示
    /// </summary>
    IEnumerator InitializeDemo()
    {
        Debug.Log("开始初始化树木危险监测演示...");
        
        // 等待一帧确保所有组件都已初始化
        yield return null;
        
        // 查找或创建TreeDangerMonitor
        treeDangerMonitor = FindObjectOfType<TreeDangerMonitor>();
        if (treeDangerMonitor == null)
        {
            GameObject monitorObj = new GameObject("TreeDangerMonitor");
            treeDangerMonitor = monitorObj.AddComponent<TreeDangerMonitor>();
            Debug.Log("已创建TreeDangerMonitor组件");
        }
        
        // 创建示例场景
        if (createSampleTrees)
        {
            CreateSampleTrees();
        }
        
        if (createSamplePowerlines)
        {
            CreateSamplePowerlines();
        }
        
        // 等待场景创建完成
        yield return new WaitForSeconds(1f);
        
        // 启动监测
        if (autoStartMonitoring)
        {
            StartMonitoring();
        }
        
        demoInitialized = true;
        Debug.Log("树木危险监测演示初始化完成");
        
        // 开始演示循环
        StartCoroutine(DemoLoop());
    }
    
    /// <summary>
    /// 创建示例树木
    /// </summary>
    void CreateSampleTrees()
    {
        Debug.Log($"开始创建 {sampleTreeCount} 棵示例树木...");
        
        // 如果没有指定预制件，创建简单的树木
        if (treePrefab == null)
        {
            treePrefab = CreateSimpleTreePrefab();
        }
        
        for (int i = 0; i < sampleTreeCount; i++)
        {
            CreateSampleTree(i);
        }
        
        Debug.Log("示例树木创建完成");
    }
    
    /// <summary>
    /// 创建单棵示例树木
    /// </summary>
    void CreateSampleTree(int index)
    {
        // 随机位置
        Vector2 randomCircle = Random.insideUnitCircle * treeAreaRadius;
        Vector3 position = new Vector3(randomCircle.x, 0, randomCircle.y);
        
        // 随机高度
        float height = Random.Range(minTreeHeight, maxTreeHeight);
        
        // 实例化树木
        GameObject tree = Instantiate(treePrefab, position, Quaternion.identity);
        tree.name = $"DemoTree_{index:D3}";
        
        // 设置标签
        try
        {
            tree.tag = "Tree";
        }
        catch (UnityException)
        {
            Debug.LogWarning("Tree标签未定义，树木将无法被自动识别");
        }
        
        // 调整树木高度
        tree.transform.localScale = new Vector3(1f, height / 10f, 1f);
        
        // 随机旋转
        tree.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        
        Debug.Log($"创建树木 {tree.name} 在位置 {position}, 高度 {height}m");
    }
    
    /// <summary>
    /// 创建示例电力线
    /// </summary>
    void CreateSamplePowerlines()
    {
        Debug.Log($"开始创建 {samplePowerlineCount} 条示例电力线...");
        
        // 如果没有指定预制件，创建简单的电力线
        if (powerlinePrefab == null)
        {
            powerlinePrefab = CreateSimplePowerlinePrefab();
        }
        
        for (int i = 0; i < samplePowerlineCount; i++)
        {
            CreateSamplePowerline(i);
        }
        
        Debug.Log("示例电力线创建完成");
    }
    
    /// <summary>
    /// 创建单条示例电力线
    /// </summary>
    void CreateSamplePowerline(int index)
    {
        // 随机位置
        Vector2 randomCircle = Random.insideUnitCircle * powerlineAreaRadius;
        Vector3 position = new Vector3(randomCircle.x, powerlineHeight, randomCircle.y);
        
        // 实例化电力线
        GameObject powerline = Instantiate(powerlinePrefab, position, Quaternion.identity);
        powerline.name = $"DemoPowerline_{index:D3}";
        
        // 设置标签
        try
        {
            powerline.tag = "Powerline";
        }
        catch (UnityException)
        {
            Debug.LogWarning("Powerline标签未定义，电力线将无法被自动识别");
        }
        
        // 添加PowerlineInteraction组件
        PowerlineInteraction interaction = powerline.GetComponent<PowerlineInteraction>();
        if (interaction == null)
        {
            interaction = powerline.AddComponent<PowerlineInteraction>();
        }
        
        // 设置电力线信息
        var powerlineInfo = new SceneInitializer.PowerlineInfo
        {
            wireType = "Conductor",
            index = index,
            length = Random.Range(50f, 200f),
            start = position,
            end = position + Vector3.right * Random.Range(20f, 50f),
            points = new System.Collections.Generic.List<Vector3>()
        };
        
        // 添加一些中间点
        int pointCount = Random.Range(3, 8);
        for (int j = 0; j < pointCount; j++)
        {
            float t = (float)j / (pointCount - 1);
            Vector3 point = Vector3.Lerp(powerlineInfo.start, powerlineInfo.end, t);
            point.y = powerlineHeight + Random.Range(-2f, 2f); // 添加一些弧垂
            powerlineInfo.points.Add(point);
        }
        
        interaction.SetPowerlineInfo(powerlineInfo);
        
        Debug.Log($"创建电力线 {powerline.name} 在位置 {position}");
    }
    
    /// <summary>
    /// 创建简单树木预制件
    /// </summary>
    GameObject CreateSimpleTreePrefab()
    {
        GameObject tree = new GameObject("SimpleTree");
        
        // 创建树干
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform);
        trunk.transform.localPosition = Vector3.zero;
        trunk.transform.localScale = new Vector3(0.5f, 5f, 0.5f);
        
        // 设置树干材质
        Material trunkMaterial = new Material(Shader.Find("Standard"));
        trunkMaterial.color = new Color(0.4f, 0.2f, 0.1f);
        trunk.GetComponent<Renderer>().material = trunkMaterial;
        
        // 创建树冠
        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = "Crown";
        crown.transform.SetParent(tree.transform);
        crown.transform.localPosition = Vector3.up * 4f;
        crown.transform.localScale = new Vector3(3f, 3f, 3f);
        
        // 设置树冠材质
        Material crownMaterial = new Material(Shader.Find("Standard"));
        crownMaterial.color = new Color(0.2f, 0.6f, 0.2f);
        crown.GetComponent<Renderer>().material = crownMaterial;
        
        // 移除碰撞器
        DestroyImmediate(trunk.GetComponent<Collider>());
        DestroyImmediate(crown.GetComponent<Collider>());
        
        return tree;
    }
    
    /// <summary>
    /// 创建简单电力线预制件
    /// </summary>
    GameObject CreateSimplePowerlinePrefab()
    {
        GameObject powerline = new GameObject("SimplePowerline");
        
        // 创建LineRenderer
        LineRenderer lineRenderer = powerline.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Standard"));
        lineRenderer.material.color = new Color(0.8f, 0.7f, 0.5f);
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, Vector3.right * 30f);
        
        return powerline;
    }
    
    /// <summary>
    /// 启动监测
    /// </summary>
    void StartMonitoring()
    {
        if (treeDangerMonitor == null) return;
        
        Debug.Log("启动树木危险监测...");
        treeDangerMonitor.enableAutoMonitoring = true;
        
        // 手动触发一次监测
        treeDangerMonitor.ManualMonitoring();
    }
    
    /// <summary>
    /// 演示循环
    /// </summary>
    IEnumerator DemoLoop()
    {
        while (demoInitialized)
        {
            yield return new WaitForSeconds(demoUpdateInterval);
            
            if (treeDangerMonitor != null)
            {
                // 显示统计信息
                var stats = treeDangerMonitor.GetDangerStatistics();
                if (stats.Count > 0)
                {
                    string statsText = "当前危险统计:\n";
                    foreach (var stat in stats)
                    {
                        statsText += $"{stat.Key}: {stat.Value}\n";
                    }
                    Debug.Log(statsText);
                }
                
                // 显示危险树木信息
                var allDangerInfo = treeDangerMonitor.GetAllDangerInfo();
                if (allDangerInfo.Count > 0)
                {
                    Debug.Log($"发现 {allDangerInfo.Count} 个危险情况");
                    
                    var criticalTrees = allDangerInfo.Where(d => d.dangerLevel >= TreeDangerMonitor.TreeDangerLevel.Critical).ToList();
                    if (criticalTrees.Count > 0)
                    {
                        Debug.LogWarning($"发现 {criticalTrees.Count} 个高危险情况！");
                        foreach (var critical in criticalTrees)
                        {
                            Debug.LogWarning($"高危险树木: {critical.tree.name} - {critical.riskDescription}");
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 重置演示场景
    /// </summary>
    [ContextMenu("重置演示场景")]
    public void ResetDemo()
    {
        Debug.Log("重置演示场景...");
        
        // 清除现有对象
        ClearDemoObjects();
        
        // 重新初始化
        StartCoroutine(InitializeDemo());
    }
    
    /// <summary>
    /// 清除演示对象
    /// </summary>
    void ClearDemoObjects()
    {
        // 清除演示树木
        GameObject[] demoTrees = GameObject.FindGameObjectsWithTag("Tree");
        foreach (var tree in demoTrees)
        {
            if (tree.name.StartsWith("DemoTree_"))
            {
                DestroyImmediate(tree);
            }
        }
        
        // 清除演示电力线
        GameObject[] demoPowerlines = GameObject.FindGameObjectsWithTag("Powerline");
        foreach (var powerline in demoPowerlines)
        {
            if (powerline.name.StartsWith("DemoPowerline_"))
            {
                DestroyImmediate(powerline);
            }
        }
        
        Debug.Log("演示对象已清除");
    }
    
    /// <summary>
    /// 手动触发监测
    /// </summary>
    [ContextMenu("手动触发监测")]
    public void ManualTriggerMonitoring()
    {
        if (treeDangerMonitor != null)
        {
            treeDangerMonitor.ManualMonitoring();
            Debug.Log("手动触发监测完成");
        }
        else
        {
            Debug.LogWarning("TreeDangerMonitor未找到");
        }
    }
    
    /// <summary>
    /// 显示监测状态
    /// </summary>
    [ContextMenu("显示监测状态")]
    public void ShowMonitoringStatus()
    {
        if (treeDangerMonitor == null)
        {
            Debug.Log("TreeDangerMonitor未找到");
            return;
        }
        
        var stats = treeDangerMonitor.GetDangerStatistics();
        var allDangerInfo = treeDangerMonitor.GetAllDangerInfo();
        
        Debug.Log($"=== 监测状态 ===\n" +
                 $"自动监测: {treeDangerMonitor.enableAutoMonitoring}\n" +
                 $"监测间隔: {treeDangerMonitor.monitoringInterval}秒\n" +
                 $"危险统计: {stats.Count} 个等级\n" +
                 $"危险情况: {allDangerInfo.Count} 个");
        
        if (allDangerInfo.Count > 0)
        {
            Debug.Log("危险树木列表:");
            foreach (var dangerInfo in allDangerInfo)
            {
                Debug.Log($"- {dangerInfo.tree.name}: {dangerInfo.dangerLevel} (距离: {dangerInfo.currentDistance:F1}m)");
            }
        }
    }
    
    void OnDestroy()
    {
        // 清理演示对象
        if (Application.isPlaying)
        {
            ClearDemoObjects();
        }
    }
    
    /// <summary>
    /// 测试方法：验证编译是否正常
    /// </summary>
    [ContextMenu("测试编译")]
    public void TestCompilation()
    {
        Debug.Log("编译测试通过！");
        
        // 测试LINQ功能
        var testList = new System.Collections.Generic.List<int> { 1, 2, 3, 4, 5 };
        var filtered = testList.Where(x => x > 3).ToList();
        Debug.Log($"LINQ测试通过，过滤结果: {string.Join(", ", filtered)}");
    }
}
