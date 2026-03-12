using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Common.Helpers;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.Service.Services
{
	public class ActorService : IActorService
	{
		private readonly CuongFilmDbContext _context;

		public ActorService(CuongFilmDbContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<ActorDto>> GetAllAsync()
		{
			return await _context.Actors
				.Select(a => new ActorDto
				{
					Id = a.Id,
					Name = a.Name,
					Slug = a.Slug,
					AvartUrl = a.AvartUrl,
					DateOfBirth = a.DateOfBirth,
					Gender = a.Gender
				})
				.ToListAsync();
		}

		public async Task<ActorDto?> GetByIdAsync(Guid id)
		{
			var actor = await _context.Actors.FindAsync(id);
			if (actor == null)
				return null;

			return new ActorDto
			{
				Id = actor.Id,
				Name = actor.Name,
				Slug = actor.Slug,
				AvartUrl = actor.AvartUrl,
				DateOfBirth = actor.DateOfBirth,
				Gender = actor.Gender
			};
		}

		public async Task<ActorDto> CreateAsync(ActorCreateDto dto)
		{
			var slug = SlugHelper.GenerateSlug(dto.Name);

			var actor = new Actor
			{
				Id = Guid.NewGuid(),
				Name = dto.Name,
				Slug = slug,
				AvartUrl = dto.AvartUrl,
				DateOfBirth = dto.DateOfBirth,
				Gender = dto.Gender
			};

			_context.Actors.Add(actor);
			await _context.SaveChangesAsync();

			return new ActorDto
			{
				Id = actor.Id,
				Name = actor.Name,
				Slug = actor.Slug,
				AvartUrl = actor.AvartUrl,
				DateOfBirth = actor.DateOfBirth,
				Gender = actor.Gender
			};
		}

		public async Task<bool> UpdateAsync(ActorUpdateDto dto)
		{
			var actor = await _context.Actors.FindAsync(dto.Id);
			if (actor == null)
				return false;

			var slug = SlugHelper.GenerateSlug(dto.Name);

			actor.Name = dto.Name;
			actor.Slug = slug;
			actor.AvartUrl = dto.AvartUrl;
			actor.DateOfBirth = dto.DateOfBirth;
			actor.Gender = dto.Gender;

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> DeleteAsync(Guid id)
		{
			var actor = await _context.Actors
				.Include(a => a.MovieActors)
				.FirstOrDefaultAsync(a => a.Id == id);

			if (actor == null)
				return false;

			// Kiểm tra nghiệp vụ: không cho xóa nếu đang tham gia phim
			if (actor.MovieActors.Any())
			{
				throw new InvalidOperationException("Không thể xóa diễn viên đang tham gia phim. Vui lòng gỡ diễn viên khỏi các phim trước.");
			}

			_context.Actors.Remove(actor);
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> ExistsAsync(Guid id)
		{
			return await _context.Actors.AnyAsync(a => a.Id == id);
		}
	}
}