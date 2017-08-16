using System;

namespace LinqToDB
{
	/// <summary>
	/// Explicit <see cref="DataContext"/> connection reuse scope.
	/// See <see cref="DataContext.KeepConnectionAlive"/> for more details.
	/// </summary>
	public class KeepConnectionAliveScope : IDisposable
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

			dataContext.KeepConnectionAlive = true;
		}

		/// <summary>
		/// Restores old connection reuse option.
		/// </summary>
		public void Dispose()
		{
			_dataContext.KeepConnectionAlive = _savedValue;
		}
	}
}
