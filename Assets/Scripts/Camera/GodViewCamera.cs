using UnityEngine;

public class GodViewCamera : MonoBehaviour
{
    [Header("缩放控制")]
    public float zoomSpeed = 10f;
    public float minZoom = 30f;
    public float maxZoom = 300f;
    
    [Header("拖拽控制")]
    public float dragSpeed = 20f;
    public bool invertDrag = false;
    
    [Header("旋转控制")]
    public float rotationSpeed = 0.1f; // 降低旋转速度
    public bool enableRotation = true;
    
    [Header("边界限制")]
    public bool useBounds = true;
    public Vector2 xBounds = new Vector2(-3000f, 3000f);
    public Vector2 zBounds = new Vector2(-3000f, 3000f);
    public Vector2 yBounds = new Vector2(15f, 600f);
    
    private Camera godCamera;
    private Vector3 lastMousePosition;
    private bool isDragging = false;
    private float currentZoom;
    
    void Start()
    {
        godCamera = GetComponent<Camera>();
        if (godCamera == null)
        {
            godCamera = Camera.main;
        }
        
        currentZoom = transform.position.y;
        
        Debug.Log("上帝视角已激活 - 鼠标滚轮缩放，左键拖拽移动，右键旋转");
    }
    
    void Update()
    {
        HandleZoom();
        HandleDrag();
        HandleRotation();
    }
    
    void HandleZoom()
    {
        // 鼠标滚轮控制缩放
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            
            Vector3 pos = transform.position;
            pos.y = currentZoom;
            
            // 应用边界限制
            if (useBounds)
            {
                pos.y = Mathf.Clamp(pos.y, yBounds.x, yBounds.y);
            }
            
            transform.position = pos;
        }
    }
    
    void HandleDrag()
    {
        // 左键拖拽移动视角
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
        
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 currentMousePosition = Input.mousePosition;
            Vector3 mouseDelta = currentMousePosition - lastMousePosition;
            
            // 计算移动方向（相对于相机的朝向）
            Vector3 right = transform.right;
            Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;
            
            // 应用拖拽移动
            float moveX = -mouseDelta.x * dragSpeed * Time.deltaTime;
            float moveZ = -mouseDelta.y * dragSpeed * Time.deltaTime;
            
            if (invertDrag)
            {
                moveX = -moveX;
                moveZ = -moveZ;
            }
            
            Vector3 movement = right * moveX + forward * moveZ;
            Vector3 newPosition = transform.position + movement;
            
            // 应用边界限制
            if (useBounds)
            {
                newPosition.x = Mathf.Clamp(newPosition.x, xBounds.x, xBounds.y);
                newPosition.z = Mathf.Clamp(newPosition.z, zBounds.x, zBounds.y);
            }
            
            transform.position = newPosition;
            lastMousePosition = currentMousePosition;
        }
    }
    
    void HandleRotation()
    {
        if (!enableRotation) return;
        
        // 右键拖拽旋转视角
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
        }
        
        if (Input.GetMouseButton(1))
        {
            Vector3 currentMousePosition = Input.mousePosition;
            Vector3 mouseDelta = currentMousePosition - lastMousePosition;
            
            // 水平旋转
            float rotationY = mouseDelta.x * rotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up, rotationY, Space.World);
            
            // 垂直旋转（俯仰角）
            float rotationX = -mouseDelta.y * rotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.right, rotationX, Space.Self);
            
            // 限制俯仰角度，避免翻转
            Vector3 eulerAngles = transform.eulerAngles;
            if (eulerAngles.x > 180f) eulerAngles.x -= 360f; // 转换为-180到180的范围
            eulerAngles.x = Mathf.Clamp(eulerAngles.x, -89f, 89f);
            transform.eulerAngles = eulerAngles;
            
            lastMousePosition = currentMousePosition;
        }
        
        // 键盘旋转（可选）
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime, Space.World);
        }
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }
    
    void OnEnable()
    {
        // 激活上帝视角时的设置
        currentZoom = transform.position.y;
        
        // 确保相机角度适合俯视
        Vector3 rotation = transform.eulerAngles;
        rotation.x = Mathf.Clamp(rotation.x, 30f, 90f); // 限制俯视角度
        transform.eulerAngles = rotation;
        
        Debug.Log("上帝视角已激活 - 鼠标滚轮缩放，左键拖拽，右键旋转");
    }
    
    void OnDisable()
    {
        isDragging = false;
        Debug.Log("上帝视角已停用");
    }
    
    // 公共方法：设置视角中心点
    public void FocusOnPoint(Vector3 targetPoint)
    {
        Vector3 newPosition = targetPoint;
        newPosition.y = currentZoom;
        
        // 应用边界限制
        if (useBounds)
        {
            newPosition.x = Mathf.Clamp(newPosition.x, xBounds.x, xBounds.y);
            newPosition.z = Mathf.Clamp(newPosition.z, zBounds.x, zBounds.y);
            newPosition.y = Mathf.Clamp(newPosition.y, yBounds.x, yBounds.y);
        }
        
        transform.position = newPosition;
    }
    
    // 公共方法：设置缩放级别
    public void SetZoom(float zoom)
    {
        currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        Vector3 pos = transform.position;
        pos.y = currentZoom;
        transform.position = pos;
    }
    
    // 公共方法：获取当前缩放级别
    public float GetCurrentZoom()
    {
        return currentZoom;
    }
    
    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        
        // 在Scene视图中显示边界范围
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((xBounds.x + xBounds.y) / 2, transform.position.y, (zBounds.x + zBounds.y) / 2);
        Vector3 size = new Vector3(xBounds.y - xBounds.x, 0.1f, zBounds.y - zBounds.x);
        Gizmos.DrawWireCube(center, size);
    }
} 