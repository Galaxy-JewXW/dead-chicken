using UnityEngine;

/// <summary>
/// 强制地形初始化器 - 确保在打包的exe中地形能够正确显示
/// </summary>
public class TerrainForceInitializer : MonoBehaviour
{
    [Header("强制初始化设置")]
    public bool forceInitializeOnStart = true;
    public bool enableDebugLogging = true;
    
    [Header("地形配置")]
    public Vector3 terrainSize = new Vector3(160000, 200, 160000); // 扩大5倍：32000 * 5 = 160000
    public Material groundMaterial;
    
    private bool isInitialized = false;
    
    void Start()
    {
        if (forceInitializeOnStart)
        {
            // 延迟初始化，确保其他系统已启动
            Invoke("ForceInitializeTerrain", 0.5f);
        }
    }
    
    /// <summary>
    /// 强制初始化地形
    /// </summary>
    [ContextMenu("强制初始化地形")]
    public void ForceInitializeTerrain()
    {
        if (isInitialized) return;
        
        Debug.Log("=== 开始强制地形初始化 ===");
        
        // 步骤1：检查现有地形
        CheckExistingTerrain();
        
        // 步骤2：创建或修复TerrainManager
        CreateOrFixTerrainManager();
        
        // 步骤3：确保地形对象存在
        EnsureTerrainExists();
        
        // 步骤4：验证地形系统
        ValidateTerrainSystem();
        
        isInitialized = true;
        Debug.Log("=== 强制地形初始化完成 ===");
    }
    
    /// <summary>
    /// 检查现有地形
    /// </summary>
    void CheckExistingTerrain()
    {
        Debug.Log("--- 检查现有地形 ---");
        
        // 检查TerrainManager
        var terrainManager = FindObjectOfType<TerrainManager>();
        if (terrainManager != null)
        {
            Debug.Log($"✅ 找到TerrainManager: {terrainManager.name}");
            if (terrainManager.terrain != null)
            {
                Debug.Log($"✅ TerrainManager.terrain存在: {terrainManager.terrain.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ TerrainManager.terrain为空");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ 未找到TerrainManager");
        }
        
        // 检查Terrain对象
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        Debug.Log($"场景中找到 {terrains.Length} 个Terrain对象");
        
        foreach (Terrain terrain in terrains)
        {
            Debug.Log($"   - {terrain.name} (位置: {terrain.transform.position})");
        }
    }
    
    /// <summary>
    /// 创建或修复TerrainManager
    /// </summary>
    void CreateOrFixTerrainManager()
    {
        Debug.Log("--- 创建或修复TerrainManager ---");
        
        var terrainManager = FindObjectOfType<TerrainManager>();
        
        if (terrainManager == null)
        {
            // 创建新的TerrainManager
            GameObject managerObj = new GameObject("TerrainManager");
            terrainManager = managerObj.AddComponent<TerrainManager>();
            Debug.Log("✅ 已创建新的TerrainManager");
        }
        
        // 设置地形尺寸
        terrainManager.terrainSize = terrainSize;
        
        // 如果地形不存在，强制创建
        if (terrainManager.terrain == null)
        {
            Debug.Log("强制创建地形...");
            terrainManager.CreateSimpleTerrain();
            
            // 再次检查
            if (terrainManager.terrain == null)
            {
                Debug.LogError("❌ 地形创建失败，将创建备用地面");
            }
            else
            {
                Debug.Log("✅ 地形创建成功");
            }
        }
    }
    
    /// <summary>
    /// 确保地形对象存在
    /// </summary>
    void EnsureTerrainExists()
    {
        Debug.Log("--- 确保地形对象存在 ---");
        
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        
        if (terrains.Length == 0)
        {
            Debug.LogWarning("⚠️ 场景中没有Terrain对象，尝试创建...");
            
            // 尝试通过TerrainManager创建
            var terrainManager = FindObjectOfType<TerrainManager>();
            if (terrainManager != null)
            {
                terrainManager.CreateSimpleTerrain();
            }
            
            // 再次检查
            terrains = FindObjectsOfType<Terrain>();
            if (terrains.Length == 0)
            {
                Debug.LogError("❌ 无法创建Terrain对象");
            }
            else
            {
                Debug.Log($"✅ 成功创建 {terrains.Length} 个Terrain对象");
            }
        }
        else
        {
            Debug.Log($"✅ 场景中有 {terrains.Length} 个Terrain对象");
        }
    }
    
    /// <summary>
    /// 创建备用地面
    /// </summary>

    
    /// <summary>
    /// 验证地形系统
    /// </summary>
    void ValidateTerrainSystem()
    {
        Debug.Log("--- 验证地形系统 ---");
        
        // 检查TerrainManager
        var terrainManager = FindObjectOfType<TerrainManager>();
        if (terrainManager != null)
        {
            Debug.Log($"✅ TerrainManager: {terrainManager.name}");
            
            if (terrainManager.terrain != null)
            {
                Debug.Log($"✅ 地形对象: {terrainManager.terrain.name}");
                Debug.Log($"   位置: {terrainManager.terrain.transform.position}");
                Debug.Log($"   尺寸: {terrainManager.terrain.terrainData.size}");
            }
            else
            {
                Debug.LogWarning("⚠️ TerrainManager.terrain为空");
            }
        }
        else
        {
            Debug.LogError("❌ 未找到TerrainManager");
        }
        
        // 检查Terrain对象
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        Debug.Log($"✅ 场景中有 {terrains.Length} 个Terrain对象");
        
        // 检查是否有可见的地面
        bool hasVisibleGround = (terrains.Length > 0);
        if (hasVisibleGround)
        {
            Debug.Log("✅ 地形系统验证通过 - 有可见地形");
        }
        else
        {
            Debug.LogError("❌ 地形系统验证失败 - 没有可见地形");
        }
    }
    
    /// <summary>
    /// 强制重新创建地形
    /// </summary>
    [ContextMenu("强制重新创建地形")]
    public void ForceRecreateTerrain()
    {
        Debug.Log("=== 强制重新创建地形 ===");
        
        isInitialized = false;
        
        // 销毁现有地形
        Terrain[] existingTerrains = FindObjectsOfType<Terrain>();
        foreach (Terrain terrain in existingTerrains)
        {
            Debug.Log($"销毁现有地形: {terrain.name}");
            DestroyImmediate(terrain.gameObject);
        }
        
        // 销毁现有TerrainManager
        var existingTerrainManager = FindObjectOfType<TerrainManager>();
        if (existingTerrainManager != null)
        {
            Debug.Log($"销毁现有TerrainManager: {existingTerrainManager.name}");
            DestroyImmediate(existingTerrainManager.gameObject);
        }
        
        // 销毁备用地面
        GameObject fallbackGround = GameObject.Find("FallbackGround");
        if (fallbackGround != null)
        {
            Debug.Log("销毁备用地面");
            DestroyImmediate(fallbackGround);
        }
        
        // 重新初始化
        ForceInitializeTerrain();
    }
    
    void Update()
    {
        // 按F5键强制重新初始化地形
        if (Input.GetKeyDown(KeyCode.F5))
        {
            ForceRecreateTerrain();
        }
        
        // 按F6键检查地形状态
        if (Input.GetKeyDown(KeyCode.F6))
        {
            ValidateTerrainSystem();
        }
    }
} 