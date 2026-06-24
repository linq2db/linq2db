using System.Text;

namespace Tests.LocalTestGenerator
{
	internal static class ProjectNameSanitizer
	{
		public static string SanitizerFallback { get; } = "LocalTests";

		public static string Sanitize(string value)
		{
			var builder = new StringBuilder(value.Length);

			foreach (var ch in value)
			{
				if (char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-')
					builder.Append(ch);
				else
					builder.Append('_');
			}

			var result = builder.ToString().Trim('.', '_', '-');

			while (result.Contains("__", StringComparison.Ordinal))
				result = result.Replace("__", "_", StringComparison.Ordinal);

			return result.Length == 0 ? SanitizerFallback : result;
		}
	}
}
