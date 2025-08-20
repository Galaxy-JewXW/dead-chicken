using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Diagnostics;
using System.IO;

using Debug = UnityEngine.Debug;

[System.Serializable]
public class ChatMessage
{
    public string role;
    public string content;
}

[System.Serializable]
public class ChatRequest
{
    public string model;
    public List<ChatMessage> messages;
    public float temperature;
    public int max_tokens;
}

[System.Serializable]
public class ChatResponse
{
    public string id;
    public string object_type;
    public long created;
    public string model;
    public List<Choice> choices;
    public Usage usage;
}

[System.Serializable]
public class Choice
{
    public int index;
    public Message message;
    public string finish_reason;
}

[System.Serializable]
public class Message
{
    public string role;
    public string content;
}

[System.Serializable]
public class Usage
{
    public int prompt_tokens;
    public int completion_tokens;
    public int total_tokens;
}

[System.Serializable]
public class PythonResponse
{
    public bool success;
    public string response;
    public string error;
    public Usage usage;
    public string model;
    public string id;
}

public class AIAPIManager : MonoBehaviour
{
    [Header("API配置")]
    [SerializeField] private string apiKey = "cfed8c512417402983a28e3ceee6bfe1.vdzks2lqATOYjgUy";
    [SerializeField] private string apiUrl = "https://open.bigmodel.cn/api/paas/v4/chat/completions";
    [SerializeField] private string model = "glm-4.5";
    [SerializeField] private float temperature = 0.7f;
    [SerializeField] private int maxTokens = 1000;

    // 智谱AI API认证配置
    private string GetAuthorizationHeader()
    {
        // 根据官方文档，直接使用API Key作为Bearer token
        // 注意：智谱AI的API Key格式应该是 "Bearer {apiKey}"
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("[AIAPI] API密钥为空");
            return null;
        }

        // 检查API密钥格式
        if (!apiKey.Contains("."))
        {
            Debug.LogWarning("[AIAPI] API密钥格式可能不正确，智谱AI的API密钥通常包含点号");
        }

        return $"Bearer {apiKey}";
    }
    
    private List<ChatMessage> conversationHistory = new List<ChatMessage>();
    
    public static AIAPIManager Instance { get; private set; }
    
    private void Awake()
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
    
    private void Start()
    {
        Debug.Log("[AIAPI] AIAPIManager 初始化完成");
        Debug.Log($"[AIAPI] 使用模型: {model}");
        Debug.Log($"[AIAPI] API密钥: {apiKey}");
        Debug.Log($"[AIAPI] API URL: {apiUrl}");
        
        // 验证API密钥格式
        if (ValidateAPIKey())
        {
            Debug.Log($"[AIAPI] API密钥格式正确: {apiKey.Substring(0, Math.Min(8, apiKey.Length))}...");
        }
        else
        {
            Debug.LogError("[AIAPI] API密钥格式错误，请检查配置");
        }
    }
    
    private bool ValidateAPIKey()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("[AIAPI] API密钥为空");
            return false;
        }
        
        if (apiKey.Length < 10)
        {
            Debug.LogError("[AIAPI] API密钥长度过短，请检查是否正确");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 发送消息到AI API（通过Python脚本）
    /// </summary>
    public void SendMessage(string userMessage, Action<string> onResponse, Action<string> onError = null)
    {
        if (string.IsNullOrEmpty(userMessage))
        {
            onError?.Invoke("消息不能为空");
            return;
        }
        
        if (!ValidateAPIKey())
        {
            onError?.Invoke("API密钥格式错误，请检查配置");
            return;
        }

        Debug.Log($"[AIAPI] 发送消息: {userMessage}");

        // 添加用户消息到历史记录
        conversationHistory.Add(new ChatMessage
        {
            role = "user",
            content = userMessage
        });

        // 通过Python脚本发送请求
        StartCoroutine(SendRequestViaPython(userMessage, onResponse, onError));
    }
    
    /// <summary>
    /// 通过Python脚本发送请求（使用文件读写方式，更可靠）
    /// </summary>
    private IEnumerator SendRequestViaPython(string userMessage, Action<string> onResponse, Action<string> onError)
    {
        Debug.Log("[AIAPI] 开始通过Python脚本发送请求（文件读写模式）...");
        
        // 获取Python脚本路径
        string pythonScriptPath = Path.Combine(Application.dataPath, "Scripts", "AI", "ai_api_handler.py");
        
        if (!File.Exists(pythonScriptPath))
        {
            string errorMsg = $"Python脚本不存在: {pythonScriptPath}";
            Debug.LogError($"[AIAPI] {errorMsg}");
            onError?.Invoke(errorMsg);
            yield break;
        }
        
        // 创建临时文件路径
        string tempDir = Path.Combine(Application.temporaryCachePath, "AIAPI");
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }
        
        string tempFileName = $"ai_response_{DateTime.Now:yyyyMMdd_HHmmss}_{UnityEngine.Random.Range(1000, 9999)}.json";
        string tempFilePath = Path.Combine(tempDir, tempFileName);
        
        Debug.Log($"[AIAPI] 临时文件路径: {tempFilePath}");
        
        // 构建命令行参数（包含临时文件路径）
        string arguments = $"\"{pythonScriptPath}\" \"{apiKey}\" \"{userMessage}\" \"{model}\" {temperature} {maxTokens} false 1957794713918672896 \"{tempFilePath}\"";
        
        Debug.Log($"[AIAPI] 执行Python脚本: {arguments}");
        
        // 创建进程
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = "python"; // 或者 "python"，取决于系统配置
        startInfo.Arguments = arguments;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;
        startInfo.StandardOutputEncoding = Encoding.UTF8;
        startInfo.StandardErrorEncoding = Encoding.UTF8;
        
        Process process = new Process();
        process.StartInfo = startInfo;
        
        try
        {
            process.Start();
            
            // 等待进程完成，但不超过30秒
            bool completed = process.WaitForExit(30000);
            
            if (!completed)
            {
                process.Kill();
                string errorMsg = "Python脚本执行超时";
                Debug.LogError($"[AIAPI] {errorMsg}");
                onError?.Invoke(errorMsg);
                yield break;
            }
            
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning($"[AIAPI] Python脚本错误输出: {error}");
            }
            
            // 优先尝试从临时文件读取结果
            string resultContent = null;
            if (File.Exists(tempFilePath))
            {
                try
                {
                    resultContent = File.ReadAllText(tempFilePath, Encoding.UTF8);
                    Debug.Log($"[AIAPI] 从临时文件读取到结果: {resultContent}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AIAPI] 读取临时文件失败: {ex.Message}");
                }
            }
            
            // 如果临时文件读取失败，尝试从stdout读取
            if (string.IsNullOrEmpty(resultContent))
            {
                resultContent = output;
                Debug.Log($"[AIAPI] 从stdout读取到结果: {resultContent}");
            }
            
            if (string.IsNullOrEmpty(resultContent))
            {
                string errorMsg = "Python脚本没有输出，且临时文件读取失败";
                Debug.LogError($"[AIAPI] {errorMsg}");
                onError?.Invoke(errorMsg);
                yield break;
            }
            
            try
            {
                // 解析JSON响应
                var result = JsonUtility.FromJson<PythonResponse>(resultContent);
                
                if (result.success)
                {
                    // 添加AI回复到历史记录
                    conversationHistory.Add(new ChatMessage
                    {
                        role = "assistant",
                        content = result.response
                    });
                    
                    // 限制历史记录长度
                    if (conversationHistory.Count > 20)
                    {
                        conversationHistory.RemoveRange(0, conversationHistory.Count - 20);
                    }
                    
                    onResponse?.Invoke(result.response);
                }
                else
                {
                    string errorMsg = $"Python脚本执行失败: {result.error}";
                    Debug.LogError($"[AIAPI] {errorMsg}");
                    onError?.Invoke(errorMsg);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"解析Python脚本输出失败: {ex.Message}";
                Debug.LogError($"[AIAPI] {errorMsg}");
                Debug.LogError($"[AIAPI] 原始输出: {resultContent}");
                onError?.Invoke(errorMsg);
            }
        }
        catch (Exception ex)
        {
            string errorMsg = $"启动Python脚本失败: {ex.Message}";
            Debug.LogError($"[AIAPI] {errorMsg}");
            onError?.Invoke(errorMsg);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
            process.Dispose();
            
            // 清理临时文件
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                    Debug.Log($"[AIAPI] 临时文件已清理: {tempFilePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AIAPI] 清理临时文件失败: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 发送HTTP请求（保留原方法作为备用）
    /// </summary>
    private IEnumerator SendRequest(ChatRequest request, Action<string> onResponse, Action<string> onError)
    {
        // 序列化请求
        string jsonRequest = JsonUtility.ToJson(request);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
        
        Debug.Log($"[AIAPI] 请求JSON: {jsonRequest}");
        Debug.Log($"[AIAPI] 请求URL: {apiUrl}");
        Debug.Log($"[AIAPI] 模型: {model}");
        Debug.Log($"[AIAPI] 消息数量: {request.messages.Count}");
        
        // 验证API密钥
        if (string.IsNullOrEmpty(apiKey))
        {
            string errorMsg = "API密钥为空，无法发送请求";
            Debug.LogError($"[AIAPI] {errorMsg}");
            onError?.Invoke(errorMsg);
            yield break;
        }
        
        using (UnityWebRequest webRequest = new UnityWebRequest(apiUrl, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            
            // 设置请求头
            webRequest.SetRequestHeader("Content-Type", "application/json");
            
            // 使用智谱AI的签名认证
            string authHeader = GetAuthorizationHeader();
            if (!string.IsNullOrEmpty(authHeader))
            {
                webRequest.SetRequestHeader("Authorization", authHeader);
                Debug.Log($"[AIAPI] 认证头已设置: {authHeader.Substring(0, Math.Min(20, authHeader.Length))}...");
            }
            else
            {
                string errorMsg = "生成认证头失败";
                Debug.LogError($"[AIAPI] {errorMsg}");
                onError?.Invoke(errorMsg);
                yield break;
            }
            
            webRequest.timeout = 30;
            
            Debug.Log($"[AIAPI] 发送请求到: {apiUrl}");
            Debug.Log($"[AIAPI] 请求超时设置: {webRequest.timeout}秒");
            
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string responseText = webRequest.downloadHandler.text;
                Debug.Log($"[AIAPI] 响应成功: {responseText}");
                
                try
                {
                    // 解析响应
                    ChatResponse response = JsonUtility.FromJson<ChatResponse>(responseText);
                    
                    if (response != null && response.choices != null && response.choices.Count > 0)
                    {
                        string aiResponse = response.choices[0].message.content;
                        
                        // 添加AI回复到历史记录
                        conversationHistory.Add(new ChatMessage
                        {
                            role = "assistant",
                            content = aiResponse
                        });
                        
                        // 限制历史记录长度
                        if (conversationHistory.Count > 20)
                        {
                            conversationHistory.RemoveRange(0, conversationHistory.Count - 20);
                        }
                        
                        onResponse?.Invoke(aiResponse);
                    }
                    else
                    {
                        string errorMsg = "响应格式错误";
                        Debug.LogError($"[AIAPI] {errorMsg}");
                        Debug.LogError($"[AIAPI] 响应内容: {responseText}");
                        onError?.Invoke(errorMsg);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AIAPI] 解析响应失败: {ex.Message}");
                    Debug.LogError($"[AIAPI] 原始响应: {responseText}");
                    onError?.Invoke($"解析失败: {ex.Message}");
                }
            }
            else
            {
                string errorMessage = $"请求失败: {webRequest.error}";
                if (webRequest.downloadHandler != null && !string.IsNullOrEmpty(webRequest.downloadHandler.text))
                {
                    errorMessage += $"\n响应: {webRequest.downloadHandler.text}";
                }
                
                // 添加更多调试信息
                Debug.LogError($"[AIAPI] {errorMessage}");
                Debug.LogError($"[AIAPI] 响应码: {webRequest.responseCode}");
                Debug.LogError($"[AIAPI] 请求结果: {webRequest.result}");
                
                // 修复：正确获取Authorization头信息
                try
                {
                    string currentAuthHeader = GetAuthorizationHeader();
                    if (!string.IsNullOrEmpty(currentAuthHeader))
                    {
                        // 只显示前20个字符，避免泄露完整密钥
                        string maskedAuth = currentAuthHeader.Length > 20 ? 
                            currentAuthHeader.Substring(0, 20) + "..." : currentAuthHeader;
                        Debug.LogError($"[AIAPI] 使用的认证头: {maskedAuth}");
                    }
                    else
                    {
                        Debug.LogError("[AIAPI] 认证头生成失败");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AIAPI] 获取认证头信息时出错: {ex.Message}");
                }
                
                onError?.Invoke(errorMessage);
            }
        }
    }
    
    /// <summary>
    /// 清空对话历史
    /// </summary>
    public void ClearHistory()
    {
        conversationHistory.Clear();
        Debug.Log("[AIAPI] 对话历史已清空");
    }
    
    /// <summary>
    /// 测试API连接
    /// </summary>
    public void TestConnection()
    {
        Debug.Log("[AIAPI] 开始测试连接...");
        
        // 强制重置为默认配置
        ResetToDefaultConfiguration();
        
        // 先测试基本配置
        Debug.Log("[AIAPI] 测试基本配置...");
        if (!ValidateAPIKey())
        {
            Debug.LogError("[AIAPI] ❌ API密钥验证失败");
            return;
        }
        
        Debug.Log($"[AIAPI] ✅ API密钥验证通过");
        Debug.Log($"[AIAPI] 测试URL: {apiUrl}");
        Debug.Log($"[AIAPI] 测试模型: {model}");
        
        // 检查Python脚本是否存在
        string pythonScriptPath = Path.Combine(Application.dataPath, "Scripts", "AI", "ai_api_handler.py");
        if (!File.Exists(pythonScriptPath))
        {
            Debug.LogError($"[AIAPI] ❌ Python脚本不存在: {pythonScriptPath}");
            return;
        }
        
        Debug.Log($"[AIAPI] ✅ Python脚本存在: {pythonScriptPath}");
        
        // 先测试文件读写功能
        Debug.Log("[AIAPI] 开始测试文件读写功能...");
        StartCoroutine(TestFileIO());
        
        // 然后测试API调用
        SendMessage("你好，请简单介绍一下自己", 
            (response) => {
                Debug.Log($"[AIAPI] ✅ 测试成功: {response}");
            },
            (error) => {
                Debug.LogError($"[AIAPI] ❌ 测试失败: {error}");
                // 提供更多诊断信息
                Debug.LogError($"[AIAPI] 请检查以下项目:");
                Debug.LogError($"[AIAPI] 1. Python环境是否正确安装");
                Debug.LogError($"[AIAPI] 2. requests库是否已安装 (pip install requests)");
                Debug.LogError($"[AIAPI] 3. 网络连接是否正常");
                Debug.LogError($"[AIAPI] 4. API密钥是否正确");
                Debug.LogError($"[AIAPI] 5. API端点是否可访问");
            });
    }
    
    /// <summary>
    /// 测试文件读写功能
    /// </summary>
    private IEnumerator TestFileIO()
    {
        Debug.Log("[AIAPI] 开始测试文件读写功能...");
        
        // 获取测试脚本路径
        string testScriptPath = Path.Combine(Application.dataPath, "Scripts", "AI", "test_file_io.py");
        
        if (!File.Exists(testScriptPath))
        {
            Debug.LogWarning($"[AIAPI] 测试脚本不存在: {testScriptPath}");
            yield break;
        }
        
        // 创建临时文件路径
        string tempDir = Path.Combine(Application.temporaryCachePath, "AIAPI");
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }
        
        string tempFileName = $"test_response_{DateTime.Now:yyyyMMdd_HHmmss}_{UnityEngine.Random.Range(1000, 9999)}.json";
        string tempFilePath = Path.Combine(tempDir, tempFileName);
        
        Debug.Log($"[AIAPI] 测试临时文件路径: {tempFilePath}");
        
        // 构建命令行参数
        string arguments = $"\"{testScriptPath}\" \"{tempFilePath}\"";
        
        Debug.Log($"[AIAPI] 执行测试脚本: {arguments}");
        
        // 创建进程
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = "python3"; // 或者 "python"，取决于系统配置
        startInfo.Arguments = arguments;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;
        startInfo.StandardOutputEncoding = Encoding.UTF8;
        startInfo.StandardErrorEncoding = Encoding.UTF8;
        
        Process process = new Process();
        process.StartInfo = startInfo;
        
        try
        {
            process.Start();
            
            // 等待进程完成，但不超过10秒
            bool completed = process.WaitForExit(10000);
            
            if (!completed)
            {
                process.Kill();
                Debug.LogWarning("[AIAPI] 测试脚本执行超时");
                yield break;
            }
            
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning($"[AIAPI] 测试脚本错误输出: {error}");
            }
            
            Debug.Log($"[AIAPI] 测试脚本stdout输出: {output}");
            
            // 尝试从临时文件读取结果
            if (File.Exists(tempFilePath))
            {
                try
                {
                    string fileContent = File.ReadAllText(tempFilePath, Encoding.UTF8);
                    Debug.Log($"[AIAPI] ✅ 从临时文件读取成功: {fileContent}");
                    
                    // 尝试解析JSON
                    try
                    {
                        var testResult = JsonUtility.FromJson<PythonResponse>(fileContent);
                        if (testResult.success)
                        {
                            Debug.Log("[AIAPI] ✅ 文件读写测试完全成功！");
                        }
                        else
                        {
                            Debug.LogWarning($"[AIAPI] ⚠️ 文件读写测试部分成功，但有错误: {testResult.error}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[AIAPI] ⚠️ 文件读写测试成功，但JSON解析失败: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AIAPI] ❌ 读取测试文件失败: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning("[AIAPI] ⚠️ 测试脚本未创建临时文件");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AIAPI] ❌ 启动测试脚本失败: {ex.Message}");
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
            process.Dispose();
            
            // 清理临时文件
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                    Debug.Log($"[AIAPI] 测试临时文件已清理: {tempFilePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AIAPI] 清理测试临时文件失败: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// 测试网络连接
    /// </summary>
    public void TestNetworkConnection()
    {
        Debug.Log("[AIAPI] 开始测试网络连接...");
        StartCoroutine(TestNetworkConnectionCoroutine());
    }
    
    private IEnumerator TestNetworkConnectionCoroutine()
    {
        // 测试基本网络连接
        using (UnityWebRequest testRequest = UnityWebRequest.Get("https://www.baidu.com"))
        {
            testRequest.timeout = 10;
            yield return testRequest.SendWebRequest();
            
            if (testRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[AIAPI] ✅ 基本网络连接正常");
            }
            else
            {
                Debug.LogError($"[AIAPI] ❌ 基本网络连接失败: {testRequest.error}");
            }
        }
        
        // 测试API端点连接
        using (UnityWebRequest apiTestRequest = UnityWebRequest.Head(apiUrl))
        {
            apiTestRequest.timeout = 15;
            yield return apiTestRequest.SendWebRequest();
            
            if (apiTestRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[AIAPI] ✅ API端点 {apiUrl} 可访问");
            }
            else
            {
                Debug.LogError($"[AIAPI] ❌ API端点 {apiUrl} 不可访问: {apiTestRequest.error}");
                Debug.LogError($"[AIAPI] 响应码: {apiTestRequest.responseCode}");
            }
        }
    }
    
    /// <summary>
    /// 设置API密钥
    /// </summary>
    public void SetAPIKey(string newApiKey)
    {
        apiKey = newApiKey;
        Debug.Log("[AIAPI] API密钥已更新");
    }
    
    /// <summary>
    /// 设置模型
    /// </summary>
    public void SetModel(string newModel)
    {
        model = newModel;
        Debug.Log($"[AIAPI] 模型已更改为: {newModel}");
    }
    
    /// <summary>
    /// 刷新配置并输出当前设置
    /// </summary>
    public void RefreshConfiguration()
    {
        Debug.Log("=== AIAPIManager 当前配置 ===");
        Debug.Log($"[AIAPI] 模型: {model}");
        Debug.Log($"[AIAPI] API密钥: {apiKey}");
        Debug.Log($"[AIAPI] API URL: {apiUrl}");
        Debug.Log($"[AIAPI] Temperature: {temperature}");
        Debug.Log($"[AIAPI] Max Tokens: {maxTokens}");
        Debug.Log("================================");
        
        // 验证配置
        ValidateConfiguration();
    }
    
    /// <summary>
    /// 验证当前配置
    /// </summary>
    private void ValidateConfiguration()
    {
        Debug.Log("[AIAPI] 开始验证配置...");
        
        // 验证API密钥
        if (ValidateAPIKey())
        {
            Debug.Log("[AIAPI] ✅ API密钥验证通过");
        }
        else
        {
            Debug.LogError("[AIAPI] ❌ API密钥验证失败");
        }
        
        // 验证URL格式
        if (Uri.TryCreate(apiUrl, UriKind.Absolute, out Uri uri))
        {
            Debug.Log($"[AIAPI] ✅ API URL格式正确: {uri.Scheme}://{uri.Host}");
        }
        else
        {
            Debug.LogError($"[AIAPI] ❌ API URL格式错误: {apiUrl}");
        }
        
        // 验证模型名称
        if (!string.IsNullOrEmpty(model))
        {
            Debug.Log($"[AIAPI] ✅ 模型名称已设置: {model}");
        }
        else
        {
            Debug.LogError("[AIAPI] ❌ 模型名称未设置");
        }
        
        // 验证参数范围
        if (temperature >= 0f && temperature <= 2f)
        {
            Debug.Log($"[AIAPI] ✅ Temperature值在有效范围内: {temperature}");
        }
        else
        {
            Debug.LogWarning($"[AIAPI] ⚠️ Temperature值可能超出推荐范围: {temperature}");
        }
        
        if (maxTokens > 0 && maxTokens <= 4000)
        {
            Debug.Log($"[AIAPI] ✅ Max Tokens值在有效范围内: {maxTokens}");
        }
        else
        {
            Debug.LogWarning($"[AIAPI] ⚠️ Max Tokens值可能超出推荐范围: {maxTokens}");
        }
    }
    
    /// <summary>
    /// 强制重置配置为默认值
    /// </summary>
    public void ResetToDefaultConfiguration()
    {
        model = "glm-4.5";
        apiKey = "cfed8c512417402983a28e3ceee6bfe1.vdzks2lqATOYjgUy";
        temperature = 0.7f;
        maxTokens = 1000;
        
        Debug.Log("[AIAPI] 配置已重置为默认值");
        RefreshConfiguration();
    }
    
    /// <summary>
    /// 手动测试API请求（详细诊断）
    /// </summary>
    [ContextMenu("详细API诊断")]
    public void DetailedAPIDiagnostic()
    {
        Debug.Log("=== 开始详细API诊断 ===");
        
        // 1. 检查基本配置
        Debug.Log("1. 检查基本配置...");
        RefreshConfiguration();
        
        // 2. 测试网络连接
        Debug.Log("2. 测试网络连接...");
        TestNetworkConnection();
        
        // 3. 测试API请求格式
        Debug.Log("3. 测试API请求格式...");
        StartCoroutine(TestAPIRequestFormat());
        
        Debug.Log("=== 详细API诊断完成 ===");
    }
    
    private IEnumerator TestAPIRequestFormat()
    {
        // 创建一个简单的测试请求
        var testRequest = new ChatRequest
        {
            model = model,
            messages = new List<ChatMessage>
            {
                new ChatMessage { role = "user", content = "你好" }
            },
            temperature = temperature,
            max_tokens = maxTokens
        };
        
        // 序列化请求
        string jsonRequest = JsonUtility.ToJson(testRequest);
        Debug.Log($"[AIAPI] 测试请求JSON: {jsonRequest}");
        
        // 检查JSON格式
        try
        {
            var parsedRequest = JsonUtility.FromJson<ChatRequest>(jsonRequest);
            Debug.Log("[AIAPI] ✅ JSON序列化/反序列化测试通过");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AIAPI] ❌ JSON序列化/反序列化测试失败: {ex.Message}");
        }
        
        // 测试实际的API调用
        Debug.Log("[AIAPI] 开始测试实际API调用...");
        
        SendMessage("测试消息", 
            (response) => {
                Debug.Log($"[AIAPI] ✅ API调用测试成功: {response}");
            },
            (error) => {
                Debug.LogError($"[AIAPI] ❌ API调用测试失败: {error}");
                Debug.LogError("[AIAPI] 请检查以下可能的问题:");
                Debug.LogError("1. API密钥是否正确且未过期");
                Debug.LogError("2. 网络连接是否正常");
                Debug.LogError("3. API端点是否可访问");
                Debug.LogError("4. 请求格式是否符合API要求");
                Debug.LogError("5. 是否有防火墙或代理限制");
            });
        
        yield return null;
    }
}
