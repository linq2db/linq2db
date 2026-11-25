using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;

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
			}

			var value = argument.EvaluateExpression();

			if (argument is not ConstantExpression constant || ExpressionCacheHelpers.ShouldRemoveConstantFromCache(constant, builder.MappingSchema))
				builder.ParametersContext.MarkAsValue(argument, Snapshot(value));

			return value;
		}

		// A sequence is compared element by element, and the one the call read may be mutated in place afterwards.
		static object? Snapshot(object? value)
			=> value is IEnumerable sequence and not string and not IQueryable ? sequence.Cast<object?>().ToArray() : value;
	}
}
