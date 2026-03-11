using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Untyped (doesn't have type of type information should be hidden from Linq To DB) SQL fragment with parameters.
	/// </summary>
	public sealed class SqlFragment : SqlExpressionBase
	{
		private const int DefaultPrecedence = LinqToDB.SqlQuery.Precedence.Primary;

		public SqlFragment(string expr, params ISqlExpression[] parameters)
			: this(expr, DefaultPrecedence, parameters)
		{
		}

		public SqlFragment(string expr, int precedence, params ISqlExpression[] parameters)
		{
			Expr = expr;
			Precedence = precedence;
			Parameters = parameters;
		}

		public string Expr { get; }
		public ISqlExpression[] Parameters { get; }

		#region Overrides

		public override QueryElementType ElementType => QueryElementType.SqlFragment;

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

			if (Expr.Contains('{', StringComparison.Ordinal))
				return writer.AppendFormat(Expr, arguments.ToArray());

			return writer
				.Append(Expr)
				.Append('{')
				.Append(string.Join(", ", arguments.Select(s => string.Format(CultureInfo.InvariantCulture, "{0}", s))))
				.Append('}');
		}

		public override string ToString()
		{
			return this.ToDebugString();
		}

		#endregion

		#region ISqlExpression

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(Expr);
			hash.Add(Precedence);
			foreach (var parameter in Parameters)
				hash.Add(parameter.GetElementHashCode());
			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (other is not SqlFragment expr
				|| Precedence != expr.Precedence
				|| !string.Equals(Expr, expr.Expr, StringComparison.Ordinal) || Parameters.Length != expr.Parameters.Length)
			{
				return false;
			}

			for (var i = 0; i < Parameters.Length; i++)
			{
				if (!Parameters[i].Equals(expr.Parameters[i], comparer))
					return false;
			}

			return comparer(this, expr);
		}

		public override bool CanBeNullable(NullabilityContext nullability) => false;

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlFragment(this);

		public override int Precedence { get; }
		public override Type? SystemType => null;
		#endregion
	}
}
