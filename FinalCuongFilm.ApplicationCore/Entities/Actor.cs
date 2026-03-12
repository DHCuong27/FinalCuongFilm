using FinalCuongFilm.ApplicationCore.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Actor
{
	[Key]
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; } = string.Empty;
	public string Slug { get; set; } = string.Empty;
	public string? AvartUrl { get; set; }
	public DateTime? DateOfBirth { get; set; }
	public string? Gender { get; set; }

	public long? TmdbId { get; set; }

	public ICollection<MovieActor> MovieActors { get; set; } = new List<MovieActor>();

	[NotMapped]
	public int Age => DateOfBirth.HasValue ? DateTime.Today.Year - DateOfBirth.Value.Year : 0;
}