using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using Mapping;
	using SqlQuery;

	class AggregationBuilder : MethodCallBuilder
	{
		public static string[] MethodNames = { "Average", "Min", "Max", "Sum" };

		public static Sql.ExpressionAttribute? GetAggregateDefinition(MethodCallExpression methodCall, MappingSchema mapping)
		{
			var functions = mapping.GetAttributes<Sql.ExpressionAttribute>(methodCall.Method.ReflectedType!,
				methodCall.Method,
				f => f.Configuration);
			return functions.FirstOrDefault(f => f.IsAggregate);
		}

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (methodCall.IsQueryable(MethodNames) || methodCall.IsAsyncExtension(MethodNames))
				return true;

			return false;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]) { CreateSubQuery = true });

			if (sequence.SelectQuery.Select.IsDistinct        ||
			    sequence.SelectQuery.Select.TakeValue != null ||
			    sequence.SelectQuery.Select.SkipValue != null ||
			   !sequence.SelectQuery.GroupBy.IsEmpty)
			{
				sequence = new SubQueryContext(sequence);
			}

			if (sequence.SelectQuery.OrderBy.Items.Count > 0)
			{
				if (sequence.SelectQuery.Select.TakeValue == null && sequence.SelectQuery.Select.SkipValue == null)
					sequence.SelectQuery.OrderBy.Items.Clear();
				else
					sequence = new SubQueryContext(sequence);
			}

			var context = new AggregationContext(buildInfo.Parent, sequence, methodCall);

			var methodName = methodCall.Method.Name.Replace("Async", "");

			var sql = sequence.ConvertToSql(null, 0, ConvertFlags.Field).Select(_ => _.Sql).ToArray();

			if (sql.Length == 1 && sql[0] is SelectQuery query)
			{
				if (query.Select.Columns.Count == 1)
				{
					var join = query.OuterApply();
					context.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);
					sql[0] = query.Select.Columns[0];
				}
			}

			ISqlExpression sqlExpression = new SqlFunction(methodCall.Type, methodName, true, sql);

			if (sqlExpression == null)
				throw new LinqToDBException("Invalid Aggregate function implementation");

			context.Sql        = context.SelectQuery;
			context.FieldIndex = context.SelectQuery.Select.Add(sqlExpression, methodName);

			return context;
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
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

			public int             FieldIndex;
			public ISqlExpression? Sql;

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
				var expr = Builder.BuildSql(_returnType, fieldIndex, sqlExpression);
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
				switch (flags)
				{
					case ConvertFlags.Field :
						{
							var result = _index ??= new[]
							{
								new SqlInfo(Sql!, Parent!.SelectQuery, Parent.SelectQuery.Select.Add(Sql!))
							};

							return result;
						}
				}


				throw new InvalidOperationException();
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
