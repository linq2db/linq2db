using System;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using SqlQuery;

	class HintWithParameterExtensionBuilder : ISqlQueryExtensionBuilder
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
				if (value.Value is Sql.SqlID id)
				{
					if (sqlBuilder.TableIDs?.TryGetValue(id.ID, out var path) == true)
						return path;
					throw new InvalidOperationException($"Table ID '{id.ID}' is not defined.");
				}

				return value.Value;
			}
		}
	}
}
