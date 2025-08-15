using UnityEngine;
using UnityEngine.UIElements;

namespace UserAuthDebug
{
    public class LoginDebugger : MonoBehaviour
    {
        [Header("调试设置")]
        [SerializeField] private bool enableDebug = true;
        
        private InitialInterfaceManager interfaceManager;
        
        void Start()
        {
            if (!enableDebug) return;
            
            interfaceManager = FindObjectOfType<InitialInterfaceManager>();
            if (interfaceManager != null)
            {
                Debug.Log("找到InitialInterfaceManager");
                InvokeRepeating("CheckLoginInterface", 1f, 2f);
            }
            else
            {
                Debug.LogError("未找到InitialInterfaceManager");
            }
        }
        
        void CheckLoginInterface()
        {
            if (interfaceManager == null) return;
            
            // 通过反射获取私有字段
            var type = typeof(InitialInterfaceManager);
            var loginPanelField = type.GetField("loginPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var registerPanelField = type.GetField("registerPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var initialPanelField = type.GetField("initialPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (loginPanelField != null)
            {
                var loginPanel = loginPanelField.GetValue(interfaceManager) as VisualElement;
                Debug.Log($"登录面板状态: {(loginPanel != null ? "已创建" : "未创建")}");
                if (loginPanel != null)
                {
                    Debug.Log($"登录面板显示状态: {loginPanel.style.display.value}");
                    Debug.Log($"登录面板可见性: {loginPanel.visible}");
                }
            }
            
            if (registerPanelField != null)
            {
                var registerPanel = registerPanelField.GetValue(interfaceManager) as VisualElement;
                Debug.Log($"注册面板状态: {(registerPanel != null ? "已创建" : "未创建")}");
            }
            
            if (initialPanelField != null)
            {
                var initialPanel = initialPanelField.GetValue(interfaceManager) as VisualElement;
                Debug.Log($"主界面面板状态: {(initialPanel != null ? "已创建" : "未创建")}");
            }
        }
        
        [ContextMenu("强制显示登录界面")]
        void ForceShowLogin()
        {
            if (interfaceManager == null) return;
            
            var type = typeof(InitialInterfaceManager);
            var method = type.GetMethod("ShowLoginInterface", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(interfaceManager, null);
                Debug.Log("强制显示登录界面");
            }
        }
    }
}
