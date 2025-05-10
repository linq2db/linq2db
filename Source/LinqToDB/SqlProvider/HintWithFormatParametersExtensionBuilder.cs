using System;
using System.Globalization;
using System.Text;

using LinqToDB.SqlQuery;

namespace LinqToDB.SqlProvider
{
	sealed class HintWithFormatParametersExtensionBuilder : ISqlQueryExtensionBuilder
	{
		void ISqlQueryExtensionBuilder.Build(NullabilityContext nullability, SqlBuilder sqlBuilder, StringBuilder stringBuilder, SqlQueryExtension sqlQueryExtension)
		{
			var args      = sqlQueryExtension.Arguments;
			var hint      = ((SqlValue)        args["hint"]).                Value;
			var format    = (string)((SqlValue)args["hintFormat"]).          Value!;
			var count     = (int)   ((SqlValue)args["hintParameters.Count"]).Value!;
			var delimiter = args.TryGetValue(".ExtensionArguments.0", out var extArg0) && extArg0 is SqlValue { Value : string val0 } ? val0 : " ";

			stringBuilder.Append(CultureInfo.InvariantCulture, $"{hint}{delimiter}");

			if (count > 0)
			{
				var ps = new object?[count];

				for (var i = 0; i < count; i++)
					ps[i] = GetValue((SqlValue)args[FormattableString.Invariant($"hintParameters.{i}")]);

				stringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, ps);
			}
			else
			{
				stringBuilder.Append(format);
			}

			object? GetValue(SqlValue value)
			{
				return value.Value is Sql.SqlID id ? sqlBuilder.BuildSqlID(id) : value.Value;
			}
		}
	}
}
