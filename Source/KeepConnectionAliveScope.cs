using System;

namespace LinqToDB
{
	public class KeepConnectionAliveScope : IDisposable
	{
		readonly DataContext _dataContext;
		readonly bool        _savedValue;

		public KeepConnectionAliveScope(DataContext dataContext)
		{
			_dataContext = dataContext;
			_savedValue  = dataContext.KeepConnectionAlive;

			dataContext.KeepConnectionAlive = true;
		}

		public void Dispose()
		{
			_dataContext.KeepConnectionAlive = _savedValue;
		}
	}
}
