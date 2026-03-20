using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace HFAuthenticator.Utils
{
    internal class ConfigManager
    {
        private static readonly string ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HFAuthenticator");
        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");
        private static readonly object _lock = new object();

        internal class AppConfig
        {
            public string IPEndpoint { get; set; }
            public bool RememberPwd { get; set; } = true;
            public string Username { get; set; }
            public int TimeoutSeconds { get; set; }
            public string Password { get; set; }
            public int RequestFrequency { get; set; }
            // Whether auto-login toggle should be on when app starts
            public bool AutoStart { get; set; }
            public bool AutoHotspot { get; set; } = false;
        }

        public static AppConfig Load()
        {
            lock (_lock)
            {
                try
                {
                    if (!Directory.Exists(ConfigDir)) Directory.CreateDirectory(ConfigDir);
                    if (!File.Exists(ConfigPath))
                    {
                        var def = GetDefaultConfig();
                        Save(def);
                        return def;
                    }

                    var text = File.ReadAllText(ConfigPath, Encoding.UTF8);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var cfg = JsonSerializer.Deserialize<AppConfig>(text, options);
                    if (cfg == null)
                    {
                        cfg = GetDefaultConfig();
                        Save(cfg);
                    }
                    return cfg;
                }
                catch
                {
                    return GetDefaultConfig();
                }
            }
        }

        public static void Save(AppConfig config)
        {
            if (config == null) return;
            lock (_lock)
            {
                if (!Directory.Exists(ConfigDir)) Directory.CreateDirectory(ConfigDir);
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(ConfigPath, json, Encoding.UTF8);
            }
        }

        private static AppConfig GetDefaultConfig()
        {
            return new AppConfig
            {
                IPEndpoint = "172.16.255.2",
                RememberPwd = true,
                Username = string.Empty,
                Password = string.Empty,
                TimeoutSeconds = 30,
                RequestFrequency = 600,
                AutoStart = false,
                AutoHotspot = false,
            };
        }
    }
}
