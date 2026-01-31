using BaseLibrary.DTOs;

namespace ClientLibrary.Helpers
{
    //Su dung Primary Constructor
    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpClientFactory">Dùng để quản lý và tạo các đối tượng HttpClient một cách tối ưu</param>
    /// <param name="localStorageService">Dùng để truy xuất dữ liệu từ bộ nhớ cục bộ</param>
    public class GetHttpClient(IHttpClientFactory httpClientFactory, LocalStorageService localStorageService)
    {
        private const string HeaderKey = "Authorization";

        /// <summary>
        /// Tạo và trả về <see cref="HttpClient"/> dùng cho các request cần xác thực.
        /// </summary>
        /// <remarks>
        /// - Sử dụng <see cref="IHttpClientFactory"/> để tạo HttpClient có tên <c>SystemApiClient</c>.
        /// - Lấy token người dùng từ localStorage.
        /// - Nếu token hợp lệ, tự động gắn Bearer token vào header Authorization.
        /// - Nếu không có token hoặc token không hợp lệ, HttpClient được trả về sẽ không có xác thực.
        /// </remarks>
        /// <returns>
        /// Một instance <see cref="HttpClient"/> đã được cấu hình phù hợp.
        /// </returns>
        public async Task<HttpClient> GetPrivateHttpClient()
        {
            //Tạo một client có định danh là SystemApiClient
            var client = httpClientFactory.CreateClient("SystemApiClient");
            
            //Lấy chuỗi Token từ trình duyệt vào bộ nhớ máy
            var stringToken = await localStorageService.GetToken();

            //Nếu không có chuỗi token => trả về trống , không có quyền truy cập
            if (string.IsNullOrEmpty(stringToken))
            {
                return client;
            }

            //Biến Json thành đối tượng UserSession
            var deserializeToken = Serializations.DeserializeJsonString<UserSession>(stringToken);
            if (deserializeToken == null)
            {
                return client;
            }
            //Gắn Authorization Header
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", deserializeToken.Token);
            return client;
        }

        public HttpClient GetPublicHttpClient()
        {
            var client = httpClientFactory.CreateClient("SystemApiClient");
            client.DefaultRequestHeaders.Remove(HeaderKey);
            return client;
        }
    }
}
