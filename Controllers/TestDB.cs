using Microsoft.AspNetCore.Mvc;
using Dapper;
using Npgsql;
namespace JiASsist.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly NpgsqlConnection _conn;

        public TestController(NpgsqlConnection conn)
        {
            _conn = conn;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using var conn = _conn;
            await conn.OpenAsync();

            var sql = "SELECT role_id, role_name FROM roles";

            var data = await conn.QueryAsync(sql);
            Console.WriteLine(data.Count());
            return Ok(data);
        }
    }
}
