using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JiASsist.Middleware
{
    public class ProjectAuthorizeFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // ADMIN → bỏ qua
            if (user.IsInRole("ADMIN"))
                return;

            // chỉ cho PM
            if (!user.IsInRole("PM"))
            {
                context.Result = new ForbidResult();
                return;
            }

            var projectId = context.RouteData.Values["projectId"]?.ToString();

            if (string.IsNullOrEmpty(projectId))
            {
                context.Result = new ForbidResult();
                return;
            }

            var projects = user.FindFirst("projects")?.Value?.Split(',');

            if (projects == null || !projects.Contains(projectId))
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
