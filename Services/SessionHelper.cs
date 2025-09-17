namespace ServerCRM.Services
{
    public static class SessionHelper
    {
        private static IHttpContextAccessor _httpContextAccessor;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public static string GetLoginCode()
        {
            return _httpContextAccessor?.HttpContext?.Session?.GetString("login_code");
        }
    }

}
