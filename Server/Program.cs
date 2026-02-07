using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using ServerLibrary.Data;
using ServerLibrary.Helpers;
using ServerLibrary.Repositories.Contracts;
using ServerLibrary.Repositories.Implementations;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

//Duoc dung de do du lieu tu file cau hinh vao class JwtSection
builder.Services.Configure<JwtSection>(builder.Configuration.GetSection("JwtSection"));
var jwtSection = builder.Configuration.GetSection(nameof(JwtSection)).Get<JwtSection>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorWasm",
        builder => builder
        // Allow the Blazor WebAssembly client origins (https and http used in launchSettings)
        .WithOrigins("https://localhost:7195", "https://localhost:5233", "http://localhost:5233")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

//starting
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ??
        throw new InvalidOperationException("Sorry, your connection is not found"));
});

/*
 ây là đoạn code quan trọng nhất ở phía Server để giải quyết lỗi 401 Unauthorized mà bạn gặp lúc trước. Nó thiết lập Middleware xác thực JWT, giúp Server biết cách kiểm tra xem cái "thẻ bài" (Token) mà Client gửi lên là thật hay giả.

Dưới đây là phân tích kỹ thuật chi tiết:

1. AddAuthentication - Cấu hình cơ chế mặc định
DefaultAuthenticateScheme: Chỉ định rằng khi có một Request gửi đến, Server sẽ mặc định dùng cơ chế JWT Bearer để kiểm tra danh tính.

DefaultChallengeScheme: Nếu người dùng chưa đăng nhập mà cố tình truy cập vào API bị khóa ([Authorize]), Server sẽ trả về một thử thách (Challenge) — trong trường hợp này là yêu cầu một Token JWT hợp lệ.

2. AddJwtBearer - Bộ quy tắc thẩm định Token
Đoạn này định nghĩa các tiêu chí để Server "chấp nhận" một Token. Nếu Token gửi lên thiếu hoặc sai bất kỳ thông số nào dưới đây, Server sẽ bác bỏ ngay lập tức.

ValidateIssuer & ValidIssuer: Kiểm tra nguồn gốc phát hành Token. Phải khớp chính xác với tên Server đã cấu hình (ví dụ: MyApiServer).

ValidateAudience & ValidAudience: Kiểm tra đối tượng sử dụng. Token này có phải được cấp cho ứng dụng Client của bạn không?

ValidateIssuerSigningKey & IssuerSigningKey: Đây là phần quan trọng nhất về bảo mật.

Server dùng một mã bí mật (jwtSection.Key) để băm (hash) và kiểm tra chữ ký của Token.

Nếu ai đó sửa nội dung Token (như sửa Role từ User lên Admin), chữ ký sẽ bị sai và Server sẽ phát hiện ra ngay.

ValidateLifetime: Kiểm tra thời hạn (Expiry Date). Nếu Token đã hết hạn, Server sẽ từ chối, buộc Client phải thực hiện quy trình refresh-token.

3. Logic vận hành (Technical Workflow)
Khi Client gửi một request kèm Header: Authorization: Bearer <token>

Middleware nhận được Token.

Nó thực hiện Decode (giải mã) dựa trên các tham số bạn vừa cấu hình (Issuer, Audience).

Nó kiểm tra Signature (chữ ký) bằng SymmetricSecurityKey.

Nếu tất cả hợp lệ, nó sẽ trích xuất các thông tin (Claims) từ Token và nạp vào đối tượng User.Identity.

Cuối cùng, nó cho phép request đi tiếp vào Controller.

4. Lưu ý quan trọng
Để đoạn code này hoạt động, bạn bắt buộc phải có 2 dòng này trong app pipeline của Program.cs (Server) theo đúng thứ tự:
app.UseAuthentication(); // Kích hoạt bộ lọc xác thực bạn vừa cấu hình ở trên
app.UseAuthorization();  // Kiểm tra quyền (Role) sau khi đã xác thực xong
 */
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = jwtSection!.Issuer,
        ValidAudience = jwtSection.Audience,
        ClockSkew = TimeSpan.Zero,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection.Key!))
    };
});

builder.Services.AddScoped<IUserAccount, UserAccountRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Apply CORS policy so the Blazor WASM client (different origin/port) can call API endpoints
app.UseCors("AllowBlazorWasm");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
