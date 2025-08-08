#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
简化的电力线提取脚本
减少冗长的输出信息，只显示关键进度
"""

import sys
import os
from Generator import Generator

def main():
    """主函数"""
    if len(sys.argv) < 2:
        print("使用方法: python simple_extract.py <输入文件路径> [输出JSON路径]")
        print("示例: python simple_extract.py ../data/A/A.las A_result.json")
        return
    
    input_file = sys.argv[1]
    output_file = sys.argv[2] if len(sys.argv) > 2 else "result.json"
    
    # 检查输入文件是否存在
    if not os.path.exists(input_file):
        print(f"错误：输入文件不存在: {input_file}")
        return
    
    print(f"开始处理: {input_file}")
    
    try:
        # 创建生成器实例
        generator = Generator(file_path=input_file, json_path=output_file)
        
        # 执行电力线提取
        print("正在提取电力线...")
        generator.generate(
            min_line_points=50, 
            min_line_length=200.0,
            length_method='path',
            reference_point_method='center'
        )
        
        # 生成结果文件
        print("正在生成结果文件...")
        generator.dump(fit_method="catenary")
        
        print(f"处理完成！结果已保存到: {output_file}")
        
    except Exception as e:
        print(f"处理过程中出现错误: {str(e)}")
        return 1
    
    return 0

if __name__ == "__main__":
    exit(main()) 