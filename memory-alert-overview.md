# CtrlV 新功能：内存占用过高托盘图标闪烁提醒

## 功能说明
- 系统物理内存占用超过阈值（默认 90%，可调 10~99）时，托盘图标变红色闪烁（亮红/暗红交替）
- 首次触发时弹一次 Windows 气泡通知；之后只闪图标不重复打扰
- 内存降到阈值 -2 以下时图标自动恢复蓝色（滞回防抖，避免卡在 89~90% 时图标乱抖）
- 设置窗口新增开关与阈值输入框，保存后立即生效（无需重启）
- 默认关闭，不开启时程序零开销

## 轻量化设计（为什么不会拖慢常驻程序）
| 项目 | 做法 | 开销 |
|---|---|---|
| 测内存 | Win32 API `GlobalMemoryStatusEx`，不引 NuGet 包 | 单次约 1 微秒 |
| 检测频率 | DispatcherTimer 每 10 秒，未启用则不启动 | CPU 占用测不出来 |
| 图标 | 启动时预生成 3 个缓存（蓝/亮红/暗红），切换只是赋值 | 每个图标几 KB，零内存分配 |
| 代码量 | 新文件约 190 行 + 各处接线十几行 | exe 体积增加 < 5KB |

## 改动文件清单
1. **`CtrlV/Services/MemoryMonitor.cs`（新建）**：监控核心。定时检测、状态机（正常/报警）、图标绘制与切换、气泡提醒
2. **`CtrlV/Services/SettingsManager.cs`**：AppSettings 加 `memoryAlertEnabled`、`memoryAlertThreshold` 两个字段，旧 settings.json 自动兼容
3. **`CtrlV/App.xaml.cs`**：启动时按设置初始化监控；设置窗口关闭后热生效；退出时释放资源。蓝图标绘制改为复用 `MemoryMonitor.CreateCircleIcon`，消除重复代码
4. **`CtrlV/SettingsWindow.xaml / .cs`**：新增"内存占用过高时托盘图标闪烁提醒"复选框 + 阈值输入框（带范围校验 10~99）

## 编译结果
`dotnet build --no-restore` 通过，0 警告 0 错误。

## 实测修复记录（老杨验收反馈）
1. **图标不闪烁**（v1 初版）：闪烁帧切换误绑在 10 秒检测定时器上，肉眼看不到变化。已拆出独立 500ms 闪烁定时器，仅报警期间运行。
2. **右键退出报错 "Cannot access a disposed object: 'Icon'"**：释放顺序错误——先销毁了图标再释放 NotifyIcon，而后者内部还要访问 Icon。已改为：停定时器 → 释放 NotifyIcon → 销毁图标，且 `Shutdown()` 放入 finally 保证必执行。

## 已知边界 / 后续可选项
- 检测间隔固定 10 秒（未做成可调——内存变化缓慢，10 秒足够且最省资源）
- 气泡只在"从正常→报警"的跳变时弹一次；解除后再次越阈会再提醒一次
- 如果以后想让"解除报警"也弹气泡，只需在 `MemoryMonitor.Check()` 恢复分支加一行 `SafeBalloon`
