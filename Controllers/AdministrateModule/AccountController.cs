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

            var sql = @"SELECT u.*, 
                               string_agg(up.project_id, ', ') AS project_join
                        FROM users u
                        LEFT JOIN user_project up 
                               ON up.user_id = u.user_id
                        GROUP BY u.user_id
                        ORDER BY u.status, u.user_id;";

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
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();

            try
            {
                string sql = "";
                model.Password = PasswordHasher.Hash(model.Password);

                var projectIds = string.IsNullOrWhiteSpace(model.ProjectJoin)
                    ? new List<string>()
                    : model.ProjectJoin.Split(',')
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();

                if (model.ActionType == "A")
                {
                    model.CreatedAt = DateTime.UtcNow;

                    try
                    {
                        model.UserId = await conn.ExecuteScalarAsync<string>(
                            "select get_next_user_id(@schema)",
                            new { schema = "JiASsist" }, tran
                        );
                    }
                    catch
                    {
                        model.UserId = Guid.NewGuid().ToString();
                    }

                    sql = @"INSERT INTO users(
                user_id, username, password, email, fullname, status, created_at, created_by, admin_yn, pm_yn)
            VALUES (@UserId, @UserName, @Password, @Email, @Fullname, @Status, @CreatedAt, @CreatedBy, @AdminYn, @PmYn)
            RETURNING *";
                }
                else
                {
                    model.UpdateAt = DateTime.UtcNow;

                    sql = @"UPDATE users SET 
                    username = @UserName,
                    email = @Email,
                    admin_yn = @AdminYn,
                    pm_yn = @PmYn,
                    status = @Status,
                    update_by = @UpdateBy,
                    update_at = @UpdateAt,
                    fullname = @Fullname
                WHERE user_id = @UserId
                RETURNING *";
                }

                var result = await conn.QueryFirstOrDefaultAsync<User>(sql, model, tran);

                await conn.ExecuteAsync(
                    "DELETE FROM user_project WHERE user_id = @UserId",
                    new { model.UserId }, tran
                );

                var now = DateTime.UtcNow;

                if (projectIds.Any())
                {
                    var insertProjectSql = @"
                        INSERT INTO user_project(user_id, project_id, update_at)
                        VALUES (@UserId, @ProjectId, @UpdateAt)";

                    foreach (var p in projectIds.Distinct())
                    {
                        await conn.ExecuteAsync(insertProjectSql,
                            new
                            {
                                model.UserId,
                                ProjectId = p,
                                UpdateAt = now
                            }, tran);
                    }
                }

                await tran.CommitAsync();

                return Ok(new ApiResponse<User>
                {
                    Success = true,
                    Message = model.ActionType == "A" ? "Created successfully" : "Updated successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }
    }
}
