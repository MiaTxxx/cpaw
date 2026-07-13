# AIUsage Windows 重构计划

## 目标

将 AIUsage 重建为 Windows 专用产品并交付 Windows 1.0。现有 Swift 工程仅作为行为参考；迁移依据是稳定的 JSON 契约、现有 161 个 Swift 后端测试，以及由这些测试冻结的 golden fixtures。禁止逐文件翻译 Swift 代码。

## 硬性约束

- 技术栈：.NET 8、WPF、MVVM；最低支持 Windows 10 22H2；先交付 x64，Windows 1.0 稳定后再启动 ARM64。
- 进程模型：WPF 前端、用户级后台守护进程、独立 ProxyHost 子进程三层隔离。
- UI 与守护进程仅通过用户专属 Named Pipe 上的双向 JSON-RPC 通信，不开放本地管理端口。
- Claude、Codex、OpenCode 的每个激活节点使用独立 Kestrel ProxyHost，并由守护进程通过 Windows Job Object 管理。
- 元数据、偏好、统计和代理日志使用 SQLite WAL；凭据使用 DPAPI CurrentUser 加密的独立 vault。
- 时间统一为带时区 ISO-8601，token 使用 64 位整数，金额保持现有 JSON 浮点语义，枚举必须容忍未知值。
- CPA 只使用官方 Windows ZIP，校验 SHA-256、压缩路径和架构，并通过原子指针切换与回滚版本。
- 使用签名的用户级安装器和 GitHub Releases 更新通道，常规运行不依赖管理员权限。
- 保留现有信息架构，使用 Windows 原生交互，不迁移 macOS 数据，也不做 Swift on Windows 兼容。

## 解决方案结构

| 项目 | 职责 |
|---|---|
| `AIUsage.Contracts` | UI 与守护进程共享的版本化 DTO、命令、事件和错误模型 |
| `AIUsage.Core` | 供应商、额度归一化、费用计算、账户身份、配置编辑和协议转换 |
| `AIUsage.Application` | 刷新、账户、代理节点、CPA 生命周期和通知规则编排 |
| `AIUsage.Infrastructure.Windows` | DPAPI、SQLite、进程、路径、浏览器、系统代理、通知和自启动适配 |
| `AIUsage.Daemon` | 用户级后台任务、持久化、刷新和代理子进程监管 |
| `AIUsage.ProxyHost` | Claude、Codex、OpenCode 的 Kestrel 代理与流式 SSE |
| `AIUsage.Desktop` | WPF 前端，只消费 Contracts 和守护进程快照 |

## 强制工作循环

1. 每个提交后，Windows 解决方案必须可构建，已有测试必须全部通过。
2. 迁移行为前，先从对应 Swift 测试冻结确定性且已脱敏的 golden fixture。
3. 平台能力先定义细粒度接口，再实现 Windows adapter，保持 Core/Application 可注入、可测试。
4. Provider 按家迁移，每家一个独立提交并配套测试。
5. Swift 行为与 Windows 约束冲突时，优先保持 JSON 契约兼容，并记录偏离理由。

## 里程碑

### 阶段 A：地基

- .NET solution、统一构建配置、xUnit 和 Windows x64 CI。
- Swift golden fixture 生成、确定性校验、脱敏扫描和清单。
- Contracts v1：dashboard、provider、account、usage、proxy JSON 往返测试。

### 阶段 B：纯逻辑

- 领域模型、额度归一化、费用聚合。
- Canonical 请求、响应、流式事件和协议转换。
- 账户身份、去重和弱身份保护。

### 阶段 C：平台与引擎

- 平台接口、DPAPI vault、SQLite、原子写入和日志脱敏。
- Provider Engine 的并发、超时、取消、部分失败和刷新快照。
- 按供应商迁移 API、OAuth、设备码、认证文件和本地账本。

### 阶段 D：Preview 1

- 守护进程、单实例、Named Pipe JSON-RPC、心跳与 UI 重连。
- WPF 导航、主题、本地化、托盘与关闭到后台。
- 账户、额度刷新、仪表盘、通知和基础设置。

### 阶段 E：Preview 2

- 独立 ProxyHost 与进程监管。
- Claude、Codex、OpenCode 三条代理轨道及配置接管、恢复和统计。

### 阶段 F：Preview 3

- CPA 安装、校验、更新、回滚、账户池、模型目录和安全同步。
- 调用分析、费用统计、热力图、日志保留和诊断导出。

### 阶段 G：Windows 1.0

- OAuth/WebView2、设备码、认证文件和 Token 导入。
- 自启动、Toast、系统代理检测、自动更新和故障恢复。
- 安装、升级、卸载保留数据、性能、安全及 Windows 10/11 x64 回归。

## Windows 1.0 验收

1. 额度监控、账户管理、三类代理、CPA Gateway、统计、托盘、登录、通知和自动更新全部可用。
2. UI 退出后，刷新和已激活代理继续运行。
3. UI 或单个代理进程崩溃不破坏配置和其他代理轨道。
4. 所有凭据受 DPAPI 保护，日志、数据库元数据和 RPC 不泄露明文密钥。
5. Windows 10 22H2 与 Windows 11 x64 均通过安装和核心功能回归。
6. 主要转换与归一化结果通过 golden 差分证明与 Swift 行为兼容。

## Windows 1.0 明确不做

- 不迁移 macOS Keychain、UserDefaults 或历史数据库。
- 不抓取 Chromium Cookie，不实现 Claude Science，不自动安装系统根证书。
- 没有安全 Windows 数据源的供应商明确显示“当前平台暂不支持”。
- 不开发 macOS 新功能，不兼容 Swift on Windows，不复刻 macOS 像素级界面。
