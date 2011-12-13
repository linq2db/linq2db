using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Data.Linq.Builder
{
	using LinqToDB.Linq;
	using Data.Sql;
	using Reflection;

	class AggregationBuilder : MethodCallBuilder
	{
		public static string[] MethodNames = new[] { "Average", "Min", "Max", "Sum" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(MethodNames);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (sequence.SqlQuery.Select.IsDistinct        ||
			    sequence.SqlQuery.Select.TakeValue != null ||
			    sequence.SqlQuery.Select.SkipValue != null ||
			   !sequence.SqlQuery.GroupBy.IsEmpty)
			{
				sequence = new SubQueryContext(sequence);
			}

			if (sequence.SqlQuery.OrderBy.Items.Count > 0)
			{
				if (sequence.SqlQuery.Select.TakeValue == null && sequence.SqlQuery.Select.SkipValue == null)
					sequence.SqlQuery.OrderBy.Items.Clear();
				else
					sequence = new SubQueryContext(sequence);
			}

			var context = new AggregationContext(buildInfo.Parent, sequence, methodCall);
			var sql     = sequence.ConvertToSql(null, 0, ConvertFlags.Field).Select(_ => _.Sql).ToArray();

			if (sql.Length == 1 && sql[0] is SqlQuery)
			{
				var query = (SqlQuery)sql[0];

				if (query.Select.Columns.Count == 1)
				{
					var join = SqlQuery.OuterApply(query);
					context.SqlQuery.From.Tables[0].Joins.Add(join.JoinedTable);
					sql[0] = query.Select.Columns[0];
				}
			}

			context.Sql        = context.SqlQuery;
			context.FieldIndex = context.SqlQuery.Select.Add(
				new SqlFunction(methodCall.Type, methodCall.Method.Name, sql));

			return context;
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		class AggregationContext : SequenceContextBase
		{
			public AggregationContext(IBuildContext parent, IBuildContext sequence, MethodCallExpression methodCall)
				: base(parent, sequence, null)
			{
				_returnType = methodCall.Method.ReturnType;
				_methodName = methodCall.Method.Name;
			}

			readonly string _methodName;
			readonly Type   _returnType;
			private  SqlInfo[] _index;

			public int            FieldIndex;
			public ISqlExpression Sql;

			static object CheckNullValue(object value, object context)
			{
				if (value == null || value is DBNull)
					throw new InvalidOperationException(string.Format("Function {0} returns non-nullable value, but result is NULL. Use nullable version of the function instead.", context));

				return value;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildExpression(FieldIndex);
				var mapper = Builder.BuildMapper<object>(expr);

				query.SetElementQuery(mapper.Compile());
			}

			public override Expression BuildExpression(Expression expression, int level)
			{
				return BuildExpression(ConvertToIndex(expression, level, ConvertFlags.Field)[0].Index);
			}

			Expression BuildExpression(int fieldIndex)
			{
				Expression expr;

				if (_returnType.IsClass || _methodName == "Sum" || TypeHelper.IsNullableType(_returnType))
				{
					expr = Builder.BuildSql(_returnType, fieldIndex);
				}
				else
				{
					expr = Builder.BuildSql(
						_returnType,
						fieldIndex, 
						ReflectionHelper.Expressor<object>.MethodExpressor(o => CheckNullValue(o, o)),
						Expression.Constant(_methodName));
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

				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				switch (flags)
				{
					case ConvertFlags.Field :
						return _index ?? (_index = new[]
						{
							new SqlInfo { Query = Parent.SqlQuery, Index = Parent.SqlQuery.Select.Add(Sql), Sql = Sql, }
						});
				}

				throw new NotImplementedException();
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
