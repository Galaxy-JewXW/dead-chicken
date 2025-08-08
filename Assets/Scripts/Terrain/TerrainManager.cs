using UnityEngine;
using System.IO;

public class TerrainManager : MonoBehaviour
{
    [Header("地形设置")]
    public TerrainData terrainData;
    public Terrain terrain;
    public Material terrainMaterial;

    [Header("地形尺寸")]
    public Vector3 terrainSize = new Vector3(160000, 200, 160000); // 扩大5倍：32000 * 5 = 160000
    
    // 添加默认纹理
    private Texture2D grassTexture;
    private Texture2D dirtTexture;
    private Texture2D rockTexture;

    void Start()
    {
        // 延迟创建地形，确保在exe中正确初始化
        StartCoroutine(CreateTerrainDelayed());
    }
    
    /// <summary>
    /// 延迟创建地形，解决exe中地形不显示的问题
    /// </summary>
    System.Collections.IEnumerator CreateTerrainDelayed()
    {
        // 等待一帧，确保所有组件都已初始化
        yield return new WaitForEndOfFrame();
        
        // 再次等待一小段时间，确保在exe中完全初始化
        yield return new WaitForSeconds(0.1f);
        
        CreateSimpleTerrain();
        
        // 设置光照和雾效
        SetupLightingAndFog();
        
        // 强制刷新地形渲染
        if (terrain != null)
        {
            terrain.Flush();
            
            // 在exe中强制刷新地形数据
            #if !UNITY_EDITOR
            if (terrainData != null)
            {
                terrainData.RefreshPrototypes();
                Debug.Log("地形数据已强制刷新");
            }
            #endif
            
            Debug.Log("地形已强制刷新渲染");
        }
    }
    
    /// <summary>
    /// 创建简单的平面地形
    /// </summary>
    public void CreateSimpleTerrain()
    {
        try
        {
            Debug.Log("开始创建简单地形...");
            
            // 使用Inspector中配置的地形尺寸，位置居中
            Vector3 terrainPosition = new Vector3(-terrainSize.x * 0.5f, 0, -terrainSize.z * 0.5f);
            
            // 创建TerrainData
            terrainData = new TerrainData();
            terrainData.heightmapResolution = 513; // 常用的地形分辨率
            terrainData.size = terrainSize;
            
            // 注意：detailResolution和detailResolutionPerPatch是只读属性，不能在运行时修改
            // 这些值在创建TerrainData时已经设置好了
            
            // 设置控制纹理分辨率 - 确保在exe中正确设置
            terrainData.baseMapResolution = 1024;
            terrainData.alphamapResolution = 512;
            
            // 强制设置地形细节分辨率（在exe中可能丢失）
            #if !UNITY_EDITOR
            // 在exe中强制重新设置地形参数
            terrainData.SetDetailResolution(1024, 16);
            #endif
            
            Debug.Log($"地形数据创建完成 - 高度图分辨率: {terrainData.heightmapResolution}, 细节分辨率: {terrainData.detailResolution}");
            
            // 创建Terrain对象
            GameObject terrainObj = Terrain.CreateTerrainGameObject(terrainData);
            terrain = terrainObj.GetComponent<Terrain>();
            terrain.transform.position = terrainPosition;
            
            // 设置地形组件参数
            terrain.terrainData = terrainData;
            terrain.drawInstanced = false; // 关闭实例化渲染，解决exe中地形不显示的问题
            
            Debug.Log($"地形对象创建完成，位置: {terrainPosition}");
            
            // 创建平坦的高度图
            CreateFlatHeightmap();
            
            // 先创建默认的草地纹理
            CreateDefaultTextures();
            
            // 设置基本材质 - 确保在exe中正确设置
            if (terrainMaterial != null)
            {
                terrain.materialTemplate = terrainMaterial;
                Debug.Log("已设置地形材质");
            }
            else
            {
                // 在exe中创建默认材质，确保地形可见
                CreateDefaultTerrainMaterial();
                Debug.Log("已创建默认地形材质");
            }
            
            Debug.Log("简单地形创建完成");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"创建地形时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建平坦的高度图
    /// </summary>
    void CreateFlatHeightmap()
    {
        int resolution = terrainData.heightmapResolution;
        float[,] heights = new float[resolution, resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // 完全平坦的地形
                heights[y, x] = 0f;
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }
    
    /// <summary>
    /// 创建默认纹理
    /// </summary>
    private void CreateDefaultTextures()
    {
        try
        {
            Debug.Log("开始创建默认地形纹理...");
            
            // 确保地形数据有效
            if (terrainData == null)
            {
                Debug.LogError("TerrainData为空，无法创建纹理");
                return;
            }
            
            // 尝试从Resources文件夹加载纹理
            Texture2D loadedGrass = Resources.Load<Texture2D>("Textures/Grass");
            Texture2D loadedDirt = Resources.Load<Texture2D>("Textures/Dirt");
            Texture2D loadedRock = Resources.Load<Texture2D>("Textures/Rock");
            
            // 如果没有找到纹理，则创建程序生成的纹理
            grassTexture = loadedGrass != null ? loadedGrass : CreateColorTexture(new Color(0.3f, 0.5f, 0.2f));
            dirtTexture = loadedDirt != null ? loadedDirt : CreateColorTexture(new Color(0.6f, 0.4f, 0.2f));
            rockTexture = loadedRock != null ? loadedRock : CreateColorTexture(new Color(0.5f, 0.5f, 0.5f));
            
            Debug.Log("地形纹理创建完成");
            
            // 设置地形纹理
            TerrainLayer[] layers = new TerrainLayer[3];
            
            // 草地层
            layers[0] = new TerrainLayer();
            layers[0].diffuseTexture = grassTexture;
            layers[0].tileSize = new Vector2(20f, 20f);
            
            // 泥土层
            layers[1] = new TerrainLayer();
            layers[1].diffuseTexture = dirtTexture;
            layers[1].tileSize = new Vector2(20f, 20f);
            
            // 岩石层
            layers[2] = new TerrainLayer();
            layers[2].diffuseTexture = rockTexture;
            layers[2].tileSize = new Vector2(20f, 20f);
            
            // 应用纹理层
            terrainData.terrainLayers = layers;
            Debug.Log("地形纹理层设置完成");
            
            // 强制刷新地形数据
            terrainData.RefreshPrototypes();
            Debug.Log("地形数据已刷新");
            
            // 设置默认纹理权重 - 混合草地和泥土
            int alphamapWidth = terrainData.alphamapWidth;
            int alphamapHeight = terrainData.alphamapHeight;
            
            if (alphamapWidth <= 0 || alphamapHeight <= 0)
            {
                Debug.LogWarning($"AlphaMap尺寸无效: {alphamapWidth}x{alphamapHeight}，跳过纹理混合设置");
                return;
            }
            
            float[,,] alphamap = new float[alphamapWidth, alphamapHeight, 3];
            
            for (int y = 0; y < alphamapHeight; y++)
            {
                for (int x = 0; x < alphamapWidth; x++)
                {
                    // 混合草地和泥土，使地面看起来更自然
                    alphamap[x, y, 0] = 0.7f;  // 草地
                    alphamap[x, y, 1] = 0.3f;  // 泥土
                    alphamap[x, y, 2] = 0.0f;  // 岩石
                    
                    // 添加一些随机变化
                    float random = Random.Range(-0.1f, 0.1f);
                    alphamap[x, y, 0] += random;
                    alphamap[x, y, 1] -= random;
                    
                    // 确保权重在有效范围内
                    alphamap[x, y, 0] = Mathf.Clamp01(alphamap[x, y, 0]);
                    alphamap[x, y, 1] = Mathf.Clamp01(alphamap[x, y, 1]);
                    
                    // 确保权重总和为1
                    float sum = alphamap[x, y, 0] + alphamap[x, y, 1] + alphamap[x, y, 2];
                    alphamap[x, y, 0] /= sum;
                    alphamap[x, y, 1] /= sum;
                    alphamap[x, y, 2] /= sum;
                }
            }
            
            // 应用纹理混合图
            try
            {
                terrainData.SetAlphamaps(0, 0, alphamap);
                Debug.Log($"纹理混合图设置完成，尺寸: {alphamapWidth}x{alphamapHeight}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"设置纹理混合图失败: {ex.Message}");
            }
            
            // 添加草地细节
            AddGrassDetails();
            
            Debug.Log("默认地形纹理创建完成");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"创建默认纹理时出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 添加草地细节
    /// </summary>
    private void AddGrassDetails()
    {
        try
        {
            // 确保地形数据有效
            if (terrainData == null)
            {
                Debug.LogWarning("TerrainData为空，跳过草地细节创建");
                return;
            }
            
            // 检查细节分辨率（只读属性，只能检查不能修改）
            if (terrainData.detailResolution <= 0)
            {
                Debug.LogWarning("地形细节分辨率为0，这可能导致地形显示问题");
                Debug.LogWarning("建议在Unity编辑器中重新创建地形或调整地形设置");
            }
            
            // 设置多种草地细节
            DetailPrototype[] details = new DetailPrototype[3]; // 3种不同的草
            
            // 创建第一种草 - 主要的高草
            details[0] = new DetailPrototype();
            details[0].renderMode = DetailRenderMode.GrassBillboard;
            details[0].healthyColor = new Color(0.3f, 0.7f, 0.2f);
            details[0].dryColor = new Color(0.7f, 0.7f, 0.3f);
            details[0].minWidth = 1.0f;
            details[0].maxWidth = 2.0f;
            details[0].minHeight = 2.0f;
            details[0].maxHeight = 3.5f;
            details[0].density = 2.0f;
            
            // 创建第二种草 - 矮草
            details[1] = new DetailPrototype();
            details[1].renderMode = DetailRenderMode.GrassBillboard;
            details[1].healthyColor = new Color(0.25f, 0.6f, 0.15f);
            details[1].dryColor = new Color(0.6f, 0.6f, 0.2f);
            details[1].minWidth = 0.8f;
            details[1].maxWidth = 1.5f;
            details[1].minHeight = 0.8f;
            details[1].maxHeight = 1.5f;
            details[1].density = 3.0f;
            
            // 创建第三种草 - 杂草/野花
            details[2] = new DetailPrototype();
            details[2].renderMode = DetailRenderMode.GrassBillboard;
            details[2].healthyColor = new Color(0.4f, 0.7f, 0.3f);
            details[2].dryColor = new Color(0.8f, 0.7f, 0.2f);
            details[2].minWidth = 0.7f;
            details[2].maxWidth = 1.2f;
            details[2].minHeight = 1.2f;
            details[2].maxHeight = 2.0f;
            details[2].density = 1.0f;
            
            // 尝试加载草的纹理
            Texture2D grassDetailTexture1 = Resources.Load<Texture2D>("Textures/GrassDetail1");
            Texture2D grassDetailTexture2 = Resources.Load<Texture2D>("Textures/GrassDetail2");
            Texture2D grassDetailTexture3 = Resources.Load<Texture2D>("Textures/GrassDetail3");
            
            // 为草设置纹理
            details[0].usePrototypeMesh = false;
            details[0].prototypeTexture = grassDetailTexture1 != null ? grassDetailTexture1 : CreateGrassTexture(1);
            
            details[1].usePrototypeMesh = false;
            details[1].prototypeTexture = grassDetailTexture2 != null ? grassDetailTexture2 : CreateGrassTexture(2);
            
            details[2].usePrototypeMesh = false;
            details[2].prototypeTexture = grassDetailTexture3 != null ? grassDetailTexture3 : CreateGrassTexture(3);
            
            // 设置细节原型
            terrainData.detailPrototypes = details;
            
            // 为每种草创建分布图
            for (int detailIndex = 0; detailIndex < details.Length; detailIndex++)
            {
                // 创建草地分布图
                int detailMapSize = terrainData.detailResolution;
                
                // 确保细节分辨率有效
                if (detailMapSize <= 0)
                {
                    Debug.LogWarning($"细节分辨率无效: {detailMapSize}，跳过草地分布图创建");
                    continue;
                }
                
                int[,] detailMap = new int[detailMapSize, detailMapSize];
                
                // 根据不同类型的草设置不同的分布参数
                float probability = (detailIndex == 0) ? 0.5f : (detailIndex == 1) ? 0.7f : 0.3f;
                int minCount = (detailIndex == 0) ? 2 : (detailIndex == 1) ? 3 : 1;
                int maxCount = (detailIndex == 0) ? 5 : (detailIndex == 1) ? 8 : 3;
                
                // 随机分布草
                for (int y = 0; y < detailMapSize; y++)
                {
                    for (int x = 0; x < detailMapSize; x++)
                    {
                        // 随机确定是否在此处放置草
                        detailMap[y, x] = Random.Range(0f, 1f) < probability ? 
                            Random.Range(minCount, maxCount + 1) : 0;
                    }
                }
                
                // 设置草地分布图
                try
                {
                    terrainData.SetDetailLayer(0, 0, detailIndex, detailMap);
                    Debug.Log($"成功设置草地分布图 {detailIndex}，分辨率: {detailMapSize}x{detailMapSize}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"设置草地分布图 {detailIndex} 失败: {ex.Message}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"创建草地细节时出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 创建简单的草纹理
    /// </summary>
    private Texture2D CreateGrassTexture(int grassType = 1)
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        // 根据草的类型选择不同的生成参数
        float widthFactor = (grassType == 1) ? 0.3f : (grassType == 2) ? 0.4f : 0.35f;
        float heightFactor = (grassType == 1) ? 0.9f : (grassType == 2) ? 0.6f : 0.75f;
        Color baseColor = (grassType == 1) ? 
            new Color(0.2f, 0.5f, 0.1f, 1.0f) : 
            (grassType == 2) ? new Color(0.25f, 0.55f, 0.15f, 1.0f) : 
            new Color(0.3f, 0.6f, 0.2f, 1.0f);
        Color tipColor = (grassType == 1) ? 
            new Color(0.7f, 0.7f, 0.2f, 1.0f) : 
            (grassType == 2) ? new Color(0.6f, 0.65f, 0.2f, 1.0f) : 
            new Color(0.8f, 0.7f, 0.3f, 1.0f);
        
        // 创建草的形状
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                
                // 计算到中心的距离
                float centerX = size / 2;
                float centerY = size / 8; // 草的底部在下方
                float distanceX = Mathf.Abs(x - centerX);
                float distanceY = y - centerY;
                float normalizedHeight = (float)y / size;
                
                // 计算草的宽度 - 根据高度变窄
                float width = size * widthFactor * (1.0f - normalizedHeight * 0.7f);
                
                // 创建草的形状
                if (distanceY > 0 && distanceX < width)
                {
                    // 根据高度混合颜色，顶部更黄
                    float tipBlend = Mathf.Clamp01((normalizedHeight - heightFactor) / (1.0f - heightFactor) * 2.0f);
                    Color grassColor = Color.Lerp(baseColor, tipColor, tipBlend);
                    
                    // 添加一些随机变化
                    float random = Random.Range(-0.1f, 0.1f);
                    grassColor.r += random;
                    grassColor.g += random;
                    grassColor.b += random;
                    
                    // 确保颜色在有效范围内
                    grassColor.r = Mathf.Clamp01(grassColor.r);
                    grassColor.g = Mathf.Clamp01(grassColor.g);
                    grassColor.b = Mathf.Clamp01(grassColor.b);
                    
                    colors[index] = grassColor;
                    
                    // 为第三种草添加一些花的效果
                    if (grassType == 3 && normalizedHeight > 0.7f && Random.Range(0f, 1f) < 0.3f)
                    {
                        // 随机添加一些花的颜色
                        float flowerType = Random.Range(0f, 1f);
                        colors[index] = (flowerType < 0.33f) ? 
                            new Color(1f, 1f, 1f, 1f) : // 白色小花
                            (flowerType < 0.66f) ? 
                                new Color(1f, 1f, 0.2f, 1f) : // 黄色小花
                                new Color(0.8f, 0.4f, 0.8f, 1f); // 紫色小花
                    }
                }
                else
                {
                    // 透明背景
                    colors[index] = new Color(0, 0, 0, 0);
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return texture;
    }

    /// <summary>
    /// 创建纯色纹理
    /// </summary>
    private Texture2D CreateColorTexture(Color color)
    {
        // 创建一个小的纹理，用于地形
        Texture2D texture = new Texture2D(256, 256);
        Color[] colors = new Color[256 * 256];
        
        // 添加一些随机变化，使纹理看起来更自然
        for (int i = 0; i < colors.Length; i++)
        {
            float random = Random.Range(-0.1f, 0.1f);
            colors[i] = new Color(
                Mathf.Clamp01(color.r + random),
                Mathf.Clamp01(color.g + random),
                Mathf.Clamp01(color.b + random),
                1.0f
            );
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return texture;
    }
    
    /// <summary>
    /// 创建默认地形材质（用于exe中）
    /// </summary>
    private void CreateDefaultTerrainMaterial()
    {
        try
        {
            // 尝试多种shader，按优先级排序
            Material defaultMaterial = null;
            string[] shaderNames = {
                "Nature/Terrain/Standard",
                "Terrain/Standard",
                "Standard",
                "Universal Render Pipeline/Lit",
                "HDRP/Lit"
            };
            
            foreach (string shaderName in shaderNames)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    defaultMaterial = new Material(shader);
                    Debug.Log($"成功使用shader: {shaderName}");
                    break;
                }
            }
            
            if (defaultMaterial == null)
            {
                Debug.LogError("无法找到任何可用的shader，使用默认材质");
                defaultMaterial = new Material(Shader.Find("Standard"));
            }
            
            // 设置材质属性
            defaultMaterial.name = "DefaultTerrainMaterial";
            
            // 根据shader类型设置不同的属性
            if (defaultMaterial.shader.name.Contains("Terrain"))
            {
                // 地形专用shader
                defaultMaterial.SetFloat("_Glossiness", 0.0f);
                defaultMaterial.SetFloat("_Metallic", 0.0f);
                defaultMaterial.SetFloat("_Smoothness", 0.0f);
                
                // 设置地形纹理
                if (grassTexture != null)
                {
                    defaultMaterial.SetTexture("_MainTex", grassTexture);
                }
            }
            else
            {
                // 标准shader
                defaultMaterial.color = new Color(0.3f, 0.6f, 0.2f, 1.0f); // 绿色
                defaultMaterial.SetFloat("_Glossiness", 0.0f);
                defaultMaterial.SetFloat("_Metallic", 0.0f);
                defaultMaterial.SetFloat("_Smoothness", 0.0f);
                
                // 设置纹理
                if (grassTexture != null)
                {
                    defaultMaterial.SetTexture("_MainTex", grassTexture);
                }
            }
            
            // 应用材质到地形
            terrain.materialTemplate = defaultMaterial;
            
            Debug.Log($"已创建并应用默认地形材质，使用shader: {defaultMaterial.shader.name}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"创建默认地形材质失败: {ex.Message}");
            
            // 最后的备用方案：使用最简单的材质
            try
            {
                Material fallbackMaterial = new Material(Shader.Find("Standard"));
                fallbackMaterial.color = new Color(0.3f, 0.6f, 0.2f, 1.0f);
                terrain.materialTemplate = fallbackMaterial;
                Debug.Log("已应用备用材质");
            }
            catch
            {
                Debug.LogError("所有材质创建方法都失败了");
            }
        }
    }
    
    /// <summary>
    /// 设置光照和雾效
    /// </summary>
    private void SetupLightingAndFog()
    {
        // 启用雾效
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.65f, 0.7f, 0.75f, 1.0f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 150f;
        RenderSettings.fogEndDistance = 1000f;
        
        // 调整环境光
        RenderSettings.ambientIntensity = 1.0f;
        
        // 使用兼容的环境光模式设置
        #if UNITY_EDITOR
        // 在编辑器中尝试设置环境光模式
        try
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        }
        catch (System.Exception)
        {
            // 如果API不可用，使用默认设置
            Debug.Log("AmbientMode API不可用，使用默认环境光设置");
        }
        #endif
        
        RenderSettings.ambientSkyColor = new Color(0.5f, 0.5f, 0.6f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.45f, 0.4f);
        RenderSettings.ambientGroundColor = new Color(0.3f, 0.3f, 0.25f);
        
        // 调整光照
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.intensity = 0.8f;
                light.color = new Color(1.0f, 0.98f, 0.95f);
                light.shadows = LightShadows.Soft;
            }

        }
        
        // 调整反射设置
        #if UNITY_EDITOR
        // 在编辑器中尝试设置反射模式
        try
        {
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
        }
        catch (System.Exception)
        {
            // 如果API不可用，使用默认设置
            Debug.Log("DefaultReflectionMode API不可用，使用默认反射设置");
        }
        #endif
        RenderSettings.reflectionIntensity = 0.3f;
        
        // 设置地形材质不反光
        if (terrain != null && terrain.materialTemplate != null)
        {
            terrain.materialTemplate.SetFloat("_Glossiness", 0.0f);
            terrain.materialTemplate.SetFloat("_Metallic", 0.0f);
        }
    }
    
    /// <summary>
    /// 获取地形高度
    /// </summary>
    public float GetTerrainHeight(Vector3 worldPosition)
    {
        if (terrain != null && terrainData != null)
        {
            Vector3 terrainPosition = worldPosition - terrain.transform.position;
            
            if (terrainPosition.x >= 0 && terrainPosition.x <= terrainData.size.x &&
                terrainPosition.z >= 0 && terrainPosition.z <= terrainData.size.z)
            {
                return terrain.SampleHeight(worldPosition);
            }
        }
        
        return 0f;
    }
    
    /// <summary>
    /// 将点投影到地形表面
    /// </summary>
    public Vector3 ProjectPointToTerrain(Vector3 point, float heightOffset = 0f)
    {
        float terrainHeight = GetTerrainHeight(point);
        return new Vector3(point.x, terrainHeight + heightOffset, point.z);
    }
    

    
    /// <summary>
    /// 强制重新创建地形（用于修复地形问题）
    /// </summary>
    [ContextMenu("重新创建地形")]
    public void RecreateTerrain()
    {
        Debug.Log("强制重新创建地形...");
        
        // 销毁现有地形
        if (terrain != null)
        {
            DestroyImmediate(terrain.gameObject);
            terrain = null;
        }
        
        // 清理地形数据
        if (terrainData != null)
        {
            terrainData = null;
        }
        
        // 在exe中延迟重新创建地形
        #if !UNITY_EDITOR
        StartCoroutine(RecreateTerrainDelayed());
        #else
        // 在编辑器中直接重新创建
        CreateSimpleTerrain();
        #endif
    }
    
    /// <summary>
    /// 延迟重新创建地形（用于exe）
    /// </summary>
    System.Collections.IEnumerator RecreateTerrainDelayed()
    {
        // 等待一帧，确保清理完成
        yield return new WaitForEndOfFrame();
        
        // 在exe中多等待一些时间
        yield return new WaitForSeconds(0.2f);
        
        // 重新创建地形
        CreateSimpleTerrain();
        
        // 强制刷新地形渲染
        if (terrain != null)
        {
            terrain.Flush();
            Debug.Log("地形重新创建完成并已强制刷新渲染");
        }
    }
    
    /// <summary>
    /// 修复紫色地形问题
    /// </summary>
    [ContextMenu("修复紫色地形")]
    public void FixPurpleTerrain()
    {
        Debug.Log("开始修复紫色地形问题...");
        
        if (terrain == null || terrainData == null)
        {
            Debug.LogError("地形或地形数据为空，无法修复");
            return;
        }
        
        // 重新创建纹理
        CreateDefaultTextures();
        
        // 重新创建材质
        if (terrainMaterial == null)
        {
            CreateDefaultTerrainMaterial();
        }
        
        // 强制刷新
        terrain.Flush();
        terrainData.RefreshPrototypes();
        
        Debug.Log("紫色地形修复完成");
    }
}

