using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace HFAuthenticator.Utils
{
    internal class BZLogin : ILoginServiceProvider
    {
        private readonly Uri ActionUrl = new Uri("http://192.168.39.253:8888");
        private const string RsaKey = "010001";
        private const string RsaValue = "b7092329837f00a7537c14806876be85a50e3db3e1edbee8cf7451a9cc31c09425eee3f9185d7250742c1eff6e3b2f661a5ca6a764b015560e54d55ff809718b";

        private static string RsaEncrypt(string plainText)
        {
            byte[] modulus = HexStringToByteArray(RsaValue);
            byte[] exponent = HexStringToByteArray(RsaKey);

            var rsaParameters = new RSAParameters
            {
                Modulus = modulus,
                Exponent = exponent
            };

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            // PKCS#1 v1.5 最大明文长度 = 模数字节数 - 11
            int maxLength = modulus.Length - 11;
            if (plainBytes.Length > maxLength)
            {
                throw new ArgumentException($"密码过长，最大允许 {maxLength} 字节（当前 {plainBytes.Length} 字节）");
            }

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(rsaParameters);
                byte[] encryptedBytes = rsa.Encrypt(plainBytes, RSAEncryptionPadding.Pkcs1);
                return ByteArrayToHexString(encryptedBytes);
            }
        }

        private static byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1)
                hex = "0" + hex;

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        private static string ByteArrayToHexString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
        private HttpClient _httpClient;

        public void SetConfig(HttpClient httpClient, Uri baseAddress)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            try
            {
                _httpClient.BaseAddress = baseAddress == null ? ActionUrl : baseAddress;
            }
            catch { }
        }

        public async Task<LoginResult> PasswordLoginAsync(string username, string password, bool rememberPwd = true)
        {
            string authorization;
            try
            {
                authorization = RsaEncrypt(password);
            }
            catch (Exception ex)
            {
                Console.WriteLine("RSA 加密失败: " + ex.Message);
                return new LoginResult
                {
                    Success = false,
                    ResponseBody = ex.Message
                };
            }

            var formData = new Dictionary<string, string>
            {
                { "userName", username },
                { "password", password },
                { "actionType", "umlogin" },
                { "language", "1" },
                { "userIpMac", "" },
                { "authorization", authorization },
                { "redirectUrl", "" }
            };

            _httpClient.DefaultRequestHeaders.ExpectContinue = false;
            var content = new FormUrlEncodedContent(formData);
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync("/", content);
                string responseBody = await response.Content.ReadAsStringAsync();
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    return new LoginResult
                    {
                        Success = false,
                        ResponseBody = ex.Message + "\n" + responseBody
                    };
                }

                Console.WriteLine("请求已发送。");
                Console.WriteLine($"状态码: {response.StatusCode}");
                Console.WriteLine("响应内容:");
                Console.WriteLine(responseBody);
                return new LoginResult
                {
                    Success = true,
                    ResponseBody = responseBody
                };
            }
            catch (Exception ex)
            {
                return new LoginResult
                {
                    Success = false,
                    ResponseBody = ex.StackTrace
                };
            }
        }
    }
}
