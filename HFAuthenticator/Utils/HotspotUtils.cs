using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;

namespace HFAuthenticator.Utils
{
    internal class HotspotUtils
    {

        /// <summary>
        /// 尝试打开系统的移动热点（Wi-Fi 热点）。
        /// </summary>
        /// <returns>如果成功启动返回 true，否则抛出异常或返回 false。</returns>
        public static async Task<bool> TurnOnHotspotAsync()
        {
            try
            {
                // 1. 获取当前用于 Internet 连接的网络配置文件（通常是正在上网的网卡）
                var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                if (connectionProfile == null)
                    throw new InvalidOperationException("未检测到可用的 Internet 连接，无法共享。");

                // 2. 创建 TetheringManager 实例（自动选择共享适配器）
                var tetheringManager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(connectionProfile);

                // 3. 检查当前热点状态
                if (tetheringManager.TetheringOperationalState == TetheringOperationalState.On)
                {
                    Console.WriteLine("热点已处于开启状态。");
                    return true;
                }

                // 4. 启动热点
                var result = await tetheringManager.StartTetheringAsync();
                if (result.Status == TetheringOperationStatus.Success)
                {
                    Console.WriteLine("热点启动成功。");
                    return true;
                }
                else
                {
                    // 可根据 result.Status 细化错误类型（如 Wi-Fi 设备未开启、配置冲突等）
                    throw new InvalidOperationException($"启动热点失败，状态：{result.Status}");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 停止移动热点。
        /// </summary>
        public static async Task<bool> TurnOffHotspotAsync()
        {
            try
            {
                // 1. 获取当前用于 Internet 连接的网络配置文件（通常是正在上网的网卡）
                var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                if (connectionProfile == null)
                    throw new InvalidOperationException("未检测到可用的 Internet 连接，无法共享。");

                // 2. 创建 TetheringManager 实例（自动选择共享适配器）
                var tetheringManager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(connectionProfile);

                // 3. 检查当前热点状态
                if (tetheringManager.TetheringOperationalState == TetheringOperationalState.Off)
                {
                    return true;
                }

                // 4. 停止热点
                var result = await tetheringManager.StopTetheringAsync();
                if (result.Status == TetheringOperationStatus.Success)
                {
                    return true;
                }
                else
                {
                    throw new InvalidOperationException($"停止热点失败，状态：{result.Status}");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 检查热点是否开启。返回 true=开启，false=关闭，null=未知/出错。
        /// </summary>
        public static Task<bool?> IsHotspotOnAsync()
        {
            try
            {
                var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                if (connectionProfile == null)
                    return Task.FromResult<bool?>(null);

                var tetheringManager = NetworkOperatorTetheringManager.CreateFromConnectionProfile(connectionProfile);
                var state = tetheringManager.TetheringOperationalState;
                return Task.FromResult<bool?>(state == TetheringOperationalState.On);
            }
            catch (Exception)
            {
                return Task.FromResult<bool?>(null);
            }
        }

        /// <summary>
        /// 同步版本（适用于简单调用，注意不要在 UI 线程直接使用，避免死锁）。
        /// 推荐在后台线程或 Task.Run 中使用。
        /// </summary>
        public static bool TurnOnHotspot()
        {
            // 使用 Task.Run 避免同步上下文死锁
            return Task.Run(async () => await TurnOnHotspotAsync()).GetAwaiter().GetResult();
        }

        public static bool TurnOffHotspot()
        {
            return Task.Run(async () => await TurnOffHotspotAsync()).GetAwaiter().GetResult();
        }
    }
}

