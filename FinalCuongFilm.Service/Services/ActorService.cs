using AutoMapper;
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
		private readonly IMapper _mapper;

		public ActorService(CuongFilmDbContext context, IMapper mapper)
		{
			_mapper = mapper;
			_context = context;
		}

		// Get all actor
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

		// Get actor by id
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

		// Create new actor
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

		// Update actor
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

		// Delete actor
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

		// Pagination with search
		public async Task<PagedResult<ActorDto>> GetPagedAsync(string? searchString = null, int pageIndex = 1, int pageSize = 10)
		{
			if (pageIndex < 1) pageIndex = 1;
			if (pageSize < 1) pageSize = 10;

			// 1. Lấy toàn bộ danh sách Actor ra chờ sẵn
			var query = _context.Actors.AsQueryable();

			// 2. LOGIC TÌM KIẾM Ở ĐÂY: Nếu có chữ tìm kiếm thì lọc lại query
			if (!string.IsNullOrEmpty(searchString))
			{
				// Chuyển về chữ thường để tìm kiếm không phân biệt Hoa/Thường
				var searchLower = searchString.ToLower();

				query = query.Where(a => a.Name.ToLower().Contains(searchLower)
									  || a.Slug.ToLower().Contains(searchLower));
			}

			// 3. Đếm số lượng sau khi đã lọc (để chia số trang cho đúng)
			int totalCount = await query.CountAsync();

			// 4. Phân trang như bình thường
			var items = await query.OrderByDescending(m => m.Id)
								   .Skip((pageIndex - 1) * pageSize)
								   .Take(pageSize)
								   .ToListAsync();

			var dtos = _mapper.Map<List<ActorDto>>(items);

			return new PagedResult<ActorDto>
			{
				Items = dtos,
				TotalCount = totalCount,
				PageIndex = pageIndex,
				PageSize = pageSize
			};
		}

	
		public async Task<bool> ExistsAsync(Guid id)
		{
			return await _context.Actors.AnyAsync(a => a.Id == id);
		}

	}
}