using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace JiASsist.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseController : ControllerBase
    {
        protected readonly NpgsqlConnection _conn;

        public BaseController(NpgsqlConnection conn)
        {
            _conn = conn;
        }

        public class LoadComboboxRequest
        {
            public string[] Queries { get; set; } = Array.Empty<string>();
            public string[] Keys { get; set; } = Array.Empty<string>(); // tên key tương ứng với dữ liệu trả về
        }

        [HttpPost("LoadDataCombobox")]
        public async Task<IActionResult> LoadDataCombobox([FromBody] LoadComboboxRequest request)
        {
            if (request.Queries == null || request.Queries.Length == 0)
                return BadRequest(new { Success = false, Message = "No queries provided" });

            if (request.Keys == null || request.Keys.Length != request.Queries.Length)
                return BadRequest(new { Success = false, Message = "Keys must be provided and match queries length" });

            var result = new Dictionary<string, IEnumerable<dynamic>>();

            try
            {
                using var conn = _conn;
                await conn.OpenAsync();

                for (int i = 0; i < request.Queries.Length; i++)
                {
                    var sql = request.Queries[i];

                    if (string.IsNullOrWhiteSpace(sql))
                    {
                        result[request.Keys[i]] = Array.Empty<dynamic>();
                        continue;
                    }

                    var data = await conn.QueryAsync(sql);
                    result[request.Keys[i]] = data;
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Load data successfully",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error loading data",
                    Error = ex.Message
                });
            }
        }
    }
}