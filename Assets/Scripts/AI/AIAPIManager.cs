using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
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

    private string systemPrompt = @"你是一个名为“电网智询 (Grid-AI)”的智能助手，内嵌于一套“电力线三维重建与管理系统”中。你的核心任务是帮助电力行业的专业人员（如工程师、巡检员、管理人员）通过自然语言对话，快速、准确地从系统中获取信息、执行分析和进行可视化交互。

[角色定义]
1. 身份：你是电力数据分析专家和系统操作向导。
2. 沟通风格：你的回答必须精确、简洁、专业。优先使用列表、表格等结构化方式呈现数据，确保信息清晰易读。
3. 知识边界：你的所有知识严格限定于当前系统中加载和处理的数据。这些数据包括：原始点云数据 (分类后的地面、植被、建筑物、电力线等)、电力设施三维模型 (电力线、杆塔)、分析结果 (危险点、交叉跨越、对地距离、弧垂、植被侵入等)、元数据 (线路ID、电压等级、杆塔编号、巡检日期等)。你绝对不能凭空捏造数据或回答与当前系统数据无关的问题。如果用户提问超出范围，你必须礼貌地拒绝并重申你的职责范围。

[核心能力与任务]
你必须能够理解用户的意图，并将其分解为以下几类核心任务：
1. 数据查询与筛选 (Data Query & Filtering)
2. 空间分析与量算 (Spatial Analysis & Measurement)
3. 风险识别与告警 (Risk Identification & Alerts)
4. 视图控制与可视化 (View Control & Visualization): 当需要进行视图操作时，你需生成特定格式的JSON指令，例如：{""action"": ""view_control"", ""command"": ""highlight"", ""target"": {""type"": ""line"", ""id"": ""L-55""}}";


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
    
    // 添加响应缓存，提升AI响应速度
    private Dictionary<string, string> responseCache = new Dictionary<string, string>();
    
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

        // 检查是否是常见问题，提供快速响应
        string quickResponse = GetQuickResponse(userMessage);
        if (!string.IsNullOrEmpty(quickResponse))
        {
            Debug.Log("[AIAPI] 使用快速响应");
            quickResponseCount++;
            totalRequestCount++;
            onResponse?.Invoke(quickResponse);
            return;
        }
        
        totalRequestCount++;

        // 显示友好的等待提示
        onResponse?.Invoke("🤔 正在思考中，请稍候...");
        
        // 通过Python脚本发送请求
        StartCoroutine(SendRequestViaPython(userMessage, onResponse, onError));
    }
    
    /// <summary>
    /// 执行一次Python脚本
    /// </summary>
    private IEnumerator ExecutePythonScriptOnce(string userMessage, Action<string> onResponse, Action<string> onError)
    {
        Debug.Log("[AIAPI] 开始执行Python脚本...");
        
        // 获取Python脚本路径
        string pythonScriptPath = Path.Combine(Application.dataPath, "Scripts", "AI", "ai_api_handler.py");
        
        if (!File.Exists(pythonScriptPath))
        {
            string errorMsg = $"Python脚本不存在: {pythonScriptPath}";
            Debug.LogError($"[AIAPI] {errorMsg}");
            onError?.Invoke(errorMsg);
            yield break;
        }
        
        // 构建命令行参数
        string arguments = $"\"{pythonScriptPath}\" \"{apiKey}\" \"{userMessage}\" \"{model}\" {temperature} {maxTokens} false 1957794713918672896";
        
        Debug.Log($"[AIAPI] 执行Python脚本: {arguments}");
        
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
        
        // 启动进程
        bool processStarted = false;
        try
        {
            process.Start();
            processStarted = true;
        }
        catch (Exception ex)
        {
            string errorMsg = $"启动Python脚本失败: {ex.Message}";
            Debug.LogError($"[AIAPI] {errorMsg}");
            onError?.Invoke(errorMsg);
            yield break;
        }
        
        if (!processStarted)
        {
            yield break;
        }
        
        // 使用协程方式等待进程完成，提供更好的进度反馈
        float startTime = Time.time;
        float timeout = 30f; // 增加超时时间到30秒
        
        while (!process.HasExited && (Time.time - startTime) < timeout)
        {
            // 每0.5秒检查一次进程状态
            yield return new WaitForSeconds(0.5f);
            
            // 提供进度反馈
            float elapsed = Time.time - startTime;
            if (elapsed % 5f < 0.5f) // 每5秒显示一次进度
            {
                Debug.Log($"[AIAPI] Python脚本执行中... {elapsed:F1}s / {timeout}s");
            }
        }
        
        if (!process.HasExited)
        {
            // 超时，强制结束进程
            try
            {
                process.Kill();
                Debug.LogWarning("[AIAPI] Python脚本执行超时，强制结束进程");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AIAPI] 结束进程时出错: {ex.Message}");
            }
            
            string errorMsg = $"Python脚本执行超时（{timeout}秒）";
            Debug.LogError($"[AIAPI] {errorMsg}");
            onError?.Invoke(errorMsg);
            yield break;
        }
        
        // 等待进程完全结束
        process.WaitForExit();
        
        // 读取输出
        string output = "";
        string error = "";
        
        try
        {
            output = process.StandardOutput.ReadToEnd();
            error = process.StandardError.ReadToEnd();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AIAPI] 读取进程输出时出错: {ex.Message}");
        }
        
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogWarning($"[AIAPI] Python脚本错误输出: {error}");
        }
        
        // 改进：更智能的输出检查
        if (string.IsNullOrEmpty(output))
        {
            // 检查进程退出码
            if (process.ExitCode != 0)
            {
                string errorMsg = $"Python脚本执行失败，退出码: {process.ExitCode}";
                if (!string.IsNullOrEmpty(error))
                {
                    errorMsg += $"\n错误信息: {error}";
                }
                Debug.LogError($"[AIAPI] {errorMsg}");
                // 不直接调用onError，让重试机制处理
                yield break;
            }
            else
            {
                // 进程正常退出但没有输出，可能是静默执行
                string errorMsg = "Python脚本执行完成但没有输出，请检查脚本逻辑";
                Debug.LogWarning($"[AIAPI] {errorMsg}");
                // 不直接调用onError，让重试机制处理
                yield break;
            }
        }
        
        Debug.Log($"[AIAPI] Python脚本输出: {output}");
        
        try
        {
            // 解析JSON响应
            var result = JsonUtility.FromJson<PythonResponse>(output);
            
            if (result.success)
            {
                // 缓存响应结果
                string cacheKey = GetCacheKey(userMessage);
                responseCache[cacheKey] = result.response;
                
                // 限制缓存大小
                if (responseCache.Count > 100)
                {
                    var oldestKey = responseCache.Keys.First();
                    responseCache.Remove(oldestKey);
                }
                
                // 记录响应时间
                float responseTime = Time.time - startTime;
                responseTimes.Add(responseTime);
                if (responseTimes.Count > 50) // 限制记录数量
                {
                    responseTimes.RemoveAt(0);
                }
                
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
                // 不直接调用onError，让重试机制处理
                yield break;
            }
        }
        catch (Exception ex)
        {
            string errorMsg = $"解析Python脚本输出失败: {ex.Message}";
            Debug.LogError($"[AIAPI] {errorMsg}");
            Debug.LogError($"[AIAPI] 原始输出: {output}");
            // 不直接调用onError，让重试机制处理
            yield break;
        }
        
        // 清理进程
        try
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
            process.Dispose();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AIAPI] 清理进程时出错: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 通过Python脚本发送请求（主方法，包含重试机制）
    /// </summary>
    private IEnumerator SendRequestViaPython(string userMessage, Action<string> onResponse, Action<string> onError)
    {
        Debug.Log("[AIAPI] 开始通过Python脚本发送请求...");
        
        // 检查缓存
        string cacheKey = GetCacheKey(userMessage);
        if (responseCache.ContainsKey(cacheKey))
        {
            Debug.Log("[AIAPI] 使用缓存响应，立即返回");
            onResponse?.Invoke(responseCache[cacheKey]);
            yield break;
        }
        
        // 静默重试机制，用户只看到"正在思考中"
        int maxRetries = 2;
        int currentRetry = 0;
        
        while (currentRetry <= maxRetries)
        {
            if (currentRetry > 0)
            {
                Debug.Log($"[AIAPI] 第 {currentRetry} 次重试...");
                // 不向用户显示重试信息，保持"正在思考中"的状态
                yield return new WaitForSeconds(1f); // 重试前等待1秒
            }
            
            bool success = false;
            yield return StartCoroutine(ExecutePythonScriptOnce(userMessage, 
                (response) => { 
                    success = true; 
                    onResponse?.Invoke(response); 
                }, 
                (error) => { 
                    success = false; 
                    if (currentRetry == maxRetries) onError?.Invoke(error); 
                }));
            
            if (success)
            {
                yield break; // 成功则退出
            }
            
            currentRetry++;
        }
        
        // 所有重试都失败了
        string finalErrorMsg = "抱歉，AI服务暂时不可用，请稍后再试。如果问题持续存在，请检查网络连接和API配置。";
        Debug.LogError($"[AIAPI] 所有重试都失败了: {userMessage}");
        onError?.Invoke(finalErrorMsg);
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
    
    /// <summary>
    /// 生成缓存键
    /// </summary>
    private string GetCacheKey(string userMessage)
    {
        // 使用消息内容的哈希值作为缓存键
        return System.Security.Cryptography.MD5.Create()
            .ComputeHash(System.Text.Encoding.UTF8.GetBytes(userMessage))
            .Aggregate("", (s, b) => s + b.ToString("x2"));
    }
    
    /// <summary>
    /// 获取快速响应（针对常见问题）
    /// </summary>
    private string GetQuickResponse(string userMessage)
    {
        string message = userMessage.ToLower().Trim();
        
        // 系统功能相关
        if (message.Contains("系统") && message.Contains("功能"))
        {
            return "本系统主要功能包括：\n" +
                   "• 电力线3D可视化\n" +
                   "• 电塔总览和管理\n" +
                   "• 危险物监测和标记\n" +
                   "• 点云数据处理\n" +
                   "• 距离测量和空间分析\n" +
                   "• 无人机巡检管理\n" +
                   "• AI智能助手支持";
        }
        
        // 电塔相关
        if (message.Contains("电塔") || message.Contains("tower"))
        {
            return "电塔管理功能：\n" +
                   "• 电塔位置总览\n" +
                   "• 电塔状态监控\n" +
                   "• 快速跳转定位\n" +
                   "• 电塔信息统计\n" +
                   "• 支持大量电塔数据";
        }
        
        // 电力线相关
        if (message.Contains("电力线") || message.Contains("powerline"))
        {
            return "电力线功能：\n" +
                   "• 3D电力线可视化\n" +
                   "• 电力线信息查看\n" +
                   "• 电力线标记系统\n" +
                   "• 空间距离测量";
        }
        
        // 点云相关
        if (message.Contains("点云") || message.Contains("point cloud"))
        {
            return "点云功能：\n" +
                   "• 点云数据加载\n" +
                   "• 点云可视化\n" +
                   "• 点云数据处理\n" +
                   "• 点云与电力线结合";
        }
        
        // 危险物相关
        if (message.Contains("危险") || message.Contains("danger"))
        {
            return "危险物监测：\n" +
                   "• 树木危险监测\n" +
                   "• 危险物标记\n" +
                   "• 风险等级评估\n" +
                   "• 自动巡检功能";
        }
        
        // 测量相关
        if (message.Contains("测量") || message.Contains("measure"))
        {
            return "测量功能：\n" +
                   "• 3D空间距离测量\n" +
                   "• 多点测量\n" +
                   "• 测量结果记录\n" +
                   "• 精确坐标显示";
        }
        
        // 相机控制
        if (message.Contains("相机") || message.Contains("camera"))
        {
            return "相机控制：\n" +
                   "• 第一人称视角\n" +
                   "• 上帝视角\n" +
                   "• 飞行视角\n" +
                   "• 平滑相机移动";
        }
        
        return null; // 没有快速响应
    }
    
    /// <summary>
    /// 清空缓存
    /// </summary>
    public void ClearCache()
    {
        responseCache.Clear();
        Debug.Log("[AIAPI] 响应缓存已清空");
    }
    
    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public string GetCacheInfo()
    {
        return $"缓存条目数: {responseCache.Count}";
    }
    
    /// <summary>
    /// 获取AI性能统计信息
    /// </summary>
    public string GetPerformanceInfo()
    {
        return $"AI助手性能统计：\n" +
               $"• 缓存命中率: {GetCacheHitRate():F1}%\n" +
               $"• 快速响应次数: {quickResponseCount}\n" +
               $"• 总请求次数: {totalRequestCount}\n" +
               $"• 平均响应时间: {GetAverageResponseTime():F1}秒";
    }
    
    // 性能统计字段
    private int quickResponseCount = 0;
    private int totalRequestCount = 0;
    private List<float> responseTimes = new List<float>();
    
    /// <summary>
    /// 计算缓存命中率
    /// </summary>
    private float GetCacheHitRate()
    {
        if (totalRequestCount == 0) return 0f;
        return (float)(totalRequestCount - responseTimes.Count) / totalRequestCount * 100f;
    }
    
    /// <summary>
    /// 计算平均响应时间
    /// </summary>
    private float GetAverageResponseTime()
    {
        if (responseTimes.Count == 0) return 0f;
        return responseTimes.Average();
    }
}
