using Microsoft.EntityFrameworkCore;
using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Common.Helpers;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;

namespace FinalCuongFilm.Service.Services
{
	public class GenreService : IGenreService
	{
		private readonly CuongFilmDbContext _context;

		public GenreService(CuongFilmDbContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<GenreDto>> GetAllAsync()
		{
			return await _context.Genres
				.Select(g => new GenreDto
				{
					Id = g.Id,
					Name = g.Name,
					Slug = g.Slug,
					Description = g.Description
				})
				.ToListAsync();
		}

		public async Task<GenreDto?> GetByIdAsync(Guid id)
		{
			var genre = await _context.Genres.FindAsync(id);
			if (genre == null)
				return null;

			return new GenreDto
			{
				Id = genre.Id,
				Name = genre.Name,
				Slug = genre.Slug,
				Description = genre.Description
			};
		}

		public async Task<GenreDto> CreateAsync(GenreCreateDto dto)
		{
			var slug = SlugHelper.GenerateSlug(dto.Name);

			var genre = new Genre
			{
				Id = Guid.NewGuid(),
				Name = dto.Name,
				Slug = slug,
				Description = dto.Description
			};

			_context.Genres.Add(genre);
			await _context.SaveChangesAsync();

			return new GenreDto
			{
				Id = genre.Id,
				Name = genre.Name,
				Slug = genre.Slug,
				Description = genre.Description
			};
		}

		public async Task<bool> UpdateAsync(GenreUpdateDto dto)
		{
			var genre = await _context.Genres.FindAsync(dto.Id);
			if (genre == null)
				return false;

			var slug = SlugHelper.GenerateSlug(dto.Name);

			genre.Name = dto.Name;
			genre.Slug = slug;
			genre.Description = dto.Description;

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> DeleteAsync(Guid id)
		{
			var genre = await _context.Genres
				.Include(g => g.MovieGenres)
				.FirstOrDefaultAsync(g => g.Id == id);

			if (genre == null)
				return false;

			// Kiểm tra nghiệp vụ: không cho xóa nếu có phim thuộc thể loại này
			if (genre.MovieGenres.Any())
			{
				throw new InvalidOperationException("Không thể xóa thể loại đang được sử dụng. Vui lòng gỡ thể loại khỏi các phim trước.");
			}

			_context.Genres.Remove(genre);
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> ExistsAsync(Guid id)
		{
			return await _context.Genres.AnyAsync(g => g.Id == id);
		}
	}
}