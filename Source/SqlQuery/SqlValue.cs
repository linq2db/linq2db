using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlValue : ISqlExpression, IValueContainer
	{
		public SqlValue(Type systemType, object value)
		{
			SystemType = systemType;
			Value      = value;
		}

		public SqlValue(object value)
		{
			Value = value;

			if (value != null)
				SystemType = value.GetType();
		}

		public   object    Value      { get; internal set; }
		public   Type      SystemType { get; private set; }
		internal DataType? DataType   { get; set; }

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region ISqlExpression Members

		public int Precedence
		{
			get { return SqlQuery.Precedence.Primary; }
		}

		#endregion

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			return func(this);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression other)
		{
			if (this == other)
				return true;

			var value = other as SqlValue;
			return
				value       != null              &&
				SystemType == value.SystemType &&
				(Value == null && value.Value == null || Value != null && Value.Equals(value.Value));
		}

		#endregion

		#region ISqlExpression Members

		public bool CanBeNull
		{
			get { return Value == null; }
		}

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return ((ISqlExpression)this).Equals(other) && comparer(this, other);
		}

		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			ICloneableElement clone;

			if (!objectTree.TryGetValue(this, out clone))
				objectTree.Add(this, clone = new SqlValue(SystemType, Value));

			return clone;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType { get { return QueryElementType.SqlValue; } }

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			return 
				Value == null ?
					sb.Append("NULL") :
				Value is string ?
					sb
						.Append('\'')
						.Append(Value.ToString().Replace("\'", "''"))
						.Append('\'')
				:
					sb.Append(Value);
		}

		#endregion
	}
}
