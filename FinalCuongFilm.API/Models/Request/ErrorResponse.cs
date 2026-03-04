namespace FinalCuongFilm.API.Models.Request
{
	public class ErrorResponse
	{
		public string Message { get; set; } = string.Empty;
		public int StatusCode { get; set; }
		public List<string> Errors { get; set; } = new();
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	}
}