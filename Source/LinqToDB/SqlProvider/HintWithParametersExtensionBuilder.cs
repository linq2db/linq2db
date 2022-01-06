using System;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using SqlQuery;

	class HintWithParametersExtensionBuilder : ISqlQueryExtensionBuilder
	{
		void ISqlQueryExtensionBuilder.Build(ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
		{
			var args  = sqlQueryExtension.Arguments;
			var hint  = ((SqlValue)     args["hint"]).                Value;
			var count = (int)((SqlValue)args["hintParameters.Count"]).Value!;

			var firstDelimiter = args.TryGetValue(".ExtensionArguments.0", out var extArg0) && extArg0 is SqlValue { Value : string val0 } ? val0 : ", ";
			var nextDelimiter  = args.TryGetValue(".ExtensionArguments.1", out var extArg1) && extArg1 is SqlValue { Value : string val1 } ? val1 : null;

			stringBuilder.Append(hint);

			if (count > 0)
			{
				stringBuilder.Append('(');

				var delimiter = string.Empty;

				for (var i = 0; i < count; i++)
				{
					if (i == 1)
						delimiter = firstDelimiter;
					else if (i > 0)
						delimiter = nextDelimiter ?? firstDelimiter;

					stringBuilder
						.Append(delimiter);

					var value = GetValue((SqlValue)args[$"hintParameters.{i}"]);
					stringBuilder.Append(value);
				}

				stringBuilder.Append(')');
			}

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
