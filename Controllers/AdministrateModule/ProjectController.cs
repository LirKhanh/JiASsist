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

        [HttpGet("project")]
        public async Task<IActionResult> GetAll()
        {
            using var conn = _conn;
            await conn.OpenAsync();

            var sql = @"SELECT * FROM projects";

            var projects  = await conn.QueryAsync<Project>(sql);

            return Ok(new ApiResponse<IEnumerable<Project>>
            {
                Success = true,
                Message = "Get workflow steps successfully",
                Data = projects,
                Error = ""
            });
        }


        [HttpPost("project")]
        public async Task<IActionResult> AddAndEdit([FromBody] Project model)
        {
            using var conn = _conn;
            string sql = "";
            await conn.OpenAsync();
            if (model.ActionType == "A")
            {
                sql = @"INSERT INTO projects(project_id, project_name, description,pm_id,start_date, end_date, status, created_by, created_at)
                VALUES (@ProjectId, @ProjectName, @Description,@PmId,@StartDate, @EndDate,  @Status, @CreatedBy, @CreatedAt)
                RETURNING *";
            }
            else
            {
                sql = @"UPDATE projects set project_name = @ProjectName, description = @Description,pm_id = @PmId,start_date = @StartDate, end_date =@EndDate, status = @Status, update_by = @UpdateBy, update_at = @UpdateAt
                WHERE project_id = @ProjectId
                RETURNING *";
            }

            var result = await conn.QueryFirstOrDefaultAsync<WorkflowStep>(sql, model);

            return Ok(new ApiResponse<WorkflowStep>
            {
                Success = true,
                Message = "Created successfully",
                Data = result
            });
        }
    }
}
