using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions.Internal;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.SapHana
{
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

			return string.Format(CultureInfo.InvariantCulture, "{0}", value);
		}

		public override void SetTable<TContext>(DataOptions options, TContext context, ISqlBuilder sqlBuilder, MappingSchema mappingSchema, SqlTable table, MethodCallExpression methodCall, Sql.ExpressionAttribute.ConvertFunc<TContext> converter)
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

			arg[0] = new SqlFragment(
				string.Join(", ",
					Enumerable.Range(0, sqlValues.Count)
						.Select(static x => FormattableString.Invariant($"{{{x}}}"))),
				sqlValues.ToArray());

			table.SqlTableType   = SqlTableType.Expression;
			table.Expression     = "{0}('PLACEHOLDER' = {2}) {1}";
			table.TableArguments = arg.ToArray();
		}
	}
}
