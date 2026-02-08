using Blazored.LocalStorage;
using Client;
using Client.ApplicationStates;
using ClientLibrary.Helpers;
using ClientLibrary.Services.Contracts;
using ClientLibrary.Services.Implementations;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Syncfusion.Blazor;
using Syncfusion.Blazor.Popups;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddTransient<CustomHttpClient>();
builder.Services.AddHttpClient("SystemApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7065");
}).AddHttpMessageHandler<CustomHttpClient>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7065/") });
//kích hoạt hệ thống phân quyền
builder.Services.AddAuthorizationCore();
//Đăng ký thư viện bên thứ 3 để thao tác với LocalStorage
builder.Services.AddBlazoredLocalStorage();
//Đăng ký Helpers Class dùng để tạo HttpClient
builder.Services.AddScoped<GetHttpClient>();
//Một lớp Wrapper để bao bọc thư việc BlazoredLocalStorage
builder.Services.AddScoped<LocalStorageService>();
//Kỹ thuật Dependency Inversion, ép Blazor phải sử dụng logic lấy Token từ Local Storage->Decrypt->SetPrincipal
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
//Đăng ký dịch vụ nghiệp vụ liên quan tới tài khoản (Login, register)
builder.Services.AddScoped<IUserAccountService, UserAccountService>();

builder.Services.AddScoped<DepartmentState>();

builder.Services.AddSyncfusionBlazor();
builder.Services.AddScoped<SfDialogService>();


await builder.Build().RunAsync();
