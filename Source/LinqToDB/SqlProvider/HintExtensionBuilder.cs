using System;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using SqlQuery;

	class HintExtensionBuilder : ISqlExtensionBuilder
	{
		public void Build(ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
		{
			var hint = (SqlValue)sqlQueryExtension.Arguments["hint"];
			stringBuilder.Append((string)hint.Value!);
		}
	}
}
