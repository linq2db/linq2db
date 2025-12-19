using System;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlValue : SqlExpressionBase
	{
		public SqlValue(Type systemType, object? value)
		{
			ValueType = new DbDataType(!value.IsNullValue() ? systemType.UnwrapNullableType() : systemType);
			Value     = value;
		}

		public SqlValue(DbDataType valueType, object? value)
		{
			ValueType = valueType;
			Value     = value;
		}

		public SqlValue(object value)
		{
			Value     = value ?? throw new ArgumentNullException(nameof(value), "Untyped null value");
			ValueType = new DbDataType(value.GetType());
		}

		/// <summary>
		/// Provider specific value
		/// </summary>
		public object? Value { get; }

		public DbDataType ValueType
		{
			get;
			set
			{
				if (field == value)
					return;
				field = value;
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
						.Append(strVal.Replace("\'", "''", StringComparison.Ordinal))
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

		public override int   Precedence => LinqToDB.SqlQuery.Precedence.Primary;
		public override Type? SystemType => ValueType.SystemType;

		public override bool CanBeNullable(NullabilityContext nullability) => Value == null;

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

		public void Deconstruct(out object? value)
		{
			value = Value;
		}
	}
}
