using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(LinqExtensions.Unpivot), nameof(LinqExtensions.UnpivotMulti))]
	sealed class UnpivotBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable
				&& call.Method.DeclaringType == typeof(LinqExtensions)
				&& (string.Equals(call.Method.Name, nameof(LinqExtensions.UnpivotMulti), StringComparison.Ordinal)
					? call.Arguments.Count == 4
					: call.Arguments.Count == 5);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (string.Equals(methodCall.Method.Name, nameof(LinqExtensions.UnpivotMulti), StringComparison.Ordinal))
				return BuildMultiValue(builder, methodCall, buildInfo);

			var info = UnpivotInfo.Parse(methodCall);

			// Resolve the name column through the source's own mapping schema rather than the context's: build it
			// ahead of the lowering for that, with its own SelectQuery so the throwaway build stays out of ours.
			var sourceResult = builder.TryBuildSequence(new BuildInfo(buildInfo, info.Source, new SelectQuery()));

			if (sourceResult.BuildContext == null)
				return sourceResult;

			var lowered = BuildLoweredExpression(info, sourceResult.BuildContext.MappingSchema);

			return BuildSequenceResult.FromContext(builder.BuildSequence(new BuildInfo(buildInfo, lowered)));
		}

		#region Multi-value

		static BuildSequenceResult BuildMultiValue(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var genericArgs    = methodCall.Method.GetGenericArguments();
			var sourceType     = genericArgs[0];
			var resultType     = genericArgs[2];
			var sourceExpr     = methodCall.Arguments[0];
			var resultSelector = methodCall.Arguments[1].UnwrapLambda();
			// Columns arrive flattened group-major alongside the group names - see BuildMultiValueUnpivot.
			var names       = methodCall.Arguments[2].EvaluateExpression<string[]>()!;
			var columnExprs = ((NewArrayExpression)methodCall.Arguments[3]).Expressions;
			var groupSize   = resultSelector.Parameters.Count - 2;

			var groups = new (string name, LambdaExpression[] columns)[names.Length];

			for (var i = 0; i < names.Length; i++)
			{
				var columns = new LambdaExpression[groupSize];

				for (var j = 0; j < groupSize; j++)
					columns[j] = columnExprs[(i * groupSize) + j].UnwrapLambda();

				groups[i] = (names[i], columns);
			}

			// UNION ALL: one projected SELECT per group.
			var selectMethod = Methods.Queryable.Select.MakeGenericMethod(sourceType, resultType);
			var concatMethod = Methods.Queryable.Concat.MakeGenericMethod(resultType);

			Expression? accumulated = null;

			foreach (var (name, columns) in groups)
			{
				var rowParam = Expression.Parameter(sourceType, "row");
				var args     = new Expression[columns.Length + 2];
				args[0] = rowParam;
				args[1] = Expression.Constant(name);
				for (var i = 0; i < columns.Length; i++)
					args[i + 2] = columns[i].GetBody(rowParam);

				var branch = Expression.Call(selectMethod, sourceExpr, Expression.Quote(Expression.Lambda(resultSelector.GetBody(args), rowParam)));
				accumulated = accumulated == null ? branch : Expression.Call(concatMethod, accumulated, branch);
			}

			return BuildSequenceResult.FromContext(builder.BuildSequence(new BuildInfo(buildInfo, accumulated!)));
		}

		#endregion

		#region Portable lowering

		static Expression BuildLoweredExpression(UnpivotInfo info, MappingSchema mappingSchema)
		{
			var selectMethod = Methods.Queryable.Select.MakeGenericMethod(info.SourceType, info.ResultType);
			var whereMethod  = Methods.Queryable.Where .MakeGenericMethod(info.SourceType);
			var concatMethod = Methods.Queryable.Concat.MakeGenericMethod(info.ResultType);

			var excludeNulls = !info.IncludeNulls
				&& (!info.ValueType.IsValueType || Nullable.GetUnderlyingType(info.ValueType) != null);

			Expression? accumulated = null;

			foreach (var column in info.Columns)
			{
				var name     = GetPhysicalColumnName(column, info.SourceType, mappingSchema);
				var rowParam = Expression.Parameter(info.SourceType, "row");

				var projectionBody   = info.ResultSelector.GetBody(rowParam, Expression.Constant(name), column.GetBody(rowParam));
				var projectionLambda = Expression.Lambda(projectionBody, rowParam);

				var branchSource = info.Source;

				if (excludeNulls)
				{
					var whereParam  = Expression.Parameter(info.SourceType, "row");
					var whereBody   = Expression.NotEqual(column.GetBody(whereParam), Expression.Constant(null, info.ValueType));
					var whereLambda = Expression.Lambda(whereBody, whereParam);

					branchSource = Expression.Call(whereMethod, branchSource, Expression.Quote(whereLambda));
				}

				var branch = Expression.Call(selectMethod, branchSource, Expression.Quote(projectionLambda));

				accumulated = accumulated == null ? branch : Expression.Call(concatMethod, accumulated, branch);
			}

			return accumulated!;
		}

		#endregion

		// The name column reports the physical column name, so a query reads the same names whether or not the
		// member was renamed by the mapping.
		static string GetPhysicalColumnName(LambdaExpression column, Type sourceType, MappingSchema mappingSchema)
		{
			var memberName = GetColumnName(column);

			return mappingSchema.GetEntityDescriptor(sourceType)[memberName]?.ColumnName ?? memberName;
		}

		static string GetColumnName(LambdaExpression column)
		{
			var body = column.Body;

			while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
				body = unary.Operand;

			return body is MemberExpression member
				? member.Member.Name
				: throw new LinqToDBException($"{nameof(LinqExtensions.Unpivot)} column selector must be a simple member access (e.g. x => x.Jan).");
		}

		sealed class UnpivotInfo
		{
			public required Expression                      Source         { get; init; }
			public required LambdaExpression                ResultSelector { get; init; }
			public required IReadOnlyList<LambdaExpression> Columns        { get; init; }
			public required bool                            IncludeNulls   { get; init; }
			public required Type                            SourceType     { get; init; }
			public required Type                            ValueType      { get; init; }
			public required Type                            ResultType     { get; init; }

			public static UnpivotInfo Parse(MethodCallExpression methodCall)
			{
				var genericArgs = methodCall.Method.GetGenericArguments();

				var columns = new List<LambdaExpression> { methodCall.Arguments[3].UnwrapLambda() };
				if (methodCall.Arguments[4] is NewArrayExpression array)
					columns.AddRange(array.Expressions.Select(static e => e.UnwrapLambda()));

				return new UnpivotInfo
				{
					Source         = methodCall.Arguments[0],
					IncludeNulls   = (UnpivotNulls)methodCall.Arguments[1].EvaluateExpression()! == UnpivotNulls.IncludeNulls,
					ResultSelector = methodCall.Arguments[2].UnwrapLambda(),
					Columns        = columns,
					SourceType     = genericArgs[0],
					ValueType      = genericArgs[1],
					ResultType     = genericArgs[2],
				};
			}
		}
	}
}
