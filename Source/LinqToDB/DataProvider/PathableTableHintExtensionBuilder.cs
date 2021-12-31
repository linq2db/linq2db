using System;
using System.Text;

namespace LinqToDB.DataProvider.Oracle
{
	using SqlProvider;
	using SqlQuery;

	class PathableTableHintExtensionBuilder : ISqlTableExtensionBuilder
	{
		void ISqlTableExtensionBuilder.Build(ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension, SqlTable table, string alias)
		{
			if (sqlBuilder is not IPathableSqlBuilder builder)
				throw new ArgumentException($"{nameof(PathableTableHintExtensionBuilder)} supports only {typeof(IPathableSqlBuilder).FullName}", nameof(sqlBuilder));

			if (stringBuilder.Length > 0 && stringBuilder[stringBuilder.Length - 1] != ' ')
				stringBuilder.Append(' ');

			var args = sqlQueryExtension.Arguments;
			var hint = (SqlValue)args["hint"];

			stringBuilder.Append((string)hint.Value!);
			stringBuilder.Append('(');

			if (builder.TablePath is { Length: > 0 })
			{
				stringBuilder.Append(builder.TablePath);
				stringBuilder.Append('.');
			}

			stringBuilder.Append(alias);

			if (builder.QueryName is not null)
				stringBuilder
					.Append('@')
					.Append(builder.QueryName)
					;

			if (args.TryGetValue("hintParameter", out var hintParameter))
			{
				var param = ((SqlValue)hintParameter).Value;

				stringBuilder.Append(' ');
				stringBuilder.Append(param);
			}
			else if (args.TryGetValue("hintParameters.Count", out var hintParametersCount))
			{
				var parameterDelimiter = args.TryGetValue(".ExtensionArguments.0", out var extArg0) && extArg0 is SqlValue { Value : string val } ? val : " ";
				var count              = (int)((SqlValue)hintParametersCount).Value!;

				stringBuilder.Append(' ');

				for (var i = 0; i < count; i++)
				{
					var value = ((SqlValue)args[$"hintParameters.{i}"]).Value;

					if (i > 0)
						stringBuilder.Append(parameterDelimiter);
					stringBuilder.Append(value);
				}
			}

			stringBuilder.Append(')');
		}
	}
}
