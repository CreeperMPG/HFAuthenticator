using HFAuthenticator.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HFAuthenticator
{
    internal interface ILoginServiceProvider
    {
        void SetConfig(HttpClient httpClient, Uri baseAddress);
        Task<LoginResult> PasswordLoginAsync(string userName, string password, bool rememberPwd = true);
    }
}
