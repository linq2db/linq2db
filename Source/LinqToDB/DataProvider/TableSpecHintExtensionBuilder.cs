using System;
using System.Globalization;
using System.Text;

namespace LinqToDB.DataProvider
{
	using SqlProvider;
	using SqlQuery;

	sealed class TableSpecHintExtensionBuilder : ISqlTableExtensionBuilder
	{
		void ISqlTableExtensionBuilder.Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension, SqlTable table, string alias)
		{
			if (stringBuilder.Length > 0 && stringBuilder[^1] != ' ')
				stringBuilder.Append(' ');

			var args = sqlQueryExtension.Arguments;
			var hint = (SqlValue)args["hint"];

			stringBuilder.Append((string)hint.Value!);
			stringBuilder.Append('(');

			if (sqlBuilder.TablePath is { Length: > 0 })
			{
				stringBuilder.Append(sqlBuilder.TablePath);
				stringBuilder.Append('.');
			}

			stringBuilder.Append(alias);

			if (sqlBuilder.QueryName is not null && sqlBuilder.SqlProviderFlags.IsNamingQueryBlockSupported)
				stringBuilder
					.Append('@')
					.Append(sqlBuilder.QueryName)
					;

			if (args.TryGetValue("hintParameter", out var hintParameter))
			{
				var param     = ((SqlValue)hintParameter).Value;
				var delimiter = args.TryGetValue(".ExtensionArguments.0", out var extArg) && extArg is SqlValue { Value : string val } ? val : " ";

				stringBuilder.Append(CultureInfo.InvariantCulture, $"{delimiter}{param}");
			}
			else if (args.TryGetValue("hintParameters.Count", out var hintParametersCount))
			{
				var delimiter0 = args.TryGetValue(".ExtensionArguments.0", out var extArg0) && extArg0 is SqlValue { Value : string val0 } ? val0 : " ";
				var delimiter1 = args.TryGetValue(".ExtensionArguments.1", out var extArg1) && extArg1 is SqlValue { Value : string val1 } ? val1 : " ";
				var count      = (int)((SqlValue)hintParametersCount).Value!;

				if (count > 0)
				{
					stringBuilder.Append(delimiter0);

					for (var i = 0; i < count; i++)
					{
						var value = ((SqlValue)args[FormattableString.Invariant($"hintParameters.{i}")]).Value;

						if (i > 0)
							stringBuilder.Append(delimiter1);
						stringBuilder.Append(CultureInfo.InvariantCulture, $"{value}");
					}
				}
			}

			stringBuilder.Append(')');
		}
	}
}
