using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace LinqToDB.SqlQuery
{
	using Common;

	public class SqlValue : ISqlExpression, IValueContainer
	{
		public SqlValue(Type systemType, object? value)
		{
			ValueType  = new DbDataType(systemType);
			Value      = value;
		}

		public SqlValue(DbDataType valueType, object? value)
		{
			ValueType  = valueType;
			Value      = value;
		}

		public SqlValue(object value)
		{
			Value     = value ?? throw new ArgumentNullException("Untyped null value");
			ValueType = new DbDataType(value.GetType());
		}

		object? _value;
		
		public object? Value
		{
			get => _value;
			internal set
			{
				if (_value == value)
					return;
				
				_value    = value;
				_hashCode = null;
			}
		}

		DbDataType _valueType;
		
		public DbDataType ValueType
		{
			get => _valueType;
			set
			{
				if (_valueType == value)
					return;
				_valueType = value;
				_hashCode  = null;
			}
		}

		Type ISqlExpression.SystemType => ValueType.SystemType;

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

		ISqlExpression ISqlExpressionWalkable.Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
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
				other is SqlValue value           &&
				ValueType.Equals(value.ValueType) &&
				(Value == null && value.Value == null || Value != null && Value.Equals(value.Value));
		}

		int? _hashCode;

		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		public override int GetHashCode()
		{
			if (_hashCode.HasValue)
				return _hashCode.Value;

			var hashCode = 17;

			hashCode = unchecked(hashCode + (hashCode * 397) ^ ValueType.GetHashCode());

			if (Value != null)
				hashCode = unchecked(hashCode + (hashCode * 397) ^ Value.GetHashCode());

			_hashCode = hashCode;
			return hashCode;
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
