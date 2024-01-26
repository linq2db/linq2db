using System;
using System.Data.Common;

namespace LinqToDB.DataProvider
{
	interface IConnectionWrapper : IDisposable
	{
		void Open();
		DbConnection Connection { get; }
	}
}
