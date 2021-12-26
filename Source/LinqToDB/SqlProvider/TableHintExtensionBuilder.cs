using System;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using SqlQuery;

	class TableHintExtensionBuilder : ISqlExtensionBuilder
	{
		public void Build(ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
		{
			var hint = (SqlValue)sqlQueryExtension.Arguments["tableHint"];
			stringBuilder.Append((string)hint.Value!);
		}
	}
}
