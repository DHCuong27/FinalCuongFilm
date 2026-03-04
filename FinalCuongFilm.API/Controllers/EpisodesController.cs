using FinalCuongFilm.API.Models.Response;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.API.Controllers
{
	/// <summary>
	/// API endpoints for managing episodes
	/// </summary>
	[Route("api/[controller]")]
	[ApiController]
	[Produces("application/json")]
	public class EpisodesController : ControllerBase
	{
		private readonly IEpisodeService _episodeService;
		private readonly ILogger<EpisodesController> _logger;

		public EpisodesController(IEpisodeService episodeService, ILogger<EpisodesController> logger)
		{
			_episodeService = episodeService;
			_logger = logger;
		}

		/// <summary>
		/// Lấy tất cả tập phim (có phân trang)
		/// </summary>
		[HttpGet]
		[ProducesResponseType(typeof(ApiResponse<PaginatedResult<EpisodeDto>>), StatusCodes.Status200OK)]
		public async Task<ActionResult<ApiResponse<PaginatedResult<EpisodeDto>>>> GetAll(
			[FromQuery] int pageNumber = 1,
			[FromQuery] int pageSize = 20)
		{
			if (pageSize > 100) pageSize = 100;
			var all = await _episodeService.GetAllAsync();
			var result = PaginatedResult<EpisodeDto>.Create(all, pageNumber, pageSize);
			return Ok(ApiResponse<PaginatedResult<EpisodeDto>>.SuccessResult(result));
		}

		/// <summary>
		/// Lấy các tập phim theo Movie ID
		/// </summary>
		[HttpGet("movie/{movieId:guid}")]
		[ProducesResponseType(typeof(ApiResponse<IEnumerable<EpisodeDto>>), StatusCodes.Status200OK)]
		public async Task<ActionResult<ApiResponse<IEnumerable<EpisodeDto>>>> GetByMovieId(Guid movieId)
		{
			var episodes = await _episodeService.GetByMovieIdAsync(movieId);
			return Ok(ApiResponse<IEnumerable<EpisodeDto>>.SuccessResult(episodes));
		}

		/// <summary>
		/// Lấy tập phim theo ID
		/// </summary>
		[HttpGet("{id:guid}")]
		[ProducesResponseType(typeof(ApiResponse<EpisodeDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ApiResponse<EpisodeDto>>> GetById(Guid id)
		{
			var episode = await _episodeService.GetByIdAsync(id);
			if (episode == null)
				return NotFound(ApiResponse<EpisodeDto>.FailureResult($"Không tìm thấy tập phim ID={id}", statusCode: 404));

			return Ok(ApiResponse<EpisodeDto>.SuccessResult(episode));
		}

		/// <summary>
		/// Tạo tập phim mới [Admin]
		/// </summary>
		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(ApiResponse<EpisodeDto>), StatusCodes.Status201Created)]
		public async Task<ActionResult<ApiResponse<EpisodeDto>>> Create([FromBody] EpisodeCreateDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ApiResponse<EpisodeDto>.FailureResult("Dữ liệu không hợp lệ"));

			try
			{
				var created = await _episodeService.CreateAsync(dto);
				_logger.LogInformation("Episode created: {EpisodeId} for movie {MovieId}", created.Id, dto.MovieId);
				return CreatedAtAction(nameof(GetById), new { id = created.Id },
					ApiResponse<EpisodeDto>.SuccessResult(created, "Tạo tập phim thành công", 201));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating episode");
				return StatusCode(500, ApiResponse<EpisodeDto>.FailureResult("Lỗi khi tạo tập phim", ex.Message, 500));
			}
		}

		/// <summary>
		/// Cập nhật tập phim [Admin]
		/// </summary>
		[HttpPut("{id:guid}")]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] EpisodeUpdateDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ApiResponse<object>.FailureResult("Dữ liệu không hợp lệ"));
			if (id != dto.Id)
				return BadRequest(ApiResponse<object>.FailureResult("ID không khớp"));

			var exists = await _episodeService.ExistsAsync(id);
			if (!exists)
				return NotFound(ApiResponse<object>.FailureResult($"Không tìm thấy tập phim ID={id}", statusCode: 404));

			var result = await _episodeService.UpdateAsync(dto);
			if (!result)
				return StatusCode(500, ApiResponse<object>.FailureResult("Cập nhật thất bại", statusCode: 500));

			return Ok(ApiResponse<object>.SuccessResult(new { id }, "Cập nhật tập phim thành công"));
		}

		/// <summary>
		/// Xóa tập phim [Admin]
		/// </summary>
		[HttpDelete("{id:guid}")]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
		{
			var exists = await _episodeService.ExistsAsync(id);
			if (!exists)
				return NotFound(ApiResponse<object>.FailureResult($"Không tìm thấy tập phim ID={id}", statusCode: 404));

			var result = await _episodeService.DeleteAsync(id);
			if (!result)
				return StatusCode(500, ApiResponse<object>.FailureResult("Xóa thất bại", statusCode: 500));

			_logger.LogInformation("Episode deleted: {Id}", id);
			return Ok(ApiResponse<object>.SuccessResult(new { id }, "Xóa tập phim thành công"));
		}
	}
}