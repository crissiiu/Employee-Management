using BaseLibrary.DTOs;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ClientLibrary.Helpers
{
    public class CustomAuthenticationStateProvider(LocalStorageService localStorageService) : AuthenticationStateProvider
    {
        //Khởi tạo trạng thái ẩn danh
        /*
         * Để một giá trị mặc định. Nếu không tìm thấy bằng chứng đăng nhập hợp lệ
         * hệ thống sẽ coi người dùng là khách thay vì để ứng dụng bị lỗi
         */
        private readonly ClaimsPrincipal anonymous = new(
                new ClaimsIdentity());
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            //Luồng kiểm tra bằng chứng xác thực
            /*
             * Hàm GetAuthenticationStateAsync chạy theo cơ chế kiểm tra từng lớp (Guard Clauses):
             * -  Lấy token: Đầu tiên code truy cập và localStorage để lấy chuỗi token.
             * Nếu không thấy token trả về trạng thái anonymous.
             * 
             * - Giải mã JSON: Hàm Serializations.DeserializeJsonString để biến chuỗi thành đối tượng
             * UserSession. Nếu giải mã lỗi trả về anonymous.
             * 
             * - Trích xuất quyền hạn(Claims): Decrypt Token để đọc thông tin bên trong token (Email, Role, ...).
             * Nếu token giả hoặc hết hạn, tiếp tục trả về anymous.
             * 
             * Thiết lập trạng thái đăng nhập thành công:
             * - Nếu vượt qua tất cả các bước trên, tạo một danh tính mới chứa đầy đủ thông tin người dùng.
             * - Blazor sẽ dựa vào giá trị này để hiển thị giao diện phù hợp, ví dụ hiển thị tên người dùng, hiển thị trang admin.
             */

            var stringToken = await localStorageService.GetToken();
            if (string.IsNullOrEmpty(stringToken))
            {
                return await Task.FromResult(new AuthenticationState(anonymous));
            }

            var deserializeToken = Serializations.DeserializeJsonString<UserSession>(stringToken);
            if (deserializeToken == null)
            {
                return await Task.FromResult(new AuthenticationState(anonymous));
            }

            var getUserClaims = DecryptToken(deserializeToken.Token!);
            if (getUserClaims == null)
            {
                return await Task.FromResult(new AuthenticationState(anonymous));
            }

            var claimsPrincipal = SetClaimPrincipal(getUserClaims);
            return await Task.FromResult(new AuthenticationState(claimsPrincipal));
        }

        /// <summary>
        /// Trọng tâm kỹ thuật là Triggering State Changes (Kích hoạt thay đổi trạng thái).
        /// Dùng để xử lý kịch bản Login và Logout trong thời gian thực.
        /// 1.Mục đích:
        /// - Hàm này đồng bộ hoá trạng thái đăng nhập giữa 3 nơi: Bộ nhớ trình duyệt (LocalStorage)
        /// Logic ứng dụng (Principal) và giao diện người dùng (UI Components).
        /// 2. Logic chi tiết:
        /// - Kịch bản A: Đăng nhập thành công (Khối if) khi nhận được userSession có chứa Token:
        ///     - Persistence(Lưu trữ): Gọi Seriallizations.SerializrObj để biến object session thành
        ///     chuỗi Json và đẩy vào LocalStorage. Bước này khi F5 không bị mất login.
        ///     - Rehydration(Khôi phục danh tính): Gọi DecryptToken để mổ xẻ Jwt lấy thông tin User,
        ///     sau đó gọi hàm SetClaimPrincipal để tạo Principal.
        ///     - Kết quả: Biến claimsPrincipal từ trạng thái trống (Anonymous) chuyển sang trạng thái
        ///     xác thực
        ///- Kịch bản B: Đăng xuất hoặc Token lỗi (khối else)
        ///     - Cleanup: Gọi localStorageService.RemoveToken() để xoá sạch dấu vết phiên làm việc trong
        ///     trình duyệt.
        ///     - Reset: lúc này claimsPrincipal vẫn là một new ClaimsPrincipal() rỗng.
        ///3. Thành phần quan trọng nhất
        ///- Chức năng: Phát một tín hiệu (Event) cho toàn bộ ứng dụng Blazor.
        ///- Tác động UI: khi dòng này chạy, tất cả Component đang nằm trong thẻ <AuthorizeView> sẽ lập
        ///tức render lại:
        ///     - Nếu là login sẽ hiện nút logout
        ///     - new là logout sẽ hiện nút login
        /// </summary>
        /// <param name="userSession"></param>
        /// <returns></returns>
        public async Task UpdateAuthenticationState(UserSession userSession)
        {
            var claimsPrincipal = new ClaimsPrincipal();
            if (claimsPrincipal != null || userSession.RefreshToken != null)
            {
                var serializationSession = Serializations.SerializeObj(userSession);
                await localStorageService.SetToken(serializationSession);
                var getUserClaims = DecryptToken(userSession.Token!);
                claimsPrincipal = SetClaimPrincipal(getUserClaims);
            }
            else
            {
                await localStorageService.RemoveToken();
            }
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal!)));
        }

        /// <summary>
        /// Chuyển đổi các thông tin thô của người dùng từ Object CustomerUserClaims
        /// thành một đối tượng ClaimsPrincipal. Trong .NET chỉ có ClaumsPrincipal mới
        /// được các thành phần như AuthorizeView hay [Authorize] công nhận để thực thi
        /// phân quyền.
        /// 
        /// -  Kiểm tra Email null: Đây là bước Guard Clause. Nếu dữ liệu đầu vào không có
        /// - Email hàm trả về ClaimsPrincipal rỗng tương đương với trạng thái chưa đăng nhập
        /// - new List<Claim>: khởi tạo danh sách các "thẻ bài" định danh: mỗi new Claims(...)
        /// định nghĩa một thuộc tính cụ thể của người dùng theo tiêu chuẩn Microsoft
        /// - new ClaimsIdentity(...,"JwtAuth")
        ///     - Gom danh sách các Claims ở trênt thành một danh tính
        ///     - Tham số "JwtAuth" cực kỳ quan trọng: khi bạn truyền một chuỗi tên phương thức
        ///     xác thực vào đây, thuộc tính IsAuthenticated sẽ được set thành true, nếu không có
        ///     chuỗi này, hệ thống vẫn coi người dùng là khác dù có đủ Claims.
        ///- return new ClaimsPrincipal(...):Đóng gói danh tính để trả về cho hệ thống
        /// </summary>
        /// <param name="claims"></param>
        /// <returns></returns>
        public static ClaimsPrincipal SetClaimPrincipal(CustomUserClaims claims)
        {
            if (claims.Email is null)
            {
                return new ClaimsPrincipal();
            }
            return new ClaimsPrincipal(new ClaimsIdentity(
                new List<Claim>
                {
                    new (ClaimTypes.NameIdentifier, claims.Id!),
                    new (ClaimTypes.Name, claims.Name!),
                    new (ClaimTypes.Email, claims.Email!),
                    new (ClaimTypes.Role, claims.Role!)
                }, "JwtAuth"));
        }

        /// <summary>
        /// Khi đăng nhập thành công server sẽ trả về một chuỗi ký tự jwt.
        /// Hàm này có nhiệm vụ đọc nội dung trong chuỗi đó.
        /// JwtSecurityTokenHandler đây là công cụ chuyên dụng để đọc jwt.
        /// Trích xuất Claims:
        ///  - NameIdentifier: ID người dùng
        ///  - Name: Tên người dùng
        ///  - Email: Email người dùng
        ///  - Role: Quyền hạn người dùng
        ///  Chuyển đổi chuỗi văn bản thành đối tượng CustomUserClaims có
        ///  cấu trúc để các thành phần khác của ứng dụng dễ dàng sử dụng.
        /// </summary>
        /// <param name="jwtToken"></param>
        /// <returns></returns>
        private static CustomUserClaims DecryptToken(string jwtToken)
        {
            if (string.IsNullOrEmpty(jwtToken))
            {
                return new CustomUserClaims();
            }

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwtToken);
            var userId = token.Claims.FirstOrDefault(_ => _.Type == ClaimTypes.NameIdentifier);
            var name = token.Claims.FirstOrDefault(_ => _.Type == ClaimTypes.Name);
            var email = token.Claims.FirstOrDefault(_ => _.Type == ClaimTypes.Email);
            var role = token.Claims.FirstOrDefault(_ => _.Type == ClaimTypes.Role);
            return new CustomUserClaims
            {
                Id = userId!.Value!,
                Name = name!.Value!,
                Email = email!.Value!,
                Role = role!.Value!
            };
        }
    }
}
