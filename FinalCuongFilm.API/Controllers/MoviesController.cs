using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.API.Models.Response;

namespace FinalCuongFilm.API.Controllers
{
	/// <summary>
	/// API endpoints for managing movies
	/// </summary>
	[Route("api/[controller]")]
	[ApiController]
	[Produces("application/json")]
	public class MoviesController : ControllerBase
	{
		private readonly IMovieService _movieService;
		private readonly ILogger<MoviesController> _logger;

		public MoviesController(
			IMovieService movieService,
			ILogger<MoviesController> logger)
		{
			_movieService = movieService;
			_logger = logger;
		}

		/// <summary>
		/// Get all movies with pagination and filters
		/// </summary>
		/// <param name="pageNumber">Page number (default: 1)</param>
		/// <param name="pageSize">Page size (default: 10, max: 100)</param>
		/// <param name="search">Search keyword</param>
		/// <param name="type">Movie type filter</param>
		/// <param name="isActive">Active status filter</param>
		/// <returns>Paginated list of movies</returns>
		[HttpGet]
		[ProducesResponseType(typeof(ApiResponse<PaginatedResult<MovieDto>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<ApiResponse<PaginatedResult<MovieDto>>>> GetAll(
			[FromQuery] int pageNumber = 1,
			[FromQuery] int pageSize = 10,
			[FromQuery] string? search = null,
			[FromQuery] int? type = null,
			[FromQuery] bool? isActive = null)
		{
			_logger.LogInformation("Getting movies - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

			if (pageSize > 100)
			{
				return BadRequest(ApiResponse<PaginatedResult<MovieDto>>.FailureResult(
					"Page size cannot exceed 100"
				));
			}

			var allMovies = await _movieService.GetAllAsync();
			var query = allMovies.AsEnumerable();

			// Apply filters
			if (!string.IsNullOrWhiteSpace(search))
			{
				query = query.Where(m =>
					m.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
					(m.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
				);
			}

			if (type.HasValue)
			{
				query = query.Where(m => (int)m.Type == type.Value);
			}

			if (isActive.HasValue)
			{
				query = query.Where(m => m.IsActive == isActive.Value);
			}

			// Paginate
			var result = PaginatedResult<MovieDto>.Create(query, pageNumber, pageSize);

			return Ok(ApiResponse<PaginatedResult<MovieDto>>.SuccessResult(result));
		}

		/// <summary>
		/// Get movie by ID
		/// </summary>
		/// <param name="id">Movie ID</param>
		/// <returns>Movie details</returns>
		[HttpGet("{id}")]
		[ProducesResponseType(typeof(ApiResponse<MovieDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ApiResponse<MovieDto>>> GetById(Guid id)
		{
			_logger.LogInformation("Getting movie with ID: {MovieId}", id);

			var movie = await _movieService.GetByIdAsync(id);

			if (movie == null)
			{
				return NotFound(ApiResponse<MovieDto>.FailureResult(
					"Movie not found",
					$"Movie with ID {id} does not exist",
					StatusCodes.Status404NotFound
				));
			}

			return Ok(ApiResponse<MovieDto>.SuccessResult(movie));
		}

		/// <summary>
		/// Create a new movie
		/// </summary>
		/// <param name="dto">Movie creation data</param>
		/// <returns>Created movie</returns>
		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(ApiResponse<MovieDto>), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<ApiResponse<MovieDto>>> Create([FromBody] MovieCreateDto dto)
		{
			_logger.LogInformation("Creating new movie: {Title}", dto.Title);

			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values
					.SelectMany(v => v.Errors)
					.Select(e => e.ErrorMessage)
					.ToList();

				return BadRequest(ApiResponse<MovieDto>.FailureResult(
					"Validation failed",
					errors
				));
			}

			var movie = await _movieService.CreateAsync(dto);

			return CreatedAtAction(
				nameof(GetById),
				new { id = movie.Id },
				ApiResponse<MovieDto>.SuccessResult(movie, "Movie created successfully", StatusCodes.Status201Created)
			);
		}

		/// <summary>
		/// Update an existing movie
		/// </summary>
		/// <param name="id">Movie ID</param>
		/// <param name="dto">Movie update data</param>
		/// <returns>Updated movie</returns>
		[HttpPut("{id}")]
		[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(ApiResponse<MovieDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ApiResponse<MovieDto>>> Update(Guid id, [FromBody] MovieUpdateDto dto)
		{
			_logger.LogInformation("Updating movie with ID: {MovieId}", id);

			if (id != dto.Id)
			{
				return BadRequest(ApiResponse<MovieDto>.FailureResult("ID mismatch"));
			}

			var result = await _movieService.UpdateAsync(id, dto);

			if (result == null)
			{
				return NotFound(ApiResponse<MovieDto>.FailureResult(
					"Movie not found",
					statusCode: StatusCodes.Status404NotFound
				));
			}

			return Ok(ApiResponse<MovieDto>.SuccessResult(result, "Movie updated successfully"));
		}

		/// <summary>
		/// Delete a movie
		/// </summary>
		/// <param name="id">Movie ID</param>
		/// <returns>Success status</returns>
		[HttpDelete("{id}")]
		[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
		{
			_logger.LogInformation("Deleting movie with ID: {MovieId}", id);

			var success = await _movieService.DeleteAsync(id);

			if (!success)
			{
				return NotFound(ApiResponse<bool>.FailureResult(
					"Movie not found",
					statusCode: StatusCodes.Status404NotFound
				));
			}

			return Ok(ApiResponse<bool>.SuccessResult(true, "Movie deleted successfully"));
		}
	}
}