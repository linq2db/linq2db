using System.Globalization;
using System.Text;

using LinqToDB.SqlQuery;

namespace LinqToDB.SqlProvider
{
	sealed class HintWithParameterExtensionBuilder : ISqlQueryExtensionBuilder
	{
		void ISqlQueryExtensionBuilder.Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
		{
			var hint  = ((SqlValue)sqlQueryExtension.Arguments["hint"]).    Value;
			var param = GetValue((SqlValue)sqlQueryExtension.Arguments["hintParameter"]);

			stringBuilder.Append(CultureInfo.InvariantCulture, $"{hint}({param})");

			object? GetValue(SqlValue value)
			{
				return value.Value is Sql.SqlID id ? sqlBuilder.BuildSqlID(id) : value.Value;
			}
		}
	}
}
