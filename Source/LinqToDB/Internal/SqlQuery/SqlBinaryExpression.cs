using System;

using LinqToDB.Common;

namespace LinqToDB.Internal.SqlQuery
{
	public class SqlBinaryExpression : SqlExpressionBase
	{
		public SqlBinaryExpression(DbDataType dbDataType, ISqlExpression expr1, string operation, ISqlExpression expr2, int precedence = LinqToDB.SqlQuery.Precedence.Unknown)
		{
			_expr1     = expr1     ?? throw new ArgumentNullException(nameof(expr1));
			Operation  = operation ?? throw new ArgumentNullException(nameof(operation));
			_expr2     = expr2     ?? throw new ArgumentNullException(nameof(expr2));
			Type       = dbDataType;
			Precedence = precedence;
		}

		public SqlBinaryExpression(Type systemType, ISqlExpression expr1, string operation, ISqlExpression expr2, int precedence = LinqToDB.SqlQuery.Precedence.Unknown)
			: this(new DbDataType(systemType), expr1, operation, expr2, precedence)
		{
		}

		private ISqlExpression _expr1;

		public ISqlExpression Expr1
		{
			get => _expr1;
			internal set
			{
				_expr1    = value;
				_hashCode = null;
			}
		}

		public string         Operation  { get; }

		private ISqlExpression _expr2;

		public ISqlExpression Expr2
		{
			get => _expr2;
			internal set
			{
				_expr2    = value;
				_hashCode = null;
			}
		}

		public override QueryElementType ElementType => QueryElementType.SqlBinaryExpression;

		public DbDataType Type { get; }

		public override Type SystemType => Type.SystemType;
		public override int  Precedence { get; }

		int?                   _hashCode;

		public override int GetHashCode()
		{
			// ReSharper disable NonReadonlyMemberInGetHashCode
			if (_hashCode.HasValue)
				return _hashCode.Value;

			var hashCode = Operation.GetHashCode();

			hashCode = unchecked(hashCode + (hashCode * 397) ^ SystemType.GetHashCode());
			hashCode = unchecked(hashCode + (hashCode * 397) ^ Expr1.GetHashCode());
			hashCode = unchecked(hashCode + (hashCode * 397) ^ Expr2.GetHashCode());

			_hashCode = hashCode;
			return hashCode;
			// ReSharper restore NonReadonlyMemberInGetHashCode
		}

		#region ISqlExpression Members

		public override bool CanBeNullable(NullabilityContext nullability) => Expr1.CanBeNullable(nullability) || Expr2.CanBeNullable(nullability);

		public override bool Equals(ISqlExpression? other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			if (this == other)
				return true;

			return
				other is SqlBinaryExpression expr  &&
				Operation  == expr.Operation       &&
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

		#endregion

		public void Deconstruct(out Type systemType, out ISqlExpression expr1, out string operation, out ISqlExpression expr2)
		{
			systemType = SystemType;
			expr1      = Expr1;
			operation  = Operation;
			expr2      = Expr2;
		}

		public void Deconstruct(out ISqlExpression expr1, out string operation, out ISqlExpression expr2)
		{
			expr1     = Expr1;
			operation = Operation;
			expr2     = Expr2;
		}
	}
}
