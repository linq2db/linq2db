using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LinqToDB.SqlQuery
{
	[Serializable, DebuggerDisplay("SQL = {SqlText}")]
	public class SqlBinaryExpression : ISqlExpression
	{
		public SqlBinaryExpression(Type systemType, ISqlExpression expr1, string operation, ISqlExpression expr2, int precedence)
		{
			if (expr1     == null) throw new ArgumentNullException("expr1");
			if (operation == null) throw new ArgumentNullException("operation");
			if (expr2     == null) throw new ArgumentNullException("expr2");

			Expr1      = expr1;
			Operation  = operation;
			Expr2      = expr2;
			SystemType = systemType;
			Precedence = precedence;
		}

		public SqlBinaryExpression(Type systemType, ISqlExpression expr1, string operation, ISqlExpression expr2)
			: this(systemType, expr1, operation, expr2, SqlQuery.Precedence.Unknown)
		{
		}

		public ISqlExpression Expr1      { get; internal set; }
		public string         Operation  { get; private set;  }
		public ISqlExpression Expr2      { get; internal set; }
		public Type           SystemType { get; private set;  }
		public int            Precedence { get; private set;  }

		#region Overrides

		public string SqlText { get { return ToString(); } }

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			Expr1 = Expr1.Walk(skipColumns, func);
			Expr2 = Expr2.Walk(skipColumns, func);

			return func(this);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression other)
		{
			return Equals(other, SqlExpression.DefaultComparer);
		}

		#endregion

		#region ISqlExpression Members

		public bool CanBeNull()
		{
			return Expr1.CanBeNull() || Expr2.CanBeNull();
		}

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			if (this == other)
				return true;

			var expr = other as SqlBinaryExpression;

			return
				expr        != null                &&
				Operation  == expr.Operation       &&
				SystemType == expr.SystemType      &&
				Expr1.Equals(expr.Expr1, comparer) &&
				Expr2.Equals(expr.Expr2, comparer) &&
				comparer(this, other);
		}

		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			ICloneableElement clone;

			if (!objectTree.TryGetValue(this, out clone))
			{
				objectTree.Add(this, clone = new SqlBinaryExpression(
					SystemType,
					(ISqlExpression)Expr1.Clone(objectTree, doClone),
					Operation,
					(ISqlExpression)Expr2.Clone(objectTree, doClone),
					Precedence));
			}

			return clone;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType { get { return QueryElementType.SqlBinaryExpression; } }

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
	}
}
