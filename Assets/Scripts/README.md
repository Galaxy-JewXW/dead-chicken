# 电力线可视化系统

一个完整的Unity电力线可视化解决方案，支持真实的电塔模型、动态缩放和智能连接。

> 📖 **完整使用指南请查看：[电力线可视化系统完整指南.md](../电力线可视化系统完整指南.md)**

## 核心功能

### 🏗️ 智能电塔系统
- **动态缩放**：根据电线高度自动计算电塔缩放比例
- **精确连接**：电线连接点位于电塔80%高度位置
- **模型支持**：支持自定义3D电塔模型（如GoodTower.prefab）
- **地形适配**：自动适应地形高度变化

### ⚡ 增强电力线渲染
- **物理弧垂**：真实的电线下垂效果
- **多线类型**：支持主导线、地线、OPGW等
- **LOD优化**：距离级别细节优化
- **材质系统**：可配置的电线材质和颜色

### 🎮 交互功能
- **危险标记**：点击标记危险区域
- **距离测量**：鼠标点击测量距离
- **相机控制**：多种相机模式（第一人称、俯视、飞行）

## 快速开始

### 1. 基础设置
```csharp
// 在场景中添加 EnhancedPowerlineRenderer 组件
// 配置电塔预制体和电线材质
```

### 2. 使用GoodTower电塔
1. 创建空物体，添加 `GoodTowerSetup` 脚本
2. 将 `GoodTower.prefab` 拖拽到 `Tower Prefab` 字段
3. 点击 `Setup Good Tower System` 按钮

### 3. 常见问题解决
- **电塔位置错误**：右键 `GoodTowerSetup` → "修正现有电塔位置"
- **连接不正确**：右键 `GoodTowerSetup` → "重新生成所有电塔"

## 主要组件

### EnhancedPowerlineRenderer
核心渲染器，负责电力线和电塔的创建与管理。

**关键参数**：
- `csvFileName`: CSV数据文件名
- `towerPrefab`: 电塔预制体
- `useSimpleTower`: 是否使用简单电塔
- `enableSag`: 启用弧垂效果

### GoodTowerSetup
一键配置工具，简化GoodTower电塔的设置过程。

**功能**：
- 自动配置所有相关组件
- 修正电塔位置问题
- 重新生成电塔系统

### SceneInitializer
场景初始化器，支持地形适配和多段电力线。

## 技术特点

### 动态缩放算法
```csharp
// 根据电线高度计算电塔缩放
float requiredTowerHeight = wireHeight / 0.8f;  // 连接点在80%高度
float scale = requiredTowerHeight / originalTowerHeight;
```

### 位置修正系统
```csharp
// 确保电塔底部贴在地面上
float heightOffset = actualTowerHeight * 0.5f;
tower.transform.position = groundPosition + Vector3.up * heightOffset;
```

## 数据格式

CSV文件格式（位于Resources文件夹）：
```
x,y,z
100.0,50.0,200.0
150.0,45.0,250.0
```

支持多段电力线，用空行分隔不同线路。

## 性能优化

- **LOD系统**：根据距离调整细节
- **批量渲染**：优化大量电线的渲染
- **智能更新**：只在需要时重新计算

## 扩展功能

### 相机系统
- `FirstPersonCamera`: 第一人称视角
- `GodViewCamera`: 俯视视角  
- `FlyCamera`: 自由飞行

### UI系统
- `PowerlineUIManager`: 电力线控制面板
- `DangerUI`: 危险标记界面

### 地形系统
- `TerrainManager`: 地形高度查询和适配

## 开发说明

本系统经过多次优化，已实现：
- ✅ 电塔-电线完美连接
- ✅ 动态缩放自动计算
- ✅ 位置偏移自动修正
- ✅ 一键配置和修复

适用于电力系统可视化、工程仿真、教育演示等场景。

## 字体设置与推荐

本项目支持自定义和统一字体，提升界面美观性和可读性。

### 字体设置方法
1. 将.ttf或.otf字体文件放入`Assets/Fonts/`或`Assets/Resources/Fonts/`目录。
2. 在Unity Inspector中设置FontManager组件，拖入主字体。
3. 支持运行时通过FontManager动态切换字体。

### 推荐字体
- 思源黑体（Source Han Sans）：现代、支持中日韩
- 微软雅黑：Windows系统自带，清晰易读
- 苹方字体：macOS系统字体，优雅美观
- Noto Sans：Google开源，多语言支持

### 字体切换示例
```csharp
Font newFont = Resources.Load<Font>("Fonts/你的字体名称");
FontManager.Instance.ChangeFont(newFont);
```

### 常见问题
- 字体不显示：检查字体文件是否导入、是否支持所需字符
- 中文异常：确保字体支持中文，Character设置为Unicode
- 字体大小不一致：检查FontManager配置

### 注意事项
- 字体文件过大影响包体积，建议子集化
- 商业项目请注意字体授权
- 避免频繁切换字体，建议预加载