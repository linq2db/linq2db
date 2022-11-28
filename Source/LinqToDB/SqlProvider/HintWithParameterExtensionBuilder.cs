using System;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using SqlQuery;

	sealed class HintWithParameterExtensionBuilder : ISqlQueryExtensionBuilder
	{
		void ISqlQueryExtensionBuilder.Build(ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
		{
			var hint  = ((SqlValue)sqlQueryExtension.Arguments["hint"]).    Value;
			var param = GetValue((SqlValue)sqlQueryExtension.Arguments["hintParameter"]);

			stringBuilder
				.Append(hint)
				.Append('(')
				.Append(param)
				.Append(')');


			object? GetValue(SqlValue value)
			{
				return value.Value is Sql.SqlID id ? sqlBuilder.BuildSqlID(id) : value.Value;
			}
		}
	}
}
