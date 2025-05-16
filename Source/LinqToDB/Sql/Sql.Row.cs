using System;
using System.Linq;

using LinqToDB.SqlQuery;

namespace LinqToDB
{
	partial class Sql
	{
		sealed class RowBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var args = Array.ConvertAll(builder.Arguments, x => builder.ConvertExpressionToSql(x));

				if (args.Any(a => a == null))
				{
					builder.IsConvertible = false;
					return;
				}

				builder.ResultExpression = new SqlRowExpression(args!);
			}
		}

		sealed class OverlapsBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var args = Array.ConvertAll(builder.Arguments, x => builder.ConvertExpressionToSql(x));

				if (args.Any(a => a == null))
				{
					builder.IsConvertible = false;
					return;
				}

				builder.ResultExpression = new SqlSearchCondition(false, canBeUnknown: null, new SqlPredicate.ExprExpr(args[0]!, SqlPredicate.Operator.Overlaps, args[1]!, false));
			}
		}
	}
}
