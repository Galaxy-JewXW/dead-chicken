using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// UI Toolkit版本的测量控制器 - 负责距离测量功能
/// </summary>
public class UIToolkitMeasureController : MonoBehaviour
{
    [Header("UI组件")]
    public UIDocument uiDocument;
    
    // 拖拽检测参数
    private const float dragThreshold = 5f;
    private const float dragTimeThreshold = 0.5f;
    
    // 测量状态
    private bool isMeasuring = false;
    private List<Vector3> measurePoints = new List<Vector3>();
    
    // 拖拽检测
    private bool isMouseDown = false;
    private Vector3 mouseDownPosition;
    private float mouseDownTime;
    
    // 可视化组件
    private LineRenderer lineRenderer;
    private List<GameObject> markers = new List<GameObject>();
    
    // UI组件
    private Button startButton;
    private Label statusLabel;
    private Label distanceLabel;
    private Label pointCountLabel;
    
    // 添加StringBuilder用于字符串拼接
    private System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
    
    // 性能优化    
    public void Initialize()
    {
        SetupLineRenderer();
        SetupUI();
    }
    
    void SetupLineRenderer()
    {
        GameObject lineObj = new GameObject("MeasureLine");
        lineRenderer = lineObj.AddComponent<LineRenderer>();
        
        // 使用简单有效的LineRenderer配置
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material.color = Color.yellow;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;
    }
    
    void SetupUI()
    {
        if (uiDocument == null) return;
        
        var root = uiDocument.rootVisualElement;
        
        // 获取UI元素
        statusLabel = root.Q<Label>("measure-status");
        distanceLabel = root.Q<Label>("measure-distance");
        pointCountLabel = root.Q<Label>("measure-points");
        startButton = root.Q<Button>("measure-start-btn");
        
        // 绑定按钮事件
        if (startButton != null)
        {
            startButton.clicked += ToggleMeasuring;
        }
        
        // 初始化UI状态
        UpdateUI();
    }
    
    void Update()
    {
        if (!isMeasuring) return;
        
        // 处理鼠标输入
        if (Input.GetMouseButtonDown(0))
        {
            isMouseDown = true;
            mouseDownPosition = Input.mousePosition;
            mouseDownTime = Time.time;
        }
        else if (Input.GetMouseButtonUp(0) && isMouseDown)
        {
            isMouseDown = false;
            
            // 检查鼠标是否在UI上，如果是则忽略此次点击
            var uiManager = FindObjectOfType<SimpleUIToolkitManager>();
            if (uiManager != null && uiManager.IsMouseOverUI())
            {
                return; // 鼠标在UI上，不处理点击
            }
            
            // 检测是否为拖拽操作
            float dragDistance = Vector3.Distance(Input.mousePosition, mouseDownPosition);
            float dragTime = Time.time - mouseDownTime;
            
            // 如果移动距离小于阈值且时间较短，认为是点击而非拖拽
            if (dragDistance < dragThreshold && dragTime < dragTimeThreshold)
            {
                // 射线检测
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    AddMeasurePoint(hit.point);
                }
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            // 右键结束测量
            StopMeasuring();
        }
    }
    
    public void ToggleMeasuring()
    {
        if (isMeasuring)
        {
            StopMeasuring();
        }
        else
        {
            StartMeasuring();
        }
    }
    
    public void StartMeasuring()
    {
        isMeasuring = true;
        measurePoints.Clear();
        
        // 重置拖拽状态
        isMouseDown = false;
        
        ClearMarkers();
        
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }
        
        UpdateUI();
    }
    
    void StopMeasuring()
    {
        isMeasuring = false;
        
        // 重置拖拽状态
        isMouseDown = false;
        
        UpdateUI();
    }
    
    public void ClearMeasurements()
    {
        measurePoints.Clear();
        isMeasuring = false;
        
        // 重置拖拽状态
        isMouseDown = false;
        
        ClearMarkers();
        
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }
        
        UpdateUI();
        
        // 通知SimpleUIToolkitManager更新UI
        try
        {
            var simpleUIManager = FindObjectOfType<SimpleUIToolkitManager>();
            if (simpleUIManager != null)
            {
                simpleUIManager.UpdateMeasureInfo();
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogWarning($"通知UI更新时出错: {ex.Message}");
        }
    }
    
    void ClearMarkers()
    {
        foreach (var marker in markers)
        {
            if (marker != null)
                DestroyImmediate(marker);
        }
        markers.Clear();
    }
    
    public void AddMeasurePoint(Vector3 point)
    {
        measurePoints.Add(point);
        CreateMarker(point, measurePoints.Count);
        
        // 确保LineRenderer被启用
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }
        
        UpdateLineRenderer();
        UpdateUI();
        
        // 通知SimpleUIToolkitManager更新UI
        try
        {
            var simpleUIManager = FindObjectOfType<SimpleUIToolkitManager>();
            if (simpleUIManager != null)
            {
                simpleUIManager.UpdateMeasureInfo();
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogWarning($"通知UI更新时出错: {ex.Message}");
        }
    }
    
    void CreateMarker(Vector3 position, int index)
    {
        // 创建红色球体标记
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * 0.4f; // 稍微增大标记点
        marker.name = "MeasureMarker_" + index;
        
        // 设置红色材质
        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material markerMaterial = new Material(Shader.Find("Standard"));
            markerMaterial.color = Color.red;
            markerMaterial.SetFloat("_Metallic", 0.3f);
            markerMaterial.SetFloat("_Smoothness", 0.8f);
            
            // 使标记点更亮，更容易看到
            markerMaterial.EnableKeyword("_EMISSION");
            markerMaterial.SetColor("_EmissionColor", Color.red * 0.3f);
            
            renderer.material = markerMaterial;
        }
        
        // 移除碰撞体，避免干扰射线检测
        Collider collider = marker.GetComponent<Collider>();
        if (collider != null)
            DestroyImmediate(collider);
        
        // 创建编号标签
        GameObject labelObj = new GameObject("MeasureLabel_" + index);
        labelObj.transform.position = position + Vector3.up * 0.7f; // 调整标签高度
        labelObj.transform.SetParent(marker.transform);
        
        // 添加TextMesh组件
        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = index.ToString();
        textMesh.fontSize = 25; // 增大字体
        textMesh.color = Color.white;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        // 设置字体样式
        textMesh.fontStyle = FontStyle.Bold;
        
        // 添加Billboard脚本让标签始终面向相机
        labelObj.AddComponent<Billboard>();
        
        
        markers.Add(marker);
    }
    
    void UpdateLineRenderer()
    {
        // 参考MeasureUIController.cs的简单有效实现
        if (lineRenderer == null || measurePoints.Count == 0) return;
        
        lineRenderer.positionCount = measurePoints.Count;
        for (int i = 0; i < measurePoints.Count; i++)
        {
            lineRenderer.SetPosition(i, measurePoints[i]);
        }
        
        // 确保线条可见
        if (measurePoints.Count >= 2)
        {
            lineRenderer.enabled = true;
        }
    }
    
    void UpdateUI()
    {
        // 简化UI更新，主要功能由SimpleUIToolkitManager处理
        if (startButton != null)
        {
            startButton.text = isMeasuring ? "停止测量" : "开始测量";
        }
        
        // 其他UI更新由各自的UI管理器处理
        if (statusLabel != null)
        {
            if (isMeasuring)
            {
                if (measurePoints.Count == 0)
                {
                    statusLabel.text = "点击场景添加测量点\n避免拖拽视角时点击\n右键结束测量";
                }
                else
                {
                    statusLabel.text = $"已添加 {measurePoints.Count} 个测量点\n继续点击添加更多点\n右键结束测量";
                }
            }
            else if (measurePoints.Count > 0)
            {
                statusLabel.text = $"测量完成\n共 {measurePoints.Count} 个测量点";
            }
            else
            {
                statusLabel.text = "点击开始测量\n支持多点连续测量";
            }
        }
        
        if (distanceLabel != null)
        {
            float totalDistance = 0f;
            
            for (int i = 1; i < measurePoints.Count; i++)
            {
                totalDistance += Vector3.Distance(measurePoints[i - 1], measurePoints[i]);
            }
            
            stringBuilder.Clear();
            if (measurePoints.Count > 1)
            {
                stringBuilder.AppendFormat("总距离: {0:F2}m", totalDistance);
                float avgDistance = totalDistance / (measurePoints.Count - 1);
                stringBuilder.AppendFormat("\n平均段距: {0:F2}m", avgDistance);
            }
            else
            {
                stringBuilder.Append("距离: 0.0m");
            }
            distanceLabel.text = stringBuilder.ToString();
        }
        
        if (pointCountLabel != null)
        {
            stringBuilder.Clear();
            if (measurePoints.Count > 1)
            {
                stringBuilder.AppendFormat("测量点: {0} ({1}段)", measurePoints.Count, measurePoints.Count - 1);
            }
            else
            {
                stringBuilder.AppendFormat("测量点: {0}", measurePoints.Count);
            }
            pointCountLabel.text = stringBuilder.ToString();
        }
    }
    
    public void Show()
    {
        // UI Toolkit版本不需要显示/隐藏逻辑，由SimpleUIToolkitManager处理
        // 这里可以做一些状态重置或初始化工作
    }
    
    public void Hide()
    {
        // 隐藏时停止测量
        if (isMeasuring)
        {
            StopMeasuring();
        }
    }
    
    void OnDestroy()
    {
        ClearMarkers();
        if (lineRenderer != null && lineRenderer.gameObject != null)
        {
            DestroyImmediate(lineRenderer.gameObject);
        }
    }
} 