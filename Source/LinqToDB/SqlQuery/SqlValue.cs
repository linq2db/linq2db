using System;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Common;

namespace LinqToDB.SqlQuery
{
	public class SqlValue : SqlExpressionBase
	{
		public SqlValue(Type systemType, object? value)
		{
			_valueType = new DbDataType(value != null && value is not DBNull ? systemType.UnwrapNullableType() : systemType);
			Value      = value;
		}

		public SqlValue(DbDataType valueType, object? value)
		{
			_valueType = valueType;
			Value      = value;
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
			}
		}

		#region Overrides

		public override QueryElementType ElementType => QueryElementType.SqlValue;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.DebugAppendUniqueId(this);

			if (Value is null)
			{
				writer.Append("NULL");
			}
			else
			{
				if (Value is string strVal)
				{
					writer
						.Append('\'')
						.Append(strVal.Replace("\'", "''"))
						.Append('\'');
				}
				else
				{
					writer.Append(Value);
				}
			}

			return writer;
		}

		public override int GetElementHashCode()
		{
			return HashCode.Combine(
				ValueType,
				Value
			);
		}

		public override int   Precedence => SqlQuery.Precedence.Primary;
		public override Type? SystemType => ValueType.SystemType;

		public override bool CanBeNullable(NullabilityContext nullability) => CanBeNull;

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (this == other)
				return true;

			return
				other is SqlValue value          
				&& ValueType.Equals(value.ValueType)
				&& (Value == null && value.Value == null || Value != null && Value.Equals(value.Value))
				&& comparer(this, other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(
				ValueType,
				Value
			);
		}

		#endregion

		public bool CanBeNull => Value == null;

		public void Deconstruct(out object? value)
		{
			value = Value;
		}
	}
}
