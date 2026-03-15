namespace FinalCuongFilm.MVC.Models.ViewModels
{
	public class UserViewModel
	{
		public string Id { get; set; } = string.Empty;
		public string UserName { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public IList<string> Roles { get; set; } = new List<string>();
		public bool IsLockedOut { get; set; }

		// Nếu ApplicationUser của bạn có trường FullName hoặc Avatar thì thêm vào đây
		// public string FullName { get; set; } 
	}
}