using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{
    [Header("移动控制")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float gravity = -25f;        // 重力
    
    [Header("视角控制")]
    public float mouseSensitivity = 2f;
    public float verticalLookLimit = 80f;
    
    [Header("地面检测")]
    public LayerMask groundMask = -1;
    public float groundCheckDistance = 0.1f;
    
    private CharacterController characterController;
    private Camera playerCamera;
    private Vector3 velocity;
    private bool isGrounded;
    private float playerHeight = 3f; // 人眼高度，符合真实身高
    
    void Start()
    {
        // 获取或添加CharacterController组件
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }
        
        // 设置CharacterController参数（每次都更新，确保参数正确）
        characterController.height = playerHeight;
        characterController.radius = 0.4f;  // 稍微增大半径
        characterController.center = new Vector3(0, playerHeight / 2, 0);
        
        // 获取相机组件
        playerCamera = GetComponent<Camera>();
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        // 锁定鼠标到屏幕中央
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log($"第一人称视角已激活 - 视角高度:{playerHeight}m, 重力:{gravity}");
    }
    
    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleCursorToggle();
    }
    
    void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * -1;


        transform.Rotate(Vector3.up, mouseX, Space.World);
        transform.Rotate(Vector3.right, mouseY, Space.Self);
    }

    


    void HandleMovement()
    {
        Vector3 groundCheckPos = transform.position - Vector3.up * (playerHeight / 2 - 0.1f);
        isGrounded = Physics.CheckSphere(groundCheckPos, groundCheckDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;  // 稍大一点的贴地速度
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = transform.right * horizontal + transform.forward * vertical;
        direction.y = 0;
        direction.Normalize();

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : moveSpeed;

        characterController.Move(direction * currentSpeed * Time.deltaTime);

        // 手动重力
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
        
        // 确保相机不会掉到最低高度以下
        Vector3 currentPos = transform.position;
        float minHeight = 2;
        if (currentPos.y < minHeight)
        {
            currentPos.y = minHeight;
            transform.position = currentPos;
            velocity.y = 0; // 停止下降
        }
    }



    
    void HandleCursorToggle()
    {
        // ESC键切换鼠标锁定状态
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
    
    void OnEnable()
    {
        // 激活第一人称模式时的设置
        if (characterController != null)
        {
            // 确保角色控制器在合适的高度
            Vector3 pos = transform.position;
            pos.y = Mathf.Max(pos.y, 2); // 确保相机在地面以上
            transform.position = pos;
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log($"第一人称视角已激活 - 高度:{transform.position.y:F1}m, 重力:{gravity}");
    }
    
    void OnDisable()
    {
        // 退出第一人称模式时解锁鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("第一人称视角已停用");
    }
    
    // 公共方法：设置玩家位置
    public void SetPlayerPosition(Vector3 position)
    {
        // 确保位置不会太低
        float minHeight = 2;
        position.y = Mathf.Max(position.y, minHeight);
        
        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
        }
        else
        {
            transform.position = position;
        }
    }
    
    // 公共方法：获取是否在地面
    public bool IsGrounded()
    {
        return isGrounded;
    }
    
    void OnDrawGizmosSelected()
    {
        // 在Scene视图中显示地面检测范围
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, groundCheckDistance);
    }
} 