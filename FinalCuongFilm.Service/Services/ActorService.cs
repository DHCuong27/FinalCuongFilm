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

		public async Task<ActorDto?> GetByIdAsync(Guid id)
		{
			var actor = await _context.Actors
				.Include(a => a.MovieActors)
					.ThenInclude(ma => ma.Movie) // BẮC CẦU LẤY THÔNG TIN PHIM
				.FirstOrDefaultAsync(a => a.Id == id);

			if (actor == null)
				return null;

			return new ActorDto
			{
				Id = actor.Id,
				Name = actor.Name,
				Slug = actor.Slug,
				AvartUrl = actor.AvartUrl,
				DateOfBirth = actor.DateOfBirth,
				Gender = actor.Gender,

				SelectedMovieIds = actor.MovieActors.Select(ma => ma.MovieId).ToList(),

				ParticipatedMovieTitles = actor.MovieActors
											   .Where(ma => ma.Movie != null)
											   .Select(ma => ma.Movie.Title)
											   .ToList(),

				// THÊM ĐOẠN NÀY: Map list phim hiển thị ra Dashboard
				ParticipatedMovies = actor.MovieActors
									   .Where(ma => ma.Movie != null && ma.Movie.IsActive) // Chỉ lấy phim đang active
									   .Select(ma => new ActorMovieDto
									   {
										   Id = ma.Movie.Id,
										   Title = ma.Movie.Title,
										   Slug = ma.Movie.Slug,
										   PosterUrl = ma.Movie.PosterUrl
									   })
									   .ToList()
			};
		}

		// Create new actor
		public async Task<ActorDto> CreateAsync(ActorCreateDto dto)
		{
			// 1. Tạo thực thể Actor
			var actor = new Actor
			{
				Name = dto.Name,
				Slug = SlugHelper.GenerateSlug(dto.Name), // Tạo Slug tự động từ tên
				AvartUrl = dto.AvartUrl,
				DateOfBirth = dto.DateOfBirth,
				Gender = dto.Gender
			};

			// Thêm Actor vào DbContext (Lúc này Id mới được EF Core khởi tạo ngầm)
			_context.Actors.Add(actor);

			// 2. Xử lý mối quan hệ Nhiều - Nhiều (Gán Phim cho Diễn viên)
			if (dto.MovieIds != null && dto.MovieIds.Any())
			{
				foreach (var movieId in dto.MovieIds)
				{
					var movieActor = new MovieActor
					{
						ActorId = actor.Id, // Link với ID của Actor vừa tạo
						MovieId = movieId
					};
					_context.MovieActors.Add(movieActor);
				}
			}

			// 3. Lưu toàn bộ xuống Database trong 1 Transaction duy nhất
			await _context.SaveChangesAsync();

			// 4. Trả về DTO
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
			// FIX 2.2: Include bảng trung gian để so sánh Phim Cũ và Phim Mới
			var actor = await _context.Actors
				.Include(a => a.MovieActors)
				.FirstOrDefaultAsync(a => a.Id == dto.Id);

			if (actor == null) return false;

			// Cập nhật thông tin cơ bản
			actor.Name = dto.Name;
			actor.Slug = SlugHelper.GenerateSlug(dto.Name);
			if (!string.IsNullOrEmpty(dto.AvartUrl))
				actor.AvartUrl = dto.AvartUrl;
			actor.DateOfBirth = dto.DateOfBirth;
			actor.Gender = dto.Gender;

		
			// LOGIC CẬP NHẬT NHIỀU - NHIỀU (PARTICIPATED MOVIES)
		
			if (dto.MovieIds != null)
			{
				var existingMovieIds = actor.MovieActors.Select(ma => ma.MovieId).ToList();

				// 1. Phim nào lúc trước có, nhưng giờ admin đã bỏ chọn (Xóa đi) -> Remove khỏi DB
				var moviesToRemove = actor.MovieActors.Where(ma => !dto.MovieIds.Contains(ma.MovieId)).ToList();
				_context.MovieActors.RemoveRange(moviesToRemove);

				// 2. Phim nào mới được admin chọn thêm vào -> Add vào DB
				var newMovieIds = dto.MovieIds.Except(existingMovieIds).ToList();
				foreach (var movieId in newMovieIds)
				{
					_context.MovieActors.Add(new MovieActor { ActorId = actor.Id, MovieId = movieId });
				}
			}

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