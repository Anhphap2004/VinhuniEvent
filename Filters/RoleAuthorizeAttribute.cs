using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore; // nếu cần truy vấn database
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
            var userId = context.HttpContext.Session.GetInt32("UserId");

            // 1. Trường hợp chưa đăng nhập
            if (userId == null)
            {
                // Redirect về trang Login
                context.Result = new RedirectToActionResult("Index", "Login", new { area = "" });
                return;
            }

            var dbContext = context.HttpContext.RequestServices.GetService<VinhuniEvent.Models.ApplicationDbContext>();
            var user = dbContext?.Users.FirstOrDefault(u => u.UserId == userId.Value);

            // 2. Trường hợp không đủ quyền (RoleId không nằm trong danh sách cho phép)
            if (user == null || !_allowedRoles.Contains(user.RoleId))
            {
                // Trả về trang View 403 đã tạo ở bước 1
                context.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/AccessDenied.cshtml"
                };
                return;
            }

            base.OnActionExecuting(context);
        }

    }
}
