using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq; // Added for .TakeLast()

/// <summary>
/// AI助手管理器 - 提供智能聊天和系统帮助功能
/// </summary>
public class AIAssistantManager : MonoBehaviour
{
    [Header("AI助手设置")]
    public bool enableAIAssistant = true;
    public string assistantName = "电力线助手";
    public Color assistantColor = new Color(0.2f, 0.8f, 0.4f, 1f); // 绿色主题
    public Color secondaryColor = new Color(0.3f, 0.3f, 0.3f, 0.9f); // 次要颜色
    public Color backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f); // 背景色
    public Color userMessageColor = new Color(0.2f, 0.6f, 1f, 0.9f); // 用户消息颜色
    public Color aiMessageColor = new Color(0.25f, 0.25f, 0.35f, 0.9f); // AI消息颜色
    
    [Header("配置文件")]
    [Tooltip("AI助手配置文件，包含知识库和设置")]
    public AIAssistantConfig config;
    
    [Header("聊天设置")]
    public int maxChatHistory = 50; // 最大聊天记录数
    public float typingSpeed = 0.05f; // 打字速度
    public bool enableMessageAnimation = true; // 启用消息动画
    public bool enableTypingIndicator = true; // 启用打字指示器
    public bool enableScaleAnimation = false; // 启用缩放动画（可能在某些Unity版本中不兼容）
    
    [Header("UI组件")]
    private UIDocument uiDocument;
    private VisualElement rootElement;
    private VisualElement chatPanel;
    private VisualElement chatContainer;
    private TextField inputField;
    private Button sendButton;
    private Button toggleButton;
    private VisualElement typingIndicator; // 打字指示器
    
    // 聊天记录
    private List<ChatMessage> chatHistory = new List<ChatMessage>();
    private bool isTyping = false;
    
    // 对话系统增强
    private List<ConversationContext> conversationContexts = new List<ConversationContext>();
    private int currentConversationId = 0;
    private const int MAX_CONTEXT_LENGTH = 2000; // 最大上下文长度
    
    // 消息类型枚举
    public enum MessageType
    {
        Text,           // 普通文本
        System,         // 系统消息
        Error,          // 错误消息
        Success,        // 成功消息
        Warning,        // 警告消息
        Code,           // 代码块
        Image,          // 图片
        File            // 文件
    }
    
    // 对话上下文结构
    [System.Serializable]
    public class ConversationContext
    {
        public int id;
        public string title;
        public DateTime createdAt;
        public List<ChatMessage> messages;
        public string summary;
        public bool isActive;
    }
    
    // 聊天消息结构增强
    [System.Serializable]
    public class ChatMessage
    {
        public string content;
        public bool isUser;
        public DateTime timestamp;
        public MessageType messageType;
        public string senderName;
        public string avatarUrl;
        public bool isEdited;
        public DateTime? editedAt;
        public List<string> attachments;
        public Dictionary<string, object> metadata;
        
        public ChatMessage()
        {
            timestamp = DateTime.Now;
            messageType = MessageType.Text;
            attachments = new List<string>();
            metadata = new Dictionary<string, object>();
        }
    }
    
    // 单例模式
    public static AIAssistantManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (enableAIAssistant)
        {
            // 延迟一帧初始化，确保场景完全加载
            StartCoroutine(DelayedStart());
        }
    }
    
    /// <summary>
    /// 延迟启动
    /// </summary>
    private IEnumerator DelayedStart()
    {
        yield return null; // 等待一帧
        
        Debug.Log("AI助手开始初始化...");
        InitializeAIAssistant();
    }
    
    /// <summary>
    /// 初始化AI助手
    /// </summary>
    public void InitializeAIAssistant()
    {
        try
        {
            Debug.Log("开始初始化AI助手...");
            
            // 如果没有配置文件，尝试加载默认配置
            if (config == null)
            {
                config = Resources.Load<AIAssistantConfig>("AIAssistantConfig");
                if (config == null)
                {
                    Debug.LogWarning("未找到AI助手配置文件，将使用默认设置");
                    config = ScriptableObject.CreateInstance<AIAssistantConfig>();
                }
            }
            
            // 应用配置文件设置
            ApplyConfigSettings();
            
            CreateUIDocument();
            SetupChatUI();
            AddWelcomeMessage();
            
            // 确保切换按钮显示
            if (toggleButton != null)
            {
                toggleButton.style.display = DisplayStyle.Flex;
                Debug.Log("AI助手切换按钮已设置为显示状态");
            }
            
            Debug.Log("AI助手初始化完成");
            
            // 延迟一帧后再次检查状态
            StartCoroutine(DelayedStatusCheck());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AI助手初始化失败: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// 延迟状态检查
    /// </summary>
    private IEnumerator DelayedStatusCheck()
    {
        yield return null; // 等待一帧
        
        if (toggleButton != null && toggleButton.style.display == DisplayStyle.None)
        {
            Debug.LogWarning("AI助手按钮仍未显示，强制显示");
            toggleButton.style.display = DisplayStyle.Flex;
            toggleButton.BringToFront(); // 确保按钮在最上层
        }
        
        Debug.Log("AI助手状态检查完成");
    }
    
    /// <summary>
    /// 应用配置文件设置
    /// </summary>
    private void ApplyConfigSettings()
    {
        if (config != null)
        {
            assistantName = config.assistantName;
            assistantColor = config.primaryColor;
            secondaryColor = config.secondaryColor;
            maxChatHistory = config.maxChatHistory;
            typingSpeed = config.typingSpeed;
            enableMessageAnimation = config.enableTypingEffect;
            enableTypingIndicator = config.enableTypingEffect;
            
            // 验证颜色设置
            ValidateColorSettings();
        }
        else
        {
            // 使用默认颜色设置
            SetDefaultColors();
        }
    }
    
    /// <summary>
    /// 验证颜色设置
    /// </summary>
    private void ValidateColorSettings()
    {
        // 确保颜色值在有效范围内
        assistantColor = new Color(
            Mathf.Clamp01(assistantColor.r),
            Mathf.Clamp01(assistantColor.g),
            Mathf.Clamp01(assistantColor.b),
            Mathf.Clamp01(assistantColor.a)
        );
        
        secondaryColor = new Color(
            Mathf.Clamp01(secondaryColor.r),
            Mathf.Clamp01(secondaryColor.g),
            Mathf.Clamp01(secondaryColor.b),
            Mathf.Clamp01(secondaryColor.a)
        );
        
        // 根据主色调自动生成其他颜色
        backgroundColor = new Color(
            assistantColor.r * 0.1f,
            assistantColor.g * 0.1f,
            assistantColor.b * 0.15f,
            0.95f
        );
        
        userMessageColor = new Color(
            Mathf.Min(assistantColor.r + 0.1f, 1f),
            Mathf.Min(assistantColor.g + 0.1f, 1f),
            Mathf.Min(assistantColor.b + 0.2f, 1f),
            0.9f
        );
        
        aiMessageColor = new Color(
            secondaryColor.r * 0.8f,
            secondaryColor.g * 0.8f,
            secondaryColor.b * 0.9f,
            0.9f
        );
    }
    
    /// <summary>
    /// 设置默认颜色
    /// </summary>
    private void SetDefaultColors()
    {
        assistantColor = new Color(0.2f, 0.8f, 0.4f, 1f);
        secondaryColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        userMessageColor = new Color(0.2f, 0.6f, 1f, 0.9f);
        aiMessageColor = new Color(0.25f, 0.25f, 0.35f, 0.9f);
    }
    
    /// <summary>
    /// 创建UI文档
    /// </summary>
    private void CreateUIDocument()
    {
        // 首先尝试找到场景中现有的UIDocument
        var existingUIDocument = FindObjectOfType<UIDocument>();
        if (existingUIDocument != null)
        {
            uiDocument = existingUIDocument;
            Debug.Log("使用场景中现有的UIDocument");
        }
        else
        {
            // 如果没有找到，创建独立的UIDocument
            GameObject uiObject = new GameObject("AI Assistant UI");
            uiObject.transform.SetParent(transform);
            
            uiDocument = uiObject.AddComponent<UIDocument>();
            Debug.Log("创建了新的UIDocument");
        }
        
        // 创建UI结构
        CreateUIStructure();
    }
    
    /// <summary>
    /// 创建UI结构
    /// </summary>
    private void CreateUIStructure()
    {
        // 创建根元素
        rootElement = new VisualElement();
        rootElement.style.width = Length.Percent(100);
        rootElement.style.height = Length.Percent(100);
        rootElement.style.position = Position.Absolute;
        rootElement.style.top = 0;
        rootElement.style.left = 0;
        // 使用BringToFront()来确保在最上层，而不是zIndex
        
        // 创建聊天面板 - 全屏显示优化
        chatPanel = new VisualElement();
        chatPanel.style.width = Length.Percent(90); // 全屏宽度
        chatPanel.style.height = Length.Percent(90); // 全屏高度
        chatPanel.style.position = Position.Absolute;
        chatPanel.style.top = Length.Percent(5);
        chatPanel.style.left = Length.Percent(5);
        chatPanel.style.right = Length.Percent(5);
        chatPanel.style.bottom = Length.Percent(5);
        chatPanel.style.justifyContent = Justify.Center;
        chatPanel.style.alignItems = Align.Center;
        chatPanel.style.backgroundColor = backgroundColor;
        chatPanel.style.borderTopLeftRadius = 20;
        chatPanel.style.borderTopRightRadius = 20;
        chatPanel.style.borderBottomLeftRadius = 20;
        chatPanel.style.borderBottomRightRadius = 20;
        chatPanel.style.paddingTop = 25;
        chatPanel.style.paddingBottom = 25;
        chatPanel.style.paddingLeft = 30;
        chatPanel.style.paddingRight = 30;
        chatPanel.style.display = DisplayStyle.None; // 默认隐藏
        // 使用BringToFront()来确保聊天面板在最上层，而不是zIndex
        
        // 添加边框
        chatPanel.style.borderTopWidth = 1;
        chatPanel.style.borderBottomWidth = 1;
        chatPanel.style.borderLeftWidth = 1;
        chatPanel.style.borderRightWidth = 1;
        chatPanel.style.borderTopColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
        chatPanel.style.borderBottomColor = new Color(0.1f, 0.1f, 0.15f, 0.5f);
        chatPanel.style.borderLeftColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
        chatPanel.style.borderRightColor = new Color(0.1f, 0.1f, 0.15f, 0.5f);
        
        // 创建标题栏
        CreateTitleBar();
        
        // 创建聊天容器
        CreateChatContainer();
        
        // 创建输入区域
        CreateInputArea();
        
        // 创建切换按钮
        CreateToggleButton();
        
        // 添加到根元素
        rootElement.Add(chatPanel);
        rootElement.Add(toggleButton);
        
        // 设置UIDocument
        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            uiDocument.rootVisualElement.Add(rootElement);
            
            // 确保UI元素在最上层
            rootElement.BringToFront();
            chatPanel.BringToFront();
            
            // 强制刷新UI
            rootElement.MarkDirtyRepaint();
            chatPanel.MarkDirtyRepaint();
            
            Debug.Log("UI结构已创建并添加到UIDocument");
        }
        else
        {
            Debug.LogError("UIDocument或rootVisualElement为空，无法添加UI结构");
        }
    }
    
    /// <summary>
    /// 创建标题栏
    /// </summary>
    private void CreateTitleBar()
    {
        var titleBar = new VisualElement();
        titleBar.style.flexDirection = FlexDirection.Row;
        titleBar.style.justifyContent = Justify.SpaceBetween;
        titleBar.style.alignItems = Align.Center;
        titleBar.style.marginBottom = 15;
        titleBar.style.paddingTop = 8;
        titleBar.style.paddingBottom = 8;
        titleBar.style.paddingLeft = 12;
        titleBar.style.paddingRight = 12;
        
        // 添加渐变背景
        titleBar.style.backgroundImage = new StyleBackground(
            new Texture2D(1, 1) // 这里可以替换为实际的渐变纹理
        );
        titleBar.style.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);
        titleBar.style.borderTopLeftRadius = 10;
        titleBar.style.borderTopRightRadius = 10;
        titleBar.style.borderBottomLeftRadius = 10;
        titleBar.style.borderBottomRightRadius = 10;
        
        // 创建标题标签
        var titleLabel = new Label(assistantName);
        titleLabel.style.color = assistantColor;
        titleLabel.style.fontSize = 20;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginLeft = 5;
        
        // 创建关闭按钮 - 改进样式
        var closeButton = new Button(() => ToggleChatPanel(false));
        closeButton.text = "×";
        closeButton.style.width = 32;
        closeButton.style.height = 32;
        closeButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        closeButton.style.color = Color.white;
        closeButton.style.borderTopWidth = 0;
        closeButton.style.borderBottomWidth = 0;
        closeButton.style.borderLeftWidth = 0;
        closeButton.style.borderRightWidth = 0;
        closeButton.style.fontSize = 22;
        closeButton.style.borderTopLeftRadius = 16;
        closeButton.style.borderTopRightRadius = 16;
        closeButton.style.borderBottomLeftRadius = 16;
        closeButton.style.borderBottomRightRadius = 16;
        
        // 添加悬停效果
        closeButton.RegisterCallback<MouseEnterEvent>(evt => {
            closeButton.style.backgroundColor = new Color(1f, 0.3f, 0.3f, 0.9f);
        });
        closeButton.RegisterCallback<MouseLeaveEvent>(evt => {
            closeButton.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        });
        
        titleBar.Add(titleLabel);
        titleBar.Add(closeButton);
        
        chatPanel.Add(titleBar);
    }
    
    /// <summary>
    /// 创建聊天容器
    /// </summary>
    private void CreateChatContainer()
    {
        chatContainer = new ScrollView();
        chatContainer.style.flexGrow = 1;
        chatContainer.style.marginBottom = 20;
        chatContainer.style.backgroundColor = new Color(0.12f, 0.12f, 0.18f, 0.6f);
        chatContainer.style.borderTopLeftRadius = 15;
        chatContainer.style.borderTopRightRadius = 15;
        chatContainer.style.borderBottomLeftRadius = 15;
        chatContainer.style.borderBottomRightRadius = 15;
        chatContainer.style.paddingTop = 20;
        chatContainer.style.paddingBottom = 20;
        chatContainer.style.paddingLeft = 20;
        chatContainer.style.paddingRight = 20;
        
        // 添加边框
        chatContainer.style.borderTopWidth = 1;
        chatContainer.style.borderBottomWidth = 1;
        chatContainer.style.borderLeftWidth = 1;
        chatContainer.style.borderRightWidth = 1;
        chatContainer.style.borderTopColor = new Color(0.25f, 0.25f, 0.35f, 0.3f);
        chatContainer.style.borderBottomColor = new Color(0.1f, 0.1f, 0.15f, 0.3f);
        chatContainer.style.borderLeftColor = new Color(0.25f, 0.25f, 0.35f, 0.3f);
        chatContainer.style.borderRightColor = new Color(0.1f, 0.1f, 0.15f, 0.3f);
        
        // 自定义滚动条样式 - 使用兼容的方式
        // chatContainer.verticalScroller.style.backgroundColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
        // chatContainer.verticalScroller.style.width = 8;
        // chatContainer.verticalScroller.style.borderTopLeftRadius = 4;
        // chatContainer.verticalScroller.style.borderTopRightRadius = 4;
        // chatContainer.verticalScroller.style.borderBottomLeftRadius = 4;
        // chatContainer.verticalScroller.style.borderBottomRightRadius = 4;
        
        // 滚动条滑块样式
        // chatContainer.verticalScroller.slider.style.backgroundColor = new Color(0.4f, 0.4f, 0.6f, 0.8f);
        // chatContainer.verticalScroller.slider.style.borderTopLeftRadius = 4;
        // chatContainer.verticalScroller.slider.style.borderTopRightRadius = 4;
        // chatContainer.verticalScroller.slider.style.borderBottomLeftRadius = 4;
        // chatContainer.verticalScroller.slider.style.borderBottomRightRadius = 4;
        
        chatPanel.Add(chatContainer);
    }
    
    /// <summary>
    /// 创建输入区域
    /// </summary>
    private void CreateInputArea()
    {
        var inputArea = new VisualElement();
        inputArea.style.flexDirection = FlexDirection.Row;
        inputArea.style.alignItems = Align.Center;
        inputArea.style.marginTop = 20;
        
        // 创建输入框 - 改进样式
        inputField = new TextField();
        inputField.style.flexGrow = 1;
        inputField.style.backgroundColor = new Color(0.18f, 0.18f, 0.25f, 0.9f);
        inputField.style.color = Color.white;
        inputField.style.borderTopLeftRadius = 8;
        inputField.style.borderTopRightRadius = 8;
        inputField.style.borderBottomLeftRadius = 8;
        inputField.style.borderBottomRightRadius = 8;
        inputField.style.paddingLeft = 16;
        inputField.style.paddingRight = 16;
        inputField.style.paddingTop = 12;
        inputField.style.paddingBottom = 12;
        inputField.style.marginRight = 15;
        inputField.style.fontSize = 16;
        
        // 添加边框
        inputField.style.borderTopWidth = 1;
        inputField.style.borderBottomWidth = 1;
        inputField.style.borderLeftWidth = 1;
        inputField.style.borderRightWidth = 1;
        inputField.style.borderTopColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
        inputField.style.borderBottomColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
        inputField.style.borderLeftColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
        inputField.style.borderRightColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
        
        // 添加焦点效果
        inputField.RegisterCallback<FocusInEvent>(evt => {
            inputField.style.borderTopColor = assistantColor;
            inputField.style.borderBottomColor = assistantColor;
            inputField.style.borderLeftColor = assistantColor;
            inputField.style.borderRightColor = assistantColor;
        });
        inputField.RegisterCallback<FocusOutEvent>(evt => {
            inputField.style.borderTopColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
            inputField.style.borderBottomColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
            inputField.style.borderLeftColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
            inputField.style.borderRightColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
        });
        
        // 添加回车键发送功能
        inputField.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                SendMessage();
            }
        });
        
        // 创建发送按钮 - 改进样式
        sendButton = new Button(SendMessage);
        sendButton.text = "发送";
        sendButton.style.width = 90;
        sendButton.style.height = 48;
        
        // 现代化发送按钮样式
        sendButton.style.backgroundImage = new StyleBackground(CreateSendButtonGradient());
        sendButton.style.backgroundColor = assistantColor; // 备用颜色
        
        sendButton.style.color = Color.white;
        sendButton.style.borderTopLeftRadius = 21;
        sendButton.style.borderTopRightRadius = 21;
        sendButton.style.borderBottomLeftRadius = 21;
        sendButton.style.borderBottomRightRadius = 21;
        sendButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        sendButton.style.fontSize = 16;
        sendButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        
        // 内边距
        sendButton.style.paddingTop = 12;
        sendButton.style.paddingBottom = 12;
        sendButton.style.paddingLeft = 18;
        sendButton.style.paddingRight = 18;
        
        // 添加悬停效果
        sendButton.RegisterCallback<MouseEnterEvent>(evt => {
            sendButton.style.backgroundImage = new StyleBackground(CreateSendButtonHoverGradient());
            sendButton.style.scale = new Scale(new Vector3(1.05f, 1.05f, 1f));
        });
        sendButton.RegisterCallback<MouseLeaveEvent>(evt => {
            sendButton.style.backgroundImage = new StyleBackground(CreateSendButtonGradient());
            sendButton.style.scale = new Scale(new Vector3(1f, 1f, 1f));
        });
        
        // 添加点击效果
        sendButton.RegisterCallback<MouseDownEvent>(evt => {
            sendButton.style.scale = new Scale(new Vector3(0.95f, 0.95f, 1f));
        });
        sendButton.RegisterCallback<MouseUpEvent>(evt => {
            sendButton.style.scale = new Scale(new Vector3(1.05f, 1.05f, 1f));
        });
        
        inputArea.Add(inputField);
        inputArea.Add(sendButton);
        
        chatPanel.Add(inputArea);
    }
    
    /// <summary>
    /// 创建切换按钮
    /// </summary>
    private void CreateToggleButton()
    {
        toggleButton = new Button(() => ToggleChatPanel());
        toggleButton.text = "AI助手";
        toggleButton.style.width = 100;
        toggleButton.style.height = 48;
        toggleButton.style.position = Position.Absolute;
        toggleButton.style.bottom = 20;
        toggleButton.style.right = 460;
        toggleButton.style.display = DisplayStyle.Flex; // 确保按钮显示
        
        // 现代化按钮样式 - 渐变背景
        toggleButton.style.backgroundImage = new StyleBackground(CreateGradientTexture());
        toggleButton.style.backgroundColor = assistantColor; // 备用颜色
        
        // 文字样式
        toggleButton.style.color = Color.white;
        toggleButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        toggleButton.style.fontSize = 16;
        toggleButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        
        // 圆角和边框
        toggleButton.style.borderTopLeftRadius = 24;
        toggleButton.style.borderTopRightRadius = 24;
        toggleButton.style.borderBottomLeftRadius = 24;
        toggleButton.style.borderBottomRightRadius = 24;
        toggleButton.style.borderTopWidth = 0;
        toggleButton.style.borderBottomWidth = 0;
        toggleButton.style.borderLeftWidth = 0;
        toggleButton.style.borderRightWidth = 0;
        
        // 内边距
        toggleButton.style.paddingTop = 12;
        toggleButton.style.paddingBottom = 12;
        toggleButton.style.paddingLeft = 16;
        toggleButton.style.paddingRight = 16;
        
        // 添加悬停效果
        toggleButton.RegisterCallback<MouseEnterEvent>(evt => {
            // 悬停时改变渐变
            toggleButton.style.backgroundImage = new StyleBackground(CreateHoverGradientTexture());
            toggleButton.style.scale = new Scale(new Vector3(1.05f, 1.05f, 1f));
        });
        toggleButton.RegisterCallback<MouseLeaveEvent>(evt => {
            // 恢复原始渐变
            toggleButton.style.backgroundImage = new StyleBackground(CreateGradientTexture());
            toggleButton.style.scale = new Scale(new Vector3(1f, 1f, 1f));
        });
        
        // 添加点击效果
        toggleButton.RegisterCallback<MouseDownEvent>(evt => {
            toggleButton.style.scale = new Scale(new Vector3(0.95f, 0.95f, 1f));
        });
        toggleButton.RegisterCallback<MouseUpEvent>(evt => {
            toggleButton.style.scale = new Scale(new Vector3(1.05f, 1.05f, 1f));
        });
        
        // 添加调试信息
        Debug.Log("AI助手按钮已创建，位置：右下角");
    }
    
    /// <summary>
    /// 创建渐变纹理
    /// </summary>
    private Texture2D CreateGradientTexture()
    {
        int width = 256;
        int height = 64;
        Texture2D texture = new Texture2D(width, height);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float t = (float)y / height;
                Color color = Color.Lerp(assistantColor, 
                    new Color(assistantColor.r * 0.8f, assistantColor.g * 0.8f, assistantColor.b * 0.8f, 1f), t);
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// 创建悬停渐变纹理
    /// </summary>
    private Texture2D CreateHoverGradientTexture()
    {
        int width = 256;
        int height = 64;
        Texture2D texture = new Texture2D(width, height);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float t = (float)y / height;
                Color color = Color.Lerp(
                    new Color(assistantColor.r * 1.2f, assistantColor.g * 1.2f, assistantColor.b * 1.2f, 1f), 
                    new Color(assistantColor.r * 0.9f, assistantColor.g * 0.9f, assistantColor.b * 0.9f, 1f), t);
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// 创建发送按钮渐变纹理
    /// </summary>
    private Texture2D CreateSendButtonGradient()
    {
        int width = 256;
        int height = 64;
        Texture2D texture = new Texture2D(width, height);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float t = (float)y / height;
                Color color = Color.Lerp(
                    new Color(assistantColor.r * 1.1f, assistantColor.g * 1.1f, assistantColor.b * 1.1f, 1f),
                    new Color(assistantColor.r * 0.9f, assistantColor.g * 0.9f, assistantColor.b * 0.9f, 1f), t);
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// 创建发送按钮悬停渐变纹理
    /// </summary>
    private Texture2D CreateSendButtonHoverGradient()
    {
        int width = 256;
        int height = 64;
        Texture2D texture = new Texture2D(width, height);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float t = (float)y / height;
                Color color = Color.Lerp(
                    new Color(assistantColor.r * 1.2f, assistantColor.g * 1.2f, assistantColor.b * 1.2f, 1f),
                    new Color(assistantColor.r * 0.9f, assistantColor.g * 0.9f, assistantColor.b * 0.9f, 1f), t);
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// 创建用户头像渐变纹理
    /// </summary>
    private Texture2D CreateUserAvatarGradient()
    {
        int width = 64;
        int height = 64;
        Texture2D texture = new Texture2D(width, height);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float t = (float)y / height;
                Color color = Color.Lerp(
                    new Color(0.2f, 0.6f, 1f, 1f), // 蓝色
                    new Color(0.1f, 0.4f, 0.8f, 1f), t);
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// 创建AI头像渐变纹理
    /// </summary>
    private Texture2D CreateAIAvatarGradient()
    {
        int width = 64;
        int height = 64;
        Texture2D texture = new Texture2D(width, height);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float t = (float)y / height;
                Color color = Color.Lerp(
                    assistantColor,
                    new Color(assistantColor.r * 0.7f, assistantColor.g * 0.7f, assistantColor.b * 0.7f, 1f), t);
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// 创建用户消息气泡渐变纹理
    /// </summary>
    private Texture2D CreateUserBubbleGradient()
    {
        int width = 256;
        int height = 64;
        Texture2D texture = new Texture2D(width, height);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float t = (float)y / height;
                Color color = Color.Lerp(
                    new Color(0.2f, 0.6f, 1f, 0.9f), // 蓝色
                    new Color(0.1f, 0.4f, 0.8f, 0.9f), t);
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// 创建AI消息气泡渐变纹理
    /// </summary>
    private Texture2D CreateAIBubbleGradient()
    {
        int width = 256;
        int height = 64;
        Texture2D texture = new Texture2D(width, height);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float t = (float)y / height;
                Color color = Color.Lerp(
                    new Color(0.25f, 0.25f, 0.35f, 0.9f),
                    new Color(0.15f, 0.15f, 0.25f, 0.9f), t);
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// 设置聊天UI（占位符方法）
    /// </summary>
    private void SetupChatUI()
    {
        // 这个方法将在后续实现中完善
    }
    
    /// <summary>
    /// 切换聊天面板显示状态
    /// </summary>
    public void ToggleChatPanel(bool? forceState = null)
    {
        if (chatPanel == null)
        {
            Debug.LogError("聊天面板未初始化！");
            return;
        }
        
        bool newState = forceState ?? (chatPanel.style.display == DisplayStyle.None);
        
        Debug.Log($"切换聊天面板显示状态: {newState}");
        Debug.Log($"当前显示状态: {chatPanel.style.display}");
        
        chatPanel.style.display = newState ? DisplayStyle.Flex : DisplayStyle.None;
        
        // 确保面板在最上层
        if (newState && rootElement != null)
        {
            rootElement.BringToFront();
            chatPanel.BringToFront();
            
            // 强制刷新UI
            rootElement.MarkDirtyRepaint();
            chatPanel.MarkDirtyRepaint();
            
            Debug.Log("聊天面板已显示并置顶");
        }
        
        if (newState)
        {
            inputField?.Focus();
        }
    }
    
    /// <summary>
    /// 发送消息
    /// </summary>
    private void SendMessage()
    {
        string message = inputField.value?.Trim();
        if (string.IsNullOrEmpty(message) || isTyping)
            return;
        
        // 添加用户消息
        AddMessage(message, true);
        inputField.value = "";
        
        // 处理AI回复
        ProcessUserMessage(message);
    }
    
    /// <summary>
    /// 处理用户消息
    /// </summary>
    private void ProcessUserMessage(string message)
    {
        // 显示打字指示器
        ShowTypingIndicator();
        
        // 延迟处理AI回复，模拟思考时间
        StartCoroutine(ProcessAIResponseWithDelay(message));
    }
    
    /// <summary>
    /// 延迟处理AI回复的协程
    /// </summary>
    private IEnumerator ProcessAIResponseWithDelay(string userMessage)
    {
        // 模拟AI思考时间
        float thinkingTime = Mathf.Clamp(userMessage.Length * 0.05f, 0.5f, 2f);
        yield return new WaitForSeconds(thinkingTime);
        
        // 隐藏打字指示器
        HideTypingIndicator();
        
        // 生成AI回复
        string response = GenerateAIResponse(userMessage);
        
        // 添加AI回复消息
        AddMessage(response, false);
    }
    
    /// <summary>
    /// 生成AI回复（使用配置文件知识库）
    /// </summary>
    private string GenerateAIResponse(string userMessage)
    {
        if (config == null)
        {
            return "抱歉，AI助手配置未加载，无法提供智能回复。";
        }
        
        // 首先尝试快速回复
        string quickResponse = config.GetQuickResponse(userMessage);
        if (!string.IsNullOrEmpty(quickResponse))
        {
            return quickResponse;
        }
        
        // 然后查找知识库
        var knowledgeEntry = config.FindKnowledgeEntry(userMessage);
        if (knowledgeEntry != null)
        {
            return knowledgeEntry.response;
        }
        
        // 如果都没有找到，提供智能回复
        return GenerateIntelligentResponse(userMessage);
    }
    
    /// <summary>
    /// 生成智能回复
    /// </summary>
    private string GenerateIntelligentResponse(string userMessage)
    {
        string lowerMessage = userMessage.ToLower();
        
        // 基于关键词和上下文的智能回复
        if (lowerMessage.Contains("如何") || lowerMessage.Contains("怎么"))
        {
            return GenerateHowToResponse(userMessage);
        }
        else if (lowerMessage.Contains("问题") || lowerMessage.Contains("错误") || lowerMessage.Contains("故障"))
        {
            return GenerateTroubleshootingResponse(userMessage);
        }
        else if (lowerMessage.Contains("功能") || lowerMessage.Contains("特性") || lowerMessage.Contains("能做什么"))
        {
            return GenerateFeatureResponse(userMessage);
        }
        else if (lowerMessage.Contains("谢谢") || lowerMessage.Contains("感谢"))
        {
            return "不客气！很高兴能帮助到您。如果还有其他问题，随时可以询问我。";
        }
        else if (lowerMessage.Contains("再见") || lowerMessage.Contains("拜拜"))
        {
            return "再见！祝您使用愉快。如果遇到问题，随时可以回来找我帮忙。";
        }
        else
        {
            return GenerateContextualResponse(userMessage);
        }
    }
    
    /// <summary>
    /// 生成操作指导回复
    /// </summary>
    private string GenerateHowToResponse(string userMessage)
    {
        string lowerMessage = userMessage.ToLower();
        
        if (lowerMessage.Contains("相机") || lowerMessage.Contains("视角"))
        {
            return "相机控制操作指南：\n\n" +
                   "🎮 基础控制：\n" +
                   "• WASD键：前后左右移动\n" +
                   "• 鼠标滚轮：缩放场景\n" +
                   "• 右键拖拽：旋转视角\n" +
                   "• 中键拖拽：平移视角\n\n" +
                   "🔧 高级功能：\n" +
                   "• 双击物体：聚焦到该物体\n" +
                   "• Shift+滚轮：快速缩放\n" +
                   "• Ctrl+滚轮：精细缩放\n\n" +
                   "💡 提示：可以在设置中调整相机灵敏度";
        }
        else if (lowerMessage.Contains("测量") || lowerMessage.Contains("距离"))
        {
            return "测量功能使用说明：\n\n" +
                   "📏 距离测量：\n" +
                   "• 点击起点：选择测量起点\n" +
                   "• 点击终点：选择测量终点\n" +
                   "• 自动显示：距离、角度、高度差\n\n" +
                   "📐 面积测量：\n" +
                   "• 点击多个点：形成测量区域\n" +
                   "• 双击结束：显示总面积\n\n" +
                   "🔄 清除测量：按ESC键或右键菜单";
        }
        else if (lowerMessage.Contains("标记") || lowerMessage.Contains("标注"))
        {
            return "标记和标注功能：\n\n" +
                   "📍 位置标记：\n" +
                   "• 右键点击：添加标记点\n" +
                   "• 输入描述：记录重要信息\n" +
                   "• 颜色分类：不同类型使用不同颜色\n\n" +
                   "📝 文本标注：\n" +
                   "• 双击标记：编辑文本内容\n" +
                   "• 字体设置：调整大小和样式\n" +
                   "• 导出功能：保存所有标记信息";
        }
        else
        {
            return "关于操作指导，我可以为您提供：\n\n" +
                   "🎯 具体功能指导：\n" +
                   "• 相机控制和视角操作\n" +
                   "• 测量和计算功能\n" +
                   "• 标记和标注工具\n" +
                   "• 数据导入导出\n\n" +
                   "💡 请告诉我您想了解哪个具体功能，我会提供详细的操作步骤。";
        }
    }
    
    /// <summary>
    /// 生成故障排除回复
    /// </summary>
    private string GenerateTroubleshootingResponse(string userMessage)
    {
        string lowerMessage = userMessage.ToLower();
        
        if (lowerMessage.Contains("性能") || lowerMessage.Contains("卡顿") || lowerMessage.Contains("慢"))
        {
            return "性能优化建议：\n\n" +
                   "⚡ 立即优化：\n" +
                   "• 降低图形质量：设置→图形→质量\n" +
                   "• 启用LOD：自动调整细节层次\n" +
                   "• 关闭阴影：减少渲染负担\n\n" +
                   "🔧 系统设置：\n" +
                   "• 更新显卡驱动\n" +
                   "• 关闭后台程序\n" +
                   "• 增加虚拟内存\n\n" +
                   "📊 监控工具：使用性能监视器查看瓶颈";
        }
        else if (lowerMessage.Contains("显示") || lowerMessage.Contains("画面") || lowerMessage.Contains("渲染"))
        {
            return "显示问题解决方案：\n\n" +
                   "🖥️ 画面异常：\n" +
                   "• 检查显卡驱动：确保是最新版本\n" +
                   "• 验证文件完整性：Steam→属性→验证\n" +
                   "• 重置图形设置：删除配置文件\n\n" +
                   "🎨 材质问题：\n" +
                   "• 重新导入材质：右键→重新导入\n" +
                   "• 检查贴图路径：确保文件存在\n" +
                   "• 清除缓存：清除Library文件夹\n\n" +
                   "🔍 如果问题持续，请提供错误截图";
        }
        else if (lowerMessage.Contains("数据") || lowerMessage.Contains("导入") || lowerMessage.Contains("文件"))
        {
            return "数据处理问题解决：\n\n" +
                   "📁 文件导入：\n" +
                   "• 检查文件格式：支持CSV、JSON、XML\n" +
                   "• 验证数据完整性：确保必要字段存在\n" +
                   "• 检查编码格式：推荐UTF-8\n\n" +
                   "🔄 数据同步：\n" +
                   "• 刷新数据源：右键→刷新\n" +
                   "• 检查网络连接：确保数据源可访问\n" +
                   "• 清除缓存：删除临时文件\n\n" +
                   "📊 数据验证：使用内置验证工具检查数据";
        }
        else
        {
            return "常见问题及解决方案：\n\n" +
                   "🔧 系统问题：\n" +
                   "• 性能问题：启用LOD优化、降低图形质量\n" +
                   "• 显示异常：更新驱动、验证文件完整性\n" +
                   "• 数据问题：检查格式、验证完整性\n\n" +
                   "💡 请描述具体遇到的问题，我会提供针对性的解决方案。";
        }
    }
    
    /// <summary>
    /// 生成功能特性回复
    /// </summary>
    private string GenerateFeatureResponse(string userMessage)
    {
        return "系统主要功能特性：\n\n" +
               "🌐 电力线可视化：\n" +
               "• 3D电塔模型和连接线\n" +
               "• 实时数据更新和状态显示\n" +
               "• 多层级信息展示\n\n" +
               "📷 相机控制：\n" +
               "• 自由视角移动和旋转\n" +
               "• 自动聚焦和路径动画\n" +
               "• 多相机预设和切换\n\n" +
               "⚠️ 危险监测：\n" +
               "• 实时预警和风险评估\n" +
               "• 历史数据分析和趋势\n" +
               "• 自动报告生成\n\n" +
               "🚁 无人机管理：\n" +
               "• 巡检路径规划和优化\n" +
               "• 任务分配和状态监控\n" +
               "• 数据采集和分析\n\n" +
               "🗺️ 地形适配：\n" +
               "• 高精度地形数据\n" +
               "• 自动路径优化\n" +
               "• 环境影响评估";
    }
    
    /// <summary>
    /// 生成上下文相关回复
    /// </summary>
    private string GenerateContextualResponse(string userMessage)
    {
        // 分析当前对话上下文
        if (conversationContexts.Count > 0 && currentConversationId < conversationContexts.Count)
        {
            var currentContext = conversationContexts[currentConversationId];
            if (currentContext.messages.Count > 1)
            {
                // 基于对话历史生成更相关的回复
                var recentMessages = currentContext.messages.TakeLast(3).ToList();
                if (recentMessages.Any(m => m.content.Contains("相机") || m.content.Contains("视角")))
                {
                    return "看起来您对相机控制很感兴趣。我可以为您提供更详细的相机操作指导，或者您有其他具体问题吗？";
                }
                else if (recentMessages.Any(m => m.content.Contains("测量") || m.content.Contains("距离")))
                {
                    return "测量功能确实很实用。您想了解其他测量工具，还是有其他功能需要帮助？";
                }
            }
        }
        
        return "我理解您的问题，但可能需要更具体的信息。您可以：\n\n" +
               "🎯 询问特定功能：\n" +
               "• 相机控制和视角操作\n" +
               "• 测量和计算工具\n" +
               "• 数据导入导出\n" +
               "• 系统设置和优化\n\n" +
               "💡 或者告诉我您遇到的具体问题，我会提供针对性的帮助。";
    }
    
    /// <summary>
    /// 添加消息到聊天记录
    /// </summary>
    public void AddMessage(string content, bool isUser, MessageType messageType = MessageType.Text)
    {
        var message = new ChatMessage
        {
            content = content,
            isUser = isUser,
            messageType = messageType,
            senderName = isUser ? "您" : assistantName
        };
        
        chatHistory.Add(message);
        
        // 限制聊天记录数量
        if (chatHistory.Count > maxChatHistory)
        {
            chatHistory.RemoveAt(0);
        }
        
        // 显示消息
        DisplayMessage(message);
        
        // 更新对话上下文
        UpdateConversationContext(message);
    }
    
    /// <summary>
    /// 更新对话上下文
    /// </summary>
    private void UpdateConversationContext(ChatMessage message)
    {
        if (conversationContexts.Count == 0 || !conversationContexts[currentConversationId].isActive)
        {
            CreateNewConversation();
        }
        
        var currentContext = conversationContexts[currentConversationId];
        currentContext.messages.Add(message);
        
        // 如果上下文过长，进行摘要
        if (GetContextLength(currentContext) > MAX_CONTEXT_LENGTH)
        {
            SummarizeConversation(currentContext);
        }
    }
    
    /// <summary>
    /// 创建新对话
    /// </summary>
    private void CreateNewConversation()
    {
        var newContext = new ConversationContext
        {
            id = currentConversationId++,
            title = $"对话 {DateTime.Now:MM-dd HH:mm}",
            createdAt = DateTime.Now,
            messages = new List<ChatMessage>(),
            isActive = true
        };
        
        conversationContexts.Add(newContext);
        currentConversationId = conversationContexts.Count - 1;
    }
    
    /// <summary>
    /// 获取上下文长度
    /// </summary>
    private int GetContextLength(ConversationContext context)
    {
        int length = 0;
        foreach (var msg in context.messages)
        {
            length += msg.content.Length;
        }
        return length;
    }
    
    /// <summary>
    /// 对话摘要
    /// </summary>
    private void SummarizeConversation(ConversationContext context)
    {
        // 简单的摘要逻辑：保留最近的几条消息
        if (context.messages.Count > 10)
        {
            var recentMessages = context.messages.TakeLast(5).ToList();
            context.messages = recentMessages;
            context.summary = $"对话已摘要，保留了最近的{recentMessages.Count}条消息";
        }
    }
    
    /// <summary>
    /// 获取对话历史
    /// </summary>
    public List<ConversationContext> GetConversationHistory()
    {
        return conversationContexts;
    }
    
    /// <summary>
    /// 切换到指定对话
    /// </summary>
    public void SwitchToConversation(int conversationId)
    {
        if (conversationId >= 0 && conversationId < conversationContexts.Count)
        {
            // 停用当前对话
            if (currentConversationId < conversationContexts.Count)
            {
                conversationContexts[currentConversationId].isActive = false;
            }
            
            // 激活新对话
            currentConversationId = conversationId;
            conversationContexts[currentConversationId].isActive = true;
            
            // 清空当前显示
            chatContainer.Clear();
            
            // 显示对话内容
            foreach (var message in conversationContexts[currentConversationId].messages)
            {
                DisplayMessage(message);
            }
        }
    }
    
    /// <summary>
    /// 显示消息
    /// </summary>
    private void DisplayMessage(ChatMessage message)
    {
        var messageElement = new VisualElement();
        messageElement.style.flexDirection = FlexDirection.Row;
        messageElement.style.marginBottom = 20;
        messageElement.style.alignItems = Align.FlexStart;
        
        if (message.isUser)
        {
            messageElement.style.justifyContent = Justify.FlexEnd;
        }
        
        // 创建头像
        var avatar = CreateAvatar(message);
        messageElement.Add(avatar);
        
        // 创建消息容器
        var messageContainer = new VisualElement();
        messageContainer.style.flexDirection = FlexDirection.Column;
        messageContainer.style.maxWidth = Length.Percent(75);
        messageContainer.style.alignItems = message.isUser ? Align.FlexEnd : Align.FlexStart;
        messageContainer.style.marginLeft = message.isUser ? 0 : 12;
        messageElement.style.marginRight = message.isUser ? 12 : 0;
        
        // 创建消息气泡
        var bubble = CreateMessageBubble(message);
        messageContainer.Add(bubble);
        
        // 添加时间戳
        var timestampLabel = CreateTimestampLabel(message);
        messageContainer.Add(timestampLabel);
        
        messageElement.Add(messageContainer);
        chatContainer.Add(messageElement);
        
        // 添加消息出现动画
        if (enableMessageAnimation)
        {
            messageElement.style.opacity = 0f;
            StartCoroutine(AnimateMessageAppearance(messageElement));
        }
        
        // 滚动到底部
        if (chatContainer is ScrollView scrollView)
        {
            scrollView.scrollOffset = new Vector2(0, scrollView.scrollOffset.y + 100);
        }
    }
    
    /// <summary>
    /// 创建头像
    /// </summary>
    private VisualElement CreateAvatar(ChatMessage message)
    {
        var avatar = new VisualElement();
        avatar.style.width = 36;
        avatar.style.height = 36;
        avatar.style.borderTopLeftRadius = 18;
        avatar.style.borderTopRightRadius = 18;
        avatar.style.borderBottomLeftRadius = 18;
        avatar.style.borderBottomRightRadius = 18;
        avatar.style.marginRight = message.isUser ? 12 : 0;
        avatar.style.marginLeft = message.isUser ? 0 : 12;
        
        if (message.isUser)
        {
            // 用户头像 - 蓝色渐变
            avatar.style.backgroundImage = new StyleBackground(CreateUserAvatarGradient());
        }
        else
        {
            // AI头像 - 使用助手颜色
            avatar.style.backgroundImage = new StyleBackground(CreateAIAvatarGradient());
        }
        
        // 添加头像文字
        var avatarText = new Label(message.isUser ? "您" : "AI");
        avatarText.style.color = Color.white;
        avatarText.style.fontSize = 12;
        avatarText.style.unityFontStyleAndWeight = FontStyle.Bold;
        avatarText.style.unityTextAlign = TextAnchor.MiddleCenter;
        avatarText.style.position = Position.Absolute;
        avatarText.style.left = 0;
        avatarText.style.right = 0;
        avatarText.style.top = 0;
        avatarText.style.bottom = 0;
        
        avatar.Add(avatarText);
        return avatar;
    }
    
    /// <summary>
    /// 创建消息气泡
    /// </summary>
    private VisualElement CreateMessageBubble(ChatMessage message)
    {
        var bubble = new VisualElement();
        bubble.style.paddingTop = 12;
        bubble.style.paddingBottom = 12;
        bubble.style.paddingLeft = 16;
        bubble.style.paddingRight = 16;
        bubble.style.marginBottom = 6;
        
        // 根据消息类型设置样式
        if (message.isUser)
        {
            // 用户消息 - 蓝色渐变
            bubble.style.backgroundImage = new StyleBackground(CreateUserBubbleGradient());
            bubble.style.borderTopLeftRadius = 20;
            bubble.style.borderTopRightRadius = 8;
            bubble.style.borderBottomLeftRadius = 20;
            bubble.style.borderBottomRightRadius = 20;
        }
        else
        {
            // AI消息 - 深色渐变
            bubble.style.backgroundImage = new StyleBackground(CreateAIBubbleGradient());
            bubble.style.borderTopLeftRadius = 8;
            bubble.style.borderTopRightRadius = 20;
            bubble.style.borderBottomLeftRadius = 20;
            bubble.style.borderBottomRightRadius = 20;
        }
        
        // 创建消息文本
        var textLabel = new Label(message.content);
        textLabel.style.color = Color.white;
        textLabel.style.whiteSpace = WhiteSpace.Normal;
        textLabel.style.unityTextAlign = TextAnchor.UpperLeft;
        textLabel.style.fontSize = 14;
        // textLabel.style.lineHeight = 20; // 移除不兼容的行高设置
        
        bubble.Add(textLabel);
        return bubble;
    }
    
    /// <summary>
    /// 创建时间戳标签
    /// </summary>
    private Label CreateTimestampLabel(ChatMessage message)
    {
        var timestampLabel = new Label(message.timestamp.ToString("HH:mm"));
        timestampLabel.style.color = new Color(0.6f, 0.6f, 0.7f, 0.8f);
        timestampLabel.style.fontSize = 11;
        timestampLabel.style.marginTop = 4;
        timestampLabel.style.marginLeft = message.isUser ? 0 : 8;
        timestampLabel.style.marginRight = message.isUser ? 8 : 0;
        
        return timestampLabel;
    }
    
    /// <summary>
    /// 添加欢迎消息
    /// </summary>
    private void AddWelcomeMessage()
    {
        string welcomeMessage = $"欢迎使用{assistantName}！\n\n我可以帮助您：\n• 了解系统功能\n• 提供操作指导\n• 解答技术问题\n• 查询系统状态\n\n有什么可以帮助您的吗？";
        AddMessage(welcomeMessage, false);
        
        // 添加快捷操作按钮
        AddQuickActionButtons();
    }
    
    /// <summary>
    /// 添加快捷操作按钮
    /// </summary>
    private void AddQuickActionButtons()
    {
        var quickActionsContainer = new VisualElement();
        quickActionsContainer.style.flexDirection = FlexDirection.Row;
        // quickActionsContainer.style.justifyContent = Justify.SpaceEvenly; // 移除不兼容的布局
        quickActionsContainer.style.justifyContent = Justify.SpaceAround; // 使用兼容的布局方式
        quickActionsContainer.style.marginTop = 15;
        quickActionsContainer.style.marginBottom = 10;
        
        // 系统帮助按钮
        var helpButton = CreateQuickActionButton("系统帮助", "了解系统基本功能", () => {
            AddMessage("系统主要功能包括：\n• 电力线可视化和管理\n• 多视角相机控制\n• 危险监测和预警\n• 无人机巡检管理\n• 地形适配和优化", false);
        });
        
        // 操作指南按钮
        var guideButton = CreateQuickActionButton("操作指南", "获取操作指导", () => {
            AddMessage("基本操作步骤：\n1. 使用WASD键移动相机\n2. 鼠标滚轮缩放场景\n3. 右键拖拽旋转视角\n4. 点击电塔查看信息\n5. 使用工具栏切换功能", false);
        });
        
        // 故障排除按钮
        var troubleshootButton = CreateQuickActionButton("故障排除", "常见问题解答", () => {
            AddMessage("常见问题及解决方案：\n• 电塔位置不准确：使用位置修正功能\n• 连接线显示异常：检查数据格式\n• 性能问题：启用LOD优化\n• 材质问题：检查材质设置", false);
        });
        
        quickActionsContainer.Add(helpButton);
        quickActionsContainer.Add(guideButton);
        quickActionsContainer.Add(troubleshootButton);
        
        // 将快捷操作按钮添加到聊天容器
        chatContainer.Add(quickActionsContainer);
        
        // 添加对话管理区域
        AddConversationManagement();
    }
    
    /// <summary>
    /// 添加对话管理区域
    /// </summary>
    private void AddConversationManagement()
    {
        var conversationArea = new VisualElement();
        conversationArea.style.flexDirection = FlexDirection.Column;
        conversationArea.style.marginTop = 20;
        conversationArea.style.paddingTop = 15;
        conversationArea.style.paddingBottom = 15;
        conversationArea.style.paddingLeft = 15;
        conversationArea.style.paddingRight = 15;
        conversationArea.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.3f);
        conversationArea.style.borderTopLeftRadius = 12;
        conversationArea.style.borderTopRightRadius = 12;
        conversationArea.style.borderBottomLeftRadius = 12;
        conversationArea.style.borderBottomRightRadius = 12;
        
        // 标题
        var titleLabel = new Label("对话管理");
        titleLabel.style.color = assistantColor;
        titleLabel.style.fontSize = 16;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 12;
        conversationArea.Add(titleLabel);
        
        // 按钮容器
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.justifyContent = Justify.SpaceAround;
        buttonContainer.style.marginTop = 10;
        
        // 新建对话按钮
        var newChatButton = CreateManagementButton("新建对话", () => {
            CreateNewConversation();
            chatContainer.Clear();
            AddWelcomeMessage();
        });
        
        // 清空历史按钮
        var clearButton = CreateManagementButton("清空历史", () => {
            ClearChatHistory();
        });
        
        // 导出对话按钮
        var exportButton = CreateManagementButton("导出对话", () => {
            ExportConversation();
        });
        
        buttonContainer.Add(newChatButton);
        buttonContainer.Add(clearButton);
        buttonContainer.Add(exportButton);
        
        conversationArea.Add(buttonContainer);
        chatContainer.Add(conversationArea);
    }
    
    /// <summary>
    /// 创建管理按钮
    /// </summary>
    private Button CreateManagementButton(string text, Action onClick)
    {
        var button = new Button(onClick);
        button.text = text;
        button.style.width = 100;
        button.style.height = 36;
        
        // 管理按钮样式
        button.style.backgroundImage = new StyleBackground(CreateManagementButtonGradient());
        button.style.backgroundColor = new Color(0.3f, 0.3f, 0.4f, 0.8f);
        
        button.style.color = Color.white;
        button.style.borderTopLeftRadius = 18;
        button.style.borderTopRightRadius = 18;
        button.style.borderBottomLeftRadius = 18;
        button.style.borderBottomRightRadius = 18;
        button.style.fontSize = 12;
        button.style.unityFontStyleAndWeight = FontStyle.Bold;
        button.style.unityTextAlign = TextAnchor.MiddleCenter;
        
        // 内边距
        button.style.paddingTop = 6;
        button.style.paddingBottom = 6;
        button.style.paddingLeft = 10;
        button.style.paddingRight = 10;
        
        // 添加悬停效果
        button.RegisterCallback<MouseEnterEvent>(evt => {
            button.style.backgroundImage = new StyleBackground(CreateManagementButtonHoverGradient());
            button.style.scale = new Scale(new Vector3(1.05f, 1.05f, 1f));
        });
        button.RegisterCallback<MouseLeaveEvent>(evt => {
            button.style.backgroundImage = new StyleBackground(CreateManagementButtonGradient());
            button.style.scale = new Scale(new Vector3(1f, 1f, 1f));
        });
        
        return button;
    }
    
    /// <summary>
    /// 创建管理按钮渐变纹理
    /// </summary>
    private Texture2D CreateManagementButtonGradient()
    {
        int width = 256;
        int height = 64;
        Texture2D texture = new Texture2D(width, height);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float t = (float)y / height;
                Color color = Color.Lerp(
                    new Color(0.3f, 0.3f, 0.4f, 0.8f),
                    new Color(0.2f, 0.2f, 0.3f, 0.8f), t);
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// 创建管理按钮悬停渐变纹理
    /// </summary>
    private Texture2D CreateManagementButtonHoverGradient()
    {
        int width = 256;
        int height = 64;
        Texture2D texture = new Texture2D(width, height);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float t = (float)y / height;
                Color color = Color.Lerp(
                    new Color(0.4f, 0.4f, 0.5f, 0.9f),
                    new Color(0.3f, 0.3f, 0.4f, 0.9f), t);
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// 导出对话
    /// </summary>
    private void ExportConversation()
    {
        if (conversationContexts.Count == 0)
        {
            AddMessage("暂无对话记录可导出", false, MessageType.System);
            return;
        }
        
        var currentContext = conversationContexts[currentConversationId];
        string exportText = $"对话导出 - {currentContext.title}\n";
        exportText += $"创建时间: {currentContext.createdAt:yyyy-MM-dd HH:mm:ss}\n";
        exportText += new string('=', 50) + "\n\n";
        
        foreach (var message in currentContext.messages)
        {
            exportText += $"[{message.timestamp:HH:mm:ss}] {message.senderName}: {message.content}\n\n";
        }
        
        // 这里可以添加实际的导出逻辑，比如保存到文件
        AddMessage($"对话已导出，共{currentContext.messages.Count}条消息", false, MessageType.Success);
        Debug.Log($"对话导出内容:\n{exportText}");
    }
    
    /// <summary>
    /// 创建快捷操作按钮
    /// </summary>
    private Button CreateQuickActionButton(string text, string tooltip, Action onClick)
    {
        var button = new Button(onClick);
        button.text = text;
        button.style.width = 120;
        button.style.height = 40;
        
        // 现代化快捷操作按钮样式
        button.style.backgroundImage = new StyleBackground(CreateQuickActionGradient());
        button.style.backgroundColor = secondaryColor; // 备用颜色
        
        button.style.color = Color.white;
        button.style.borderTopLeftRadius = 20;
        button.style.borderTopRightRadius = 20;
        button.style.borderBottomLeftRadius = 20;
        button.style.borderBottomRightRadius = 20;
        button.style.fontSize = 13;
        button.style.unityFontStyleAndWeight = FontStyle.Bold;
        button.style.unityTextAlign = TextAnchor.MiddleCenter;
        
        // 内边距
        button.style.paddingTop = 8;
        button.style.paddingBottom = 8;
        button.style.paddingLeft = 12;
        button.style.paddingRight = 12;
        
        // 添加悬停效果
        button.RegisterCallback<MouseEnterEvent>(evt => {
            button.style.backgroundImage = new StyleBackground(CreateQuickActionHoverGradient());
            button.style.scale = new Scale(new Vector3(1.05f, 1.05f, 1f));
        });
        button.RegisterCallback<MouseLeaveEvent>(evt => {
            button.style.backgroundImage = new StyleBackground(CreateQuickActionGradient());
            button.style.scale = new Scale(new Vector3(1f, 1f, 1f));
        });
        
        // 添加点击效果
        button.RegisterCallback<MouseDownEvent>(evt => {
            button.style.scale = new Scale(new Vector3(0.95f, 0.95f, 1f));
        });
        button.RegisterCallback<MouseUpEvent>(evt => {
            button.style.scale = new Scale(new Vector3(1.05f, 1.05f, 1f));
        });
        
        return button;
    }
    
    /// <summary>
    /// 创建快捷操作按钮渐变纹理
    /// </summary>
    private Texture2D CreateQuickActionGradient()
    {
        int width = 256;
        int height = 64;
        Texture2D texture = new Texture2D(width, height);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float t = (float)y / height;
                Color color = Color.Lerp(
                    new Color(secondaryColor.r * 1.1f, secondaryColor.g * 1.1f, secondaryColor.b * 1.1f, 1f),
                    new Color(secondaryColor.r * 0.8f, secondaryColor.g * 0.8f, secondaryColor.b * 0.8f, 1f), t);
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// 创建快捷操作按钮悬停渐变纹理
    /// </summary>
    private Texture2D CreateQuickActionHoverGradient()
    {
        int width = 256;
        int height = 64;
        Texture2D texture = new Texture2D(width, height);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float t = (float)y / height;
                Color color = Color.Lerp(
                    new Color(secondaryColor.r * 1.3f, secondaryColor.g * 1.3f, secondaryColor.b * 1.3f, 1f),
                    new Color(secondaryColor.r * 0.9f, secondaryColor.g * 0.9f, secondaryColor.b * 0.9f, 1f), t);
                texture.SetPixel(x, y, color);
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// 清空聊天记录
    /// </summary>
    public void ClearChatHistory()
    {
        chatHistory.Clear();
        chatContainer.Clear();
        AddWelcomeMessage();
    }
    
    /// <summary>
    /// 获取聊天记录
    /// </summary>
    public List<ChatMessage> GetChatHistory()
    {
        return new List<ChatMessage>(chatHistory);
    }
    
    /// <summary>
    /// 调试方法：检查AI助手状态
    /// </summary>
    [ContextMenu("检查AI助手状态")]
    public void DebugAIAssistantStatus()
    {
        Debug.Log("=== AI助手状态检查 ===");
        Debug.Log($"AI助手启用状态: {enableAIAssistant}");
        Debug.Log($"配置文件: {(config != null ? "已加载" : "未加载")}");
        Debug.Log($"UI文档: {(uiDocument != null ? "已创建" : "未创建")}");
        Debug.Log($"根元素: {(rootElement != null ? "已创建" : "未创建")}");
        Debug.Log($"聊天面板: {(chatPanel != null ? "已创建" : "未创建")}");
        Debug.Log($"切换按钮: {(toggleButton != null ? "已创建" : "未创建")}");
        Debug.Log($"聊天容器: {(chatContainer != null ? "已创建" : "未创建")}");
        Debug.Log($"输入字段: {(inputField != null ? "已创建" : "未创建")}");
        
        if (toggleButton != null)
        {
            Debug.Log($"按钮显示状态: {toggleButton.style.display}");
            Debug.Log($"按钮位置: bottom={toggleButton.style.bottom}, right={toggleButton.style.right}");
        }
        
        Debug.Log("=====================");
    }
    
    /// <summary>
    /// 强制显示AI助手按钮
    /// </summary>
    [ContextMenu("强制显示AI助手")]
    public void ForceShowAIAssistant()
    {
        if (toggleButton != null)
        {
            toggleButton.style.display = DisplayStyle.Flex;
            Debug.Log("AI助手按钮已强制显示");
        }
        else
        {
            Debug.LogWarning("AI助手按钮未创建");
        }
    }
    
    /// <summary>
    /// 自动查找或创建AI助手
    /// </summary>
    [ContextMenu("自动查找或创建AI助手")]
    public static void AutoFindOrCreateAIAssistant()
    {
        // 首先尝试查找现有的AI助手
        AIAssistantManager existingAssistant = FindObjectOfType<AIAssistantManager>();
        
        if (existingAssistant != null)
        {
            Debug.Log("找到现有AI助手，重新初始化");
            existingAssistant.InitializeAIAssistant();
            return;
        }
        
        // 如果没有找到，创建一个新的
        Debug.Log("未找到AI助手，正在创建新的...");
        GameObject assistantObject = new GameObject("AI Assistant");
        AIAssistantManager newAssistant = assistantObject.AddComponent<AIAssistantManager>();
        
        // 设置为DontDestroyOnLoad，确保在场景切换时保持
        DontDestroyOnLoad(assistantObject);
        
        Debug.Log("AI助手已创建并添加到场景中");
    }
    
    /// <summary>
    /// 消息出现动画协程
    /// </summary>
    private IEnumerator AnimateMessageAppearance(VisualElement messageElement)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        
        // 设置初始状态 - 主要使用透明度动画
        if (enableScaleAnimation)
        {
            try
            {
                SetElementScale(messageElement, 0.8f);
            }
            catch
            {
                Debug.Log("跳过缩放动画，使用透明度动画");
            }
        }
        SetElementOpacity(messageElement, 0f);
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float easeProgress = 1f - Mathf.Pow(1f - progress, 3f); // 缓出效果
            
            if (enableScaleAnimation)
            {
                try
                {
                    SetElementScale(messageElement, 0.8f + (0.2f * easeProgress));
                }
                catch
                {
                    // 如果缩放失败，只使用透明度动画
                }
            }
            SetElementOpacity(messageElement, easeProgress);
            
            yield return null;
        }
        
        // 确保最终状态
        if (enableScaleAnimation)
        {
            try
            {
                SetElementScale(messageElement, 1f);
            }
            catch
            {
                Debug.Log("跳过最终缩放设置");
            }
        }
        SetElementOpacity(messageElement, 1f);
    }
    
    /// <summary>
    /// 显示打字指示器
    /// </summary>
    private void ShowTypingIndicator()
    {
        if (!enableTypingIndicator) return;
        
        if (typingIndicator == null)
        {
            CreateTypingIndicator();
        }
        
        typingIndicator.style.display = DisplayStyle.Flex;
    }
    
    /// <summary>
    /// 隐藏打字指示器
    /// </summary>
    private void HideTypingIndicator()
    {
        if (typingIndicator != null)
        {
            typingIndicator.style.display = DisplayStyle.None;
        }
    }
    
    /// <summary>
    /// 创建打字指示器
    /// </summary>
    private void CreateTypingIndicator()
    {
        typingIndicator = new VisualElement();
        typingIndicator.style.flexDirection = FlexDirection.Row;
        typingIndicator.style.alignItems = Align.Center;
        typingIndicator.style.marginBottom = 15;
        typingIndicator.style.marginLeft = 8;
        typingIndicator.style.display = DisplayStyle.None;
        
        // 创建AI头像
        var avatar = new VisualElement();
        avatar.style.width = 32;
        avatar.style.height = 32;
        avatar.style.backgroundColor = assistantColor;
        avatar.style.borderTopLeftRadius = 16;
        avatar.style.borderTopRightRadius = 16;
        avatar.style.borderBottomLeftRadius = 16;
        avatar.style.borderBottomRightRadius = 16;
        avatar.style.marginRight = 10;
        
        // 创建打字动画点
        var dotsContainer = new VisualElement();
        dotsContainer.style.flexDirection = FlexDirection.Row;
        dotsContainer.style.alignItems = Align.Center;
        
        for (int i = 0; i < 3; i++)
        {
            var dot = new VisualElement();
            dot.style.width = 8;
            dot.style.height = 8;
            dot.style.backgroundColor = new Color(0.6f, 0.6f, 0.7f, 0.8f);
            dot.style.borderTopLeftRadius = 4;
            dot.style.borderTopRightRadius = 4;
            dot.style.borderBottomLeftRadius = 4;
            dot.style.borderBottomRightRadius = 4;
            dot.style.marginRight = 4;
            
            // 添加动画延迟
            dot.style.opacity = 0.3f;
            StartCoroutine(AnimateTypingDot(dot, i * 0.2f));
            
            dotsContainer.Add(dot);
        }
        
        typingIndicator.Add(avatar);
        typingIndicator.Add(dotsContainer);
        
        chatContainer.Add(typingIndicator);
    }
    
    /// <summary>
    /// 打字点动画协程
    /// </summary>
    private IEnumerator AnimateTypingDot(VisualElement dot, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        while (typingIndicator.style.display == DisplayStyle.Flex)
        {
            // 淡入
            float fadeInDuration = 0.3f;
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                dot.style.opacity = 0.3f + (0.7f * (elapsed / fadeInDuration));
                yield return null;
            }
            
            // 淡出
            float fadeOutDuration = 0.3f;
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                dot.style.opacity = 1f - (0.7f * (elapsed / fadeOutDuration));
                yield return null;
            }
            
            yield return new WaitForSeconds(0.2f);
        }
    }
    
    /// <summary>
    /// 动态更新UI颜色主题
    /// </summary>
    public void UpdateColorTheme(Color newPrimaryColor, Color newSecondaryColor)
    {
        assistantColor = newPrimaryColor;
        secondaryColor = newSecondaryColor;
        
        // 重新计算相关颜色
        ValidateColorSettings();
        
        // 更新现有UI元素的颜色
        UpdateUIElementColors();
    }
    
    /// <summary>
    /// 更新UI元素颜色
    /// </summary>
    private void UpdateUIElementColors()
    {
        if (sendButton != null)
        {
            sendButton.style.backgroundColor = assistantColor;
        }
        
        if (toggleButton != null)
        {
            toggleButton.style.backgroundColor = assistantColor;
        }
        
        if (chatPanel != null)
        {
            chatPanel.style.backgroundColor = backgroundColor;
        }
        
        // 更新聊天容器的边框颜色
        if (chatContainer != null)
        {
            chatContainer.style.borderTopColor = new Color(0.25f, 0.25f, 0.35f, 0.3f);
            chatContainer.style.borderBottomColor = new Color(0.1f, 0.1f, 0.15f, 0.3f);
            chatContainer.style.borderLeftColor = new Color(0.25f, 0.25f, 0.35f, 0.3f);
            chatContainer.style.borderRightColor = new Color(0.1f, 0.1f, 0.15f, 0.3f);
        }
    }
    
    /// <summary>
    /// 切换深色/浅色主题
    /// </summary>
    public void ToggleTheme()
    {
        if (backgroundColor.r < 0.5f) // 当前是深色主题
        {
            // 切换到浅色主题
            backgroundColor = new Color(0.9f, 0.9f, 0.95f, 0.95f);
            aiMessageColor = new Color(0.8f, 0.8f, 0.9f, 0.9f);
        }
        else // 当前是浅色主题
        {
            // 切换到深色主题
            backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            aiMessageColor = new Color(0.25f, 0.25f, 0.35f, 0.9f);
        }
        
        UpdateUIElementColors();
    }
    
    /// <summary>
    /// 获取当前主题信息
    /// </summary>
    public string GetCurrentThemeInfo()
    {
        string themeType = backgroundColor.r < 0.5f ? "深色主题" : "浅色主题";
        return $"当前主题: {themeType}\n主色调: RGB({assistantColor.r:F2}, {assistantColor.g:F2}, {assistantColor.b:F2})\n次要色: RGB({secondaryColor.r:F2}, {secondaryColor.g:F2}, {secondaryColor.b:F2})";
    }
    
    /// <summary>
    /// 兼容性方法：设置元素缩放
    /// </summary>
    private void SetElementScale(VisualElement element, float scale)
    {
        try
        {
            element.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"设置元素缩放失败: {e.Message}");
            // 如果缩放失败，尝试使用transform
            // if (element.worldTransform != null)
            // {
            //     element.worldTransform.scale = new Vector3(scale, scale, 1f);
            // }
        }
    }
    
    /// <summary>
    /// 兼容性方法：设置元素缩放（简化版本）
    /// </summary>
    private void SetElementScaleSimple(VisualElement element, float scale)
    {
        try
        {
            // 尝试使用Scale
            element.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }
        catch
        {
            // 如果失败，使用其他方式实现缩放效果
            try
            {
                // 使用width和height来模拟缩放效果
                float baseWidth = 100f; // 假设基础宽度
                float baseHeight = 100f; // 假设基础高度
                element.style.width = baseWidth * scale;
                element.style.height = baseHeight * scale;
            }
            catch
            {
                Debug.LogWarning("无法设置元素缩放，跳过缩放效果");
            }
        }
    }
    
    /// <summary>
    /// 兼容性方法：设置元素透明度
    /// </summary>
    private void SetElementOpacity(VisualElement element, float opacity)
    {
        try
        {
            element.style.opacity = opacity;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"设置元素透明度失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 获取Unity版本信息
    /// </summary>
    public string GetUnityVersionInfo()
    {
        return $"Unity版本: {Application.unityVersion}\n" +
               $"平台: {Application.platform}\n" +
               $"系统语言: {Application.systemLanguage}\n" +
               $"目标帧率: {Application.targetFrameRate}";
    }
    
    /// <summary>
    /// 检查UI Toolkit兼容性
    /// </summary>
    public void CheckUIToolkitCompatibility()
    {
        Debug.Log("=== UI Toolkit兼容性检查 ===");
        Debug.Log($"Unity版本: {Application.unityVersion}");
        
        // 检查基本功能
        try
        {
            var testElement = new VisualElement();
            testElement.style.backgroundColor = Color.red;
            testElement.style.width = 100;
            testElement.style.height = 100;
            Debug.Log("✓ 基本样式设置正常");
            
            // 测试Scale
            try
            {
                testElement.style.scale = new Scale(new Vector3(1f, 1f, 1f));
                Debug.Log("✓ Scale样式支持正常");
            }
            catch
            {
                Debug.LogWarning("⚠ Scale样式不支持，将使用兼容模式");
            }
            
            // 测试透明度
            try
            {
                testElement.style.opacity = 0.5f;
                Debug.Log("✓ 透明度样式支持正常");
            }
            catch
            {
                Debug.LogWarning("⚠ 透明度样式不支持");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ UI Toolkit兼容性检查失败: {e.Message}");
        }
        
        Debug.Log("=============================");
    }

    /// <summary>
    /// 检查聊天面板状态
    /// </summary>
    public void CheckChatPanelStatus()
    {
        Debug.Log("=== AI助手聊天面板状态检查 ===");
        Debug.Log($"chatPanel: {(chatPanel != null ? "已创建" : "未创建")}");
        if (chatPanel != null)
        {
            Debug.Log($"显示状态: {chatPanel.style.display}");
            Debug.Log($"位置: top={chatPanel.style.top}, left={chatPanel.style.left}");
            Debug.Log($"尺寸: width={chatPanel.style.width}, height={chatPanel.style.height}");
            Debug.Log($"层级: 使用BringToFront()确保在最上层");
        }
        
        Debug.Log($"rootElement: {(rootElement != null ? "已创建" : "未创建")}");
        Debug.Log($"uiDocument: {(uiDocument != null ? "已创建" : "未创建")}");
        if (uiDocument != null)
        {
            Debug.Log($"rootVisualElement: {(uiDocument.rootVisualElement != null ? "已创建" : "未创建")}");
        }
        Debug.Log("=== 状态检查完成 ===");
    }
}
