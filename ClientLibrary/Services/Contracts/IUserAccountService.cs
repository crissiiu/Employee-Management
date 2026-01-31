using BaseLibrary.DTOs;
using BaseLibrary.Responses;

namespace ClientLibrary.Services.Contracts
{
    public interface IUserAccountService
    {
        // General Response
        Task<GeneralResponse> CreateAsync(Register user);

        // Sign In
        Task<LoginResponse> SignInAsync(Login user);

        // Refresh Token
        Task<LoginResponse> RefreshTokenAsync(RefreshToken token);

        // WeatherForecast
        Task<WeatherForecast[]> GetWeatherForecastAsync();
    }
}
