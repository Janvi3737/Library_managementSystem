using LibraryManagementSystem.ClassLibrary.Data;
using LibraryManagementSystem.ClassLibrary.Models;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LibraryManagementSystem.Filters
{
    /// <summary>
    /// Writes an AuditLog row for every successful admin mutation (POST/PUT/DELETE).
    /// Skips GETs to keep the log noise-free. Skips the audit controller itself
    /// and the backup download (which is read-only).
    /// </summary>
    public class AuditLogFilter : IAsyncActionFilter
    {
        private readonly AppDbContext _db;

        public AuditLogFilter(AppDbContext db)
        {
            _db = db;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var executed = await next();

            var http = context.HttpContext;
            var method = http.Request.Method;
            if (!IsMutating(method))
                return;

            // Only log if no exception occurred.
            if (executed.Exception != null)
                return;

            var controller = (context.RouteData.Values["controller"] as string) ?? "";
            var action = (context.RouteData.Values["action"] as string) ?? "";

            // Avoid recursive log entries when viewing the log itself.
            if (controller.Equals("Audit", StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                _db.AuditLogs.Add(new AuditLog
                {
                    UserEmail = http.User?.Identity?.Name,
                    Action = $"{method} /{controller}/{action}",
                    Controller = controller,
                    ActionName = action,
                    Details = http.Request.Path + http.Request.QueryString
                });
                await _db.SaveChangesAsync();
            }
            catch
            {
                // Audit log failure must never break the user's action.
            }
        }

        private static bool IsMutating(string method) =>
            method == HttpMethods.Post ||
            method == HttpMethods.Put ||
            method == HttpMethods.Delete ||
            method == HttpMethods.Patch;
    }
}
