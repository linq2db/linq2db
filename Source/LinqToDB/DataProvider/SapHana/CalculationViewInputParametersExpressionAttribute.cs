using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.SapHana
{
	using System.Globalization;
	using System.Linq.Expressions;
	using LinqToDB.Expressions;
	using LinqToDB.SqlProvider;
	using SqlQuery;

	public class CalculationViewInputParametersExpressionAttribute : Sql.TableExpressionAttribute
	{
		public CalculationViewInputParametersExpressionAttribute() :
			base("")
		{
		}

		// we can't use BasicSqlBuilder.GetValueBuilder, because
		// a) we need to escape with ' every value,
		// b) we don't have dataprovider here ether
		private static string? ValueToString(object value)
		{
			if (value is string stringValue)
				return stringValue;
			if (value is decimal decimalValue)
				return decimalValue.ToString(new NumberFormatInfo());
			if (value is double doubleValue)
				return doubleValue.ToString(new NumberFormatInfo());
			if (value is float floatValue)
				return floatValue.ToString(new NumberFormatInfo());

			return value.ToString();
		}

		public override void SetTable<TContext>(DataOptions options, TContext context, ISqlBuilder sqlBuilder, MappingSchema mappingSchema, SqlTable table, MethodCallExpression methodCall, Func<TContext, Expression, ColumnDescriptor?, ISqlExpression> converter)
		{
			var paramsList = methodCall.Method.GetParameters();

			var sqlValues = new List<ISqlExpression>();

			for(var i = 0; i < paramsList.Length; i++)
			{
				var val = methodCall.Arguments[i].EvaluateExpression();
				if (val == null)
					continue;
				var p = paramsList[i];
				sqlValues.Add(new SqlValue("$$" + p.Name + "$$"));
				sqlValues.Add(new SqlValue(ValueToString(val)!));
			}

			var arg = new ISqlExpression[1];

			arg[0] = new SqlExpression(
				string.Join(", ",
					Enumerable.Range(0, sqlValues.Count)
						.Select(static x => "{" + x + "}")),
				sqlValues.ToArray());

			table.SqlTableType   = SqlTableType.Expression;
			table.Expression     = "{0}('PLACEHOLDER' = {2}) {1}";
			table.TableArguments = arg.ToArray();
		}
	}
}
