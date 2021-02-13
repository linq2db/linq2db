using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

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

		//TODO: Setter used only in EnumerableContext and should be hidden.
		public object?  Value { get; internal set; }

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
			if (TakeValues == null)
				TakeValues = new List<int>();

			TakeValues.Add(take);

			SetTakeConverterInternal(take);
		}

		void SetTakeConverterInternal(int take)
		{
			var conv = _valueConverter;

			if (conv == null)
				_valueConverter = v => v == null ? null : (object) ((int) v + take);
			else
				_valueConverter = v => v == null ? null : (object) ((int) conv(v)! + take);
		}

		#endregion

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

		public bool CanBeNull => SqlDataType.TypeCanBeNull(Type.SystemType);

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return ((ISqlExpression)this).Equals(other) && comparer(this, other);
		}

		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			// we do not allow parameters cloning 
			return this;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.SqlParameter;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			if (Name?.StartsWith("@") == false)
				sb.Append('@');

			sb
				.Append(Name ?? "parameter");

#if DEBUG
			sb.Append('(').Append(_paramNumber).Append(')');
#endif
			if (Value != null)
				sb
					.Append('[')
					.Append(Value)
					.Append(']');
			return sb;
		}

		#endregion
	}
}
