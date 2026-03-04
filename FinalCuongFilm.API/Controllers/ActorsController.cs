using FinalCuongFilm.API.Models.Response;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.API.Controllers
{
	/// <summary>
	/// API endpoints for managing actors
	/// </summary>
	[Route("api/[controller]")]
	[ApiController]
	[Produces("application/json")]
	public class ActorsController : ControllerBase
	{
		private readonly IActorService _actorService;
		private readonly ILogger<ActorsController> _logger;

		public ActorsController(IActorService actorService, ILogger<ActorsController> logger)
		{
			_actorService = actorService;
			_logger = logger;
		}

		/// <summary>
		/// Lấy danh sách tất cả diễn viên (có phân trang + tìm kiếm)
		/// </summary>
		[HttpGet]
		[ProducesResponseType(typeof(ApiResponse<PaginatedResult<ActorDto>>), StatusCodes.Status200OK)]
		public async Task<ActionResult<ApiResponse<PaginatedResult<ActorDto>>>> GetAll(
			[FromQuery] int pageNumber = 1,
			[FromQuery] int pageSize = 20,
			[FromQuery] string? search = null)
		{
			if (pageSize > 100) pageSize = 100;

			var all = await _actorService.GetAllAsync();
			var query = all.AsEnumerable();

			if (!string.IsNullOrWhiteSpace(search))
				query = query.Where(a => a.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

			var result = PaginatedResult<ActorDto>.Create(query, pageNumber, pageSize);
			return Ok(ApiResponse<PaginatedResult<ActorDto>>.SuccessResult(result));
		}

		/// <summary>
		/// Lấy diễn viên theo ID
		/// </summary>
		[HttpGet("{id:guid}")]
		[ProducesResponseType(typeof(ApiResponse<ActorDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ApiResponse<ActorDto>>> GetById(Guid id)
		{
			var actor = await _actorService.GetByIdAsync(id);
			if (actor == null)
				return NotFound(ApiResponse<ActorDto>.FailureResult($"Không tìm thấy diễn viên ID={id}", statusCode: 404));

			return Ok(ApiResponse<ActorDto>.SuccessResult(actor));
		}

		/// <summary>
		/// Tạo diễn viên mới [Admin]
		/// </summary>
		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(ApiResponse<ActorDto>), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<ApiResponse<ActorDto>>> Create([FromBody] ActorCreateDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ApiResponse<ActorDto>.FailureResult("Dữ liệu không hợp lệ"));

			try
			{
				var created = await _actorService.CreateAsync(dto);
				_logger.LogInformation("Actor created: {Name}", created.Name);
				return CreatedAtAction(nameof(GetById), new { id = created.Id },
					ApiResponse<ActorDto>.SuccessResult(created, "Tạo diễn viên thành công", 201));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating actor");
				return StatusCode(500, ApiResponse<ActorDto>.FailureResult("Lỗi khi tạo diễn viên", ex.Message, 500));
			}
		}

		/// <summary>
		/// Cập nhật diễn viên [Admin]
		/// </summary>
		[HttpPut("{id:guid}")]
		[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] ActorUpdateDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ApiResponse<object>.FailureResult("Dữ liệu không hợp lệ"));

			if (id != dto.Id)
				return BadRequest(ApiResponse<object>.FailureResult("ID không khớp"));

			var exists = await _actorService.ExistsAsync(id);
			if (!exists)
				return NotFound(ApiResponse<object>.FailureResult($"Không tìm thấy diễn viên ID={id}", statusCode: 404));

			var result = await _actorService.UpdateAsync(dto);
			if (!result)
				return StatusCode(500, ApiResponse<object>.FailureResult("Cập nhật thất bại", statusCode: 500));

			return Ok(ApiResponse<object>.SuccessResult(new { id }, "Cập nhật diễn viên thành công"));
		}

		/// <summary>
		/// Xóa diễn viên [Admin]
		/// </summary>
		[HttpDelete("{id:guid}")]
		[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
		{
			var exists = await _actorService.ExistsAsync(id);
			if (!exists)
				return NotFound(ApiResponse<object>.FailureResult($"Không tìm thấy diễn viên ID={id}", statusCode: 404));

			var result = await _actorService.DeleteAsync(id);
			if (!result)
				return StatusCode(500, ApiResponse<object>.FailureResult("Xóa thất bại", statusCode: 500));

			_logger.LogInformation("Actor deleted: {Id}", id);
			return Ok(ApiResponse<object>.SuccessResult(new { id }, "Xóa diễn viên thành công"));
		}
	}
}