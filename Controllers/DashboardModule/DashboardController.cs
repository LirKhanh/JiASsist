using Npgsql;

namespace JiASsist.Controllers.DashboardModule
{
    public class AccountController : BaseController
    {
        public AccountController(NpgsqlConnection conn) : base(conn)
        { }
    }
}
