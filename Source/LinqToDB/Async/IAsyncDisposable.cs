using System.Threading.Tasks;

namespace LinqToDB.Async
{
	/// <summary>
	/// Provides a mechanism for releasing unmanaged resources asynchronously.
	/// </summary>
	public interface IAsyncDisposable
	{
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources asynchronously.
		/// </summary>
		/// <returns>
		/// A task that represents the asynchronous dispose operation.
		/// </returns>
		/// <returns></returns>
		public ValueTask DisposeAsync();
	}
}
