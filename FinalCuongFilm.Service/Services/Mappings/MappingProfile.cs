using AutoMapper;
using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.Service.Mappings
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			// ===== Movie Mappings =====

			// Movie → MovieDto (Read)
			CreateMap<Movie, MovieDto>()
				.ForMember(dest => dest.CountryName, opt => opt.MapFrom(src => src.Country != null ? src.Country.Name : null))
				.ForMember(dest => dest.LanguageName, opt => opt.MapFrom(src => src.Language != null ? src.Language.Name : null))
				//.ForMember(dest => dest.GenreNames, opt => opt.MapFrom(src => src.Movie_Genres.Select(mg => mg.Genre.Name).ToList()))
				.ForMember(dest => dest.SelectedGenreIds, opt => opt.MapFrom(src => src.Movie_Genres.Select(mg => mg.GenreId).ToList()));

			// MovieDto → Movie
			CreateMap<MovieDto, Movie>()
				.ForMember(dest => dest.Country, opt => opt.Ignore())
				.ForMember(dest => dest.Language, opt => opt.Ignore())
				.ForMember(dest => dest.Movie_Genres, opt => opt.Ignore())
				.ForMember(dest => dest.Movie_Actors, opt => opt.Ignore())
				.ForMember(dest => dest.Movie_Tags, opt => opt.Ignore())
				.ForMember(dest => dest.Episodes, opt => opt.Ignore())
				.ForMember(dest => dest.MediaFiles, opt => opt.Ignore())
				.ForMember(dest => dest.Favorites, opt => opt.Ignore())
				.ForMember(dest => dest.Reviews, opt => opt.Ignore());

			//  ADD: MovieCreateDto → Movie (Create)
			CreateMap<MovieCreateDto, Movie>()
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.Country, opt => opt.Ignore())
				.ForMember(dest => dest.Language, opt => opt.Ignore())
				.ForMember(dest => dest.Movie_Genres, opt => opt.Ignore())
				.ForMember(dest => dest.Movie_Actors, opt => opt.Ignore())
				.ForMember(dest => dest.Movie_Tags, opt => opt.Ignore())
				.ForMember(dest => dest.Episodes, opt => opt.Ignore())
				.ForMember(dest => dest.MediaFiles, opt => opt.Ignore())
				.ForMember(dest => dest.Favorites, opt => opt.Ignore())
				.ForMember(dest => dest.Reviews, opt => opt.Ignore());

			//  ADD: MovieUpdateDto → Movie (Update)
			CreateMap<MovieUpdateDto, Movie>()
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.Country, opt => opt.Ignore())
				.ForMember(dest => dest.Language, opt => opt.Ignore())
				.ForMember(dest => dest.Movie_Genres, opt => opt.Ignore())
				.ForMember(dest => dest.Movie_Actors, opt => opt.Ignore())
				.ForMember(dest => dest.Movie_Tags, opt => opt.Ignore())
				.ForMember(dest => dest.Episodes, opt => opt.Ignore())
				.ForMember(dest => dest.MediaFiles, opt => opt.Ignore())
				.ForMember(dest => dest.Favorites, opt => opt.Ignore())
				.ForMember(dest => dest.Reviews, opt => opt.Ignore())
				.ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

			// ===== Episode Mappings =====
			CreateMap<Episode, EpisodeDto>().ReverseMap();

			//  ADD: EpisodeCreateDto → Episode
			CreateMap<EpisodeCreateDto, Episode>()
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.Movie, opt => opt.Ignore())
				.ForMember(dest => dest.MediaFiles, opt => opt.Ignore());

			//  ADD: EpisodeUpdateDto → Episode
			CreateMap<EpisodeUpdateDto, Episode>()
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.Movie, opt => opt.Ignore())
				.ForMember(dest => dest.MediaFiles, opt => opt.Ignore())
				.ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

			// ===== MediaFile Mappings =====
			CreateMap<MediaFile, MediaFileDto>().ReverseMap();

			// ===== Genre Mappings =====
			CreateMap<Genre, GenreDto>().ReverseMap();

			//  ADD: GenreCreateDto → Genre
			CreateMap<GenreCreateDto, Genre>()
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.Movie_Genres, opt => opt.Ignore());

			// ===== Country Mappings =====
			CreateMap<Country, CountryDto>().ReverseMap();

			//  ADD: CountryCreateDto → Country
			CreateMap<CountryCreateDto, Country>()
				.ForMember(dest => dest.Id, opt => opt.Ignore());

			// ===== Language Mappings =====
			CreateMap<Language, LanguageDto>().ReverseMap();

			//  ADD: LanguageCreateDto → Language
			CreateMap<LanguageCreateDto, Language>()
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.Movies, opt => opt.Ignore());

			// ===== Actor Mappings =====
			CreateMap<Actor, ActorDto>().ReverseMap();

			//  ADD: ActorCreateDto → Actor
			CreateMap<ActorCreateDto, Actor>()
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.Movie_Actors, opt => opt.Ignore());

			// ===== Tag Mappings =====
			//CreateMap<Tag, TagDto>().ReverseMap();

			//  ADD: TagCreateDto → Tag
			//CreateMap<TagCreateDto, Tag>()
			//	.ForMember(dest => dest.Id, opt => opt.Ignore())
			//	.ForMember(dest => dest.Movie_Tags, opt => opt.Ignore());

			// ===== Review Mappings =====
			CreateMap<Review, ReviewDto>().ReverseMap();

			//  ADD: ReviewCreateDto → Review
			CreateMap<ReviewCreateDto, Review>()
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.Movie, opt => opt.Ignore());

			// ===== Favorite Mappings =====
			CreateMap<Favorite, FavoriteDto>()
				.ForMember(dest => dest.MovieTitle, opt => opt.MapFrom(src => src.Movie != null ? src.Movie.Title : null));

			CreateMap<FavoriteDto, Favorite>()
				.ForMember(dest => dest.Movie, opt => opt.Ignore());

			//  ADD: FavoriteCreateDto → Favorite
			CreateMap<FavoriteCreateDto, Favorite>()
				.ForMember(dest => dest.Id, opt => opt.Ignore())
				.ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
				.ForMember(dest => dest.Movie, opt => opt.Ignore());
		}
	}
}