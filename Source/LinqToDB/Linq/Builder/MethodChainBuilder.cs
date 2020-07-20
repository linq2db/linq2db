using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

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

			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, root) { CreateSubQuery = true });

			var finalFunction = functions.First();
				
			var sqlExpression = finalFunction.GetExpression(builder.DataContext, buildInfo.SelectQuery, methodCall,
				(e, descriptor) => builder.ConvertToExtensionSql(sequence, e, descriptor));

			var context = new ChainContext(buildInfo.Parent, sequence, methodCall);
			context.Sql        = context.SelectQuery;
			context.FieldIndex = context.SelectQuery.Select.Add(sqlExpression, methodCall.Method.Name);

			return context;
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		class ChainContext : SequenceContextBase
		{
			public ChainContext(IBuildContext? parent, IBuildContext sequence, MethodCallExpression methodCall)
				: base(parent, sequence, null)
			{
				_returnType = methodCall.Method.ReturnType;
				_methodName = methodCall.Method.Name;

				if (_returnType.IsGenericType && _returnType.GetGenericTypeDefinition() == typeof(Task<>))
				{
					_returnType = _returnType.GetGenericArguments()[0];
					_methodName = _methodName.Replace("Async", "");
				}
			}

			readonly string     _methodName;
			readonly Type       _returnType;
			private  SqlInfo[]? _index;

			public int             FieldIndex;
			public ISqlExpression? Sql;

			static int CheckNullValue(IDataRecord reader, object context)
			{
				if (reader.IsDBNull(0))
					throw new InvalidOperationException(
						$"Function {context} returns non-nullable value, but result is NULL. Use nullable version of the function instead.");
				return 0;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildExpression(FieldIndex, Sql);
				var mapper = Builder.BuildMapper<object>(expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				var info  = ConvertToIndex(expression, level, ConvertFlags.Field)[0];
				var index = info.Index;
				if (Parent != null)
					index = ConvertToParentIndex(index, Parent);
				return BuildExpression(index, info.Sql);
			}

			Expression BuildExpression(int fieldIndex, ISqlExpression? sqlExpression)
			{
				Expression expr;

				if (_returnType.IsClass || _returnType.IsNullable())
				{
					expr = Builder.BuildSql(_returnType, fieldIndex, sqlExpression);
				}
				else
				{
					expr = Expression.Block(
						Expression.Call(null, MemberHelper.MethodOf(() => CheckNullValue(null!, null!)), ExpressionBuilder.DataReaderParam, Expression.Constant(_methodName)),
						Builder.BuildSql(_returnType, fieldIndex, sqlExpression));
				}

				return expr;
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				switch (flags)
				{
					case ConvertFlags.All   :
					case ConvertFlags.Key   :
					case ConvertFlags.Field : return Sequence.ConvertToSql(expression, level + 1, flags);
				}

				throw new InvalidOperationException();
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				return flags switch
				{
					ConvertFlags.Field =>
						_index ??= new[]
						{
							new SqlInfo(Sql!, Parent!.SelectQuery, Parent.SelectQuery.Select.Add(Sql!))
						},
					_ => throw new InvalidOperationException(),
				};
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				return requestFlag switch
				{
					RequestFor.Root       => new IsExpressionResult(Lambda != null && expression == Lambda.Parameters[0]),
					RequestFor.Expression => IsExpressionResult.True,
					_                     => IsExpressionResult.False,
				};
			}

			public override IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				throw new NotImplementedException();
			}
		}

	}
}
