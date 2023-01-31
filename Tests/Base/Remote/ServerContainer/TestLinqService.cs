using System;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Remote;

namespace Tests.Remote.ServerContainer
{
	public class TestLinqService : LinqService
	{
		public override DataConnection CreateDataContext(string? configuration)
		{
			var dc = base.CreateDataContext(configuration);

			if (configuration?.IsAnyOf(TestProvName.AllMariaDB) == true)
				dc.AddMappingSchema(TestBase._mariaDBSchema);

			return dc;
		}
	}
}
