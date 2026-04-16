namespace FinalCuongFilm.Common.DTOs
{
	public class UserDto
	{
		public string Id { get; set; }
		public string FullName { get; set; }
		public string Email { get; set; }
		public string AvatarUrl { get; set; }
		public IList<string> Roles { get; set; }
		public bool IsActive { get; set; } // Trạng thái bị khóa hay đang hoạt động
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}

	public class ManageUserRolesViewModel
	{
		public string UserId { get; set; }
		public string Email { get; set; }
		public List<RoleSelection> Roles { get; set; } = new List<RoleSelection>();
	}

	public class RoleSelection
	{
		public string RoleName { get; set; }
		public bool IsSelected { get; set; }
	}
}