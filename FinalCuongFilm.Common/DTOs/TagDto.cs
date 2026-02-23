using System.ComponentModel.DataAnnotations;

namespace FinalCuongFilm.Common.DTOs
{
	public class TagDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}
	

	public class TagCreateDto
	{
		[Required(ErrorMessage = "Vui lòng nhập tên tag")]
		[MaxLength(100, ErrorMessage = "Tên tag tối đa 100 ký tự")]
		public string Name { get; set; } = string.Empty;
	}
}