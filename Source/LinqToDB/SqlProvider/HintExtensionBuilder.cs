using System.Text;

using LinqToDB.SqlQuery;

namespace LinqToDB.SqlProvider
{
	sealed class HintExtensionBuilder : ISqlQueryExtensionBuilder
	{
		void ISqlQueryExtensionBuilder.Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
		{
			var hint = (SqlValue)sqlQueryExtension.Arguments["hint"];
			stringBuilder.Append((string)hint.Value!);
		}
	}
}
