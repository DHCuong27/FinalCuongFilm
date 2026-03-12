using System.Threading.Tasks;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface IMovieImportService
	{
		Task<(bool Success, string Message)> ImportMovieAsync(string title);
	}
}