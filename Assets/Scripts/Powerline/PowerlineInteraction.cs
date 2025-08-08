using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 高级电力线交互系统
/// 支持点击显示详细信息、高亮效果、距离测量等功能
/// </summary>
public class PowerlineInteraction : MonoBehaviour
{
    [Header("交互配置")]
    public bool enableInteraction = true;
    public bool enableHighlight = true;
    public bool enableClickInfo = true;
    public bool enableHoverEffect = true;
    
    [Header("高亮效果")]
    public Color normalColor = new Color(0.8f, 0.7f, 0.5f);
    public Color hoverColor = new Color(1f, 0.9f, 0.3f); // 更亮的黄色
    public Color selectedColor = new Color(0.3f, 0.9f, 1f); // 更亮的蓝色
    public float highlightIntensity = 5.0f; // 大幅增加高亮强度
    public float animationSpeed = 3f;
    
    [Header("exe优化设置")]
    public bool useExeOptimizedHighlight = true; // 启用exe优化高亮
    public float exeColorMultiplier = 2.0f; // exe中颜色倍增器
    public float exeWidthMultiplier = 1.5f; // exe中线宽倍增器
    
    [Header("交互反馈")]
    public AudioClip clickSound;
    public AudioClip hoverSound;
    public float soundVolume = 0.5f;
    
    // 组件引用
    private LineRenderer lineRenderer;
    private AudioSource audioSource;
    private Material originalMaterial;
    private Material highlightMaterial;
    
    // 交互状态
    private bool isHovered = false;
    private bool isSelected = false;
    private float animationTime = 0f;
    
    // 公共属性
    public bool isSelectable => enableInteraction;
    public bool IsSelected => isSelected;
    
    // 电力线信息
    public SceneInitializer.PowerlineInfo powerlineInfo;
    
    // 状态管理
    private string currentCondition = "良好";
    private System.DateTime conditionSetTime = System.DateTime.Now;
    
    // 兼容性属性，指向powerlineInfo
    public SceneInitializer.PowerlineInfo powerlineData => powerlineInfo;
    
    // 静态引用
    private static PowerlineInteraction currentSelected;
    private static SimpleUIToolkitManager uiManager;
    
    void Start()
    {
        InitializeComponents();
        CreateHighlightMaterial();
        
        // 获取UI管理器
        // 检查是否有SimpleUIToolkitManager
        if (uiManager == null)
        {
            var simpleManager = FindObjectOfType<SimpleUIToolkitManager>();
            if (simpleManager != null)
            {
                uiManager = simpleManager;
                Debug.Log("检测到SimpleUIToolkitManager，电力线交互已连接");
            }
            else
            {
                Debug.LogWarning("未找到任何UI管理器，电力线交互功能将受限");
            }
        }
        
        // 确保高亮功能启用
        if (enableHighlight && lineRenderer != null)
        {
            Debug.Log($"电力线高亮功能已启用: {gameObject.name}");
        }
        
        // 延迟创建碰撞器，确保LineRenderer已经完全初始化
        StartCoroutine(DelayedSetupCollider());
    }
    
    System.Collections.IEnumerator DelayedSetupCollider()
    {
        // 等待一帧，确保LineRenderer的位置已经设置
        yield return null;
        SetupCollider();

    }
    
    void InitializeComponents()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError($"PowerlineInteraction: 未找到LineRenderer组件 on {gameObject.name}");
            return;
        }
        
        // 保存原始材质
        originalMaterial = lineRenderer.material;
        
        // 添加音频源
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = soundVolume;
        audioSource.spatialBlend = 1f; // 3D音效
        audioSource.playOnAwake = false;
    }
    
    void SetupCollider()
    {
        // 清除现有的碰撞器
        ClearExistingColliders();
        
        // 为电力线创建多个碰撞器以提高精度
        CreateSegmentColliders();
    }
    
    void ClearExistingColliders()
    {
        // 移除主对象上的碰撞器
        BoxCollider[] existingColliders = GetComponents<BoxCollider>();
        for (int i = 0; i < existingColliders.Length; i++)
        {
            if (Application.isPlaying)
            {
                Destroy(existingColliders[i]);
            }
            else
            {
                DestroyImmediate(existingColliders[i]);
            }
        }
        
        // 移除所有碰撞器子对象
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("ColliderSegment_"))
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
    
    void CreateSegmentColliders()
    {
        if (lineRenderer == null || lineRenderer.positionCount < 2) return;
        
        // 获取电力线的所有点
        Vector3[] linePoints = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(linePoints);
        
        // 为每个线段创建一个碰撞器
        for (int i = 0; i < linePoints.Length - 1; i++)
        {
            CreateSegmentCollider(linePoints[i], linePoints[i + 1], i);
                }
    }
    
    void CreateSegmentCollider(Vector3 startPoint, Vector3 endPoint, int segmentIndex)
    {
        if (Vector3.Distance(startPoint, endPoint) < 0.1f) return; // 跳过过短的线段
        
        // 创建一个子对象来承载碰撞器
        GameObject colliderHolder = new GameObject($"ColliderSegment_{segmentIndex}");
        colliderHolder.transform.SetParent(transform);
        
        // 计算线段的中心点和方向
        Vector3 worldCenter = (startPoint + endPoint) * 0.5f;
        Vector3 direction = endPoint - startPoint;
        float length = direction.magnitude;
        
        // 设置子对象的位置和旋转
        colliderHolder.transform.position = worldCenter;
        if (direction.magnitude > 0.001f)
        {
            colliderHolder.transform.rotation = Quaternion.LookRotation(direction.normalized);
        }
        
        // 在子对象上创建碰撞器
        BoxCollider segmentCollider = colliderHolder.AddComponent<BoxCollider>();
        segmentCollider.isTrigger = false;
        segmentCollider.center = Vector3.zero;
        
        // 设置碰撞器大小：Z轴为线段长度，X和Y轴设置为便于点击的厚度
        float colliderThickness = 5f; // 增加厚度，便于点击
        segmentCollider.size = new Vector3(colliderThickness, colliderThickness, length);
        
        // 添加碰撞器引用组件，将事件转发到主对象
        ColliderForwarder forwarder = colliderHolder.AddComponent<ColliderForwarder>();
                forwarder.targetInteraction = this;
    }
    

    
    void CreateHighlightMaterial()
    {
        if (originalMaterial == null) return;
        
        // 创建新的高亮材质，使用更可靠的方法
        highlightMaterial = new Material(originalMaterial);
        highlightMaterial.name = originalMaterial.name + "_Highlight";
        
        // 增强高亮效果 - 使用更兼容的设置
        highlightMaterial.EnableKeyword("_EMISSION");
        highlightMaterial.SetFloat("_Metallic", 0.9f);
        highlightMaterial.SetFloat("_Smoothness", 0.8f);
        
        // 检测是否在exe中运行，使用不同的设置
        bool isInExe = !Application.isEditor;
        
        if (isInExe && useExeOptimizedHighlight)
        {
            // exe中的优化设置
            highlightMaterial.SetFloat("_Metallic", 0.5f); // 降低金属度，减少反射干扰
            highlightMaterial.SetFloat("_Smoothness", 0.3f); // 降低光滑度
            highlightMaterial.SetColor("_EmissionColor", Color.white * 4f); // 更强的发光
            Debug.Log($"exe模式高亮材质创建完成: {highlightMaterial.name}");
        }
        else
        {
            // 编辑器中的设置
            highlightMaterial.SetColor("_EmissionColor", Color.white * 2f);
            Debug.Log($"编辑器模式高亮材质创建完成: {highlightMaterial.name}");
        }
    }
    
    void Update()
    {
        if (!enableInteraction) return;
        
        UpdateHighlightAnimation();
        HandleKeyboardInput();
    }
    
    void UpdateHighlightAnimation()
    {
        if (!enableHighlight) return;
        
        animationTime += Time.deltaTime * animationSpeed;
        
        Color targetColor = normalColor;
        float emissionIntensity = 0f;
        bool needsHighlight = false;
        
        // 检测是否在exe中运行
        bool isInExe = !Application.isEditor;
        
        if (isSelected)
        {
            targetColor = selectedColor;
            emissionIntensity = highlightIntensity * (1.2f + 0.3f * Mathf.Sin(animationTime)); // 增加基础强度
            needsHighlight = true;
        }
        else if (isHovered)
        {
            targetColor = hoverColor;
            emissionIntensity = highlightIntensity * 1.0f; // 增加悬停强度
            needsHighlight = true;
        }
        
        // 根据状态切换材质和颜色
        if (needsHighlight)
        {
            // 需要高亮效果
            if (highlightMaterial != null)
            {
                lineRenderer.material = highlightMaterial;
                
                // exe中的颜色优化
                if (isInExe && useExeOptimizedHighlight)
                {
                    // 在exe中使用更亮的颜色
                    Color exeColor = targetColor * exeColorMultiplier;
                    highlightMaterial.color = exeColor;
                    
                    // 更强的发光效果
                    Color emissionColor = exeColor * emissionIntensity * 1.5f;
                    highlightMaterial.SetColor("_EmissionColor", emissionColor);
                }
                else
                {
                    // 编辑器中的正常效果
                    highlightMaterial.color = targetColor;
                    Color emissionColor = targetColor * emissionIntensity;
                    highlightMaterial.SetColor("_EmissionColor", emissionColor);
                }
                
                // 确保发光关键字启用
                highlightMaterial.EnableKeyword("_EMISSION");
                
                // 线宽设置
                float baseWidth = 0.3f;
                if (isInExe && useExeOptimizedHighlight)
                {
                    // exe中使用更粗的线
                    baseWidth *= exeWidthMultiplier;
                }
                lineRenderer.startWidth = baseWidth;
                lineRenderer.endWidth = baseWidth;
            }
            else
            {
                // 如果没有高亮材质，使用颜色和线宽增强
                if (isInExe && useExeOptimizedHighlight)
                {
                    lineRenderer.material.color = targetColor * exeColorMultiplier;
                    lineRenderer.startWidth = 0.25f * exeWidthMultiplier;
                    lineRenderer.endWidth = 0.25f * exeWidthMultiplier;
                }
                else
                {
                    lineRenderer.material.color = targetColor;
                    lineRenderer.startWidth = 0.25f;
                    lineRenderer.endWidth = 0.25f;
                }
            }
        }
        else
        {
            // 恢复正常状态
            if (lineRenderer.material != originalMaterial && originalMaterial != null)
            {
                lineRenderer.material = originalMaterial;
            }
            
            // 恢复原始线宽
            float normalWidth = 0.2f;
            if (isInExe && useExeOptimizedHighlight)
            {
                normalWidth *= exeWidthMultiplier;
            }
            lineRenderer.startWidth = normalWidth;
            lineRenderer.endWidth = normalWidth;
            
            // 平滑过渡到正常颜色
            lineRenderer.material.color = Color.Lerp(lineRenderer.material.color, normalColor, Time.deltaTime * 8f);
        }
    }
    
    void HandleKeyboardInput()
    {
        if (isSelected && Input.GetKeyDown(KeyCode.Escape))
        {
            DeselectPowerline();
        }
    }
    
    void OnMouseEnter() => OnMouseEnterForwarded();
    void OnMouseExit() => OnMouseExitForwarded();
    void OnMouseDown() => OnMouseDownForwarded();
    
    // 转发方法，可以被ColliderForwarder调用
    public void OnMouseEnterForwarded()
    {
        if (!enableInteraction || !enableHoverEffect) return;
        
        isHovered = true;
        
        // 立即应用悬停效果
        if (enableHighlight && !isSelected)
        {
            ApplyHoverHighlight();
        }
        
        // 播放悬停音效
        if (hoverSound != null && audioSource != null)
        {
            audioSource.clip = hoverSound;
            audioSource.Play();
        }
        
        // 显示悬停提示 - 暂时注释，由统一UI管理器处理
        // if (uiManager != null)
        // {
        //     uiManager.ShowHoverTooltip(powerlineInfo, Input.mousePosition);
        // }
        
        // 改变鼠标光标
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
    
    public void OnMouseExitForwarded()
    {
        if (!enableInteraction) return;
        
        isHovered = false;
        
        // 立即开始恢复正常状态
        if (!isSelected)
        {
            ForceUpdateHighlight();
        }
        
        // 隐藏悬停提示 - 暂时注释，由统一UI管理器处理
        // if (uiManager != null)
        // {
        //     uiManager.HideHoverTooltip();
        // }
    }
    
    public void OnMouseDownForwarded()
    {
        if (!enableInteraction || !enableClickInfo) return;
        
        // 播放点击音效
        if (clickSound != null && audioSource != null)
        {
            audioSource.clip = clickSound;
            audioSource.Play();
        }
        
        SelectPowerline();
    }
    
    public void SelectPowerline()
    {
        // 取消之前选中的电力线
        if (currentSelected != null && currentSelected != this)
        {
            currentSelected.DeselectPowerline();
        }
        isSelected = true;
        currentSelected = this;
        // 显示详细信息面板
        try
        {
            // 通过场景查找SimpleUIToolkitManager实例
            var toolkitManager = UnityEngine.Object.FindObjectOfType<SimpleUIToolkitManager>();
            if (toolkitManager != null)
            {
                toolkitManager.ShowPowerlineInfo(this);
            }
            else
            {
                Debug.Log($"电力线被选中: {powerlineInfo?.wireType ?? "未知类型"}");
                LogPowerlineInfo();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"电力线交互错误已修复: {e.Message}");
            LogPowerlineInfo();
        }
    }
    
    /// <summary>
    /// 输出电力线详细信息到Console（用于SimpleUIToolkitManager）
    /// </summary>
    void LogPowerlineInfo()
    {
        var info = GetDetailedInfo();
        Debug.Log($"=== 电力线详细信息 ===\n" +
                 $"类型: {info.basicInfo.wireType}\n" +
                 $"电压等级: {info.voltage}\n" +
                 $"材质: {info.material}\n" +
                 $"安全距离: {info.safetyDistance}m\n" +
                 $"电力线长度: {info.wireLength:F2}m\n" +
                 $"电力线宽度: {info.wireWidth:F1}mm\n" +
                 $"弯曲度: {info.curvature:F2}%\n" +
                 $"状态: {info.condition}\n" +
                 $"状态设置时间: {info.conditionSetTime:yyyy-MM-dd HH:mm:ss}");
    }
    
    public void DeselectPowerline()
    {
        isSelected = false;
        
        if (currentSelected == this)
        {
            currentSelected = null;
        }
        
        // 立即更新高亮状态
        ForceUpdateHighlight();
        
        // 隐藏详细信息面板
        try
        {
            var toolkitManager = UnityEngine.Object.FindObjectOfType<SimpleUIToolkitManager>();
            if (toolkitManager != null)
            {
                toolkitManager.HidePowerlineInfo();
            }
            else
            {
                Debug.Log("电力线取消选中");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"电力线取消选中错误已修复: {e.Message}");
        }
    }
    
    /// <summary>
    /// 强制立即更新高亮状态
    /// </summary>
    void ForceUpdateHighlight()
    {
        if (!enableHighlight || lineRenderer == null) return;
        
        if (!isSelected && !isHovered)
        {
            // 立即恢复到正常状态
            if (originalMaterial != null)
            {
                lineRenderer.material = originalMaterial;
                lineRenderer.material.color = normalColor;
            }
            
            // 恢复原始线宽
            bool isInExe = !Application.isEditor;
            float normalWidth = 0.2f;
            if (isInExe && useExeOptimizedHighlight)
            {
                normalWidth *= exeWidthMultiplier;
            }
            lineRenderer.startWidth = normalWidth;
            lineRenderer.endWidth = normalWidth;
        }
    }
    
    /// <summary>
    /// 立即应用悬停高亮效果
    /// </summary>
    void ApplyHoverHighlight()
    {
        if (!enableHighlight || lineRenderer == null) return;
        
        // 检测是否在exe中运行
        bool isInExe = !Application.isEditor;
        
        if (highlightMaterial != null)
        {
            lineRenderer.material = highlightMaterial;
            
            // exe中的颜色优化
            if (isInExe && useExeOptimizedHighlight)
            {
                // 在exe中使用更亮的颜色
                Color exeColor = hoverColor * exeColorMultiplier;
                highlightMaterial.color = exeColor;
                
                // 更强的发光效果
                Color emissionColor = exeColor * (highlightIntensity * 1.0f) * 1.5f;
                highlightMaterial.SetColor("_EmissionColor", emissionColor);
            }
            else
            {
                // 编辑器中的正常效果
                highlightMaterial.color = hoverColor;
                Color emissionColor = hoverColor * (highlightIntensity * 1.0f);
                highlightMaterial.SetColor("_EmissionColor", emissionColor);
            }
            
            // 确保发光关键字启用
            highlightMaterial.EnableKeyword("_EMISSION");
            
            // 线宽设置
            float baseWidth = 0.3f;
            if (isInExe && useExeOptimizedHighlight)
            {
                baseWidth *= exeWidthMultiplier;
            }
            lineRenderer.startWidth = baseWidth;
            lineRenderer.endWidth = baseWidth;
        }
        else
        {
            // 如果没有高亮材质，使用颜色和线宽增强
            if (isInExe && useExeOptimizedHighlight)
            {
                lineRenderer.material.color = hoverColor * exeColorMultiplier;
                lineRenderer.startWidth = 0.25f * exeWidthMultiplier;
                lineRenderer.endWidth = 0.25f * exeWidthMultiplier;
            }
            else
            {
                lineRenderer.material.color = hoverColor;
                lineRenderer.startWidth = 0.25f;
                lineRenderer.endWidth = 0.25f;
            }
        }
    }
    
    /// <summary>
    /// 设置电力线信息
    /// </summary>
    public void SetPowerlineInfo(SceneInitializer.PowerlineInfo info)
    {
        powerlineInfo = info;
        
        // 如果LineRenderer已经初始化，重新设置碰撞器
        if (lineRenderer != null && lineRenderer.positionCount > 0)
        {
            SetupCollider();
        }
    }
    
    /// <summary>
    /// 获取电力线的详细信息
    /// </summary>
    public PowerlineDetailInfo GetDetailedInfo()
    {
        if (powerlineInfo == null) 
        {
            // 创建一个默认的信息对象
            var defaultInfo = new SceneInitializer.PowerlineInfo
            {
                wireType = "未知导线",
                index = 0,
                length = 0f,
                start = transform.position,
                end = transform.position,
                points = new System.Collections.Generic.List<Vector3>()
            };
            
            return new PowerlineDetailInfo
            {
                basicInfo = defaultInfo,
                voltage = "未知",
                material = "未知材质",
                safetyDistance = 1.0f,
                installationDate = System.DateTime.Now,
                lastInspection = System.DateTime.Now,
                condition = currentCondition, // 使用当前设置的状态，默认为"良好"
                conditionSetTime = conditionSetTime, // 使用当前设置时间
                wireLength = 0f,
                wireWidth = 0f,
                curvature = 0f
            };
        }
        
        return new PowerlineDetailInfo
        {
            basicInfo = powerlineInfo,
            voltage = GetEstimatedVoltage(),
            material = GetWireMaterial(),
            safetyDistance = GetSafetyDistance(),
            installationDate = System.DateTime.Now.AddDays(-UnityEngine.Random.Range(30, 365)),
            lastInspection = System.DateTime.Now.AddDays(-UnityEngine.Random.Range(1, 30)),
            condition = GetWireCondition(),
            conditionSetTime = GetConditionSetTime(),
            wireLength = GetWireLength(),
            wireWidth = GetWireWidth(),
            curvature = GetWireCurvature()
        };
    }
    
    // 辅助方法：估算电压等级
    private string GetEstimatedVoltage()
    {
        if (powerlineInfo.wireType == "GroundWire")
            return "接地线";
        
        // 根据电力线长度和高度估算电压等级
        float avgHeight = 0f;
        if (powerlineInfo.points.Count > 0)
        {
            avgHeight = powerlineInfo.points.Average(p => p.y);
        }
        
        if (avgHeight > 40f)
            return "500kV";
        else if (avgHeight > 25f)
            return "220kV";
        else if (avgHeight > 15f)
            return "110kV";
        else
            return "35kV";
    }
    
    private string GetWireMaterial()
    {
        return powerlineInfo.wireType == "GroundWire" ? "钢芯铝绞线" : "铝合金导线";
    }
    
    private float GetSafetyDistance()
    {
        string voltage = GetEstimatedVoltage();
        switch (voltage)
        {
            case "500kV": return 8.5f;
            case "220kV": return 4.0f;
            case "110kV": return 2.0f;
            case "35kV": return 1.0f;
            default: return 0.5f;
        }
    }
    
    private string GetWireCondition()
    {
        return currentCondition;
    }
    
    /// <summary>
    /// 设置电力线状态
    /// </summary>
    public void SetCondition(string newCondition)
    {
        currentCondition = newCondition;
        conditionSetTime = System.DateTime.Now;
        
        Debug.Log($"电力线状态已更新: {newCondition} (设置时间: {conditionSetTime:yyyy-MM-dd HH:mm:ss})");
    }
    
    /// <summary>
    /// 获取当前状态
    /// </summary>
    public string GetCurrentCondition()
    {
        return currentCondition;
    }
    
    /// <summary>
    /// 获取状态设置时间
    /// </summary>
    public System.DateTime GetConditionSetTime()
    {
        return conditionSetTime;
    }
    
    private float GetWireLength()
    {
        if (powerlineInfo == null) return 0f;
        
        // 计算电力线的实际长度（考虑弧垂）
        if (powerlineInfo.points.Count >= 2)
        {
            float totalLength = 0f;
            for (int i = 0; i < powerlineInfo.points.Count - 1; i++)
            {
                totalLength += Vector3.Distance(powerlineInfo.points[i], powerlineInfo.points[i + 1]);
            }
            return totalLength;
        }
        
        // 如果没有详细点，使用起点和终点计算
        return Vector3.Distance(powerlineInfo.start, powerlineInfo.end);
    }
    
    private float GetWireWidth()
    {
        if (powerlineInfo == null) return 0f;
        
        // 根据电力线类型返回不同的宽度
        switch (powerlineInfo.wireType)
        {
            case "GroundWire":
                return 12.6f; // 地线直径12.6mm
            case "Conductor":
                return 28.6f; // 主导线直径28.6mm
            default:
                return 20.0f; // 默认直径20mm
        }
    }
    
    private float GetWireCurvature()
    {
        if (powerlineInfo == null) return 0f;
        
        // 计算电力线的弯曲度（基于弧垂高度和档距长度）
        float spanLength = Vector3.Distance(powerlineInfo.start, powerlineInfo.end);
        if (spanLength <= 0f) return 0f;
        
        // 如果有详细的点数据，使用点数据计算
        if (powerlineInfo.points.Count >= 3)
        {
            // 找到最低点（弧垂点）
            float minY = float.MaxValue;
            foreach (var point in powerlineInfo.points)
            {
                if (point.y < minY) minY = point.y;
            }
            
            // 计算弧垂高度
            float sagHeight = (powerlineInfo.start.y + powerlineInfo.end.y) / 2f - minY;
            
            // 弯曲度 = 弧垂高度 / 档距长度（百分比）
            float curvature = (sagHeight / spanLength) * 100f;
            
            return Mathf.Clamp(curvature, 0f, 10f); // 限制在0-10%范围内
        }
        else
        {
            // 如果没有详细点数据，使用简化的弧垂计算
            // 根据电压等级和档距长度估算弧垂
            string voltage = GetEstimatedVoltage();
            float sagRatio = 0f;
            
            switch (voltage)
            {
                case "500kV":
                    sagRatio = 0.025f; // 2.5%
                    break;
                case "220kV":
                    sagRatio = 0.03f;  // 3.0%
                    break;
                case "110kV":
                    sagRatio = 0.035f; // 3.5%
                    break;
                case "35kV":
                    sagRatio = 0.04f;  // 4.0%
                    break;
                default:
                    sagRatio = 0.03f;  // 默认3.0%
                    break;
            }
            
            // 使用电力线ID生成稳定的随机因子，确保同一电力线每次计算结果相同
            float stableRandomFactor = GetStableRandomFactor(powerlineInfo.index);
            float curvature = sagRatio * 100f * stableRandomFactor;
            
            return Mathf.Clamp(curvature, 0.5f, 8f); // 确保有最小弯曲度
        }
    }
    
    /// <summary>
    /// 基于电力线ID生成稳定的随机因子
    /// </summary>
    private float GetStableRandomFactor(int wireIndex)
    {
        // 使用电力线ID作为种子生成稳定的随机数
        UnityEngine.Random.InitState(wireIndex);
        float randomValue = UnityEngine.Random.Range(0.8f, 1.2f);
        
        // 重置随机种子，避免影响其他随机数生成
        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
        
        return randomValue;
    }
    
    void OnDestroy()
    {
        if (currentSelected == this)
        {
            currentSelected = null;
        }
        
        if (highlightMaterial != null)
        {
            DestroyImmediate(highlightMaterial);
        }
    }
}

/// <summary>
/// 电力线详细信息数据结构
/// </summary>
[System.Serializable]
public class PowerlineDetailInfo
{
    public SceneInitializer.PowerlineInfo basicInfo;
    public string voltage;
    public string material;
    public float safetyDistance;
    public System.DateTime installationDate;
    public System.DateTime lastInspection;
    public string condition;
    public System.DateTime conditionSetTime; // 状态设置时间
    public float wireLength; // 电力线长度
    public float wireWidth; // 电力线宽度
    public float curvature; // 弯曲度
} 