using System;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB
{
	public static class ExtensionBuilderExtensions
	{
		public static Sql.SqlExtensionParam AddParameter(this Sql.ISqlExtensionBuilder builder, string name, string value)
		{
			return builder.AddParameter(name, new SqlValue(value));
		}

		public static Sql.SqlExtensionParam AddFragment(this Sql.ISqlExtensionBuilder builder, string name, string expr)
		{
			return builder.AddParameter(name, new SqlFragment(expr));
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "builder is an extension point")]
		public static ISqlExpression Add(this Sql.ISqlExtensionBuilder builder, ISqlExpression left, ISqlExpression right, Type type)
		{
			if (type == typeof(string))
				throw new InvalidOperationException("String concatenation must use builder.Concat(...) so it produces SqlConcatExpression. builder.Add is for numeric / temporal arithmetic only.");

			return new SqlBinaryExpression(type, left, "+", right, Precedence.Additive);
		}

		public static ISqlExpression Add<T>(this Sql.ISqlExtensionBuilder builder, ISqlExpression left, ISqlExpression right)
		{
			return builder.Add(left, right, typeof(T));
		}

		public static ISqlExpression Add(this Sql.ISqlExtensionBuilder builder, ISqlExpression left, int value)
		{
			return builder.Add<int>(left, new SqlValue(value));
		}

		/// <summary>
		/// Builds a strict-null <c>SqlConcatExpression</c> (any-NULL operand → NULL result).
		/// Use this from <c>Sql.IExtensionCallBuilder</c> implementations instead of building
		/// a binary <c>+</c> on string-typed operands.
		/// </summary>
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "builder is an extension point")]
		public static ISqlExpression Concat(this Sql.ISqlExtensionBuilder builder, ISqlExpression x, ISqlExpression y)
		{
			return new SqlConcatExpression(true, x, y);
		}

		/// <summary>
		/// Builds a strict-null <c>SqlConcatExpression</c> over <c>expressions</c>
		/// (any-NULL operand → NULL result).
		/// </summary>
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "builder is an extension point")]
		public static ISqlExpression Concat(this Sql.ISqlExtensionBuilder builder, params ISqlExpression[] expressions)
		{
			if (expressions.Length == 0)
				throw new InvalidOperationException("At least one expression must be provided for concatenation.");

			return new SqlConcatExpression(true, expressions);
		}

		/// <summary>
		/// Builds a <c>SqlConcatExpression</c> with the specified <c>preserveNull</c>
		/// semantic — preserveNull enabled for strict any-NULL → NULL (e.g. <c>Sql.Concat</c>);
		/// preserveNull disabled for null-as-empty (operands wrapped in <c>Coalesce(.., '')</c> at the
		/// lowering layer; <c>string.Concat</c>).
		/// </summary>
		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "builder is an extension point")]
		public static ISqlExpression Concat(this Sql.ISqlExtensionBuilder builder, bool preserveNull, params ISqlExpression[] expressions)
		{
			if (expressions.Length == 0)
				throw new InvalidOperationException("At least one expression must be provided for concatenation.");

			return new SqlConcatExpression(preserveNull, expressions);
		}

		public static ISqlExpression Inc(this Sql.ISqlExtensionBuilder builder, ISqlExpression expr)
		{
			return builder.Add(expr, 1);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "builder is an extension point")]
		public static ISqlExpression Sub(this Sql.ISqlExtensionBuilder builder, ISqlExpression left, ISqlExpression right, Type type)
		{
			return new SqlBinaryExpression(type, left, "-", right, Precedence.Subtraction);
		}

		public static ISqlExpression Sub<T>(this Sql.ISqlExtensionBuilder builder, ISqlExpression left, ISqlExpression right)
		{
			return builder.Sub(left, right, typeof(T));
		}

		public static ISqlExpression Sub(this Sql.ISqlExtensionBuilder builder, ISqlExpression left, int value)
		{
			return builder.Sub<int>(left, new SqlValue(value));
		}

		public static ISqlExpression Dec(this Sql.ISqlExtensionBuilder builder, ISqlExpression expr)
		{
			return builder.Sub(expr, 1);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "builder is an extension point")]
		public static ISqlExpression Mul(this Sql.ISqlExtensionBuilder builder, ISqlExpression left, ISqlExpression right, Type type)
		{
			return new SqlBinaryExpression(type, left, "*", right, Precedence.Multiplicative);
		}

		public static ISqlExpression Mul<T>(this Sql.ISqlExtensionBuilder builder, ISqlExpression left, ISqlExpression right)
		{
			return builder.Mul(left, right, typeof(T));
		}

		public static ISqlExpression Mul(this Sql.ISqlExtensionBuilder builder, ISqlExpression expr1, int value)
		{
			return builder.Mul<int>(expr1, new SqlValue(value));
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "builder is an extension point")]
		public static ISqlExpression Div(this Sql.ISqlExtensionBuilder builder, ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return new SqlBinaryExpression(type, expr1, "/", expr2, Precedence.Multiplicative);
		}

		public static ISqlExpression Div<T>(this Sql.ISqlExtensionBuilder builder, ISqlExpression expr1, ISqlExpression expr2)
		{
			return builder.Div(expr1, expr2, typeof(T));
		}

		public static ISqlExpression Div(this Sql.ISqlExtensionBuilder builder, ISqlExpression expr1, int value)
		{
			return builder.Div<int>(expr1, new SqlValue(value));
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "builder is an extension point")]
		public static ISqlExpression BitNot(this Sql.ISqlExtensionBuilder builder, ISqlExpression expr, Type type)
		{
			return new SqlUnaryExpression(type, expr, SqlUnaryOperation.BitwiseNegation, Precedence.Bitwise);
		}

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "builder is an extension point")]
		public static ISqlExpression Negate(this Sql.ISqlExtensionBuilder builder, ISqlExpression expr, Type type)
		{
			return new SqlUnaryExpression(type, expr, SqlUnaryOperation.Negation, Precedence.Unary);
		}
	}
}
