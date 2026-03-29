using FinalCuongFilm.API.Models.Response;
using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.API.Controllers
{
	/// <summary>
	/// API endpoints for managing countries
	/// </summary>
	[Route("api/[controller]")]
	[ApiController]
	[Produces("application/json")]
	public class CountriesController : ControllerBase
	{
		private readonly ICountryService _countryService;
		private readonly ILogger<CountriesController> _logger;

		public CountriesController(ICountryService countryService, ILogger<CountriesController> logger)
		{
			_countryService = countryService;
			_logger = logger;
		}

		/// <summary>
		/// Lấy tất cả quốc gia
		/// </summary>
		[HttpGet]
		[ProducesResponseType(typeof(ApiResponse<IEnumerable<CountryDto>>), StatusCodes.Status200OK)]
		public async Task<ActionResult<ApiResponse<IEnumerable<CountryDto>>>> GetAll()
		{
			var countries = await _countryService.GetAllAsync();
			return Ok(ApiResponse<IEnumerable<CountryDto>>.SuccessResult(countries));
		}

		/// <summary>
		/// Lấy quốc gia theo ID
		/// </summary>
		[HttpGet("{id:guid}")]
		[ProducesResponseType(typeof(ApiResponse<CountryDto>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ApiResponse<CountryDto>>> GetById(Guid id)
		{
			var country = await _countryService.GetByIdAsync(id);
			if (country == null)
				return NotFound(ApiResponse<CountryDto>.FailureResult($"No country found ID={id}", statusCode: 404));

			return Ok(ApiResponse<CountryDto>.SuccessResult(country));
		}

		/// <summary>
		/// Tạo quốc gia mới [Admin]
		/// </summary>
		[HttpPost]
		[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(ApiResponse<CountryDto>), StatusCodes.Status201Created)]
		public async Task<ActionResult<ApiResponse<CountryDto>>> Create([FromBody] CountryCreateDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ApiResponse<CountryDto>.FailureResult("Invalid data"));

			try
			{
				var created = await _countryService.CreateAsync(dto);
				return CreatedAtAction(nameof(GetById), new { id = created.Id },
					ApiResponse<CountryDto>.SuccessResult(created, "Creating a successful nation", 201));
			}
			catch (Exception ex)
			{
				return StatusCode(500, ApiResponse<CountryDto>.FailureResult("Error creating a country", ex.Message, 500));
			}
		}

		/// <summary>
		/// Cập nhật quốc gia [Admin]
		/// </summary>
		[HttpPut("{id:guid}")]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] CountryUpdateDto dto)
		{
			if (!ModelState.IsValid)
				return BadRequest(ApiResponse<object>.FailureResult("Invalid data"));
			if (id != dto.Id)
				return BadRequest(ApiResponse<object>.FailureResult("ID not match"));

			var exists = await _countryService.ExistsAsync(id);
			if (!exists)
				return NotFound(ApiResponse<object>.FailureResult($"No country found ID={id}", statusCode: 404));

			var result = await _countryService.UpdateAsync(dto);
			if (!result)
				return StatusCode(500, ApiResponse<object>.FailureResult("Update failed", statusCode: 500));

			return Ok(ApiResponse<object>.SuccessResult(new { id }, "Country update successful"));
		}

		/// <summary>
		/// Xóa quốc gia [Admin]
		/// </summary>
		[HttpDelete("{id:guid}")]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
		{
			var exists = await _countryService.ExistsAsync(id);
			if (!exists)
				return NotFound(ApiResponse<object>.FailureResult($"No country found ID={id}", statusCode: 404));

			var result = await _countryService.DeleteAsync(id);
			if (!result)
				return StatusCode(500, ApiResponse<object>.FailureResult("Delete failed", statusCode: 500));

			return Ok(ApiResponse<object>.SuccessResult(new { id }, "Country deletion successful"));
		}
	}
}