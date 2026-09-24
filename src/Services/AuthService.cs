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
        public async Task<ResponseApi<dynamic?>> CreateAsync(CreateUserDTO request)
        {
            try
            {
                ResponseApi<User?> existed = await userRepository.GetByEmailAsync(request.Email);
                if (existed.Data is not null) return new(null, 400, "E-mail inválido, tente usar outro");

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

                ResponseApi<User?> response = await userRepository.CreateAsync(user);
                if (response.Data is null) return new(null, 400, "Falha ao criar conta.");

                await mailHelper.SendMail(request.Email, "Código de Confirmação", $"Seu código de confirmação: {access.CodeAccess}");

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
                ResponseApi<User?> response = await userRepository.GetByEmailAsync(request.Email);
                if (response.Data is null) return new(null, 400, "E-mail ou senha são incorretos");

                if (response.Data.Blocked) return new(null, 400, "Conta bloqueada, entre em contato com o Administrador do TaskFlow");

                if (!response.Data.ValidatedAccess)
                {
                    dynamic generateCode = Util.GenerateCodeAccess();

                    response.Data.CodeAccess = generateCode.CodeAccess;
                    response.Data.CodeAccessExpiration = generateCode.CodeAccessExpiration;

                    await userRepository.UpdateAsync(response.Data);

                    await mailHelper.SendMail(request.Email, "Código de Confirmação", $"Seu código de confirmação: {generateCode.CodeAccess}");

                    return new(null, 400, "Conta não foi confirmada, enviamos um e-mail de confirmação novamente");
                }

                bool isValid = BCrypt.Net.BCrypt.Verify(request.Password, response.Data.Password);
                if (!isValid) return new(null, 400, "Dados incorretos");

                return new(new { Token = GenerateJwtToken(response.Data), RefreshToken = GenerateJwtToken(response.Data, true), response.Data.Photo, response.Data.Name, admin = response.Data.Admin.ToString() }, 200, "Login feito com sucesso.");
            }
            catch
            {
                return new(null, 500, $"Ocorreu um erro inesperado. Por favor, tente novamente mais tarde");
            }
        }
        #endregion
        #region RESET PASSWORD
        public async Task<ResponseApi<dynamic?>> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            try
            {
                ResponseApi<User?> response = await userRepository.GetByEmailAsync(request.Email);
                if (response.Data is null) return new(null, 400, "E-mail inválido");

                dynamic generateCode = Util.GenerateCodeAccess();

                response.Data.CodeAccess = generateCode.CodeAccess;
                response.Data.CodeAccessExpiration = generateCode.CodeAccessExpiration;

                await userRepository.UpdateAsync(response.Data);

                var res = await mailHelper.SendMail(response.Data.Email, "Código de Verificação", $"Seu código de veficação: {generateCode.CodeAccess}");

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
                ResponseApi<User?> response = await userRepository.GetByCodeAsync(request.Code);
                if (response.Data is null) return new(null, 400, "Código inválido");

                DateTime today = DateTime.Now;

                if (today > response.Data.CodeAccessExpiration) return new(null, 400, "Código expirou, deve solicitar outro");

                dynamic generateCode = Util.GenerateCodeAccess();

                response.Data.Password = BCrypt.Net.BCrypt.HashPassword(request.Password); ;

                await userRepository.UpdateAsync(response.Data);

                return new(new { }, 200, "Senha resetada com sucesso.");
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
                ResponseApi<User?> response = await userRepository.GetByEmailAsync(request.Email);
                if (response.Data is null) return new(null, 400, "E-mail inválido");

                dynamic generateCode = Util.GenerateCodeAccess();

                response.Data.CodeAccess = generateCode.CodeAccess;
                response.Data.CodeAccessExpiration = generateCode.CodeAccessExpiration;
                response.Data.ValidatedAccess = false;

                await userRepository.UpdateAsync(response.Data);

                var res = await mailHelper.SendMail(response.Data.Email, "Código de Verificação", $"Seu código de veficação: {generateCode.CodeAccess}");

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