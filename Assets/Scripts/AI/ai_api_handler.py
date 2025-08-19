#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
AI API Handler for Unity
使用zai库处理智谱AI API调用的Python脚本
"""

import json
import sys
import time
from typing import Dict, List, Optional, Any

try:
    from zai import ZhipuAiClient
except ImportError:
    print(json.dumps({
        "success": False,
        "error": "zai库未安装，请运行: pip install zai"
    }))
    sys.exit(1)

class AIAPIHandler:
    def __init__(self, api_key: str):
        """
        初始化AI API处理器
        
        Args:
            api_key: 智谱AI API密钥
        """
        self.api_key = api_key
        self.client = ZhipuAiClient(api_key=api_key)
        
    def send_message(self, 
                    user_message: str, 
                    model: str = "glm-4",
                    temperature: float = 0.7,
                    max_tokens: int = 1000,
                    system_prompt: str = None,
                    conversation_history: List[Dict] = None,
                    stream: bool = False,
                    tools: List[Dict] = None) -> Dict[str, Any]:
        """
        发送消息到AI API
        
        Args:
            user_message: 用户消息
            model: 模型名称
            temperature: 温度参数
            max_tokens: 最大token数
            system_prompt: 系统提示词
            conversation_history: 对话历史
            stream: 是否使用流式响应
            tools: 工具列表（如知识库检索等）
            
        Returns:
            Dict包含响应结果或错误信息
        """
        try:
            # 构建消息列表
            messages = []
            
            # 添加系统提示词
            if system_prompt:
                messages.append({
                    "role": "system",
                    "content": system_prompt
                })
            
            # 添加对话历史
            if conversation_history:
                messages.extend(conversation_history)
            
            # 添加当前用户消息
            messages.append({
                "role": "user",
                "content": user_message
            })
            
            # 构建请求参数
            request_params = {
                "model": model,
                "messages": messages,
                "temperature": temperature,
                "max_tokens": max_tokens,
                "stream": stream
            }
            
            # 添加工具参数（如果提供）
            if tools:
                request_params["tools"] = tools
            
            # 发送请求
            return self._handle_normal_response(request_params)
                
        except Exception as e:
            return {
                "success": False,
                "error": f"API调用错误: {str(e)}"
            }
    
    def _handle_normal_response(self, request_params: Dict) -> Dict[str, Any]:
        """
        处理普通响应
        """
        try:
            response = self.client.chat.completions.create(**request_params)
            
            # 提取AI回复
            if response.choices and len(response.choices) > 0:
                choice = response.choices[0]
                if hasattr(choice, 'message') and choice.message:
                    ai_response = choice.message.content
                else:
                    ai_response = choice.delta.content if hasattr(choice, 'delta') else ""
                
                return {
                    "success": True,
                    "response": ai_response,
                    "usage": {
                        "prompt_tokens": response.usage.prompt_tokens if response.usage else 0,
                        "completion_tokens": response.usage.completion_tokens if response.usage else 0,
                        "total_tokens": response.usage.total_tokens if response.usage else 0
                    },
                    "model": response.model,
                    "id": response.id
                }
            else:
                return {
                    "success": False,
                    "error": "响应中没有choices"
                }
                
        except Exception as e:
            return {
                "success": False,
                "error": f"处理响应时出错: {str(e)}"
            }
    
    def create_retrieval_tool(self, knowledge_id: str, prompt_template: str = None) -> Dict:
        """
        创建知识库检索工具
        
        Args:
            knowledge_id: 知识库ID
            prompt_template: 提示词模板
            
        Returns:
            工具配置字典
        """
        if prompt_template is None:
            prompt_template = """从文档
\"\"\"
{{knowledge}}
\"\"\"
中找问题
\"\"\"
{{question}}
\"\"\"
的答案，找到答案就仅使用文档语句回答问题，找不到答案就用自身知识回答。
不要复述问题，直接开始回答。"""
        
        return {
            "type": "retrieval",
            "retrieval": {
                "knowledge_id": knowledge_id,
                "prompt_template": prompt_template
            }
        }

def main():
    """
    主函数，处理命令行参数并执行API调用
    """
    if len(sys.argv) < 3:
        print(json.dumps({
            "success": False,
            "error": "参数不足。用法: python ai_api_handler.py <api_key> <user_message> [model] [temperature] [max_tokens] [stream] [knowledge_id]"
        }))
        sys.exit(1)
    
    # 解析命令行参数
    api_key = sys.argv[1]
    user_message = sys.argv[2]
    model = sys.argv[3] if len(sys.argv) > 3 else "glm-4.5"
    temperature = float(sys.argv[4]) if len(sys.argv) > 4 else 0.7
    max_tokens = int(sys.argv[5]) if len(sys.argv) > 5 else 1000
    stream = sys.argv[6].lower() == "true" if len(sys.argv) > 6 else False
    knowledge_id = sys.argv[7] if len(sys.argv) > 7 else None
    
    # 系统提示词
    system_prompt = """你是一个名为"电网智询 (Grid-AI)"的智能助手，内嵌于一套"电力线三维重建与管理系统"中。你的核心任务是帮助电力行业的专业人员（如工程师、巡检员、管理人员）通过自然语言对话，快速、准确地从系统中获取信息、执行分析和进行可视化交互。

[角色定义]
1. 身份：你是电力数据分析专家和系统操作向导。
2. 沟通风格：你的回答必须精确、简洁、专业。优先使用列表、表格等结构化方式呈现数据，确保信息清晰易读。
3. 知识边界：你的所有知识严格限定于当前系统中加载和处理的数据。这些数据包括：原始点云数据 (分类后的地面、植被、建筑物、电力线等)、电力设施三维模型 (电力线、杆塔)、分析结果 (危险点、交叉跨越、对地距离、弧垂、植被侵入等)、元数据 (线路ID、电压等级、杆塔编号、巡检日期等)。你绝对不能凭空捏造数据或回答与当前系统数据无关的问题。如果用户提问超出范围，你必须礼貌地拒绝并重申你的职责范围。

[核心能力与任务]
你必须能够理解用户的意图，并将其分解为以下几类核心任务：
1. 数据查询与筛选 (Data Query & Filtering)
2. 空间分析与量算 (Spatial Analysis & Measurement)
3. 风险识别与告警 (Risk Identification & Alerts)
4. 视图控制与可视化 (View Control & Visualization): 当需要进行视图操作时，你需生成特定格式的JSON指令，例如：{"action": "view_control", "command": "highlight", "target": {"type": "line", "id": "L-55"}};"""
    
    # 创建API处理器
    handler = AIAPIHandler(api_key)
    
    # 准备工具列表
    tools = None
    if knowledge_id:
        tools = [handler.create_retrieval_tool(knowledge_id)]
    
    # 发送消息
    result = handler.send_message(
        user_message=user_message,
        model=model,
        temperature=temperature,
        max_tokens=max_tokens,
        system_prompt=system_prompt,
        stream=stream,
        tools=tools
    )
    
    # 输出结果（JSON格式，供Unity解析）
    print(json.dumps(result, ensure_ascii=False))

if __name__ == "__main__":
    main()
