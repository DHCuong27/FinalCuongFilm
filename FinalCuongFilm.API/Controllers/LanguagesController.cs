using FinalCuongFilm.API.Models.Response;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.API.Controllers
{
	/// <summary>
	/// API endpoints for managing languages
	/// </summary>
	[Route("api/[controller]")]
	[ApiController]
	[Produces("application/json")]
	public class LanguagesController : ControllerBase
	{
		private readonly ILanguageService _languageService;
		private readonly ILogger<LanguagesController> _logger;

		public LanguagesController(ILanguageService languageService, ILogger<LanguagesController> logger)
		{
			_languageService = languageService;
			_logger = logger;
		}

		/// <summary>
		/// Lấy tất cả ngôn ngữ
		/// </summary>
		[HttpGet]
		[ProducesResponseType(typeof(ApiResponse<IEnumerable<LanguageDto>>), StatusCodes.Status200OK)]
		public async Task<ActionResult<ApiResponse<IEnumerable<LanguageDto>>>> GetAll()
		{
			var langs = await _languageService.GetAllAsync();
			return Ok(ApiResponse<IEnumerable<LanguageDto>>.SuccessResult(langs));
		}

		/// <summary>
		/// Lấy ngôn ngữ theo ID
		/// </summary>
		[HttpGet("{id:guid}")]
		[ProducesResponseType(typeof(ApiResponse<LanguageDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ApiResponse<LanguageDto>>> GetById(Guid id)
		{
			var lang = await _languageService.GetByIdAsync(id);
			if (lang == null)
				return NotFound(ApiResponse<LanguageDto>.FailureResult($"Không tìm thấy ngôn ngữ ID={id}", statusCode: 404));

			return Ok(ApiResponse<LanguageDto>.SuccessResult(lang));
		}

		/// <summary>
		/// Tạo ngôn ngữ mới [Admin]
		/// </summary>
		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(ApiResponse<LanguageDto>), StatusCodes.Status201Created)]
		public async Task<ActionResult<ApiResponse<LanguageDto>>> Create([FromBody] LanguageCreateDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ApiResponse<LanguageDto>.FailureResult("Dữ liệu không hợp lệ"));

			try
			{
				var created = await _languageService.CreateAsync(dto);
				return CreatedAtAction(nameof(GetById), new { id = created.Id },
					ApiResponse<LanguageDto>.SuccessResult(created, "Tạo ngôn ngữ thành công", 201));
			}
			catch (Exception ex)
			{
				return StatusCode(500, ApiResponse<LanguageDto>.FailureResult("Lỗi khi tạo ngôn ngữ", ex.Message, 500));
			}
		}

		/// <summary>
		/// Cập nhật ngôn ngữ [Admin]
		/// </summary>
		[HttpPut("{id:guid}")]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] LanguageUpdateDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ApiResponse<object>.FailureResult("Dữ liệu không hợp lệ"));
			if (id != dto.Id)
				return BadRequest(ApiResponse<object>.FailureResult("ID không khớp"));

			var exists = await _languageService.ExistsAsync(id);
			if (!exists)
				return NotFound(ApiResponse<object>.FailureResult($"Không tìm thấy ngôn ngữ ID={id}", statusCode: 404));

			var result = await _languageService.UpdateAsync(dto);
			if (!result)
				return StatusCode(500, ApiResponse<object>.FailureResult("Cập nhật thất bại", statusCode: 500));

			return Ok(ApiResponse<object>.SuccessResult(new { id }, "Cập nhật ngôn ngữ thành công"));
		}

		/// <summary>
		/// Xóa ngôn ngữ [Admin]
		/// </summary>
		[HttpDelete("{id:guid}")]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
		{
			var exists = await _languageService.ExistsAsync(id);
			if (!exists)
				return NotFound(ApiResponse<object>.FailureResult($"Không tìm thấy ngôn ngữ ID={id}", statusCode: 404));

			var result = await _languageService.DeleteAsync(id);
			if (!result)
				return StatusCode(500, ApiResponse<object>.FailureResult("Xóa thất bại", statusCode: 500));

			return Ok(ApiResponse<object>.SuccessResult(new { id }, "Xóa ngôn ngữ thành công"));
		}
	}
}