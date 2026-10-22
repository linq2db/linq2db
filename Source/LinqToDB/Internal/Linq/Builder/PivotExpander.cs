using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Linq.Builder.Visitors;
using LinqToDB.Internal.Mapping;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	// Rewrites g.Pivot(forColumn, values).Cell(...)...<terminal> into the filtered aggregates it stands for, one per
	// (cell, value) pair: aggregate(g.Where(row => forColumn(row) == value)). It runs in expose, the last stage whose
	// output is the query cache key, because the value set and the cell names decide the shape of the query: a changed
	// input is a different tree, so nothing has to be compared by value after the key is taken.
	static class PivotExpander
	{
		static readonly MethodInfo _expandMethod = typeof(PivotExpander).GetMethod(nameof(Expand), BindingFlags.Static | BindingFlags.NonPublic)!;

		static readonly ConstructorInfo _cellsConstructor = typeof(PivotCells).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null)!;

		// A real member wins the lookup, so a cell named after one would be shadowed rather than reachable. Methods and
		// the indexer do not take part in that lookup.
		static readonly HashSet<string> _cellsMembers = new(
			typeof(PivotCells).GetFields(BindingFlags.Public | BindingFlags.Instance).Select(f => f.Name)
				.Concat(typeof(PivotCells).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetIndexParameters().Length == 0).Select(p => p.Name)),
			StringComparer.Ordinal);

		public static bool IsPivotCall(MethodCallExpression call)
			=> IsBuilderMember(call) || IsPivotMethod(call);

		public static bool IsTerminal(MethodCallExpression call)
			=> IsBuilderMember(call) && call.Method.Name is nameof(PivotBuilder<,>.AsDictionary) or nameof(PivotBuilder<,>.AsDynamic) or nameof(PivotBuilder<,>.Select);

		static bool IsBuilderMember(MethodCallExpression call)
			=> call.Method.DeclaringType is { IsGenericType: true } type && type.GetGenericTypeDefinition() == typeof(PivotBuilder<,>);

		static bool IsPivotMethod(MethodCallExpression call)
			=> call.Method.DeclaringType == typeof(LinqExtensions) && string.Equals(call.Method.Name, nameof(LinqExtensions.Pivot), StringComparison.Ordinal);

		public static Expression Expand(MethodCallExpression terminal, ExposeExpressionVisitor visitor)
		{
			var builderType = terminal.Method.DeclaringType!.GetGenericArguments();

			return _expandMethod.MakeGenericMethod(builderType).InvokeExt<Expression>(null, [terminal, visitor]);
		}

		sealed class Template<TSource, TFor>(LambdaExpression aggregate, bool liftResult, Func<TFor, string>? name)
		{
			public LambdaExpression    Aggregate  { get; } = aggregate;
			public bool                LiftResult { get; } = liftResult;
			public Func<TFor, string>? Name       { get; } = name;
		}

		static Expression Expand<TSource, TFor>(MethodCallExpression terminal, ExposeExpressionVisitor visitor)
		{
			// The chain is read from the terminal down to the Pivot call, outermost call first.
			var calls = new List<MethodCallExpression>();
			var node  = terminal.Object;

			while (node is MethodCallExpression call && IsBuilderMember(call) && !IsTerminal(call))
			{
				calls.Add(call);
				node = call.Object;
			}

			if (node is not MethodCallExpression pivot || !IsPivotMethod(pivot))
				throw new LinqToDBException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.Pivot.Error_PivotChainNotFromGroup, terminal.Method.Name));

			var group     = pivot.Arguments[0];
			var forColumn = pivot.Arguments[1] as LambdaExpression
				?? throw new LinqToDBException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.Pivot.Error_PivotLambdaNotInline, "column"));
			var values    = ReadValues<TFor>(pivot.Arguments[2], visitor);
			var where     = Methods.Enumerable.Where.MakeGenericMethod(typeof(TSource));

			Expression RowsOf(TFor value)
				=> Expression.Call(where, group, Expression.Lambda(Expression.Equal(forColumn.Body, Expression.Constant(value, typeof(TFor))), forColumn.Parameters));

			if (string.Equals(terminal.Method.Name, nameof(PivotBuilder<,>.Select), StringComparison.Ordinal))
			{
				if (calls.Count > 0)
					throw new LinqToDBException(ErrorHelper.Pivot.Error_PivotSelectAfterCells);

				var selector = terminal.Arguments[0] as LambdaExpression
					?? throw new LinqToDBException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.Pivot.Error_PivotLambdaNotInline, "selector"));

				var items = values.Select(value => selector.GetBody(Expression.Constant(value, typeof(TFor)), RowsOf(value)));

				return Expression.Convert(Expression.NewArrayInit(terminal.Method.GetGenericArguments()[0], items), terminal.Type);
			}

			var templates = new List<Template<TSource, TFor>>();

			for (var i = calls.Count - 1; i >= 0; i--)
			{
				var call = calls[i];

				if (string.Equals(call.Method.Name, nameof(PivotBuilder<,>.Cell), StringComparison.Ordinal))
				{
					var aggregate = call.Arguments[0] as LambdaExpression
						?? throw new LinqToDBException(ErrorHelper.Pivot.Error_PivotAggregateNotInline);

					templates.Add(new(
						aggregate,
						// A cell no row matches must read null, not default(TCell).
						!call.Method.GetGenericArguments()[0].IsNullableOrReferenceType,
						call.Arguments[1] is ConstantExpression { Value: null } ? null : (Func<TFor, string>?)ReadClientValue(call.Arguments[1], "name factory", visitor)));
				}
				else
				{
					var cells = ReadClientValue(call.Arguments[0], "cell templates", visitor) as IEnumerable<PivotCell<TSource, TFor>>
						?? throw new LinqToDBException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.Pivot.Error_PivotArgumentNull, "cell templates"));

					foreach (var cell in cells)
					{
						if (cell == null)
							throw new LinqToDBException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.Pivot.Error_PivotArgumentNull, "cell template"));

						templates.Add(new(cell.Aggregate, cell.LiftResult, cell.Name));
					}
				}
			}

			if (templates.Count == 0)
				throw new LinqToDBException(ErrorHelper.Pivot.Error_PivotNeedsCellTemplate);

			var bindings = new List<MemberBinding>(templates.Count * values.Length);
			var seen     = new HashSet<string>(StringComparer.Ordinal);

			foreach (var template in templates)
			{
				if (template.Name == null && templates.Count > 1)
					throw new LinqToDBException(ErrorHelper.Pivot.Error_PivotNameFactoryRequired);

				foreach (var value in values)
				{
					var name = template.Name != null
						? template.Name(value)
						: Convert.ToString(value, CultureInfo.InvariantCulture);

					if (string.IsNullOrEmpty(name))
						throw new LinqToDBException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.Pivot.Error_PivotValueEmptyName, value));

					if (!seen.Add(name!))
						throw new LinqToDBException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.Pivot.Error_PivotDuplicateName, name));

					if (_cellsMembers.Contains(name!))
						throw new LinqToDBException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.Pivot.Error_DynamicColumnShadowsMember, name, nameof(PivotCells)));

					var body = template.Aggregate.GetBody(RowsOf(value));

					// A cell no row matches must read null, not default(TCell).
					if (template.LiftResult)
						body = Expression.Convert(body, typeof(Nullable<>).MakeGenericType(body.Type));

					bindings.Add(Expression.Bind(new DynamicColumnInfo(typeof(PivotCells), body.Type, name!), body));
				}
			}

			var cellsInit = Expression.MemberInit(Expression.New(_cellsConstructor), bindings);

			return cellsInit.Type == terminal.Type ? cellsInit : Expression.Convert(cellsInit, terminal.Type);
		}

		static TFor[] ReadValues<TFor>(Expression argument, ExposeExpressionVisitor visitor)
		{
			var values = ReadClientValue(argument, "value set", visitor) as IEnumerable<TFor>
				?? throw new LinqToDBException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.Pivot.Error_PivotArgumentNull, "value set"));

			return values.ToArray();
		}

		// Read once per expansion, against the current values: the result is baked into the tree the cache compares. A
		// value taken from a compiled query's argument array is recorded, so the compiled query keys its builds on it.
		static object? ReadClientValue(Expression argument, string what, ExposeExpressionVisitor visitor)
		{
			if (ReadsQueryParameter(argument))
				throw new LinqToDBException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.Pivot.Error_PivotReadsQueryParameter, what));

			if (!ReferenceEquals(visitor.ResolveCompiledQueryArguments(argument), argument))
				visitor.RecordMaterializedArgumentSlots(argument);

			return visitor.EvaluateExpression(argument);
		}

		// A parameter no lambda inside the argument declares is the group, a row, or an outer query's parameter.
		static bool ReadsQueryParameter(Expression argument)
		{
			var declared = new HashSet<ParameterExpression>();

			argument.Visit(declared, static (d, e) =>
			{
				if (e is LambdaExpression lambda)
				{
					foreach (var parameter in lambda.Parameters)
						d.Add(parameter);
				}
			});

			return argument.Find(declared, static (d, e) =>
				e is ParameterExpression parameter
				&& !d.Contains(parameter)
				&& parameter != ExpressionBuilder.ParametersParam
				&& parameter != ExpressionConstants.DataContextParam) != null;
		}
	}
}
