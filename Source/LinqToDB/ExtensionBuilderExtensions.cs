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
			return new SqlUnaryExpression(type, expr, SqlUnaryOperation.Negation, Precedence.Bitwise);
		}
	}
}
