using UnityEngine;

/// <summary>
/// 地形测试脚本 - 用于验证地形修复是否有效
/// </summary>
public class TerrainTest : MonoBehaviour
{
    [Header("测试配置")]
    public bool runTestOnStart = true;
    public bool createSimpleGround = false;
    
    private TerrainManager terrainManager;
    
    void Start()
    {
        if (runTestOnStart)
        {
            TestTerrainSystem();
        }
    }
    
    [ContextMenu("测试地形系统")]
    public void TestTerrainSystem()
    {
        Debug.Log("=== 开始地形系统测试 ===");
        
        // 查找或创建TerrainManager
        terrainManager = FindObjectOfType<TerrainManager>();
        if (terrainManager == null)
        {
            Debug.Log("未找到TerrainManager，创建一个新的");
            GameObject terrainManagerObj = new GameObject("TerrainManager");
            terrainManager = terrainManagerObj.AddComponent<TerrainManager>();
        }
        
        // 测试地形创建
        TestTerrainCreation();
        
        // 测试地形高度查询
        TestTerrainHeightQuery();
        
        // 测试地面平面创建
        if (createSimpleGround)
        {
            CreateSimpleGroundPlane();
        }
        
        Debug.Log("=== 地形系统测试完成 ===");
    }
    
    void TestTerrainCreation()
    {
        Debug.Log("--- 测试地形创建 ---");
        
        try
        {
            // 创建地形
            terrainManager.CreateSimpleTerrain();
            
            // 检查地形是否创建成功
            if (terrainManager.terrain != null && terrainManager.terrainData != null)
            {
                Debug.Log($"✅ 地形创建成功");
                Debug.Log($"   地形位置: {terrainManager.terrain.transform.position}");
                Debug.Log($"   地形尺寸: {terrainManager.terrainData.size}");
                Debug.Log($"   高度图分辨率: {terrainManager.terrainData.heightmapResolution}");
                Debug.Log($"   细节分辨率: {terrainManager.terrainData.detailResolution}");
                Debug.Log($"   AlphaMap分辨率: {terrainManager.terrainData.alphamapResolution}");
            }
            else
            {
                Debug.LogError("❌ 地形创建失败");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ 地形创建异常: {ex.Message}");
        }
    }
    
    void TestTerrainHeightQuery()
    {
        Debug.Log("--- 测试地形高度查询 ---");
        
        if (terrainManager.terrain == null)
        {
            Debug.LogWarning("⚠️ 地形未创建，跳过高度查询测试");
            return;
        }
        
        // 测试几个位置的高度查询
        Vector3[] testPositions = {
            new Vector3(0, 100, 0),
            new Vector3(100, 100, 100),
            new Vector3(-100, 100, -100),
            new Vector3(500, 100, 500)
        };
        
        foreach (Vector3 pos in testPositions)
        {
            float height = terrainManager.GetTerrainHeight(pos);
            Debug.Log($"   位置 {pos}: 地形高度 = {height}");
        }
    }
    
    void CreateSimpleGroundPlane()
    {
        Debug.Log("--- 创建简单地面平面 ---");
        
        try
        {
            // 创建一个简单的平面作为地面
            GameObject groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundPlane.name = "SimpleGroundPlane";
            groundPlane.transform.position = Vector3.zero;
            groundPlane.transform.localScale = new Vector3(100, 1, 100); // 1000x1000单位
            
            // 设置材质
            Renderer renderer = groundPlane.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material groundMaterial = new Material(Shader.Find("Standard"));
                groundMaterial.color = new Color(0.3f, 0.5f, 0.2f); // 绿色
                groundMaterial.SetFloat("_Glossiness", 0.0f);
                groundMaterial.SetFloat("_Metallic", 0.0f);
                renderer.material = groundMaterial;
            }
            
            Debug.Log("✅ 简单地面平面创建成功");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ 创建简单地面平面失败: {ex.Message}");
        }
    }
    
    [ContextMenu("检查地形状态")]
    public void CheckTerrainStatus()
    {
        Debug.Log("=== 检查地形状态 ===");
        
        // 查找场景中的所有地形
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        Debug.Log($"场景中找到 {terrains.Length} 个地形对象");
        
        foreach (Terrain terrain in terrains)
        {
            Debug.Log($"地形: {terrain.name}");
            Debug.Log($"  位置: {terrain.transform.position}");
            Debug.Log($"  尺寸: {terrain.terrainData.size}");
            Debug.Log($"  高度图分辨率: {terrain.terrainData.heightmapResolution}");
            Debug.Log($"  细节分辨率: {terrain.terrainData.detailResolution}");
            Debug.Log($"  AlphaMap分辨率: {terrain.terrainData.alphamapResolution}");
            Debug.Log($"  纹理层数量: {terrain.terrainData.terrainLayers.Length}");
            Debug.Log($"  细节原型数量: {terrain.terrainData.detailPrototypes.Length}");
        }
        
        // 查找TerrainManager
        TerrainManager[] managers = FindObjectsOfType<TerrainManager>();
        Debug.Log($"场景中找到 {managers.Length} 个TerrainManager");
        
        foreach (TerrainManager manager in managers)
        {
            Debug.Log($"TerrainManager: {manager.name}");
            Debug.Log($"  地形对象: {(manager.terrain != null ? "存在" : "不存在")}");
            Debug.Log($"  地形数据: {(manager.terrainData != null ? "存在" : "不存在")}");
        }
    }
    
    [ContextMenu("修复地形问题")]
    public void FixTerrainIssues()
    {
        Debug.Log("=== 尝试修复地形问题 ===");
        
        // 查找所有地形
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        
        foreach (Terrain terrain in terrains)
        {
            try
            {
                // 检查细节分辨率（只读属性，只能检查不能修改）
                if (terrain.terrainData.detailResolution <= 0)
                {
                    Debug.LogWarning($"地形 {terrain.name} 的细节分辨率为0，这可能导致地形显示问题");
                    Debug.LogWarning("建议在Unity编辑器中重新创建地形或调整地形设置");
                }
                
                // 检查AlphaMap分辨率（只读属性，只能检查不能修改）
                if (terrain.terrainData.alphamapResolution <= 0)
                {
                    Debug.LogWarning($"地形 {terrain.name} 的AlphaMap分辨率为0，这可能导致纹理问题");
                }
                
                // 检查基础贴图分辨率（只读属性，只能检查不能修改）
                if (terrain.terrainData.baseMapResolution <= 0)
                {
                    Debug.LogWarning($"地形 {terrain.name} 的基础贴图分辨率为0，这可能导致纹理问题");
                }
                
                Debug.Log($"✅ 地形 {terrain.name} 检查完成");
                
                // 如果发现问题，建议重新创建地形
                if (terrain.terrainData.detailResolution <= 0 || 
                    terrain.terrainData.alphamapResolution <= 0 || 
                    terrain.terrainData.baseMapResolution <= 0)
                {
                    Debug.LogWarning($"建议重新创建地形 {terrain.name} 以修复分辨率问题");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ 修复地形 {terrain.name} 失败: {ex.Message}");
            }
        }
    }
    
    [ContextMenu("强制重新创建所有地形")]
    public void ForceRecreateAllTerrains()
    {
        Debug.Log("=== 强制重新创建所有地形 ===");
        
        // 查找所有TerrainManager
        TerrainManager[] managers = FindObjectsOfType<TerrainManager>();
        
        foreach (TerrainManager manager in managers)
        {
            try
            {
                Debug.Log($"重新创建地形管理器: {manager.name}");
                manager.RecreateTerrain();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"重新创建地形管理器 {manager.name} 失败: {ex.Message}");
            }
        }
        
        Debug.Log("=== 地形重新创建完成 ===");
    }
} 