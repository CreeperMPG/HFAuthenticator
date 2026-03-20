using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HFAuthenticator.Utils
{
    public static class RC4Encryptor
    {
        public static string DoEncryptRC4(string src, string passwd)
        {
            // 与 JS 中的 $.trim(src+'') 等效
            src = (src ?? "").Trim();
            passwd = passwd ?? "";

            int j = 0, a = 0, b = 0, c = 0;
            int plen = passwd.Length;
            int size = src.Length;

            int[] key = new int[256];
            int[] sbox = new int[256];
            StringBuilder output = new StringBuilder(size * 2); // 每个字符输出2个十六进制数字

            // 初始化 key 和 sbox（与 JS 代码完全一致）
            for (int i = 0; i < 256; i++)
            {
                key[i] = passwd[i % plen];
                sbox[i] = i;
            }

            // KSA (Key Scheduling Algorithm)
            for (int i = 0; i < 256; i++)
            {
                j = (j + sbox[i] + key[i]) % 256;
                // 交换 sbox[i] 和 sbox[j]
                int temp = sbox[i];
                sbox[i] = sbox[j];
                sbox[j] = temp;
            }

            // PRGA (Pseudo-Random Generation Algorithm) 并生成输出
            for (int i = 0; i < size; i++)
            {
                a = (a + 1) % 256;
                b = (b + sbox[a]) % 256;

                // 交换 sbox[a] 和 sbox[b]
                int temp = sbox[a];
                sbox[a] = sbox[b];
                sbox[b] = temp;

                c = (sbox[a] + sbox[b]) % 256;

                // 异或运算
                int xorResult = src[i] ^ sbox[c];

                // 转换为十六进制字符串（确保2位）
                string hex = xorResult.ToString("x2");

                // JS 代码中的额外处理，但 ToString("x2") 已经确保了2位十六进制
                output.Append(hex);
            }

            return output.ToString();
        }

        // 可选：解密函数（RC4 是对称加密，解密使用相同逻辑）
        public static string DoDecryptRC4(string hexEncoded, string passwd)
        {
            if (hexEncoded.Length % 2 != 0)
                throw new ArgumentException("十六进制字符串长度必须为偶数");

            // 将十六进制字符串转换回字节数组
            byte[] bytes = new byte[hexEncoded.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexEncoded.Substring(i * 2, 2), 16);
            }

            // 使用相同的 RC4 逻辑解密
            string src = Encoding.UTF8.GetString(bytes);
            return DoEncryptRC4(src, passwd);
        }
    }
}
