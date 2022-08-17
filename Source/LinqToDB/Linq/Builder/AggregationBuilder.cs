using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using Mapping;
	using SqlQuery;
	using Reflection;
	using LinqToDB.Common.Internal;
	using LinqToDB.Expressions;

	class AggregationBuilder : MethodCallBuilder
	{
		public  static readonly string[] MethodNames      = { "Average"     , "Min"     , "Max"     , "Sum"      };
		private static readonly string[] MethodNamesAsync = { "AverageAsync", "MinAsync", "MaxAsync", "SumAsync" };

		public static Sql.ExpressionAttribute? GetAggregateDefinition(MethodCallExpression methodCall, MappingSchema mapping)
		{
			var function = methodCall.Method.GetExpressionAttribute(mapping);
			return function != null && (function.IsAggregate || function.IsWindowFunction) ? function : null;
		}

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (methodCall.IsQueryable(MethodNames) || methodCall.IsAsyncExtension(MethodNamesAsync))
				return true;

			return false;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]) { CreateSubQuery = true });

			var prevSequence = sequence;

			// Wrap by subquery to handle aggregate limitations, especially for SQL Server
			//
			sequence = new SubQueryContext(sequence);

			if (prevSequence.SelectQuery.OrderBy.Items.Count > 0)
			{
				if (prevSequence.SelectQuery.Select.TakeValue == null && prevSequence.SelectQuery.Select.SkipValue == null)
					prevSequence.SelectQuery.OrderBy.Items.Clear();
			}

			var context = new AggregationContext(buildInfo.Parent, sequence, methodCall);

			var methodName = methodCall.Method.Name.Replace("Async", "");

			var sql = sequence.ConvertToSql(null, 0, ConvertFlags.Field).Select(_ => _.Sql).ToArray();

			if (sql.Length == 1)
			{
				if (sql[0] is SelectQuery query)
				{
					if (query.Select.Columns.Count == 1)
					{
						var join = query.OuterApply();
						context.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);
						sql[0] = query.Select.Columns[0];
					}
				}
			}

			ISqlExpression sqlExpression = new SqlFunction(methodCall.Type, methodName, true, sql);

			if (sqlExpression == null)
				throw new LinqToDBException("Invalid Aggregate function implementation");

			context.Sql        = context.SelectQuery;
			context.FieldIndex = context.SelectQuery.Select.Add(sqlExpression, methodName);

			return context;
		}

		class AggregationContext : SequenceContextBase
		{
			public AggregationContext(IBuildContext? parent, IBuildContext sequence, MethodCallExpression methodCall)
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
			private  int?       _parentIndex;

			public int            FieldIndex;
			public ISqlExpression Sql = null!;

			static int CheckNullValue(bool isNull, object context)
			{
				if (isNull)
					ThrowHelper.ThrowInvalidOperationException(
						$"Function {context} returns non-nullable value, but result is NULL. Use nullable version of the function instead.");
				return 0;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildExpression(FieldIndex, Sql);
				var mapper = Builder.BuildMapper<object>(expr);

				CompleteColumns();
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

				if (SequenceHelper.UnwrapSubqueryContext(Sequence) is DefaultIfEmptyBuilder.DefaultIfEmptyContext defaultIfEmpty)
				{
					expr = Builder.BuildSql(_returnType, fieldIndex, sqlExpression);
					if (defaultIfEmpty.DefaultValue != null && expr is ConvertFromDataReaderExpression convert)
					{
						var generator = new ExpressionGenerator();
						expr = convert.MakeNullable();
						if (expr.Type.IsNullable())
						{
							var exprVar      = generator.AssignToVariable(expr, "nullable");
							var defaultValue = defaultIfEmpty.DefaultValue;
							if (defaultValue.Type != expr.Type)
							{
								var convertLambda = Builder.MappingSchema.GenerateSafeConvert(defaultValue.Type, expr.Type);
								defaultValue = InternalExtensions.ApplyLambdaToExpression(convertLambda, defaultValue);
							}

							var resultVar = generator.AssignToVariable(defaultValue, "result");

							generator.AddExpression(Expression.IfThen(
								Expression.NotEqual(exprVar, ExpressionInstances.UntypedNull),
								Expression.Assign(resultVar, Expression.Convert(exprVar, resultVar.Type))));

							generator.AddExpression(resultVar);

							expr = generator.Build();
						}
					}
				}
				else
				if (_methodName == "Sum" || _returnType.IsNullableType())
				{
					expr = Builder.BuildSql(_returnType, fieldIndex, sqlExpression);
				}
				else
				{
					expr = Expression.Block(
						Expression.Call(null, MemberHelper.MethodOf(() => CheckNullValue(false, null!)), Expression.Call(ExpressionBuilder.DataReaderParam, Methods.ADONet.IsDBNull, ExpressionInstances.Constant0), Expression.Constant(_methodName)),
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

				return ThrowHelper.ThrowInvalidOperationException<SqlInfo[]>();
			}

			public override int ConvertToParentIndex(int index, IBuildContext context)
			{
				if (index != FieldIndex)
					ThrowHelper.ThrowInvalidOperationException();

				if (_parentIndex != null)
					return _parentIndex.Value;

				if (Parent != null)
				{
					index = Parent.SelectQuery.Select.Add(Sql);
					_parentIndex = Parent.ConvertToParentIndex(index, Parent);
				}
				else
				{
					_parentIndex = index;
				}

				return _parentIndex.Value;
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				switch (flags)
				{
					case ConvertFlags.Field :
						{
							var result = _index ??= new[]
							{
								new SqlInfo(Sql!, SelectQuery, FieldIndex)
							};

							return result;
						}
				}


				return ThrowHelper.ThrowInvalidOperationException<SqlInfo[]>();
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				return requestFlag switch
				{
					RequestFor.Root       => IsExpressionResult.GetResult(Lambda != null && expression == Lambda.Parameters[0]),
					RequestFor.Expression => IsExpressionResult.True,
					_                     => IsExpressionResult.False,
				};
			}

			public override IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				return ThrowHelper.ThrowNotImplementedException<IBuildContext>();
			}
		}
	}
}
