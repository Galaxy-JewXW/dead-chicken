using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace PowerlineSystem
{
    /// <summary>
    /// LAS到OFF格式转换器
    /// 增强版本 - 包含详细的错误诊断和依赖检查
    /// </summary>
    public static class LasToOffConverter
    {
        /// <summary>
        /// 转换LAS文件到OFF格式
        /// </summary>
        /// <param name="lasFilePath">LAS文件路径</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>转换后的OFF文件路径，失败返回null</returns>
        public static string ConvertLasToOff(string lasFilePath, string outputDirectory = null)
        {
            // 1. 基础验证
            if (string.IsNullOrEmpty(lasFilePath))
            {
                UnityEngine.Debug.LogError("LAS文件路径为空");
                return null;
            }

            if (!File.Exists(lasFilePath))
            {
                UnityEngine.Debug.LogError($"LAS文件不存在: {lasFilePath}");
                return null;
            }

            // 2. 检查文件大小
            var fileInfo = new FileInfo(lasFilePath);
            if (fileInfo.Length == 0)
            {
                UnityEngine.Debug.LogError("LAS文件为空");
                return null;
            }

            UnityEngine.Debug.Log($"LAS文件信息: {fileInfo.Name}, 大小: {fileInfo.Length / 1024}KB");

            // 3. 检查依赖
            if (!CheckDependencies())
            {
                UnityEngine.Debug.LogError("Python环境或laspy依赖检查失败，请检查安装");
                return null;
            }

            // 4. 设置输出目录
            if (string.IsNullOrEmpty(outputDirectory))
            {
                outputDirectory = Path.Combine(Application.dataPath, "Resources", "pointcloud");
            }

            // 确保输出目录存在
            if (!Directory.Exists(outputDirectory))
            {
                try
                {
                    Directory.CreateDirectory(outputDirectory);
                    UnityEngine.Debug.Log($"创建输出目录: {outputDirectory}");
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"创建输出目录失败: {ex.Message}");
                    return null;
                }
            }

            // 5. 生成输出文件名
            string fileName = Path.GetFileNameWithoutExtension(lasFilePath);
            string outputFilePath = Path.Combine(outputDirectory, fileName + ".off");

            // 6. 获取las2off脚本路径
            string las2offScriptPath = FindLas2offScript();
            
            if (string.IsNullOrEmpty(las2offScriptPath))
            {
                UnityEngine.Debug.LogError("找不到las2off转换脚本");
                return null;
            }

            string tempLasPath = null;
            try
            {
                UnityEngine.Debug.Log($"开始转换LAS文件: {lasFilePath} -> {outputFilePath}");

                // 7. 复制LAS文件到las2off目录
                string las2offDir = Path.GetDirectoryName(las2offScriptPath);
                tempLasPath = Path.Combine(las2offDir, Path.GetFileName(lasFilePath));
                
                // 如果目标文件已存在且被占用，先尝试删除
                if (File.Exists(tempLasPath))
                {
                    try
                    {
                        File.Delete(tempLasPath);
                        UnityEngine.Debug.Log($"已删除旧的临时文件: {tempLasPath}");
                    }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"删除旧临时文件失败: {ex.Message}");
                        // 生成唯一的临时文件名
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(lasFilePath);
                        string extension = Path.GetExtension(lasFilePath);
                        tempLasPath = Path.Combine(las2offDir, $"{fileNameWithoutExt}_{System.DateTime.Now.Ticks}{extension}");
                        UnityEngine.Debug.Log($"使用新的临时文件名: {tempLasPath}");
                    }
                }
                
                try
                {
                    // 使用FileStream确保文件完全写入
                    using (var sourceStream = new FileStream(lasFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var destStream = new FileStream(tempLasPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        sourceStream.CopyTo(destStream);
                    }
                    UnityEngine.Debug.Log($"已复制LAS文件到临时位置: {tempLasPath}");
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"复制LAS文件失败: {ex.Message}");
                    UnityEngine.Debug.LogError($"源文件: {lasFilePath}");
                    UnityEngine.Debug.LogError($"目标文件: {tempLasPath}");
                    
                    // 提供具体的解决方案建议
                    if (ex.Message.Contains("Access") || ex.Message.Contains("denied"))
                    {
                        UnityEngine.Debug.LogError("💡 解决方案: 文件可能被其他程序占用，请关闭相关程序后重试");
                        UnityEngine.Debug.LogError("💡 或者尝试以管理员身份运行Unity");
                    }
                    else if (ex.Message.Contains("NotFound"))
                    {
                        UnityEngine.Debug.LogError("💡 解决方案: 源文件不存在或路径错误");
                    }
                    
                    return null;
                }

                // 8. 修改las2off.py脚本中的文件名
                ModifyLas2offScript(las2offScriptPath, Path.GetFileName(lasFilePath), fileName + ".off");

                // 9. 执行Python转换脚本
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "python",
                    Arguments = $"-u \"{las2offScriptPath}\"", // -u 确保输出不缓冲
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = las2offDir,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                
                // 设置环境变量确保Python使用UTF-8编码
                startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
                startInfo.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";

                UnityEngine.Debug.Log($"执行Python脚本: {startInfo.FileName} {startInfo.Arguments}");
                UnityEngine.Debug.Log($"工作目录: {startInfo.WorkingDirectory}");

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    // 记录Python脚本的输出
                    if (!string.IsNullOrEmpty(output))
                    {
                        UnityEngine.Debug.Log($"Python脚本输出: {output}");
                    }
                    if (!string.IsNullOrEmpty(error))
                    {
                        UnityEngine.Debug.LogWarning($"Python脚本错误输出: {error}");
                    }

                    UnityEngine.Debug.Log($"Python脚本退出码: {process.ExitCode}");

                    if (process.ExitCode == 0)
                    {
                        UnityEngine.Debug.Log($"LAS转换成功完成");
                        
                        // 10. 移动生成的OFF文件到目标位置
                        string generatedOffPath = Path.Combine(las2offDir, fileName + ".off");
                        if (File.Exists(generatedOffPath))
                        {
                            try
                            {
                                // 如果目标文件已存在，先删除
                                if (File.Exists(outputFilePath))
                                {
                                    File.Delete(outputFilePath);
                                }
                                File.Move(generatedOffPath, outputFilePath);
                                UnityEngine.Debug.Log($"已移动OFF文件到目标位置: {outputFilePath}");
                            }
                            catch (System.Exception ex)
                            {
                                UnityEngine.Debug.LogError($"移动OFF文件失败: {ex.Message}");
                                return null;
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.LogError($"Python脚本未生成OFF文件: {generatedOffPath}");
                            return null;
                        }

                        // 11. 清理临时文件
                        try
                        {
                            if (File.Exists(tempLasPath))
                            {
                                // 等待一小段时间确保文件不再被使用
                                System.Threading.Thread.Sleep(100);
                                File.Delete(tempLasPath);
                                UnityEngine.Debug.Log("已清理临时LAS文件");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            UnityEngine.Debug.LogWarning($"清理临时文件失败: {ex.Message}");
                            // 不阻止转换成功，只是警告
                        }

                        // 12. 提示Unity刷新资源系统
                        #if UNITY_EDITOR
                        UnityEditor.AssetDatabase.Refresh();
                        UnityEngine.Debug.Log("💡 Unity资源系统已刷新");
                        #endif

                        // 13. 验证输出文件
                        if (File.Exists(outputFilePath))
                        {
                            var outputFileInfo = new FileInfo(outputFilePath);
                            UnityEngine.Debug.Log($"✅ OFF文件创建成功: {outputFilePath}");
                            UnityEngine.Debug.Log($"文件大小: {outputFileInfo.Length / 1024}KB");
                            return outputFilePath;
                        }
                        else
                        {
                            UnityEngine.Debug.LogError($"❌ 转换完成但未找到输出文件: {outputFilePath}");
                            return null;
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"❌ LAS转换失败，退出码: {process.ExitCode}");
                        UnityEngine.Debug.LogError($"Python输出: {output}");
                        UnityEngine.Debug.LogError($"Python错误: {error}");
                        
                        // 提供更详细的错误诊断
                        if (error.Contains("laspy") || output.Contains("laspy"))
                        {
                            UnityEngine.Debug.LogError("💡 解决方案: 请运行 'pip install laspy' 安装laspy库");
                        }
                        else if (error.Contains("numpy") || output.Contains("numpy"))
                        {
                            UnityEngine.Debug.LogError("💡 解决方案: 请运行 'pip install numpy' 安装numpy库");
                        }
                        else if (error.Contains("Permission") || error.Contains("permission"))
                        {
                            UnityEngine.Debug.LogError("💡 解决方案: 请检查文件权限，或尝试以管理员身份运行");
                        }
                        else if (error.Contains("FileNotFound") || error.Contains("No such file") || output.Contains("不存在"))
                        {
                            UnityEngine.Debug.LogError("💡 解决方案: 检查LAS文件是否存在且可读");
                        }
                        else if (error.Contains("ImportError") || error.Contains("ModuleNotFoundError"))
                        {
                            UnityEngine.Debug.LogError("💡 解决方案: Python依赖库缺失，请运行 'pip install laspy numpy open3d scipy scikit-learn tqdm'");
                        }
                        else
                        {
                            UnityEngine.Debug.LogError("💡 通用解决方案: 请检查Python环境和依赖库安装");
                        }
                        
                        return null;
                    }
                }
                
                // 清理临时文件（无论成功还是失败）
                if (!string.IsNullOrEmpty(tempLasPath))
                {
                    try
                    {
                        if (File.Exists(tempLasPath))
                        {
                            System.Threading.Thread.Sleep(100);
                            File.Delete(tempLasPath);
                            UnityEngine.Debug.Log("已清理临时LAS文件");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"清理临时文件失败: {ex.Message}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ LAS转换异常: {ex.Message}");
                UnityEngine.Debug.LogError($"异常堆栈: {ex.StackTrace}");
                
                // 清理临时文件
                if (!string.IsNullOrEmpty(tempLasPath))
                {
                    try
                    {
                        if (File.Exists(tempLasPath))
                        {
                            System.Threading.Thread.Sleep(100);
                            File.Delete(tempLasPath);
                            UnityEngine.Debug.Log("已清理临时LAS文件");
                        }
                    }
                    catch (System.Exception cleanupEx)
                    {
                        UnityEngine.Debug.LogWarning($"清理临时文件失败: {cleanupEx.Message}");
                    }
                }
                
                return null;
            }
        }

        /// <summary>
        /// 修改las2off.py脚本中的文件名
        /// </summary>
        /// <param name="scriptPath">脚本路径</param>
        /// <param name="inputFileName">输入文件名</param>
        /// <param name="outputFileName">输出文件名</param>
        private static void ModifyLas2offScript(string scriptPath, string inputFileName, string outputFileName)
        {
            try
            {
                // 读取原始脚本内容
                string scriptContent = File.ReadAllText(scriptPath, Encoding.UTF8);
                
                // 替换文件名 - 使用正则表达式匹配任何引号内的文件名
                scriptContent = System.Text.RegularExpressions.Regex.Replace(
                    scriptContent, 
                    @"INPUT_FILE\s*=\s*""[^""]*""", 
                    $"INPUT_FILE = \"{inputFileName}\""
                );
                scriptContent = System.Text.RegularExpressions.Regex.Replace(
                    scriptContent, 
                    @"OUTPUT_FILE\s*=\s*""[^""]*""", 
                    $"OUTPUT_FILE = \"{outputFileName}\""
                );
                
                // 写回文件
                File.WriteAllText(scriptPath, scriptContent, Encoding.UTF8);
                
                UnityEngine.Debug.Log($"✅ 已修改las2off脚本: {inputFileName} -> {outputFileName}");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ 修改las2off脚本失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查Python和laspy依赖是否可用
        /// </summary>
        /// <returns>依赖检查结果</returns>
        public static bool CheckDependencies()
        {
            try
            {
                UnityEngine.Debug.Log("🔍 检查Python环境...");
                
                // 检查Python是否可用
                ProcessStartInfo pythonCheck = new ProcessStartInfo()
                {
                    FileName = "python",
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
                    
                    if (process.ExitCode != 0)
                    {
                        UnityEngine.Debug.LogError($"❌ Python未安装或不在PATH中");
                        UnityEngine.Debug.LogError($"错误信息: {error}");
                        return false;
                    }
                    
                    string version = output.Trim();
                    UnityEngine.Debug.Log($"✅ Python版本: {version}");
                    
                    // 检查Python版本是否为3.11
                    if (!version.Contains("3.11"))
                    {
                        UnityEngine.Debug.LogError($"❌ 需要Python 3.11版本，当前版本: {version}");
                        UnityEngine.Debug.LogError("💡 解决方案: 请安装Python 3.11版本");
                        return false;
                    }
                }

                UnityEngine.Debug.Log("🔍 检查laspy库...");
                
                // 检查laspy是否可用
                ProcessStartInfo laspyCheck = new ProcessStartInfo()
                {
                    FileName = "python",
                    Arguments = "-c \"import laspy; print('laspy available')\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(laspyCheck))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0 || !output.Contains("laspy available"))
                    {
                        UnityEngine.Debug.LogError($"❌ laspy库未安装");
                        UnityEngine.Debug.LogError($"错误信息: {error}");
                        UnityEngine.Debug.LogError("💡 解决方案: 请运行 'pip install laspy' 安装laspy库");
                        return false;
                    }
                    
                    UnityEngine.Debug.Log("✅ laspy库可用");
                }

                UnityEngine.Debug.Log("🔍 检查numpy库...");
                
                // 检查numpy是否可用
                ProcessStartInfo numpyCheck = new ProcessStartInfo()
                {
                    FileName = "python",
                    Arguments = "-c \"import numpy; print('numpy available')\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(numpyCheck))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0 || !output.Contains("numpy available"))
                    {
                        UnityEngine.Debug.LogError($"❌ numpy库未安装");
                        UnityEngine.Debug.LogError($"错误信息: {error}");
                        UnityEngine.Debug.LogError("💡 解决方案: 请运行 'pip install numpy' 安装numpy库");
                        return false;
                    }
                    
                    UnityEngine.Debug.Log("✅ numpy库可用");
                }

                UnityEngine.Debug.Log("🔍 检查open3d库...");
                
                // 检查open3d是否可用
                ProcessStartInfo open3dCheck = new ProcessStartInfo()
                {
                    FileName = "python",
                    Arguments = "-c \"import open3d; print('open3d available')\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(open3dCheck))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0 || !output.Contains("open3d available"))
                    {
                        UnityEngine.Debug.LogError($"❌ open3d库未安装");
                        UnityEngine.Debug.LogError($"错误信息: {error}");
                        UnityEngine.Debug.LogError("💡 解决方案: 请运行 'pip install open3d' 安装open3d库");
                        return false;
                    }
                    
                    UnityEngine.Debug.Log("✅ open3d库可用");
                }

                UnityEngine.Debug.Log("✅ 所有依赖检查通过");
                return true;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ 依赖检查异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 查找las2off脚本路径
        /// </summary>
        /// <returns>找到的脚本路径，未找到返回null</returns>
        private static string FindLas2offScript()
        {
            // 尝试多个可能的las2off脚本路径，参考电力线提取的实现方式
            string[] possibleLas2offPaths = {
                Path.Combine(Application.dataPath, "las2off", "las2off.py"),  // 标准路径（编辑器模式）
                Path.Combine(Application.streamingAssetsPath, "extract", "las2off.py"),  // StreamingAssets路径（打包后）
                Path.Combine(Application.dataPath, "..", "las2off", "las2off.py"),  // 上级目录
                Path.Combine(Application.dataPath, "..", "..", "las2off", "las2off.py"),  // 上上级目录
                Path.Combine(Application.dataPath, "..", "..", "..", "las2off", "las2off.py"),  // 上上上级目录
                Path.Combine(Application.dataPath, "..", "..", "..", "..", "las2off", "las2off.py")  // 上上上上级目录
            };
            
            // 查找存在的las2off脚本文件
            foreach (string scriptPath in possibleLas2offPaths)
            {
                string fullPath = Path.GetFullPath(scriptPath);
                if (File.Exists(fullPath))
                {
                    UnityEngine.Debug.Log($"找到las2off脚本: {fullPath}");
                    return fullPath;
                }
            }
            
            // 如果没找到，输出调试信息
            UnityEngine.Debug.LogError("未找到las2off脚本，尝试的路径:");
            foreach (string scriptPath in possibleLas2offPaths)
            {
                UnityEngine.Debug.LogError($"  {Path.GetFullPath(scriptPath)}");
            }
            
            return null;
        }

        /// <summary>
        /// 安装Python依赖
        /// </summary>
        /// <returns>安装结果</returns>
        public static bool InstallDependencies()
        {
            try
            {
                UnityEngine.Debug.Log("📦 开始安装Python依赖...");
                
                // 安装laspy
                ProcessStartInfo laspyInstall = new ProcessStartInfo()
                {
                    FileName = "pip",
                    Arguments = "install laspy",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(laspyInstall))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    UnityEngine.Debug.Log($"pip install laspy 输出: {output}");
                    if (!string.IsNullOrEmpty(error))
                    {
                        UnityEngine.Debug.LogWarning($"pip install laspy 错误: {error}");
                    }
                }

                UnityEngine.Debug.Log("✅ 依赖安装完成");
                return true;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"❌ 安装依赖失败: {ex.Message}");
                return false;
            }
        }
    }
} 