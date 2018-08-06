using System;
using System.Collections.Generic;
using System.Text;
using LinqToDB.Common;

namespace LinqToDB.SqlQuery
{
	public class SqlValue : ISqlExpression, IValueContainer
	{
		public SqlValue(Type systemType, object value)
		{
			ValueType  = new DbDataType(systemType);
			Value      = value;
		}

		public SqlValue(DbDataType valueType, object value)
		{
			ValueType  = valueType;
			Value      = value;
		}

		public SqlValue(object value)
		{
			Value = value;

			if (value != null)
				ValueType = new DbDataType(value.GetType());
		}

		public   object     Value      { get; internal set; }
		public   DbDataType ValueType  { get; set; }
		public   Type       SystemType => ValueType.SystemType;

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region ISqlExpression Members

		public int Precedence => SqlQuery.Precedence.Primary;

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

			return
				other is SqlValue value        &&
				SystemType == value.SystemType &&
				(Value == null && value.Value == null || Value != null && Value.Equals(value.Value));
		}

		#endregion

		#region ISqlExpression Members

		public bool CanBeNull => Value == null;

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

			if (!objectTree.TryGetValue(this, out var clone))
				objectTree.Add(this, clone = new SqlValue(ValueType, Value));

			return clone;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.SqlValue;

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
