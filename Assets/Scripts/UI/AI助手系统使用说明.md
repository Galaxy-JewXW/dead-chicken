# AI助手系统使用说明

## 概述

AI助手系统为电力线可视化系统提供了智能聊天和帮助功能，用户可以通过自然语言与系统交互，获取操作指导、功能说明和问题解答。

## 系统组件

### 1. AIAssistantManager.cs
AI助手的核心管理器，负责：
- 聊天界面的创建和管理
- 消息处理和AI回复生成
- 聊天记录管理
- UI交互控制

### 2. AIAssistantConfig.cs
AI助手的配置文件，包含：
- 知识库条目
- 快速回复设置
- 系统集成选项
- 界面主题配置

### 3. AIAssistantInitializer.cs
AI助手的初始化器，负责：
- 自动创建和配置AI助手
- 集成到现有UI系统
- 提供便捷的管理功能

## 快速开始

### 方法1：自动初始化（推荐）

1. 在场景中创建一个空物体
2. 添加 `AIAssistantInitializer` 组件
3. 配置相关参数
4. 运行场景，AI助手将自动初始化

### 方法2：手动创建

1. 创建空物体，添加 `AIAssistantManager` 组件
2. 配置AI助手参数
3. 调用 `InitializeAIAssistant()` 方法

## 配置说明

### 基础设置
- `assistantName`: AI助手的名称
- `assistantColor`: 主要颜色主题
- `maxChatHistory`: 最大聊天记录数
- `typingSpeed`: 打字效果速度

### 知识库配置
知识库支持以下分类：
- **系统功能**: 介绍系统主要功能
- **操作指导**: 提供操作步骤说明
- **问题解答**: 常见问题及解决方案

### 快速回复
配置常用的快速回复，提高响应速度。

## 使用方法

### 基本聊天
1. 点击右下角的"AI助手"按钮
2. 在聊天界面输入问题
3. 按回车键或点击发送按钮
4. 查看AI助手的回复

### 支持的问题类型
- 系统功能咨询
- 操作指导请求
- 问题排查帮助
- 功能特性了解

### 快捷键
- `Enter`: 发送消息
- `Esc`: 关闭聊天面板

## 自定义配置

### 添加知识库条目
```csharp
// 在AIAssistantConfig中添加
config.AddKnowledgeEntry(
    "自定义分类", 
    "关键词", 
    "回复内容", 
    new string[] { "相关关键词1", "相关关键词2" }, 
    优先级
);
```

### 修改快速回复
在配置文件中编辑 `quickResponses` 列表，添加或修改快速回复规则。

### 自定义主题
修改 `primaryColor` 和 `secondaryColor` 来自定义界面主题。

## 系统集成

### 与现有UI系统集成
AI助手系统可以自动集成到：
- `SimpleUIToolkitManager`
- `InitialInterfaceManager`
- 其他自定义UI管理器

### 扩展功能
可以通过继承和重写方法来实现：
- 自定义AI回复逻辑
- 集成外部AI服务
- 添加语音识别功能
- 实现多语言支持

## 故障排除

### 常见问题

1. **AI助手不显示**
   - 检查 `enableAIAssistant` 是否启用
   - 确认 `AIAssistantInitializer` 已正确配置
   - 查看控制台错误信息

2. **配置文件加载失败**
   - 确认配置文件路径正确
   - 检查配置文件格式
   - 系统将使用默认配置

3. **UI显示异常**
   - 检查Unity版本兼容性
   - 确认UI Toolkit已启用
   - 验证样式设置

### 调试方法

1. 使用 `AIAssistantInitializer` 的上下文菜单
2. 查看控制台日志信息
3. 检查Inspector面板配置
4. 使用 `ReinitializeAIAssistant()` 重新初始化

## 性能优化

### 聊天记录管理
- 设置合理的 `maxChatHistory` 值
- 定期清理聊天记录
- 使用分页加载大量历史记录

### 知识库优化
- 合理设置关键词优先级
- 避免重复的关键词定义
- 使用相关关键词提高匹配精度

## 扩展开发

### 添加新的AI功能
1. 继承 `AIAssistantManager` 类
2. 重写 `GenerateAIResponse` 方法
3. 实现自定义的AI逻辑
4. 集成外部AI服务API

### 自定义UI界面
1. 修改 `CreateUIStructure` 方法
2. 添加新的UI元素
3. 实现自定义的交互逻辑
4. 应用自定义样式

## 版本历史

### v1.0.0
- 基础聊天功能
- 知识库系统
- 快速回复功能
- 自动初始化
- UI集成支持

## 技术支持

如遇到问题或需要技术支持，请：
1. 查看控制台错误信息
2. 检查配置文件设置
3. 参考本文档的故障排除部分
4. 联系开发团队获取帮助

---

**注意**: 本系统基于Unity UI Toolkit开发，请确保项目已正确配置UI Toolkit环境。
