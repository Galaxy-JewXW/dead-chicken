using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;
using System;

/// <summary>
/// AI助手管理器 - 提供智能聊天和系统帮助功能
/// </summary>
public class AIAssistantManager : MonoBehaviour
{
    [Header("AI助手设置")]
    public bool enableAIAssistant = true;
    public string assistantName = "电力线助手";
    public Color assistantColor = new Color(0.2f, 0.8f, 0.4f, 1f); // 绿色主题
    
    [Header("配置文件")]
    [Tooltip("AI助手配置文件，包含知识库和设置")]
    public AIAssistantConfig config;
    
    [Header("聊天设置")]
    public int maxChatHistory = 50; // 最大聊天记录数
    public float typingSpeed = 0.05f; // 打字速度
    
    [Header("UI组件")]
    private UIDocument uiDocument;
    private VisualElement rootElement;
    private VisualElement chatPanel;
    private VisualElement chatContainer;
    private TextField inputField;
    private Button sendButton;
    private Button toggleButton;
    
    // 聊天记录
    private List<ChatMessage> chatHistory = new List<ChatMessage>();
    private bool isTyping = false;
    
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
            maxChatHistory = config.maxChatHistory;
            typingSpeed = config.typingSpeed;
        }
    }
    
    /// <summary>
    /// 创建UI文档
    /// </summary>
    private void CreateUIDocument()
    {
        // 创建独立的UIDocument
        GameObject uiObject = new GameObject("AI Assistant UI");
        uiObject.transform.SetParent(transform);
        
        uiDocument = uiObject.AddComponent<UIDocument>();
        
        // 创建UI结构
        CreateUIStructure();
    }
    
    /// <summary>
    /// 聊天消息结构
    /// </summary>
    [System.Serializable]
    public class ChatMessage
    {
        public string content;
        public bool isUser;
        public DateTime timestamp;
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
        
        // 创建聊天面板
        chatPanel = new VisualElement();
        chatPanel.style.width = 400;
        chatPanel.style.height = 600;
        chatPanel.style.position = Position.Absolute;
        chatPanel.style.bottom = 20;
        chatPanel.style.right = 20;
        chatPanel.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        chatPanel.style.borderTopLeftRadius = 10;
        chatPanel.style.borderTopRightRadius = 10;
        chatPanel.style.borderBottomLeftRadius = 10;
        chatPanel.style.borderBottomRightRadius = 10;
        chatPanel.style.paddingTop = 10;
        chatPanel.style.paddingBottom = 10;
        chatPanel.style.paddingLeft = 10;
        chatPanel.style.paddingRight = 10;
        chatPanel.style.display = DisplayStyle.None; // 默认隐藏
        
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
        uiDocument.rootVisualElement.Add(rootElement);
        
        // 确保UI元素在最上层
        rootElement.BringToFront();
        
        // 强制刷新UI
        rootElement.MarkDirtyRepaint();
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
        titleBar.style.marginBottom = 10;
        
        var titleLabel = new Label(assistantName);
        titleLabel.style.color = assistantColor;
        titleLabel.style.fontSize = 18;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        
        var closeButton = new Button(() => ToggleChatPanel(false));
        closeButton.text = "×";
        closeButton.style.width = 30;
        closeButton.style.height = 30;
        closeButton.style.backgroundColor = Color.clear;
        closeButton.style.color = Color.white;
        closeButton.style.borderTopWidth = 0;
        closeButton.style.borderBottomWidth = 0;
        closeButton.style.borderLeftWidth = 0;
        closeButton.style.borderRightWidth = 0;
        closeButton.style.fontSize = 20;
        
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
        chatContainer.style.marginBottom = 10;
        chatContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        chatContainer.style.borderTopLeftRadius = 5;
        chatContainer.style.borderTopRightRadius = 5;
        chatContainer.style.borderBottomLeftRadius = 5;
        chatContainer.style.borderBottomRightRadius = 5;
        chatContainer.style.paddingTop = 10;
        chatContainer.style.paddingBottom = 10;
        chatContainer.style.paddingLeft = 10;
        chatContainer.style.paddingRight = 10;
        
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
        
        inputField = new TextField();
        inputField.style.flexGrow = 1;
        inputField.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        inputField.style.color = Color.white;
        inputField.style.borderTopLeftRadius = 5;
        inputField.style.borderTopRightRadius = 5;
        inputField.style.borderBottomLeftRadius = 5;
        inputField.style.borderBottomRightRadius = 5;
        inputField.style.paddingLeft = 10;
        inputField.style.paddingRight = 10;
        inputField.style.paddingTop = 5;
        inputField.style.paddingBottom = 5;
        inputField.style.marginRight = 10; // 添加右边距
        
        // 添加回车键发送功能
        inputField.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                SendMessage();
            }
        });
        
        sendButton = new Button(SendMessage);
        sendButton.text = "发送";
        sendButton.style.width = 60;
        sendButton.style.height = 35;
        sendButton.style.backgroundColor = assistantColor;
        sendButton.style.color = Color.white;
        sendButton.style.borderTopLeftRadius = 5;
        sendButton.style.borderTopRightRadius = 5;
        sendButton.style.borderBottomLeftRadius = 5;
        sendButton.style.borderBottomRightRadius = 5;
        sendButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        
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
        toggleButton.style.width = 80;
        toggleButton.style.height = 40;
        toggleButton.style.position = Position.Absolute;
        toggleButton.style.bottom = 20;
        toggleButton.style.right = 440;
        toggleButton.style.backgroundColor = assistantColor;
        toggleButton.style.color = Color.white;
        toggleButton.style.borderTopLeftRadius = 5;
        toggleButton.style.borderTopRightRadius = 5;
        toggleButton.style.borderBottomLeftRadius = 5;
        toggleButton.style.borderBottomRightRadius = 5;
        toggleButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        toggleButton.style.fontSize = 14;
        toggleButton.style.display = DisplayStyle.Flex; // 确保按钮显示
        
        // 添加调试信息
        Debug.Log("AI助手按钮已创建，位置：右下角");
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
        bool newState = forceState ?? (chatPanel.style.display == DisplayStyle.None);
        chatPanel.style.display = newState ? DisplayStyle.Flex : DisplayStyle.None;
        
        if (newState)
        {
            inputField.Focus();
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
        // 这里将实现AI逻辑
        string response = GenerateAIResponse(message);
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
        
        // 如果都没有找到，提供通用回复
        return GenerateFallbackResponse(userMessage);
    }
    
    /// <summary>
    /// 生成备用回复
    /// </summary>
    private string GenerateFallbackResponse(string userMessage)
    {
        string lowerMessage = userMessage.ToLower();
        
        // 基于关键词的智能回复
        if (lowerMessage.Contains("如何") || lowerMessage.Contains("怎么"))
        {
            return "关于操作指导，您可以：\n• 询问具体功能的使用方法\n• 查看系统帮助文档\n• 使用快捷键操作\n• 参考操作示例";
        }
        else if (lowerMessage.Contains("问题") || lowerMessage.Contains("错误"))
        {
            return "如果遇到问题，建议您：\n• 检查输入数据格式\n• 确认组件配置正确\n• 查看控制台错误信息\n• 重启相关功能模块";
        }
        else if (lowerMessage.Contains("功能") || lowerMessage.Contains("特性"))
        {
            return "系统主要功能包括：\n• 电力线可视化和管理\n• 多视角相机控制\n• 危险监测和预警\n• 无人机巡检管理\n• 地形适配和优化";
        }
        else
        {
            return "我理解您的问题，但可能需要更具体的信息。您可以：\n• 询问特定功能的使用方法\n• 了解系统配置选项\n• 获取操作指导\n• 查看常见问题解答";
        }
    }
    
    /// <summary>
    /// 添加消息到聊天记录
    /// </summary>
    public void AddMessage(string content, bool isUser)
    {
        var message = new ChatMessage
        {
            content = content,
            isUser = isUser,
            timestamp = DateTime.Now
        };
        
        chatHistory.Add(message);
        
        // 限制聊天记录数量
        if (chatHistory.Count > maxChatHistory)
        {
            chatHistory.RemoveAt(0);
        }
        
        // 显示消息
        DisplayMessage(message);
    }
    
    /// <summary>
    /// 显示消息
    /// </summary>
    private void DisplayMessage(ChatMessage message)
    {
        var messageElement = new VisualElement();
        messageElement.style.flexDirection = FlexDirection.Row;
        messageElement.style.marginBottom = 10;
        messageElement.style.alignItems = Align.FlexStart;
        
        if (message.isUser)
        {
            messageElement.style.justifyContent = Justify.FlexEnd;
        }
        
        var bubble = new VisualElement();
        bubble.style.maxWidth = Length.Percent(80);
        bubble.style.paddingTop = 8;
        bubble.style.paddingBottom = 8;
        bubble.style.paddingLeft = 12;
        bubble.style.paddingRight = 12;
        bubble.style.borderTopLeftRadius = 15;
        bubble.style.borderTopRightRadius = 15;
        bubble.style.borderBottomLeftRadius = 15;
        bubble.style.borderBottomRightRadius = 15;
        bubble.style.backgroundColor = message.isUser ? assistantColor : new Color(0.3f, 0.3f, 0.3f, 0.8f);
        
        var textLabel = new Label(message.content);
        textLabel.style.color = Color.white;
        textLabel.style.whiteSpace = WhiteSpace.Normal;
        textLabel.style.unityTextAlign = TextAnchor.UpperLeft;
        
        bubble.Add(textLabel);
        messageElement.Add(bubble);
        
        chatContainer.Add(messageElement);
        
        // 滚动到底部 - 使用ScrollView的方法
        if (chatContainer is ScrollView scrollView)
        {
            scrollView.scrollOffset = new Vector2(0, scrollView.scrollOffset.y + 100);
        }
    }
    
    /// <summary>
    /// 添加欢迎消息
    /// </summary>
    private void AddWelcomeMessage()
    {
        string welcomeMessage = $"欢迎使用{assistantName}！\n\n我可以帮助您：\n• 了解系统功能\n• 提供操作指导\n• 解答技术问题\n• 查询系统状态\n\n有什么可以帮助您的吗？";
        AddMessage(welcomeMessage, false);
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
}
