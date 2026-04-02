using Dapper;
using JiASsist.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Microsoft.Extensions.Options;
using JiASsist.Helpers;
using JiASsist.Models.AuthModule;
using Microsoft.AspNetCore.Authorization;

namespace JiASsist.Controllers.AdministrateModule
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class AccountController : BaseController
    {
        public AccountController(NpgsqlConnection conn) : base(conn)
        { }
        [HttpGet("GetAccount")]
        public async Task<IActionResult> GetAll()
        {
            using var conn = _conn;
            await conn.OpenAsync();

            var sql = @"SELECT * FROM users order by status, user_id";

            var users = await conn.QueryAsync<User>(sql);

            return Ok(new ApiResponse<IEnumerable<User>>
            {
                Success = true,
                Message = "Get users successfully",
                Data = users,
                Error = ""
            });
        }
        [HttpPost("AddOrEdit")]
        public async Task<IActionResult> AddOrEdit([FromBody] User model)
        {
            using var conn = _conn;
            string sql = "";
            model.Password = PasswordHasher.Hash(model.Password); ;

            await conn.OpenAsync();
            if (model.ActionType == "A")
            {
                model.CreatedAt = DateTime.UtcNow;
                try
                {
                    model.UserId = await conn.ExecuteScalarAsync<string>(
                        "select get_next_user_id(@schema)",
                        new { schema = "JiASsist" }
                    );
                }
                catch
                {
                    model.UserId = Guid.NewGuid().ToString();
                }
                sql = @"INSERT INTO users(
	            user_id, username, password, email, fullname, project_join, status, created_at, created_by, admin_yn, pm_yn)
                VALUES (@UserId, @UserName,@Password, @Email,@Fullname,@ProjectJoin, @Status, @CreatedAt, @CreatedBy, @AdminYn, @PmYn)
                RETURNING *";
            }
            else
            {
                model.UpdateAt = DateTime.UtcNow;
                sql = @"UPDATE users set 
                        username = @UserName,email = @Email, project_join = @ProjectJoin, admin_yn = @AdminYn, pm_yn = @PmYn,
                        status = @Status, update_by = @UpdateBy, update_at = @UpdateAt, fullname= @Fullname
                WHERE user_id = @UserId
                RETURNING *";
            }

            var result = await conn.QueryFirstOrDefaultAsync<User>(sql, model);

            return Ok(new ApiResponse<User>
            {
                Success = true,
                Message = "Created successfully",
                Data = result
            });
        }
    }
}
