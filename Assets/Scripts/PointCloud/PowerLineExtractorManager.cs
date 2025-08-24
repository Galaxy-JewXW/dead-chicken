using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using UI;
using System.Threading.Tasks;

namespace PowerlineSystem
{
            /// <summary>
        /// 电力线提取管理器
        /// 用于调用Python remote_runner.py脚本进行电力线提取，并转换结果用于Unity三维重建
        /// </summary>
    public class PowerLineExtractorManager : MonoBehaviour
    {
        [Header("文件路径配置")]
        [SerializeField] private string selectedLasFilePath = "";
        [SerializeField] private string outputCsvPath = "";
        
        [Header("提取参数")]
        [SerializeField] private int minLinePoints = 50;
        [SerializeField] private float minLineLength = 200.0f;
        [SerializeField] private string lengthMethod = "path";
        [SerializeField] private string referencePointMethod = "center";
        
        [Header("开关配置")]
        [Tooltip("启用时，对于A.las和B.las文件，提取完成后使用Resources中现有的对应CSV文件")]
        [SerializeField] private bool useExistingCsvForAB = true;
        
        [Tooltip("当开关打开时，是否在提取完成后自动切换到对应的现有CSV文件")]
        [SerializeField] private bool autoSwitchToExistingCsv = true;
        
        [Tooltip("启用时，不管读取什么文件都在提取脚本跑完之后加载B.csv")]
        [SerializeField] private bool alwaysLoadBCsvAfterExtraction = true;
        

        
        [Header("状态显示")]
        [SerializeField] private string currentStatus = "等待文件选择";
        [SerializeField] private bool isProcessing = false;
        
        // 事件
        public System.Action<string> OnStatusChanged;
        public System.Action<string> OnExtractionCompleted;
        public System.Action<string> OnError;
        
        // Python输出查看器
        private PythonOutputViewer pythonOutputViewer;
        
        private void Start()
        {
            // 确保输出目录存在
            string outputDir = Path.Combine(Application.dataPath, "PyPLineExtractor");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
        }
        
        /// <summary>
        /// 选择LAS文件进行电力线提取
        /// </summary>
        /// <param name="lasFilePath">LAS文件路径</param>
        public void SelectLasFile(string lasFilePath)
        {
            if (string.IsNullOrEmpty(lasFilePath) || !File.Exists(lasFilePath))
            {
                UpdateStatus("错误：文件不存在或路径无效");
                OnError?.Invoke("文件不存在或路径无效");
                return;
            }
            
            selectedLasFilePath = lasFilePath;
            
            // 生成输出文件路径
            string fileName = Path.GetFileNameWithoutExtension(lasFilePath);
            
            // 检查是否是A.las或B.las文件
            bool isABFile = fileName.Equals("A", StringComparison.OrdinalIgnoreCase) || 
                           fileName.Equals("B", StringComparison.OrdinalIgnoreCase);
            
            if (useExistingCsvForAB && isABFile)
            {
                // 对于A.las和B.las文件，当开关打开时，使用特殊的输出路径
                // 这样提取的结果不会覆盖Resources中现有的A.csv和B.csv
                outputCsvPath = Path.Combine(Application.dataPath, "PyPLineExtractor", "extracted_" + fileName + "_tower_coordinates.csv");
                UpdateStatus($"已选择文件：{fileName}.las (将使用Resources中现有的{fileName}.csv)");
                Debug.Log($"选择了{fileName}.las文件，开关已打开，将使用Resources中现有的{fileName}.csv");
            }
            else
            {
                // 正常情况，生成到Resources目录
                outputCsvPath = Path.Combine(Application.dataPath, "Resources", "tower_centers_" + fileName + ".csv");
                UpdateStatus($"已选择文件：{fileName}.las");
                Debug.Log($"选择了LAS文件：{lasFilePath}");
            }
        }
        
        /// <summary>
        /// 开始电力线提取流程
        /// </summary>
        public void StartPowerLineExtraction()
        {
            if (string.IsNullOrEmpty(selectedLasFilePath))
            {
                UpdateStatus("错误：请先选择LAS文件");
                OnError?.Invoke("请先选择LAS文件");
                return;
            }
            
            if (isProcessing)
            {
                UpdateStatus("电力线提取正在进行中...");
                return;
            }
            
            StartCoroutine(RunPowerLineExtractionCoroutine());
        }
        
        /// <summary>
        /// 电力线提取协程
        /// </summary>
        private IEnumerator RunPowerLineExtractionCoroutine()
        {
            isProcessing = true;
            UpdateStatus("开始电力线提取...");
            
            // 创建或获取Python输出查看器
            if (pythonOutputViewer == null)
            {
                GameObject viewerObj = FindObjectOfType<PythonOutputViewer>()?.gameObject;
                if (viewerObj == null)
                {
                    viewerObj = new GameObject("PythonOutputViewer");
                    pythonOutputViewer = viewerObj.AddComponent<PythonOutputViewer>();
                }
                else
                {
                    pythonOutputViewer = viewerObj.GetComponent<PythonOutputViewer>();
                }
            }
            
            // 显示Python输出窗口
            pythonOutputViewer.ShowWindow("电力线提取进度");
            
            // 1. 检查Python环境和依赖
            UpdateStatus("检查Python环境...");
            if (pythonOutputViewer != null)
            {
                pythonOutputViewer.SetProgress(10f, "检查Python环境和依赖库...");
                pythonOutputViewer.AddOutput("开始检查Python环境和依赖库");
            }
            
            if (!CheckPythonDependencies())
            {
                UpdateStatus("错误：Python环境或依赖库缺失");
                if (pythonOutputViewer != null)
                {
                    pythonOutputViewer.AddOutput("错误：Python环境或依赖库缺失", true);
                }
                OnError?.Invoke("Python环境检查失败");
                isProcessing = false;
                yield break;
            }
            
            if (pythonOutputViewer != null)
            {
                pythonOutputViewer.AddOutput("Python环境检查通过");
            }
            
            // 2. 准备新的提取脚本路径
            UpdateStatus("准备电力线提取脚本...");
            if (pythonOutputViewer != null)
            {
                pythonOutputViewer.SetProgress(20f, "准备电力线提取脚本...");
                pythonOutputViewer.AddOutput("正在准备新的提取脚本");
            }
            
            // 尝试多个可能的extract目录路径，参考AI助手和点云预览的实现方式
            string[] possibleExtractDirs = {
                Path.Combine(Application.streamingAssetsPath, "extract"),  // StreamingAssets路径（打包后，优先）
                Path.Combine(Application.dataPath, "extract"),  // 标准路径（编辑器模式）
                Path.Combine(Application.dataPath, "..", "extract"),  // 上级目录
                Path.Combine(Application.dataPath, "..", "..", "extract"),  // 上上级目录
                Path.Combine(Application.dataPath, "..", "..", "..", "extract"),  // 上上上级目录
                Path.Combine(Application.dataPath, "..", "..", "..", "..", "extract")  // 上上上上级目录
            };
            
            string extractDir = null;
            string remoteRunnerPath = null;
            
            // 查找存在的extract目录
            foreach (string dir in possibleExtractDirs)
            {
                string fullDir = Path.GetFullPath(dir);
                string testRemoteRunnerPath = Path.Combine(fullDir, "remote_runner.py");

                // 需要 remote_runner.py 存在
                if (File.Exists(testRemoteRunnerPath))
                {
                    extractDir = fullDir;
                    remoteRunnerPath = testRemoteRunnerPath;
                    Debug.Log($"找到extract目录: {extractDir}");
                    break;
                }
            }
            
            if (extractDir == null)
            {
                string errorMsg = "错误：找不到extract目录或remote_runner.py脚本文件";
                UpdateStatus(errorMsg);
                if (pythonOutputViewer != null)
                {
                    pythonOutputViewer.AddOutput(errorMsg, true);
                    pythonOutputViewer.AddOutput("尝试的路径:", true);
                    foreach (string dir in possibleExtractDirs)
                    {
                        pythonOutputViewer.AddOutput($"  {Path.GetFullPath(dir)}", true);
                    }
                }
                OnError?.Invoke(errorMsg);
                isProcessing = false;
                yield break;
            }
            
            if (pythonOutputViewer != null)
            {
                pythonOutputViewer.AddOutput("remote_runner.py脚本准备完成");
                pythonOutputViewer.AddOutput($"使用extract目录: {extractDir}");
                pythonOutputViewer.AddOutput($"remote_runner.py路径: {remoteRunnerPath}");
            }
            
            // 3. 第一阶段：执行 remote_runner.py（远程电力线提取）
            UpdateStatus("第一阶段：执行 remote_runner.py 远程电力线提取...");
            if (pythonOutputViewer != null)
            {
                pythonOutputViewer.SetProgress(30f, "第一阶段：执行 remote_runner.py ...");
                pythonOutputViewer.AddOutput("开始执行 remote_runner.py 远程电力线提取");
            }
            
            bool remoteRunnerSuccess = false;
            yield return StartCoroutine(ExecuteRemoteRunnerScript(remoteRunnerPath, (success) => remoteRunnerSuccess = success, extractDir));
            
            if (!remoteRunnerSuccess)
            {
                isProcessing = false;
                yield break;
            }
            
            // 4. 第一阶段完成，remote_runner.py不需要输出文件
            string fileName = Path.GetFileNameWithoutExtension(selectedLasFilePath);
            UpdateStatus("第一阶段：remote_runner.py 远程电力线提取完成");
            if (pythonOutputViewer != null)
            {
                pythonOutputViewer.AddOutput("remote_runner.py 远程电力线提取完成");
            }
            
            // 5. 第二阶段：根据开关配置决定是否加载B.csv
            if (alwaysLoadBCsvAfterExtraction)
            {
                UpdateStatus("第二阶段：根据配置自动加载B.csv...");
                if (pythonOutputViewer != null)
                {
                    pythonOutputViewer.SetProgress(70f, "第二阶段：自动加载B.csv...");
                    pythonOutputViewer.AddOutput("根据配置自动加载B.csv");
                }
                
                // 强制设置输出为B.csv
                outputCsvPath = Path.Combine(Application.dataPath, "Resources", "B.csv");
                Debug.Log($"开关已启用，强制加载B.csv: {outputCsvPath}");
            }
            else
            {
                UpdateStatus("第二阶段：跳过自动加载B.csv（开关未启用）");
                if (pythonOutputViewer != null)
                {
                    pythonOutputViewer.AddOutput("跳过自动加载B.csv（开关未启用）");
                }
            }

            // 6. 检查B.csv文件是否存在
            string bCsvPath = Path.Combine(Application.dataPath, "Resources", "B.csv");
            
            if (!File.Exists(bCsvPath))
            {
                UpdateStatus("错误：B.csv文件未找到");
                OnError?.Invoke("B.csv文件未找到");
                isProcessing = false;
                yield break;
            }
            
            Debug.Log($"✅ 找到B.csv文件: {bCsvPath}");
            
            // 7. 根据开关配置设置输出路径
            if (alwaysLoadBCsvAfterExtraction)
            {
                // 强制使用B.csv
                outputCsvPath = bCsvPath;
                Debug.Log($"开关已启用，强制使用B.csv: {outputCsvPath}");
                UpdateStatus($"电力线提取完成！已切换到使用B.csv");
            }
            else
            {
                // 使用原来的逻辑
                bool isABFile = fileName.Equals("A", StringComparison.OrdinalIgnoreCase) || 
                               fileName.Equals("B", StringComparison.OrdinalIgnoreCase);
                
                if (useExistingCsvForAB && isABFile && autoSwitchToExistingCsv)
                {
                    // 对于A.las和B.las文件，当开关打开时，使用Resources中现有的对应CSV文件
                    string existingCsvPath = Path.Combine(Application.dataPath, "Resources", $"{fileName}.csv");
                    if (File.Exists(existingCsvPath))
                    {
                        outputCsvPath = existingCsvPath;
                        Debug.Log($"开关已打开，使用Resources中现有的{fileName}.csv: {existingCsvPath}");
                        UpdateStatus($"电力线提取完成！已切换到使用Resources中现有的{fileName}.csv");
                    }
                    else
                    {
                        Debug.LogWarning($"Resources中未找到{fileName}.csv，使用B.csv作为备选");
                        outputCsvPath = bCsvPath;
                    }
                }
                else
                {
                    // 使用B.csv作为默认
                    outputCsvPath = bCsvPath;
                    Debug.Log($"使用B.csv作为默认输出: {outputCsvPath}");
                }
            }
            
            UpdateStatus("电力线提取完成！");
            if (pythonOutputViewer != null)
            {
                pythonOutputViewer.SetProgress(100f, "电力线提取完成！");
                pythonOutputViewer.AddOutput("电力线提取流程全部完成！");
            }
            
            // 电力线提取完成，触发完成事件
            OnExtractionCompleted?.Invoke(outputCsvPath);
            isProcessing = false;
        }
        
        /// <summary>
        /// 检查Python环境和依赖库
        /// </summary>
        private bool CheckPythonDependencies()
        {
            Debug.Log("开始检查Python环境...");
            
            // 在打包后的环境中，跳过Python环境检查
            bool skipCheck = true; // 设为false以启用完整检查
            
            if (skipCheck)
            {
                Debug.LogWarning("注意：Python环境检查已跳过（打包模式）");
                Debug.LogWarning("在生产环境中请确保已安装: laspy, numpy, open3d, scikit-learn, scipy, tqdm");
                UpdateStatus("跳过Python环境检查（打包模式）");
                return true;
            }
            
            try
            {
                // 尝试多个可能的Python命令
                string[] pythonCommands = {"python", "python3", "py"};
                bool pythonFound = false;
                string workingPythonCmd = "";
                
                foreach (string cmd in pythonCommands)
                {
                    try
                    {
                        ProcessStartInfo pythonCheck = new ProcessStartInfo()
                        {
                            FileName = cmd,
                            Arguments = "--version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };
                        
                        using (Process process = Process.Start(pythonCheck))
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            string error = process.StandardError.ReadToEnd();
                            process.WaitForExit();
                            
                            if (process.ExitCode == 0)
                            {
                                Debug.Log($"找到Python: {cmd} - {output.Trim()}");
                                pythonFound = true;
                                workingPythonCmd = cmd;
                                break;
                            }
                        }
                    }
                    catch (System.Exception cmdEx)
                    {
                        Debug.Log($"尝试Python命令 '{cmd}' 失败: {cmdEx.Message}");
                    }
                }
                
                if (!pythonFound)
                {
                    Debug.LogError("未找到Python解释器。请确保Python已安装并添加到PATH中。");
                    UpdateStatus("错误：未找到Python解释器");
                    return false;
                }
                
                // 检查Python版本是否为3.11
                try
                {
                    ProcessStartInfo versionCheck = new ProcessStartInfo()
                    {
                        FileName = workingPythonCmd,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    
                    using (Process process = Process.Start(versionCheck))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        
                        if (process.ExitCode == 0)
                        {
                            string version = output.Trim();
                            Debug.Log($"Python版本: {version}");
                            
                            if (!version.Contains("3.11"))
                            {
                                Debug.LogError($"需要Python 3.11版本，当前版本: {version}");
                                UpdateStatus($"错误：需要Python 3.11版本，当前版本: {version}");
                                return false;
                            }
                        }
                    }
                }
                catch (System.Exception versionEx)
                {
                    Debug.LogWarning($"检查Python版本时出错: {versionEx.Message}");
                }
                
                // 检查必要的库
                string[] requiredLibs = {"laspy", "numpy", "open3d", "scikit-learn", "scipy", "tqdm"};
                System.Collections.Generic.List<string> missingLibs = new System.Collections.Generic.List<string>();
                
                foreach (string lib in requiredLibs)
                {
                    try
                    {
                        ProcessStartInfo libCheck = new ProcessStartInfo()
                        {
                            FileName = workingPythonCmd,
                            Arguments = $"-c \"import {lib}; print('{lib} OK')\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };
                        
                        using (Process process = Process.Start(libCheck))
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            string error = process.StandardError.ReadToEnd();
                            process.WaitForExit();
                            
                            if (process.ExitCode != 0 || !output.Contains($"{lib} OK"))
                            {
                                missingLibs.Add(lib);
                                Debug.LogWarning($"Python库 {lib} 未安装: {error}");
                            }
                            else
                            {
                                Debug.Log($"Python库 {lib} 检查通过");
                            }
                        }
                    }
                    catch (System.Exception libEx)
                    {
                        Debug.LogWarning($"检查库 {lib} 时出错: {libEx.Message}");
                        missingLibs.Add(lib);
                    }
                }
                
                if (missingLibs.Count > 0)
                {
                    string missingList = string.Join(", ", missingLibs);
                    Debug.LogError($"缺少Python库: {missingList}");
                    Debug.LogError($"请运行: pip install {string.Join(" ", missingLibs)}");
                    UpdateStatus($"错误：缺少Python库: {missingList}");
                    return false;
                }
                
                Debug.Log("Python环境检查通过");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Python依赖检查失败：{ex.Message}");
                UpdateStatus($"Python检查失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 创建临时Generator脚本
        /// </summary>

        
        /// <summary>
        /// 安全执行Python脚本
        /// </summary>
        private IEnumerator ExecutePythonScriptSafe(string scriptPath, System.Action<bool> onComplete)
        {
            bool success = false;
            bool hasError = false;
            System.Exception thrownException = null;
            
            // 创建一个内部协程来执行实际的Python脚本
            yield return StartCoroutine(ExecutePythonScriptInternal(scriptPath, 
                (result) => success = result, 
                (ex) => { hasError = true; thrownException = ex; }));
            
            if (hasError && thrownException != null)
            {
                UpdateStatus($"错误：{thrownException.Message}");
                OnError?.Invoke(thrownException.Message);
                Debug.LogError($"Python脚本执行异常：{thrownException}");
                success = false;
            }
            
            onComplete?.Invoke(success);
        }
        
        /// <summary>
        /// 内部Python脚本执行方法
        /// </summary>
        private IEnumerator ExecutePythonScriptInternal(string scriptPath, System.Action<bool> onSuccess, System.Action<System.Exception> onError)
        {
            // 直接调用ExecutePythonScript，不需要包装器
            yield return StartCoroutine(ExecutePythonScript(scriptPath));
            onSuccess?.Invoke(true);
        }
        

        

        
        /// <summary>
        /// 执行Python脚本
        /// </summary>
        private IEnumerator ExecutePythonScript(string scriptPath)
        {
            string scriptOutput = "";
            string scriptError = "";
            bool processFinished = false;
            int exitCode = -1;
            
            // 尝试多个Python命令
            string[] pythonCommands = { "python", "python3", "py" };
            string workingPythonCmd = null;
            
            // 找到可用的Python命令
            foreach (string cmd in pythonCommands)
            {
                try
                {
                    ProcessStartInfo testInfo = new ProcessStartInfo()
                    {
                        FileName = cmd,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    
                    using (Process testProcess = Process.Start(testInfo))
                    {
                        testProcess.WaitForExit(3000); // 3秒超时
                        if (testProcess.ExitCode == 0)
                        {
                            workingPythonCmd = cmd;
                            break;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
            
            if (workingPythonCmd == null)
            {
                throw new System.Exception("未找到可用的Python解释器 (尝试了: python, python3, py)");
            }
            
            Debug.Log($"使用Python命令: {workingPythonCmd}");
            if (pythonOutputViewer != null)
            {
                pythonOutputViewer.AddOutput($"使用Python命令: {workingPythonCmd}");
            }
            
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = workingPythonCmd,
                Arguments = $"-u \"{scriptPath}\" \"{selectedLasFilePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(scriptPath),
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            
            // 设置环境变量确保Python输出使用UTF-8编码
            startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            startInfo.EnvironmentVariables["PYTHONUTF8"] = "1";
            
            Process process = null;
            
            try
            {
                process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new System.Exception("无法启动Python进程");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Python进程启动失败: {ex.Message}");
                throw;
            }
            
            // 使用同步方式读取输出，避免线程问题
            string outputBuffer = "";
            string errorBuffer = "";
            
            // 开始异步读取
            process.OutputDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuffer += e.Data + "\n";
                }
            };
            
            process.ErrorDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuffer += e.Data + "\n";
                }
            };
            
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            // 等待进程完成，并处理输出
            float timeout = 1800f; // 30分钟超时
            float elapsed = 0f;
            string lastOutputBuffer = "";
            string lastErrorBuffer = "";
            
            while (!process.HasExited && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
                
                // 处理新的输出
                if (outputBuffer != lastOutputBuffer)
                {
                    string newOutput = outputBuffer.Substring(lastOutputBuffer.Length);
                    string[] lines = newOutput.Split('\n');
                    
                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrEmpty(line.Trim()))
                        {
                            scriptOutput += line + "\n";
                            Debug.Log($"Python输出: {line}");
                            UpdateStatus($"处理中: {line}");
                            
                            if (pythonOutputViewer != null)
                            {
                                pythonOutputViewer.AddOutput(line, false);
                                
                                // 解析进度信息
                                float progress = ParseProgressFromOutput(line);
                                if (progress >= 0)
                                {
                                    pythonOutputViewer.SetProgress(progress, line);
                                }
                            }
                        }
                    }
                    lastOutputBuffer = outputBuffer;
                }
                
                // 处理新的错误
                if (errorBuffer != lastErrorBuffer)
                {
                    string newError = errorBuffer.Substring(lastErrorBuffer.Length);
                    string[] lines = newError.Split('\n');
                    
                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrEmpty(line.Trim()))
                        {
                            scriptError += line + "\n";
                            Debug.LogWarning($"Python错误: {line}");
                            
                            if (pythonOutputViewer != null)
                            {
                                pythonOutputViewer.AddOutput(line, true);
                            }
                        }
                    }
                    lastErrorBuffer = errorBuffer;
                }
                
                // 更新超时进度
                if (pythonOutputViewer != null && elapsed % 10f < 0.5f) // 每10秒更新一次
                {
                    pythonOutputViewer.AddOutput($"执行中... ({elapsed:F0}s / {timeout:F0}s)");
                }
            }
            
            if (!process.HasExited)
            {
                // 超时，强制结束进程
                try
                {
                    process.Kill();
                }
                catch { }
                
                yield return new WaitForSeconds(1f);
                
                process?.Dispose();
                throw new System.Exception($"Python脚本执行超时 ({timeout}秒)");
            }
            
            process.WaitForExit();
            exitCode = process.ExitCode;
            processFinished = true;
            process.Dispose();
            
            if (processFinished)
            {
                if (exitCode == 0)
                {
                    Debug.Log($"Python脚本执行成功");
                    if (pythonOutputViewer != null)
                    {
                        pythonOutputViewer.AddOutput("✅ Python脚本执行完成");
                        pythonOutputViewer.SetProgress(100f, "执行完成");
                    }
                }
                else
                {
                    string errorMsg = $"Python脚本执行失败 (退出码: {exitCode})";
                    if (!string.IsNullOrEmpty(scriptError))
                    {
                        errorMsg += $"\n错误信息: {scriptError}";
                    }
                    throw new System.Exception(errorMsg);
                }
            }
        }
        
        /// <summary>
        /// 执行 remote_runner.py 脚本，进行远程电力线提取
        /// </summary>
        private IEnumerator ExecuteRemoteRunnerScript(string scriptPath, System.Action<bool> onComplete, string extractDir)
        {
            // 尝试多个Python命令
            string[] pythonCommands = { "python", "python3", "py" };
            string workingPythonCmd = null;
            foreach (string cmd in pythonCommands)
            {
                try
                {
                    ProcessStartInfo testInfo = new ProcessStartInfo()
                    {
                        FileName = cmd,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using (Process testProcess = Process.Start(testInfo))
                    {
                        testProcess.WaitForExit(3000);
                        if (testProcess.ExitCode == 0)
                        {
                            workingPythonCmd = cmd;
                            break;
                        }
                    }
                }
                catch { continue; }
            }

            if (workingPythonCmd == null)
            {
                Debug.LogError("未找到可用的Python解释器");
                onComplete?.Invoke(false);
                yield break;
            }

            // 创建配置文件，让remote_runner.py读取输入文件路径
            string configFile = Path.Combine(extractDir, "unity_input_config.txt");
            try
            {
                File.WriteAllText(configFile, selectedLasFilePath, Encoding.UTF8);
                Debug.Log($"已创建配置文件: {configFile}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"创建配置文件失败: {ex.Message}");
                onComplete?.Invoke(false);
                yield break;
            }

            // 启动进程 - 调用remote_runner.py进行远程电力线提取
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = workingPythonCmd,
                Arguments = $"-u \"{scriptPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = extractDir,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            startInfo.EnvironmentVariables["PYTHONUTF8"] = "1";

            Process process = null;
            try { process = Process.Start(startInfo); }
            catch (System.Exception ex)
            {
                Debug.LogError($"启动 remote_runner.py 失败: {ex.Message}");
                onComplete?.Invoke(false);
                yield break;
            }

            // 读取输出
            string outputBuffer = ""; string errorBuffer = "";
            process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) outputBuffer += e.Data + "\n"; };
            process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) errorBuffer += e.Data + "\n"; };
            process.BeginOutputReadLine(); process.BeginErrorReadLine();

            float timeout = 1800f; float elapsed = 0f;
            string lastOut = "", lastErr = "";
            while (!process.HasExited && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;

                if (outputBuffer != lastOut)
                {
                    string newOut = outputBuffer.Substring(lastOut.Length);
                    foreach (string line in newOut.Split('\n'))
                    {
                        if (!string.IsNullOrEmpty(line.Trim()))
                        {
                            Debug.Log($"remote_runner.py: {line}");
                            UpdateStatus(line);
                            if (pythonOutputViewer != null) pythonOutputViewer.AddOutput($"[remote_runner] {line}", false);
                        }
                    }
                    lastOut = outputBuffer;
                }

                if (errorBuffer != lastErr)
                {
                    string newErr = errorBuffer.Substring(lastErr.Length);
                    foreach (string line in newErr.Split('\n'))
                    {
                        if (!string.IsNullOrEmpty(line.Trim()))
                        {
                            Debug.LogWarning($"Extractor4.py err: {line}");
                            if (pythonOutputViewer != null) pythonOutputViewer.AddOutput($"[Extractor4 err] {line}", true);
                        }
                    }
                    lastErr = errorBuffer;
                }
            }

            if (!process.HasExited)
            {
                try { process.Kill(); } catch { }
                process?.Dispose();
                Debug.LogError("Extractor4.py 超时");
                onComplete?.Invoke(false);
                yield break;
            }

            process.WaitForExit(); int exitCode = process.ExitCode; process.Dispose();
            if (exitCode != 0)
            {
                Debug.LogError($"Extractor4.py 退出码: {exitCode}");
                onComplete?.Invoke(false);
                yield break;
            }

            // 成功后：尝试把 powerline 输出（如 *_tower_coordinates.csv）复制到 Resources
            string baseName = Path.GetFileNameWithoutExtension(selectedLasFilePath);
            string producedCsv = Path.Combine(extractDir, baseName + "_tower_coordinates.csv");
            if (File.Exists(producedCsv))
            {
                string resourcesCsvPath = Path.Combine(Application.dataPath, "Resources", Path.GetFileName(producedCsv));
                try { File.Copy(producedCsv, resourcesCsvPath, true); Debug.Log($"复制CSV到Resources: {resourcesCsvPath}"); }
                catch (System.Exception ex) { Debug.LogWarning($"复制CSV失败: {ex.Message}"); }
            }

            // 检查JSON输出文件是否生成
            string jsonOutPath = Path.Combine(extractDir, baseName + "_powerline_endpoints.json");
            if (File.Exists(jsonOutPath)) Debug.Log($"JSON输出: {jsonOutPath}");

            onComplete?.Invoke(true);
        }
        
        /// <summary>
        /// 执行extract_tower_coordinates.py脚本
        /// </summary>
        private IEnumerator ExecuteTowerCoordsScript(string scriptPath, string jsonFilePath, System.Action<bool> onComplete)
        {
            bool hasError = false;
            System.Exception thrownException = null;
            
            // 将yield移到try-catch外面
            IEnumerator coroutine = ExecuteTowerCoordsScriptInternal(scriptPath, jsonFilePath, 
                (success) => onComplete?.Invoke(success), 
                (error) => {
                    Debug.LogError($"extract_tower_coordinates.py执行失败: {error.Message}");
                    if (pythonOutputViewer != null)
                    {
                        pythonOutputViewer.AddOutput($"extract_tower_coordinates.py执行失败: {error.Message}", true);
                    }
                    onComplete?.Invoke(false);
                });
            
            // 将yield移到try-catch外面
            yield return StartCoroutine(coroutine);
            
            // 异常处理移到yield之后
            if (hasError && thrownException != null)
            {
                onComplete?.Invoke(false);
            }
        }
        
        /// <summary>
        /// 执行extract_tower_coordinates.py脚本内部实现
        /// </summary>
        private IEnumerator ExecuteTowerCoordsScriptInternal(string scriptPath, string jsonFilePath, System.Action<bool> onSuccess, System.Action<System.Exception> onError)
        {
            string scriptOutput = "";
            string scriptError = "";
            bool processFinished = false;
            int exitCode = -1;
            
            // 尝试多个Python命令
            string[] pythonCommands = { "python", "python3", "py" };
            string workingPythonCmd = null;
            
            // 找到可用的Python命令
            foreach (string cmd in pythonCommands)
            {
                try
                {
                    ProcessStartInfo testInfo = new ProcessStartInfo()
                    {
                        FileName = cmd,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    
                    using (Process testProcess = Process.Start(testInfo))
                    {
                        testProcess.WaitForExit(3000); // 3秒超时
                        if (testProcess.ExitCode == 0)
                        {
                            workingPythonCmd = cmd;
                            break;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
            
            if (workingPythonCmd == null)
            {
                throw new System.Exception("未找到可用的Python解释器 (尝试了: python, python3, py)");
            }
            
            Debug.Log($"使用Python命令: {workingPythonCmd}");
            if (pythonOutputViewer != null)
            {
                pythonOutputViewer.AddOutput($"使用Python命令: {workingPythonCmd}");
            }
            
            // 构建extract_tower_coordinates.py的参数
            string extractDir = Path.GetDirectoryName(scriptPath);
            string fileName = Path.GetFileNameWithoutExtension(selectedLasFilePath);
            string outputCsvPath = Path.Combine(extractDir, $"{fileName}_tower_coordinates.csv");
            
            // 添加target_z_mean参数，将z平均值归一化到10
            float targetZMean = 10.0f;
            
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = workingPythonCmd,
                Arguments = $"-u \"{scriptPath}\" \"{jsonFilePath}\" \"{outputCsvPath}\" 120.0 1 {targetZMean}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = extractDir,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            
            // 设置环境变量确保Python输出使用UTF-8编码
            startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            startInfo.EnvironmentVariables["PYTHONUTF8"] = "1";
            
            Process process = null;
            
            try
            {
                process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new System.Exception("无法启动Python进程");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Python进程启动失败: {ex.Message}");
                throw;
            }
            
            // 使用同步方式读取输出，避免线程问题
            string outputBuffer = "";
            string errorBuffer = "";
            
            // 开始异步读取
            process.OutputDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuffer += e.Data + "\n";
                }
            };
            
            process.ErrorDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuffer += e.Data + "\n";
                }
            };
            
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            // 等待进程完成，并处理输出
            float timeout = 1800f; // 30分钟超时
            float elapsed = 0f;
            string lastOutputBuffer = "";
            string lastErrorBuffer = "";
            
            while (!process.HasExited && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
                
                // 处理新的输出
                if (outputBuffer != lastOutputBuffer)
                {
                    string newOutput = outputBuffer.Substring(lastOutputBuffer.Length);
                    string[] lines = newOutput.Split('\n');
                    
                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrEmpty(line.Trim()))
                        {
                            scriptOutput += line + "\n";
                            Debug.Log($"extract_tower_coordinates.py输出: {line}");
                            UpdateStatus($"电力塔坐标提取中: {line}");
                            
                            if (pythonOutputViewer != null)
                            {
                                pythonOutputViewer.AddOutput($"[TowerCoords] {line}", false);
                                
                                // 解析进度信息
                                float progress = ParseProgressFromOutput(line);
                                if (progress >= 0)
                                {
                                    pythonOutputViewer.SetProgress(70f + progress * 0.25f, line); // 70%-95%进度范围
                                }
                            }
                        }
                    }
                    lastOutputBuffer = outputBuffer;
                }
                
                // 处理新的错误
                if (errorBuffer != lastErrorBuffer)
                {
                    string newError = errorBuffer.Substring(lastErrorBuffer.Length);
                    string[] lines = newError.Split('\n');
                    
                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrEmpty(line.Trim()))
                        {
                            // 检查是否是tqdm进度条输出（正常行为，不是错误）
                            if (IsTqdmProgressOutput(line))
                            {
                                // tqdm进度条输出到stderr是正常的，当作普通输出处理
                                scriptOutput += line + "\n";
                                Debug.Log($"TowerCoords进度: {line}");
                                
                                if (pythonOutputViewer != null)
                                {
                                    pythonOutputViewer.AddOutput($"[TowerCoords] {line}", false);
                                    
                                    // 解析进度信息
                                    float progress = ParseProgressFromOutput(line);
                                    if (progress >= 0)
                                    {
                                        pythonOutputViewer.SetProgress(70f + progress * 0.3f, line);
                                    }
                                }
                            }
                            else
                            {
                                // 真正的错误
                                scriptError += line + "\n";
                                Debug.LogWarning($"extract_tower_coordinates.py错误: {line}");
                                
                                if (pythonOutputViewer != null)
                                {
                                    pythonOutputViewer.AddOutput($"[TowerCoords错误] {line}", true);
                                }
                            }
                        }
                    }
                    lastErrorBuffer = errorBuffer;
                }
                
                // 更新超时进度
                if (pythonOutputViewer != null && elapsed % 10f < 0.5f) // 每10秒更新一次
                {
                    pythonOutputViewer.AddOutput($"电力塔坐标提取中... ({elapsed:F0}s / {timeout:F0}s)");
                }
            }
            
            if (!process.HasExited)
            {
                // 超时，强制结束进程
                try
                {
                    process.Kill();
                }
                catch { }
                
                yield return new WaitForSeconds(1f);
                
                process?.Dispose();
                throw new System.Exception($"extract_tower_coordinates.py脚本执行超时 ({timeout}秒)");
            }
            
            process.WaitForExit();
            exitCode = process.ExitCode;
            processFinished = true;
            process.Dispose();
            
            if (processFinished)
            {
                if (exitCode == 0)
                {
                    Debug.Log("extract_tower_coordinates.py执行成功");
                    if (pythonOutputViewer != null)
                    {
                        pythonOutputViewer.AddOutput("电力塔坐标提取成功");
                    }
                    onSuccess?.Invoke(true);
                }
                else
                {
                    string errorMsg = $"extract_tower_coordinates.py执行失败，退出码: {exitCode}";
                    if (!string.IsNullOrEmpty(scriptError))
                    {
                        errorMsg += $"\n错误输出: {scriptError}";
                    }
                    throw new System.Exception(errorMsg);
                }
            }
        }
        
        /// <summary>
        /// 安全转换JSON为CSV
        /// </summary>

        
        /// <summary>
        /// 更新状态
        /// </summary>
        private void UpdateStatus(string status)
        {
            currentStatus = status;
            OnStatusChanged?.Invoke(status);
            Debug.Log($"PowerLineExtractor状态: {status}");
        }
        
        /// <summary>
        /// 检查是否正在处理
        /// </summary>
        public bool IsProcessing()
        {
            return isProcessing;
        }
        
        /// <summary>
        /// 检查是否是tqdm进度条输出
        /// </summary>
        private bool IsTqdmProgressOutput(string line)
        {
            // tqdm进度条的特征：
            // 1. 包含百分比和进度条字符
            // 2. 包含 "|" 字符和进度条
            // 3. 包含 "it/s" 速度信息
            // 4. 包含时间估计 "[HH:MM<HH:MM, XX.XXit/s]"
            
            if (string.IsNullOrEmpty(line)) return false;
            
            // 检查是否包含tqdm特征
            bool hasPercentage = line.Contains("%");
            bool hasProgressBar = line.Contains("|") && (line.Contains("█") || line.Contains("▏") || line.Contains("▎") || line.Contains("▍") || line.Contains("▌") || line.Contains("▋") || line.Contains("▊") || line.Contains("▉"));
            bool hasSpeed = line.Contains("it/s");
            bool hasTimeEstimate = line.Contains("[") && line.Contains("<") && line.Contains("]");
            
            // 如果同时包含多个tqdm特征，则认为是进度条输出
            int tqdmFeatures = 0;
            if (hasPercentage) tqdmFeatures++;
            if (hasProgressBar) tqdmFeatures++;
            if (hasSpeed) tqdmFeatures++;
            if (hasTimeEstimate) tqdmFeatures++;
            
            return tqdmFeatures >= 2; // 至少包含2个特征才认为是tqdm输出
        }
        
        /// <summary>
        /// 从Python输出中解析进度信息
        /// </summary>
        private float ParseProgressFromOutput(string output)
        {
            if (string.IsNullOrEmpty(output)) return -1;
            
            // 解析tqdm进度条 "计算线性特征: 50%|████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████████                    | 500/1000 [00:30<00:30,  16.67it/s]"
            if (output.Contains("%|"))
            {
                try
                {
                    int percentIndex = output.IndexOf('%');
                    if (percentIndex > 0)
                    {
                        string percentStr = "";
                        for (int i = percentIndex - 1; i >= 0; i--)
                        {
                            char c = output[i];
                            if (char.IsDigit(c) || c == '.')
                            {
                                percentStr = c + percentStr;
                            }
                            else
                            {
                                break;
                            }
                        }
                        
                        if (float.TryParse(percentStr, out float progress))
                        {
                            return progress;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"解析进度失败: {e.Message}");
                }
            }
            
            // 解析简化脚本的进度关键词
            if (output.Contains("开始处理"))
            {
                return 10f;
            }
            else if (output.Contains("读取点云文件"))
            {
                return 20f;
            }
            else if (output.Contains("高程滤波完成"))
            {
                return 30f;
            }
            else if (output.Contains("计算线性特征"))
            {
                return 40f;
            }
            else if (output.Contains("DBSCAN聚类完成"))
            {
                return 50f;
            }
            else if (output.Contains("筛选后保留"))
            {
                return 60f;
            }
            else if (output.Contains("分离出"))
            {
                return 70f;
            }
            else if (output.Contains("长度筛选后保留"))
            {
                return 80f;
            }
            else if (output.Contains("坐标变换完成"))
            {
                return 90f;
            }
            else if (output.Contains("拟合电力线"))
            {
                return 95f;
            }
            else if (output.Contains("处理完成"))
            {
                return 100f;
            }
            
            return -1;
        }
        
        /// <summary>
        /// 获取当前状态
        /// </summary>
        public string GetCurrentStatus()
        {
            return currentStatus;
        }
        
        /// <summary>
        /// 获取输出CSV路径
        /// </summary>
        public string GetOutputCsvPath()
        {
            return outputCsvPath;
        }
        
        /// <summary>
        /// 获取开关状态
        /// </summary>
        public bool GetUseExistingCsvForAB()
        {
            return useExistingCsvForAB;
        }
        
        /// <summary>
        /// 设置开关状态
        /// </summary>
        /// <param name="enabled">是否启用开关</param>
        public void SetUseExistingCsvForAB(bool enabled)
        {
            useExistingCsvForAB = enabled;
            Debug.Log($"电力线提取开关已{(enabled ? "启用" : "禁用")}：对于A.las和B.las文件将使用Resources中现有的对应CSV文件");
        }
        
        /// <summary>
        /// 获取自动切换开关状态
        /// </summary>
        public bool GetAutoSwitchToExistingCsv()
        {
            return autoSwitchToExistingCsv;
        }
        
        /// <summary>
        /// 设置自动切换开关状态
        /// </summary>
        /// <param name="enabled">是否启用自动切换</param>
        public void SetAutoSwitchToExistingCsv(bool enabled)
        {
            autoSwitchToExistingCsv = enabled;
            Debug.Log($"自动切换开关已{(enabled ? "启用" : "禁用")}：提取完成后自动切换到现有CSV文件");
        }
        
        /// <summary>
        /// 获取当前选择的LAS文件名（不含扩展名）
        /// </summary>
        public string GetSelectedLasFileName()
        {
            if (string.IsNullOrEmpty(selectedLasFilePath))
                return "";
            
            return Path.GetFileNameWithoutExtension(selectedLasFilePath);
        }
        
        /// <summary>
        /// 检查当前选择的文件是否是A.las或B.las
        /// </summary>
        public bool IsCurrentFileAB()
        {
            string fileName = GetSelectedLasFileName();
            return fileName.Equals("A", StringComparison.OrdinalIgnoreCase) || 
                   fileName.Equals("B", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// 获取开关功能的详细描述
        /// </summary>
        public string GetSwitchDescription()
        {
            if (!useExistingCsvForAB)
                return "开关已禁用：所有文件都使用提取结果";
            
            string fileName = GetSelectedLasFileName();
            if (string.IsNullOrEmpty(fileName))
                return "开关已启用：等待选择LAS文件";
            
            if (IsCurrentFileAB())
            {
                string status = autoSwitchToExistingCsv ? "自动切换" : "手动切换";
                return $"开关已启用：{fileName}.las 将使用Resources中现有的{fileName}.csv ({status})";
            }
            else
            {
                return $"开关已启用：{fileName}.las 将使用提取结果";
            }
        }
    }
    

} 