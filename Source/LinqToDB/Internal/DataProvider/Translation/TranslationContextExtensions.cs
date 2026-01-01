using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public static class TranslationContextExtensions
	{
		public static bool TryEvaluate<T>(this ITranslationContext translationContext, Expression expression, out T result)
		{
			if (translationContext.CanBeEvaluated(expression))
			{
				var value = translationContext.Evaluate(expression);
				if (value is T t)
				{
					result = t;
					return true;
				}
			}

			result = default!;
			return false;
		}

		public static DbDataType GetDbDataType(this ITranslationContext translationContext, ISqlExpression sqlExpression)
		{
			return QueryHelper.GetDbDataType(sqlExpression, translationContext.MappingSchema);
		}

		public static SqlPlaceholderExpression CreatePlaceholder(this ITranslationContext translationContext, ISqlExpression sqlExpression, Expression basedOn)
		{
			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, sqlExpression, basedOn);
		}

		public static bool TranslateToSqlExpression(this ITranslationContext translationContext, Expression expression, [NotNullWhen(true)] out ISqlExpression? translated)
		{
			var result = translationContext.Translate(expression, TranslationFlags.Sql);

			if (result is not SqlPlaceholderExpression placeholder)
			{
				translated = null;
				return false;
			}

			translated = placeholder.Sql;
			return true;
		}

		public static bool TranslateToSqlExpression(this ITranslationContext translationContext, Expression expression, [NotNullWhen(true)] out ISqlExpression? translated, [NotNullWhen(false)] out SqlErrorExpression? error)
		{
			var result = translationContext.Translate(expression, TranslationFlags.Sql);

			if (result is not SqlPlaceholderExpression placeholder)
			{
				translated = null;
				error      = SqlErrorExpression.EnsureError(result);
				return false;
			}

			translated = placeholder.Sql;
			error      = null;
			return true;
		}

		public static IDisposable? UsingTypeFromExpression(this ITranslationContext translationContext, ISqlExpression? fromExpression)
		{
			if (fromExpression is null)
				return null;

			var columnDescriptor = QueryHelper.GetColumnDescriptor(fromExpression);

			if (columnDescriptor == null)
				return null;

			return translationContext.UsingColumnDescriptor(columnDescriptor);
		}

		public static IDisposable? UsingTypeFromExpression(this ITranslationContext translationContext, params Expression[] fromExpressions)
		{
			for (var i = 0; i < fromExpressions.Length; i++)
			{
				var translated = translationContext.Translate(fromExpressions[i], TranslationFlags.Sql);
				if (translated is not SqlPlaceholderExpression placeholder)
					continue;
				var found = translationContext.UsingTypeFromExpression(placeholder.Sql);
				if (found != null)
					return found;
			}

			return null;
		}

		[Flags]
		public enum AllowedAggregationOperators
		{
			Filter = 1 << 0,
			OrderBy = 1 << 1,
			Distinct = 1 << 2,

			All = Filter | OrderBy | Distinct,
		}

		public record OrderByInformation(Expression Expr, bool IsDescending, Sql.NullsPosition Nulls);

		public class AggregationInfo
		{
			public Expression?          RootContext      { get; set; }
			public Expression?          FilterExpression { get; set; }
			public Expression?          ValueExpression  { get; set; }
			public OrderByInformation[] OrderBy          { get; set; } = [];
			public bool                 IsDistinct       { get; set; }
			public bool                 IsGroupBy        { get; set; }

			public bool TranslateValue(ITranslationContext translationContext, [NotNullWhen(true)] out ISqlExpression? sql, out Expression? error)
			{
				error = null;
				sql   = null;

				if (ValueExpression == null || RootContext == null)
					return false;

				using var currentCtx = translationContext.UsingCurrentAggregationContext(RootContext);

				var translated = translationContext.Translate(ValueExpression, TranslationFlags.Sql);

				if (translated is not SqlPlaceholderExpression placeholder)
				{
					error = translated is SqlErrorExpression
						? translated
						: translationContext.CreateErrorExpression(ValueExpression, "Cannot translate aggregation value expression to SQL.", typeof(SqlPlaceholderExpression));

					return false;
				}

				sql = placeholder.Sql;

				return true;
			}

			public SqlPlaceholderExpression CreatePlaceholder(ITranslationContext translationContext, ISqlExpression sqlExpression, Expression path)
			{
				if (RootContext == null)
					throw new InvalidOperationException("Root context is not set for aggregation info.");

				SelectQuery query;

				if (IsGroupBy)
				{
					query = ((GroupByBuilder.GroupByContext)((ContextRefExpression)RootContext).BuildContext).SubQuery.SelectQuery;
				}
				else if (RootContext is ContextRefExpression contextRefExpression)
				{
					query = contextRefExpression.BuildContext.SelectQuery;
				}
				else
				{
					throw new InvalidOperationException("Root context is not a valid ContextRefExpression.");
				}

				return translationContext.CreatePlaceholder(query, sqlExpression, path);
			}
		}

		static readonly string[] _orderByNames = [nameof(Queryable.OrderBy), nameof(Queryable.OrderByDescending), nameof(Queryable.ThenBy), nameof(Queryable.ThenByDescending)];
		static readonly string[] _allowedNames = [nameof(Queryable.Select), nameof(Queryable.Where), nameof(Queryable.Distinct), nameof(Queryable.OrderBy), .._orderByNames];

		public static bool GetAggregationInfo(
			this ITranslationContext                 translationContext,
			Expression                               expression,
			AllowedAggregationOperators              allowedOperations,
			[NotNullWhen(true)] out AggregationInfo? aggregationInfo
		)
		{
			aggregationInfo = null;

			Expression?               filterExpression = null;
			Expression?               rootContext      = null;
			Expression?               valueExpression  = null;
			List<OrderByInformation>? orderBy          = null;
			bool                      isDistinct       = false;
			bool                      isGroupBy        = false;

			List<MethodCallExpression>? chain = null;

			var current = expression.UnwrapConvert();

			ContextRefExpression? contextRef;

			while (true)
			{
				if (current is ContextRefExpression refExpression)
				{
					var root = translationContext.Translate(current, TranslationFlags.Traverse);
					if (ExpressionEqualityComparer.Instance.Equals(root, current))
					{
						contextRef = refExpression;
						break;
					}

					current = root;
					continue;
				}

				if (current is MethodCallExpression methodCall)
				{
					if (methodCall.IsQueryable(nameof(Queryable.AsQueryable)))
					{
						current = methodCall.Arguments[0];
						continue;
					}

					if (methodCall.IsQueryable(_allowedNames))
					{
						chain ??= new List<MethodCallExpression>();
						chain.Add(methodCall);
						current = methodCall.Arguments[0];
						continue;
					}
				}

				return false;
			}

			if (contextRef is { BuildContext: GroupByBuilder.GroupByContext })
			{
				isGroupBy = true;
			}

			rootContext = contextRef;

			var currentRef = contextRef;

			if (chain != null)
			{
				for (int i = chain.Count - 1; i >= 0; i--)
				{
					var method = chain[i];
					if (method.IsQueryable(nameof(Queryable.Distinct)))
					{
						// Distinct should be the first method in the chain
						if (i != 0)
						{
							return false;
						}

						if (!allowedOperations.HasFlag(AllowedAggregationOperators.Distinct))
							return false;

						isDistinct = true;
					}
					else if (method.IsQueryable(nameof(Queryable.Select)))
					{
						// do not support complex projections
						if (method.Arguments.Count != 2)
						{
							return false;
						}

						var body = SequenceHelper.PrepareBody(method.Arguments[1].UnwrapLambda(), currentRef.BuildContext);

						var selectContext = new SelectContext(contextRef.BuildContext, body, contextRef.BuildContext, false);
						currentRef = new ContextRefExpression(selectContext.ElementType, selectContext);
					}
					else if (method.IsQueryable(nameof(Queryable.Where)))
					{
						if (!allowedOperations.HasFlag(AllowedAggregationOperators.Filter))
							return false;

						var filter = SequenceHelper.PrepareBody(method.Arguments[1].UnwrapLambda(), currentRef.BuildContext);
						if (filterExpression == null)
							filterExpression = filter;
						else
							filterExpression = Expression.AndAlso(filterExpression, filter);
					}
					else if (method.IsQueryable(_orderByNames))
					{
						if (!allowedOperations.HasFlag(AllowedAggregationOperators.OrderBy))
							return false;

						var orderByExpression = SequenceHelper.PrepareBody(method.Arguments[1].UnwrapLambda(), currentRef.BuildContext);
						orderBy ??= new List<OrderByInformation>();

						orderBy.Add(new OrderByInformation(
							orderByExpression.UnwrapConvert(),
							method.Method.Name is nameof(Queryable.OrderByDescending) or nameof(Queryable.ThenByDescending),
							Sql.NullsPosition.None
						));
					}
					else
					{
						return false;
					}
				}
			}

			valueExpression = currentRef;

			aggregationInfo = new AggregationInfo
			{
				RootContext      = rootContext,
				FilterExpression = filterExpression,
				ValueExpression  = valueExpression,
				OrderBy          = orderBy?.ToArray() ?? [],
				IsDistinct       = isDistinct,
				IsGroupBy        = isGroupBy,
			};

			return true;
		}

	}
}
