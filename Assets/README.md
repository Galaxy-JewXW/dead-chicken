# 电力线可视化系统

> 基于Unity的专业电力线三维可视化和巡检仿真系统，集成了完整的从点云到三维重建的电力线提取流程

## 🎯 系统概述

本系统提供从LAS点云到三维电力线重建、可视化、分析、交互的端到端解决方案，集成Python算法与Unity 3D引擎，支持大规模点云实时处理。系统具备完整的电力线巡检仿真功能，包括无人机自动巡检、危险标记、测量工具、场景总览等专业功能。

## 🚀 核心功能亮点

### 🔍 点云处理与电力线提取
- **一键上传LAS点云**：支持拖拽上传，自动格式验证
- **智能电力线提取**：基于Python机器学习算法自动识别电力线结构
- **点云预览功能**：上传后可直接预览点云，支持旋转、缩放、平移
- **数据格式转换**：LAS→OFF→Unity，支持多种点云格式

### 🏗️ 三维重建与可视化
- **自动电力塔建模**：基于提取结果自动生成三维电力塔
- **真实物理弧垂**：电线下垂效果符合物理规律
- **精确引脚连接**：8根导线精确连接到电塔引脚位置
- **动态缩放系统**：根据高度自动缩放电塔模型
- **地形适配**：自动适应地形高度变化

### 🎮 交互与巡检功能
- **多视角相机系统**：第一人称、上帝视角、飞行视角
- **无人机自动巡检**：全线路自动巡检，支持暂停/继续/停止
- **测量工具**：多点连续测量，实时距离计算
- **危险标记系统**：4种危险类型，3个危险等级
- **电力线标记**：为电力线添加自定义标记和备注

### 📊 场景管理与分析
- **场景总览系统**：全局拓扑图，电塔交互，统计信息
- **电力线信息面板**：长度、宽度、弯曲度、状态管理
- **电塔概览**：电塔列表、状态监控、详细信息
- **实时统计**：电塔数量、线路长度、危险物统计

### 🎨 UI与用户体验
- **现代化UI Toolkit**：Material Design风格，响应式布局
- **多模式切换**：相机、测量、危险标记、电力线信息等模式
- **字体自定义**：支持多种字体，运行时切换
- **主题系统**：统一颜色主题，可自定义配置

## 📂 项目结构

```
Assets/
├── Scripts/                    # 主要C#代码
│   ├── PointCloud/            # 点云处理模块
│   │   ├── PowerLineExtractorManager.cs    # 电力线提取管理器
│   │   ├── PointCloudViewer.cs             # 点云查看器
│   │   ├── PowerlinePointCloudManager.cs   # 点云管理器
│   │   └── PowerlineExtractionSceneBuilder.cs # 场景构建器
│   ├── Powerline/             # 电力线核心模块
│   │   ├── SceneInitializer.cs             # 场景初始化器
│   │   ├── TowerPinpointSystem.cs          # 引脚连接系统
│   │   ├── PowerlineInteraction.cs         # 交互控制器
│   │   └── PowerlineMarkingSystem.cs       # 标记系统
│   ├── UI/                    # 用户界面模块
│   │   ├── SimpleUIToolkitManager.cs       # 主UI管理器
│   │   ├── UIToolkitMeasureController.cs   # 测量控制器
│   │   ├── UIToolkitDangerController.cs    # 危险标记控制器
│   │   ├── SceneOverviewManager.cs         # 场景总览管理器
│   │   ├── DronePatrolManager.cs           # 无人机巡检管理器
│   │   └── FontManager.cs                  # 字体管理器
│   ├── Camera/                # 相机控制模块
│   │   ├── CameraManager.cs                # 多视角管理器
│   │   ├── FirstPersonCamera.cs            # 第一人称相机
│   │   ├── GodViewCamera.cs                # 上帝视角相机
│   │   └── FlyCamera.cs                    # 飞行相机
│   ├── Materials/             # 材质管理模块
│   │   └── PowerlineMaterialManager.cs     # 电力线材质管理器
│   ├── Terrain/               # 地形管理模块
│   │   └── TerrainManager.cs               # 地形管理器
│   └── Components/            # 通用组件模块
│       └── MarkerPrefab.cs                 # 标记组件
├── extract/                   # Python电力线提取
│   ├── Extractor4.py                      # 电力线提取器
│   ├── main_pipeline.py                   # 主流程脚本
│   └── extract_tower_coordinates.py       # 电塔坐标提取
├── PyPLineExtractor/          # Python提取算法
│   ├── Generator.py                       # 主生成器
│   ├── Extractor.py                       # 核心算法
│   └── requirements.txt                   # Python依赖
├── las2off/                   # LAS转换工具
│   ├── las2off.py                         # 转换脚本
│   └── requirements.txt                   # 依赖列表
├── Resources/                 # 资源文件
│   ├── pointcloud/           # 点云数据
│   ├── Prefabs/              # 预制件
│   ├── Materials/            # 材质资源
│   └── Styles/               # UI样式
├── Fonts/                     # 字体文件
├── Scenes/                    # Unity场景
└── Shaders/                   # 自定义着色器
```

## 🔧 详细功能说明

### 1. 点云处理与电力线提取

#### 点云上传与预览
- 支持LAS格式点云文件上传
- 自动格式验证和依赖检查
- 点云预览窗口，支持3D交互
- 统计信息显示（点数、网格数等）

#### 电力线提取流程
```
LAS点云 → Python提取 → JSON结果 → CSV转换 → Unity三维重建
```

**Python算法特性**：
- 线性特征分析和DBSCAN聚类
- 动态参数调整，适应复杂地形
- 智能断裂线段合并
- 长度筛选和质量控制

### 2. 三维重建系统

#### 电力塔建模
- **自动创建**：基于CSV数据自动生成电力塔
- **动态缩放**：根据高度自动计算缩放比例
- **引脚系统**：8个精确引脚位置（上层4个地线，下层4个主导线）
- **地形适配**：自动贴合地面高度

#### 电力线渲染
- **物理弧垂**：基于正弦函数的真实下垂效果
- **多线类型**：地线(12.6mm)、主导线(28.6mm)
- **材质系统**：金属材质，高光反射
- **LOD优化**：距离级别细节优化

### 3. 交互与巡检功能

#### 相机控制系统
- **第一人称视角(F1)**：WASD移动，鼠标控制，跳跃功能
- **上帝视角(F2)**：拖拽缩放，边界限制，全局浏览
- **飞行视角(F3)**：自由飞行，无地形限制

#### 无人机巡检系统
- **自动巡检**：按电塔顺序自动巡检
- **智能路径**：侧边观察，3.8倍电塔高度
- **交互控制**：暂停/继续/停止，ESC退出
- **视角控制**：暂停时可自由旋转视角

#### 测量工具
- **多点测量**：连续点击添加测量点
- **实时计算**：自动计算距离和总长度
- **可视化**：测量线条和标记点
- **详细信息**：段落距离和总距离显示

#### 危险标记系统
- **4种类型**：建筑物、植被、设备、其他
- **3个等级**：低、中、高危险
- **双击创建**：双击地面创建标记
- **详细信息**：类型、等级、描述编辑

### 4. 场景管理与分析

#### 场景总览系统
- **全局拓扑图**：电塔和电力线可视化
- **交互功能**：缩放、拖拽、电塔悬停
- **跳转功能**：点击电塔跳转到3D场景
- **统计信息**：电塔数量、线路长度、危险物统计

#### 电力线信息管理
- **物理参数**：长度、宽度、弯曲度
- **状态管理**：优秀/良好/需要维护
- **标记系统**：自定义标记和备注
- **时间记录**：状态设置时间自动记录

#### 电塔概览系统
- **电塔列表**：所有电塔的详细信息
- **状态监控**：正常/警告/异常状态
- **搜索功能**：按名称或ID搜索
- **详细信息**：位置、高度、连接信息

### 5. UI系统

#### 现代化界面
- **UI Toolkit**：基于Unity最新UI框架
- **Material Design**：现代化设计风格
- **响应式布局**：适配不同屏幕尺寸
- **主题系统**：统一颜色和字体配置

#### 多模式切换
- **相机模式**：相机控制界面
- **测量模式**：测量工具界面
- **危险标记模式**：危险物管理界面
- **电力线信息模式**：电力线详情界面
- **场景总览模式**：全局视图界面

#### 字体系统
- **字体管理**：FontManager统一管理
- **多字体支持**：思源黑体、微软雅黑、苹方等
- **运行时切换**：支持动态字体切换
- **自动应用**：所有UI元素自动应用字体

## 📊 支持的数据格式

### B.csv格式（分组连线）
```
group_id,order,x,y,z,line_count
0,0,293.72,336.85,4.93,2
0,1,291.10,327.31,7.75,2
1,0,-366.53,76.75,5.99,4
```
- **分组连线**：同组内按order顺序连线，不同group不连线
- **坐标转换**：X,Y→Unity的X,Z，Z→Y（高度），自动缩放×10并居中
- **文件位置**：必须放在Assets/Resources/目录下

### 其他格式
- **LAS点云**：标准LAS 1.2格式
- **JSON结果**：Python提取输出格式
- **CSV坐标**：电塔位置坐标文件

## 🛠️ 环境依赖

### Python环境
```bash
# 需要Python 3.11版本
pip install laspy numpy open3d scikit-learn scipy tqdm
```

### Unity环境
- Unity 2022.3 LTS 或更高版本
- 支持的图形API: DirectX 11/12, OpenGL, Vulkan
- 最低内存要求: 8GB RAM
- 推荐配置: 16GB RAM, 独立显卡

## 🚀 快速开始

### 1. 环境设置
```bash
# 克隆项目
git clone [repository-url]
cd powerline-visualization

# 安装Python依赖
pip install -r PyPLineExtractor/requirements.txt
pip install -r las2off/requirements.txt
```

### 2. Unity项目设置
1. 启动Unity Hub，添加项目
2. 打开Assets/Scenes/SampleScene.unity
3. 确保所有组件正确配置

### 3. 基本使用流程
1. **上传点云**：点击"点云"按钮，选择LAS文件
2. **预览点云**：点击"预览点云"查看点云数据
3. **提取电力线**：点击"提取电力线"开始处理
4. **交互体验**：使用各种工具进行巡检和分析

### 4. 快捷键操作
- **F1/F2/F3**：切换相机视角
- **ESC**：停止无人机巡检
- **鼠标滚轮**：缩放视图
- **右键拖拽**：旋转视角
- **双击地面**：创建危险标记

## 🔧 高级功能

### 无人机巡检配置
```csharp
// 在DronePatrolManager中配置
public float droneSpeed = 2f;        // 巡检速度
public float droneHeight = 3.8f;     // 高度比例
public float droneDistance = 25f;    // 侧边距离
```

### 电力线提取参数
```csharp
// 在PowerLineExtractorManager中配置
public int minLinePoints = 50;       // 最小线点数
public float minLineLength = 200.0f; // 最小线长度
public float targetTowerHeight = 10f; // 目标电塔高度
```

### UI主题自定义
```csharp
// 在SimpleUIToolkitManager中配置
public Color primaryColor = new Color(0.39f, 0.4f, 0.95f, 1f);
public Font fallbackFont;            // 备用字体
```

## 🐛 常见问题与解决方案

### Python环境问题
**Q: Python环境检查失败？**
```bash
# 检查Python版本（需要3.11）
python --version

# 安装依赖
pip install laspy numpy open3d scikit-learn scipy tqdm

# 如果权限问题，使用--user参数
pip install --user laspy numpy open3d scikit-learn scipy tqdm
```

### 电力线提取问题
**Q: 提取结果为空？**
- 检查LAS文件是否包含电力线数据
- 降低minLinePoints参数（如30）
- 降低minLineLength参数（如100.0f）
- 确保点云质量足够高

### 显示问题
**Q: 电力塔/电力线不显示？**
- 检查CSV文件格式和位置
- 确认GoodTower.prefab在Resources/Prefabs/目录
- 查看Console错误信息
- 检查材质配置

### 性能问题
**Q: 系统运行缓慢？**
- 降低点云密度
- 启用LOD系统
- 调整渲染参数
- 使用更高性能硬件

### UI问题
**Q: 字体显示异常？**
- 检查字体文件是否正确导入
- 确认FontManager配置
- 使用备用字体设置
- 重启Unity编辑器

## 📈 性能优化建议

### 点云处理优化
- 使用LOD系统减少渲染负担
- 分批处理大型点云文件
- 启用GPU Instancing
- 优化材质和着色器

### 内存管理
- 及时清理不需要的点云数据
- 使用对象池管理UI元素
- 避免频繁创建销毁对象
- 监控内存使用情况

### 渲染优化
- 使用Occlusion Culling
- 启用Frustum Culling
- 优化光照设置
- 使用LOD系统

## 🤝 开发与扩展

### 代码规范
- **C#**：遵循Microsoft C#编码规范
- **Python**：遵循PEP 8规范
- **注释**：使用中文注释，保持代码可读性
- **命名**：使用有意义的变量和函数名

### 扩展开发
- 模块化设计，易于扩展
- 标准化接口，支持插件
- 事件驱动架构
- 配置化参数管理

### 测试建议
- 单元测试覆盖核心算法
- 集成测试验证模块协作
- 性能测试确保流畅运行
- 用户测试验证易用性

## 📄 许可证

本项目基于MIT许可证开源。详见 [LICENSE](LICENSE) 文件。

## 📞 支持与反馈

如有问题或建议，请通过以下方式联系：
- **Issues**：在GitHub上提交Issue
- **文档**：查看项目Wiki页面
- **邮件**：[contact@email.com]

---

## 🔄 更新日志

### v2.1.0 - 完整功能版本
- ✨ 新增无人机巡检系统
- ✨ 新增场景总览功能
- ✨ 新增电力线标记系统
- ✨ 完善测量和危险标记功能
- 🔧 优化UI系统和用户体验
- 🐛 修复多个已知问题

### v2.0.0 - 电力线提取集成版本
- ✨ 新增完整的电力线提取和三维重建流程
- 🔧 集成Python机器学习算法
- 🎮 更新UI界面，添加"提取电力线"功能
- 📊 支持LAS点云文件直接处理
- 🏗️ 自动电力塔建模和电力线生成

### v1.0.0 - 基础可视化版本
- 🎯 基础电力线三维可视化功能
- 🏗️ 电力塔和电力线建模
- 🎮 基础交互和相机控制
- 📊 CSV数据导入支持

---

**电力线可视化系统** - 让电力线检查更智能、更直观！ ⚡🔌

> 如需详细用法、参数说明、常见问题等，请参见各目录下README.md。

## 📚 详细文档

### AI助手系统相关文档

#### AI API使用说明
- **文件位置**: `AI_API_使用说明.md`
- **功能描述**: AI助手系统的API接口使用方法和配置说明
- **主要特性**:
  - Python脚本路径配置
  - API密钥设置
  - 错误处理和调试

#### AI助手Python脚本路径修复
- **文件位置**: `AI助手Python脚本路径修复说明.md`
- **问题描述**: 解决AI助手在打包后Python脚本路径错误的问题
- **解决方案**:
  - 使用`Application.streamingAssetsPath`动态路径
  - 实现多路径查找策略
  - 参考电力线提取模块的路径处理方式

#### AI助手启动显示控制
- **文件位置**: `AI助手启动显示控制说明.md`
- **功能描述**: 控制AI助手在系统启动时的显示行为
- **配置选项**:
  - `showChatPanelOnStart`: 控制聊天面板是否在启动时显示
  - `showAIAssistantPanelOnStart`: 控制AI助手面板是否在启动时显示
  - `showGreenBubbleButton`: 控制绿色气泡按钮的显示

#### AI助手路径修复完成总结
- **文件位置**: `AI助手路径修复完成总结.md`
- **修复内容**:
  - Python脚本路径问题解决
  - 多路径查找机制实现
  - 打包后路径兼容性验证

#### Python脚本路径设置总结
- **文件位置**: `Python脚本路径设置总结.md`
- **技术要点**: 
  - StreamingAssets目录的使用
  - 动态路径解析策略
  - Unity构建环境下的路径处理

#### 电力线提取Python脚本路径优化
- **文件位置**: `电力线提取Python脚本路径优化说明.md`
- **优化内容**: 电力线提取操作的Python脚本路径处理优化
- **主要改进**:
  - 优先使用StreamingAssets路径，确保打包后兼容性
  - 实现多路径查找策略，与AI助手和点云预览保持一致
  - 添加输出路径的备用方案和错误处理

### 系统功能文档

#### 无人机巡检智能路径规划
- **文件位置**: `无人机巡检智能路径规划功能说明.md`
- **功能描述**: 无人机自动巡检系统的路径规划和执行
- **主要特性**:
  - 智能路径生成算法
  - 多电塔巡检序列
  - 实时路径调整和优化

#### 树木危险监测系统
- **文件位置**: `树木危险监测系统使用说明.md`
- **功能描述**: 自动检测和标记危险树木的系统
- **监测功能**:
  - 高度危险检测
  - 距离电力线安全距离计算
  - 危险等级分类和标记

#### 电力线可视化系统技术文档
- **文件位置**: `电力线可视化系统技术文档.md`
- **技术架构**:
  - 点云处理流程
  - 三维重建算法
  - 实时渲染优化

#### 统计大屏构建错误修复
- **文件位置**: `统计大屏构建错误修复说明.md`
- **修复内容**:
  - UI构建错误解决方案
  - 字体和样式问题处理
  - 性能优化建议

### 删除气泡UI说明
- **文件位置**: `删除气泡UI说明.md`
- **问题描述**: 系统启动时出现不需要的气泡UI
- **解决方案**: 
  - 配置控制绿色气泡按钮显示
  - 保留顶栏AI助手按钮
  - UI层级管理优化

## 📋 文档整合完成说明

### 整合概述
为了简化项目结构，提高文档的可维护性，所有分散的`.md`文件已整合到各个文件夹的`README.md`中。

### 整合完成情况

#### ✅ 已整合的文档
1. **根目录文档** → 主`README.md`
   - AI助手系统相关文档
   - 系统功能文档
   - 技术文档和说明

2. **Scripts/UI文档** → `Scripts/UI/README.md`
   - AI助手系统使用说明
   - UI界面优化总结
   - 树木危险监测相关文档
   - 统计大屏和用户认证系统文档

3. **Scripts/Camera文档** → `Scripts/Camera/README.md`
   - 视角切换自动寻找电塔功能说明

4. **las2off文档** → `las2off/README.md`
   - 安装指南和详细使用说明

#### 📁 当前文档结构
```
Assets/
├── README.md                           # 主文档（包含所有根目录文档整合）
├── Scripts/
│   ├── README.md                       # 脚本模块总览
│   ├── UI/README.md                    # UI系统文档（包含所有UI相关文档整合）
│   ├── Camera/README.md                # 相机模块文档（包含视角切换功能说明）
│   ├── PointCloud/README.md            # 点云处理模块文档
│   ├── Powerline/README.md             # 电力线核心模块文档
│   ├── Terrain/README.md               # 地形管理模块文档
│   ├── Materials/README.md             # 材质管理模块文档
│   └── Components/README.md            # 通用组件模块文档
├── extract/README.md                   # 电力线提取系统文档
└── las2off/README.md                   # LAS转换工具文档（包含安装指南整合）
```

#### 🗑️ 已删除的分散文档
- 根目录下的所有功能说明.md文件
- Scripts/UI下的所有功能说明.md文件
- Scripts/Camera下的功能说明.md文件
- las2off下的INSTALL_GUIDE.md文件

### 整合优势
1. **结构清晰**: 每个模块的文档集中在对应的README.md中
2. **易于维护**: 减少文件数量，避免文档分散
3. **查找方便**: 用户可以在对应模块文件夹中找到完整文档
4. **版本控制**: 文档与代码模块同步更新

### 使用建议
- **查找特定功能文档**: 直接查看对应模块文件夹下的README.md
- **了解系统整体**: 查看根目录的README.md
- **模块开发**: 参考对应模块的README.md了解技术细节
- **问题排查**: 各模块README.md中包含常见问题和解决方案

### 维护说明
- 新增功能时，请将文档添加到对应模块的README.md中
- 更新文档时，确保README.md保持最新状态
- 删除功能时，同步更新对应的README.md
- 定期检查文档的完整性和准确性 

## 🔧 电力线提取流程优化

### 修改说明
- **移除地形提取功能**：删除了`worker.py`和`RawTerrainImporter.cs`，不再生成地形RAW文件
- **直接使用Extractor4.py**：电力线提取现在直接调用`Extractor4.py`脚本，而不是通过`worker.py`
- **JSON到CSV转换**：使用`extract_tower_coordinates.py`脚本将`Extractor4.py`生成的JSON文件转换为CSV格式

### 新的工作流程
1. **第一阶段**：执行`Extractor4.py`进行电力线提取
   - 输入：LAS点云文件
   - 输出：`*_powerline_endpoints.json`（电力线端点信息）
   - 输出：`*_extracted_powerlines.las`（提取的电力线点云）

2. **第二阶段**：使用`extract_tower_coordinates.py`转换格式
   - 输入：`*_powerline_endpoints.json`
   - 输出：`*_tower_coordinates.csv`（电力塔坐标，符合B.csv格式）

3. **文件复制**：将生成的CSV文件复制到Unity的Resources目录

### 脚本参数说明
- **Extractor4.py**：`python Extractor4.py <input_las_file>`
- **extract_tower_coordinates.py**：`python extract_tower_coordinates.py <input_json> <output_csv> [eps] [min_samples] [target_z_mean]`

### 注意事项
- 确保`extract/`目录中包含`Extractor4.py`和`extract_tower_coordinates.py`脚本
- 脚本会在当前工作目录生成输出文件
- 如果找不到`extract_tower_coordinates.py`脚本，会跳过CSV转换步骤 