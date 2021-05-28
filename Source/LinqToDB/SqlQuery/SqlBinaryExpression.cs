﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LinqToDB.SqlQuery
{
	[Serializable, DebuggerDisplay("SQL = {" + nameof(SqlText) + "}")]
	public class SqlBinaryExpression : ISqlExpression
	{
		public SqlBinaryExpression(Type systemType, ISqlExpression expr1, string operation, ISqlExpression expr2, int precedence)
		{
			_expr1     = expr1     ?? throw new ArgumentNullException(nameof(expr1));
			Operation  = operation ?? throw new ArgumentNullException(nameof(operation));
			_expr2     = expr2     ?? throw new ArgumentNullException(nameof(expr2));
			SystemType = systemType;
			Precedence = precedence;
		}

		public SqlBinaryExpression(Type systemType, ISqlExpression expr1, string operation, ISqlExpression expr2)
			: this(systemType, expr1, operation, expr2, SqlQuery.Precedence.Unknown)
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

		public Type           SystemType { get; }
		public int            Precedence { get; }

		#region Overrides

		public string SqlText => ToString()!;

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
		{
			Expr1 = Expr1.Walk(options, func)!;
			Expr2 = Expr2.Walk(options, func)!;

			return func(this);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			return Equals(other, SqlExpression.DefaultComparer);
		}

		#endregion

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

		public bool CanBeNull => Expr1.CanBeNull || Expr2.CanBeNull;

		public bool Equals(ISqlExpression? other, Func<ISqlExpression,ISqlExpression,bool> comparer)
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

		public QueryElementType ElementType => QueryElementType.SqlBinaryExpression;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			Expr1
				.ToString(sb, dic)
				.Append(' ')
				.Append(Operation)
				.Append(' ');

			return Expr2.ToString(sb, dic);
		}

		#endregion

		public void Deconstruct(out ISqlExpression expr1, out string operation, out ISqlExpression expr2)
		{
			expr1     = Expr1;
			operation = Operation;
			expr2     = Expr2;
		}
	}
}
