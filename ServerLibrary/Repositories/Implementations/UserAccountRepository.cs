using BaseLibrary.DTOs;
using BaseLibrary.Entities;
using BaseLibrary.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServerLibrary.Data;
using ServerLibrary.Helpers;
using ServerLibrary.Repositories.Contracts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Constants = ServerLibrary.Helpers.Contants;

namespace ServerLibrary.Repositories.Implementations
{
    public class UserAccountRepository(IOptions<JwtSection> config, AppDbContext appDbContext) : IUserAccount
    {
        public async Task<LoginResponse> CreateAsync(Login user)
        {
            if (user is null)
            {
                return new LoginResponse(false, "Model is empty");
            }

            var applicationUser = await FindUserByEmail(user.Email!);
            if (applicationUser is null)
            {
                return new LoginResponse(false, "User not found");
            }

            //Verify password
            if (!BCrypt.Net.BCrypt.Verify(user.Password, applicationUser.Password))
            {
                return new LoginResponse(false, "Email/Password not valid");
            }

            var getUserRole = await FindUserRole(applicationUser.Id);
            if (getUserRole is null)
            {
                return new LoginResponse(false, "User role not found");
            }

            var getRoleName = await FindRoleName(getUserRole.RoleId);
            if (getUserRole is null)
            {
                return new LoginResponse(false, "User role not found");
            }

            string jwtToken = GenerateToken(applicationUser, getRoleName!.Name!);
            string refreshToken = GenerateRefreshToken();

            //Save refresh token to database
            var findUser = await appDbContext.RefreshTokenInfos.
                FirstOrDefaultAsync(_ => _.UserId.Equals(applicationUser.Id));
            if (findUser is not null)
            {
                findUser.Token = refreshToken;
                await appDbContext.SaveChangesAsync();
            }
            else
            {
                await AddToDatabase(new RefreshTokenInfo()
                {
                    Token = refreshToken,
                    UserId = applicationUser.Id
                });
            }
            return new LoginResponse(true, "Login successful", jwtToken, refreshToken);
        }

        /// <summary>
        /// Generates a JSON Web Token (JWT) for the given `user` and `role`.
        /// 
        /// Steps:
        /// 1. Build a symmetric security key from the configured secret key (`config.Value.Key`).
        /// 2. Create HMAC-SHA256 signing credentials from that key.
        /// 3. Create an array of standard claims:
        ///    - `NameIdentifier`: the user's Id (as string)
        ///    - `Name`: the user's full name
        ///    - `Email`: the user's email
        ///    - `Role`: the provided role name
        /// 4. Create a `JwtSecurityToken` using issuer, audience, claims, expiry (1 day), and the signing credentials.
        /// 5. Serialize and return the token string using `JwtSecurityTokenHandler`.
        /// 
        /// Note: This method assumes `config.Value` contains non-null `Key`, `Issuer`, and `Audience`.
        /// </summary>
        /// <param name="user">The `ApplicationUser` whose identity will be embedded in the token.</param>
        /// <param name="role">The role name to include as a claim in the token.</param>
        /// <returns>A signed JWT as a string, valid for 1 day from creation.</returns>
        private string GenerateToken(ApplicationUser user, string role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Value.Key!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var userClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Fullname!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: config.Value.Issuer,
                audience: config.Value.Audience,
                claims: userClaims,
                expires: DateTime.Now.AddMinutes(1),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<UserRole> FindUserRole(int userId) =>
            await appDbContext.UserRoles.FirstOrDefaultAsync(_ => _.UserId == userId);
        private async Task<SystemRole> FindRoleName(int roleId) =>
            await appDbContext.SystemRoles.FirstOrDefaultAsync(_ => _.Id == roleId);
        private static string GenerateRefreshToken() =>
                        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        public async Task<GeneralResponse> CreateAsync(Register user)
        {
            if (user is null)
            {
                return new GeneralResponse(false, "Model is empty");
            }

            var checkUser = await FindUserByEmail(user!.Email!);
            if (checkUser != null)
            {
                return new GeneralResponse(false, "User registered already");
            }

            var applicationUser = await AddToDatabase(new ApplicationUser()
            {
                Fullname = user.Fullname,
                Email = user.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(user.Password)
            });

            //check, create and assign role to user
            var checkAdminRole = await appDbContext.SystemRoles.
                FirstOrDefaultAsync(_ => _.Name!.Equals(Constants.Admin));
            if (checkAdminRole is null)
            {
                var createAdminRole = await AddToDatabase(new SystemRole()
                {
                    Name = Constants.Admin
                });
                await AddToDatabase(new UserRole()
                {
                    RoleId = createAdminRole.Id,
                    UserId = applicationUser.Id
                });
                return new GeneralResponse(true, "Account created!");
            }

            var checkUserRole = await appDbContext.SystemRoles.
                FirstOrDefaultAsync(_ => _.Name!.Equals(Contants.User));
            SystemRole response = new();
            if (checkUserRole is null)
            {
                response = await AddToDatabase(new SystemRole()
                {
                    Name = Contants.User
                });
                await AddToDatabase(new UserRole()
                {
                    RoleId = response.Id,
                    UserId = applicationUser.Id
                });
            }
            else
            {
                await AddToDatabase(new UserRole()
                {
                    RoleId = checkUserRole.Id,
                    UserId = applicationUser.Id
                });
            }
            return new GeneralResponse(true, "Account created!");
        }

        public async Task<ApplicationUser> FindUserByEmail(string email) =>
            await appDbContext.ApplicationUsers
                .FirstOrDefaultAsync(_ => _.Email!.ToLower()!.Equals(email!.ToLower()));

        public async Task<T> AddToDatabase<T>(T model)
        {
            var result = appDbContext.Add(model);
            await appDbContext.SaveChangesAsync();
            return (T)result.Entity;
        }

        public async Task<LoginResponse> RefreshTokenAsync(RefreshToken token)
        {
            //Kiểm tra đối tượng token có bị trống không
            if (token is null)
            {
                return new LoginResponse(false, "Token is empty");
            }
            //Kiểm tra token có tồn tại không
            var findToken = await appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.Token!.Equals(token.Token));
            if (findToken is null)
            {
                return new LoginResponse(false, "Token not found");
            }
            //Lấy user gắn liền với token để tìm thông tin người dùng
            var getUser = await appDbContext.ApplicationUsers.FirstOrDefaultAsync(_ => _.Id == findToken.UserId);
            if (getUser is null)
            {
                return new LoginResponse(false, "Refresh token found not be generated because user not found");
            }

            //Lấy quyền và tạo jwt mới
            var userRole = await FindUserRole(getUser.Id);
            var roleName = await FindRoleName(userRole.RoleId);
            string jwtToken = GenerateToken(getUser, roleName.Name!);
            string refreshToken = GenerateRefreshToken();

            //Ghi lại token cũ và thay thế bằng totken mới
            var updateToken = await appDbContext.RefreshTokenInfos.FirstOrDefaultAsync(_ => _.Id == findToken.Id);
            if (updateToken is null)
            {
                return new LoginResponse(false, "Refresh token could not be generated because user has not sign in");
            }
            await appDbContext.SaveChangesAsync();
            return new LoginResponse(true, "Token refreshed successfully", jwtToken, refreshToken);
        }
    }
}
