using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Common.Helpers;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.Service.Services
{
	public class LanguageService : ILanguageService
	{
		private readonly CuongFilmDbContext _context;

		public LanguageService(CuongFilmDbContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<LanguageDto>> GetAllAsync()
		{
			return await _context.Languages
				.Select(l => new LanguageDto
				{
					Id = l.Id,
					Name = l.Name,
					Slug = l.Slug
				})
				.ToListAsync();
		}

		public async Task<LanguageDto?> GetByIdAsync(Guid id)
		{
			var language = await _context.Languages.FindAsync(id);
			if (language == null)
				return null;

			return new LanguageDto
			{
				Id = language.Id,
				Name = language.Name,
				Slug = language.Slug
			};
		}

		public async Task<LanguageDto> CreateAsync(LanguageCreateDto dto)
		{
			var slug = SlugHelper.GenerateSlug(dto.Name);

			var language = new Language
			{
				Id = Guid.NewGuid(),
				Name = dto.Name,
				Slug = slug
			};

			_context.Languages.Add(language);
			await _context.SaveChangesAsync();

			return new LanguageDto
			{
				Id = language.Id,
				Name = language.Name,
				Slug = language.Slug
			};
		}

		public async Task<bool> UpdateAsync(LanguageUpdateDto dto)
		{
			var language = await _context.Languages.FindAsync(dto.Id);
			if (language == null)
				return false;

			var slug = SlugHelper.GenerateSlug(dto.Name);

			language.Name = dto.Name;
			language.Slug = slug;

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> DeleteAsync(Guid id)
		{
			var language = await _context.Languages.FindAsync(id);
			if (language == null)
				return false;

			// Kiểm tra nghiệp vụ: không cho xóa nếu có phim sử dụng ngôn ngữ này
			var hasMovies = await _context.Movies.AnyAsync(m => m.LanguageId == id);
			if (hasMovies)
			{
				throw new InvalidOperationException("Không thể xóa ngôn ngữ đang được sử dụng. Vui lòng cập nhật ngôn ngữ cho các phim trước.");
			}

			_context.Languages.Remove(language);
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> ExistsAsync(Guid id)
		{
			return await _context.Languages.AnyAsync(l => l.Id == id);
		}
	}
}