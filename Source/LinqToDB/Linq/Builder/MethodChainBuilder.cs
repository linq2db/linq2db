using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
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
			return functions.Any(f => f.ChainPrecedence >= 0);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var functions = Sql.ExtensionAttribute.GetExtensionAttributes(methodCall, builder.MappingSchema);

			var chain = Sql.ExtensionAttribute.BuildFunctionsChain(builder.MappingSchema, methodCall);
			IBuildContext sequence = null;

			foreach (var expression in chain)
			{
				if (expression is MethodCallExpression mc)
				{
					if (mc.Arguments.Count > 0)
					{
						if (typeof(IEnumerable<>).IsSameOrParentOf(mc.Arguments[0].Type))
							sequence = builder.BuildSequence(new BuildInfo(buildInfo, mc.Arguments[0]));
						else if (typeof(Sql.IQueryableContainer).IsSameOrParentOf(mc.Arguments[0].Type))
							sequence = builder.BuildSequence(new BuildInfo(buildInfo,
								((Sql.IQueryableContainer)mc.Arguments[0].EvaluateExpression()).Query.Expression));
					}
				}
			}

			if (sequence == null)
				throw new LinqToDBException($"{methodCall} can not be converted to SQL.");

			var finalFunction = functions.FirstOrDefault(f => f.ChainPrecedence == 0);
			if (finalFunction == null)
				finalFunction = functions.FirstOrDefault(f => f.ChainPrecedence < 0);
			if (finalFunction == null)
			{
				// it means that function is just sequence provider
				return sequence;
			}
				
			var sqlExpression = finalFunction.GetExpression(builder.MappingSchema, buildInfo.SelectQuery, methodCall,
				e => builder.ConvertToExtensionSql(sequence, e));

			var context = new ChainContext(buildInfo.Parent, sequence, methodCall);
			context.Sql        = context.SelectQuery;
			context.FieldIndex = context.SelectQuery.Select.Add(sqlExpression, methodCall.Method.Name);

			return context;
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		class ChainContext : SequenceContextBase
		{
			public ChainContext(IBuildContext parent, IBuildContext sequence, MethodCallExpression methodCall)
				: base(parent, sequence, null)
			{
				_returnType = methodCall.Method.ReturnType;
				_methodName = methodCall.Method.Name;

				if (_returnType.IsGenericTypeEx() && _returnType.GetGenericTypeDefinition() == typeof(Task<>))
				{
					_returnType = _returnType.GetGenericArgumentsEx()[0];
					_methodName = _methodName.Replace("Async", "");
				}
			}

			readonly string    _methodName;
			readonly Type      _returnType;
			private  SqlInfo[] _index;

			public int            FieldIndex;
			public ISqlExpression Sql;

			static int CheckNullValue(IDataRecord reader, object context)
			{
				if (reader.IsDBNull(0))
					throw new InvalidOperationException(
						$"Function {context} returns non-nullable value, but result is NULL. Use nullable version of the function instead.");
				return 0;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildExpression(FieldIndex);
				var mapper = Builder.BuildMapper<object>(expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
			{
				var index = ConvertToIndex(expression, level, ConvertFlags.Field)[0].Index;
				if (Parent != null)
					index = ConvertToParentIndex(index, Parent);
				return BuildExpression(index);
			}

			Expression BuildExpression(int fieldIndex)
			{
				Expression expr;

				if (_returnType.IsClassEx() || _returnType.IsNullable())
				{
					expr = Builder.BuildSql(_returnType, fieldIndex);
				}
				else
				{
					expr = Expression.Block(
						Expression.Call(null, MemberHelper.MethodOf(() => CheckNullValue(null, null)), ExpressionBuilder.DataReaderParam, Expression.Constant(_methodName)),
						Builder.BuildSql(_returnType, fieldIndex));
				}

				return expr;
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				switch (flags)
				{
					case ConvertFlags.All   :
					case ConvertFlags.Key   :
					case ConvertFlags.Field : return Sequence.ConvertToSql(expression, level + 1, flags);
				}

				throw new InvalidOperationException();
			}

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				switch (flags)
				{
					case ConvertFlags.Field :
						return _index ?? (_index = new[]
						{
							new SqlInfo { Query = Parent.SelectQuery, Index = Parent.SelectQuery.Select.Add(Sql), Sql = Sql, }
						});
				}

				throw new InvalidOperationException();
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				switch (requestFlag)
				{
					case RequestFor.Root       : return new IsExpressionResult(Lambda != null && expression == Lambda.Parameters[0]);
					case RequestFor.Expression : return IsExpressionResult.True;
				}

				return IsExpressionResult.False;
			}

			public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				throw new NotImplementedException();
			}
		}

	}
}
