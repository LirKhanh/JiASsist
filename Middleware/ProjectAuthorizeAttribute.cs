using Microsoft.AspNetCore.Mvc;

namespace JiASsist.Middleware
{
    public class ProjectAuthorizeAttribute : TypeFilterAttribute
    {
        public ProjectAuthorizeAttribute() : base(typeof(ProjectAuthorizeFilter))
        {
        }
    }
}
