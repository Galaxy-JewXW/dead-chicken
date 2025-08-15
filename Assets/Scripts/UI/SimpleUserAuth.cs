using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace UserAuth
{
    /// <summary>
    /// 简化版用户认证系统
    /// 提供基本的用户注册、登录功能
    /// </summary>
    public class SimpleUserAuth : MonoBehaviour
    {
        [Header("认证设置")]
        [SerializeField] private bool enableDebugLog = true;
        
        // 当前登录用户
        private UserData currentUser;
        
        // 用户数据文件路径
        private string userDataPath;
        
        // 事件
        public event Action<UserData> OnUserLoggedIn;
        public event Action OnUserLoggedOut;
        public event Action<string> OnAuthMessage;
        
        void Start()
        {
            InitializeAuthSystem();
        }
        
        /// <summary>
        /// 初始化认证系统
        /// </summary>
        private void InitializeAuthSystem()
        {
            // 设置用户数据文件路径
            userDataPath = Path.Combine(Application.persistentDataPath, "users.json");
            
            // 确保数据目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(userDataPath));
            
            if (enableDebugLog)
                Debug.Log("[SimpleUserAuth] 用户认证系统已初始化");
        }
        
        /// <summary>
        /// 用户注册
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="email">邮箱</param>
        /// <returns>是否注册成功</returns>
        public bool RegisterUser(string username, string password, string email)
        {
            try
            {
                // 基本验证
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ShowMessage("用户名和密码不能为空");
                    return false;
                }
                
                if (username.Length < 3)
                {
                    ShowMessage("用户名长度不能少于3个字符");
                    return false;
                }
                
                if (password.Length < 6)
                {
                    ShowMessage("密码长度不能少于6个字符");
                    return false;
                }
                
                // 检查用户是否已存在
                if (IsUserExists(username))
                {
                    ShowMessage("用户名已存在");
                    return false;
                }
                
                // 创建新用户
                UserData newUser = new UserData
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = username,
                    Email = email,
                    Password = password, // 注意：实际项目中应该加密存储
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };
                
                // 保存用户数据
                SaveUser(newUser);
                
                ShowMessage($"用户 {username} 注册成功！");
                if (enableDebugLog)
                    Debug.Log($"[SimpleUserAuth] 用户 {username} 注册成功");
                
                return true;
            }
            catch (Exception ex)
            {
                ShowMessage($"注册失败: {ex.Message}");
                if (enableDebugLog)
                    Debug.LogError($"[SimpleUserAuth] 注册失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>是否登录成功</returns>
        public bool LoginUser(string username, string password)
        {
            try
            {
                // 基本验证
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ShowMessage("用户名和密码不能为空");
                    return false;
                }
                
                // 查找用户
                UserData user = FindUser(username);
                if (user == null)
                {
                    ShowMessage("用户名不存在");
                    return false;
                }
                
                // 验证密码
                if (user.Password != password)
                {
                    ShowMessage("密码错误");
                    return false;
                }
                
                // 检查用户状态
                if (!user.IsActive)
                {
                    ShowMessage("账户已被禁用");
                    return false;
                }
                
                // 登录成功
                currentUser = user;
                user.LastLoginAt = DateTime.Now;
                user.LoginCount++;
                
                // 更新用户数据
                UpdateUser(user);
                
                ShowMessage($"欢迎回来，{username}！");
                OnUserLoggedIn?.Invoke(user);
                
                if (enableDebugLog)
                    Debug.Log($"[SimpleUserAuth] 用户 {username} 登录成功");
                
                return true;
            }
            catch (Exception ex)
            {
                ShowMessage($"登录失败: {ex.Message}");
                if (enableDebugLog)
                    Debug.LogError($"[SimpleUserAuth] 登录失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 用户登出
        /// </summary>
        public void Logout()
        {
            if (currentUser != null)
            {
                if (enableDebugLog)
                    Debug.Log($"[SimpleUserAuth] 用户 {currentUser.Username} 登出");
                
                currentUser = null;
                OnUserLoggedOut?.Invoke();
                ShowMessage("已成功登出");
            }
        }
        
        /// <summary>
        /// 获取当前登录用户
        /// </summary>
        public UserData GetCurrentUser()
        {
            return currentUser;
        }
        
        /// <summary>
        /// 检查用户是否已登录
        /// </summary>
        public bool IsUserLoggedIn()
        {
            return currentUser != null;
        }
        
        /// <summary>
        /// 获取所有用户列表（仅用于调试）
        /// </summary>
        public List<UserData> GetAllUsers()
        {
            return LoadAllUsers();
        }
        
        #region 私有方法
        
        /// <summary>
        /// 检查用户是否存在
        /// </summary>
        private bool IsUserExists(string username)
        {
            var users = LoadAllUsers();
            return users.Exists(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// 根据用户名查找用户
        /// </summary>
        private UserData FindUser(string username)
        {
            var users = LoadAllUsers();
            return users.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// 加载所有用户数据
        /// </summary>
        private List<UserData> LoadAllUsers()
        {
            try
            {
                if (File.Exists(userDataPath))
                {
                    string json = File.ReadAllText(userDataPath);
                    return JsonUtility.FromJson<UserList>(json)?.Users ?? new List<UserData>();
                }
            }
            catch (Exception ex)
            {
                if (enableDebugLog)
                    Debug.LogError($"[SimpleUserAuth] 加载用户数据失败: {ex.Message}");
            }
            return new List<UserData>();
        }
        
        /// <summary>
        /// 保存新用户
        /// </summary>
        private void SaveUser(UserData user)
        {
            try
            {
                var users = LoadAllUsers();
                users.Add(user);
                
                var userList = new UserList { Users = users };
                string json = JsonUtility.ToJson(userList, true);
                File.WriteAllText(userDataPath, json);
            }
            catch (Exception ex)
            {
                if (enableDebugLog)
                    Debug.LogError($"[SimpleUserAuth] 保存用户数据失败: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 更新用户数据
        /// </summary>
        private void UpdateUser(UserData updatedUser)
        {
            try
            {
                var users = LoadAllUsers();
                var existingUser = users.Find(u => u.Id == updatedUser.Id);
                
                if (existingUser != null)
                {
                    int index = users.IndexOf(existingUser);
                    users[index] = updatedUser;
                    
                    var userList = new UserList { Users = users };
                    string json = JsonUtility.ToJson(userList, true);
                    File.WriteAllText(userDataPath, json);
                }
            }
            catch (Exception ex)
            {
                if (enableDebugLog)
                    Debug.LogError($"[SimpleUserAuth] 更新用户数据失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 显示认证消息
        /// </summary>
        private void ShowMessage(string message)
        {
            OnAuthMessage?.Invoke(message);
            if (enableDebugLog)
                Debug.Log($"[SimpleUserAuth] {message}");
        }
        
        #endregion
    }
    
    /// <summary>
    /// 用户数据模型
    /// </summary>
    [System.Serializable]
    public class UserData
    {
        public string Id;
        public string Username;
        public string Email;
        public string Password;
        public DateTime CreatedAt;
        public DateTime LastLoginAt;
        public int LoginCount;
        public bool IsActive;
        
        public UserData()
        {
            Id = "";
            Username = "";
            Email = "";
            Password = "";
            CreatedAt = DateTime.Now;
            LastLoginAt = DateTime.Now;
            LoginCount = 0;
            IsActive = true;
        }
    }
    
    /// <summary>
    /// 用户列表包装类（用于JSON序列化）
    /// </summary>
    [System.Serializable]
    public class UserList
    {
        public List<UserData> Users;
        
        public UserList()
        {
            Users = new List<UserData>();
        }
    }
}
