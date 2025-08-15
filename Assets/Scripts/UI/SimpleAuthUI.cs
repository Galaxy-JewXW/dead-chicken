using UnityEngine;
using UnityEngine.UIElements;
using UserAuth;

namespace UserAuthUI
{
    /// <summary>
    /// 简单的用户认证UI界面
    /// 提供注册和登录功能
    /// </summary>
    public class SimpleAuthUI : MonoBehaviour
    {
        [Header("UI组件引用")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private SimpleUserAuth authSystem;
        
        [Header("UI设置")]
        [SerializeField] private bool showLoginByDefault = true;
        
        // UI元素引用
        private VisualElement rootElement;
        private VisualElement loginPanel;
        private VisualElement registerPanel;
        private VisualElement userInfoPanel;
        
        // 输入字段
        private TextField usernameField;
        private TextField passwordField;
        private TextField emailField;
        private TextField confirmPasswordField;
        
        // 按钮
        private Button loginButton;
        private Button registerButton;
        private Button switchToRegisterButton;
        private Button switchToLoginButton;
        private Button logoutButton;
        
        // 标签
        private Label messageLabel;
        private Label userInfoLabel;
        
        void Start()
        {
            // 获取认证系统引用
            if (authSystem == null)
                authSystem = FindObjectOfType<SimpleUserAuth>();
            
            if (authSystem == null)
            {
                Debug.LogError("[SimpleAuthUI] 未找到SimpleUserAuth组件");
                return;
            }
            
            // 订阅认证事件
            authSystem.OnUserLoggedIn += OnUserLoggedIn;
            authSystem.OnUserLoggedOut += OnUserLoggedOut;
            authSystem.OnAuthMessage += OnAuthMessage;
            
            // 初始化UI
            InitializeUI();
            
            // 显示默认面板
            if (showLoginByDefault)
                ShowLoginPanel();
            else
                ShowRegisterPanel();
        }
        
        /// <summary>
        /// 初始化UI界面
        /// </summary>
        private void InitializeUI()
        {
            if (uiDocument == null)
            {
                Debug.LogError("[SimpleAuthUI] 未找到UIDocument组件");
                return;
            }
            
            rootElement = uiDocument.rootVisualElement;
            
            // 查找UI元素
            FindUIElements();
            
            // 设置事件监听
            SetupEventListeners();
            
            // 检查当前登录状态
            CheckCurrentLoginStatus();
        }
        
        /// <summary>
        /// 查找UI元素
        /// </summary>
        private void FindUIElements()
        {
            // 查找面板
            loginPanel = rootElement.Q<VisualElement>("login-panel");
            registerPanel = rootElement.Q<VisualElement>("register-panel");
            userInfoPanel = rootElement.Q<VisualElement>("user-info-panel");
            
            // 查找输入字段
            usernameField = rootElement.Q<TextField>("username-field");
            passwordField = rootElement.Q<TextField>("password-field");
            emailField = rootElement.Q<TextField>("email-field");
            confirmPasswordField = rootElement.Q<TextField>("confirm-password-field");
            
            // 查找按钮
            loginButton = rootElement.Q<Button>("login-button");
            registerButton = rootElement.Q<Button>("register-button");
            switchToRegisterButton = rootElement.Q<Button>("switch-to-register-button");
            switchToLoginButton = rootElement.Q<Button>("switch-to-login-button");
            logoutButton = rootElement.Q<Button>("logout-button");
            
            // 查找标签
            messageLabel = rootElement.Q<Label>("message-label");
            userInfoLabel = rootElement.Q<Label>("user-info-label");
            
            // 验证UI元素是否存在
            ValidateUIElements();
        }
        
        /// <summary>
        /// 验证UI元素
        /// </summary>
        private void ValidateUIElements()
        {
            if (loginPanel == null) Debug.LogWarning("[SimpleAuthUI] 未找到login-panel");
            if (registerPanel == null) Debug.LogWarning("[SimpleAuthUI] 未找到register-panel");
            if (userInfoPanel == null) Debug.LogWarning("[SimpleAuthUI] 未找到user-info-panel");
            if (usernameField == null) Debug.LogWarning("[SimpleAuthUI] 未找到username-field");
            if (passwordField == null) Debug.LogWarning("[SimpleAuthUI] 未找到password-field");
            if (emailField == null) Debug.LogWarning("[SimpleAuthUI] 未找到email-field");
            if (confirmPasswordField == null) Debug.LogWarning("[SimpleAuthUI] 未找到confirm-password-field");
            if (loginButton == null) Debug.LogWarning("[SimpleAuthUI] 未找到login-button");
            if (registerButton == null) Debug.LogWarning("[SimpleAuthUI] 未找到register-button");
            if (messageLabel == null) Debug.LogWarning("[SimpleAuthUI] 未找到message-label");
        }
        
        /// <summary>
        /// 设置事件监听
        /// </summary>
        private void SetupEventListeners()
        {
            // 登录按钮
            if (loginButton != null)
                loginButton.clicked += OnLoginButtonClicked;
            
            // 注册按钮
            if (registerButton != null)
                registerButton.clicked += OnRegisterButtonClicked;
            
            // 切换面板按钮
            if (switchToRegisterButton != null)
                switchToRegisterButton.clicked += ShowRegisterPanel;
            
            if (switchToLoginButton != null)
                switchToLoginButton.clicked += ShowLoginPanel;
            
            // 登出按钮
            if (logoutButton != null)
                logoutButton.clicked += OnLogoutButtonClicked;
        }
        
        /// <summary>
        /// 检查当前登录状态
        /// </summary>
        private void CheckCurrentLoginStatus()
        {
            if (authSystem.IsUserLoggedIn())
            {
                OnUserLoggedIn(authSystem.GetCurrentUser());
            }
        }
        
        /// <summary>
        /// 显示登录面板
        /// </summary>
        private void ShowLoginPanel()
        {
            if (loginPanel != null) loginPanel.style.display = DisplayStyle.Flex;
            if (registerPanel != null) registerPanel.style.display = DisplayStyle.None;
            if (userInfoPanel != null) userInfoPanel.style.display = DisplayStyle.None;
            
            ClearMessage();
            ClearInputFields();
        }
        
        /// <summary>
        /// 显示注册面板
        /// </summary>
        private void ShowRegisterPanel()
        {
            if (loginPanel != null) loginPanel.style.display = DisplayStyle.None;
            if (registerPanel != null) registerPanel.style.display = DisplayStyle.Flex;
            if (userInfoPanel != null) userInfoPanel.style.display = DisplayStyle.None;
            
            ClearMessage();
            ClearInputFields();
        }
        
        /// <summary>
        /// 显示用户信息面板
        /// </summary>
        private void ShowUserInfoPanel()
        {
            if (loginPanel != null) loginPanel.style.display = DisplayStyle.None;
            if (registerPanel != null) registerPanel.style.display = DisplayStyle.None;
            if (userInfoPanel != null) userInfoPanel.style.display = DisplayStyle.Flex;
        }
        
        /// <summary>
        /// 登录按钮点击事件
        /// </summary>
        private void OnLoginButtonClicked()
        {
            if (usernameField == null || passwordField == null) return;
            
            string username = usernameField.value;
            string password = passwordField.value;
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowMessage("请输入用户名和密码", MessageType.Error);
                return;
            }
            
            // 调用认证系统登录
            bool success = authSystem.LoginUser(username, password);
            
            if (!success)
            {
                // 登录失败时显示错误消息（由认证系统处理）
                return;
            }
        }
        
        /// <summary>
        /// 注册按钮点击事件
        /// </summary>
        private void OnRegisterButtonClicked()
        {
            if (usernameField == null || passwordField == null || confirmPasswordField == null) return;
            
            string username = usernameField.value;
            string password = passwordField.value;
            string confirmPassword = confirmPasswordField.value;
            
            // 基本验证
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowMessage("请填写用户名和密码", MessageType.Error);
                return;
            }
            
            if (password != confirmPassword)
            {
                ShowMessage("两次输入的密码不一致", MessageType.Error);
                return;
            }
            
            // 调用认证系统注册
            bool success = authSystem.RegisterUser(username, password);
            
            if (success)
            {
                ShowMessage("注册成功！请登录", MessageType.Success);
                // 注册成功后切换到登录面板
                ShowLoginPanel();
            }
        }
        
        /// <summary>
        /// 登出按钮点击事件
        /// </summary>
        private void OnLogoutButtonClicked()
        {
            authSystem.Logout();
        }
        
        /// <summary>
        /// 用户登录成功事件
        /// </summary>
        private void OnUserLoggedIn(UserData user)
        {
            // 不自动显示用户信息面板，让主界面管理器处理界面切换
            // ShowUserInfoPanel();
            
            if (userInfoLabel != null)
            {
                userInfoLabel.text = $"欢迎，{user.Username}！\n" +
                                   $"注册时间：{user.CreatedAt:yyyy-MM-dd HH:mm}\n" +
                                   $"登录次数：{user.LoginCount}";
            }
            
            ShowMessage($"登录成功！欢迎回来，{user.Username}", MessageType.Success);
        }
        
        /// <summary>
        /// 用户登出事件
        /// </summary>
        private void OnUserLoggedOut()
        {
            // 不自动显示登录面板，让主界面管理器处理界面切换
            // ShowLoginPanel();
            ShowMessage("已成功登出", MessageType.Info);
        }
        
        /// <summary>
        /// 认证消息事件
        /// </summary>
        private void OnAuthMessage(string message)
        {
            ShowMessage(message, MessageType.Info);
        }
        
        /// <summary>
        /// 显示消息
        /// </summary>
        private void ShowMessage(string message, MessageType type = MessageType.Info)
        {
            if (messageLabel != null)
            {
                messageLabel.text = message;
                
                // 根据消息类型设置颜色
                switch (type)
                {
                    case MessageType.Success:
                        messageLabel.style.color = new Color(0.2f, 0.8f, 0.2f);
                        break;
                    case MessageType.Error:
                        messageLabel.style.color = new Color(0.8f, 0.2f, 0.2f);
                        break;
                    case MessageType.Warning:
                        messageLabel.style.color = new Color(0.8f, 0.6f, 0.2f);
                        break;
                    default:
                        messageLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
                        break;
                }
            }
        }
        
        /// <summary>
        /// 清除消息
        /// </summary>
        private void ClearMessage()
        {
            if (messageLabel != null)
                messageLabel.text = "";
        }
        
        /// <summary>
        /// 清除输入字段
        /// </summary>
        private void ClearInputFields()
        {
            if (usernameField != null) usernameField.value = "";
            if (passwordField != null) passwordField.value = "";
            if (emailField != null) emailField.value = "";
            if (confirmPasswordField != null) confirmPasswordField.value = "";
        }
        
        /// <summary>
        /// 消息类型枚举
        /// </summary>
        public enum MessageType
        {
            Info,
            Success,
            Warning,
            Error
        }
        
        void OnDestroy()
        {
            // 取消事件订阅
            if (authSystem != null)
            {
                authSystem.OnUserLoggedIn -= OnUserLoggedIn;
                authSystem.OnUserLoggedOut -= OnUserLoggedOut;
                authSystem.OnAuthMessage -= OnAuthMessage;
            }
        }
    }
}
