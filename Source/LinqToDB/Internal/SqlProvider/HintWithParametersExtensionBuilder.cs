using System;
using System.Globalization;
using System.Text;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.SqlProvider
{
	sealed class HintWithParametersExtensionBuilder : ISqlQueryExtensionBuilder
	{
		void ISqlQueryExtensionBuilder.Build(NullabilityContext nullability, ISqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
		{
			var args  = sqlQueryExtension.Arguments;
			var hint  = ((SqlValue)     args["hint"]).                Value;
			var count = (int)((SqlValue)args["hintParameters.Count"]).Value!;

			var firstDelimiter = args.TryGetValue(".ExtensionArguments.0", out var extArg0) && extArg0 is SqlValue { Value : string val0 } ? val0 : ", ";
			var nextDelimiter  = args.TryGetValue(".ExtensionArguments.1", out var extArg1) && extArg1 is SqlValue { Value : string val1 } ? val1 : null;

			stringBuilder.Append(CultureInfo.InvariantCulture, $"{hint}");

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

					var value = GetValue((SqlValue)args[FormattableString.Invariant($"hintParameters.{i}")]);
					stringBuilder.Append(CultureInfo.InvariantCulture, $"{value}");
				}

				stringBuilder.Append(')');
			}

			object? GetValue(SqlValue value)
			{
				return value.Value is Sql.SqlID id ? sqlBuilder.BuildSqlID(id) : value.Value;
			}
		}
	}
}
