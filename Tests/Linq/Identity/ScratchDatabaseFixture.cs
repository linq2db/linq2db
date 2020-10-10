using System;

namespace Tests.Identity
{
	public class ScratchDatabaseFixture : IDisposable
	{
		private SqlServerTestStore _testStore = SqlServerTestStore.CreateScratch();

		public string ConnectionString => _testStore.Connection.ConnectionString;

		public void Dispose()
		{
			_testStore?.Dispose();
		}
	}
}
