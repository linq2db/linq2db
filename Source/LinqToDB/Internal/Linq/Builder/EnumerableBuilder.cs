using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(LinqExtensions.AsQueryable))]
	[BuildsExpression(ExpressionType.Constant, ExpressionType.Call, ExpressionType.MemberAccess, ExpressionType.NewArrayInit)]
	sealed class EnumerableBuilder : ISequenceBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsSameGenericMethod(Methods.LinqToDB.AsQueryableConfigured);

		public static bool CanBuild(Expression expr, ExpressionBuilder builder)
		{
			if (expr.NodeType == ExpressionType.NewArrayInit)
				return true;

			if (!typeof(IEnumerable<>).IsSameOrParentOf(expr.Type))
				return false;

			if (typeof(IEnumerable<>).GetGenericType(expr.Type) is null)
				return false;

			return expr.NodeType switch
			{
				ExpressionType.MemberAccess => CanBuildMemberChain(((MemberExpression)expr).Expression),
				ExpressionType.Constant     => ((ConstantExpression)expr).Value is IEnumerable,
				ExpressionType.Call         => builder.CanBeEvaluatedOnClient(expr),
				_ => false,
			};

			static bool CanBuildMemberChain(Expression? expr)
			{
				while (expr is { NodeType: ExpressionType.MemberAccess })
					expr = ((MemberExpression)expr).Expression;

				return expr is null or { NodeType: ExpressionType.Constant };
			}
		}

		public BuildSequenceResult BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			// Configured 3-arg form: source.AsQueryable(dataContext, configure).
			if (buildInfo.Expression is MethodCallExpression mc && mc.IsSameGenericMethod(Methods.LinqToDB.AsQueryableConfigured))
				return BuildConfigured(builder, mc, buildInfo);

			// Inline-rows source not renderable by the provider — every emission path (native VALUES
			// or SELECT…UNION ALL with optional FROM <FakeTable>) produces SQL the provider would
			// reject at execute time. Fail at build time with a clear message rather than letting the
			// provider surface a cryptic ODBC / parser error.
			if (!builder.DataContext.SqlProviderFlags.IsInlineRowsSourceSupported)
				return BuildSequenceResult.Error(buildInfo.Expression, ErrorHelper.Error_AsQueryable_InlineRowsSourceNotSupported);

			var collectionType = typeof(IEnumerable<>).GetGenericType(buildInfo.Expression.Type) ??
			                     throw new InvalidOperationException();

			if (buildInfo.Expression is NewArrayExpression)
			{
				if (buildInfo.Parent == null)
					return BuildSequenceResult.Error(buildInfo.Expression);

				var expressions = ((NewArrayExpression)buildInfo.Expression).Expressions.Select(e =>
						builder.UpdateNesting(buildInfo.Parent!, builder.BuildSqlExpression(buildInfo.Parent, e)))
					.ToArray();

				var dynamicContext = new EnumerableContextDynamic(
					builder.GetTranslationModifier(),
					buildInfo.Parent,
					builder,
					expressions,
					buildInfo.SelectQuery,
					collectionType.GetGenericArguments()[0]);

				return BuildSequenceResult.FromContext(dynamicContext);
			}

			if (builder.CanBeEvaluatedOnClient(buildInfo.Expression))
			{
				var param = builder.ParametersContext.BuildParameter(buildInfo.Parent, buildInfo.Expression, null,
					buildParameterType : ParametersContext.BuildParameterType.InPredicate);

				if (param != null)
				{
					var elementType       = collectionType.GetGenericArguments()[0];
					var enumerableContext = new EnumerableContext(builder.GetTranslationModifier(), builder, param, buildInfo.SelectQuery, elementType);

					// 2-arg AsQueryable(db) has no per-call configure lambda — but the DataOptions
					// default still applies. Per-property merge isn't needed (nothing to merge with);
					// just stamp the DataOptions LocalCollections spec directly when present.
					ApplyDataOptionsTempTableDefault(builder, enumerableContext, buildInfo.Expression, elementType, perCallSpec: null);

					return BuildSequenceResult.FromContext(enumerableContext);
				}
			}

			return BuildSequenceResult.Error(buildInfo.Expression);
		}

		/// <summary>
		/// Resolves the final <see cref="LinqToDB.TempTableSpec"/> by merging the (optional)
		/// per-call spec with the DataOptions LocalCollections default, then stamps it onto the
		/// <see cref="LinqToDB.Internal.SqlQuery.SqlValuesTable"/> if the resolved threshold is set
		/// and the provider supports runtime temp tables. Per-property merge: per-call wins for
		/// any field it set, DataOptions fills the rest. Used by both the 2-arg and 3-arg
		/// AsQueryable build paths.
		/// </summary>
		static void ApplyDataOptionsTempTableDefault(
			ExpressionBuilder  builder,
			EnumerableContext  enumerableContext,
			Expression         keyExpression,
			Type               elementType,
			TempTableSpec?     perCallSpec)
		{
			var dataOptionsSpec = builder.DataContext.Options.Find<TempTableOptions>()?.LocalCollections;
			var finalSpec       = MergeTempTableSpecs(perCallSpec, dataOptionsSpec);

			if (finalSpec is { Threshold: not null } && builder.DataContext.SqlProviderFlags.IsRuntimeTempTableCreationSupported)
			{
				enumerableContext.Table.TempTableSpec        = finalSpec;
				enumerableContext.Table.TempTableElementType = elementType;
				enumerableContext.Table.TempTableName        = builder.GetOrAssignTempTableName(keyExpression);
			}
		}

		/// <summary>
		/// Per-property merge: per-call wins where it set a field, DataOptions falls back for the
		/// rest. <see cref="TempTableSpec.DisposeWithConnection"/> uses logical-OR (either source
		/// requesting it switches the lifetime mode).
		/// </summary>
		static TempTableSpec? MergeTempTableSpecs(TempTableSpec? perCall, TempTableSpec? dataOptions)
		{
			if (perCall     is null) return dataOptions;
			if (dataOptions is null) return perCall;

			return new TempTableSpec(
				Threshold:             perCall.Threshold             ?? dataOptions.Threshold,
				DisposeWithConnection: perCall.DisposeWithConnection || dataOptions.DisposeWithConnection,
				BulkCopyOptions:       perCall.BulkCopyOptions       ?? dataOptions.BulkCopyOptions);
		}

		/// <summary>
		/// Builds the single-column <c>SELECT item FROM &lt;values-table&gt;</c> sub-query
		/// used as the <see cref="SqlPredicate.InList.TempTableSubQuery"/> companion for the
		/// <c>UseTempTablesForContains</c> rewrite. The wrapped <see cref="SqlValuesTable"/>
		/// carries <see cref="SqlValuesTable.TempTableSpec"/> /
		/// <see cref="SqlValuesTable.TempTableElementType"/> /
		/// <see cref="SqlValuesTable.TempTableName"/> stamped in the same order as
		/// <see cref="ApplyDataOptionsTempTableDefault"/>. The single field is named
		/// <c>"item"</c> to match <see cref="LinqToDB.Internal.Common.ValueHolder{T}"/>'s
		/// <c>[Column("item")]</c> attribute so the execute-time run-step's BULK-inserted
		/// temp-table column lines up with the SQL builder's emission. Self-join sibling
		/// Contains predicates against the same captured local collection share a name via
		/// <see cref="ExpressionBuilder.GetOrAssignTempTableName"/>, collapsing to one
		/// run-step.
		/// </summary>
		internal static SelectQuery BuildScalarValuesTableForContains(
			ExpressionBuilder builder,
			ISqlExpression    source,
			Type              elementType,
			Expression        keyExpression,
			TempTableSpec     spec)
		{
			var valuesTable = new SqlValuesTable(source);

			var dbDataType = builder.MappingSchema.GetDbDataType(elementType);
			var canBeNull  = elementType.IsNullableOrReferenceType;
			var field      = new SqlField(dbDataType, "item", canBeNull);

			valuesTable.AddFieldWithValueBuilder(field, rawItem => new SqlValue(dbDataType, rawItem));

			valuesTable.TempTableSpec        = spec;
			valuesTable.TempTableElementType = elementType;
			valuesTable.TempTableName        = builder.GetOrAssignTempTableName(keyExpression);

			var subQuery = new SelectQuery();
			subQuery.From.Table(valuesTable);
			subQuery.Select.AddNew(field);

			return subQuery;
		}

		/// <summary>
		/// Builds the multi-column <c>SELECT &lt;cols&gt; FROM &lt;values-table&gt;</c> sub-query
		/// used as the <see cref="SqlPredicate.InList.TempTableSubQuery"/> companion for
		/// entity / composite-PK <c>UseTempTablesForContains</c> rewrites.
		/// <c>SqlExpressionConvertVisitor</c> later synthesises an <c>EXISTS</c> sub-query
		/// from this companion when the run-step's threshold check picks <c>UseTempTable</c>.
		/// <para>
		/// Field names mirror the entity's actual <see cref="ColumnDescriptor.ColumnName"/>
		/// values — the run-step BULK-inserts entity instances directly into a
		/// <c>TempTable&lt;TEntity&gt;</c>, so the SQL builder's
		/// <c>WHERE t.[EmployeeID] = outer.[EmployeeID]</c> emission lines up with the columns
		/// <see cref="LinqToDB.ITable{T}"/>'s CREATE TABLE materialised. User-defined
		/// <see cref="ColumnDescriptor.ValueConverter"/> / <see cref="ColumnDescriptor.DataType"/>
		/// overrides therefore propagate from the entity type into both the inline-VALUES
		/// fallback (via <c>GetSqlValueFromObject</c>) and the temp-table path (via the entity's
		/// <c>EntityDescriptor</c> driving BulkCopy + DDL) — strict-typing providers
		/// (PostgreSQL) see consistent column types on both sides of the comparison.
		/// </para>
		/// </summary>
		internal static SelectQuery? BuildMultiColumnValuesTableForContains(
			ExpressionBuilder               builder,
			ISqlExpression                  source,
			Type                            elementType,
			IReadOnlyList<ColumnDescriptor> keyColumns,
			Expression                      keyExpression,
			TempTableSpec                   spec)
		{
			if (keyColumns.Count == 0)
				return null;

			var valuesTable = new SqlValuesTable(source);

			var fields = new SqlField[keyColumns.Count];

			for (var i = 0; i < keyColumns.Count; i++)
			{
				var cd    = keyColumns[i];
				var field = new SqlField(cd.GetDbDataType(true), cd.ColumnName, cd.CanBeNull);

				// Entity column: GetSqlValueFromObject applies any column conversions
				// (e.g. enum → string mapping) for inline-VALUES emission. The temp-table
				// path uses TempTable<TEntity> BulkCopy directly, which goes through the
				// same EntityDescriptor — so converters propagate to both paths.
				valuesTable.AddFieldWithValueBuilder(field, obj => cd.GetSqlValueFromObject(obj));

				fields[i] = field;
			}

			valuesTable.TempTableSpec        = spec;
			valuesTable.TempTableElementType = elementType;
			valuesTable.TempTableName        = builder.GetOrAssignTempTableName(keyExpression);

			var subQuery = new SelectQuery();
			subQuery.From.Table(valuesTable);

			foreach (var f in fields)
				subQuery.Select.AddNew(f);

			return subQuery;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}

		#region Configured 3-arg AsQueryable

		static BuildSequenceResult BuildConfigured(ExpressionBuilder builder, MethodCallExpression mc, BuildInfo buildInfo)
		{
			// Mirror the gate from BuildSequence so the configured overload also fails fast on
			// providers that can't render an inline-rows source.
			if (!builder.DataContext.SqlProviderFlags.IsInlineRowsSourceSupported)
				return BuildSequenceResult.Error(mc, ErrorHelper.Error_AsQueryable_InlineRowsSourceNotSupported);

			var elementType = mc.Method.GetGenericArguments()[0];
			var sourceArg   = mc.Arguments[0];
			var configureArg = mc.Arguments[2];

			// The configured overload expects a materialised IEnumerable<T>. Traverse the source
			// expression first to resolve closures / context refs, then verify it can be evaluated on
			// the client. An inline array that references outer query state (e.g.
			// `from t in db.Person from v in new[] { new Row { Id = t.ID } }.AsQueryable(db, b => b.Parameterize())`)
			// cannot be compiled — reject with a clear error; the user should use the 2-arg
			// AsQueryable(IDataContext) overload, which has EnumerableContextDynamic for per-element
			// expressions.
			var traversedSource = builder.BuildTraverseExpression(sourceArg);
			if (!builder.CanBeEvaluatedOnClient(traversedSource))
				return BuildSequenceResult.Error(mc, "AsQueryable configure: source could not be evaluated on the client; ensure the source is a materialised IEnumerable<T> (use the 2-arg AsQueryable(IDataContext) overload for sources referencing outer query state).");

			var configureLambda = configureArg.UnwrapLambda();
			if (!TryParseConfigure(elementType, configureLambda, out var defaultForceParameter, out var rowParameter, out var excepted, out var perCallTempTableSpec, out var parseError))
				return BuildSequenceResult.Error(mc, parseError);

			var parameterization = new EnumerableParameterizationConfig(defaultForceParameter, rowParameter, excepted, perCallTempTableSpec);

			var param = builder.ParametersContext.BuildParameter(buildInfo.Parent, traversedSource, null,
				buildParameterType: ParametersContext.BuildParameterType.InPredicate);

			if (param == null)
				return BuildSequenceResult.Error(mc);

			var enumerableContext = new EnumerableContext(
				builder.GetTranslationModifier(),
				builder,
				param,
				buildInfo.SelectQuery,
				elementType,
				parameterization);

			// Merge per-call spec with DataOptions LocalCollections default and stamp onto the
			// SqlValuesTable. The provider-flag gate inside ApplyDataOptionsTempTableDefault
			// silently drops the opt-in when the provider doesn't support runtime temp tables
			// (e.g. Oracle's GLOBAL TEMPORARY TABLE requires upfront DDL + CREATE TABLE
			// privilege) — chain falls through to the regular inline-VALUES path. Run-step
			// registration is derived from this metadata lazily by Query.InitQueries (via an
			// AST scan) — keeps the translator focused on building the AST, not on execute-time
			// side effects.
			ApplyDataOptionsTempTableDefault(builder, enumerableContext, traversedSource, elementType, perCallTempTableSpec);

			return BuildSequenceResult.FromContext(enumerableContext);
		}

		static bool TryParseConfigure(
			Type                                 elementType,
			LambdaExpression                     configureLambda,
			out bool                             defaultForceParameter,
			out ParameterExpression?             rowParameter,
			out IReadOnlyList<MemberExpression>? excepted,
			out TempTableSpec?                   tempTableSpec,
			out string                           error)
		{
			// Initial value is unreachable in practice — the interface design forces every chain
			// through Parameterize() or Inline() before Except is available — but we still need
			// a defined value before the loop runs.
			defaultForceParameter = true;
			rowParameter          = null;
			excepted              = null;
			tempTableSpec         = null;
			error                 = string.Empty;

			List<MemberExpression>? exceptedList = null;

			var builderParameter = configureLambda.Parameters[0];
			var current          = configureLambda.Body;

			while (current is MethodCallExpression call)
			{
				switch (call.Method.Name)
				{
					case nameof(IAsQueryableBuilder<>.Parameterize):
						defaultForceParameter = true;
						current = call.Object ?? call.Arguments[0];
						break;

					case nameof(IAsQueryableBuilder<>.Inline):
						defaultForceParameter = false;
						current = call.Object ?? call.Arguments[0];
						break;

					case nameof(IAsQueryableExceptBuilder<>.Except):
					{
						var membersArg = call.Arguments[call.Arguments.Count - 1];
						if (membersArg is not NewArrayExpression nae)
						{
							error = "AsQueryable configure: Except(...) argument must be a member-selector array literal.";
							return false;
						}

						exceptedList ??= new List<MemberExpression>();
						rowParameter ??= Expression.Parameter(elementType, "p");

						foreach (var item in nae.Expressions)
						{
							var lambda = item.UnwrapLambda();

							// Substitute the per-selector lambda parameter with our shared rowParameter so
							// every Excepted entry has the same root, then strip the implicit boxing
							// Convert that Expression<Func<T, object?>> adds.
							var rerooted = lambda.GetBody(rowParameter).UnwrapConvert();

							if (rerooted is not MemberExpression memberAccess)
							{
								error = $"AsQueryable configure: Except(...) selector must be a member access on the lambda parameter; got '{lambda.Body}'.";
								return false;
							}

							Expression leaf = memberAccess;
							while (leaf is MemberExpression me)
								leaf = me.Expression!;

							if (!ReferenceEquals(leaf, rowParameter))
							{
								error = $"AsQueryable configure: Except(...) selector must be a member access on the lambda parameter; got '{lambda.Body}'.";
								return false;
							}

							exceptedList.Add(memberAccess);
						}

						current = call.Object ?? call.Arguments[0];
						break;
					}

					case nameof(IAsQueryableExceptBuilder<>.UseTempTable):
					{
						if (tempTableSpec != null)
						{
							error = "AsQueryable configure: UseTempTable(...) appears more than once.";
							return false;
						}

						// Two overloads share the name: UseTempTable(int threshold) and
						// UseTempTable(Func<ITempTableConfigBuilder, ITempTableConfigBuilder>). Discriminate by
						// argument shape.
						var arg = call.Arguments[call.Arguments.Count - 1];

						if (arg is ConstantExpression { Value: int constValue })
						{
							if (constValue < 0)
							{
								error = "AsQueryable configure: UseTempTable(threshold) value must be >= 0.";
								return false;
							}

							tempTableSpec = new TempTableSpec(
								Threshold:             constValue,
								DisposeWithConnection: false,
								BulkCopyOptions:       null);
						}
						else if (arg is LambdaExpression configBuilderLambda)
						{
							// Compile + invoke the inner builder lambda once at LINQ-translation time
							// against a real builder impl, snapshot the accumulated spec. Uses the
							// project-banned-API-safe CompileExpression extension (Common/Compilation.cs).
							// The cost is a one-time compile per query-translation — amortised by the
							// query cache (subsequent executes reuse the cached translation).
							var compiled = (Func<ITempTableConfigBuilder, ITempTableConfigBuilder>)configBuilderLambda.CompileExpression();
							var impl     = new TempTableConfigBuilderImpl();
							compiled(impl);
							tempTableSpec = impl.Build();
						}
						else
						{
							error = "AsQueryable configure: UseTempTable(...) argument must be an int literal or a configure lambda.";
							return false;
						}

						current = call.Object ?? call.Arguments[0];
						break;
					}

					default:
						error = $"AsQueryable configure: unsupported method '{call.Method.Name}' in chain.";
						return false;
				}
			}

			if (current != builderParameter)
			{
				error = "AsQueryable configure: chain root must be the lambda parameter.";
				return false;
			}

			excepted = exceptedList;
			return true;
		}

		#endregion
	}
}
