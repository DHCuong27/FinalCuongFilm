using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FinalCuongFilm.Common.Helpers
{
	public static class SlugHelper
	{
		public static string GenerateSlug(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return string.Empty;

			// Chuyển về lowercase
			text = text.ToLowerInvariant();

			// Chuyển đổi Unicode tiếng Việt sang không dấu
			text = RemoveVietnameseTones(text);

			// Xóa các ký tự đặc biệt, chỉ giữ chữ, số, và dấu gạch ngang
			text = Regex.Replace(text, @"[^a-z0-9\s-]", "");

			// Thay thế khoảng trắng bằng dấu gạch ngang
			text = Regex.Replace(text, @"\s+", "-").Trim('-');

			// Xóa các dấu gạch ngang liên tiếp
			text = Regex.Replace(text, @"-+", "-");

			return text;
		}

		private static string RemoveVietnameseTones(string text)
		{
			string[] vietnameseSigns = new string[]
			{
				"aAeEoOuUiIdDyY",
				"áàạảãâấầậẩẫăắằặẳẵ",
				"ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
				"éèẹẻẽêếềệểễ",
				"ÉÈẸẺẼÊẾỀỆỂỄ",
				"óòọỏõôốồộổỗơớờợởỡ",
				"ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
				"úùụủũưứừựửữ",
				"ÚÙỤỦŨƯỨỪỰỬỮ",
				"íìịỉĩ",
				"ÍÌỊỈĨ",
				"đ",
				"Đ",
				"ýỳỵỷỹ",
				"ÝỲỴỶỸ"
			};

			for (int i = 1; i < vietnameseSigns.Length; i++)
			{
				for (int j = 0; j < vietnameseSigns[i].Length; j++)
				{
					text = text.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
				}
			}

			return text;
		}
	}
}