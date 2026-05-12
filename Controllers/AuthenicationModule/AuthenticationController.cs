using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;
using JiASsist.Helpers;
using JiASsist.Models;
using JiASsist.Models.AuthModule;
using Microsoft.AspNetCore.Authorization;


namespace JiASsist.Controllers.AuthenicationModule
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
            var sql = @"SELECT user_id as UserId, username as Username, password as Password, 
                               status as Status, fullName as FullName, admin_yn as AdminYn,pm_yn as PmYn
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
            var workflowSteps = await conn.QueryAsync<WorkflowStep>(@"SELECT step_id as StepId, step_name as StepName, step as Step, pre_step_id as PreStepId, next_step_id as NextStepId
                                FROM workflow_step
                                WHERE status is true order by step;
                                ");
            var issuePriorities = await conn.QueryAsync<IssuePriority>(@"SELECT issue_priority_id as IssuePriorityId, issue_priority_name as IssuePriorityName
	                            FROM issue_priorities where status is true;");
            var issueTypes = await conn.QueryAsync<IssueType>(@"SELECT issue_type_id as IssueTypeId, issue_type_name as IssueTypeName
	                            FROM issue_types where status is true;");
            var response = new LoginResponse
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes),
                User = user,
                WorkflowStep = workflowSteps,
                IssuePriorities = issuePriorities,
                IssueTypes = issueTypes,
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

            var insertSql = @"INSERT INTO users (user_id, username, password, email, fullname, status, created_at, created_by)
                               VALUES (@UserId, @Username, @Password, @Email, @Fullname, @Status, @CreatedAt, @CreatedBy)";

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
