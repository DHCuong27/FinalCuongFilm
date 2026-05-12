using Hangfire.Dashboard;

namespace FinalCuongFilm.MVC.Filters
{
	public class HangfireCustomAuthorizationFilter : IDashboardAuthorizationFilter
	{
		public bool Authorize(DashboardContext context)
		{
			var httpContext = context.GetHttpContext();

			// MẸO: Nếu đang chạy trên máy cá nhân (localhost) thì mở cửa luôn, không cần hỏi vé
			if (httpContext.Request.Host.Host == "localhost")
			{
				return true;
			}

			// Nếu chạy trên mạng (Railway/Azure) thì bắt buộc phải đăng nhập và là Admin
			return httpContext.User.Identity != null &&
				   httpContext.User.Identity.IsAuthenticated &&
				   httpContext.User.IsInRole("Admin");
		}
	}
}