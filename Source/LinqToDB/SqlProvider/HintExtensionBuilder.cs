using System;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using SqlQuery;

	sealed class HintExtensionBuilder : ISqlQueryExtensionBuilder
	{
		void ISqlQueryExtensionBuilder.Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
		{
			var hint = (SqlValue)sqlQueryExtension.Arguments["hint"];
			stringBuilder.Append((string)hint.Value!);
		}
	}
}
