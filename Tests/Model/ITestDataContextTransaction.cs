using System;

using LinqToDB;

namespace Tests.Model
{
	public interface ITestDataContextTransaction : IDisposable
	{
		void Commit();
	}
}
