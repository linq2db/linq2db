using System;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB
{
	/// <summary>
	/// Explicit <see cref="DataContext"/> connection reuse scope.
	/// See <see cref="DataContext.KeepConnectionAlive"/> for more details.
	/// </summary>
	[PublicAPI]
	public class KeepConnectionAliveScope : IDisposable, IAsyncDisposable
	{
		readonly DataContext _dataContext;
		readonly bool        _savedValue;

		/// <summary>
		/// Creates connection reuse scope for <see cref="DataContext"/>.
		/// </summary>
		/// <param name="dataContext">Data context.</param>
		public KeepConnectionAliveScope(DataContext dataContext)
		{
			_dataContext = dataContext;
			_savedValue  = dataContext.KeepConnectionAlive;

			// it is safe to call sync API as 'true' value doesn't trigger blocking operation
			dataContext.SetKeepConnectionAlive(true);
		}

		/// <summary>
		/// Restores old connection reuse option.
		/// </summary>
		public void Dispose()
		{
			_dataContext.SetKeepConnectionAlive(_savedValue);
		}

		public async ValueTask DisposeAsync()
		{
			await _dataContext.SetKeepConnectionAliveAsync(_savedValue).ConfigureAwait(false);
		}
	}
}
