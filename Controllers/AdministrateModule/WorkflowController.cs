using Dapper;
using JiASsist.Helpers;
using JiASsist.Models;
using Npgsql;

using JiASsist.Models.AuthModule;
using Microsoft.AspNetCore.Mvc;

namespace JiASsist.Controllers.AdministrateModule
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class WorkflowController : BaseController
    {
        public WorkflowController(NpgsqlConnection conn) : base(conn)
        { }

        [HttpGet("GetWorkFlow")]
        public async Task<IActionResult> GetAll()
        {
            using var conn = _conn;
            await conn.OpenAsync();

            var sql = @"SELECT * FROM workflow_step";

            var workflowSteps = await conn.QueryAsync<WorkflowStep>(sql);

            return Ok(new ApiResponse<IEnumerable<WorkflowStep>>
            {
                Success = true,
                Message = "Get workflow steps successfully",
                Data = workflowSteps,
                Error = ""
            });
        }


        [HttpPost("AddOrEdit")]
        public async Task<IActionResult> AddOrEdit([FromBody] WorkflowStep model)
        {
            using var conn = _conn;
            string sql = "";
            await conn.OpenAsync();
            if (model.ActionType == "A")
            {
                model.CreatedAt = DateTime.UtcNow;
                sql = @"INSERT INTO workflow_step(step_id, step_name, step, status, created_by, created_at)
                VALUES (@StepId, @StepName, @Step, @Status, @CreatedBy, @CreatedAt)
                RETURNING *";
            }
            else {
                model.UpdateAt = DateTime.UtcNow;
                sql = @"UPDATE workflow_step set step_name = @StepName, step = @Step, status = @Status, update_by = @UpdateBy, update_at = @UpdateAt
                WHERE step_id = @StepId
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
