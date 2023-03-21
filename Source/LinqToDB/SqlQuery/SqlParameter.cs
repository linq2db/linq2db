using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace LinqToDB.SqlQuery
{
	using Common;

	public class SqlParameter : ISqlExpression
	{
		public SqlParameter(DbDataType type, string? name, object? value)
		{
			IsQueryParameter = true;
			Name             = name;
			Type             = type;
			Value            = value;

#if DEBUG
			_paramNumber = ++_paramCounter;
#endif
		}

#if DEBUG
		readonly int _paramNumber;
		static   int _paramCounter;
#endif

		// meh, nullable...
		public string?    Name             { get; set; }
		public DbDataType Type             { get; set; }
		public bool       IsQueryParameter { get; set; }
		internal int?     AccessorId       { get; set; }

		Type ISqlExpression.SystemType => Type.SystemType;

		public object?    Value            { get; }

		public object? CorrectParameterValue(object? rawValue)
		{
			var value = rawValue;

			var valueConverter = ValueConverter;
			return valueConverter == null ? value : valueConverter(value);
		}

		#region Value Converter

		internal List<int>? TakeValues;

		private Func<object?, object?>? _valueConverter;
		public  Func<object?, object?>?  ValueConverter
		{
			get
			{
				if (_valueConverter == null && TakeValues != null)
					foreach (var take in TakeValues.ToArray())
						SetTakeConverter(take);

				return _valueConverter;
			}

			set => _valueConverter = value;
		}

		internal void SetTakeConverter(int take)
		{
			TakeValues ??= new List<int>();

			TakeValues.Add(take);

			SetTakeConverterInternal(take);
		}

		void SetTakeConverterInternal(int take)
		{
			var conv = _valueConverter;

			if (conv == null)
				_valueConverter = v => v == null ? null : ((int) v + take);
			else
				_valueConverter = v => v == null ? null : ((int) conv(v)! + take);
		}

		#endregion

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

		#region ISqlExpressionWalkable Members

		ISqlExpression ISqlExpressionWalkable.Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			return func(context, this);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			return ReferenceEquals(this, other);
		}

		public override int GetHashCode()
		{
			return RuntimeHelpers.GetHashCode(this);
		}

		#endregion

		#region ISqlExpression Members

		public bool CanBeNullable(NullabilityContext nullability) => CanBeNull;

		public bool CanBeNull => SqlDataType.TypeCanBeNull(Type.SystemType);

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return ((ISqlExpression)this).Equals(other) && comparer(this, other);
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.SqlParameter;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			if (Name?.StartsWith("@") == false)
				writer.Append('@');

			writer
				.Append(Name ?? "parameter");

#if DEBUG
			writer.Append('(').Append(_paramNumber).Append(')');
#endif
			if (Value != null)
				writer
					.Append('[')
					.Append(Value)
					.Append(']');
			return writer;
		}

		#endregion
	}
}
