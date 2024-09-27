using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq.Builder
{
	using Async;
	using Common;
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;

	[BuildsMethodCall("GroupBy")]
	sealed class GroupByBuilder : MethodCallBuilder
	{
		static readonly MethodInfo[] GroupingSetMethods = { Methods.LinqToDB.GroupBy.Rollup, Methods.LinqToDB.GroupBy.Cube, Methods.LinqToDB.GroupBy.GroupingSets };

		#region Builder Methods

		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
		{
			if (!call.IsQueryable())
				return false;

			var body = ((LambdaExpression)call.Arguments[1].Unwrap()).Body.Unwrap();
			if (body.NodeType == ExpressionType.MemberInit)
			{
				var mi = (MemberInitExpression)body;
				if (mi.NewExpression.Arguments.Count > 0 || 
					mi.Bindings.Count == 0 ||
					mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment))
				{
					throw new NotSupportedException($"Explicit construction of entity type '{body.Type}' in group by is not allowed.");
				}
			}

			return (call.Arguments[call.Arguments.Count - 1].Unwrap().NodeType == ExpressionType.Lambda);
		}

		static IEnumerable<Expression> EnumGroupingSets(Expression expression)
		{
			if (expression is NewExpression newExpression)
			{
				foreach (var arg in newExpression.Arguments)
				{
					yield return arg;
				}
			}
			else if (expression is SqlGenericConstructorExpression generic)
			{
				foreach (var arg in generic.Assignments)
				{
					yield return arg.Expression;
				}
			}
		}

		/*

		-- describing subqueries when building GroupByContext

		 SELECT GroupByContext.*
		 FROM
		 (
		    SELECT
				GroupByContext.SubQuery.Field1,
				GroupByContext.SubQuery.Field2,
				Count(*),
				SUM(GroupByContext.SubQuery.Field3)
		    FROM
			(
				SELECT dataSubquery.*
				FROM (
				   SELECT dataSequence.*
				   FROM dataSequence
				   -- all associations are attached here
				) dataSubquery
		    ) GroupByContext.SubQuery	-- groupingSubquery
		    GROUP BY
					GroupByContext.SubQuery.Field1,
					GroupByContext.SubQuery.Field2
		 ) GroupByContext

		 OUTER APPLY (  -- applying complex aggregates
			SELECT Count(*) FROM dataSubquery
			WHERE dataSubquery.Field > 10 AND
				-- filter by grouping key
				dataSubquery.Field1 == GroupByContext.Field1 AND dataSubquery.Field2 == GroupByContext.Field2
		 )

		 */

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequenceExpr    = methodCall.Arguments[0];
			var groupingKind    = GroupingType.Default;

			var dataSequenceResult = builder.TryBuildSequence(new BuildInfo(buildInfo, sequenceExpr));
			if (dataSequenceResult.BuildContext == null)
				return dataSequenceResult;

			var dataSequence = dataSequenceResult.BuildContext;

			var dataSubquery     = new SubQueryContext(dataSequence);
			var groupingSubquery = new SubQueryContext(dataSubquery);

			var keySequence     = dataSequence;

			var groupingType = methodCall.Type.GetGenericArguments()[0];
			var keySelector  = methodCall.Arguments[1].UnwrapLambda();

			// Detecting Grouping Sets
			//
			var keySelectorBody = keySelector.Body.Unwrap();

			if (keySelectorBody.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)keySelectorBody;
				if (mc.IsSameGenericMethod(GroupingSetMethods))
				{
					var groupingKey = mc.Arguments[0].Unwrap();
					if (mc.IsSameGenericMethod(Methods.LinqToDB.GroupBy.Rollup))
						groupingKind = GroupingType.Rollup;
					else if (mc.IsSameGenericMethod(Methods.LinqToDB.GroupBy.Cube))
						groupingKind = GroupingType.Cube;
					else if (mc.IsSameGenericMethod(Methods.LinqToDB.GroupBy.GroupingSets))
						groupingKind = GroupingType.GroupBySets;
					else throw new InvalidOperationException();

					keySelector = Expression.Lambda(groupingKey, keySelector.Parameters);
				}
			}

			var resultSelector  = SequenceHelper.GetArgumentLambda(methodCall, "resultSelector");
			var elementSelector = SequenceHelper.GetArgumentLambda(methodCall, "elementSelector");

			if (elementSelector == null)
			{
				var param = Expression.Parameter(methodCall.Method.GetGenericArguments()[0], "selector");
				elementSelector = Expression.Lambda(param, param);
			}

			var key                 = new KeyContext(groupingSubquery, keySelector, keySequence, buildInfo.IsSubQuery);
			var keyRef              = new ContextRefExpression(key.Body.Type, key);
			var currentPlaceholders = new List<SqlPlaceholderExpression>();

			if (!AppendGrouping(groupingSubquery, currentPlaceholders, builder, dataSequence, keyRef, groupingKind, buildInfo.GetFlags(), out var errorExpression))
			{
				return BuildSequenceResult.Error(errorExpression);
			}

			groupingSubquery.SelectQuery.GroupBy.GroupingType = groupingKind;

			var element = new ElementContext(buildInfo.Parent, elementSelector, dataSubquery, buildInfo.IsSubQuery);
			var groupBy = new GroupByContext(groupingSubquery, sequenceExpr, groupingType, key, keyRef, currentPlaceholders, element,
				!builder.DataOptions.LinqOptions.GuardGrouping || builder.IsGroupingGuardDisabled, true);

			// Will be used for eager loading generation
			element.GroupByContext = groupBy;
			// Will be used for completing GroupBy part
			key.GroupByContext = groupBy;

#if DEBUG
			Debug.WriteLine("BuildMethodCall GroupBy:\n" + groupBy.SelectQuery);
#endif

			if (resultSelector != null)
			{
				var groupContextRef = new ContextRefExpression(groupBy.GetInterfaceGroupingType(), groupBy);
				var keyExpr         = Expression.PropertyOrField(groupContextRef, nameof(IGrouping<int, int>.Key));

				var newBody = resultSelector.Body.Replace(resultSelector.Parameters[0], keyExpr);

				if (resultSelector.Parameters.Count > 1)
				{
					newBody = newBody.Replace(resultSelector.Parameters[1], groupContextRef.WithType(resultSelector.Parameters[1].Type));
				}

				var result  = new SelectContext(buildInfo.Parent, newBody, groupBy, false);
				return BuildSequenceResult.FromContext(result);
			}

			return BuildSequenceResult.FromContext(groupBy);
		}

		/// <summary>
		/// Appends GroupBy items to <paramref name="sequence"/> SelectQuery.
		/// </summary>
		/// <param name="sequence">Context which contains groping query.</param>
		/// <param name="currentPlaceholders"></param>
		/// <param name="builder"></param>
		/// <param name="onSequence">Context from which level we want to get groping SQL.</param>
		/// <param name="path">Actual expression which should be translated to grouping keys.</param>
		/// <param name="groupingKind"></param>
		/// <param name="flags"></param>
		/// <param name="errorExpression"></param>
		static bool AppendGrouping(IBuildContext sequence, List<SqlPlaceholderExpression> currentPlaceholders,
			ExpressionBuilder builder, IBuildContext onSequence, Expression path, GroupingType groupingKind,
			ProjectFlags flags, [NotNullWhen(false)] out Expression? errorExpression)
		{
			errorExpression = null;

			if (groupingKind == GroupingType.GroupBySets)
			{
				var hasSets  = false;
				var expanded = builder.MakeExpression(onSequence, path, ProjectFlags.ExtractProjection);
				foreach (var groupingSet in EnumGroupingSets(expanded))
				{
					hasSets = true;
					var setExpr = builder.BuildSqlExpression(onSequence, groupingSet,
						ProjectFlags.SQL | ProjectFlags.Keys,
						buildFlags : ExpressionBuilder.BuildFlags.ForceAssignments);

					if (!SequenceHelper.IsSqlReady(setExpr))
					{
						errorExpression = SqlErrorExpression.EnsureError(setExpr, path.Type);
						return false;
					}

					setExpr = builder.UpdateNesting(sequence, setExpr);

					var placeholders = ExpressionBuilder.CollectPlaceholders(setExpr);

					sequence.SelectQuery.GroupBy.Items.Add(new SqlGroupingSet(placeholders.Select(p => p.Sql)));
				}

				if (!hasSets)
					throw new LinqException($"Invalid grouping sets expression '{path}'.");
			}
			else
			{
				var groupSqlExpr = builder.ConvertToSqlExpr(onSequence, path, flags.SqlFlag() | ProjectFlags.Keys);
				
				if (!SequenceHelper.IsSqlReady(groupSqlExpr))
				{
					var sqLError = groupSqlExpr.Find(1, (_, e) => e is SqlErrorExpression);
					errorExpression = sqLError ?? SqlErrorExpression.EnsureError(path, path.Type);
					return false;
				}

				AppendGroupBy(builder, currentPlaceholders, sequence.SelectQuery, groupSqlExpr);
			}

			return true;
		}

		static void AppendGroupBy(ExpressionBuilder builder, List<SqlPlaceholderExpression> currentPlaceholders, SelectQuery query, Expression groupByExpression)
		{
			var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(groupByExpression);

			// it is a case whe we do not group elements
			if (placeholders.Count == 1 && QueryHelper.IsConstantFast(placeholders[0].Sql))
			{
				return;
			}

			foreach (var p in placeholders)
			{
				if (currentPlaceholders.Find(cp => ExpressionEqualityComparer.Instance.Equals(cp.Path, p.Path)) == null)
				{
					currentPlaceholders.Add(p);

					var updated = builder.UpdateNesting(query, p);
					query.GroupBy.Items.Add(updated.Sql);
				}
			}
		}

		#endregion

		#region Element Context

		internal class ElementContext : SelectContext
		{
			public ElementContext(IBuildContext? parent, LambdaExpression lambda, IBuildContext sequence, bool isSubQuery) :
				base(parent, SequenceHelper.PrepareBody(lambda, sequence), sequence, isSubQuery)
			{
				Lambda   = lambda;
				Sequence = sequence;
			}

			public LambdaExpression Lambda   { get; set; }
			public IBuildContext    Sequence { get; }

			public GroupByContext GroupByContext { get; set; } = null!;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.IsRoot() && SequenceHelper.IsSameContext(path, this))
					return path;

				var newExpr = base.MakeExpression(path, flags);

				return newExpr;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new ElementContext(null, context.CloneExpression(Lambda), context.CloneContext(Sequence), IsSubQuery);
			}
		}

		#endregion

		#region KeyContext

		internal sealed class KeyContext : SelectContext
		{
			public KeyContext(IBuildContext? parent, LambdaExpression lambda, IBuildContext sequence, bool isSubQuery) :
				base(parent, SequenceHelper.PrepareBody(lambda, sequence), sequence, isSubQuery)
			{
				Lambda   = lambda;
				Sequence = sequence;
			}

			public LambdaExpression Lambda         { get; }
			public IBuildContext    Sequence       { get; }
			public GroupByContext   GroupByContext { get; set; } = null!;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.IsRoot() || flags.IsAssociationRoot())
				{
					if (SequenceHelper.IsSameContext(path, this))
						return path;

					var root = base.MakeExpression(path, flags);
					return root;
				}

				var newFlags = flags;
				if (newFlags.IsExpression())
					newFlags = (newFlags & ~ProjectFlags.Expression) | ProjectFlags.SQL;

				if (newFlags.IsKeys() && SequenceHelper.IsSameContext(path, this))
				{
					if (Body.Type != path.Type && newFlags.IsSql())
					{
						var resultExpr = Builder.ConvertToSqlExpr(this, Body, newFlags);
						return resultExpr;
					}
				}

				newFlags |= ProjectFlags.Keys;

				var result = base.MakeExpression(path, newFlags);

				if (!ExpressionEqualityComparer.Instance.Equals(result, path))
				{
					// project deeper
					result = Builder.MakeExpression(this, result, newFlags);
				}

				if (newFlags.IsSql() || newFlags.IsExpression() || newFlags.IsExtractProjection())
				{
					if (newFlags.IsExtractProjection())
						newFlags = newFlags & ~ProjectFlags.ExtractProjection | ProjectFlags.SQL;

					result = Builder.ConvertToSqlExpr(this, result, newFlags);

					if (!newFlags.IsTest())
					{
						if (GroupByContext != null)
						{
							if (GroupByContext.SubQuery.SelectQuery.GroupBy.GroupingType != GroupingType.GroupBySets)
							{
								// appending missing keys
								AppendGroupBy(Builder, GroupByContext.CurrentPlaceholders, GroupByContext.SubQuery.SelectQuery,
									result);
							}

							// we return SQL nested as GroupByContext.SubQuery
							result = Builder.UpdateNesting(GroupByContext.SubQuery, result);
						}
					}
				}

				return result;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new KeyContext(null, context.CloneExpression(Lambda), context.CloneContext(Sequence!), IsSubQuery);
			}
		}

		#endregion

		#region GroupByContext

		internal class GroupByContext : SubQueryContext
		{
			public GroupByContext(
				IBuildContext                  sequence,
				Expression                     sequenceExpr,
				Type                           groupingType,
				KeyContext                     key,
				ContextRefExpression           keyRef,
				List<SqlPlaceholderExpression> currentPlaceholders,
				ElementContext                 element,
				bool                           isGroupingGuardDisabled,
				bool                           addToSql)
				: this(sequence, new SelectQuery(), sequenceExpr, groupingType, key, keyRef, currentPlaceholders, element, isGroupingGuardDisabled, addToSql)
			{
			}

			public GroupByContext(
				IBuildContext                  sequence,
				SelectQuery                    selectQuery,
				Expression                     sequenceExpr,
				Type                           groupingType,
				KeyContext                     key,
				ContextRefExpression           keyRef,
				List<SqlPlaceholderExpression> currentPlaceholders,
				ElementContext                 element,
				bool                           isGroupingGuardDisabled,
				bool                           addToSql)
				: base(sequence, selectQuery, addToSql)
			{
				_sequenceExpr       = sequenceExpr;
				_key                = key;
				_keyRef             = keyRef;
				CurrentPlaceholders = currentPlaceholders;
				Element             = element;
				_groupingType       = groupingType;

				IsGroupingGuardDisabled = isGroupingGuardDisabled;

				key.GroupByContext = this;
				key.Parent         = this;
			}

			readonly Expression                     _sequenceExpr;
			readonly KeyContext                     _key;
			readonly ContextRefExpression           _keyRef;
			public   List<SqlPlaceholderExpression> CurrentPlaceholders { get; }
			readonly Type                           _groupingType;
			public   bool                           IsGroupingGuardDisabled { get; }

			public ElementContext Element { get; }

			public override Type ElementType => _groupingType;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				var isSameContext = SequenceHelper.IsSameContext(path, this);

				if (isSameContext)
				{
					if (flags.IsExtractProjection())
					{
						if (path.Type == ElementType)
							return MakeSubQueryExpression(path);
						return path;
					}
				}

				if (isSameContext && (flags.IsRoot() || flags.IsTraverse()))
				{
					return path;
				}

				if (flags.IsAggregationRoot())
				{
					return path;
				}

				if (isSameContext && flags.IsKeys() && GetInterfaceGroupingType().IsSameOrParentOf(path.Type))
				{
					var result = Builder.MakeExpression(this, _keyRef, flags);
					return result;
				}

				if (isSameContext && flags.IsExpression()/* && GetInterfaceGroupingType().IsSameOrParentOf(path.Type)*/)
				{
					if (!IsGroupingGuardDisabled)
					{
						var ex = new LinqToDBException(
							"""
							You should explicitly specify selected fields for server-side GroupBy() call or add AsEnumerable() call before GroupBy() to perform client-side grouping.
							Set Configuration.Linq.GuardGrouping = false to disable this check.
							Additionally this guard exception can be disabled by extension GroupBy(...).DisableGuard().
							NOTE! By disabling this guard you accept Eager Loading for grouping query.
							"""
						)
						{
							HelpLink = "https://github.com/linq2db/linq2db/issues/365"
						};

						throw ex;
					}

					var groupingType = GetGroupingType();

					var groupingPath = ((ContextRefExpression)path).WithType(groupingType);

					var assignments = new List<SqlGenericConstructorExpression.Assignment>(2);

					assignments.Add(new SqlGenericConstructorExpression.Assignment(
						groupingType.GetProperty(nameof(Grouping<int, int>.Key))!,
						Expression.Property(groupingPath, nameof(IGrouping<int, int>.Key)), true, false));

					var eagerLoadingExpression = MakeSubQueryExpression(new ContextRefExpression(groupingType, this));

					assignments.Add(new SqlGenericConstructorExpression.Assignment(
						groupingType.GetProperty(nameof(Grouping<int, int>.Items))!,
						eagerLoadingExpression, true, false));

					return new SqlGenericConstructorExpression(
						SqlGenericConstructorExpression.CreateType.Auto,
						groupingType, null, assignments.AsReadOnly(), MappingSchema, path);
				}

				if (path is MemberExpression me)
				{
					var currentMemberExpr = me;
					var found             = false;
					while (true)
					{
						if (currentMemberExpr.Expression is ContextRefExpression && currentMemberExpr.Member.Name == "Key")
						{
							found = true;
							break;
						}

						if (currentMemberExpr.Expression is MemberExpression memberExpr)
						{
							currentMemberExpr = memberExpr;
						}
						else
							break;
					}

					if (found)
					{
						var keyRef  = new ContextRefExpression(currentMemberExpr.Type, _key);
						var keyPath = me.Replace(currentMemberExpr, keyRef);

						if (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot))
						{
							return new ContextRefExpression(path.Type, new ScopeContext(_key, this));
						}

						var result = Builder.MakeExpression(_key, keyPath, flags);

						return result;
					}
				}

				if (!isSameContext || !flags.IsSql())
				{
					var root = Builder.GetRootContext(this, path, true);
					if (root != null && typeof(IGrouping<,>).IsSameOrParentOf(root.Type))
					{
						return path;
					}
				}

				if (isSameContext && flags.IsSql() && !flags.IsKeys() && path.Type != Element.ElementType)
				{
					return path;
				}

				var newPath = SequenceHelper.CorrectExpression(path, this, Element);

				return newPath;
			}

			public Type GetGroupingType()
			{
				var groupingType = typeof(Grouping<,>).MakeGenericType(
					_key.Body.Type, Element.Body.Type);
				return groupingType;
			}

			public Type GetInterfaceGroupingType()
			{
				var groupingType = typeof(IGrouping<,>).MakeGenericType(
					_key.Body.Type, Element.Body.Type);
				return groupingType;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				var clone = new GroupByContext(context.CloneContext(SubQuery), context.CloneElement(SelectQuery), context.CloneExpression(_sequenceExpr), _groupingType,
					context.CloneContext(_key), context.CloneExpression(_keyRef),
					CurrentPlaceholders.Select(p => context.CloneExpression(p)).ToList(), context.CloneContext(Element),
					IsGroupingGuardDisabled, false);

				return clone;
			}

			static Expression MakeSubQueryExpression(MappingSchema mappingSchema, Expression sequence,
				ParameterExpression                                param,         Expression expr1, Expression expr2)
			{
				var filterLambda = Expression.Lambda(ExpressionBuilder.Equal(mappingSchema, expr1, expr2), param);
				return TypeHelper.MakeMethodCall(Methods.Enumerable.Where, sequence, filterLambda);
			}

			public Expression MakeSubQueryExpression(Expression buildExpression)
			{
				var expr = MakeSubQueryExpression(
					MappingSchema,
					_sequenceExpr,
					_key.Lambda.Parameters[0],
					ExpressionHelper.PropertyOrField(buildExpression, "Key"),
					_key.Lambda.Body);

				// do not repeat simple projection
				if (Element.Body != Element.Lambda.Parameters[0])
				{
					expr = TypeHelper.MakeMethodCall(Methods.Enumerable.Select, expr, Element.Lambda);
				}

				return expr;
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				if (!buildInfo.IsSubQuery)
					return this;

				if (buildInfo.IsAggregation && !buildInfo.CreateSubQuery)
					return this;

				if (!SequenceHelper.IsSameContext(expression, this))
					return null;

				var expr = MakeSubQueryExpression(((ContextRefExpression)buildInfo.Expression).WithType(GetInterfaceGroupingType()));

				var parentContext = buildInfo.Parent ?? this;

				expr = Builder.UpdateNesting(parentContext, expr);

				var buildResult = Builder.TryBuildSequence(new BuildInfo(buildInfo, expr) { IsAggregation = false, CreateSubQuery = false});

				return buildResult.BuildContext;
			}

			internal class Grouping<TKey,TElement> : IGrouping<TKey,TElement>
			{
				public TKey                   Key   { get; set; } = default!;
				public IEnumerable<TElement>? Items { get; set; } = default!;

				public IEnumerator<TElement> GetEnumerator()
				{
					if (Items == null)
						return Enumerable.Empty<TElement>().GetEnumerator();

					return Items.GetEnumerator();
				}

				IEnumerator IEnumerable.GetEnumerator()
				{
					return GetEnumerator();
				}
			}

			class GroupingEnumerable<TKey, TElement> : IResultEnumerable<IGrouping<TKey, TElement>>
			{
				readonly IResultEnumerable<TElement> _elements;
				readonly Func<TElement, TKey>        _groupingKey;

				public GroupingEnumerable(IResultEnumerable<TElement> elements, Func<TElement, TKey> groupingKey)
				{
					_elements    = elements;
					_groupingKey = groupingKey;
				}

				public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
				{
					return _elements.GroupBy(_groupingKey).GetEnumerator();
				}

				IEnumerator IEnumerable.GetEnumerator()
				{
					return GetEnumerator();
				}

				public IAsyncEnumerator<IGrouping<TKey, TElement>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
				{
					return new GroupingAsyncEnumerator(_elements, _groupingKey, cancellationToken);
				}

				class GroupingAsyncEnumerator : IAsyncEnumerator<IGrouping<TKey, TElement>>
				{
					readonly IResultEnumerable<TElement> _elements;
					readonly Func<TElement, TKey>        _groupingKey;
					readonly CancellationToken           _cancellationToken;

					IEnumerator<IGrouping<TKey, TElement>>? _grouped;
					IGrouping<TKey, TElement>?              _current;

					public GroupingAsyncEnumerator(IResultEnumerable<TElement> elements, Func<TElement, TKey> groupingKey, CancellationToken cancellationToken)
					{
						_elements          = elements;
						_groupingKey       = groupingKey;
						_cancellationToken = cancellationToken;
					}

					public ValueTask DisposeAsync()
					{
						_grouped?.Dispose();
						return new ValueTask();
					}

					public IGrouping<TKey, TElement> Current
					{
						get
						{
							if (_grouped == null)
								throw new InvalidOperationException("Enumeration not started.");

							if (_current == null)
								throw new InvalidOperationException("Enumeration returned no result.");

							return _current;
						}
					}

					public async ValueTask<bool> MoveNextAsync()
					{
						_grouped ??= (await _elements.ToListAsync(_cancellationToken)
								.ConfigureAwait(false))
							.GroupBy(_groupingKey)
							.GetEnumerator();

						if (_grouped.MoveNext())
						{
							_current = _grouped.Current;
							return true;
						}

						_current = null;
						return false;
					}
				}
			}
		}

		#endregion
	}
}
