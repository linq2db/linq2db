using System;
using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Async;
	using Common;
	using Common.Internal;
	using Extensions;
	using Mapping;
	using SqlQuery;
	using Reflection;
	using LinqToDB.Expressions;

	sealed class GroupByBuilder : MethodCallBuilder
	{
		static readonly MethodInfo[] GroupingSetMethods = { Methods.LinqToDB.GroupBy.Rollup, Methods.LinqToDB.GroupBy.Cube, Methods.LinqToDB.GroupBy.GroupingSets };

		#region Builder Methods

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (!methodCall.IsQueryable("GroupBy"))
				return false;

			var body = ((LambdaExpression)methodCall.Arguments[1].Unwrap()).Body.Unwrap();

			if (body.NodeType == ExpressionType	.MemberInit)
			{
				var mi = (MemberInitExpression)body;
				bool throwExpr;

				if (mi.NewExpression.Arguments.Count > 0 || mi.Bindings.Count == 0)
					throwExpr = true;
				else
					throwExpr = mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment);

				if (throwExpr)
					throw new NotSupportedException($"Explicit construction of entity type '{body.Type}' in group by is not allowed.");
			}

			return (methodCall.Arguments[methodCall.Arguments.Count - 1].Unwrap().NodeType == ExpressionType.Lambda);
		}

		static IEnumerable<Expression> EnumGroupingSets(Expression expression)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.New:
					{
						var newExpression = (NewExpression)expression;

						foreach (var arg in newExpression.Arguments)
						{
							yield return arg;
						}
						break;
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

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequenceExpr    = methodCall.Arguments[0];
			LambdaExpression?   groupingKey = null;
			var groupingKind    = GroupingType.Default;
			if (sequenceExpr.NodeType == ExpressionType.Call)
			{
				var call = (MethodCallExpression)methodCall.Arguments[0];

				if (call.IsQueryable("Select"))
				{
					var selectParam = (LambdaExpression)call.Arguments[1].Unwrap();
					var type = selectParam.Body.Type;

					if (type.IsGenericType /*&& type.GetGenericTypeDefinition() == typeof(ExpressionBuilder.GroupSubQuery<,>)*/)
					{
						var selectParamBody = selectParam.Body.Unwrap();
						MethodCallExpression? groupingMethod = null;
						if (selectParamBody is MemberInitExpression mi)
						{
							var assignment = mi.Bindings.OfType<MemberAssignment>().FirstOrDefault(m => m.Member.Name == "Key");
							if (assignment?.Expression.NodeType == ExpressionType.Call)
							{
								var mc = (MethodCallExpression)assignment.Expression;
								if (mc.IsSameGenericMethod(GroupingSetMethods))
								{
									groupingMethod = mc;
									groupingKey    = (LambdaExpression)mc.Arguments[0].Unwrap();
									if (mc.IsSameGenericMethod(Methods.LinqToDB.GroupBy.Rollup))
										groupingKind = GroupingType.Rollup;
									else if (mc.IsSameGenericMethod(Methods.LinqToDB.GroupBy.Cube))
										groupingKind = GroupingType.Cube;
									else if (mc.IsSameGenericMethod(Methods.LinqToDB.GroupBy.GroupingSets))
										groupingKind = GroupingType.GroupBySets;
									else throw new InvalidOperationException();
								}
							}
						}

						if (groupingMethod != null && groupingKey != null)
						{
							sequenceExpr = sequenceExpr.Replace(groupingMethod, groupingKey.Body.Unwrap());
						}

					}
				}
			}

			var dataSequence = builder.BuildSequence(new BuildInfo(buildInfo, sequenceExpr));

			var dataSubquery     = new SubQueryContext(dataSequence);
			var groupingSubquery = new SubQueryContext(dataSubquery);

			var keySequence     = dataSequence;

			var groupingType = methodCall.Type.GetGenericArguments()[0];
			var keySelector  = methodCall.Arguments[1].UnwrapLambda();

			var resultSelector  = SequenceHelper.GetArgumentLambda(methodCall, "resultSelector");
			var elementSelector = SequenceHelper.GetArgumentLambda(methodCall, "elementSelector");

			if (elementSelector == null)
			{
				var param = Expression.Parameter(methodCall.Method.GetGenericArguments()[0], "selector");
				elementSelector = Expression.Lambda(param, param);
			}

			var key                 = new KeyContext(groupingSubquery, keySelector, keySequence, buildInfo.IsSubQuery);
			var keyRef              = new ContextRefExpression(keySelector.Parameters[0].Type, key);
			var currentPlaceholders = new List<SqlPlaceholderExpression>();
			if (groupingKind != GroupingType.GroupBySets)
			{
				AppendGrouping(groupingSubquery, currentPlaceholders, builder, dataSequence, key.Body, buildInfo.GetFlags());
			}
			else
			{
				var groupingSetBody = groupingKey!.Body;

				var hasSets = false;
				foreach (var groupingSet in EnumGroupingSets(groupingSetBody))
				{
					throw new NotImplementedException();
					/*var groupSql = builder.ConvertExpressions(keySequence, groupingSet, ConvertFlags.Key, null);
					groupingSubquery.SelectQuery.GroupBy.Items.Add(
						new SqlGroupingSet(groupSql.Select(s => keySequence.SelectQuery.Select.AddColumn(s.Sql))));*/
				}

				if (!hasSets)
					throw new LinqException($"Invalid grouping sets expression '{groupingSetBody}'.");
			}

			groupingSubquery.SelectQuery.GroupBy.GroupingType = groupingKind;

			var element = new ElementContext(buildInfo.Parent, elementSelector, dataSubquery, buildInfo.IsSubQuery);
			var groupBy = new GroupByContext(groupingSubquery, sequenceExpr, groupingType, key, keyRef, currentPlaceholders, element, 
				!builder.DataOptions.LinqOptions.GuardGrouping || builder.IsGroupingGuardDisabled, true);

			// Will be used for eager loading generation
			element.GroupByContext = groupBy;
			// Will be used for completing GroupBy part
			key.GroupByContext = groupBy;

			Debug.WriteLine("BuildMethodCall GroupBy:\n" + groupBy.SelectQuery);

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
				return result;
			}

			return groupBy;
		}

		/// <summary>
		/// Appends GroupBy items to <paramref name="sequence"/> SelectQuery.
		/// </summary>
		/// <param name="sequence">Context which contains groping query.</param>
		/// <param name="currentPlaceholders"></param>
		/// <param name="builder"></param>
		/// <param name="onSequence">Context from which level we want to get groping SQL.</param>
		/// <param name="path">Actual expression which should be translated to grouping keys.</param>
		static void AppendGrouping(IBuildContext sequence, List<SqlPlaceholderExpression> currentPlaceholders, ExpressionBuilder builder, IBuildContext onSequence, Expression path, ProjectFlags flags)
		{
			var groupSqlExpr = builder.ConvertToSqlExpr(onSequence, path, flags | ProjectFlags.Keys);

			if (!flags.IsTest())
			{
				// only keys
				groupSqlExpr = builder.UpdateNesting(sequence, groupSqlExpr);
			}

			AppendGroupBy(builder, currentPlaceholders, sequence.SelectQuery, groupSqlExpr);
		}

		static void AppendGroupBy(ExpressionBuilder builder, List<SqlPlaceholderExpression> currentPlaceholders, SelectQuery query, Expression result)
		{
			var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(result);
			var allowed      = placeholders.Where(p => !QueryHelper.IsConstantFast(p.Sql));

			foreach (var p in allowed)
			{
				if (currentPlaceholders.Find(cp => ExpressionEqualityComparer.Instance.Equals(cp.Path, p.Path)) == null)
				{
					currentPlaceholders.Add(p);
					query.GroupBy.Expr(p.Sql);
				}
			}
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
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

			public override Expression GetEagerLoadExpression(Expression path)
			{
				var subquery = GroupByContext.MakeSubQueryExpression(new ContextRefExpression(path.Type, GroupByContext));
				return subquery;
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.HasFlag(ProjectFlags.Root) && SequenceHelper.IsSameContext(path, this))
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
				if (flags.HasFlag(ProjectFlags.Expand))
				{
					return path;
				}

				if (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot))
				{
					if (SequenceHelper.IsSameContext(path, this))
						return path;

					var root = base.MakeExpression(path, flags);
					return root;
				}

				var newFlags = flags;
				if (newFlags.HasFlag(ProjectFlags.Expression))
					newFlags = (newFlags & ~ProjectFlags.Expression) | ProjectFlags.SQL;

				newFlags |= ProjectFlags.Keys;

				var result = base.MakeExpression(path, newFlags);

				if (newFlags.HasFlag(ProjectFlags.SQL))
				{
					result = Builder.ConvertToSqlExpr(this, result, newFlags);

					if (!newFlags.IsTest())
					{
						// appending missing keys
						AppendGroupBy(Builder, GroupByContext.CurrentPlaceholders, GroupByContext.SubQuery.SelectQuery, result);

						// we return SQL nested as GroupByContext
						result = Builder.UpdateNesting(GroupByContext, result);
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
				: this(sequence, new SelectQuery { ParentSelect = sequence.SelectQuery.ParentSelect }, sequenceExpr, groupingType, key, keyRef, currentPlaceholders, element, isGroupingGuardDisabled, addToSql)
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

				_isGroupingGuardDisabled = isGroupingGuardDisabled;

				key.GroupByContext = this;
				key.Parent         = this;
			}

			readonly Expression                     _sequenceExpr;
			readonly KeyContext                     _key;
			readonly ContextRefExpression           _keyRef;
			public   List<SqlPlaceholderExpression> CurrentPlaceholders { get; }
			readonly Type                           _groupingType;
			readonly bool                           _isGroupingGuardDisabled;

			public ElementContext Element { get; }

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) &&
				    (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AggregationRoot) || flags.HasFlag(ProjectFlags.Test)))
				{
					return path;
				}

				if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Keys))
				{
					var result = Builder.MakeExpression(this, _keyRef, flags);
					return result;
				}

				if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Expression)/* && GetInterfaceGroupingType().IsSameOrParentOf(path.Type)*/)
				{
					if (!_isGroupingGuardDisabled)
					{
						var ex = new LinqToDBException(
							"You should explicitly specify selected fields for server-side GroupBy() call or add AsEnumerable() call before GroupBy() to perform client-side grouping.\n" +
							"Set Configuration.Linq.GuardGrouping = false to disable this check.\n" +
							"Additionally this guard exception can be disabled by extension GroupBy(...).DisableGuard().\n" +
							"NOTE! By disabling this guard you accept Eager Loading for grouping query."
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
						new SqlEagerLoadExpression(groupingPath, groupingPath, eagerLoadingExpression), true, false));

					return new SqlGenericConstructorExpression(
						SqlGenericConstructorExpression.CreateType.Auto,
						groupingType, null, assignments.AsReadOnly());
				}

				if (path is MemberExpression me && me.Expression is ContextRefExpression && me.Member.Name == "Key")
				{
					// do not expand
					if (flags.HasFlag(ProjectFlags.Expand))
						return path;

					var keyPath = new ContextRefExpression(me.Type, _key);

					if (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot))
					{
						return new ContextRefExpression(path.Type, new ScopeContext(_key, this));
					}

					var result = Builder.MakeExpression(this, keyPath, flags);

					return result;
				}

				if (!SequenceHelper.IsSameContext(path, this) || !flags.HasFlag(ProjectFlags.SQL))
				{
					var root = Builder.GetRootObject(path);
					if (root is ContextRefExpression contextRef &&
					    typeof(IGrouping<,>).IsSameOrParentOf(contextRef.Type))
					{
						return path;
					}
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
				var clone = new GroupByContext(context.CloneContext(SubQuery), context.CloneElement(SelectQuery), _sequenceExpr, _groupingType,
					context.CloneContext(_key), context.CloneExpression(_keyRef),
					CurrentPlaceholders.Select(p => context.CloneExpression(p)).ToList(), context.CloneContext(Element),
					_isGroupingGuardDisabled, false);

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
					Builder.MappingSchema,
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
				if (buildInfo.AggregationTest)
					return new AggregationRoot(Element);

				if (!buildInfo.IsSubQuery)
					return this;

				if (buildInfo.IsAggregation && !buildInfo.CreateSubQuery)
					return this;

				if (!SequenceHelper.IsSameContext(expression, this))
					return null;

				var expr = MakeSubQueryExpression(((ContextRefExpression)buildInfo.Expression).WithType(GetInterfaceGroupingType()));
				expr = SequenceHelper.MoveToScopedContext(expr, this);

				var ctx = Builder.BuildSequence(new BuildInfo(buildInfo, expr) { IsAggregation = false});

				return ctx;
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

#if !NATIVE_ASYNC
					public Task DisposeAsync()
					{
						_grouped?.Dispose();
						return TaskCache.CompletedTask;
					}
#else
					public ValueTask DisposeAsync()
					{
						_grouped?.Dispose();
						return new ValueTask();
					}
#endif
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

#if !NATIVE_ASYNC
					public async Task<bool> MoveNextAsync()
#else
					public async ValueTask<bool> MoveNextAsync()
#endif
					{
						if (_grouped == null)
						{
							_grouped = (await _elements.ToListAsync(_cancellationToken)
									.ConfigureAwait(Configuration.ContinueOnCapturedContext))
								.GroupBy(_groupingKey)
								.GetEnumerator();
						}

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

		public class AggregationRoot : PassThroughContext
		{
			public AggregationRoot(IBuildContext context) : base(new CloningContext().CloneContext(context))
			{
				SelectQuery.From.Tables.Clear();
				SelectQuery.Select.Columns.Clear();
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new AggregationRoot(context.CloneContext(Context));
			}
		}
	}
}
