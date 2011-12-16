using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.Sql
{
	public class SqlValue : ISqlExpression, IValueContainer
	{
		public SqlValue(Type systemType, object value)
		{
			_systemType = systemType;
			_value      = value;
		}

		public SqlValue(object value)
		{
			_value = value;

			if (value != null)
				_systemType = value.GetType();
		}

		readonly object _value;      public object  Value      { get { return _value;      } }
		readonly Type   _systemType; public Type    SystemType { get { return _systemType; } }

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
			get { return LinqToDB.Sql.Precedence.Primary; }
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
				_systemType == value._systemType &&
				(_value == null && value._value == null || _value != null && _value.Equals(value._value));
		}

		#endregion

		#region ISqlExpression Members

		public bool CanBeNull()
		{
			return Value == null;
		}

		#endregion

		#region ISqlExpression Members

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			ICloneableElement clone;

			if (!objectTree.TryGetValue(this, out clone))
				objectTree.Add(this, clone = new SqlValue(_systemType, _value));

			return clone;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType { get { return QueryElementType.SqlValue; } }

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			return 
				_value == null ?
					sb.Append("NULL") :
				_value is string ?
					sb
						.Append('\'')
						.Append(_value.ToString().Replace("\'", "''"))
						.Append('\'')
				:
					sb.Append(_value);
		}

		#endregion
	}
}
