# Quadono β0.3.0 新功能测试指南

## 核心功能改进

### 1. 任务管理增强

#### 添加任务（支持描述和截止日期）
```bash
# 基本添加
quadono add "完成任务管理系统" 1 60 "实现编辑功能"

# 添加带截止日期的任务
quadono add "年终总结" 2 120 "写年度工作总结" --due=12-31
```

#### 任务编辑
```bash
# 编辑任务标题
quadono edit abc123 "新标题"

# 编辑多个属性
quadono edit abc123 "新标题" 3 90 "新描述"
```

#### 任务状态管理
```bash
# 开始任务
quadono start abc123

# 暂停任务  
quadono pause abc123

# 标记完成
quadono done abc123
```

#### 增强的任务列表
```bash
# 按象限筛选
quadono list --quadrant=1

# 按状态筛选
quadono list --status=inprogress

# 查看今日任务
quadono list --today

# 查看所有任务（包括已完成）
quadono list --all

# 组合筛选
quadono list --quadrant=2 --status=pending
```

#### 四象限矩阵视图
```bash
# 可视化四象限矩阵
quadono matrix
```

### 2. 番茄钟功能完善

#### 新的番茄钟命令
```bash
# 开始番茄钟
quadono pom start

# 开始番茄钟并关联任务
quadono pom start --task=abc123

# 停止番茄钟
quadono pom stop

# 查看番茄钟状态
quadono pom status

# 查看统计信息
quadono pom stats
```

### 3. 重要日期增强

#### 支持具体年份
```bash
# 添加一次性重要日（支持年份）
quadono important add "高考" exam critical 2025-06-07 "普通高等学校招生考试"

# 添加年度重要日（无年份）
quadono important add "生日" other normal 08-15 "我的生日"
```

#### 提前提醒功能
```bash
# 添加带提醒的重要日
quadono important add "CSP复赛" contest veryimportant 2024-10-20 "CCF计算机软件能力认证" --remind=30,7,1
```

### 4. 节日管理增强

#### 支持农历节日
```bash
# 添加农历节日（功能开发中）
quadono holidays add "春节" chinese --lunar 1 1 "农历新年"
```

## 使用示例

### 完整工作流程示例

1. **创建任务**
```bash
quadono add "完成项目报告" 1 90 "Q4季度总结报告" --due=12-15
quadono add "学习新技术" 2 60 "学习.NET 8.0新特性"
quadono add "回复邮件" 3 15 "处理客户询问"
quadono add "整理桌面" 4 10 "清理工作环境"
```

2. **查看四象限矩阵**
```bash
quadono matrix
```

3. **开始工作**
```bash
# 找到重要且紧急的任务ID
quadono list --quadrant=1

# 开始任务
quadono start abc123

# 开始番茄钟
quadono pom start --task=abc123
```

4. **工作期间**
```bash
# 查看番茄钟状态
quadono pom status

# 如果需要暂停
quadono pause abc123
quadono pom stop
```

5. **完成后**
```bash
# 标记任务完成
quadono done abc123

# 查看统计
quadono pom stats
```

## 改进亮点

1. **任务编辑功能** - 终于支持修改任务属性
2. **状态管理** - 可以开始、暂停任务
3. **增强筛选** - 多维度任务筛选
4. **四象限可视化** - 直观的矩阵视图
5. **完善的番茄钟** - start/stop/status/stats完整功能
6. **年份支持** - 重要日可以指定具体年份
7. **提醒功能** - 支持多次提前提醒
8. **描述字段** - 任务可以添加详细描述

## 注意事项

- 任务ID可以使用前8位或者完整标题
- 番茄钟支持ESC键提前终止
- 重要日期的提醒天数会显示在描述中
- 四象限矩阵会自动调整显示大小