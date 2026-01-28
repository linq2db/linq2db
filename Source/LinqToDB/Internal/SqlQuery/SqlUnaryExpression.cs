using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlUnaryExpression : SqlExpressionBase
	{
		public SqlUnaryExpression(DbDataType dbDataType, ISqlExpression expr, SqlUnaryOperation operation, int precedence = LinqToDB.SqlQuery.Precedence.Unknown)
		{
			Expr       = expr      ?? throw new ArgumentNullException(nameof(expr));
			Operation  = operation;
			Type       = dbDataType;
			Precedence = precedence;
		}

		public SqlUnaryExpression(Type systemType, ISqlExpression expr, SqlUnaryOperation operation, int precedence = LinqToDB.SqlQuery.Precedence.Unknown)
			: this(new DbDataType(systemType), expr, operation, precedence)
		{
		}

		public ISqlExpression Expr
		{
			get;
			internal set
			{
				field    = value;
				_hashCode = null;
			}
		}

		public SqlUnaryOperation Operation  { get; }

		public override QueryElementType ElementType => QueryElementType.SqlUnaryExpression;

		public DbDataType Type { get; }

		public override Type SystemType => Type.SystemType;
		public override int  Precedence { get; }

		int?                   _hashCode;

		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		public override int GetHashCode()
		{
			return _hashCode ??= HashCode.Combine(
				Operation,
				SystemType,
				Expr
			);
		}

		#region ISqlExpression Members

		public override bool CanBeNullable(NullabilityContext nullability) => Expr.CanBeNullable(nullability);

		public override bool Equals(ISqlExpression? other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			if (this == other)
				return true;

			return
				other is SqlUnaryExpression expr &&
				Operation  == expr.Operation     &&
				SystemType == expr.SystemType    &&
				Expr.Equals(expr.Expr, comparer) &&
				comparer(this, other);
		}

		#endregion

		#region IQueryElement Members

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.Append(Operation)
				.Append(' ')
				.AppendElement(Expr);

			return writer;
		}

		public override int GetElementHashCode()
		{
			return HashCode.Combine(
				Operation,
				SystemType,
				Expr.GetElementHashCode()
			);
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlUnaryExpression(this);

		#endregion

		public void Deconstruct(out DbDataType type, out ISqlExpression expr, out SqlUnaryOperation operation)
		{
			type      = Type;
			expr      = Expr;
			operation = Operation;
		}

		public void Deconstruct(out ISqlExpression expr, out SqlUnaryOperation operation)
		{
			expr      = Expr;
			operation = Operation;
		}
	}
}
