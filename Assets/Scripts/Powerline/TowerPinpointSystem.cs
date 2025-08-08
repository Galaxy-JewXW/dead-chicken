using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 电塔引脚精确定位系统
/// 识别电塔的8个引脚位置并将导线精确连接到引脚上
/// </summary>
public class TowerPinpointSystem : MonoBehaviour
{
    [Header("引脚配置")]
    [Tooltip("启用精确引脚连接")]
    public bool enablePrecisePinConnection = true;
    
    [Header("引脚布局配置")]
    [Tooltip("上层横臂高度比例（相对于电塔总高度）")]
    [Range(0.7f, 0.95f)]
    public float upperCrossArmHeightRatio = 0.85f;
    
    [Tooltip("下层横臂高度比例（相对于电塔总高度）")]
    [Range(0.5f, 0.8f)]
    public float lowerCrossArmHeightRatio = 0.65f;
    
    [Tooltip("横臂宽度（米）")]
    public float crossArmWidth = 12f;
    
    [Tooltip("引脚间距（米）")]
    public float pinSpacing = 4f;
    
    [Header("高级引脚配置")]
    [Tooltip("自动检测引脚位置")]
    public bool autoDetectPins = true;
    
    [Tooltip("引脚检测精度")]
    public float pinDetectionRadius = 1f;
    
    [Header("手动调试")]
    [Tooltip("上层横臂高度（本地坐标）")]
    public float debugUpperArmHeight = 1f;
    
    [Tooltip("下层横臂高度（本地坐标）")]
    public float debugLowerArmHeight = 0.65f;
    
    [Tooltip("横臂宽度（本地坐标）")]
    public float debugArmWidth = 0.6f;
    
    [Header("引脚可视化")]
    [Tooltip("显示引脚标记")]
    public bool showPinMarkers = true;
    
    [Tooltip("引脚标记大小")]
    public float pinMarkerSize = 0.05f;
    
    [Tooltip("引脚标记颜色")]
    public Color pinMarkerColor = Color.red;
    
    [Header("导线分配")]
    [Tooltip("导线到引脚的分配方案")]
    public WireToPinMapping wireToPinMapping = WireToPinMapping.Standard_3_2_Layout;
    
    public enum WireToPinMapping
    {
        [Tooltip("标准布局：上层2个地线，下层3个主导线")]
        Standard_3_2_Layout,
        [Tooltip("对称布局：每层均匀分布")]
        Symmetric_Layout,
        [Tooltip("高压布局：6根主导线+2根地线")]
        HighVoltage_6_2_Layout,
        [Tooltip("自定义布局")]
        Custom_Layout
    }
    
    /// <summary>
    /// 电塔引脚定义
    /// </summary>
    [System.Serializable]
    public struct TowerPin
    {
        public int pinId;
        public Vector3 localPosition; // 相对于电塔的本地坐标
        public PinType pinType;
        public bool isOccupied;
        public string wireType; // "Conductor", "GroundWire", "OPGW"
        public int wireIndex;
        
        public enum PinType
        {
            UpperLeft,      // 上层左侧
            UpperRight,     // 上层右侧
            UpperCenter,    // 上层中央
            UpperOuter,     // 上层外侧
            LowerLeft,      // 下层左侧
            LowerRight,     // 下层右侧
            LowerCenter,    // 下层中央
            LowerOuter      // 下层外侧
        }
    }
    
    /// <summary>
    /// 获取电塔的8个引脚位置
    /// </summary>
    public List<TowerPin> GetTowerPins(GameObject tower)
    {
        List<TowerPin> pins = new List<TowerPin>();
        
        // 获取电塔的缩放信息
        Vector3 towerScale = tower.transform.localScale;
        float scaleFactor = towerScale.x; // 假设均匀缩放
        
        // 分析电塔的实际尺寸（使用本地坐标系）
        Renderer[] renderers = tower.GetComponentsInChildren<Renderer>();
        float actualTowerHeight = 20f; // 默认高度（本地坐标）
        float actualTowerWidth = 15f;   // 默认宽度（本地坐标）
        
        if (renderers.Length > 0)
        {
            // 计算本地边界框
            Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool boundsInitialized = false;
            
            foreach (var renderer in renderers)
            {
                // 将世界坐标边界转换为本地坐标
                Bounds worldBounds = renderer.bounds;
                Vector3 localMin = tower.transform.InverseTransformPoint(worldBounds.min);
                Vector3 localMax = tower.transform.InverseTransformPoint(worldBounds.max);
                
                Bounds rendererLocalBounds = new Bounds();
                rendererLocalBounds.SetMinMax(localMin, localMax);
                
                if (!boundsInitialized)
                {
                    localBounds = rendererLocalBounds;
                    boundsInitialized = true;
                }
                else
                {
                    localBounds.Encapsulate(rendererLocalBounds);
                }
            }
            
            if (boundsInitialized)
            {
                actualTowerHeight = localBounds.size.y;
                actualTowerWidth = Mathf.Max(localBounds.size.x, localBounds.size.z);
                
                Debug.Log($"电塔本地尺寸 - 高度: {actualTowerHeight}, 宽度: {actualTowerWidth}");
                Debug.Log($"电塔本地边界: {localBounds}");
            }
        }
        
        // 根据实际电塔尺寸调整参数
        crossArmWidth = actualTowerWidth * 0.8f; // 横臂宽度约为电塔宽度的80%
        
        // 计算两层横臂的高度（使用更保守的比例）
        float upperArmHeight = actualTowerHeight * 0.7f;  // 上层横臂在70%高度
        float lowerArmHeight = actualTowerHeight * 0.5f;  // 下层横臂在50%高度
        
        Debug.Log($"计算的横臂高度 - 上层: {upperArmHeight}, 下层: {lowerArmHeight}, 横臂宽度: {crossArmWidth}");
        
        // 基于实际观察：电塔有四层横臂，每层前后各有1个引脚，总共8个引脚
        // 横臂沿着Z轴方向延伸（前后方向），不是X轴方向（左右方向）
        
        float frontArmZ = debugArmWidth * 0.5f;   // 前侧横臂位置（正Z方向）
        float backArmZ = -debugArmWidth * 0.5f;   // 后侧横臂位置（负Z方向）
        
        // 计算四层横臂的高度
        float layer1Y = debugUpperArmHeight;                    // 最上层
        float layer2Y = debugUpperArmHeight * 0.8f;             // 第二层
        float layer3Y = debugLowerArmHeight;                    // 第三层
        float layer4Y = debugLowerArmHeight * 0.7f;             // 最下层
        
        // 前侧4个引脚（从上到下四层）
        pins.Add(new TowerPin
        {
            pinId = 0,
            localPosition = new Vector3(0f, layer1Y, frontArmZ), // 前侧第1层（最上）
            pinType = TowerPin.PinType.UpperLeft,
            isOccupied = false
        });
        
        pins.Add(new TowerPin
        {
            pinId = 1,
            localPosition = new Vector3(0f, layer2Y, frontArmZ), // 前侧第2层
            pinType = TowerPin.PinType.UpperLeft,
            isOccupied = false
        });
        
        pins.Add(new TowerPin
        {
            pinId = 2,
            localPosition = new Vector3(0f, layer3Y, frontArmZ), // 前侧第3层
            pinType = TowerPin.PinType.LowerLeft,
            isOccupied = false
        });
        
        pins.Add(new TowerPin
        {
            pinId = 3,
            localPosition = new Vector3(0f, layer4Y, frontArmZ), // 前侧第4层（最下）
            pinType = TowerPin.PinType.LowerLeft,
            isOccupied = false
        });
        
        // 后侧4个引脚（从上到下四层）
        pins.Add(new TowerPin
        {
            pinId = 4,
            localPosition = new Vector3(0f, layer1Y, backArmZ), // 后侧第1层（最上）
            pinType = TowerPin.PinType.UpperRight,
            isOccupied = false
        });
        
        pins.Add(new TowerPin
        {
            pinId = 5,
            localPosition = new Vector3(0f, layer2Y, backArmZ), // 后侧第2层
            pinType = TowerPin.PinType.UpperRight,
            isOccupied = false
        });
        
        pins.Add(new TowerPin
        {
            pinId = 6,
            localPosition = new Vector3(0f, layer3Y, backArmZ), // 后侧第3层
            pinType = TowerPin.PinType.LowerRight,
            isOccupied = false
        });
        
        pins.Add(new TowerPin
        {
            pinId = 7,
            localPosition = new Vector3(0f, layer4Y, backArmZ), // 后侧第4层（最下）
            pinType = TowerPin.PinType.LowerRight,
            isOccupied = false
        });
        
        Debug.Log($"生成了 {pins.Count} 个引脚（四层，前后各4个）");
        Debug.Log($"引脚位置 - 前侧Z: {frontArmZ}, 后侧Z: {backArmZ}");
        Debug.Log($"引脚高度 - 第1层: {layer1Y}, 第2层: {layer2Y}, 第3层: {layer3Y}, 第4层: {layer4Y}");
        
        return pins;
    }
    
    /// <summary>
    /// 根据导线类型和索引分配引脚
    /// </summary>
    public Vector3 GetPinPositionForWire(GameObject tower, string wireType, int wireIndex, int totalConductors, int totalGroundWires)
    {
        List<TowerPin> pins = GetTowerPins(tower);
        
        // 根据分配方案选择引脚
        int pinIndex = GetPinIndexForWire(wireType, wireIndex, totalConductors, totalGroundWires);
        
        if (pinIndex >= 0 && pinIndex < pins.Count)
        {
            Vector3 localPos = pins[pinIndex].localPosition;
            // 转换为世界坐标
            return tower.transform.TransformPoint(localPos);
        }
        
        // 如果没有找到合适的引脚，返回默认位置
        return tower.transform.position + Vector3.up * 20f;
    }
    
    /// <summary>
    /// 根据导线类型和索引获取引脚索引
    /// </summary>
    int GetPinIndexForWire(string wireType, int wireIndex, int totalConductors, int totalGroundWires)
    {
        switch (wireToPinMapping)
        {
            case WireToPinMapping.Standard_3_2_Layout:
                return GetStandardLayoutPinIndex(wireType, wireIndex, totalConductors, totalGroundWires);
                
            case WireToPinMapping.Symmetric_Layout:
                return GetSymmetricLayoutPinIndex(wireType, wireIndex, totalConductors, totalGroundWires);
                
            case WireToPinMapping.HighVoltage_6_2_Layout:
                return GetHighVoltageLayoutPinIndex(wireType, wireIndex, totalConductors, totalGroundWires);
                
            case WireToPinMapping.Custom_Layout:
            default:
                return GetCustomLayoutPinIndex(wireType, wireIndex, totalConductors, totalGroundWires);
        }
    }
    
    /// <summary>
    /// 标准布局：四层引脚，地线在上两层，主导线在下两层
    /// </summary>
    int GetStandardLayoutPinIndex(string wireType, int wireIndex, int totalConductors, int totalGroundWires)
    {
        if (wireType == "GroundWire")
        {
            // 地线连接到上两层引脚（引脚0,1,4,5）
            switch (wireIndex)
            {
                case 0: return 0; // 左侧第1层（最上）
                case 1: return 4; // 右侧第1层（最上）
                case 2: return 1; // 左侧第2层
                case 3: return 5; // 右侧第2层
                default: return 0; // 默认左侧第1层
            }
        }
        else // Conductor
        {
            // 主导线连接到下两层引脚（引脚2,3,6,7）
            switch (wireIndex)
            {
                case 0: return 2; // 左侧第3层
                case 1: return 6; // 右侧第3层
                case 2: return 3; // 左侧第4层（最下）
                case 3: return 7; // 右侧第4层（最下）
                default: return 2; // 默认左侧第3层
            }
        }
    }
    
    /// <summary>
    /// 对称布局：每层均匀分布
    /// </summary>
    int GetSymmetricLayoutPinIndex(string wireType, int wireIndex, int totalConductors, int totalGroundWires)
    {
        if (wireType == "GroundWire")
        {
            // 地线均匀分布在上层
            float ratio = totalGroundWires > 1 ? (float)wireIndex / (totalGroundWires - 1) : 0.5f;
            int pinIndex = Mathf.RoundToInt(ratio * 3); // 0-3的范围
            return pinIndex;
        }
        else // Conductor
        {
            // 主导线均匀分布在下层
            float ratio = totalConductors > 1 ? (float)wireIndex / (totalConductors - 1) : 0.5f;
            int pinIndex = Mathf.RoundToInt(ratio * 3) + 4; // 4-7的范围
            return pinIndex;
        }
    }
    
    /// <summary>
    /// 高压布局：6根主导线+2根地线
    /// </summary>
    int GetHighVoltageLayoutPinIndex(string wireType, int wireIndex, int totalConductors, int totalGroundWires)
    {
        if (wireType == "GroundWire")
        {
            // 地线在上层外侧
            return wireIndex == 0 ? 0 : 3; // 最左和最右
        }
        else // Conductor
        {
            // 6根主导线分布在两层
            if (wireIndex < 4)
            {
                // 前4根在下层
                return 4 + wireIndex;
            }
            else
            {
                // 后2根在上层中间
                return 1 + (wireIndex - 4);
            }
        }
    }
    
    /// <summary>
    /// 自定义布局
    /// </summary>
    int GetCustomLayoutPinIndex(string wireType, int wireIndex, int totalConductors, int totalGroundWires)
    {
        // 可以根据需要自定义布局逻辑
        return GetStandardLayoutPinIndex(wireType, wireIndex, totalConductors, totalGroundWires);
    }
    
    /// <summary>
    /// 为电塔添加引脚可视化标记
    /// </summary>
    public void AddPinMarkers(GameObject tower)
    {
        if (!showPinMarkers) return;
        
        List<TowerPin> pins = GetTowerPins(tower);
        
        foreach (var pin in pins)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"Pin_{pin.pinId}_{pin.pinType}";
            marker.transform.SetParent(tower.transform);
            marker.transform.localPosition = pin.localPosition;
            marker.transform.localScale = Vector3.one * pinMarkerSize;
            
            // 设置材质
            Renderer renderer = marker.GetComponent<Renderer>();
            Material pinMaterial = new Material(Shader.Find("Standard"));
            pinMaterial.color = pinMarkerColor;
            pinMaterial.SetFloat("_Metallic", 0.8f);
            pinMaterial.SetFloat("_Smoothness", 0.9f);
            renderer.material = pinMaterial;
            
            // 移除碰撞器
            DestroyImmediate(marker.GetComponent<Collider>());
        }
    }
    
    /// <summary>
    /// 获取引脚信息文本
    /// </summary>
    public string GetPinLayoutInfo()
    {
        return $"引脚布局: {wireToPinMapping}\n" +
               $"上层横臂高度: {upperCrossArmHeightRatio:P0}\n" +
               $"下层横臂高度: {lowerCrossArmHeightRatio:P0}\n" +
               $"横臂宽度: {crossArmWidth}m\n" +
               $"引脚间距: {pinSpacing}m";
    }
    
    /// <summary>
    /// 测试引脚位置
    /// </summary>
    [ContextMenu("测试引脚位置")]
    public void TestPinPositions()
    {
        // 首先尝试通过标签查找
        GameObject[] towers = null;
        try
        {
            towers = GameObject.FindGameObjectsWithTag("Tower");
        }
        catch (UnityException)
        {
            Debug.LogWarning("Tower标签未定义，将通过名称查找电塔");
            towers = new GameObject[0];
        }
        
        if (towers.Length == 0)
        {
            // 查找名称包含Tower的对象
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains("Tower") || obj.name.Contains("GoodTower"))
                {
                    Debug.Log($"找到电塔: {obj.name}");
                    TestSingleTowerPins(obj);
                    return;
                }
            }
            Debug.LogError("未找到任何电塔GameObject！请确保场景中有名称包含'Tower'或'GoodTower'的对象。");
        }
        else
        {
            TestSingleTowerPins(towers[0]);
        }
    }
    
    void TestSingleTowerPins(GameObject tower)
    {
        Debug.Log($"=== 测试电塔引脚位置: {tower.name} ===");
        Debug.Log($"电塔位置: {tower.transform.position}");
        Debug.Log($"电塔缩放: {tower.transform.localScale}");
        
        // 分析电塔结构
        AnalyzeTowerStructure(tower);
        
        List<TowerPin> pins = GetTowerPins(tower);
        for (int i = 0; i < pins.Count; i++)
        {
            Vector3 worldPos = tower.transform.TransformPoint(pins[i].localPosition);
            Debug.Log($"引脚 {i}: {pins[i].pinType}, 本地坐标: {pins[i].localPosition}, 世界坐标: {worldPos}");
        }
        
        // 添加可视化标记
        AddPinMarkers(tower);
        
        Debug.Log($"引脚测试完成，共 {pins.Count} 个引脚");
    }
    
    /// <summary>
    /// 分析电塔的实际结构
    /// </summary>
    void AnalyzeTowerStructure(GameObject tower)
    {
        Debug.Log("=== 分析电塔结构 ===");
        
        // 获取电塔的所有子对象
        Transform[] allChildren = tower.GetComponentsInChildren<Transform>();
        Debug.Log($"电塔总共有 {allChildren.Length} 个子对象");
        
        // 分析电塔的边界
        Renderer[] renderers = tower.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds totalBounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                totalBounds.Encapsulate(renderer.bounds);
            }
            
            Debug.Log($"电塔边界: Center={totalBounds.center}, Size={totalBounds.size}");
            Debug.Log($"电塔高度: {totalBounds.size.y}m");
            Debug.Log($"电塔宽度: {totalBounds.size.x}m");
            Debug.Log($"电塔深度: {totalBounds.size.z}m");
            
            // 根据实际尺寸调整引脚参数
            float actualHeight = totalBounds.size.y;
            float actualWidth = totalBounds.size.x;
            
            // 更新横臂宽度（基于电塔实际宽度）
            crossArmWidth = actualWidth * 0.8f; // 横臂宽度约为电塔宽度的80%
            
            Debug.Log($"调整后的横臂宽度: {crossArmWidth}m");
        }
        
        // 查找可能的引脚位置（通过子对象名称）
        List<Transform> potentialPins = new List<Transform>();
        foreach (Transform child in allChildren)
        {
            string name = child.name.ToLower();
            if (name.Contains("pin") || name.Contains("insulator") || name.Contains("connector"))
            {
                potentialPins.Add(child);
                Debug.Log($"发现潜在引脚: {child.name} at {child.position}");
            }
        }
        
        if (potentialPins.Count > 0)
        {
            Debug.Log($"发现 {potentialPins.Count} 个潜在引脚位置");
        }
        else
        {
            Debug.Log("未发现明显的引脚标识，将使用几何计算方法");
        }
    }
    
    /// <summary>
    /// 从CSV文件加载电力塔并创建实例
    /// </summary>
    /// <param name="csvFilePath">CSV文件路径</param>
    /// <param name="towerPrefab">电塔预制件</param>
    /// <param name="parentObject">父对象</param>
    public void LoadTowersFromCsv(string csvFilePath, GameObject towerPrefab = null, Transform parentObject = null)
    {
        Debug.Log($"开始从CSV加载电力塔: {csvFilePath}");
        
        try
        {
            // 读取CSV文件
            if (!System.IO.File.Exists(csvFilePath))
            {
                Debug.LogError($"CSV文件不存在: {csvFilePath}");
                return;
            }
            
            string[] lines = System.IO.File.ReadAllLines(csvFilePath);
            
            // 清除现有的电塔
            ClearExistingTowers();
            
            // 获取或查找电塔预制件
            if (towerPrefab == null)
            {
                // 尝试从Resources加载默认的电塔预制件
                towerPrefab = Resources.Load<GameObject>("Prefabs/GoodTower");
                if (towerPrefab == null)
                {
                    Debug.LogError("未找到电塔预制件，请提供towerPrefab参数或确保Resources/Prefabs/GoodTower.prefab存在");
                    return;
                }
            }
            
            // 创建电塔父对象
            if (parentObject == null)
            {
                GameObject powerlineParent = GameObject.Find("PowerlineParent");
                if (powerlineParent == null)
                {
                    powerlineParent = new GameObject("PowerlineParent");
                }
                parentObject = powerlineParent.transform;
            }
            
            List<GameObject> createdTowers = new List<GameObject>();
            int towerCount = 0;
            
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                string[] tokens = line.Split(',');
                
                if (tokens.Length >= 3 && 
                    float.TryParse(tokens[0], out float x) &&
                    float.TryParse(tokens[1], out float y) &&
                    float.TryParse(tokens[2], out float z))
                {
                    // 创建电塔位置
                    Vector3 position = new Vector3(x, y, z);
                    
                    // 实例化电塔
                    GameObject tower = Object.Instantiate(towerPrefab, position, Quaternion.identity, parentObject);
                    tower.name = $"Tower_{towerCount:D3}";
                    
                    // 尝试设置标签（如果标签存在的话）
                    try
                    {
                        tower.tag = "Tower";
                    }
                    catch (UnityException)
                    {
                        Debug.LogWarning("Tower标签未定义，请在Unity的Tags & Layers中添加Tower标签");
                    }
                    
                    // 添加TowerPinpointSystem组件（如果没有的话）
                    TowerPinpointSystem towerPinSystem = tower.GetComponent<TowerPinpointSystem>();
                    if (towerPinSystem == null)
                    {
                        towerPinSystem = tower.AddComponent<TowerPinpointSystem>();
                        // 复制当前组件的配置
                        CopyConfigurationTo(towerPinSystem);
                    }
                    
                    // 添加引脚标记（如果启用）
                    if (showPinMarkers)
                    {
                        towerPinSystem.AddPinMarkers(tower);
                    }
                    
                    createdTowers.Add(tower);
                    towerCount++;
                    
                    Debug.Log($"创建电塔 {towerCount}: 位置 ({x:F2}, {y:F2}, {z:F2})");
                }
                else
                {
                    Debug.LogWarning($"跳过无效的CSV行: {line}");
                }
            }
            
            Debug.Log($"成功从CSV创建了 {createdTowers.Count} 个电力塔");
            
            // 如果有SceneInitializer，触发电力线重建
            SceneInitializer sceneInitializer = FindObjectOfType<SceneInitializer>();
            if (sceneInitializer != null)
            {
                Debug.Log("找到SceneInitializer，触发电力线重建...");
                sceneInitializer.RegenerateAll();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"从CSV加载电力塔失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 清除现有的电塔
    /// </summary>
    void ClearExistingTowers()
    {
        // 查找并删除现有的电塔
        GameObject[] existingTowers = null;
        try
        {
            existingTowers = GameObject.FindGameObjectsWithTag("Tower");
        }
        catch (UnityException)
        {
            Debug.LogWarning("Tower标签未定义，跳过标签查找");
            existingTowers = new GameObject[0];
        }
        
        foreach (GameObject tower in existingTowers)
        {
            Object.DestroyImmediate(tower);
        }
        
        // 也查找名称包含Tower的对象
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Tower") && obj != this.gameObject)
            {
                Object.DestroyImmediate(obj);
            }
        }
        
        Debug.Log("已清除现有的电塔");
    }
    
    /// <summary>
    /// 将当前组件的配置复制到目标组件
    /// </summary>
    void CopyConfigurationTo(TowerPinpointSystem target)
    {
        target.enablePrecisePinConnection = this.enablePrecisePinConnection;
        target.upperCrossArmHeightRatio = this.upperCrossArmHeightRatio;
        target.lowerCrossArmHeightRatio = this.lowerCrossArmHeightRatio;
        target.crossArmWidth = this.crossArmWidth;
        target.pinSpacing = this.pinSpacing;
        target.autoDetectPins = this.autoDetectPins;
        target.pinDetectionRadius = this.pinDetectionRadius;
        target.debugUpperArmHeight = this.debugUpperArmHeight;
        target.debugLowerArmHeight = this.debugLowerArmHeight;
        target.debugArmWidth = this.debugArmWidth;
        target.showPinMarkers = this.showPinMarkers;
        target.pinMarkerSize = this.pinMarkerSize;
        target.pinMarkerColor = this.pinMarkerColor;
        target.wireToPinMapping = this.wireToPinMapping;
    }
} 