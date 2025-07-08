using System;

using LinqToDB.Data;

namespace LinqToDB.Tools.DataProvider.SqlServer.Schemas
{
	public class SystemDB : DataConnection, ISystemSchemaData
	{
		public SystemDB(string configuration)
			: base(configuration)
		{
		}

		public SystemDB(DataOptions options)
			: base(options)
		{
		}

		public SystemDB(DataOptions<SystemDB> options)
			: base(options.Options)
		{
		}

		SystemSchemaModel? _system;

		public SystemSchemaModel System => _system ??= new SystemSchemaModel(this);
	}
}
