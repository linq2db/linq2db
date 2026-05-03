using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Extensions
{
	internal static class IAsyncDisposableExtensions
	{
		public static async ValueTask DisposeAsync(ConfiguredAsyncDisposable? disposable)
		{
			if (disposable is { } d)
				await d.DisposeAsync();
		}
	}
}
