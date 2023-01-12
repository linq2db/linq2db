using System;

namespace LinqToDB.DataProvider
{
	interface IConnectionWrapper : IDisposable
	{
		public void      Open();
	}
}
