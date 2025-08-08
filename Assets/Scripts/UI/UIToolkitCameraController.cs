using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit 相机控制器 - Material Design风格
/// </summary>
public class UIToolkitCameraController : MonoBehaviour
{
    private SimpleUIToolkitManager uiManager;
    private VisualElement container;
    private VisualElement cameraPanel;
    private Button firstPersonBtn;
    private Button godViewBtn;
    private Button flyViewBtn;
    private Button resetBtn;
    private Label cameraInfoLabel;
    private VisualElement settingsCard;
    
    // 相机组件引用
    private CameraManager cameraManager;
    
    public void Initialize(SimpleUIToolkitManager manager, VisualElement parent)
    {
        uiManager = manager;
        container = parent;
        cameraManager = FindObjectOfType<CameraManager>();
        
        CreateCameraUI();
    }
    
    void CreateCameraUI()
    {
        cameraPanel = new VisualElement();
        cameraPanel.name = "camera-panel";
        cameraPanel.AddToClassList("material-card");
        cameraPanel.style.display = DisplayStyle.None;

        var title = new Label("相机控制");
        title.AddToClassList("card-title");
        cameraPanel.Add(title);

        container.Add(cameraPanel);
    }
    
    void CreateModeButtons()
    {
        var modeSection = new VisualElement();
        modeSection.AddToClassList("card-content");
        
        var modeTitle = new Label("视角模式");
        modeTitle.style.color = new Color(0, 0, 0, 0.87f);
        modeSection.Add(modeTitle);
        
        // 按钮容器
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Column;
        
        // 创建模式按钮
        firstPersonBtn = CreateModeButton("🚶 第一人称", "切换到第一人称视角", () => {
            SwitchCameraMode("FirstPerson");
        });
        
        godViewBtn = CreateModeButton("🗺️ 上帝视角", "切换到上帝视角", () => {
            SwitchCameraMode("GodView");
        });
        
        flyViewBtn = CreateModeButton("🚁 飞行视角", "切换到自由飞行视角", () => {
            SwitchCameraMode("Fly");
        });
        
        resetBtn = CreateModeButton("🔄 重置相机", "重置相机到默认位置", () => {
            ResetCamera();
        });
        
        buttonContainer.Add(firstPersonBtn);
        buttonContainer.Add(godViewBtn);
        buttonContainer.Add(flyViewBtn);
        buttonContainer.Add(resetBtn);
        
        modeSection.Add(buttonContainer);
        cameraPanel.Add(modeSection);
    }
    
    Button CreateModeButton(string text, string tooltip, System.Action onClick)
    {
        var button = new Button();
        button.text = text;
        button.clicked += onClick;
        
        // 简化的按钮样式 - 只使用基本属性
        button.style.height = 48;
        button.style.marginBottom = 8;
        button.style.backgroundColor = new Color(0.396f, 0.4f, 0.945f, 1f); // Primary color
        button.style.color = Color.white;
        
        // 悬停效果 - 简化版本，避免使用不支持的属性
        button.RegisterCallback<MouseEnterEvent>(evt => {
            button.style.backgroundColor = new Color(0.314f, 0.275f, 0.898f, 1f); // Darker primary
        });
        
        button.RegisterCallback<MouseLeaveEvent>(evt => {
            button.style.backgroundColor = new Color(0.396f, 0.4f, 0.945f, 1f);
        });
        
        return button;
    }
    
    void CreateInfoDisplay()
    {
        var infoSection = new VisualElement();
        infoSection.AddToClassList("card-content");
        // infoSection.style.marginTop = 16;
        
        var infoTitle = new Label("相机信息");
        infoTitle.style.color = new Color(0, 0, 0, 0.87f);
        infoSection.Add(infoTitle);
        
        // 信息显示区域
        var infoContainer = new VisualElement();
        infoContainer.style.backgroundColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        
        cameraInfoLabel = new Label("位置: (0, 0, 0)\n旋转: (0, 0, 0)\n模式: 正常");
        cameraInfoLabel.style.color = new Color(0, 0, 0, 0.6f);
        
        infoContainer.Add(cameraInfoLabel);
        infoSection.Add(infoContainer);
        cameraPanel.Add(infoSection);
    }
    
    void CreateSettingsCard()
    {
        settingsCard = new VisualElement();
        settingsCard.AddToClassList("material-card");
        settingsCard.style.backgroundColor = new Color(0.98f, 0.98f, 1f, 1f);
        
        var settingsTitle = new Label("相机设置");
        settingsTitle.AddToClassList("card-title");
        settingsCard.Add(settingsTitle);
        
        // 移动速度滑块
        var speedContainer = new VisualElement();
        
        var speedLabel = new Label("移动速度");
        speedContainer.Add(speedLabel);
        
        var speedSlider = new Slider("移动速度", 1f, 20f);
        speedSlider.value = 5f;
        speedContainer.Add(speedSlider);
        
        settingsCard.Add(speedContainer);
        
        // 鼠标灵敏度滑块
        var sensitivityContainer = new VisualElement();
        
        var sensitivityLabel = new Label("鼠标灵敏度");
        sensitivityContainer.Add(sensitivityLabel);
        
        var sensitivitySlider = new Slider("鼠标灵敏度", 0.1f, 5f);
        sensitivitySlider.value = 1f;
        sensitivityContainer.Add(sensitivitySlider);
        
        settingsCard.Add(sensitivityContainer);
        
        // 平滑移动开关
        var smoothContainer = new VisualElement();
        
        var smoothToggle = new Toggle("平滑移动");
        smoothToggle.value = true;
        smoothContainer.Add(smoothToggle);
        
        settingsCard.Add(smoothContainer);
        
        cameraPanel.Add(settingsCard);
    }
    
    void CreateShortcutHelp()
    {
        var helpCard = new VisualElement();
        helpCard.style.backgroundColor = new Color(1f, 0.98f, 0.9f, 1f);
        
        var helpTitle = new Label("💡 快捷键提示");
        helpTitle.style.color = new Color(0.8f, 0.5f, 0f, 1f);
        helpCard.Add(helpTitle);
        
        var helpText = new Label(
            "• 数字键 1 - 第一人称视角\n" +
            "• 数字键 2 - 上帝视角\n" +
            "• 数字键 3 - 飞行视角\n" +
            "• R键 - 重置相机\n" +
            "• ESC键 - 退出相机模式"
        );
        helpText.style.color = new Color(0.6f, 0.4f, 0f, 1f);
        helpCard.Add(helpText);
        
        cameraPanel.Add(helpCard);
    }
    
    void SwitchCameraMode(string mode)
    {
        if (cameraManager != null)
        {
            // 这里调用实际的相机切换逻辑
            Debug.Log($"切换相机模式: {mode}");
            uiManager.UpdateStatusBar($"相机模式: {mode}");
            
            // 更新按钮状态
            UpdateButtonStates(mode);
        }
    }
    
    void UpdateButtonStates(string activeMode)
    {
        // 重置所有按钮样式
        ResetButtonStyle(firstPersonBtn);
        ResetButtonStyle(godViewBtn);
        ResetButtonStyle(flyViewBtn);
        
        // 高亮当前活动按钮
        Button activeButton = null;
        switch (activeMode)
        {
            case "FirstPerson":
                activeButton = firstPersonBtn;
                break;
            case "GodView":
                activeButton = godViewBtn;
                break;
            case "Fly":
                activeButton = flyViewBtn;
                break;
        }
        
        if (activeButton != null)
        {
            activeButton.style.backgroundColor = new Color(0.298f, 0.686f, 0.314f, 1f); // Success green
        }
    }
    
    void ResetButtonStyle(Button button)
    {
        button.style.backgroundColor = new Color(0.396f, 0.4f, 0.945f, 1f); // Primary color
    }
    
    void ResetCamera()
    {
        if (cameraManager != null)
        {
            Debug.Log("重置相机位置");
            uiManager.UpdateStatusBar("相机已重置");
        }
    }
    
    void Update()
    {
        if (cameraPanel != null && cameraPanel.style.display == DisplayStyle.Flex)
        {
            UpdateCameraInfo();
        }
    }
    
    void UpdateCameraInfo()
    {
        if (cameraInfoLabel != null && Camera.main != null)
        {
            var cam = Camera.main;
            var pos = cam.transform.position;
            var rot = cam.transform.eulerAngles;
            
            cameraInfoLabel.text = $"位置: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})\n" +
                                  $"旋转: ({rot.x:F1}°, {rot.y:F1}°, {rot.z:F1}°)\n" +
                                  $"视野: {cam.fieldOfView:F1}°";
        }
    }
    
    public void Show()
    {
        if (cameraPanel != null)
        {
            cameraPanel.style.display = DisplayStyle.Flex;
        }
    }
    
    public void Hide()
    {
        if (cameraPanel != null)
        {
            cameraPanel.style.display = DisplayStyle.None;
        }
    }
} 