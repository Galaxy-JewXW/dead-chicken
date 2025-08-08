using UnityEngine;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace PowerlineSystem
{
    /// <summary>
    /// Python依赖自动安装器
    /// 帮助用户自动安装LAS转换所需的Python依赖
    /// </summary>
    public class PythonDependencyInstaller : MonoBehaviour
    {
        [Header("安装配置")]
        [Tooltip("是否在Start时自动检查依赖")]
        public bool autoCheckOnStart = true;
        
        [Tooltip("是否自动安装缺失的依赖")]
        public bool autoInstallMissing = false;
        
        [Header("依赖列表")]
        [Tooltip("需要安装的Python包")]
        public string[] requiredPackages = { "laspy", "numpy", "open3d", "scipy", "scikit-learn", "tqdm" };
        
        [Header("状态显示")]
        [SerializeField] private string currentStatus = "等待检查";
        [SerializeField] private bool isInstalling = false;
        
        // 事件
        public System.Action<string> OnStatusChanged;
        public System.Action<bool> OnInstallationComplete;
        
        void Start()
        {
            if (autoCheckOnStart)
            {
                CheckAndInstallDependencies();
            }
        }
        
        /// <summary>
        /// 检查并安装依赖
        /// </summary>
        public void CheckAndInstallDependencies()
        {
            StartCoroutine(CheckAndInstallCoroutine());
        }
        
        System.Collections.IEnumerator CheckAndInstallCoroutine()
        {
            UpdateStatus("🔍 检查Python环境...");
            
            // 检查Python是否可用
            if (!CheckPythonAvailable())
            {
                UpdateStatus("❌ Python未安装或不在PATH中");
                OnInstallationComplete?.Invoke(false);
                yield break;
            }
            
            UpdateStatus("✅ Python环境正常");
            yield return new WaitForSeconds(0.5f);
            
            // 检查每个依赖包
            bool allDependenciesInstalled = true;
            foreach (string package in requiredPackages)
            {
                UpdateStatus($"🔍 检查 {package}...");
                
                if (!CheckPackageInstalled(package))
                {
                    allDependenciesInstalled = false;
                    UpdateStatus($"❌ {package} 未安装");
                    
                    if (autoInstallMissing)
                    {
                        UpdateStatus($"📦 正在安装 {package}...");
                        isInstalling = true;
                        
                        if (InstallPackage(package))
                        {
                            UpdateStatus($"✅ {package} 安装成功");
                        }
                        else
                        {
                            UpdateStatus($"❌ {package} 安装失败");
                            OnInstallationComplete?.Invoke(false);
                            yield break;
                        }
                        
                        isInstalling = false;
                        yield return new WaitForSeconds(1f);
                    }
                }
                else
                {
                    UpdateStatus($"✅ {package} 已安装");
                }
                
                yield return new WaitForSeconds(0.3f);
            }
            
            if (allDependenciesInstalled || (autoInstallMissing && !isInstalling))
            {
                UpdateStatus("✅ 所有依赖检查完成");
                OnInstallationComplete?.Invoke(true);
            }
            else
            {
                UpdateStatus("⚠️ 部分依赖缺失，请手动安装");
                OnInstallationComplete?.Invoke(false);
            }
        }
        
        /// <summary>
        /// 检查Python是否可用
        /// </summary>
        private bool CheckPythonAvailable()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "python",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    if (process.ExitCode == 0)
                    {
                        string version = output.Trim();
                        Debug.Log($"Python版本: {version}");
                        
                        // 检查Python版本是否为3.11
                        if (!version.Contains("3.11"))
                        {
                            Debug.LogError($"需要Python 3.11版本，当前版本: {version}");
                            return false;
                        }
                        
                        return true;
                    }
                    
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"检查Python失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 检查包是否已安装
        /// </summary>
        private bool CheckPackageInstalled(string packageName)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "python",
                    Arguments = $"-c \"import {packageName}; print('{packageName} available')\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    return process.ExitCode == 0 && output.Contains($"{packageName} available");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"检查包 {packageName} 失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 安装Python包
        /// </summary>
        private bool InstallPackage(string packageName)
        {
            try
            {
                Debug.Log($"开始安装 {packageName}...");
                
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "pip",
                    Arguments = $"install {packageName}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    Debug.Log($"pip install {packageName} 输出: {output}");
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogWarning($"pip install {packageName} 错误: {error}");
                    }
                    
                    return process.ExitCode == 0;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"安装 {packageName} 失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 更新状态
        /// </summary>
        private void UpdateStatus(string status)
        {
            currentStatus = status;
            Debug.Log($"[PythonDependencyInstaller] {status}");
            OnStatusChanged?.Invoke(status);
        }
        
        /// <summary>
        /// 手动安装所有依赖
        /// </summary>
        [ContextMenu("手动安装所有依赖")]
        public void InstallAllDependencies()
        {
            autoInstallMissing = true;
            CheckAndInstallDependencies();
        }
        
        /// <summary>
        /// 检查依赖状态
        /// </summary>
        [ContextMenu("检查依赖状态")]
        public void CheckDependencyStatus()
        {
            autoInstallMissing = false;
            CheckAndInstallDependencies();
        }
        
        /// <summary>
        /// 获取当前状态
        /// </summary>
        public string GetCurrentStatus()
        {
            return currentStatus;
        }
        
        /// <summary>
        /// 是否正在安装
        /// </summary>
        public bool IsInstalling()
        {
            return isInstalling;
        }
    }
} 