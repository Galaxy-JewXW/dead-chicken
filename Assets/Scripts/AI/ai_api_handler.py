import json
import sys
import time
import os
import tempfile
from typing import Dict, List, Optional, Any

if sys.stdout.encoding != 'utf-8':
    sys.stdout.reconfigure(encoding='utf-8')

os.environ['PYTHONIOENCODING'] = 'utf-8'

try:
    from zai import ZhipuAiClient
except ImportError:
    error_result = {
        "success": False,
        "error": "zai库未安装，请运行: pip install zai"
    }
    print(json.dumps(error_result, ensure_ascii=False))
    # 同时尝试写入临时文件
    try:
        temp_file = os.environ.get('UNITY_TEMP_FILE')
        if temp_file:
            with open(temp_file, 'w', encoding='utf-8') as f:
                json.dump(error_result, f, ensure_ascii=False, indent=2)
    except:
        pass
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
                "stream": stream,
                "thinking": {
                    "type": "disabled"
                }
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
    
    def check_need_knowledge_base(self, user_message: str, model: str = "glm-4") -> bool:
        """
        判断用户问题是否需要调用知识库
        
        Args:
            user_message: 用户消息
            model: 模型名称
            
        Returns:
            bool: True表示需要知识库，False表示不需要
        """
        check_prompt = """你是一个智能判断助手。请分析用户的问题，判断是否需要调用"电力线三维重建与管理系统"的知识库文档来回答。

**判断标准：**

**需要知识库（回答"需要"）**的问题类型：
- 
- 系统具体功能使用方法（如：如何导入点云数据、怎么生成三维模型等）
- 系统界面操作步骤（如：在哪里找到某个按钮、某个功能在哪个菜单等）
- 系统技术参数和配置（如：支持的文件格式、系统要求等）
- 系统故障排查（如：为什么导入失败、如何解决某个错误等）
- 系统特定术语和概念（如：系统中某个专有名词的含义等）

**不需要知识库（回答"不需要"）**的问题类型：
- 电力行业通用知识（如：高压线标准、电力设备规格、行业术语解释等）
- 一般性电力工程概念（如：弧垂、风偏、相间距等专业概念）
- 电力安全规范（如：安全距离、作业规范等）
- 电力设备基本知识（如：杆塔类型、导线材料等）

用户问题：{question}"""
        
        try:
            result = self.send_message(
                user_message=check_prompt.format(question=user_message),
                model=model,
                temperature=0.1,  # 使用较低的温度确保判断一致性
                max_tokens=50
            )
            
            if result.get("success"):
                response = result.get("response", "").strip().lower()
                return not "不需要" in response
            else:
                # 如果判断失败，默认不使用知识库
                return False
                
        except Exception as e:
            # 异常情况下默认不使用知识库
            return False
    
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

def output_result(result: Dict, temp_file_path: str = None):
    """
    输出结果到stdout和临时文件（如果提供）
    
    Args:
        result: 要输出的结果字典
        temp_file_path: 临时文件路径（可选）
    """
    # 输出到stdout
    json_str = json.dumps(result, ensure_ascii=False, indent=None)
    print(json_str)
    
    # 如果提供了临时文件路径，也写入文件
    if temp_file_path and os.path.exists(os.path.dirname(temp_file_path)):
        try:
            with open(temp_file_path, 'w', encoding='utf-8') as f:
                json.dump(result, f, ensure_ascii=False, indent=2)
            print(f"结果已同时写入临时文件: {temp_file_path}", file=sys.stderr)
        except Exception as e:
            print(f"写入临时文件失败: {e}", file=sys.stderr)

def main():
    import time
    t1 = time.time()
    """
    主函数，处理命令行参数并执行API调用
    """
    # 解析命令行参数
    user_message = sys.argv[1] if len(sys.argv) > 1 else "什么是主成分分析？"
    api_key = sys.argv[2] if len(sys.argv) > 2 else "cfed8c512417402983a28e3ceee6bfe1.vdzks2lqATOYjgUy"
    model = sys.argv[3] if len(sys.argv) > 3 else "glm-4.5"
    temperature = float(sys.argv[4]) if len(sys.argv) > 4 else 0.5
    max_tokens = int(sys.argv[5]) if len(sys.argv) > 5 else 1000
    stream = sys.argv[6].lower() == "true" if len(sys.argv) > 6 else False
    knowledge_id = sys.argv[7] if len(sys.argv) > 7 else "1957794713918672896"
    temp_file_path = sys.argv[8] if len(sys.argv) > 8 else None
    
    # 系统提示词
    system_prompt = """**[系统名称与定位]**

你是"**电网智询 (Grid-AI)**"，作为"电力线三维重建与管理系统"的智能帮助中心而存在。  
你的使命：帮助用户理解和高效使用本系统，并在必要时提供电力行业背景知识。  

---

**[核心角色]**

1. **系统功能专家**  
   - 精通系统的《用户手册》《技术文档》《使用说明/FAQ》。  
   - 熟悉所有功能、操作步骤、界面说明和故障排查方法。  
   - 职责：指导用户"如何在系统中操作"。  

2. **电力行业知识顾问**  
   - 熟悉常见电力行业规范、概念与设备。  
   - 职责：解释行业术语、提供背景知识，辅助用户理解系统功能。  

---

**[沟通原则]**

1. **直接明了**：回答以分点或步骤形式为主，简洁高效。  
2. **自然表达**：避免机械化套话，保持专业但自然、友好的人类口吻。  
3. **回答策略**：  
   
   **电力行业通用知识问题**（如：高压线标准、电力设备规格、行业术语解释等）：
   - 直接运用电力行业知识回答，无需提及文档或资料来源
   - 开门见山，直接阐述相关知识点
   - **严禁**说"文档中未找到""文档中没有提及""根据文档"等表述
   
   **系统功能操作问题**（如：如何使用某功能、界面在哪里、操作步骤等）：
   - 必须基于系统文档回答
   - 若文档未提及，明确说明"系统文档中未找到相关功能说明"

4. **身份类问题**：用户问"你是谁/你是什么系统"等 → 直接回答：  
   "我是电网智询 (Grid-AI)，作为电力线三维重建与管理系统的智能帮助中心而存在。"  

5. **禁止臆测**：  
   - 不提供实时数据（如具体线路、设备状态、测量值等）。  
   - 不编造文档中不存在的功能说明或参数。  
   - 不回答与系统和电力行业无关的问题。  

---

**[回答示例]**

**正确示例**（电力行业知识）：
用户："高压线的标准是什么？"
回答："高压线通常指电压等级在35kV及以上的输电线路..."

**错误示例**：
"文档中未找到关于高压线标准的信息。不过，高压线通常指..."
"文档中未找到关于高压线标准的信息。"

---

**[知识来源]**

1. **系统文档（功能性信息的唯一来源）**  
   - 《用户手册》  
   - 《技术文档》  
   - 《使用说明 / FAQ》  

2. **电力行业通用知识（行业背景知识来源）**  
   - 行业标准与安全规范  
   - 杆塔、导线、绝缘子等基本设备知识  
   - 弧垂、风偏、相间距等专业概念解释  
"""

    
    try:
        # 创建API处理器
        handler = AIAPIHandler(api_key)
        
        # 第一步：判断是否需要知识库
        # print(f"正在判断问题是否需要知识库...", file=sys.stderr)
        need_knowledge_base = handler.check_need_knowledge_base(user_message, model)
        # print(f"判断结果：{'需要' if need_knowledge_base else '不需要'}知识库", file=sys.stderr)
        
        # 第二步：根据判断结果准备工具列表
        tools = None
        if need_knowledge_base and knowledge_id:
            tools = [handler.create_retrieval_tool(knowledge_id)]
            # print(f"已添加知识库检索工具，knowledge_id: {knowledge_id}", file=sys.stderr)
        else:
            # print("不使用知识库，直接基于训练知识回答", file=sys.stderr)
            pass
        
        # 第三步：发送正式请求
        # print("正在生成回答...", file=sys.stderr)
        result = handler.send_message(
            user_message=user_message,
            model=model,
            temperature=temperature,
            max_tokens=max_tokens,
            system_prompt=system_prompt,
            stream=stream,
            tools=tools
        )
        
        # 在结果中添加是否使用了知识库的信息
        result["used_knowledge_base"] = need_knowledge_base
        
        # 输出结果（同时输出到stdout和临时文件）
        output_result(result, temp_file_path)
        t2 = time.time()
        # print(f"time: {t2 - t1}s", file=sys.stderr)
        
    except Exception as e:
        error_result = {
            "success": False,
            "error": f"执行过程中发生错误: {str(e)}",
            "used_knowledge_base": False
        }
        output_result(error_result, temp_file_path)
        sys.exit(1)

if __name__ == "__main__":
    main()
