using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

using LinqToDB.Data;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// Helpers for composite format strings and <see cref="FormattableString"/> values and expressions.
	/// </summary>
	public static class FormattableStringHelper
	{
		/// <summary>
		/// A format item (<c>{index[,alignment][:format]}</c>) of a composite format string.
		/// </summary>
		[StructLayout(LayoutKind.Auto)]
		public readonly struct FormatItem
		{
			/// <summary>
			/// Creates a format item.
			/// </summary>
			public FormatItem(int start, int length, int index, int indexStart, int indexLength, bool hasAlignment)
			{
				Start        = start;
				Length       = length;
				Index        = index;
				IndexStart   = indexStart;
				IndexLength  = indexLength;
				HasAlignment = hasAlignment;
			}

			/// <summary>
			/// Position of the opening brace.
			/// </summary>
			public int Start       { get; }
			/// <summary>
			/// Length of the item, both braces included.
			/// </summary>
			public int Length      { get; }
			/// <summary>
			/// Argument index.
			/// </summary>
			public int Index       { get; }
			/// <summary>
			/// Position of the first digit of the argument index.
			/// </summary>
			public int IndexStart  { get; }
			/// <summary>
			/// Number of digits of the argument index.
			/// </summary>
			public int IndexLength { get; }
			/// <summary>
			/// <see langword="true"/> when the item has an alignment component (<c>{0,5}</c>).
			/// </summary>
			public bool HasAlignment { get; }
		}

		const int MaxIndex = 1_000_000;

		/// <summary>
		/// Returns the format items of composite format string <paramref name="format"/>,
		/// or <see langword="null"/> when <paramref name="format"/> is not a valid composite format.
		/// </summary>
		public static IReadOnlyList<FormatItem>? ParseFormatItems(string format)
		{
			List<FormatItem>? items = null;

			for (var i = 0; i < format.Length; i++)
			{
				var c = format[i];

				if (c == '}')
				{
					if (i + 1 < format.Length && format[i + 1] == '}')
					{
						i++;
						continue;
					}

					return null;
				}

				if (c != '{')
					continue;

				if (i + 1 < format.Length && format[i + 1] == '{')
				{
					i++;
					continue;
				}

				var start      = i;
				var indexStart = ++i;
				var index      = 0;

				while (i < format.Length && format[i] is >= '0' and <= '9')
				{
					index = index * 10 + (format[i] - '0');

					if (index >= MaxIndex)
						return null;

					i++;
				}

				if (i == indexStart)
					return null;

				var indexLength = i - indexStart;
				var end         = format.IndexOf('}', i);

				if (end < 0)
					return null;

				while (i < end && format[i] == ' ')
					i++;

				(items ??= new()).Add(new FormatItem(start, end - start + 1, index, indexStart, indexLength, format[i] == ','));

				i = end;
			}

			return items ?? (IReadOnlyList<FormatItem>)Array.Empty<FormatItem>();
		}

		/// <summary>
		/// Returns, per argument, whether <paramref name="format"/> references it by a format item,
		/// or <see langword="null"/> when <paramref name="format"/> is not a valid composite format.
		/// </summary>
		public static bool[]? GetReferencedArguments(string format, int count)
		{
			var items = ParseFormatItems(format);

			if (items == null)
				return null;

			var referenced = new bool[count];

			foreach (var item in items)
				if (item.Index < count)
					referenced[item.Index] = true;

			return referenced;
		}

		/// <summary>
		/// Returns a literal segment of composite format string <paramref name="format"/> with <c>{{</c> and <c>}}</c> unescaped.
		/// </summary>
		public static string Unescape(string format, int start, int length)
		{
			return format.Substring(start, length)
				.Replace("{{", "{", StringComparison.Ordinal)
				.Replace("}}", "}", StringComparison.Ordinal);
		}

		/// <summary>
		/// Returns <c>FormattableStringFactory.Create(format, arguments)</c> expression.
		/// </summary>
		public static MethodCallExpression CreateExpression(string format, Expression arguments)
		{
			return Expression.Call(null, Methods.System.FormattableStringFactory_Create, Expression.Constant(format), arguments);
		}

		/// <summary>
		/// Returns <c>FormattableStringFactory.Create(format, new object[] { ... })</c> expression for <paramref name="value"/>, with arguments as query parameters.
		/// </summary>
		public static MethodCallExpression CreateExpression(FormattableString value)
		{
			return CreateExpression(value.Format, CreateArgumentsExpression(value.GetArguments()));
		}

		/// <summary>
		/// Returns <c>new object[] { ... }</c> expression for <paramref name="arguments"/>, with every non-null argument as query parameter.
		/// </summary>
		public static NewArrayExpression CreateArgumentsExpression(object?[] arguments)
		{
			return Expression.NewArrayInit(typeof(object), arguments.Select(static p =>
			{
				if (p == null)
					return Expression.Constant(null, typeof(object));

				var valueExpression = SequenceHelper.WrapAsParameter(Expression.Constant(p, p.GetType()));
				if (valueExpression.Type != typeof(object))
					valueExpression = Expression.Convert(valueExpression, typeof(object));

				return valueExpression;
			}));
		}

		/// <summary>
		/// Splits <c>FormattableStringFactory.Create(format, new object[] { ... })</c> expression into its format and argument array.
		/// </summary>
		public static bool TrySplit(Expression expression, [NotNullWhen(true)] out Expression? format, [NotNullWhen(true)] out NewArrayExpression? arguments)
		{
			if (expression is MethodCallExpression { Arguments: [var f, NewArrayExpression { NodeType: ExpressionType.NewArrayInit } a] } create
				&& create.Method == Methods.System.FormattableStringFactory_Create)
			{
				format    = f;
				arguments = a;
				return true;
			}

			format    = null;
			arguments = null;
			return false;
		}

		/// <summary>
		/// Compares two <see cref="FormattableString"/> values by format and arguments.
		/// </summary>
		public static bool AreEqual(FormattableString value1, FormattableString value2, Func<object?, object?, bool> argumentsEqual)
		{
			return string.Equals(value1.Format, value2.Format, StringComparison.Ordinal) && argumentsEqual(value1.GetArguments(), value2.GetArguments());
		}

		/// <summary>
		/// Returns hash code of <see cref="FormattableString"/> value, consistent with <see cref="AreEqual"/>.
		/// </summary>
		public static int ComputeHashCode(FormattableString value, Func<object?, int> argumentsHashCode)
		{
			return StringComparer.Ordinal.GetHashCode(value.Format) * 397 + argumentsHashCode(value.GetArguments());
		}

		/// <summary>
		/// Splits raw SQL argument of <c>FromSql</c> / <c>Sql.Expr</c> into the format and argument expressions.
		/// An argument the format does not reference by a format item is replaced with <see langword="null"/>,
		/// unless it can hold a <see cref="DataParameter"/> (referenced by name or position instead),
		/// is an <see cref="ISqlExpression"/>, or <paramref name="keepUnreferenced"/> is set.
		/// </summary>
		/// <param name="formatArg"><see cref="FormattableString"/> or <see cref="RawSqlString"/> expression.</param>
		/// <param name="parametersArg">Arguments array expression of <see cref="RawSqlString"/> overloads.</param>
		/// <param name="keepUnreferenced">Keep unreferenced arguments, e.g. when the provider binds parameters by position.</param>
		/// <param name="format">Composite format of raw SQL.</param>
		/// <param name="arguments">Argument expressions.</param>
		public static void PrepareRawSqlArguments(Expression formatArg, Expression? parametersArg, bool keepUnreferenced, out string format, out IReadOnlyList<Expression> arguments)
		{
			// Consider that FormattableString is used
			if (formatArg.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)formatArg;

				if (mc.Arguments[1].NodeType != ExpressionType.NewArrayInit)
				{
					format    = mc.Arguments[0].EvaluateExpression<string>()!;
					var args  = new Expression[mc.Arguments.Count - 1];

					for (var i = 0; i < args.Length; i++)
						args[i] = mc.Arguments[i + 1];

					arguments = args;
				}
				else
				{
					format    = mc.Arguments[0].EvaluateExpression<string>()!;
					arguments = ((NewArrayExpression)mc.Arguments[1]).Expressions;
				}
			}
			else
			{
				var evaluatedSql = formatArg.EvaluateExpression()!;
				if (evaluatedSql is FormattableString formattable)
				{
					format     = formattable.Format;

					var array = formattable.GetArguments();
					var args   = new Expression[array.Length];

					for (var i = 0; i < array.Length; i++)
					{
						Expression expr = Expression.Constant(array[i], array[i]?.GetType() ?? typeof(object));
						args[i] = expr;
					}

					arguments = args;
				}
				else
				{
					var rawSqlString = (RawSqlString)evaluatedSql;

					format        = rawSqlString.Format;
					var arrayExpr = parametersArg!;

					if (arrayExpr.NodeType == ExpressionType.NewArrayInit)
					{
						arguments = ((NewArrayExpression)arrayExpr).Expressions;
					}
					else
					{
						var array = arrayExpr.EvaluateExpression<object[]>()!;
						var args  = new Expression[array.Length];
						for (var i = 0; i < array.Length; i++)
						{
							var type = array[i]?.GetType() ?? typeof(object);

							if (typeof(ISqlExpression).IsAssignableFrom(type))
							{
								args[i] = Expression.Constant(array[i]);
								continue;
							}

							Expression expr = Expression.ArrayIndex(arrayExpr, ExpressionInstances.Int32(i));
							if (type != typeof(object))
								expr = Expression.Convert(expr, type);

							args[i] = expr;
						}

						arguments = args;
					}
				}
			}

			if (!keepUnreferenced)
				arguments = ReplaceUnreferencedArguments(format, arguments);
		}

		// an argument is built into SQL even when the format does not reference it, registering a parameter
		// some providers (Sybase ASE) reject
		static IReadOnlyList<Expression> ReplaceUnreferencedArguments(string format, IReadOnlyList<Expression> arguments)
		{
			if (arguments.Count == 0)
				return arguments;

			var referenced = GetReferencedArguments(format, arguments.Count);

			if (referenced == null)
				return arguments;

			Expression[]? result = null;

			for (var i = 0; i < arguments.Count; i++)
			{
				if (referenced[i])
					continue;

				var type = arguments[i].UnwrapConvert()!.Type;

				if (typeof(DataParameter).IsSameOrParentOf(type) || type.IsSameOrParentOf(typeof(DataParameter)) || typeof(ISqlExpression).IsAssignableFrom(type))
					continue;

				result ??= arguments.ToArray();
				result[i] = Expression.Constant(null, typeof(object));
			}

			return result ?? arguments;
		}
	}
}
