﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Common.Internal;
	using LinqToDB.Expressions;
	using Extensions;

	sealed class MethodChainBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var functions = Sql.ExtensionAttribute.GetExtensionAttributes(methodCall, builder.MappingSchema);
			return functions.Length > 0;
		}

		protected override IBuildContext? BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
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

			if (builder.CanBeCompiled(root, false))
				return null;

			var prevSequence = builder.TryBuildSequence(new BuildInfo(buildInfo, root) { CreateSubQuery = true });
			if (prevSequence == null)
				return null;

			var finalFunction = functions.First();
			var sequence      = prevSequence;

			if (finalFunction.IsAggregate)
			{
				// Wrap by subquery to handle aggregate limitations, especially for SQL Server
				//
				sequence = new SubQueryContext(sequence);
			}

			var context = new ChainContext(buildInfo.Parent, sequence, methodCall);

			var sqlExpression = finalFunction.GetExpression((builder, context, flags: buildInfo.GetFlags()), builder.DataContext, context.SelectQuery, methodCall,
				static (ctx, e, descriptor) => ctx.builder.ConvertToExtensionSql(ctx.context, ctx.flags, e, descriptor));

			context.Placeholder = ExpressionBuilder.CreatePlaceholder(context, sqlExpression, methodCall, alias: methodCall.Method.Name);

			return context;
		}

		internal sealed class ChainContext : SequenceContextBase
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

			readonly string _methodName;
			readonly Type   _returnType;

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

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<object>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Root))
					return path;

				if (flags.IsAggregationRoot() || flags.IsAssociationRoot())
				{
					var corrected = SequenceHelper.CorrectExpression(path, this, Sequence);
					return corrected;
				}

				// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
				if (Placeholder == null)
					return path;

				Expression result = Placeholder;

				if (!_returnType.IsNullableType() && flags.IsExpression())
				{
					result = Expression.Block(
						Expression.Call(null, MemberHelper.MethodOf(() => CheckNullValue(false, null!)),
							new SqlReaderIsNullExpression(Placeholder, false), Expression.Constant(_methodName)),
						Placeholder);
				}

				return result;
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
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
