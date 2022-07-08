using System;
using System.Data.Common;

namespace LinqToDB.DataProvider
{
	interface IConnectionWrapper : IDisposable
	{
		string ServerVersion { get; }

		public DbCommand CreateCommand();
		public void      Open();
	}
}
