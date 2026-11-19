using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlBinaryExpression : SqlExpressionBase
	{
		public SqlBinaryExpression(DbDataType dbDataType, ISqlExpression expr1, string operation, ISqlExpression expr2)
		{
			Expr1      = expr1     ?? throw new ArgumentNullException(nameof(expr1));
			Operation  = operation ?? throw new ArgumentNullException(nameof(operation));
			Expr2      = expr2     ?? throw new ArgumentNullException(nameof(expr2));
			Type       = dbDataType;
		}

		public SqlBinaryExpression(Type systemType, ISqlExpression expr1, string operation, ISqlExpression expr2)
			: this(new DbDataType(systemType), expr1, operation, expr2)
		{
		}

		public ISqlExpression Expr1
		{
			get;
			internal set
			{
				field    = value;
				_hashCode = null;
			}
		}

		public string         Operation  { get; }

		public ISqlExpression Expr2
		{
			get;
			internal set
			{
				field = value;
				_hashCode = null;
			}
		}

		public override QueryElementType ElementType => QueryElementType.SqlBinaryExpression;

		public DbDataType Type { get; }

		public override Type SystemType => Type.SystemType;

		/// <summary>
		/// The class of this operator on the <see cref="LinqToDB.SqlQuery.Precedence"/> scale. Dialect precedence is
		/// decided by the SQL builder's precedence table, not by this value.
		/// </summary>
		public override int  Precedence => Operation switch
		{
			"+" or "-"                                         => LinqToDB.SqlQuery.Precedence.Additive,
			"*" or "/" or "%" or "//" or "MOD" or "DIV"        => LinqToDB.SqlQuery.Precedence.Multiplicative,
			"&" or "|" or "^" or "#" or "BAND" or "BOR" or "BXOR" => LinqToDB.SqlQuery.Precedence.Bitwise,
			"||"                                               => LinqToDB.SqlQuery.Precedence.Concatenate,
			_                                                  => LinqToDB.SqlQuery.Precedence.Unknown,
		};

		int?                   _hashCode;

		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		public override int GetHashCode()
		{
			return _hashCode ??= HashCode.Combine(
				Operation,
				SystemType,
				Expr1,
				Expr2
			);
		}

		#region ISqlExpression Members

		public override bool CanBeNullable(NullabilityContext nullability) => Expr1.CanBeNullable(nullability) || Expr2.CanBeNullable(nullability);

		public override bool Equals(ISqlExpression? other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			if (this == other)
				return true;

			return
				other is SqlBinaryExpression expr  &&
string.Equals(Operation, expr.Operation, StringComparison.Ordinal) &&
				SystemType == expr.SystemType      &&
				Expr1.Equals(expr.Expr1, comparer) &&
				Expr2.Equals(expr.Expr2, comparer) &&
				comparer(this, other);
		}

		#endregion

		#region IQueryElement Members

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				//.DebugAppendUniqueId(this)
				.AppendElement(Expr1)
				.Append(' ')
				.Append(Operation)
				.Append(' ')
				.AppendElement(Expr2);

			return writer;
		}

		public override int GetElementHashCode()
		{
			return HashCode.Combine(
				Operation,
				SystemType,
				Expr1.GetElementHashCode(),
				Expr2.GetElementHashCode()
			);
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlBinaryExpression(this);

		#endregion

		public void Deconstruct(out DbDataType type, out ISqlExpression expr1, out string operation, out ISqlExpression expr2)
		{
			type      = Type;
			expr1     = Expr1;
			operation = Operation;
			expr2     = Expr2;
		}

		public void Deconstruct(out ISqlExpression expr1, out string operation, out ISqlExpression expr2)
		{
			expr1     = Expr1;
			operation = Operation;
			expr2     = Expr2;
		}
	}
}
