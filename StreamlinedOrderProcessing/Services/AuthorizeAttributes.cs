using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StreamlinedOrderProcessing.Models;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AuthorizeRolesAttribute : TypeFilterAttribute
{
    public AuthorizeRolesAttribute(params UserRole[] roles) : base(typeof(RolesFilter))
    {
        Arguments = [roles.Select(r => r.ToString()).ToArray()];
    }
}

public class RolesFilter(string[] roles) : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // Если не авторизован или роль пользователя не входит в список разрешенных
        if (!user.Identity!.IsAuthenticated || !roles.Any(role => user.IsInRole(role)))
        {
            context.Result = new ForbidResult();
        }
    }
}

public enum UserRole
{
    Admin,
    Manager,
    Employee
}