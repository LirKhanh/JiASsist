using Dapper;
using JiASsist.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace JiASsist.Controllers.AdministrateModule
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class ProjectController : BaseController
    {
        public ProjectController(NpgsqlConnection conn) : base(conn)
        {}

        [HttpGet("GetProject")]
        public async Task<IActionResult> GetProject()
        {
            using var conn = _conn;
            await conn.OpenAsync();

            var sql = @"SELECT * FROM projects";

            var projects  = await conn.QueryAsync<Project>(sql);

            return Ok(new ApiResponse<IEnumerable<Project>>
            {
                Success = true,
                Message = "Get projects successfully",
                Data = projects,
                Error = ""
            });
        }


        [HttpPost("AddOrEdit")]
        public async Task<IActionResult> AddOrEdit([FromBody] Project model)
        {
            using var conn = _conn;
            string sql = "";
            await conn.OpenAsync();
            if (model.ActionType == "A")
            {
                model.CreatedAt = DateTime.UtcNow;
                sql = @"INSERT INTO projects(project_id, project_name, description,pm_id,start_date, end_date, status, created_by, created_at)
                VALUES (@ProjectId, @ProjectName, @Description,@PmId,@StartDate, @EndDate,  @Status, @CreatedBy, @CreatedAt)
                RETURNING *";
            }
            else
            {
                model.UpdateAt = DateTime.UtcNow;
                sql = @"UPDATE projects set project_name = @ProjectName, description = @Description,pm_id = @PmId,start_date = @StartDate, end_date =@EndDate, status = @Status, update_by = @UpdateBy, update_at = @UpdateAt
                WHERE project_id = @ProjectId
                RETURNING *";
            }

            var result = await conn.QueryFirstOrDefaultAsync<Project>(sql, model);

            return Ok(new ApiResponse<Project>
            {
                Success = true,
                Message = "Created successfully",
                Data = result
            });
        }
    }
}
