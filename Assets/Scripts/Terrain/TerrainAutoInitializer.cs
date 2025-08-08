using UnityEngine;

/// <summary>
/// 地形自动初始化器
/// 确保在打包的exe中自动创建和初始化TerrainManager
/// </summary>
public class TerrainAutoInitializer : MonoBehaviour
{
    [Header("自动初始化设置")]
    [Tooltip("是否在Start时自动创建地形管理器")]
    public bool autoCreateTerrainManager = true;
    
    [Tooltip("地形管理器的GameObject名称")]
    public string terrainManagerName = "TerrainManager";
    
    [Header("地形配置")]
    [Tooltip("地形尺寸")]
    public Vector3 terrainSize = new Vector3(160000, 200, 160000); // 扩大5倍：32000 * 5 = 160000
    
    [Tooltip("是否强制重新创建地形")]
    public bool forceRecreateTerrain = false;
    
    private TerrainManager terrainManager;
    private bool isInitialized = false;
    
    void Start()
    {
        Debug.Log($"TerrainAutoInitializer.Start() - autoCreateTerrainManager: {autoCreateTerrainManager}");
        if (autoCreateTerrainManager)
        {
            // 延迟初始化，确保在exe中正确工作
            StartCoroutine(InitializeTerrainSystemDelayed());
        }
    }
    
    /// <summary>
    /// 延迟初始化地形系统，解决exe中地形不显示的问题
    /// </summary>
    System.Collections.IEnumerator InitializeTerrainSystemDelayed()
    {
        // 等待一帧，确保所有组件都已初始化
        yield return new WaitForEndOfFrame();
        
        // 在exe中多等待一些时间
        #if !UNITY_EDITOR
        yield return new WaitForSeconds(0.2f);
        #else
        yield return new WaitForSeconds(0.1f);
        #endif
        
        InitializeTerrainSystem();
    }
    
    /// <summary>
    /// 初始化地形系统
    /// </summary>
    public void InitializeTerrainSystem()
    {
        if (isInitialized) return;
        
        Debug.Log("开始初始化地形系统...");
        
        // 查找现有的地形管理器
        terrainManager = FindObjectOfType<TerrainManager>();
        
        if (terrainManager == null)
        {
            // 创建新的地形管理器
            CreateTerrainManager();
        }
        else
        {
            // 配置现有的地形管理器
            ConfigureTerrainManager(terrainManager);
        }
        
        // 确保地形已创建
        EnsureTerrainExists();
        
        isInitialized = true;
        Debug.Log("地形系统初始化完成");
    }
    
    /// <summary>
    /// 创建新的地形管理器
    /// </summary>
    void CreateTerrainManager()
    {
        Debug.Log($"创建地形管理器: {terrainManagerName}");
        
        // 创建GameObject
        GameObject managerObj = new GameObject(terrainManagerName);
        
        // 添加地形管理器组件
        terrainManager = managerObj.AddComponent<TerrainManager>();
        
        // 配置管理器
        ConfigureTerrainManager(terrainManager);
        
        Debug.Log($"已创建地形管理器: {terrainManagerName}");
    }
    
    /// <summary>
    /// 配置地形管理器
    /// </summary>
    void ConfigureTerrainManager(TerrainManager manager)
    {
        if (manager == null) return;
        
        // 设置地形尺寸
        manager.terrainSize = terrainSize;
        
        // 查找场景初始化器并建立连接
        var sceneInitializer = FindObjectOfType<SceneInitializer>();
        if (sceneInitializer != null)
        {
            sceneInitializer.terrainManager = manager;
            Debug.Log("已将地形管理器连接到场景初始化器");
        }
        
        Debug.Log($"地形管理器配置完成 - 地形尺寸: {manager.terrainSize}");
    }
    
    /// <summary>
    /// 确保地形已创建
    /// </summary>
    void EnsureTerrainExists()
    {
        if (terrainManager == null)
        {
            Debug.LogError("地形管理器为空，无法确保地形存在");
            return;
        }
        
        // 检查地形是否已创建
        if (terrainManager.terrain == null || terrainManager.terrainData == null)
        {
            Debug.Log("地形未创建，开始创建地形...");
            
            if (forceRecreateTerrain)
            {
                // 强制重新创建地形
                terrainManager.RecreateTerrain();
            }
            else
            {
                // 尝试创建简单地形
                terrainManager.CreateSimpleTerrain();
            }
            
            // 再次检查地形是否创建成功
            if (terrainManager.terrain == null || terrainManager.terrainData == null)
            {
                Debug.LogWarning("地形创建失败，尝试强制重新创建");
                // 强制重新创建地形
                terrainManager.RecreateTerrain();
                
                // 最终检查
                if (terrainManager.terrain == null || terrainManager.terrainData == null)
                {
                    Debug.LogError("地形创建最终失败");
                }
                else
                {
                    Debug.Log("地形强制重新创建成功");
                }
            }
            else
            {
                Debug.Log("地形创建成功");
                
                // 在exe中强制刷新地形渲染
                #if !UNITY_EDITOR
                if (terrainManager.terrain != null)
                {
                    terrainManager.terrain.Flush();
                    Debug.Log("已强制刷新地形渲染");
                    
                    // 修复可能的紫色地形问题
                    terrainManager.FixPurpleTerrain();
                }
                #endif
            }
        }
        else
        {
            Debug.Log("地形已存在，无需重新创建");
        }
    }
    
    /// <summary>
    /// 获取地形管理器
    /// </summary>
    public TerrainManager GetTerrainManager()
    {
        return terrainManager;
    }
    
    /// <summary>
    /// 强制重新初始化地形系统
    /// </summary>
    [ContextMenu("强制重新初始化地形系统")]
    public void ForceReinitializeTerrainSystem()
    {
        Debug.Log("强制重新初始化地形系统...");
        
        isInitialized = false;
        
        // 销毁现有地形管理器
        if (terrainManager != null)
        {
            DestroyImmediate(terrainManager.gameObject);
            terrainManager = null;
        }
        
        // 重新初始化
        InitializeTerrainSystem();
    }
    
    /// <summary>
    /// 检查地形状态
    /// </summary>
    [ContextMenu("检查地形状态")]
    public void CheckTerrainStatus()
    {
        Debug.Log("=== 检查地形状态 ===");
        
        if (terrainManager == null)
        {
            Debug.LogError("❌ 地形管理器不存在");
            return;
        }
        
        Debug.Log($"✅ 地形管理器存在: {terrainManager.name}");
        
        if (terrainManager.terrain == null)
        {
            Debug.LogError("❌ 地形对象不存在");
        }
        else
        {
            Debug.Log($"✅ 地形对象存在: {terrainManager.terrain.name}");
            Debug.Log($"   位置: {terrainManager.terrain.transform.position}");
            Debug.Log($"   尺寸: {terrainManager.terrain.terrainData.size}");
        }
        
        if (terrainManager.terrainData == null)
        {
            Debug.LogError("❌ 地形数据不存在");
        }
        else
        {
            Debug.Log($"✅ 地形数据存在");
            Debug.Log($"   高度图分辨率: {terrainManager.terrainData.heightmapResolution}");
            Debug.Log($"   细节分辨率: {terrainManager.terrainData.detailResolution}");
            Debug.Log($"   AlphaMap分辨率: {terrainManager.terrainData.alphamapResolution}");
        }
        
        // 检查场景中的地形对象
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        Debug.Log($"场景中找到 {terrains.Length} 个地形对象");
        
        foreach (Terrain terrain in terrains)
        {
            Debug.Log($"   - {terrain.name} (位置: {terrain.transform.position})");
        }
    }
} 