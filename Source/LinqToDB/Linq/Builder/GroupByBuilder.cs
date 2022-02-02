using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;
	using Reflection;

	class GroupByBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] GroupingSetMethods = new [] { Methods.LinqToDB.GroupBy.Rollup, Methods.LinqToDB.GroupBy.Cube, Methods.LinqToDB.GroupBy.GroupingSets };

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

					if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ExpressionBuilder.GroupSubQuery<,>))
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

			var groupingType    = methodCall.Type.GetGenericArguments()[0];
			var keySelector     = (LambdaExpression)methodCall.Arguments[1].Unwrap()!;
			LambdaExpression elementSelector;
			if (methodCall.Arguments.Count >= 3)
			{
				elementSelector = (LambdaExpression)methodCall.Arguments[2].Unwrap()!;
			}
			else
			{
				var param = Expression.Parameter(groupingType.GetGenericArguments()[1], "selector");
				elementSelector = Expression.Lambda(param, param);
			}

			var key                 = new KeyContext(groupingSubquery, keySelector, buildInfo.IsSubQuery, keySequence);
			var keyRef              = new ContextRefExpression(keySelector.Parameters[0].Type, key);
			var currentPlaceholders = new List<SqlPlaceholderExpression>();
			if (groupingKind != GroupingType.GroupBySets)
			{
				AppendGrouping(groupingSubquery, currentPlaceholders, builder, dataSequence, key.Body, buildInfo.GetFlags());
			}
			else
			{
				var goupingSetBody = groupingKey!.Body;
				var groupingSets = EnumGroupingSets(goupingSetBody).ToArray();
				if (groupingSets.Length == 0)
					throw new LinqException($"Invalid grouping sets expression '{goupingSetBody}'.");

				foreach (var groupingSet in groupingSets)
				{
					var groupSql = builder.ConvertExpressions(keySequence, groupingSet, ConvertFlags.Key, null);
					groupingSubquery.SelectQuery.GroupBy.Items.Add(
						new SqlGroupingSet(groupSql.Select(s => keySequence.SelectQuery.Select.AddColumn(s.Sql))));
				}
			}

			groupingSubquery.SelectQuery.GroupBy.GroupingType = groupingKind;

			var element = new ElementContext(buildInfo.Parent, elementSelector, buildInfo.IsSubQuery, dataSubquery);
			var groupBy = new GroupByContext(groupingSubquery, sequenceExpr, groupingType, key, keyRef, currentPlaceholders, element, builder.IsGroupingGuardDisabled);

			// Will be used for eager loading generation
			element.GroupByContext = groupBy;
			// Will be used for completing GroupBy part
			key.GroupByContext = groupBy;

			Debug.WriteLine("BuildMethodCall GroupBy:\n" + groupBy.SelectQuery);

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

			// only keys
			groupSqlExpr = builder.UpdateNesting(sequence, groupSqlExpr);

			AppendGroupBy(builder, currentPlaceholders, sequence.SelectQuery, groupSqlExpr);
		}

		static void AppendGroupBy(ExpressionBuilder builder, List<SqlPlaceholderExpression> currentPlaceholders, SelectQuery query, Expression result)
		{
			var placeholders = builder.CollectDistinctPlaceholders(result);
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
			public ElementContext(IBuildContext? parent, LambdaExpression lambda, bool isSubQuery, params IBuildContext[] sequences) : base(parent, lambda, isSubQuery, sequences)
			{
			}

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
		}

		#endregion

		#region KeyContext

		internal class KeyContext : SelectContext
		{
			public KeyContext(IBuildContext? parent, LambdaExpression lambda, bool isSubQuery, params IBuildContext[] sequences)
				: base(parent, lambda, isSubQuery, sequences)
			{
			}

			public GroupByContext GroupByContext { get; set; } = null!;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.HasFlag(ProjectFlags.Root))
				{
					if (SequenceHelper.IsSameContext(path, this))
						return path;

					var root = base.MakeExpression(path, flags);
					return root;
				}

				var newFlags = flags;
				if (newFlags.HasFlag(ProjectFlags.Expression))
					newFlags = (newFlags & ~ProjectFlags.Expression) | ProjectFlags.SQL;

				newFlags = newFlags | ProjectFlags.Keys;

				var result = base.MakeExpression(path, newFlags);

				result = Builder.ConvertToSqlExpr(this, result, newFlags);

				// appending missing keys
				AppendGroupBy(Builder, GroupByContext.CurrentPlaceholders, GroupByContext.SubQuery.SelectQuery, result);

				return result;
			}
		}

		#endregion

		#region GroupByContext

		internal class GroupByContext : SubQueryContext
		{
			public GroupByContext(
				IBuildContext  sequence,
				Expression     sequenceExpr,
				Type           groupingType,
				KeyContext     key,
				ContextRefExpression keyRef,
				List<SqlPlaceholderExpression> currentPlaceholders,
				SelectContext  element,
				bool           isGroupingGuardDisabled)
				: base(sequence)
			{
				_sequenceExpr        = sequenceExpr;
				_key                 = key;
				_keyRef              = keyRef;
				CurrentPlaceholders = currentPlaceholders;
				Element              = element;
				_groupingType        = groupingType;

				_isGroupingGuardDisabled = isGroupingGuardDisabled;

				key.Parent = this;
			}

			readonly Expression                     _sequenceExpr;
			readonly KeyContext                     _key;
			readonly ContextRefExpression           _keyRef;
			public   List<SqlPlaceholderExpression> CurrentPlaceholders { get; }
			readonly Type                           _groupingType;
			readonly bool                           _isGroupingGuardDisabled;

			public SelectContext   Element { get; }

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) &&
				    (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AggregtionRoot) || flags.HasFlag(ProjectFlags.Test)))
				{
					return path;
				}

				if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Keys))
				{
					var result = Builder.MakeExpression(_keyRef, flags);
					return result;
				}

				if (path is MemberExpression me && me.Expression is ContextRefExpression && me.Member.Name == "Key")
				{
					var keyPath = new ContextRefExpression(me.Type, _key);

					return keyPath;
				}

				var newPath = SequenceHelper.CorrectExpression(path, this, Element);

				return newPath;
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			readonly Dictionary<Tuple<Expression?,int,ConvertFlags>,SqlInfo[]> _expressionIndex = new ();

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public override int ConvertToParentIndex(int index, IBuildContext context)
			{
				throw new NotImplementedException();
			}

			static Expression MakeSubQueryExpression(MappingSchema mappingSchema, Expression sequence,
				ParameterExpression param, Expression expr1, Expression expr2)
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
				if (Element.Lambda.Body != Element.Lambda.Parameters[0])
				{
					expr = TypeHelper.MakeMethodCall(Methods.Enumerable.Select, expr, Element.Lambda);
				}

				return expr;
			}

			public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				if (buildInfo.AggregationTest)
					return new AggregationRoot(Element);

				if (!buildInfo.IsSubQuery)
					return this;

				if (buildInfo.IsAggregation && !buildInfo.CreateSubQuery)
				{
					return this;
				}

				if (!SequenceHelper.IsSameContext(expression, this))
					return null;

				var expr = MakeSubQueryExpression(buildInfo.Expression);

				var ctx = Builder.BuildSequence(new BuildInfo(buildInfo, expr) { IsAggregation = false });

				return ctx;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr = Builder.FinalizeProjection(this,
					Builder.MakeExpression(new ContextRefExpression(typeof(T), this), ProjectFlags.Expression));

				var mapper = Builder.BuildMapper<T>(expr);

				QueryRunner.SetRunQuery(query, mapper);
			}
		}

		#endregion

		public class AggregationRoot : PassThroughContext
		{
			public AggregationRoot(IBuildContext context) : base(context)
			{
				SelectQuery = new SelectQuery();
			}

			public override SelectQuery SelectQuery { get; set; }
		}
	}
}
