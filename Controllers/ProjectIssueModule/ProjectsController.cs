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
            OR pm_id = @UserId;
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
            SELECT issue_id, issue_name, issue_status,issue_type,issue_priority_id, project_id, update_at
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
}