# 项目修改说明

## 概述
根据用户需求，对电力线提取系统进行了以下主要修改：

## 1. Python脚本修改

### remote_runner.py
- **移除命令行参数**：不再需要 `--input` 和 `--output` 参数
- **输入方式改变**：从CS脚本中读取输入文件路径（通过环境变量或配置文件）
- **输出方式改变**：不需要输出文件，只进行远程电力线提取
- **新增功能**：在脚本最后使用open3d输出点云可视化

#### 主要变化：
```python
# 原来：python remote_runner.py --input "input.las" --output "output.las"
# 现在：python remote_runner.py

# 输入文件路径通过以下方式获取：
# 1. 环境变量 UNITY_INPUT_FILE
# 2. 配置文件 unity_input_config.txt
```

#### 新增的可视化功能：
- 支持LAS、PLY、PCD、XYZ等点云格式
- 自动检测文件格式并选择合适的加载方式
- 使用open3d进行3D可视化
- 支持鼠标交互（旋转、平移、缩放）

## 2. Unity脚本修改

### PowerLineExtractorManager.cs
- **脚本调用改变**：从调用 `Extractor4.py` 改为调用 `remote_runner.py`
- **新增开关配置**：`alwaysLoadBCsvAfterExtraction` - 控制是否总是加载B.csv
- **简化流程**：移除JSON到CSV的转换步骤
- **配置文件创建**：自动创建 `unity_input_config.txt` 供Python脚本读取

#### 新增配置项：
```csharp
[Tooltip("启用时，不管读取什么文件都在提取脚本跑完之后加载B.csv")]
[SerializeField] private bool alwaysLoadBCsvAfterExtraction = true;
```

#### 主要流程变化：
1. **第一阶段**：执行 `remote_runner.py` 进行远程电力线提取
2. **第二阶段**：根据开关配置决定是否自动加载B.csv
3. **输出处理**：不再需要处理JSON文件，直接使用B.csv

### PowerlineExtractionSceneBuilder.cs
- **新增开关配置**：`alwaysLoadBCsvAfterExtraction`
- **场景加载逻辑**：根据开关决定是否强制使用B.csv

#### 新增配置项：
```csharp
[Header("CSV加载配置")]
[Tooltip("启用时，不管读取什么文件都在提取脚本跑完之后加载B.csv")]
public bool alwaysLoadBCsvAfterExtraction = true;
```

## 3. 使用方法

### 启用新功能
1. 在Unity编辑器中，找到 `PowerLineExtractorManager` 组件
2. 勾选 `Always Load B CSV After Extraction` 选项
3. 在 `PowerlineExtractionSceneBuilder` 组件中，勾选 `Always Load B CSV After Extraction` 选项

### 运行流程
1. 选择任意LAS文件进行电力线提取
2. 系统自动调用 `remote_runner.py` 进行远程处理
3. 处理完成后，根据开关配置：
   - 如果启用：自动加载B.csv并构建场景
   - 如果禁用：使用原来的逻辑

### 点云可视化
- 在 `remote_runner.py` 执行完成后，会自动显示点云可视化窗口
- 支持鼠标交互操作
- 按 'Q' 键退出可视化

## 4. 文件依赖

### 必需文件
- `Assets/extract/remote_runner.py` - 主要的Python脚本
- `Assets/Resources/B.csv` - 电力塔坐标数据

### Python依赖
确保安装了以下Python库：
```bash
pip install -r requirements.txt
```

主要依赖：
- `open3d` - 点云可视化
- `laspy` - LAS文件读取
- `paramiko` - SSH连接
- `scp` - 文件传输

## 5. 注意事项

1. **网络连接**：`remote_runner.py` 需要连接到远程服务器
2. **文件路径**：确保B.csv文件存在于 `Assets/Resources/` 目录中
3. **Python环境**：确保Python环境中安装了所有必需的依赖库
4. **开关配置**：两个开关需要同时启用才能完全生效

## 6. 故障排除

### 常见问题
1. **Python脚本执行失败**：检查Python环境和依赖库
2. **B.csv文件未找到**：确保文件存在于正确位置
3. **可视化窗口不显示**：检查open3d安装和显示设置
4. **远程连接失败**：检查网络连接和服务器配置

### 调试信息
- 查看Unity控制台的详细日志输出
- 检查Python脚本的执行输出
- 验证配置文件的创建和内容
