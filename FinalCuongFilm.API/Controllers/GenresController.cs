using FinalCuongFilm.API.Models.Response;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.API.Controllers
{
	/// <summary>
	/// API endpoints for managing genres
	/// </summary>
	[Route("api/[controller]")]
	[ApiController]
	[Produces("application/json")]
	public class GenresController : ControllerBase
	{
		private readonly IGenreService _genreService;
		private readonly ILogger<GenresController> _logger;

		public GenresController(IGenreService genreService, ILogger<GenresController> logger)
		{
			_genreService = genreService;
			_logger = logger;
		}

		/// <summary>
		/// Lấy tất cả thể loại
		/// </summary>
		[HttpGet]
		[ProducesResponseType(typeof(ApiResponse<IEnumerable<GenreDto>>), StatusCodes.Status200OK)]
		public async Task<ActionResult<ApiResponse<IEnumerable<GenreDto>>>> GetAll()
		{
			var genres = await _genreService.GetAllAsync();
			return Ok(ApiResponse<IEnumerable<GenreDto>>.SuccessResult(genres));
		}

		/// <summary>
		/// Lấy thể loại theo ID
		/// </summary>
		[HttpGet("{id:guid}")]
		[ProducesResponseType(typeof(ApiResponse<GenreDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ApiResponse<GenreDto>>> GetById(Guid id)
		{
			var genre = await _genreService.GetByIdAsync(id);
			if (genre == null)
				return NotFound(ApiResponse<GenreDto>.FailureResult($"Không tìm thấy thể loại ID={id}", statusCode: 404));

			return Ok(ApiResponse<GenreDto>.SuccessResult(genre));
		}

		/// <summary>
		/// Tạo thể loại mới [Admin]
		/// </summary>
		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(ApiResponse<GenreDto>), StatusCodes.Status201Created)]
		public async Task<ActionResult<ApiResponse<GenreDto>>> Create([FromBody] GenreCreateDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ApiResponse<GenreDto>.FailureResult("Dữ liệu không hợp lệ"));

			try
			{
				var created = await _genreService.CreateAsync(dto);
				return CreatedAtAction(nameof(GetById), new { id = created.Id },
					ApiResponse<GenreDto>.SuccessResult(created, "Tạo thể loại thành công", 201));
			}
			catch (Exception ex)
			{
				return StatusCode(500, ApiResponse<GenreDto>.FailureResult("Lỗi khi tạo thể loại", ex.Message, 500));
			}
		}

		/// <summary>
		/// Cập nhật thể loại [Admin]
		/// </summary>
		[HttpPut("{id:guid}")]
		[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
		public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] GenreUpdateDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ApiResponse<object>.FailureResult("Dữ liệu không hợp lệ"));
			if (id != dto.Id)
				return BadRequest(ApiResponse<object>.FailureResult("ID không khớp"));

			var exists = await _genreService.ExistsAsync(id);
			if (!exists)
				return NotFound(ApiResponse<object>.FailureResult($"Không tìm thấy thể loại ID={id}", statusCode: 404));

			var result = await _genreService.UpdateAsync(dto);
			if (!result)
				return StatusCode(500, ApiResponse<object>.FailureResult("Cập nhật thất bại", statusCode: 500));

			return Ok(ApiResponse<object>.SuccessResult(new { id }, "Cập nhật thể loại thành công"));
		}

		/// <summary>
		/// Xóa thể loại [Admin]
		/// </summary>
		[HttpDelete("{id:guid}")]
		[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
		public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
		{
			var exists = await _genreService.ExistsAsync(id);
			if (!exists)
				return NotFound(ApiResponse<object>.FailureResult($"Không tìm thấy thể loại ID={id}", statusCode: 404));

			var result = await _genreService.DeleteAsync(id);
			if (!result)
				return StatusCode(500, ApiResponse<object>.FailureResult("Xóa thất bại", statusCode: 500));

			return Ok(ApiResponse<object>.SuccessResult(new { id }, "Xóa thể loại thành công"));
		}
	}
}