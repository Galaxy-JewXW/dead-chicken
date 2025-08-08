using UnityEngine;

namespace UI
{
    /// <summary>
    /// 天空盒恢复器
    /// 用于解决点云系统可能导致的天空变黑问题
    /// </summary>
    public class SkyboxRestorer : MonoBehaviour
    {
        [Header("天空盒恢复设置")]
        [Tooltip("是否在Start时自动恢复天空盒")]
        public bool autoRestoreOnStart = true;
        
        [Tooltip("默认天空盒材质")]
        public Material defaultSkybox;
        
        [Tooltip("检查间隔（秒）")]
        public float checkInterval = 1f;
        
        private float lastCheckTime;
        
        void Start()
        {
            if (autoRestoreOnStart)
            {
                RestoreSkybox();
            }
        }
        
        void Update()
        {
            // 定期检查天空盒设置
            if (Time.time - lastCheckTime > checkInterval)
            {
                CheckAndRestoreSkybox();
                lastCheckTime = Time.time;
            }
        }
        
        /// <summary>
        /// 恢复天空盒设置
        /// </summary>
        [ContextMenu("恢复天空盒")]
        public void RestoreSkybox()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // 设置相机清除标志为天空盒
                mainCamera.clearFlags = CameraClearFlags.Skybox;
                
                // 设置天空盒材质
                if (defaultSkybox != null)
                {
                    RenderSettings.skybox = defaultSkybox;
                }
                else
                {
                    // 尝试查找默认天空盒
                    Material skybox = FindDefaultSkybox();
                    if (skybox != null)
                    {
                        RenderSettings.skybox = skybox;
                        defaultSkybox = skybox; // 保存引用
                    }
                }
                
                // 强制刷新天空盒
                DynamicGI.UpdateEnvironment();
                
                Debug.Log("天空盒已恢复");
            }
        }
        
        /// <summary>
        /// 检查并恢复天空盒
        /// </summary>
        void CheckAndRestoreSkybox()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // 检查是否需要恢复天空盒
                if (mainCamera.clearFlags != CameraClearFlags.Skybox || RenderSettings.skybox == null)
                {
                    RestoreSkybox();
                }
            }
        }
        
        /// <summary>
        /// 查找默认天空盒材质
        /// </summary>
        Material FindDefaultSkybox()
        {
            // 尝试多种可能的默认天空盒路径
            string[] possiblePaths = {
                "Default-Skybox",
                "Skybox/Default-Skybox",
                "Materials/Default-Skybox",
                "Skybox/Procedural",
                "Default-Material"
            };
            
            foreach (string path in possiblePaths)
            {
                Material skybox = Resources.Load<Material>(path);
                if (skybox != null)
                {
                    Debug.Log($"找到默认天空盒: {path}");
                    return skybox;
                }
            }
            
            // 如果都没找到，创建一个简单的程序化天空盒
            Material proceduralSkybox = new Material(Shader.Find("Skybox/Procedural"));
            if (proceduralSkybox != null)
            {
                Debug.Log("创建程序化天空盒");
                return proceduralSkybox;
            }
            
            Debug.LogWarning("未找到合适的天空盒材质");
            return null;
        }
        
        /// <summary>
        /// 设置自定义天空盒
        /// </summary>
        public void SetCustomSkybox(Material skybox)
        {
            if (skybox != null)
            {
                defaultSkybox = skybox;
                RenderSettings.skybox = skybox;
                
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    mainCamera.clearFlags = CameraClearFlags.Skybox;
                }
                
                DynamicGI.UpdateEnvironment();
                Debug.Log($"设置自定义天空盒: {skybox.name}");
            }
        }
        
        /// <summary>
        /// 强制刷新天空盒
        /// </summary>
        [ContextMenu("强制刷新天空盒")]
        public void ForceRefreshSkybox()
        {
            DynamicGI.UpdateEnvironment();
            Debug.Log("天空盒已强制刷新");
        }
    }
} 