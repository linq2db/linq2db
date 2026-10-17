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

				SkipSpaces(format, ref i);

				var hasAlignment = i < format.Length && format[i] == ',';

				if (hasAlignment)
				{
					i++;
					SkipSpaces(format, ref i);

					if (i < format.Length && format[i] == '-')
						i++;

					var alignmentStart = i;

					while (i < format.Length && format[i] is >= '0' and <= '9')
						i++;

					if (i == alignmentStart)
						return null;

					SkipSpaces(format, ref i);
				}

				var end = i < format.Length && format[i] == ':' ? format.IndexOf('}', i) : i;

				if (end < 0 || end >= format.Length || format[end] != '}' || format.IndexOf('{', i, end - i) >= 0)
					return null;

				(items ??= new()).Add(new FormatItem(start, end - start + 1, index, indexStart, indexLength, hasAlignment));

				i = end;
			}

			return items ?? (IReadOnlyList<FormatItem>)Array.Empty<FormatItem>();
		}

		static void SkipSpaces(string format, ref int i)
		{
			while (i < format.Length && format[i] == ' ')
				i++;
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
			return CreateExpression(Expression.Constant(format), arguments);
		}

		/// <param name="format">Expression for the format, e.g. read from a <see cref="RawSqlString"/>.</param>
		/// <param name="arguments">Expression for the arguments array.</param>
		internal static MethodCallExpression CreateExpression(Expression format, Expression arguments)
		{
			return Expression.Call(null, Methods.System.FormattableStringFactory_Create, format, arguments);
		}

		/// <param name="formattable"><see cref="FormattableString"/> expression.</param>
		/// <param name="fromCreate"><see langword="true"/> when <paramref name="formattable"/> is a <c>FormattableStringFactory.Create</c> call, with any arguments array expression.</param>
		/// <returns>The arguments array of the <c>FormattableStringFactory.Create</c> call, or a <see cref="FormattableString.GetArguments"/> call.</returns>
		internal static Expression GetArgumentsExpression(Expression formattable, out bool fromCreate)
		{
			if (formattable is MethodCallExpression { Arguments: [_, var arguments] } create && create.Method == Methods.System.FormattableStringFactory_Create)
			{
				fromCreate = true;
				return arguments;
			}

			fromCreate = false;
			return Expression.Call(formattable, Methods.System.FormattableString_GetArguments);
		}

		/// <summary>
		/// Returns <c>FormattableStringFactory.Create(format, new object[] { ... })</c> expression for <paramref name="value"/>, with arguments as query parameters.
		/// </summary>
		public static MethodCallExpression CreateExpression(FormattableString value)
		{
			return CreateExpression(value.Format, CreateArgumentsExpression(value.GetArguments()));
		}

		/// <param name="arguments">Expression for <paramref name="value"/>'s arguments, read instead of baking their values.</param>
		/// <param name="readNulls">Read <see langword="null"/> arguments too, when nothing keys the build on <paramref name="arguments"/>.</param>
		internal static MethodCallExpression CreateExpression(FormattableString value, Expression arguments, bool readNulls)
		{
			return CreateExpression(value.Format, CreateArgumentsExpression(value.GetArguments(), arguments, readNulls));
		}

		/// <summary>
		/// Returns <c>new object[] { ... }</c> expression for <paramref name="arguments"/>, with every non-null argument as query parameter.
		/// </summary>
		public static NewArrayExpression CreateArgumentsExpression(object?[] arguments)
		{
			return CreateArgumentsExpression(arguments, null, false);
		}

		static NewArrayExpression CreateArgumentsExpression(object?[] arguments, Expression? source, bool readNulls)
		{
			return Expression.NewArrayInit(typeof(object), arguments.Select((p, i) =>
			{
				if (p == null)
				{
					return readNulls
						? Expression.ArrayIndex(source!, ExpressionInstances.Int32(i))
						: Expression.Constant(null, typeof(object));
				}

				var value           = source == null || p is ISqlExpression
					? (Expression)Expression.Constant(p, p.GetType())
					: Expression.Convert(Expression.ArrayIndex(source, ExpressionInstances.Int32(i)), p.GetType());
				var valueExpression = SequenceHelper.WrapAsParameter(value);
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
		/// <param name="argumentsEqual">Compares the two <see cref="FormattableString.GetArguments"/> arrays (called once, with the whole arrays).</param>
		public static bool AreEqual(FormattableString value1, FormattableString value2, Func<object?, object?, bool> argumentsEqual)
		{
			return string.Equals(value1.Format, value2.Format, StringComparison.Ordinal) && argumentsEqual(value1.GetArguments(), value2.GetArguments());
		}

		/// <summary>
		/// Returns hash code of <see cref="FormattableString"/> value, consistent with <see cref="AreEqual"/>.
		/// </summary>
		/// <param name="argumentsHashCode">Hashes the <see cref="FormattableString.GetArguments"/> array (called once, with the whole array).</param>
		public static int ComputeHashCode(FormattableString value, Func<object?, int> argumentsHashCode)
		{
			return StringComparer.Ordinal.GetHashCode(value.Format) * 397 + argumentsHashCode(value.GetArguments());
		}

		/// <summary>
		/// Splits raw SQL argument of <c>FromSql</c> / <c>Sql.Expr</c> into the format and argument expressions.
		/// When the format has format items, an argument none of them references is replaced with <see langword="null"/>,
		/// unless it can hold a <see cref="DataParameter"/> (referenced by name or position instead),
		/// is an <see cref="ISqlExpression"/>, or <paramref name="keepUnreferenced"/> is set.
		/// </summary>
		/// <param name="formatArg"><c>FormattableStringFactory.Create(format, new object[] { ... })</c> expression, or constant <see cref="FormattableString"/>.</param>
		/// <param name="keepUnreferenced">Keep unreferenced arguments, e.g. when the provider binds parameters by position.</param>
		/// <param name="format">Composite format of raw SQL.</param>
		/// <param name="arguments">Argument expressions.</param>
		public static void PrepareRawSqlArguments(Expression formatArg, bool keepUnreferenced, out string format, out IReadOnlyList<Expression> arguments)
		{
			// ExposeExpressionVisitor brings every FromSql / Sql.Expr overload to the Create shape, except a constant it leaves as is
			if (formatArg is ConstantExpression { Value: FormattableString formattable })
				formatArg = CreateExpression(formattable);

			if (!TrySplit(formatArg, out var formatExpr, out var argumentsExpr))
				throw new LinqToDBException($"Unexpected raw SQL expression '{formatArg}'.");

			format    = formatExpr.EvaluateExpression<string>()!;
			arguments = argumentsExpr.Expressions;

			if (!keepUnreferenced)
				arguments = ReplaceUnreferencedArguments(format, arguments);
		}

		// an argument is built into SQL even when the format does not reference it, registering a parameter
		// some providers (Sybase ASE) reject; a format without items binds its arguments by name or position
		static IReadOnlyList<Expression> ReplaceUnreferencedArguments(string format, IReadOnlyList<Expression> arguments)
		{
			if (arguments.Count == 0)
				return arguments;

			var items = ParseFormatItems(format);

			if (items == null || items.Count == 0)
				return arguments;

			var referenced = new bool[arguments.Count];

			foreach (var item in items)
				if (item.Index < referenced.Length)
					referenced[item.Index] = true;

			Expression[]? result = null;

			for (var i = 0; i < arguments.Count; i++)
			{
				if (referenced[i])
					continue;

				var type = arguments[i].UnwrapConvertToObject()!.Type;

				if (typeof(DataParameter).IsSameOrParentOf(type) || type.IsSameOrParentOf(typeof(DataParameter)) || typeof(ISqlExpression).IsAssignableFrom(type))
					continue;

				result ??= arguments.ToArray();
				result[i] = Expression.Constant(null, typeof(object));
			}

			return result ?? arguments;
		}
	}
}
