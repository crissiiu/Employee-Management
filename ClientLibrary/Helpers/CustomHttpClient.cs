using BaseLibrary.DTOs;
using ClientLibrary.Services.Contracts;
using System.Net;
using System.Net.Http.Headers;
namespace ClientLibrary.Helpers
{
    public class CustomHttpClient(GetHttpClient getHttpClient, LocalStorageService localStorageService, IUserAccountService accountService) : DelegatingHandler
    {
        /// <summary>
        /// Mục đích: Giải quyết bài toán "Silent Request"
        /// 1. Tự động gắn Token: Thay vì mỗi lần gọi API phải viết code lấy token từ LocalStorage rồi gắn vào header
        /// class này sẽ tự động làm việc đó cho mọi request.
        /// 2. Tự động xử lý hết hạn: KHi request lỗi 401, class này sẽ âm thầm gọi API "Refresh token", lấy token mới
        /// và gửi lại request cũ mà người dùng không hề hay biết
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            bool loginUrl = request.RequestUri!.AbsoluteUri.Contains("login");
            bool registerUrl = request.RequestUri!.AbsoluteUri.Contains("register");
            bool refreshTokenUrl = request.RequestUri!.AbsoluteUri.Contains("refresh-token");

            if(loginUrl || registerUrl || refreshTokenUrl)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var result = await base.SendAsync(request, cancellationToken);
            if(result.StatusCode == HttpStatusCode.Unauthorized)
            {
                //Get token from localStorage
                var stringToken = await localStorageService.GetToken();
                if (stringToken == null)
                {
                    return result;
                }
                //Check if the header containers token
                string token = string.Empty;
                try
                {
                    token = request.Headers.Authorization!.Parameter!;
                }
                catch { }

                var deserializedToken = Serializations.DeserializeJsonString<UserSession>(stringToken);
                if(deserializedToken == null)
                {
                    return result;
                }

                if (string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", deserializedToken.Token);
                    return await base.SendAsync(request, cancellationToken);
                }

                var newJwtToken = await GetReshToken(deserializedToken.RefreshToken!);
                if (string.IsNullOrEmpty(newJwtToken))
                {
                    return result;
                }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newJwtToken);
                return await base.SendAsync(request, cancellationToken);
            }
            return result;
        }

        private async Task<string> GetReshToken(string refreshToken)
        {
            var result = await accountService.RefreshTokenAsync(new RefreshToken() { Token = refreshToken });
            string serializedToken = Serializations.SerializeObj(new UserSession()
            {
                Token = result.Token,
                RefreshToken = result.RefreshToken
            });
            await localStorageService.SetToken(serializedToken);
            return result.Token;
        }
    }
}
