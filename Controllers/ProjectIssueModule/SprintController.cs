using Dapper;
using JiASsist.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace JiASsist.Controllers.ProjectIssueModule
{
    [ApiController]
    [Route("api/sprints")]
    public class SprintsController : ControllerBase
    {
        private readonly NpgsqlConnection _conn;

        public SprintsController(NpgsqlConnection conn)
        {
            _conn = conn;
        }

        // =========================
        // GET ALL BY PROJECT
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetAll(string projectId)
        {
            using var conn = _conn;
            await conn.OpenAsync();

            var data = await conn.QueryAsync<Sprint>(@"
                SELECT *
                FROM sprints
                WHERE project_id = @projectId
                ORDER BY start_date DESC
            ", new { projectId });

            return Ok(new ApiResponse<IEnumerable<Sprint>>
            {
                Success = true,
                Data = data
            });
        }

        // =========================
        // GET BY ID
        // =========================
        [HttpGet("{sprintId}/{projectId}")]
        public async Task<IActionResult> GetById(string sprintId, string projectId)
        {
            using var conn = _conn;
            await conn.OpenAsync();

            var data = await conn.QueryFirstOrDefaultAsync<Sprint>(@"
                SELECT *
                FROM sprints
                WHERE sprint_id = @sprintId AND project_id = @projectId
            ", new { sprintId, projectId });

            if (data == null)
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Sprint not found"
                });

            return Ok(new ApiResponse<Sprint>
            {
                Success = true,
                Data = data
            });
        }

        // =========================
        // CREATE
        // =========================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Sprint model)
        {
            using var conn = _conn;
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();
            model.SprintId = await conn.QueryFirstOrDefaultAsync<string>("select get_next_sprint_id(@projectId)", new { projectId = model.ProjectId }, tran);
            try
            {
                var sql = @"
                    INSERT INTO sprints(
                        sprint_id, project_id, sprint_name,
                        start_date, end_date, status,
                        created_at, created_by, update_at, update_by
                    )
                    VALUES (
                        @SprintId, @ProjectId, @SprintName,
                        @StartDate, @EndDate, @Status,
                        NOW(), @CreatedBy, NOW(), @UpdateBy
                    )
                ";

                await conn.ExecuteAsync(sql, model, tran);

                await tran.CommitAsync();

                return Ok(new ApiResponse<Sprint>
                {
                    Success = true,
                    Data = model
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

        // =========================
        // UPDATE
        // =========================
        [HttpPut("{sprintId}/{projectId}")]
        public async Task<IActionResult> Update(string sprintId, string projectId, [FromBody] Sprint model)
        {
            using var conn = _conn;
            await conn.OpenAsync();

            var sql = @"
                UPDATE sprints SET
                    sprint_name = @SprintName,
                    start_date = @StartDate,
                    end_date = @EndDate,
                    status = @Status,
                    update_at = NOW(),
                    update_by = @UpdateBy
                WHERE sprint_id = @SprintId AND project_id = @ProjectId
            ";

            var rows = await conn.ExecuteAsync(sql, new
            {
                SprintId = sprintId,
                ProjectId = projectId,
                model.SprintName,
                model.StartDate,
                model.EndDate,
                model.Status,
                model.UpdateBy
            });

            if (rows == 0)
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Sprint not found"
                });

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Updated successfully"
            });
        }

        // =========================
        // DELETE
        // =========================
        [HttpDelete("{sprintId}/{projectId}")]
        public async Task<IActionResult> Delete(string sprintId, string projectId)
        {
            using var conn = _conn;
            await conn.OpenAsync();

            var rows = await conn.ExecuteAsync(@"
                DELETE FROM sprints
                WHERE sprint_id = @sprintId AND project_id = @projectId
            ", new { sprintId, projectId });

            if (rows == 0)
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Sprint not found"
                });

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Message = "Deleted successfully"
            });
        }
    }
}