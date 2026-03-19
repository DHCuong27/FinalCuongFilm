namespace FinalCuongFilm.MVC.Models.ViewModels
{
	public class UserViewModel
	{
		public string Id { get; set; } = string.Empty;
		public string UserName { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public IList<string> Roles { get; set; } = new List<string>();
		public bool IsLockedOut { get; set; }
 
	}
}