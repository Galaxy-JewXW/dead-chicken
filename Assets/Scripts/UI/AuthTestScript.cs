using UnityEngine;
using UserAuth;

namespace UserAuthTest
{
    /// <summary>
    /// 用户认证系统测试脚本
    /// 用于测试注册、登录等功能
    /// </summary>
    public class AuthTestScript : MonoBehaviour
    {
        [Header("测试设置")]
        [SerializeField] private bool enableAutoTest = false;
        [SerializeField] private float testDelay = 2f;
        
        private SimpleUserAuth authSystem;
        
        void Start()
        {
            // 获取认证系统引用
            authSystem = FindObjectOfType<SimpleUserAuth>();
            
            if (authSystem == null)
            {
                Debug.LogError("[AuthTestScript] 未找到SimpleUserAuth组件");
                return;
            }
            
            // 订阅认证事件
            authSystem.OnUserLoggedIn += OnUserLoggedIn;
            authSystem.OnUserLoggedOut += OnUserLoggedOut;
            authSystem.OnAuthMessage += OnAuthMessage;
            
            Debug.Log("[AuthTestScript] 认证测试脚本已初始化");
            
            // 如果启用自动测试，延迟执行
            if (enableAutoTest)
            {
                Invoke(nameof(RunAutoTest), testDelay);
            }
        }
        
        /// <summary>
        /// 运行自动测试
        /// </summary>
        private void RunAutoTest()
        {
            Debug.Log("[AuthTestScript] 开始自动测试...");
            
            // 测试注册
            TestRegistration();
            
            // 延迟测试登录
            Invoke(nameof(TestLogin), testDelay);
        }
        
        /// <summary>
        /// 测试用户注册
        /// </summary>
        [ContextMenu("测试用户注册")]
        public void TestRegistration()
        {
            if (authSystem == null) return;
            
            string testUsername = "testuser_" + System.DateTime.Now.ToString("HHmmss");
            string testPassword = "testpass123";
            string testEmail = $"{testUsername}@test.com";
            
            Debug.Log($"[AuthTestScript] 测试注册用户: {testUsername}");
            
            bool success = authSystem.RegisterUser(testUsername, testPassword, testEmail);
            
            if (success)
            {
                Debug.Log($"[AuthTestScript] 用户 {testUsername} 注册成功");
            }
            else
            {
                Debug.LogWarning($"[AuthTestScript] 用户 {testUsername} 注册失败");
            }
        }
        
        /// <summary>
        /// 测试用户登录
        /// </summary>
        [ContextMenu("测试用户登录")]
        public void TestLogin()
        {
            if (authSystem == null) return;
            
            string testUsername = "testuser_" + System.DateTime.Now.ToString("HHmmss");
            string testPassword = "testpass123";
            
            Debug.Log($"[AuthTestScript] 测试登录用户: {testUsername}");
            
            bool success = authSystem.LoginUser(testUsername, testPassword);
            
            if (success)
            {
                Debug.Log($"[AuthTestScript] 用户 {testUsername} 登录成功");
            }
            else
            {
                Debug.LogWarning($"[AuthTestScript] 用户 {testUsername} 登录失败");
            }
        }
        
        /// <summary>
        /// 测试用户登出
        /// </summary>
        [ContextMenu("测试用户登出")]
        public void TestLogout()
        {
            if (authSystem == null) return;
            
            Debug.Log("[AuthTestScript] 测试用户登出");
            authSystem.Logout();
        }
        
        /// <summary>
        /// 显示所有用户
        /// </summary>
        [ContextMenu("显示所有用户")]
        public void ShowAllUsers()
        {
            if (authSystem == null) return;
            
            var users = authSystem.GetAllUsers();
            Debug.Log($"[AuthTestScript] 当前系统中共有 {users.Count} 个用户:");
            
            foreach (var user in users)
            {
                Debug.Log($"  - {user.Username} ({user.Email}) - 创建时间: {user.CreatedAt:yyyy-MM-dd HH:mm}");
            }
        }
        
        /// <summary>
        /// 检查当前登录状态
        /// </summary>
        [ContextMenu("检查登录状态")]
        public void CheckLoginStatus()
        {
            if (authSystem == null) return;
            
            bool isLoggedIn = authSystem.IsUserLoggedIn();
            Debug.Log($"[AuthTestScript] 当前登录状态: {(isLoggedIn ? "已登录" : "未登录")}");
            
            if (isLoggedIn)
            {
                var currentUser = authSystem.GetCurrentUser();
                Debug.Log($"  当前用户: {currentUser.Username} ({currentUser.Email})");
                Debug.Log($"  登录次数: {currentUser.LoginCount}");
                Debug.Log($"  最后登录: {currentUser.LastLoginAt:yyyy-MM-dd HH:mm}");
            }
        }
        
        #region 事件处理
        
        private void OnUserLoggedIn(UserData user)
        {
            Debug.Log($"[AuthTestScript] 用户登录事件: {user.Username}");
        }
        
        private void OnUserLoggedOut()
        {
            Debug.Log("[AuthTestScript] 用户登出事件");
        }
        
        private void OnAuthMessage(string message)
        {
            Debug.Log($"[AuthTestScript] 认证消息: {message}");
        }
        
        #endregion
        
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
