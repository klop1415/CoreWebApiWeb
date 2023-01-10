using System.Security.Claims;

namespace CoreWebApiWeb.Services.UsersService
{
    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor accessor;

        public UserService(IHttpContextAccessor accessor) {
            this.accessor = accessor;
        }
        public string GetName()
        {
            var result = string.Empty;
            if (accessor.HttpContext != null)
            {
                result = accessor.HttpContext.User.FindFirstValue(ClaimTypes.Name);
            }
            return result;
        }
    }
}
