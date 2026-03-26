using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;
using JiASsist.Helpers;
using JiASsist.Models;
using JiASsist.Models.AuthModule;
using Microsoft.AspNetCore.Authorization;


namespace JiASsist.Controllers.AuthModule
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly NpgsqlConnection _conn;
        private readonly JwtHelper _jwtHelper;
        private readonly JwtSettings _jwtSettings;

        public AuthenticationController(NpgsqlConnection conn, JwtHelper jwtHelper, IOptions<JwtSettings> jwtOptions)
        {
            _conn = conn;
            _jwtHelper = jwtHelper;
            _jwtSettings = jwtOptions.Value;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            using var conn = _conn;
            await conn.OpenAsync();
            var sql = @"SELECT user_id as UserId, username as Username, password as Password, status as Status, fullName as FullName, role_id as RoleId
                        FROM users
                        WHERE username = @Username
                        LIMIT 1";

            var user = await conn.QueryFirstOrDefaultAsync<User>(sql, new { request.Username });
            if (user == null || user.Status != true)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid credentials",
                    Data = null,
                    Error = "Tài khoản không tồn tại hoặc chưa được duyệt"
                });
            }

            // Verify hashed password
            //PasswordHasher.Hash(request.Password);
            if (string.IsNullOrEmpty(user.Password) || !PasswordHasher.Verify(user.Password, request.Password))
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid credentials",
                    Data = null,
                    Error = "Mật khẩu không chính xác!"
                });
            }

            var token = _jwtHelper.GenerateToken(user);

            var response = new LoginResponse
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes),
                User = user
            };
            PasswordHasher.Hash(request.Password);
            return Ok(new ApiResponse<LoginResponse>
            {
                Success = true,
                Message = "Login successful",
                Data = response,
                Error = ""
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            using var conn = _conn;
            await conn.OpenAsync();

            var existsSql = "SELECT 1 FROM users WHERE username = @Username LIMIT 1";
            var exists = await conn.QueryFirstOrDefaultAsync<int?>(existsSql, new { request.Username });
            if (exists != null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Username already exists",
                    Data = null,
                    Error = ""
                });
            }

            // Retrieve new user id from DB function. Fallback to GUID if function fails.
            string? userId = null;
            try
            {
                userId = await conn.ExecuteScalarAsync<string>("select get_next_user_id(@schema)", new { schema = "JiASsist" });
            }
            catch
            {
                // ignore and fallback
            }

            if (string.IsNullOrEmpty(userId))
            {
                userId = Guid.NewGuid().ToString();
            }

            var hashed = PasswordHasher.Hash(request.Password);

            var insertSql = @"INSERT INTO users (user_id, username, password, email, fullname, role_id, status, created_at, created_by)
                               VALUES (@UserId, @Username, @Password, @Email, @Fullname, @RoleId, @Status, @CreatedAt, @CreatedBy)";

            var parameters = new
            {
                UserId = userId,
                Username = request.Username,
                Password = hashed,
                Email = request.Email,
                Fullname = request.Fullname,
                RoleId = (string?)null,
                Status = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            await conn.ExecuteAsync(insertSql, parameters);

            var user = new User
            {
                UserId = userId,
                Username = request.Username,
                Email = request.Email,
                Fullname = request.Fullname,
                RoleId = null,
                Status = false
            };

            return Ok(new ApiResponse<User>
            {
                Success = true,
                Message = "Register successful",
                Data = user,
                Error = ""
            });
        }
    }
}
