using System;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.SqlQuery
{
	using Common;
	using Common.Internal;

	public class SqlValue : ISqlExpression
	{
		public SqlValue(Type systemType, object? value)
		{
			_valueType = new DbDataType(value != null && value is not DBNull ? systemType.UnwrapNullableType() : systemType);
			Value      = value;
		}

		public SqlValue(DbDataType valueType, object? value)
		{
			_valueType    = valueType;
			Value         = value;
		}

		public SqlValue(object value)
		{
			Value         = value ?? throw new ArgumentNullException(nameof(value), "Untyped null value");
			_valueType    = new DbDataType(value.GetType());
		}

		/// <summary>
		/// Provider specific value
		/// </summary>
		public object? Value { get; }

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
			return this.ToDebugString();
		}

#endif

		#endregion

		#region ISqlExpression Members

		public int Precedence => SqlQuery.Precedence.Primary;

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
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

		public bool CanBeNullable(NullabilityContext nullability) => CanBeNull;

		public bool CanBeNull => Value == null;

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return ((ISqlExpression)this).Equals(other) && comparer(this, other);
		}

		#endregion

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif

		public QueryElementType ElementType => QueryElementType.SqlValue;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			return
				Value == null ?
					writer.Append("NULL") :
				Value is string strVal ?
					writer
						.Append('\'')
						.Append(strVal.Replace("\'", "''"))
						.Append('\'')
				:
					writer.Append(Value);
		}

		#endregion

		public void Deconstruct(out object? value)
		{
			value = Value;
		}
	}
}
