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
}