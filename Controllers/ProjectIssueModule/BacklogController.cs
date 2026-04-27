using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace JiASsist.Controllers.ProjectIssueModule
{
    [ApiController]
    [Route("api/backlog")]
    public class BacklogController : BaseController
    {
        public BacklogController(NpgsqlConnection conn) : base(conn)
        { }


    }
}
