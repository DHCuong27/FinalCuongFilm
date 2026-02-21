using AutoMapper;
using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.Service.Mappings
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			// Movie mappings
			CreateMap<Movie, MovieDto>()
				.ForMember(dest => dest.CountryName, opt => opt.MapFrom(src => src.Country != null ? src.Country.Name : null))
				.ForMember(dest => dest.LanguageName, opt => opt.MapFrom(src => src.Language != null ? src.Language.Name : null))
				//.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Movie_Genres.Select(mg => mg.Genre.Name).ToList()))
				.ForMember(dest => dest.SelectedGenreIds, opt => opt.MapFrom(src => src.Movie_Genres.Select(mg => mg.GenreId).ToList()));

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

			// Episode mappings
			CreateMap<Episode, EpisodeDto>().ReverseMap();

			// MediaFile mappings
			CreateMap<MediaFile, MediaFileDto>().ReverseMap();

			// Genre mappings
			CreateMap<Genre, GenreDto>().ReverseMap();

			// Country mappings
			CreateMap<Country, CountryDto>().ReverseMap();

			// Language mappings
			CreateMap<Language, LanguageDto>().ReverseMap();

			// Actor mappings
			CreateMap<Actor, ActorDto>().ReverseMap();

			// Tag mappings
			//CreateMap<Tag, TagDto>().ReverseMap();

			// Review mappings
			CreateMap<Review, ReviewDto>().ReverseMap();

			// Favorite mappings
			CreateMap<Favorite, FavoriteDto>().ReverseMap();
		}
	}
}