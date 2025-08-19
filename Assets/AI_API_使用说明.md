# Unity AI API 使用说明

## 概述
这是一个简化的Unity AI API集成方案，用于调用智谱AI的API服务。

## 文件结构
- `AIAPIManager.cs` - 核心API管理器
- `AIModelTest.cs` - 测试脚本
- `AIAPIManagerInitializer.cs` - 初始化器

## 快速开始

### 1. 场景设置
1. 在场景中创建一个空的GameObject
2. 添加 `AIAPIManagerInitializer` 脚本
3. 添加 `AIModelTest` 脚本（可选）

### 2. 配置API密钥
在 `AIAPIManager.cs` 中修改以下配置：
```csharp
[SerializeField] private string apiKey = "你的API密钥";
[SerializeField] private string model = "glm-4";
```

### 3. 运行测试
1. 运行场景
2. 查看Console输出
3. 使用Context Menu进行测试

## 使用方法

### 基本调用
```csharp
AIAPIManager.Instance.SendMessage("你好", 
    (response) => {
        Debug.Log($"AI回复: {response}");
    },
    (error) => {
        Debug.LogError($"错误: {error}");
    });
```

### 测试功能
- 右键点击 `AIModelTest` 组件
- 选择 "测试AI API" 或 "发送测试消息"

### 管理对话历史
```csharp
// 清空对话历史
AIAPIManager.Instance.ClearHistory();

// 设置API密钥
AIAPIManager.Instance.SetAPIKey("新密钥");

// 设置模型
AIAPIManager.Instance.SetModel("glm-4");
```

## 配置参数

### API配置
- `apiKey`: 智谱AI API密钥
- `apiUrl`: API端点（默认：https://open.bigmodel.cn/api/paas/v4/chat/completions）
- `model`: 模型名称（默认：glm-4）
- `temperature`: 温度参数（0.0-2.0）
- `maxTokens`: 最大Token数

### 初始化设置
- `autoInitializeOnStart`: 启动时自动初始化
- `createIfNotExists`: 如果不存在则自动创建
- `autoTestOnStart`: 启动时自动测试

## 错误处理

### 常见错误
1. **API密钥错误**: 检查密钥是否正确
2. **网络连接问题**: 检查网络连接
3. **模型不存在**: 检查模型名称是否正确

### 调试信息
所有API调用都会在Console中输出详细的调试信息，包括：
- 请求JSON
- 响应内容
- 错误信息

## 最佳实践

1. **错误处理**: 始终提供错误回调函数
2. **对话管理**: 定期清空对话历史避免Token过多
3. **网络超时**: 设置合适的超时时间
4. **测试**: 在正式使用前进行充分测试

## 故障排除

### 如果API调用失败
1. 检查API密钥是否正确
2. 确认网络连接正常
3. 查看Console中的详细错误信息
4. 尝试使用不同的模型

### 如果响应解析失败
1. 检查响应格式是否正确
2. 确认JSON序列化是否正常
3. 查看原始响应内容

## 更新日志
- v2.0: 简化代码结构，提高稳定性
- v1.0: 初始版本


