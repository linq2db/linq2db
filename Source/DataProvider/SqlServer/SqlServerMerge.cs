using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.SqlServer
{
	using Data;

	class SqlServerMerge : BasicMerge
	{
		public SqlServerMerge()
		{
			ByTargetText = "BY Target ";
		}

		protected override bool BuildCommand<T>(DataConnection dataConnection, Expression<Func<T,bool>> deletePredicate, bool delete, IEnumerable<T> source)
		{
			if (!base.BuildCommand(dataConnection, deletePredicate, delete, source))
				return false;

			StringBuilder.AppendLine(";");

			return true;
		}
	}
}
