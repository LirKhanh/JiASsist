using Dapper;
using JiASsist.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JiASsist.Controllers.DashboardModule
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : BaseController
    {
        public DashboardController(NpgsqlConnection conn) : base(conn)
        { }

        [HttpGet("summary")]
        public async Task<IActionResult> GetDashboardSummary([FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "UserId is required" });
            }

            using var conn = _conn;
            await conn.OpenAsync();

            // 1. Get Projects for the User
            var projectsSql = @"
                SELECT a.project_id as key, a.project_name as name, a.pm_id, b.fullname AS lead
                FROM projects a LEFT JOIN users b ON a.pm_id = b.user_id
                WHERE project_id IN (
                    SELECT project_id FROM user_project WHERE user_id = @UserId
                ) OR pm_id = @UserId;
            ";
            var projects = await conn.QueryAsync<dynamic>(projectsSql, new { UserId = userId });

            // 2. Get Dynamic Filters configured for this user or system
            var filtersSql = @"
                SELECT filter_id, type, strsql 
                FROM filters 
                WHERE type = 'system' OR type ILIKE '%' || @UserId || '%'
            ";
            var filters = await conn.QueryAsync<dynamic>(filtersSql, new { UserId = userId });

            var dynamicFilters = new List<object>();

            // 3. Execute each filter's SQL
            foreach (var filter in filters)
            {
                try 
                {
                    string sql = filter.strsql;
                    var issues = await conn.QueryAsync<dynamic>(sql, new { UserId = userId });
                    dynamicFilters.Add(new 
                    {
                        filterId = filter.filter_id,
                        issues = issues
                    });
                }
                catch (Exception ex)
                {
                    // Log error but continue with other filters
                    Console.WriteLine($"Error executing filter {filter.filter_id}: {ex.Message}");
                    dynamicFilters.Add(new 
                    {
                        filterId = filter.filter_id,
                        error = "Lỗi khi chạy query: " + ex.Message,
                        issues = new List<object>()
                    });
                }
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new
                {
                    projects = projects,
                    dynamicFilters = dynamicFilters
                }
            });
        }
    }
}
