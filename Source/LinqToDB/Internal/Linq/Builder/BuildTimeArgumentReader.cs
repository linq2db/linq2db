using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;

namespace LinqToDB.Internal.Linq.Builder
{
	// Reads the arguments of a call a builder runs itself, after the cache key was taken.
	static class BuildTimeArgumentReader
	{
		// The key sees a captured value only as a placeholder. Every value the call reads is therefore registered for
		// by-value comparison, as extension builders do, so a changed value is a different query rather than a hit on the
		// one built for the old value. A quoted lambda is passed through rather than compiled, so it may reference an
		// enclosing lambda's parameter; what it reads becomes SQL.
		internal static object? ReadArgument(ExpressionBuilder builder, Expression argument)
		{
			switch (argument)
			{
				case UnaryExpression { NodeType: ExpressionType.Quote } quote:
					return quote.Operand;

				case NewArrayExpression { NodeType: ExpressionType.NewArrayInit } array:
				{
					var result = Array.CreateInstance(array.Type.GetElementType()!, array.Expressions.Count);

					for (var i = 0; i < array.Expressions.Count; i++)
						result.SetValue(ReadArgument(builder, array.Expressions[i]), i);

					return result;
				}

				case LambdaExpression lambda:
					RegisterBuildTimeValues(builder, lambda.Body);
					return argument.EvaluateExpression();
			}

			var value = argument.EvaluateExpression();

			// Read once, so the call and the comparison see the same items however the sequence enumerates.
			if (value is IEnumerable sequence and not string and not IQueryable and not Array && argument.Type.GetItemType() is { } itemType)
			{
				var items = sequence.Cast<object?>().ToArray();
				var array = Array.CreateInstance(itemType, items.Length);

				for (var i = 0; i < items.Length; i++)
					array.SetValue(items[i], i);

				value = array;
			}

			if (argument is not ConstantExpression constant || ExpressionCacheHelpers.ShouldRemoveConstantFromCache(constant, builder.MappingSchema))
				builder.ParametersContext.MarkAsValue(argument, Snapshot(value));

			return value;
		}

		// Code the call runs: every value it reads, outside a quoted lambda, is part of the query's identity.
		static void RegisterBuildTimeValues(ExpressionBuilder builder, Expression expression)
		{
			_ = expression.Transform(builder, static (b, e) =>
			{
				switch (e)
				{
					case UnaryExpression { NodeType: ExpressionType.Quote }:
						return new TransformInfo(e, true);

					case LambdaExpression:
						return new TransformInfo(e);

					case ConstantExpression constant when !ExpressionCacheHelpers.ShouldRemoveConstantFromCache(constant, b.MappingSchema):
						return new TransformInfo(e, true);
				}

				if (b.CanBeEvaluatedOnClient(e))
				{
					MarkGuardedValue(b, e);
					return new TransformInfo(e, true);
				}

				return new TransformInfo(e);
			});
		}

		static readonly object _threw = new();

		// The lambda may reach the read only behind a test on its own parameter, so the read can fail when the lambda would
		// not: it is compared as its value or the fact that it threw, both when marked and at every cache lookup.
		static void MarkGuardedValue(ExpressionBuilder builder, Expression expression)
		{
			object? value;

			try
			{
				value = Snapshot(expression.EvaluateExpression());
			}
			catch (Exception)
			{
				value = _threw;
			}

			var guarded = Expression.TryCatch(
				Expression.Convert(expression, typeof(object)),
				Expression.Catch(typeof(Exception), Expression.Field(null, typeof(BuildTimeArgumentReader), nameof(_threw))));

			builder.ParametersContext.MarkAsValue(guarded, value);
		}

		// A sequence is compared element by element, and the one the call read may be mutated in place afterwards.
		static object? Snapshot(object? value)
			=> value is IEnumerable sequence and not string and not IQueryable ? sequence.Cast<object?>().ToArray() : value;
	}
}
