using System.Globalization;
using System.Linq;

namespace LinqToDB.Internal.SqlQuery
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
	public sealed class SqlExpression : SqlParameterizedExpressionBase
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
	{
		private const int                       DefaultPrecedence  = LinqToDB.SqlQuery.Precedence.Unknown;
		private const SqlFlags                  DefaultFlags       = SqlFlags.IsPure;
		private const ParametersNullabilityType DefaultNullability = ParametersNullabilityType.Undefined;

		public SqlExpression(DbDataType type, string expr, params ISqlExpression[] parameters)
			: this(type, expr, DefaultPrecedence, DefaultFlags, DefaultNullability, null, parameters)
		{
		}

		public SqlExpression(DbDataType type, string expr, int precedence, params ISqlExpression[] parameters)
			: this(type, expr, precedence, DefaultFlags, DefaultNullability, null, parameters)
		{
		}

		public SqlExpression(DbDataType type, string expr, int precedence, ParametersNullabilityType nullabilityType, params ISqlExpression[] parameters)
			: this(type, expr, precedence, DefaultFlags, nullabilityType, null, parameters)
		{
		}

		public SqlExpression(DbDataType type, string expr, int precedence, ParametersNullabilityType nullabilityType, bool? canBeNull, params ISqlExpression[] parameters)
			: this(type, expr, precedence, DefaultFlags, nullabilityType, canBeNull, parameters)
		{
		}

		public SqlExpression(DbDataType type, string expr, int precedence, SqlFlags flags, params ISqlExpression[] parameters)
			: this(type, expr, precedence, flags, DefaultNullability, null, parameters)
		{
		}

		public SqlExpression(DbDataType type, string expr, int precedence, SqlFlags flags, bool? canBeNull, params ISqlExpression[] parameters)
			: this(type, expr, precedence, flags, DefaultNullability, canBeNull, parameters)
		{
		}

		public SqlExpression(DbDataType type, string expr, int precedence, SqlFlags flags, ParametersNullabilityType nullabilityType, params ISqlExpression[] parameters)
			: this(type, expr, precedence, flags, nullabilityType, null, parameters)
		{
		}

		public SqlExpression(
			DbDataType                type,
			string                    expr,
			int                       precedence,
			SqlFlags                  flags,
			ParametersNullabilityType nullabilityType,
			bool?                     canBeNull,
			params ISqlExpression[]   parameters)
			: base(type, expr, precedence, flags, nullabilityType, canBeNull, parameters)
		{
		}

		public string Expr => ExprOrName;

		#region IQueryElement Members

		public override QueryElementType ElementType => QueryElementType.SqlExpression;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.DebugAppendUniqueId(this);

			var len = writer.Length;
			var arguments  = Parameters.Select(p =>
			{
				p.ToString(writer);
				var s = writer.ToString(len, writer.Length - len);
				writer.Length = len;
				return (object)s;
			});

			if (Parameters.Length == 0)
				return writer.Append(Expr);

			if (Expr.Contains('{'))
				return writer.AppendFormat(Expr, arguments.ToArray());

			return writer
				.Append(Expr)
				.Append('{')
				.Append(string.Join(", ", arguments.Select(s => string.Format(CultureInfo.InvariantCulture, "{0}", s))))
				.Append('}');
		}

		#endregion
	}
}
