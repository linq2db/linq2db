﻿using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using LinqToDB.Expressions;

	class OrderByBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (!methodCall.IsQueryable("OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending", "ThenOrBy", "ThenOrByDescending"))
				return false;

			var body = ((LambdaExpression)methodCall.Arguments[1].Unwrap()).Body.Unwrap();

			if (body.NodeType == ExpressionType.MemberInit)
			{
				var mi = (MemberInitExpression)body;
				bool throwExpr;

				if (mi.NewExpression.Arguments.Count > 0 || mi.Bindings.Count == 0)
					throwExpr = true;
				else
					throwExpr = mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment);

				if (throwExpr)
					throw new NotSupportedException($"Explicit construction of entity type '{body.Type}' in order by is not allowed.");
			}

			return true;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var wrapped = false;

			if (sequence.SelectQuery.Select.TakeValue != null ||
				sequence.SelectQuery.Select.SkipValue != null)
			{
				sequence = new SubQueryContext(sequence);
				wrapped = true;
			}

			var lambda  = (LambdaExpression)methodCall.Arguments[1].Unwrap();
			SqlInfo[] sql;

			while (true)
			{
				var sparent = sequence.Parent;
				var order   = new ExpressionContext(buildInfo.Parent, sequence, lambda);
				var body    = lambda.Body.Unwrap();
				    sql     = builder.ConvertExpressions(order, body, ConvertFlags.Key, null);

				builder.ReplaceParent(order, sparent);

			    if (wrapped)
				    break;

				// handle situation when order by uses complex field
			    
			    var isComplex = false;

			    foreach (var sqlInfo in sql)
			    {
				    // immutable expressions will be removed later
				    //
					var isImmutable = QueryHelper.IsImmutable(sqlInfo.Sql);
					if (isImmutable)
						continue;
					
					// possible we have to extend this list
					//
				    isComplex = null != new QueryVisitor().Find(sqlInfo.Sql, 
					                e => e.ElementType == QueryElementType.SqlQuery);
				    if (isComplex)
					    break;
			    }

			    if (!isComplex)
				    break;

			    sequence = new SubQueryContext(sequence);
			    wrapped = true;
			}

	
			if (!methodCall.Method.Name.StartsWith("Then") && !Configuration.Linq.DoNotClearOrderBys)
				sequence.SelectQuery.OrderBy.Items.Clear();

			foreach (var expr in sql)
			{
				// we do not need sorting by immutable values, like "Some", Func("Some"), "Some1" + "Some2". It does nothing for ordering
				//
				if (QueryHelper.IsImmutable(expr.Sql))
					continue;
			
				var e = builder.ConvertSearchCondition(expr.Sql);
				sequence.SelectQuery.OrderBy.Expr(e, methodCall.Method.Name.EndsWith("Descending"));
			}

			return sequence;
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}
	}
}
