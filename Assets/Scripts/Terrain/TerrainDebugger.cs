using UnityEngine;

/// <summary>
/// 地形调试器 - 用于在运行时检查和诊断地形问题
/// </summary>
public class TerrainDebugger : MonoBehaviour
{
    [Header("调试设置")]
    public bool enableDebugLogging = true;
    public bool checkOnStart = true;
    public bool autoFixIssues = false;
    
    [Header("测试配置")]
    public Vector3[] testPositions = {
        new Vector3(0, 100, 0),
        new Vector3(100, 100, 100),
        new Vector3(-100, 100, -100),
        new Vector3(500, 100, 500)
    };
    
    void Start()
    {
        if (checkOnStart)
        {
            // 延迟检查，确保其他系统已初始化
            Invoke("DebugTerrainSystem", 2f);
        }
    }
    
    /// <summary>
    /// 调试地形系统
    /// </summary>
    [ContextMenu("调试地形系统")]
    public void DebugTerrainSystem()
    {
        if (!enableDebugLogging) return;
        
        Debug.Log("=== 开始地形系统调试 ===");
        
        CheckTerrainManager();
        CheckTerrainObjects();
        CheckTerrainAutoInitializer();
        TestTerrainHeightQueries();
        
        Debug.Log("=== 地形系统调试完成 ===");
    }
    
    /// <summary>
    /// 检查TerrainManager
    /// </summary>
    void CheckTerrainManager()
    {
        Debug.Log("--- 检查TerrainManager ---");
        
        var terrainManager = FindObjectOfType<TerrainManager>();
        if (terrainManager == null)
        {
            Debug.LogError("❌ 未找到TerrainManager");
            if (autoFixIssues)
            {
                Debug.Log("尝试自动创建TerrainManager...");
                var terrainAutoInitializer = FindObjectOfType<TerrainAutoInitializer>();
                if (terrainAutoInitializer != null)
                {
                    terrainAutoInitializer.InitializeTerrainSystem();
                }
            }
            return;
        }
        
        Debug.Log($"✅ 找到TerrainManager: {terrainManager.name}");
        
        if (terrainManager.terrain == null)
        {
            Debug.LogError("❌ TerrainManager.terrain为空");
        }
        else
        {
            Debug.Log($"✅ TerrainManager.terrain存在: {terrainManager.terrain.name}");
        }
        
        if (terrainManager.terrainData == null)
        {
            Debug.LogError("❌ TerrainManager.terrainData为空");
        }
        else
        {
            Debug.Log($"✅ TerrainManager.terrainData存在");
        }
    }
    
    /// <summary>
    /// 检查地形对象
    /// </summary>
    void CheckTerrainObjects()
    {
        Debug.Log("--- 检查地形对象 ---");
        
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        Debug.Log($"场景中找到 {terrains.Length} 个地形对象");
        
        if (terrains.Length == 0)
        {
            Debug.LogError("❌ 场景中没有地形对象");
            if (autoFixIssues)
            {
                Debug.Log("尝试自动创建地形...");
                var terrainAutoInitializer = FindObjectOfType<TerrainAutoInitializer>();
                if (terrainAutoInitializer != null)
                {
                    terrainAutoInitializer.InitializeTerrainSystem();
                }
            }
            return;
        }
        
        foreach (Terrain terrain in terrains)
        {
            Debug.Log($"✅ 地形: {terrain.name}");
            Debug.Log($"   位置: {terrain.transform.position}");
            Debug.Log($"   尺寸: {terrain.terrainData.size}");
            Debug.Log($"   高度图分辨率: {terrain.terrainData.heightmapResolution}");
            Debug.Log($"   细节分辨率: {terrain.terrainData.detailResolution}");
            Debug.Log($"   AlphaMap分辨率: {terrain.terrainData.alphamapResolution}");
            
            // 检查地形是否可见
            Renderer terrainRenderer = terrain.GetComponent<Renderer>();
            if (terrainRenderer != null)
            {
                Debug.Log($"   可见性: {terrainRenderer.isVisible}");
                Debug.Log($"   材质: {terrainRenderer.material?.name ?? "无材质"}");
            }
        }
    }
    
    /// <summary>
    /// 检查TerrainAutoInitializer
    /// </summary>
    void CheckTerrainAutoInitializer()
    {
        Debug.Log("--- 检查TerrainAutoInitializer ---");
        
        var terrainAutoInitializer = FindObjectOfType<TerrainAutoInitializer>();
        if (terrainAutoInitializer == null)
        {
            Debug.LogWarning("⚠️ 未找到TerrainAutoInitializer");
            return;
        }
        
        Debug.Log($"✅ 找到TerrainAutoInitializer: {terrainAutoInitializer.name}");
        
        var terrainManager = terrainAutoInitializer.GetTerrainManager();
        if (terrainManager == null)
        {
            Debug.LogError("❌ TerrainAutoInitializer.GetTerrainManager()返回null");
        }
        else
        {
            Debug.Log($"✅ TerrainAutoInitializer.GetTerrainManager()返回有效对象: {terrainManager.name}");
        }
    }
    
    /// <summary>
    /// 测试地形高度查询
    /// </summary>
    void TestTerrainHeightQueries()
    {
        Debug.Log("--- 测试地形高度查询 ---");
        
        var terrainManager = FindObjectOfType<TerrainManager>();
        if (terrainManager == null)
        {
            Debug.LogError("❌ 无法测试高度查询：TerrainManager不存在");
            return;
        }
        
        foreach (Vector3 testPos in testPositions)
        {
            float height = terrainManager.GetTerrainHeight(testPos);
            Debug.Log($"   位置 {testPos}: 地形高度 = {height}");
        }
    }
    
    /// <summary>
    /// 强制重新创建地形
    /// </summary>
    [ContextMenu("强制重新创建地形")]
    public void ForceRecreateTerrain()
    {
        Debug.Log("=== 强制重新创建地形 ===");
        
        // 查找并销毁现有地形
        Terrain[] existingTerrains = FindObjectsOfType<Terrain>();
        foreach (Terrain terrain in existingTerrains)
        {
            Debug.Log($"销毁现有地形: {terrain.name}");
            DestroyImmediate(terrain.gameObject);
        }
        
        // 查找并销毁现有TerrainManager
        var existingTerrainManager = FindObjectOfType<TerrainManager>();
        if (existingTerrainManager != null)
        {
            Debug.Log($"销毁现有TerrainManager: {existingTerrainManager.name}");
            DestroyImmediate(existingTerrainManager.gameObject);
        }
        
        // 重新初始化地形系统
        var terrainAutoInitializer = FindObjectOfType<TerrainAutoInitializer>();
        if (terrainAutoInitializer != null)
        {
            terrainAutoInitializer.ForceReinitializeTerrainSystem();
        }
        else
        {
            Debug.LogError("❌ 未找到TerrainAutoInitializer，无法重新创建地形");
        }
        
        Debug.Log("=== 地形重新创建完成 ===");
    }
    
    /// <summary>
    /// 创建简单地面平面
    /// </summary>
    [ContextMenu("创建简单地面平面")]
    public void CreateSimpleGroundPlane()
    {
        Debug.Log("=== 创建简单地面平面 ===");
        
        // 检查是否已存在地面平面
        GameObject existingGround = GameObject.Find("SimpleGroundPlane");
        if (existingGround != null)
        {
            Debug.Log("已存在简单地面平面，跳过创建");
            return;
        }
        
        // 创建简单的地面平面
        GameObject groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        groundPlane.name = "SimpleGroundPlane";
        groundPlane.transform.position = Vector3.zero;
        groundPlane.transform.localScale = new Vector3(100, 1, 100);
        
        // 设置材质
        Renderer renderer = groundPlane.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material groundMaterial = new Material(Shader.Find("Standard"));
            groundMaterial.color = new Color(0.3f, 0.5f, 0.2f);
            groundMaterial.SetFloat("_Glossiness", 0.0f);
            groundMaterial.SetFloat("_Metallic", 0.0f);
            renderer.material = groundMaterial;
        }
        
        Debug.Log("✅ 简单地面平面创建成功");
    }
    
    void Update()
    {
        // 按F1键触发调试
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DebugTerrainSystem();
        }
        
        // 按F2键强制重新创建地形
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ForceRecreateTerrain();
        }
        
        // 按F3键创建简单地面平面
        if (Input.GetKeyDown(KeyCode.F3))
        {
            CreateSimpleGroundPlane();
        }
    }
} 