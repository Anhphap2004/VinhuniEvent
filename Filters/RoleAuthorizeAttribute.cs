using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore; 
using VinhuniEvent.Models;

namespace VinhuniEvent.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RoleAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly int[] _allowedRoles;

        public RoleAuthorizeAttribute(params int[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Lấy UserId từ session
            var userId = context.HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                // Chưa login -> trả 403
                context.Result = new StatusCodeResult(403);
                return;
            }

            // Lấy DbContext từ RequestServices
            var dbContext = context.HttpContext.RequestServices.GetService<VinhuniEvent.Models.ApplicationDbContext>();
            var user = dbContext?.Users.FirstOrDefault(u => u.UserId == userId.Value);

            if (user == null || !_allowedRoles.Contains(user.RoleId))
            {
                context.Result = new StatusCodeResult(403);
                return;
            }

            base.OnActionExecuting(context);
        }

    }
}
