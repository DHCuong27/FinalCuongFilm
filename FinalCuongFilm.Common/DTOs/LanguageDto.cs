namespace FinalCuongFilm.Common.DTOs
{
	public class LanguageDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Slug { get; set; } = string.Empty;
	}

	public class LanguageCreateDto
	{
		public string Name { get; set; } = string.Empty;
		public string Slug { get; set; } = string.Empty;	
	}

	public class LanguageUpdateDto : LanguageCreateDto
	{
		public Guid Id { get; set; }
	}
}