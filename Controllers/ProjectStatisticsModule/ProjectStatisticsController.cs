using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using JiASsist.Models;
using JiASsist.Services;
using System.Text.Json;

namespace JiASsist.Controllers.ProjectStatisticsModule
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectStatisticsController : ControllerBase
    {
        private readonly NpgsqlConnection _conn;
        private readonly AiService _aiService;

        public ProjectStatisticsController(NpgsqlConnection conn, AiService aiService)
        {
            _conn = conn;
            _aiService = aiService;
        }

        [HttpGet("sprints/{projectId}")]
        public async Task<IActionResult> GetSprints(string projectId)
        {
            using var conn = _conn;
            await conn.OpenAsync();
            var sql = "SELECT sprint_id as SprintId, sprint_name as SprintName FROM sprints WHERE project_id = @projectId";
            var sprints = await conn.QueryAsync<Sprint>(sql, new { projectId });
            return Ok(new ApiResponse<IEnumerable<Sprint>>
            {
                Success = true,
                Data = sprints
            });
        }

        [HttpGet("epics/{projectId}")]
        public async Task<IActionResult> GetEpics(string projectId)
        {
            using var conn = _conn;
            await conn.OpenAsync();
            var sql = "SELECT DISTINCT epic_id FROM issues WHERE project_id = @projectId AND epic_id IS NOT NULL";
            var epics = await conn.QueryAsync<string>(sql, new { projectId });
            return Ok(new ApiResponse<IEnumerable<string>>
            {
                Success = true,
                Data = epics
            });
        }

        [HttpGet("evaluate-sprint/{projectId}/{sprintId}")]
        public async Task<IActionResult> EvaluateSprint(string projectId, string sprintId)
        {
            using var conn = _conn;
            await conn.OpenAsync();
            
            var sprint = await conn.QueryFirstOrDefaultAsync<Sprint>(
                "SELECT * FROM sprints WHERE sprint_id = @sprintId AND project_id = @projectId", 
                new { sprintId, projectId });
            
            if (sprint == null) return Ok(new ApiResponse<string> { Success = false, Message = "Sprint không tồn tại." });

            var issues = await conn.QueryAsync<Issue>(
                "SELECT * FROM issues WHERE sprint_id = @sprintId AND project_id = @projectId", 
                new { sprintId, projectId });

            if (!issues.Any()) return Ok(new ApiResponse<string> { Success = false, Message = "Không có issue nào trong sprint này." });

            var users = await conn.QueryAsync<User>("SELECT user_id as UserId, username as Username FROM users");
            
            // Lấy log cũ và trích xuất phần evaluation
            var lastLogJson = await conn.QueryFirstOrDefaultAsync<string>(
                @"SELECT log_content FROM analyst_log 
                  WHERE log_content->>'target_id' = @sprintId 
                  ORDER BY log_date DESC LIMIT 1", new { sprintId });

            string? previousEvaluation = null;
            if (!string.IsNullOrEmpty(lastLogJson))
            {
                try {
                    using var doc = JsonDocument.Parse(lastLogJson);
                    if (doc.RootElement.TryGetProperty("evaluation", out var evalProp))
                        previousEvaluation = evalProp.GetString();
                } catch { previousEvaluation = lastLogJson; }
            }

            var context = new {
                Type = "SPRINT",
                Sprint = sprint,
                Users = users.ToDictionary(u => u.UserId, u => u.Username),
                LastLog = previousEvaluation,
                CurrentDate = DateTime.Now
            };

            var rawEvaluationJson = await _aiService.EvaluateProgressAsync(issues, $"Sprint: {sprint.SprintName}", context);

            try {
                var logEntry = new {
                    target_id = sprintId,
                    evaluation = rawEvaluationJson
                };
                await conn.ExecuteAsync(
                    "INSERT INTO analyst_log (log_content, log_date) VALUES (@Content::json, @Date)", 
                    new { Content = JsonSerializer.Serialize(logEntry), Date = DateTime.Now });
            } catch (Exception ex) {
                Console.WriteLine("Lỗi Insert Log: " + ex.Message);
            }

            var formattedText = _aiService.FormatEvaluationToText(rawEvaluationJson);
            return Ok(new ApiResponse<string> { Success = true, Data = formattedText });
        }

        [HttpGet("evaluate-epic/{projectId}/{epicId}")]
        public async Task<IActionResult> EvaluateEpic(string projectId, string epicId)
        {
            using var conn = _conn;
            await conn.OpenAsync();

            var issues = await conn.QueryAsync<Issue>(
                "SELECT * FROM issues WHERE epic_id = @epicId AND project_id = @projectId", 
                new { epicId, projectId });

            if (!issues.Any()) return Ok(new ApiResponse<string> { Success = false, Message = "Không có issue nào trong epic này." });

            var users = await conn.QueryAsync<User>("SELECT user_id as UserId, username as Username FROM users");
            
            var lastLogJson = await conn.QueryFirstOrDefaultAsync<string>(
                @"SELECT log_content FROM analyst_log 
                  WHERE log_content->>'target_id' = @epicId 
                  ORDER BY log_date DESC LIMIT 1", new { epicId });

            string? previousEvaluation = null;
            if (!string.IsNullOrEmpty(lastLogJson))
            {
                try {
                    using var doc = JsonDocument.Parse(lastLogJson);
                    if (doc.RootElement.TryGetProperty("evaluation", out var evalProp))
                        previousEvaluation = evalProp.GetString();
                } catch { previousEvaluation = lastLogJson; }
            }

            var context = new {
                Type = "EPIC",
                Users = users.ToDictionary(u => u.UserId, u => u.Username),
                LastLog = previousEvaluation,
                CurrentDate = DateTime.Now
            };

            var rawEvaluationJson = await _aiService.EvaluateProgressAsync(issues, $"Epic: {epicId}", context);

            try {
                var logEntry = new {
                    target_id = epicId,
                    evaluation = rawEvaluationJson
                };
                await conn.ExecuteAsync(
                    "INSERT INTO analyst_log (log_content, log_date) VALUES (@Content::json, @Date)", 
                    new { Content = JsonSerializer.Serialize(logEntry), Date = DateTime.Now });
            } catch (Exception ex) {
                Console.WriteLine("Lỗi Insert Log: " + ex.Message);
            }

            var formattedText = _aiService.FormatEvaluationToText(rawEvaluationJson);
            return Ok(new ApiResponse<string> { Success = true, Data = formattedText });
        }

        [HttpPost("chat/{projectId}")]
        public async Task<IActionResult> Chat(string projectId, [FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new ApiResponse<string> { Success = false, Message = "Message is required." });

            using var conn = _conn;
            await conn.OpenAsync();

            var query = "SELECT * FROM issues WHERE project_id = @projectId AND status = true";
            var contextName = "Toàn bộ Dự án";

            if (!string.IsNullOrEmpty(request.SprintId))
            {
                query += " AND sprint_id = @sprintId";
                var sprintName = await conn.QueryFirstOrDefaultAsync<string>("SELECT sprint_name FROM sprints WHERE sprint_id = @sprintId", new { request.SprintId });
                contextName = $"Sprint: {sprintName ?? request.SprintId}";
            }
            if (!string.IsNullOrEmpty(request.EpicId))
            {
                query += " AND epic_id = @epicId";
                var epicPrefix = contextName == "Toàn bộ Dự án" ? "Epic: " : " & Epic: ";
                contextName += $"{epicPrefix}{request.EpicId}";
            }

            var issues = await conn.QueryAsync<Issue>(query, new { projectId, sprintId = request.SprintId, epicId = request.EpicId });
            var users = await conn.QueryAsync<User>("SELECT user_id as UserId, username as Username FROM users");

            var context = new {
                CurrentDate = DateTime.Now,
                UsersMap = users.ToDictionary(u => u.UserId, u => u.Username)
            };

            var aiResponse = await _aiService.ChatWithAssistantAsync(issues, contextName, context, request.Message);

            return Ok(new ApiResponse<string> { Success = true, Data = aiResponse });
        }
    }

    public class ChatRequest
    {
        public string? SprintId { get; set; }
        public string? EpicId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
