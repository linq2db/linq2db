using System;
using System.Collections.Generic;
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
	[BuildsMethodCall(nameof(LinqExtensions.Unpivot))]
	sealed class UnpivotBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable
				&& call.Method.DeclaringType == typeof(LinqExtensions)
				&& call.Arguments.Count == 5;

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
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
