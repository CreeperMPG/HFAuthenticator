# HF Authenticator 宏帆验证器

HF Authenticator 是一个用于自动登录 `HFXYAP` 上网认证系统的 Windows 应用，用于在希沃等设备中保持 Internet 连接并共享网络。

项目基于 `.NET Framework 4.7.2`。

## 主窗口截图

![主窗口](https://s41.ax1x.com/2026/03/22/peuVbq0.png)

## 主要功能

- 自动周期性登录认证（支持立即运行一次或按间隔自动运行）
- 本地配置持久化（`%APPDATA%\HFAuthenticator\config.json`）
- 支持控制系统移动热点（开启/关闭、启动时自动打开）
- 托盘图标与静默启动（使用 `-silent` 参数）
- 日志记录与清除（`%APPDATA%\HFAuthenticator\autolog.log`）

## 快速上手

1. 启动程序
   - 双击 `HFAuthenticator.exe` 启动。
   - 如果需要程序后台运行，使用命令行传入 `-silent` 参数启动，此时程序会只在系统托盘运行。
     - *可以将 `HFAuthenticator.exe -silent` 添加到系统启动项（系统设置 → 启动应用）以实现开机自动运行。*

2. 填写基本信息（主窗口上）
   - `自动登录` 开关：打开后程序会按设置的频率自动尝试登录；关闭则不会自动登录。
   - `用户名` / `密码`：填你的认证账号和密码（~~这个要自己想办法搞~~）。
   - `登录频率`：这是自动登录的时间间隔。用滑块调节，默认是 600 秒（10 分钟），你可以把它改短到2分钟或改长到2小时。
     - 推荐：如果你希望确保在线，设置为 10 分钟到 30 分钟之间；如果在线状态已稳定，可以，设置为几十分钟或几小时。
     - 不建议将频率设置得过短（比如 2 分钟）。过于频繁的登录可能会被系统限制或导致不必要的网络流量。
   - `热点设置` 卡片：如需共享网络，可以在此卡片中开启热点，开启“开启应用时自动打开热点”后会在每次启动程序时自动打开热点（建议配合系统自启动）。
   - **【高级】**`认证系统 IP 地址`：填认证网关的 IP（默认是 `172.16.255.2`）。一般不会更改。

3. 保存配置
   - 改完上述设置后，点击窗口的 `保存` 按钮，配置会写入到 `\%APPDATA\%\HFAuthenticator\config.json`。

4. 托盘使用
   - 程序启动后会在系统托盘显示一个图标。托盘图标右键菜单有 `Show` / `Hide` / `Exit`。
   - 关闭主窗口只会把窗口隐藏到托盘（程序仍在后台运行），要完全退出请右键托盘选择 `Exit`。

5. 日志与查看
   - 程序把运行记录写到 `\%APPDATA\%\HFAuthenticator\autolog.log`。
   - 主界面有 `查看日志` 功能可以直接打开日志对话框，便于排查问题。
   - 建议定期清理日志文件（可以在日志对话框中点击 `清除日志` 按钮），避免日志过大占用磁盘空间。

6. 热点控制（如果需要）
   - 程序可以尝试打开/关闭系统移动热点。
   - *需要提前在系统设置中设置热点 SSID 与密码，建议关闭省电模式以持久运行。*
   - *热点功能依赖你的 Windows 版本与网卡驱动，某些机器/权限下可能不可用或需要管理员权限。*

## 常见问题与注意事项
- 如果自动登录失败，先确认 `认证 IP 地址` 是否正确、能否能够 ping 通。
- 如果开启了热点但无法启动，请尝试以管理员权限运行程序或手动检查系统的热点设置。
- ~~这是初2026级的入做的，如果你是几年后看到这个项目那可能已经失效了（诸如网关、API更换此类的操作都会杀死该软件）~~
- ~~核心登录代码是从学校认证系统的前端拿到再用 AI 转的语言（Vibe Coding 太好用了你知道吗）~~

**[AD] MythManager 极域管理器，支持脱控和重放攻击，远程控制他人电脑。仓库：[Github: CreeperMPG/MythManager](https://github.com/CreeperMPG/MythManager)**

## 高级

### 配置与数据存放位置
- 配置文件：`%APPDATA%\HFAuthenticator\config.json`
- 日志文件：`%APPDATA%\HFAuthenticator\autolog.log`

### 代码结构
- `HFAuthenticator\App.xaml.cs` — 应用启动、托盘图标与 `-silent` 参数处理
- `HFAuthenticator\MainWindow.xaml.cs` — 主界面逻辑、自动登录调度、日志与热点交互
- `HFAuthenticator\Utils\ConfigManager.cs` — 配置读写与默认值（负责 `config.json`）
- `HFAuthenticator\Utils\Login.cs` — 构建并发送登录请求（使用 RC4 加密密码、解析 JSON 返回）
- `HFAuthenticator\Utils\RC4Encryptor.cs` — RC4 加密实现（输出为十六进制字符串）
- `HFAuthenticator\Utils\HotspotUtils.cs` — 系统热点启停与状态检查
