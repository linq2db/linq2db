using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Common.Internal;
	using LinqToDB.Expressions;
	using Extensions;

	class MethodChainBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var functions = Sql.ExtensionAttribute.GetExtensionAttributes(methodCall, builder.MappingSchema);
			return functions.Any();
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var functions = Sql.ExtensionAttribute.GetExtensionAttributes(methodCall, builder.MappingSchema);

			var root = methodCall.SkipMethodChain(builder.MappingSchema);

			// evaluating IQueryableContainer
			while (root.NodeType == ExpressionType.Constant && typeof(Sql.IQueryableContainer).IsSameOrParentOf(root.Type))
			{
				root = ((Sql.IQueryableContainer)root.EvaluateExpression()!).Query.Expression;
				root = root.SkipMethodChain(builder.MappingSchema);
			}

			root = builder.ConvertExpressionTree(root);

			var prevSequence  = builder.BuildSequence(new BuildInfo(buildInfo, root) { CreateSubQuery = true });
			var finalFunction = functions.First();
			var sequence      = prevSequence;

			if (finalFunction.IsAggregate)
			{
				// Wrap by subquery to handle aggregate limitations, especially for SQL Server
				//
				sequence = new SubQueryContext(sequence);
			}

			var context = new ChainContext(buildInfo.Parent, sequence, methodCall);

			var sqlExpression = finalFunction.GetExpression((builder, context), builder.DataContext, context.SelectQuery, methodCall,
				static (ctx, e, descriptor) => ctx.builder.ConvertToExtensionSql(ctx.context, e, descriptor));

			context.Placeholder = ExpressionBuilder.CreatePlaceholder(context, sqlExpression, methodCall, alias: methodCall.Method.Name);

			return context;
		}

		internal class ChainContext : SequenceContextBase
		{
			public ChainContext(IBuildContext? parent, IBuildContext sequence, MethodCallExpression methodCall)
				: base(parent, sequence, null)
			{
				MethodCall = methodCall;
				_returnType     = methodCall.Method.ReturnType;
				_methodName     = methodCall.Method.Name;

				if (_returnType.IsGenericType && _returnType.GetGenericTypeDefinition() == typeof(Task<>))
				{
					_returnType = _returnType.GetGenericArguments()[0];
					_methodName = _methodName.Replace("Async", "");
				}
			}

			readonly string     _methodName;
			readonly Type       _returnType;
			private  SqlInfo[]? _index;
			private  int?       _parentIndex;

			public SqlPlaceholderExpression Placeholder = null!;
			public MethodCallExpression     MethodCall { get; }

			// ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
			static int CheckNullValue(bool isNull, object context)
			{
				if (isNull)
					throw new InvalidOperationException(
						$"Function {context} returns non-nullable value, but result is NULL. Use nullable version of the function instead.");
				return 0;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				throw new NotImplementedException();
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var builtExpr = BuildExpression();
				var mapper    = Builder.BuildMapper<object>(builtExpr);

				CompleteColumns();
				QueryRunner.SetRunQuery(query, mapper);
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			Expression BuildExpression()
			{
				Expression expr;

				if (_returnType.IsNullableType())
				{
					expr = Placeholder;
				}
				else
				{
					expr = Expression.Block(
						Expression.Call(null, MemberHelper.MethodOf(() => CheckNullValue(false, null!)),
							new SqlReaderIsNullExpression(Placeholder, false), Expression.Constant(_methodName)),
						Placeholder);
				}

				return expr;
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override int ConvertToParentIndex(int index, IBuildContext context)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				return null;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new ChainContext(null, context.CloneContext(Sequence), context.CloneExpression(MethodCall))
				{
					Placeholder = context.CloneExpression(Placeholder)
				};
			}
		}

	}
}
