#if UNITY_STANDALONE
#define IMPORT_GLENABLE
#endif

using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace PowerlineSystem
{
    /// <summary>
    /// 电力线点云点大小启用器
    /// 基于Unity-Point-Cloud-Free-Viewer的EnablePointSize.cs
    /// 用于在支持的平台上启用OpenGL点大小功能
    /// </summary>
    public class PowerlinePointSizeEnabler : MonoBehaviour 
    {
        
        const UInt32 GL_VERTEX_PROGRAM_POINT_SIZE = 0x8642;
        const UInt32 GL_POINT_SMOOTH = 0x0B10;
        
        const string LibGLPath =
            #if UNITY_STANDALONE_WIN
            "opengl32.dll";
        #elif UNITY_STANDALONE_OSX
        "/System/Library/Frameworks/OpenGL.framework/OpenGL";
        #elif UNITY_STANDALONE_LINUX
        "libGL";  // 在Linux上未测试，可能不正确
        #else
        null;   // OpenGL ES平台不需要此功能
        #endif
        
        #if IMPORT_GLENABLE
        [DllImport(LibGLPath)]
        public static extern void glEnable(UInt32 cap);
        
        private bool mIsOpenGL;
        
        [Header("点云设置")]
        [Tooltip("是否启用点大小控制")]
        public bool enablePointSize = true;
        
        [Tooltip("是否启用点平滑")]
        public bool enablePointSmooth = true;
        
        [Tooltip("是否在每帧都调用（某些驱动可能需要）")]
        public bool enableEveryFrame = false;
        
        private bool hasInitialized = false;
        
        void Start()
        {
            // 检测图形API
            mIsOpenGL = SystemInfo.graphicsDeviceVersion.Contains("OpenGL");
            
            if (mIsOpenGL)
            {
                Debug.Log("检测到OpenGL图形API，启用点云渲染优化");
            }
            else
            {
                Debug.Log($"当前图形API: {SystemInfo.graphicsDeviceVersion}");
                Debug.Log("非OpenGL环境，点云渲染可能受限");
            }
            
            hasInitialized = true;
        }
        
        void OnPreRender()
        {
            if (!hasInitialized || !enablePointSize) return;
            
            try
            {
                if (mIsOpenGL && enablePointSize)
                {
                    glEnable(GL_VERTEX_PROGRAM_POINT_SIZE);
                }
                
                if (enablePointSmooth)
                {
                    glEnable(GL_POINT_SMOOTH);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"启用OpenGL点功能时出错: {e.Message}");
            }
        }
        
        void Update()
        {
            // 某些驱动程序可能需要每帧都调用
            if (enableEveryFrame && hasInitialized)
            {
                OnPreRender();
            }
        }
        
        /// <summary>
        /// 手动启用点大小功能
        /// </summary>
        public void EnablePointSizeManually()
        {
            if (!hasInitialized) Start();
            OnPreRender();
        }
        
        /// <summary>
        /// 获取OpenGL支持状态
        /// </summary>
        public bool IsOpenGLSupported()
        {
            return mIsOpenGL;
        }
        
        #else
        
        // 非支持平台的空实现
        void Start()
        {
            Debug.Log("当前平台不支持OpenGL点大小功能");
        }
        
        public void EnablePointSizeManually()
        {
            // 空实现
        }
        
        public bool IsOpenGLSupported()
        {
            return false;
        }
        
        #endif
        
        /// <summary>
        /// 创建点云相机的静态方法
        /// </summary>
        public static void SetupPointCloudCamera(Camera camera)
        {
            if (camera == null) return;
            
            // 添加点大小启用组件
            var enabler = camera.GetComponent<PowerlinePointSizeEnabler>();
            if (enabler == null)
            {
                enabler = camera.gameObject.AddComponent<PowerlinePointSizeEnabler>();
            }
            
            Debug.Log($"已为相机 {camera.name} 设置点云渲染支持");
        }
        
        /// <summary>
        /// 为点云专用相机设置渲染参数
        /// </summary>
        public static void SetupDedicatedPointCloudCamera(Camera camera)
        {
            if (camera == null) return;
            
            // 添加点大小启用组件
            var enabler = camera.GetComponent<PowerlinePointSizeEnabler>();
            if (enabler == null)
            {
                enabler = camera.gameObject.AddComponent<PowerlinePointSizeEnabler>();
            }
            
            // 为专用点云相机设置渲染参数
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            
            Debug.Log($"已为专用点云相机 {camera.name} 设置渲染参数");
        }
        
        void OnDestroy()
        {
            // 清理资源
            hasInitialized = false;
        }
    }
} 