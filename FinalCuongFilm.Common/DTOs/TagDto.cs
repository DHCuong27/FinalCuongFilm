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
		[Required(ErrorMessage = "Please input tag name")]
		[MaxLength(100, ErrorMessage = "Name Tag max 100")]
		public string Name { get; set; } = string.Empty;
	}
}