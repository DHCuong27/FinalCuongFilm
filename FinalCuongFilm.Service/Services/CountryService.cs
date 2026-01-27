using Microsoft.EntityFrameworkCore;
using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Common.Helpers;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;

namespace FinalCuongFilm.Service.Services
{
	public class CountryService : ICountryService
	{
		private readonly CuongFilmDbContext _context;

		public CountryService(CuongFilmDbContext context)
		{
			_context = context;
		}

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

		public async Task<bool> ExistsAsync(Guid id)
		{
			return await _context.Countries.AnyAsync(c => c.Id == id);
		}
	}
}