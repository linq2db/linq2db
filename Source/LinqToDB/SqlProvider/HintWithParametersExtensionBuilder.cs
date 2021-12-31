using System;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using DataProvider;
	using SqlQuery;

	class HintWithParametersExtensionBuilder : ISqlQueryExtensionBuilder
	{
		void ISqlQueryExtensionBuilder.Build(ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
		{
			var args  = sqlQueryExtension.Arguments;
			var hint  = ((SqlValue)     args["hint"]).                Value;
			var count = (int)((SqlValue)args["hintParameters.Count"]).Value!;

			var parameterDelimiter = args.TryGetValue(".ExtensionArguments.0", out var extArg0) && extArg0 is SqlValue { Value : string val } ? val : ", ";

			stringBuilder.Append(hint);

			if (count > 0)
			{
				stringBuilder.Append('(');

				for (var i = 0; i < count; i++)
				{
					var value = GetValue((SqlValue)args[$"hintParameters.{i}"]);
					stringBuilder
						.Append(value)
						.Append(parameterDelimiter);
				}

				stringBuilder.Length -= parameterDelimiter.Length;
				stringBuilder.Append(')');
			}

			object? GetValue(SqlValue value)
			{
				if (value.Value is Sql.SqlID id)
				{
					if (sqlBuilder is IPathableSqlBuilder pb && pb.TableIDs?.TryGetValue(id.ID, out var path) == true)
						return path;
					throw new InvalidOperationException($"Table ID '{id.ID}' is not defined.");
				}

				return value.Value;
			}
		}
	}
}
