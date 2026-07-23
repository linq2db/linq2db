using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
					? call.Arguments.Count == 3
					: call.Arguments.Count == 5);

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (string.Equals(methodCall.Method.Name, nameof(LinqExtensions.UnpivotMulti), StringComparison.Ordinal))
				return BuildMultiValue(builder, methodCall, buildInfo);

			var info = UnpivotInfo.Parse(methodCall);

			// Native UNPIVOT keyword where the provider supports it and INCLUDE NULLS matches native default;
			// otherwise portable UNION ALL lowering (works everywhere).
			if (builder.DataContext.SqlProviderFlags.IsUnpivotSupported && !info.IncludeNulls)
			{
				var native = TryBuildNative(builder, methodCall, buildInfo, info);
				if (native != null)
					return native.Value;
			}

			return BuildSequenceResult.FromContext(builder.BuildSequence(new BuildInfo(buildInfo, BuildLoweredExpression(info))));
		}

		#region Multi-value

		static BuildSequenceResult BuildMultiValue(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var genericArgs    = methodCall.Method.GetGenericArguments();
			var sourceType     = genericArgs[0];
			var valueType      = genericArgs[1];
			var resultType     = genericArgs[2];
			var sourceExpr     = methodCall.Arguments[0];
			var resultSelector = methodCall.Arguments[1].UnwrapLambda();
			var groups         = ((string name, LambdaExpression[] columns)[])methodCall.Arguments[2].EvaluateExpression()!;

			if (builder.DataContext.SqlProviderFlags.IsMultiValueUnpivotSupported)
			{
				var native = TryBuildMultiValueNative(builder, buildInfo, sourceType, valueType, resultSelector, groups);
				if (native != null)
					return native.Value;
			}

			// Portable UNION ALL fallback: one projected SELECT per group.
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

		static BuildSequenceResult? TryBuildMultiValueNative(ExpressionBuilder builder, BuildInfo buildInfo, Type sourceType, Type valueType, LambdaExpression resultSelector, (string name, LambdaExpression[] columns)[] groups)
		{
			var sourceExpr    = ((MethodCallExpression)buildInfo.Expression).Arguments[0];
			var sourceContext = builder.BuildSequence(new BuildInfo(buildInfo, sourceExpr, new SelectQuery()));
			var tableContext  = SequenceHelper.GetTableContext(sourceContext);
			if (tableContext?.SqlTable is not { } sourceTable)
				return null;

			var valueCount = groups[0].columns.Length;
			var node       = new SqlUnpivotTable(sourceTable, includeNulls: false);

			var nameField = new SqlField(builder.MappingSchema.GetDbDataType(typeof(string)), "Name", false) { Table = node };
			node.NameField = nameField;
			node.OutputFields.Add(nameField);

			var valueDbType = builder.MappingSchema.GetDbDataType(valueType);
			var valueFields = new SqlField[valueCount];
			for (var k = 0; k < valueCount; k++)
			{
				var vf = new SqlField(valueDbType, "Value" + (k + 1).ToString(CultureInfo.InvariantCulture), true) { Table = node };
				valueFields[k] = vf;
				node.ValueFields.Add(vf);
				node.OutputFields.Add(vf);
			}

			foreach (var (name, columns) in groups)
			{
				if (columns.Length != valueCount)
					return null;

				var cols = new ISqlExpression[columns.Length];
				for (var i = 0; i < columns.Length; i++)
				{
					var colName = GetColumnName(columns[i]);
					var field   = sourceTable.Fields.Find(f => string.Equals(f.Name, colName, StringComparison.Ordinal));
					if (field == null)
						return null;
					cols[i] = field;
				}

				node.Items.Add(new SqlUnpivotItem(name, cols));
			}

			var selectQuery = buildInfo.SelectQuery;
			selectQuery.From.Table(node);

			var contexts = new IBuildContext[valueCount + 2];
			contexts[0] = new UnpivotRowContext(builder.GetTranslationModifier(), builder, sourceType, selectQuery, node);
			contexts[1] = new SingleExpressionContext(builder.GetTranslationModifier(), builder, nameField, selectQuery);
			for (var k = 0; k < valueCount; k++)
				contexts[k + 2] = new SingleExpressionContext(builder.GetTranslationModifier(), builder, valueFields[k], selectQuery);

			var body       = SequenceHelper.PrepareBody(resultSelector, contexts);
			var projection = new SelectContext(builder.GetTranslationModifier(), buildInfo.Parent, builder, null, body, selectQuery, buildInfo.IsSubQuery);

			return BuildSequenceResult.FromContext(projection);
		}

		#endregion

		#region Native

		static BuildSequenceResult? TryBuildNative(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, UnpivotInfo info)
		{
			var sourceContext = builder.BuildSequence(new BuildInfo(buildInfo, info.Source, new SelectQuery()));
			var tableContext  = SequenceHelper.GetTableContext(sourceContext);

			// For now the native path handles a plain table source (the common case); anything else lowers.
			if (tableContext?.SqlTable is not { } sourceTable)
				return null;

			var node = new SqlUnpivotTable(sourceTable, includeNulls: false);

			var valueDbType = builder.MappingSchema.GetDbDataType(info.ValueType);
			var valueField  = new SqlField(valueDbType, "Value", true) { Table = node };
			var nameField   = new SqlField(builder.MappingSchema.GetDbDataType(typeof(string)), "Name", false) { Table = node };

			node.ValueFields.Add(valueField);
			node.OutputFields.Add(valueField);
			node.NameField = nameField;
			node.OutputFields.Add(nameField);

			foreach (var column in info.Columns)
			{
				var name  = GetColumnName(column);
				var field = sourceTable.Fields.Find(f => string.Equals(f.Name, name, StringComparison.Ordinal));

				// Column not resolvable to a physical source field → fall back to the portable lowering.
				if (field == null)
					return null;

				node.Items.Add(new SqlUnpivotItem(name, new ISqlExpression[] { field }));
			}

			var selectQuery = buildInfo.SelectQuery;
			selectQuery.From.Table(node);

			var rowContext   = new UnpivotRowContext(builder.GetTranslationModifier(), builder, info.SourceType, selectQuery, node);
			var nameContext  = new SingleExpressionContext(builder.GetTranslationModifier(), builder, nameField,  selectQuery);
			var valueContext = new SingleExpressionContext(builder.GetTranslationModifier(), builder, valueField, selectQuery);

			var body = SequenceHelper.PrepareBody(info.ResultSelector, rowContext, nameContext, valueContext);

			var projection = new SelectContext(builder.GetTranslationModifier(), buildInfo.Parent, builder, null, body, selectQuery, buildInfo.IsSubQuery);

			return BuildSequenceResult.FromContext(projection);
		}

		/// <summary>Row-parameter context: maps <c>row.Member</c> to a passthrough field on the UNPIVOT node.</summary>
		sealed class UnpivotRowContext : BuildContextBase
		{
			readonly SqlUnpivotTable _node;

			public UnpivotRowContext(TranslationModifier translationModifier, ExpressionBuilder builder, Type elementType, SelectQuery selectQuery, SqlUnpivotTable node)
				: base(translationModifier, builder, elementType, selectQuery)
			{
				_node = node;
			}

			public override MappingSchema MappingSchema => Builder.MappingSchema;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (!(flags.IsSql() || flags.IsExpression()))
					return path;

				if (path is MemberExpression member
					&& member.Expression is ContextRefExpression contextRef
					&& contextRef.BuildContext == this)
				{
					var field = GetOrCreateField(member.Member);
					return ExpressionBuilder.CreatePlaceholder(this, field, path);
				}

				return path;
			}

			SqlField GetOrCreateField(MemberInfo member)
			{
				var existing = _node.OutputFields.Find(f => string.Equals(f.Name, member.Name, StringComparison.Ordinal));
				if (existing != null)
					return existing;

				var memberType = member is PropertyInfo pi ? pi.PropertyType : ((FieldInfo)member).FieldType;
				var canBeNull  = !memberType.IsValueType || Nullable.GetUnderlyingType(memberType) != null;

				var field = new SqlField(Builder.MappingSchema.GetDbDataType(memberType), member.Name, canBeNull) { Table = _node };
				_node.OutputFields.Add(field);

				return field;
			}

			public override IBuildContext Clone(CloningContext context)      => throw new NotSupportedException();
			public override void          SetRunQuery<T>(Query<T> query, Expression expr) => throw new NotSupportedException();
			public override SqlStatement  GetResultStatement()              => new SqlSelectStatement(SelectQuery);
		}

		#endregion

		#region Portable lowering

		static Expression BuildLoweredExpression(UnpivotInfo info)
		{
			var selectMethod = Methods.Queryable.Select.MakeGenericMethod(info.SourceType, info.ResultType);
			var whereMethod  = Methods.Queryable.Where .MakeGenericMethod(info.SourceType);
			var concatMethod = Methods.Queryable.Concat.MakeGenericMethod(info.ResultType);

			var excludeNulls = !info.IncludeNulls
				&& (!info.ValueType.IsValueType || Nullable.GetUnderlyingType(info.ValueType) != null);

			Expression? accumulated = null;

			foreach (var column in info.Columns)
			{
				var name     = GetColumnName(column);
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

		static string GetColumnName(LambdaExpression column)
		{
			var body = column.Body;

			while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
				body = unary.Operand;

			return body is MemberExpression member
				? member.Member.Name
				: throw new LinqToDBException("Unpivot column selector must be a simple member access (e.g. x => x.Jan).");
		}

		sealed class UnpivotInfo
		{
			public required Expression                     Source         { get; init; }
			public required LambdaExpression               ResultSelector { get; init; }
			public required IReadOnlyList<LambdaExpression> Columns        { get; init; }
			public required bool                           IncludeNulls   { get; init; }
			public required Type                           SourceType     { get; init; }
			public required Type                           ValueType      { get; init; }
			public required Type                           ResultType     { get; init; }

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
