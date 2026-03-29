using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.API.Controllers
{
	[Route("api/admin/[controller]")]
	[ApiController]
	public class MovieImportController : ControllerBase
	{
		private readonly IMovieImportService _movieImportService;

		public MovieImportController(IMovieImportService movieImportService)
		{
			_movieImportService = movieImportService;
		}

		[HttpPost("import")]
		public async Task<IActionResult> ImportMovie([FromQuery] string title)
		{
			if (string.IsNullOrWhiteSpace(title))
			{
				return BadRequest(new { success = false, message = "The movie title cannot be left blank." });
			}

			try
			{
				var result = await _movieImportService.ImportMovieAsync(title);

				if (result.Success)
				{
					return Ok(new { success = true, message = $"Great! The movie has been successfully imported.'{title}' in Database." });
				}

				return BadRequest(new { success = false, message = result.Message });
			}
			catch (Exception ex)
			{
				var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

				return StatusCode(500, new
				{
					success = false,
					message = "Database saving error",
					error = innerMessage
				});
			}
		}
	}
}