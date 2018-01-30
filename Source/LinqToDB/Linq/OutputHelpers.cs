using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Extensions;
using LinqToDB.Linq.Builder;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq
{
	internal class OutputHelpers
	{
		public static void BuildOutput(
			ExpressionBuilder               builder,
			BuildInfo                       buildInfo,
			LambdaExpression                outputExpression,
			IBuildContext                   into,
			List<ISqlExpression>            items,
			IBuildContext                   inserted,
			IBuildContext                   deleted)
		{
			var contexts = new List<IBuildContext>();
			if (inserted != null)
				contexts.Add(inserted);
			if (deleted != null)
				contexts.Add(deleted);

			var path = Expression.Parameter(outputExpression.Body.Type, "p");
			var ctx = new ExpressionContext(buildInfo.Parent,
				contexts.ToArray(),
				outputExpression);

			switch (outputExpression.Body.NodeType)
			{
				case ExpressionType.MemberInit:
				{
					var ex  = (MemberInitExpression)outputExpression.Body;
					//var p   = inserted.Parent;

					BuildOutput2(builder, into, items, ctx, ex, path);

					//builder.ReplaceParent(ctx, p);
					break;
				}

				case ExpressionType.New:
				{
					var ex  = (NewExpression)outputExpression.Body;
					//var p   = inserted.Parent;

					BuildOutput3(builder, into, items, ctx, ex, path);

					//builder.ReplaceParent(ctx, p);
					break;
				}

			default:
				var sqlInfo = ctx.ConvertToSql(outputExpression.Body, 0, ConvertFlags.All);

				foreach (var info in sqlInfo)
				{
					if (info.Members.Count == 0)
						throw new LinqException("Object initializer expected for insert statement.");

					if (info.Members.Count != 1)
						throw new InvalidOperationException();

					var member = info.Members[0];
					var pe     = Expression.MakeMemberAccess(path, member);
					//var column = into.ConvertToSql(pe, 1, ConvertFlags.Field);
					var expr   = info.Sql;

					items.Add(expr);
				}
				break;
			}
		}


		static void BuildOutput2(
			ExpressionBuilder               builder,
			IBuildContext                   into,
			List<ISqlExpression>            items,
			IBuildContext                   ctx,
			MemberInitExpression            expression,
			Expression                      path)
		{
			foreach (var binding in expression.Bindings)
			{
				var member  = binding.Member;

				if (member is MethodInfo)
					member = ((MethodInfo)member).GetPropertyInfo();

				if (binding is MemberAssignment)
				{
					var ma = binding as MemberAssignment;
					var pe = Expression.MakeMemberAccess(path, member);

					if (ma.Expression is MemberInitExpression && !into.IsExpression(pe, 1, RequestFor.Field).Result)
					{
						BuildOutput2(
							builder,
							into,
							items,
							ctx,
							(MemberInitExpression)ma.Expression, Expression.MakeMemberAccess(path, member));
					}
					else
					{
						var column = into.ConvertToSql(pe, 1, ConvertFlags.Field);
						var expr   = builder.ConvertToSqlExpression(ctx, ma.Expression);

						if (expr.ElementType == QueryElementType.SqlParameter)
						{
							var parm  = (SqlParameter)expr;
							var field = column[0].Sql is SqlField
								? (SqlField)column[0].Sql
								: (SqlField)((SqlColumn)column[0].Sql).Expression;

							if (parm.DataType == DataType.Undefined)
								parm.DataType = field.DataType;
						}

						items.Add(expr);
					}
				}
				else
					throw new InvalidOperationException();
			}
		}

		static void BuildOutput3(
			ExpressionBuilder               builder,
			IBuildContext                   into,
			List<ISqlExpression>            items,
			IBuildContext                   ctx,
			NewExpression                   expression,
			Expression                      path)
		{
			for (int i = 0; i < expression.Members.Count; i++)
			{
				var member = expression.Members[i];
				if (member is MethodInfo)
					member = ((MethodInfo)member).GetPropertyInfo();

				var ma = expression.Arguments[i];
				var pe = Expression.MakeMemberAccess(path, member);

				if (ma is MemberInitExpression && !into.IsExpression(pe, 1, RequestFor.Field).Result)
				{
					BuildOutput2(
						builder,
						into,
						items,
						ctx,
						(MemberInitExpression)ma, Expression.MakeMemberAccess(path, member));
				}
				else
				{
//					var column = into.ConvertToSql(pe, 1, ConvertFlags.Field);
					var expr   = builder.ConvertToSqlExpression(ctx, ma);

					if (expr.ElementType == QueryElementType.SqlParameter)
					{
//						var parm  = (SqlParameter)expr;
//						var field = column[0].Sql is SqlField
//							? (SqlField)column[0].Sql
//							: (SqlField)((SqlColumn)column[0].Sql).Expression;
//
//						if (parm.DataType == DataType.Undefined)
//							parm.DataType = field.DataType;
					}

					items.Add(expr);
				}
			}
		}
	}
}
