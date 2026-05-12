using Dapper;
using JiASsist.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace JiASsist.Controllers.AdministrateModule
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class FilterController : BaseController
    {
        public FilterController(NpgsqlConnection conn) : base(conn)
        { }

        [HttpGet("GetFilters")]
        public async Task<IActionResult> GetAll()
        {
            using var conn = _conn;
            await conn.OpenAsync();

            var sql = "SELECT * FROM filters ORDER BY filter_id";
            var filters = await conn.QueryAsync<Filter>(sql);

            return Ok(new ApiResponse<IEnumerable<Filter>>
            {
                Success = true,
                Message = "Get filters successfully",
                Data = filters,
                Error = ""
            });
        }

        [HttpGet("GetUserFilters/{userId}")]
        public async Task<IActionResult> GetUserFilters(string userId)
        {
            using var conn = _conn;
            await conn.OpenAsync();

            var sql = "SELECT * FROM filters WHERE type ILIKE '%' || @UserId || '%' ORDER BY filter_id";
            var filters = await conn.QueryAsync<Filter>(sql, new { UserId = userId });

            return Ok(new ApiResponse<IEnumerable<Filter>>
            {
                Success = true,
                Message = "Get user filters successfully",
                Data = filters,
                Error = ""
            });
        }

        [HttpPost("AddOrEdit")]
        public async Task<IActionResult> AddOrEdit([FromBody] Filter model)
        {
            using var conn = _conn;
            await conn.OpenAsync();
            string sql = "";

            if (model.ActionType == "A")
            {
                sql = @"INSERT INTO filters (filter_id, type, strsql)
                        VALUES (@FilterId, @Type, @StrSql)
                        RETURNING *";
            }
            else
            {
                sql = @"UPDATE filters SET 
                            type = @Type,
                            strsql = @StrSql
                        WHERE filter_id = @FilterId
                        RETURNING *";
            }

            var result = await conn.QueryFirstOrDefaultAsync<Filter>(sql, model);

            return Ok(new ApiResponse<Filter>
            {
                Success = true,
                Message = model.ActionType == "A" ? "Created successfully" : "Updated successfully",
                Data = result
            });
        }

        [HttpDelete("Delete/{filterId}")]
        public async Task<IActionResult> Delete(string filterId)
        {
            using var conn = _conn;
            await conn.OpenAsync();

            var sql = "DELETE FROM filters WHERE filter_id = @FilterId";
            var result = await conn.ExecuteAsync(sql, new { FilterId = filterId });

            if (result > 0)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Deleted successfully",
                    Data = true
                });
            }

            return BadRequest(new ApiResponse<bool>
            {
                Success = false,
                Message = "Filter not found",
                Data = false
            });
        }
    }
}
