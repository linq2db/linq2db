using System;

using LinqToDB.Data;
using LinqToDB.Remote;

namespace Tests.Remote.ServerContainer
{
	public class TestLinqService : LinqService
	{
		public override DataConnection CreateDataContext(string? configuration)
		{
			var dc = base.CreateDataContext(configuration);

			return dc;
		}
	}
}
