using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Remoting.Contexts;
using System.Text.Json;
using System.Threading.Tasks;

namespace HFAuthenticator.Utils
{
    internal class Login
    {
        private readonly HttpClient _httpClient;

        public Login(HttpClient httpClient, Uri baseAddress)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            try
            {
                _httpClient.BaseAddress = baseAddress;
            }
            catch { }
        }

        public async Task<LoginResult> PasswordLoginAsync(string userName, string password, bool rememberPwd = true)
        {
            //生成时间戳作为 key（与 JS 的 +(new Date()) 对应）
            var rckey = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

            // RC4 加密密码
            var encryptedPwd = RC4Encryptor.DoEncryptRC4(password ?? string.Empty, rckey);

            // 构建请求参数
            var parameters = new Dictionary<string, string>
            {
                ["opr"] = "pwdLogin",
                ["userName"] = userName ?? string.Empty,
                ["pwd"] = encryptedPwd,
                ["auth_tag"] = rckey,
                ["rememberPwd"] = rememberPwd ? "1" : "0"
            };

            //发送请求并返回结果（不处理 UI）
            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync("/ac_portal/login.php", content);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            // 转换为 JSON
            using (JsonDocument doc = JsonDocument.Parse(responseBody))
            {
                JsonElement root = doc.RootElement;

                // 获取属性值
                bool success = root.GetProperty("success").GetBoolean();
                return new LoginResult
                {
                    Success = success,
                    ResponseBody = responseBody
                };
            }
        }
    }

    internal class LoginResult
    {
        public bool Success { get; set; }
        public string ResponseBody { get; set; }
    }
}
