using System;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB
{
	public static class ExtensionBuilderExtensions
	{
		public static Sql.SqlExtensionParam AddParameter(this Sql.ISqExtensionBuilder builder, string name, string value)
		{
			return builder.AddParameter(name, new SqlValue(value));
		}

		public static Sql.SqlExtensionParam AddExpression(this Sql.ISqExtensionBuilder builder, string name, string expr)
		{
			return builder.AddParameter(name, new SqlExpression(expr, Precedence.Primary));
		}

		public static ISqlExpression Add(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right, Type type)
		{
			return new SqlBinaryExpression(type, left, "+", right, Precedence.Additive);
		}
		public static ISqlExpression Add<T>(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right)
		{
			return builder.Add(left, right, typeof(T));
		}

		public static ISqlExpression Add(this Sql.ISqExtensionBuilder builder, ISqlExpression left, int value)
		{
			return builder.Add<int>(left, new SqlValue(value));
		}

		public static ISqlExpression Inc(this Sql.ISqExtensionBuilder builder, ISqlExpression expr)
		{
			return builder.Add(expr, 1);
		}

		public static ISqlExpression Sub(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right, Type type)
		{
			return new SqlBinaryExpression(type, left, "-", right, Precedence.Subtraction);
		}

		public static ISqlExpression Sub<T>(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right)
		{
			return builder.Sub(left, right, typeof(T));
		}

		public static ISqlExpression Sub(this Sql.ISqExtensionBuilder builder, ISqlExpression left, int value)
		{
			return builder.Sub<int>(left, new SqlValue(value));
		}

		public static ISqlExpression Dec(this Sql.ISqExtensionBuilder builder, ISqlExpression expr)
		{
			return builder.Sub(expr, 1);
		}

		public static ISqlExpression Mul(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right, Type type)
		{
			return new SqlBinaryExpression(type, left, "*", right, Precedence.Multiplicative);
		}

		public static ISqlExpression Mul<T>(this Sql.ISqExtensionBuilder builder, ISqlExpression left, ISqlExpression right)
		{
			return builder.Mul(left, right, typeof(T));
		}

		public static ISqlExpression Mul(this Sql.ISqExtensionBuilder builder, ISqlExpression expr1, int value)
		{
			return builder.Mul<int>(expr1, new SqlValue(value));
		}

		public static ISqlExpression Div(this Sql.ISqExtensionBuilder builder, ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return new SqlBinaryExpression(type, expr1, "/", expr2, Precedence.Multiplicative);
		}

		public static ISqlExpression Div<T>(this Sql.ISqExtensionBuilder builder, ISqlExpression expr1, ISqlExpression expr2)
		{
			return builder.Div(expr1, expr2, typeof(T));
		}

		public static ISqlExpression Div(this Sql.ISqExtensionBuilder builder, ISqlExpression expr1, int value)
		{
			return builder.Div<int>(expr1, new SqlValue(value));
		}
	}
}
