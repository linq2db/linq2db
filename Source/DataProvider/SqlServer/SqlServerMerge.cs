using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.SqlServer
{
	using Data;

	class SqlServerMerge : BasicMerge
	{
		public SqlServerMerge()
		{
			ByTargetText = "BY Target ";
		}

		protected override bool BuildCommand<T>(DataConnection dataConnection, bool delete, IEnumerable<T> source)
		{
			if (!base.BuildCommand(dataConnection, delete, source))
				return false;

			StringBuilder.AppendLine(";");

			return true;
		}
	}
}
