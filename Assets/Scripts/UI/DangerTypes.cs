using UnityEngine;

/// <summary>
/// Billboard组件 - 让物体始终面向摄像机
/// </summary>
public class Billboard : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0); // 翻转以正确显示文字
        }
    }
}

/// <summary>
/// 危险物类型枚举
/// </summary>
public enum DangerType
{
            Building = 0,    // 建筑危险
    Vegetation = 1,  // 植被危险
    Equipment = 2,   // 设备危险
    Other = 3        // 其他危险
}

/// <summary>
/// 危险等级枚举
/// </summary>
public enum DangerLevel
{
    Low = 0,      // 低危险
    Medium = 1,   // 中等危险
    High = 2      // 高危险
}

/// <summary>
/// 危险标记组件 - 单个危险点的数据和显示
/// </summary>
public class DangerMarker : MonoBehaviour
{
    [Header("危险信息")]
    public DangerType dangerType = DangerType.Other;
    public DangerLevel dangerLevel = DangerLevel.Medium;
    public string description = "危险描述";
    public string creator = "系统";
    public System.DateTime createTime;
    
    // 公共属性
    public System.DateTime creationTime => createTime;
    
    [Header("显示设置")]
    public GameObject markerVisual;
    public TextMesh labelText;
    public float bobSpeed = 0f;        // 浮动速度（设为0禁用浮动）
    public float bobHeight = 0f;       // 浮动高度（设为0禁用浮动）
    public float markerScale = 1.2f;   // 标记缩放大小
    
    private Vector3 originalPosition;
    private Material markerMaterial;
    private Color[] dangerColors = {
        new Color(1f, 0.8f, 0f),     // 低危险 - 金黄色
        new Color(1f, 0.4f, 0f),     // 中等危险 - 橙红色
        new Color(0.9f, 0.1f, 0.1f)  // 高危险 - 深红色
    };
    
    void Start()
    {
        createTime = System.DateTime.Now;
        originalPosition = transform.position;
        SetupMarkerVisual();
        SetupLabel();  // 在SetupMarkerVisual之后调用，确保markerVisual已创建
        UpdateMarkerAppearance();
    }
    
    void Update()
    {
        // 可选的上下浮动动画（当bobSpeed > 0时启用）
        if (markerVisual != null && bobSpeed > 0f && bobHeight > 0f)
        {
            float newY = originalPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            markerVisual.transform.position = new Vector3(originalPosition.x, newY, originalPosition.z);
        }
        
        // 移除旋转动画，保持静止状态
    }
    
    /// <summary>
    /// 设置标记的3D显示
    /// </summary>
    void SetupMarkerVisual()
    {
        if (markerVisual == null)
        {
            // 创建三角形警示标志
            markerVisual = CreateTriangleWarningSign();
            markerVisual.transform.SetParent(transform);
            markerVisual.transform.localPosition = Vector3.zero;
            markerVisual.transform.localScale = Vector3.one * markerScale;
            
            // 添加Billboard组件，让整个三角形也面向摄像机
            Billboard triangleBillboard = markerVisual.AddComponent<Billboard>();
            
            // 添加碰撞器用于点击检测
            BoxCollider collider = markerVisual.AddComponent<BoxCollider>();
            if (collider != null)
            {
                collider.isTrigger = true;
                collider.size = Vector3.one * 2f; // 增大点击区域
            }
        }
        
        // 获取材质
        Renderer renderer = markerVisual.GetComponent<Renderer>();
        if (renderer != null)
        {
            markerMaterial = renderer.material;
        }
    }

    /// <summary>
    /// 创建三角形警示标志
    /// </summary>
    GameObject CreateTriangleWarningSign()
    {
        GameObject triangleObj = new GameObject("TriangleWarningSign");
        
        // 创建Mesh
        Mesh triangleMesh = new Mesh();
        
        // 定义三角形顶点 - 创建等边三角形，顶点朝上（适中尺寸）
        float height = 2.2f;
        float width = 1.9f; // sqrt(3) for equilateral triangle，适中尺寸
        
        Vector3[] vertices = new Vector3[]
        {
            // 前面三角形
            new Vector3(0, height/2, 0.02f),          // 顶点
            new Vector3(-width/2, -height/2, 0.02f), // 左下
            new Vector3(width/2, -height/2, 0.02f),  // 右下
            
            // 后面三角形（稍微后移）
            new Vector3(0, height/2, -0.02f),         // 顶点
            new Vector3(-width/2, -height/2, -0.02f), // 左下
            new Vector3(width/2, -height/2, -0.02f),  // 右下
        };
        
        // 定义三角形面
        int[] triangles = new int[]
        {
            // 前面
            0, 1, 2,
            // 后面
            3, 5, 4,
            // 左边
            0, 4, 1,
            1, 4, 5,
            // 右边
            0, 2, 3,
            2, 5, 3,
            // 底面
            1, 5, 2,
            1, 4, 5
        };
        
        // 计算法向量
        Vector3[] normals = new Vector3[]
        {
            // 前面三角形法向量
            Vector3.forward, Vector3.forward, Vector3.forward,
            // 后面三角形法向量
            Vector3.back, Vector3.back, Vector3.back
        };
        
        // UV坐标
        Vector2[] uv = new Vector2[]
        {
            new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f), // 前面
            new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f)  // 后面
        };
        
        triangleMesh.vertices = vertices;
        triangleMesh.triangles = triangles;
        triangleMesh.normals = normals;
        triangleMesh.uv = uv;
        triangleMesh.RecalculateBounds();
        triangleMesh.RecalculateNormals(); // 重新计算法向量以获得更好的光照
        
        // 添加MeshFilter和MeshRenderer
        MeshFilter meshFilter = triangleObj.AddComponent<MeshFilter>();
        meshFilter.mesh = triangleMesh;
        
        MeshRenderer meshRenderer = triangleObj.AddComponent<MeshRenderer>();
        
        // 创建材质
        Material triangleMaterial = new Material(Shader.Find("Standard"));
        triangleMaterial.color = dangerColors[1]; // 默认中等危险橙色
        triangleMaterial.SetFloat("_Metallic", 0.2f);
        triangleMaterial.SetFloat("_Glossiness", 0.8f);
        meshRenderer.material = triangleMaterial;
        
        return triangleObj;
    }

    /// <summary>
    /// 设置文本标签
    /// </summary>
    void SetupLabel()
    {
        if (labelText == null)
        {
            GameObject labelObj = new GameObject("DangerLabel");
            // 将感叹号设为三角形的子对象，这样它们会一起旋转
            labelObj.transform.SetParent(markerVisual.transform);
            labelObj.transform.localPosition = new Vector3(0, 0, 0); // 几乎贴在三角形表面
            
            labelText = labelObj.AddComponent<TextMesh>();
            labelText.text = "!";
            labelText.fontSize = 60; // 进一步减小字体大小
            labelText.color = Color.white;
            labelText.anchor = TextAnchor.MiddleCenter;
            labelText.alignment = TextAlignment.Center;
            labelText.fontStyle = FontStyle.Bold;
            
            // 调整文字大小以匹配缩小的三角形
            labelText.transform.localScale = Vector3.one * 0.2f;
            
            // 不需要为感叹号添加Billboard组件，因为整个三角形已经面向摄像机
            // 感叹号作为三角形的子对象会跟随三角形一起旋转
            
            // 添加文字描边效果（可选）
            MeshRenderer textRenderer = labelText.GetComponent<MeshRenderer>();
            if (textRenderer != null && textRenderer.material != null)
            {
                textRenderer.material.shader = Shader.Find("GUI/Text Shader");
            }
        }
    }
    
    /// <summary>
    /// 更新标记外观
    /// </summary>
    void UpdateMarkerAppearance()
    {
        if (markerMaterial != null)
        {
            markerMaterial.color = dangerColors[(int)dangerLevel];
            
            // 高危险标记添加发光效果
            if (dangerLevel == DangerLevel.High)
            {
                markerMaterial.EnableKeyword("_EMISSION");
                markerMaterial.SetColor("_EmissionColor", dangerColors[(int)dangerLevel] * 0.5f);
            }
            else
            {
                markerMaterial.DisableKeyword("_EMISSION");
            }
        }
        
        // 更新感叹号颜色 - 根据危险等级设置对比色
        if (labelText != null)
        {
            switch (dangerLevel)
            {
                case DangerLevel.Low:
                    labelText.color = Color.black; // 黄色背景用黑色感叹号
                    break;
                case DangerLevel.Medium:
                    labelText.color = Color.white; // 橙色背景用白色感叹号
                    break;
                case DangerLevel.High:
                    labelText.color = Color.white; // 红色背景用白色感叹号
                    break;
            }
        }
    }
    
    /// <summary>
    /// 设置危险信息
    /// </summary>
    public void SetDangerInfo(DangerType type, DangerLevel level, string desc, string creatorName = "用户")
    {
        dangerType = type;
        dangerLevel = level;
        description = desc;
        creator = creatorName;
        createTime = System.DateTime.Now;
        
        UpdateMarkerAppearance();
    }
    
    /// <summary>
    /// 获取危险信息描述
    /// </summary>
    public string GetDangerInfo()
    {
        return $"[{GetDangerLevelString()}] {GetDangerTypeString()}: {description}";
    }
    
    string GetDangerTypeString()
    {
        switch (dangerType)
        {
            case DangerType.Building: return "建筑危险";
            case DangerType.Vegetation: return "植被危险";
            case DangerType.Equipment: return "设备危险";
            case DangerType.Other: return "其他危险";
            default: return "未知危险";
        }
    }
    
    string GetDangerLevelString()
    {
        switch (dangerLevel)
        {
            case DangerLevel.Low: return "低危险";
            case DangerLevel.Medium: return "中等危险";
            case DangerLevel.High: return "高危险";
            default: return "未知等级";
        }
    }
} 