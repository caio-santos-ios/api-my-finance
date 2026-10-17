using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using api_finances.src.Helpers;
using api_finances.src.Interfaces;
using api_finances.src.Models;
using api_finances.src.Models.Base;
using api_finances.src.Requests;
using api_finances.src.Shared.Utils;

namespace api_finances.src.Services
{
    public class AuthService(
        IUserRepository userRepository,
        MailHelper mailHelper
    ) : IAuthService
    {
        #region REGISTER
        public async Task<ResponseApi<dynamic?>> CreateAsync(CreateUserRequest request)
        {
            try
            {
                User? existed = await userRepository.GetByEmailAsync(request.Email);
                if (existed is not null) return new(null, 400, "E-mail inválido, tente usar outro");

                dynamic access = Util.GenerateCodeAccess();

                User user = new()
                {
                    Email = request.Email,
                    Name = request.Name,
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    CodeAccess = access.CodeAccess,
                    CodeAccessExpiration = access.CodeAccessExpiration,
                    ValidatedAccess = false,
                    Admin = request.Admin,
                };

                User? response = await userRepository.CreateAsync(user);
                if (response is null) return new(null, 400, "Falha ao criar conta.");

                await mailHelper.SendAccountConfirmationMail(request.Email, request.Name, access.CodeAccess);

                return new(new { name = user.Name }, 201, "Usuário criado com sucesso.");
            }
            catch
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde");
            }
        }
        #endregion
        #region LOGIN
        public async Task<ResponseApi<dynamic?>> LoginAsync(LoginRequest request)
        {
            try
            {
                User? response = await userRepository.GetByEmailAsync(request.Email);
                if (response is null) return new(null, 400, "E-mail ou senha são incorretos");

                if (response.Blocked) return new(null, 400, "Conta bloqueada, entre em contato com o Administrador");

                if (!response.ValidatedAccess)
                {
                    dynamic generateCode = Util.GenerateCodeAccess();

                    response.CodeAccess = generateCode.CodeAccess;
                    response.CodeAccessExpiration = generateCode.CodeAccessExpiration;

                    await userRepository.UpdateAsync(response);

                    await mailHelper.SendAccountConfirmationMail(request.Email, response.Name, generateCode.CodeAccess);

                    return new(null, 400, "Conta não foi confirmada, enviamos um e-mail de confirmação novamente");
                }

                bool isValid = BCrypt.Net.BCrypt.Verify(request.Password, response.Password);
                if (!isValid)
                {
                    if (response.IncorrectsPassword.Count() > 5)
                    {
                        response.Blocked = true;
                        await userRepository.UpdateAsync(response);
                        return new(null, 400, "Usuário bloqueado, por tentativas de acesso indevido");
                    }

                    response.IncorrectsPassword.Add(new UserIncorrectPassword()
                    {
                        Ip = request.Device.Ip,
                        Date = DateTime.UtcNow,
                        Platform = request.Device.Platform
                    });
                    await userRepository.UpdateAsync(response);
                    return new(null, 400, "Dados incorretos");
                }

                response.Devices.Add(request.Device);

                await userRepository.UpdateAsync(response);

                return new(new { Token = GenerateJwtToken(response), RefreshToken = GenerateJwtToken(response, true), response.Photo, response.Name, admin = response.Admin.ToString() }, 200, "Login feito com sucesso.");
            }
            catch
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde");
            }
        }
        public async Task<ResponseApi<dynamic?>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            try
            {
                TokenValidationParameters parameters = new()
                {
                    ValidateIssuer = true,
                    ValidIssuer = Environment.GetEnvironmentVariable("ISSUER"),
                    ValidateAudience = true,
                    ValidAudience = Environment.GetEnvironmentVariable("AUDIENCE"),
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("SECRET_KEY") ?? "")),
                    ClockSkew = TimeSpan.Zero,
                };

                new JwtSecurityTokenHandler().ValidateToken(request.RefreshToken, parameters, out SecurityToken validated);

                JwtSecurityToken jwt = (JwtSecurityToken)validated;

                if (jwt.Claims.FirstOrDefault(c => c.Type == "type")?.Value != "refresh") return new(null, 401, "Refresh token inválido.");

                string email = jwt.Claims.First(c => c.Type == "email").Value;

                User? response = await userRepository.GetByEmailAsync(email);
                if (response is null || response.Blocked) return new(null, 401, "Usuário inválido.");

                return new(new
                {
                    Token = GenerateJwtToken(response),
                    RefreshToken = GenerateJwtToken(response, true),
                    response.Photo,
                    response.Name,
                    admin = response.Admin.ToString()
                }, 200, "Token renovado com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }
        #endregion
        #region RESET PASSWORD
        public async Task<ResponseApi<dynamic?>> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            try
            {
                User? response = await userRepository.GetByEmailAsync(request.Email);
                if (response is null) return new(null, 400, "E-mail inválido");

                dynamic generateCode = Util.GenerateCodeAccess();

                response.CodeAccess = generateCode.CodeAccess;
                response.CodeAccessExpiration = generateCode.CodeAccessExpiration;

                await userRepository.UpdateAsync(response);

                await mailHelper.SendPasswordResetMail(response.Email, response.Name, generateCode.CodeAccess);

                return new(new { }, 200, "Foi enviado um código de verificação para o e-mail.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }
        public async Task<ResponseApi<dynamic?>> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                User? response = await userRepository.GetByCodeAsync(request.Code);
                if (response is null) return new(null, 400, "Código inválido");

                DateTime today = DateTime.Now;

                if (today > response.CodeAccessExpiration) return new(null, 400, "Código expirou, deve solicitar outro");

                dynamic generateCode = Util.GenerateCodeAccess();

                response.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
                response.ValidatedAccess = true;
                response.CodeAccessExpiration = null;
                response.CodeAccess = "";

                await userRepository.UpdateAsync(response);

                return new(new { }, 200, "Senha resetada com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }
        public async Task<ResponseApi<dynamic?>> CleanIncorrectPasswordAsync(CleanIncorrectPasswordRequest request)
        {
            try
            {
                User? response = await userRepository.GetByIdAsync(request.UserId);
                if (response is null) return new(null, 404, "Usuário não encontrado");

                response.Blocked = false;
                response.IncorrectsPassword = [];

                await userRepository.UpdateAsync(response);

                return new(new { }, 200, "Tentativa de login resetada com sucesso.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }
        #endregion
        #region CODE CONFIRMATION
        public async Task<ResponseApi<dynamic?>> NewCodeAsync(NewCodeRequest request)
        {
            try
            {
                User? response = await userRepository.GetByEmailAsync(request.Email);
                if (response is null) return new(null, 400, "E-mail inválido");

                dynamic generateCode = Util.GenerateCodeAccess();

                response.CodeAccess = generateCode.CodeAccess;
                response.CodeAccessExpiration = generateCode.CodeAccessExpiration;
                response.ValidatedAccess = false;

                await userRepository.UpdateAsync(response);

                await mailHelper.SendAccountConfirmationMail(request.Email, response.Name, generateCode.CodeAccess);

                return new(new { }, 200, "Foi enviado um código de verificação para o e-mail.");
            }
            catch (Exception ex)
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde - {ex.Message}");
            }
        }
        #endregion
        public string GenerateJwtToken(User user, bool refresh = false)
        {
            string SecretKey = Environment.GetEnvironmentVariable("SECRET_KEY") ?? "";
            string Issuer = Environment.GetEnvironmentVariable("ISSUER") ?? "";
            string Audience = Environment.GetEnvironmentVariable("AUDIENCE") ?? "";

            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(SecretKey));

            Claim[] claims =
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("type", refresh ? "refresh" : "access"),
                new Claim("name", user.Name),
                new Claim("photo", user.Photo),
                new Claim("admin", user.Admin.ToString()),
            ];

            SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new(
                issuer: Issuer,
                audience: Audience,
                claims: claims,
                expires: refresh ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}