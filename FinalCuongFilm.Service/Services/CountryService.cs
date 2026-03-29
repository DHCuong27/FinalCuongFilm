using AutoMapper;
using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Common.Helpers;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.Service.Services
{
	public class CountryService : ICountryService
	{
		private readonly CuongFilmDbContext _context;
		private readonly IMapper _mapper;

		public CountryService(CuongFilmDbContext context, IMapper mapper)
		{
			_mapper = mapper;
			_context = context;
		}

		// Get all country
		public async Task<IEnumerable<CountryDto>> GetAllAsync()
		{
			return await _context.Countries
				.Select(c => new CountryDto
				{
					Id = c.Id,
					Name = c.Name,
					Slug = c.Slug,
					IsoCode = c.IsoCode
				})
				.ToListAsync();
		}

		// Get country by id
		public async Task<CountryDto?> GetByIdAsync(Guid id)
		{
			var country = await _context.Countries.FindAsync(id);
			if (country == null)
				return null;

			return new CountryDto
			{
				Id = country.Id,
				Name = country.Name,
				Slug = country.Slug,
				IsoCode = country.IsoCode
			};
		}

		// Create new country
		public async Task<CountryDto> CreateAsync(CountryCreateDto dto)
		{
			var slug = SlugHelper.GenerateSlug(dto.Name);

			var country = new Country
			{
				Id = Guid.NewGuid(),
				Name = dto.Name,
				Slug = slug,
				IsoCode = dto.IsoCode
			};

			_context.Countries.Add(country);
			await _context.SaveChangesAsync();

			return new CountryDto
			{
				Id = country.Id,
				Name = country.Name,
				Slug = country.Slug,
				IsoCode = country.IsoCode
			};
		}

		// Update country
		public async Task<bool> UpdateAsync(CountryUpdateDto dto)
		{
			var country = await _context.Countries.FindAsync(dto.Id);
			if (country == null)
				return false;

			var slug = SlugHelper.GenerateSlug(dto.Name);

			country.Name = dto.Name;
			country.Slug = slug;
			country.IsoCode = dto.IsoCode;

			await _context.SaveChangesAsync();
			return true;
		}

		// Delete country
		public async Task<bool> DeleteAsync(Guid id)
		{
			var country = await _context.Countries.FindAsync(id);
			if (country == null)
				return false;

			// Kiểm tra nghiệp vụ: không cho xóa nếu có phim từ quốc gia này
			var hasMovies = await _context.Movies.AnyAsync(m => m.CountryId == id);
			if (hasMovies)
			{
				throw new InvalidOperationException("Không thể xóa quốc gia đang có phim. Vui lòng cập nhật quốc gia cho các phim trước.");
			}

			_context.Countries.Remove(country);
			await _context.SaveChangesAsync();
			return true;
		}

		// Get paged country
		public async Task<PagedResult<CountryDto>> GetPagedAsync(int pageIndex = 1, int pageSize = 10)
		{

			if (pageIndex < 1) pageIndex = 1;
			if (pageSize < 1) pageSize = 10;

			var query = _context.Countries.AsQueryable();

			int totalCount = await query.CountAsync();

			var items = await query.OrderByDescending(m => m.Id) // Sắp xếp theo số lượng phim
						   .Skip((pageIndex - 1) * pageSize)
						   .Take(pageSize)
						   .ToListAsync();

			var dtos = _mapper.Map<List<CountryDto>>(items);

			return new PagedResult<CountryDto>
			{
				Items = dtos,
				TotalCount = totalCount,
				PageIndex = pageIndex,
				PageSize = pageSize
			};
		}
		public async Task<bool> ExistsAsync(Guid id)
		{
			return await _context.Countries.AnyAsync(c => c.Id == id);
		}
	}
}