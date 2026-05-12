using Dapper;
using JiASsist.Controllers;
using JiASsist.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data;

[ApiController]
[Route("api/projects")]
public class ProjectsController : BaseController
{
    public ProjectsController(NpgsqlConnection conn) : base(conn)
    { }

    [HttpGet]
    public async Task<IActionResult> GetProjects([FromQuery] string userId)
    {
        using var conn = _conn;
        await conn.OpenAsync();

        var sql = @"
            SELECT a.project_id, a.project_name, a.pm_id, b.fullname AS pm_name
            FROM projects a left join users b on a.pm_id = b.user_id
            WHERE project_id IN (
                SELECT project_id
                FROM user_project
                WHERE user_id = @UserId
            )
            OR pm_id = @UserId
            OR b.admin_yn is true
        ";

        var data = await conn.QueryAsync<Project>(sql, new { UserId = userId });

        return Ok(new ApiResponse<IEnumerable<Project>>
        {
            Success = true,
            Data = data
        });
    }


    [HttpGet("{projectId}")]
    public async Task<IActionResult> GetProject(string projectId)
    {
        using var conn = _conn;
        await conn.OpenAsync();

        var project = await conn.QueryFirstOrDefaultAsync<Project>(@"
            SELECT project_id, project_name, pm_id
            FROM projects
            WHERE project_id = @ProjectId
        ", new { ProjectId = projectId });

        if (project == null)
            return NotFound();

        return Ok(new ApiResponse<Project>
        {
            Success = true,
            Data = project
        });
    }

    [HttpGet("{projectId}/issues")]
    public async Task<IActionResult> GetIssuesByProject(string projectId)
    {
        using var conn = _conn;
        await conn.OpenAsync();

        var issues = (await conn.QueryAsync<Issue>(@"
            SELECT *
            FROM issues
            WHERE project_id = @ProjectId
            ORDER BY update_at DESC
        ", new { ProjectId = projectId })).ToList();

        var latestIssueId = issues.FirstOrDefault()?.IssueId;

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Data = new
            {
                issues,
                latestIssueId
            }
        });
    }
    [HttpGet("assignable-users")]
    public async Task<IActionResult> GetAssignableUsers([FromQuery] string projectId)
    {
        using var conn = _conn;
        await conn.OpenAsync();

        var sql = @"
            SELECT user_id as UserId, username as Username, fullname as Fullname
            FROM users
            WHERE status = true 
            AND pm_yn = false 
            AND admin_yn = false 
            AND user_id NOT IN (
                SELECT user_id
                FROM user_project
                WHERE project_id = @ProjectId
            );
        ";

        var users = await conn.QueryAsync<User>(sql, new { ProjectId = projectId });

        return Ok(new ApiResponse<IEnumerable<User>>
        {
            Success = true,
            Data = users
        });
    }

    [HttpPost("{projectId}/assign-users")]
    public async Task<IActionResult> AssignUsersToProject(string projectId, [FromBody] List<string> userIds)
    {
        if (userIds == null || !userIds.Any())
            return BadRequest(new ApiResponse<string> { Success = false, Message = "User list is empty." });

        using var conn = _conn;
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();

        try
        {
            foreach (var userId in userIds)
            {
                var exists = await conn.ExecuteScalarAsync<bool>(
                    "SELECT EXISTS(SELECT 1 FROM user_project WHERE user_id = @UserId AND project_id = @ProjectId)",
                    new { UserId = userId, ProjectId = projectId },
                    transaction
                );

                if (!exists)
                {
                    await conn.ExecuteAsync(
                        "INSERT INTO user_project (user_id, project_id, update_at) VALUES (@UserId, @ProjectId, @UpdateAt)",
                        new { UserId = userId, ProjectId = projectId, UpdateAt = DateTime.Now },
                        transaction
                    );
                }
            }

            transaction.Commit();
            return Ok(new ApiResponse<string> { Success = true, Message = "Users assigned successfully." });
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return StatusCode(500, new ApiResponse<string> { Success = false, Message = "Error assigning users: " + ex.Message });
        }
    }
}
