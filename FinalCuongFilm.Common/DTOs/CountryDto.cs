namespace FinalCuongFilm.Common.DTOs
{
	public class CountryDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Slug { get; set; } = string.Empty;
		public string? IsoCode { get; set; }
	}

	public class CountryCreateDto
	{
		public string Name { get; set; } = string.Empty;
		public string? IsoCode { get; set; }
	}

	public class CountryUpdateDto : CountryCreateDto
	{
		public Guid Id { get; set; }
	}
}