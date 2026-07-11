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
	[BuildsMethodCall(nameof(LinqExtensions.Pivot))]
	sealed class PivotBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable
				&& call.Method.DeclaringType == typeof(LinqExtensions)
				&& call.Arguments.Count == 2;

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var pivotLambda = methodCall.Arguments[1].UnwrapLambda();
			var aggregates  = ParseAggregates(pivotLambda);

			// Native path: a single aggregate over a single FOR column (any native-PIVOT provider), or a
			// composite FOR where the provider supports multi-column PIVOT (Oracle, DuckDB).
			if (builder.DataContext.SqlProviderFlags.IsPivotSupported
				&& aggregates is { Count: 1 }
				&& (aggregates[0].ForColumnMembers.Count == 1 || builder.DataContext.SqlProviderFlags.IsMultiColumnPivotSupported))
			{
				var native = TryBuildNative(builder, methodCall, buildInfo, pivotLambda, aggregates[0]);
				if (native != null)
					return native.Value;
			}

			// Portable lowering: GROUP BY the passthrough keys + conditional aggregation (SUM(CASE WHEN ...)).
			var lowered = BuildLoweredExpression(methodCall, pivotLambda);
			return BuildSequenceResult.FromContext(builder.BuildSequence(new BuildInfo(buildInfo, lowered)));
		}

		#region Portable lowering (GroupBy + conditional aggregation)

		static Expression BuildLoweredExpression(MethodCallExpression methodCall, LambdaExpression pivotLambda)
		{
			var genericArgs = methodCall.Method.GetGenericArguments();
			var sourceType  = genericArgs[0];
			var resultType  = genericArgs[1];
			var sourceExpr  = methodCall.Arguments[0];
			var newExpr     = (NewExpression)pivotLambda.Body;

			// Collect the GROUP BY key members (p.Key.<Member>).
			var keyMembers = new List<MemberInfo>();
			foreach (var arg in newExpr.Arguments)
			{
				var unwrapped = Unwrap(arg);
				if (unwrapped is MemberExpression { Expression: MemberExpression { Member.Name: "Key" } } keyMember)
					if (!keyMembers.Contains(keyMember.Member))
						keyMembers.Add(keyMember.Member);
			}

			if (keyMembers.Count == 0)
				throw new LinqToDBException("A Pivot projection must reference at least one grouping key (p.Key.<member>).");

			var rowParam       = Expression.Parameter(sourceType, "row");
			var keyMemberExprs = keyMembers.Select(m => (Expression)Expression.MakeMemberAccess(rowParam, m)).ToArray();

			// GROUP BY key: a single member directly, or a ValueTuple of the members for a composite key.
			var keyBody = keyMembers.Count == 1
				? keyMemberExprs[0]
				: Expression.New(MakeValueTupleType(keyMemberExprs.Select(e => e.Type).ToArray()).GetConstructors()[0], keyMemberExprs);

			var keySelector  = Expression.Lambda(keyBody, rowParam);
			var keyType      = keyBody.Type;
			var groupingType = typeof(IGrouping<,>).MakeGenericType(keyType, sourceType);
			var gParam       = Expression.Parameter(groupingType, "g");
			var gKey         = Expression.Property(gParam, "Key");

			// Access a key member on g.Key: the value itself for a single key, or g.Key.ItemN for a ValueTuple.
			Expression KeyAccess(MemberInfo member)
				=> keyMembers.Count == 1
					? gKey
					: Expression.Field(gKey, "Item" + (keyMembers.IndexOf(member) + 1).ToString(CultureInfo.InvariantCulture));

			// Rebuild the projection over the grouping: keys → g.Key, aggregate markers → conditional aggregate.
			var newArgs = new Expression[newExpr.Arguments.Count];
			for (var i = 0; i < newExpr.Arguments.Count; i++)
			{
				var unwrapped = Unwrap(newExpr.Arguments[i]);

				if (unwrapped is MemberExpression { Expression: MemberExpression { Member.Name: "Key" } } keyMember)
				{
					newArgs[i] = ConvertIfNeeded(KeyAccess(keyMember.Member), newExpr.Arguments[i].Type);
					continue;
				}

				if (unwrapped is MethodCallExpression mc
					&& mc.Method.DeclaringType is { IsGenericType: true } dt
					&& dt.GetGenericTypeDefinition() == typeof(IPivotBuilder<>))
				{
					newArgs[i] = BuildConditionalAggregate(mc, sourceType, gParam, newExpr.Arguments[i].Type);
					continue;
				}

				throw new LinqToDBException("Unsupported expression in a Pivot projection; use p.Key.<member> or an aggregate marker (p.Sum/Min/Max/Count/Avg).");
			}

			var resultSelector = Expression.Lambda(newExpr.Update(newArgs), gParam);

			// IQueryable GroupBy(keySelector).Select(resultSelector).
			var qGroupBy = Methods.Queryable.GroupBy.MakeGenericMethod(sourceType, keyType);
			var qSelect  = Methods.Queryable.Select.MakeGenericMethod(groupingType, resultType);

			var grouped   = Expression.Call(qGroupBy, sourceExpr, Expression.Quote(keySelector));
			var projected = Expression.Call(qSelect, grouped, Expression.Quote(resultSelector));

			return projected;
		}

		static Type MakeValueTupleType(Type[] types) => types.Length switch
		{
			1 => typeof(ValueTuple<>)      .MakeGenericType(types),
			2 => typeof(ValueTuple<,>)     .MakeGenericType(types),
			3 => typeof(ValueTuple<,,>)    .MakeGenericType(types),
			4 => typeof(ValueTuple<,,,>)   .MakeGenericType(types),
			5 => typeof(ValueTuple<,,,,>)  .MakeGenericType(types),
			6 => typeof(ValueTuple<,,,,,>) .MakeGenericType(types),
			7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(types),
			_ => throw new LinqToDBException("The portable Pivot fallback supports at most 7 grouping keys."),
		};

		static Expression BuildConditionalAggregate(MethodCallExpression marker, Type sourceType, ParameterExpression gParam, Type resultType)
		{
			var function  = marker.Method.Name;
			var valueLam  = marker.Arguments[0].UnwrapLambda();
			var forColLam = marker.Arguments[1].UnwrapLambda();
			var forValue  = marker.Arguments[2].EvaluateExpression();

			var rowParam  = Expression.Parameter(sourceType, "row");
			var forBody   = ReplaceParameter(forColLam, rowParam);
			var predicate = BuildForEqualityPredicate(forBody, forValue);

			if (string.Equals(function, "Count", StringComparison.Ordinal))
			{
				var predLambda  = Expression.Lambda(predicate, rowParam);
				var countMethod = typeof(Enumerable).GetMethods()
					.First(m => string.Equals(m.Name, "Count", StringComparison.Ordinal) && m.IsGenericMethodDefinition && m.GetParameters().Length == 2)
					.MakeGenericMethod(sourceType);
				return ConvertIfNeeded(Expression.Call(countMethod, gParam, predLambda), resultType);
			}

			var valueBody   = ReplaceParameter(valueLam, rowParam);
			var nullableType = MakeNullable(valueBody.Type);
			var nullableVal  = valueBody.Type == nullableType ? valueBody : Expression.Convert(valueBody, nullableType);

			// row => predicate ? (nullable)value : null
			var selectorBody = Expression.Condition(predicate, nullableVal, Expression.Constant(null, nullableType));
			var selector     = Expression.Lambda(selectorBody, rowParam);

			var aggregateMethod = GetEnumerableAggregate(function, sourceType, nullableType);
			return ConvertIfNeeded(Expression.Call(aggregateMethod, gParam, selector), resultType);
		}

		static MethodInfo GetEnumerableAggregate(string function, Type sourceType, Type nullableSelectorType)
		{
			switch (function)
			{
				case "Sum":
				case "Average":
				{
					var method = typeof(Enumerable).GetMethods()
						.First(m => string.Equals(m.Name, function, StringComparison.Ordinal)
							&& m.IsGenericMethodDefinition
							&& m.GetParameters().Length == 2
							&& m.GetParameters()[1].ParameterType.IsGenericType
							&& m.GetParameters()[1].ParameterType.GetGenericArguments()[1] == nullableSelectorType);
					return method.MakeGenericMethod(sourceType);
				}
				case "Min":
				case "Max":
				{
					var method = typeof(Enumerable).GetMethods()
						.First(m => string.Equals(m.Name, function, StringComparison.Ordinal)
							&& m.IsGenericMethodDefinition
							&& m.GetGenericArguments().Length == 2
							&& m.GetParameters().Length == 2);
					return method.MakeGenericMethod(sourceType, nullableSelectorType);
				}
				default:
					throw new LinqToDBException($"Unsupported pivot aggregate '{function}'.");
			}
		}

		// Equality against the FOR value: a single comparison, or an AND of per-member comparisons for a
		// composite (anonymous-type) FOR — e.g. row.Year == 2000 AND row.Quarter == 1.
		static Expression BuildForEqualityPredicate(Expression forBody, object? forValue)
		{
			if (forBody is NewExpression { Members: { } members } newFor && members.Count > 0)
			{
				var          constant  = Expression.Constant(forValue);
				Expression?  predicate = null;

				for (var i = 0; i < newFor.Arguments.Count; i++)
				{
					var expected = ConvertIfNeeded(Expression.PropertyOrField(constant, members[i].Name), newFor.Arguments[i].Type);
					var equals   = Expression.Equal(newFor.Arguments[i], expected);
					predicate    = predicate == null ? equals : Expression.AndAlso(predicate, equals);
				}

				return predicate!;
			}

			return Expression.Equal(forBody, Expression.Constant(forValue, forBody.Type));
		}

		static Type MakeNullable(Type type)
			=> type.IsValueType && Nullable.GetUnderlyingType(type) == null ? typeof(Nullable<>).MakeGenericType(type) : type;

		static Expression ConvertIfNeeded(Expression expression, Type type)
			=> expression.Type == type ? expression : Expression.Convert(expression, type);

		static Expression ReplaceParameter(LambdaExpression lambda, Expression replacement)
			=> lambda.GetBody(replacement);

		static Expression Unwrap(Expression expression)
		{
			while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
				expression = u.Operand;
			return expression;
		}

		#endregion

		static BuildSequenceResult? TryBuildNative(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, LambdaExpression pivotLambda, AggregateSpec agg)
		{
			var sourceContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQuery()));
			var tableContext  = SequenceHelper.GetTableContext(sourceContext);

			if (tableContext?.SqlTable is not { } sourceTable)
				return null;

			var valueField = sourceTable.Fields.Find(f => string.Equals(f.Name, agg.ValueMember, StringComparison.Ordinal));
			if (valueField == null)
				return null;

			var forFields = new ISqlExpression[agg.ForColumnMembers.Count];
			for (var i = 0; i < forFields.Length; i++)
			{
				var name  = agg.ForColumnMembers[i];
				var field = sourceTable.Fields.Find(f => string.Equals(f.Name, name, StringComparison.Ordinal));
				if (field == null)
					return null;
				forFields[i] = field;
			}

			var composite    = agg.ForColumnMembers.Count > 1;
			var node         = new SqlPivotTable(sourceTable);
			var sqlAggregate = new SqlPivotAggregate(agg.Function, valueField, forFields);
			var valueDbType  = builder.MappingSchema.GetDbDataType(agg.ValueType);

			foreach (var value in agg.Values)
			{
				var outputField = new SqlField(valueDbType, value.OutputMember, true)
				{
					Table = node,
					// Single FOR: PIVOT names the output column by the value; composite FOR: alias it to the member name.
					PhysicalName = composite ? value.OutputMember : FormatPivotValue(value.ForValues[0]),
				};

				node.OutputFields.Add(outputField);
				sqlAggregate.Values.Add(new SqlPivotValue(value.ForValues.Select(static v => (ISqlExpression)new SqlValue(v!)).ToArray(), outputField));
			}

			node.Aggregates.Add(sqlAggregate);

			var selectQuery = buildInfo.SelectQuery;
			selectQuery.From.Table(node);

			var context = new PivotContext(builder.GetTranslationModifier(), builder, pivotLambda.ReturnType, selectQuery, node);
			var body    = context.BuildProjectionBody(pivotLambda, sqlAggregate);

			var projection = new SelectContext(builder.GetTranslationModifier(), buildInfo.Parent, builder, null, body, selectQuery, buildInfo.IsSubQuery);

			return BuildSequenceResult.FromContext(projection);
		}

		static string FormatPivotValue(object? value)
			=> value switch
			{
				null           => "null",
				string s       => s,
				IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
				_              => Convert.ToString(value, CultureInfo.InvariantCulture)!,
			};

		/// <summary>Builds the pivot projection directly, replacing Key members and aggregate markers with placeholders over the node's fields.</summary>
		sealed class PivotContext : BuildContextBase
		{
			readonly SqlPivotTable _node;

			public PivotContext(TranslationModifier translationModifier, ExpressionBuilder builder, Type elementType, SelectQuery selectQuery, SqlPivotTable node)
				: base(translationModifier, builder, elementType, selectQuery)
			{
				_node = node;
			}

			public override MappingSchema MappingSchema => Builder.MappingSchema;

			public Expression BuildProjectionBody(LambdaExpression pivotLambda, SqlPivotAggregate aggregate)
			{
				var newExpr = (NewExpression)pivotLambda.Body;
				var newArgs = new Expression[newExpr.Arguments.Count];

				for (var i = 0; i < newExpr.Arguments.Count; i++)
				{
					var arg = newExpr.Arguments[i];

					var unwrapped = arg;
					while (unwrapped is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
						unwrapped = u.Operand;

					// p.Key.<Member> → passthrough / GROUP BY column
					if (unwrapped is MemberExpression { Expression: MemberExpression { Member.Name: "Key" } } keyMember)
					{
						var field = GetOrCreateKeyField(keyMember.Member);
						newArgs[i] = ExpressionBuilder.CreatePlaceholder(this, field, arg);
						continue;
					}

					// p.Sum/Min/Max/Count/Avg(...) → the generated output column, matched by the projection member name.
					if (unwrapped is MethodCallExpression mc
						&& mc.Method.DeclaringType is { IsGenericType: true } dt
						&& dt.GetGenericTypeDefinition() == typeof(IPivotBuilder<>))
					{
						var memberName = newExpr.Members![i].Name;
						var value      = aggregate.Values.First(v => string.Equals(v.OutputField.Name, memberName, StringComparison.Ordinal));

						newArgs[i] = ExpressionBuilder.CreatePlaceholder(this, value.OutputField, arg);
						continue;
					}

					throw new LinqToDBException("Unsupported expression in a Pivot projection; use p.Key.<member> or an aggregate marker (p.Sum/Min/Max/Count/Avg).");
				}

				return newExpr.Update(newArgs);
			}

			SqlField GetOrCreateKeyField(MemberInfo member)
			{
				var existing = _node.KeyFields.Find(f => string.Equals(f.Name, member.Name, StringComparison.Ordinal));
				if (existing != null)
					return existing;

				var memberType = member is PropertyInfo pi ? pi.PropertyType : ((FieldInfo)member).FieldType;
				var canBeNull  = !memberType.IsValueType || Nullable.GetUnderlyingType(memberType) != null;

				var field = new SqlField(Builder.MappingSchema.GetDbDataType(memberType), member.Name, canBeNull) { Table = _node };
				_node.KeyFields.Add(field);
				_node.OutputFields.Add(field);

				return field;
			}

			public override Expression    MakeExpression(Expression path, ProjectFlags flags) => path;
			public override IBuildContext Clone(CloningContext context)                        => throw new NotSupportedException();
			public override void          SetRunQuery<T>(Query<T> query, Expression expr)      => throw new NotSupportedException();
			public override SqlStatement  GetResultStatement()                                => new SqlSelectStatement(SelectQuery);
		}

		static List<AggregateSpec> ParseAggregates(LambdaExpression pivotLambda)
		{
			var aggregates = new List<AggregateSpec>();

			if (pivotLambda.Body is NewExpression newExpr && newExpr.Members != null)
			{
				for (var i = 0; i < newExpr.Arguments.Count; i++)
				{
					var arg    = newExpr.Arguments[i];
					var member = newExpr.Members[i].Name;

					while (arg is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
						arg = u.Operand;

					if (arg is MethodCallExpression mc
						&& mc.Method.DeclaringType is { IsGenericType: true } dt
						&& dt.GetGenericTypeDefinition() == typeof(IPivotBuilder<>))
					{
						var value     = mc.Arguments[0].UnwrapLambda();
						var forColumn = mc.Arguments[1].UnwrapLambda();
						var forValue  = mc.Arguments[2].EvaluateExpression();

						var valueName     = TryGetSimpleMemberName(value);
						var forColMembers = ExtractForColumnMembers(forColumn);

						// A non-simple value selector or an unrecognized FOR selector is not eligible for the
						// native path — return empty so the caller lowers to conditional aggregation.
						if (valueName == null || forColMembers == null)
							return new List<AggregateSpec>();

						AddAggregate(aggregates, mc.Method.Name.ToUpperInvariant(), valueName, forColMembers, value.Body.Type, member, ExtractForValues(forValue, forColMembers));
					}
				}
			}

			return aggregates;
		}

		static void AddAggregate(List<AggregateSpec> aggregates, string function, string valueMember, IReadOnlyList<string> forColumnMembers, Type valueType, string outputMember, IReadOnlyList<object?> forValues)
		{
			var existing = aggregates.Find(a =>
				string.Equals(a.Function, function, StringComparison.Ordinal)
				&& string.Equals(a.ValueMember, valueMember, StringComparison.Ordinal)
				&& a.ForColumnMembers.SequenceEqual(forColumnMembers, StringComparer.Ordinal));

			if (existing == null)
			{
				existing = new AggregateSpec
				{
					Function         = function,
					ValueMember      = valueMember,
					ForColumnMembers = forColumnMembers,
					ValueType        = valueType,
					Values           = new List<PivotValueSpec>(),
				};
				aggregates.Add(existing);
			}

			existing.Values.Add(new PivotValueSpec { ForValues = forValues, OutputMember = outputMember });
		}

		static string? TryGetSimpleMemberName(LambdaExpression lambda)
		{
			var body = lambda.Body;
			while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
				body = u.Operand;
			return body is MemberExpression m ? m.Member.Name : null;
		}

		// FOR column member name(s): a single member, or the members of an anonymous type for a composite FOR.
		static IReadOnlyList<string>? ExtractForColumnMembers(LambdaExpression forColumn)
		{
			var body = forColumn.Body;
			while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
				body = u.Operand;

			if (body is MemberExpression single)
				return new[] { single.Member.Name };

			if (body is NewExpression { Arguments.Count: > 0 } newExpr)
			{
				var names = new string[newExpr.Arguments.Count];
				for (var i = 0; i < newExpr.Arguments.Count; i++)
				{
					var arg = newExpr.Arguments[i];
					while (arg is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } cu)
						arg = cu.Operand;
					if (arg is not MemberExpression m)
						return null;
					names[i] = m.Member.Name;
				}

				return names;
			}

			return null;
		}

		// FOR value(s): the scalar value for a single FOR, or the per-member values of an anonymous value for a composite FOR.
		static IReadOnlyList<object?> ExtractForValues(object? forValue, IReadOnlyList<string> memberNames)
		{
			if (memberNames.Count == 1)
				return new[] { forValue };

			var type   = forValue?.GetType();
			var values = new object?[memberNames.Count];
			for (var i = 0; i < memberNames.Count; i++)
				values[i] = type?.GetProperty(memberNames[i])?.GetValue(forValue);

			return values;
		}

		sealed class AggregateSpec
		{
			public required string               Function         { get; init; }
			public required string               ValueMember      { get; init; }
			public required IReadOnlyList<string> ForColumnMembers { get; init; }
			public required Type                 ValueType        { get; init; }
			public required List<PivotValueSpec> Values           { get; init; }
		}

		sealed class PivotValueSpec
		{
			public required IReadOnlyList<object?> ForValues    { get; init; }
			public required string                 OutputMember { get; init; }
		}
	}
}
