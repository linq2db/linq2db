using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	using LinqToDB.Extensions;

	public class SqlParameter : ISqlExpression, IValueContainer
	{
		public SqlParameter(Type systemType, string name, object value)
		{
			if (systemType.ToNullableUnderlying().IsEnumEx())
				throw new ArgumentException();

			IsQueryParameter = true;
			Name             = name;
			SystemType       = systemType;
			_value           = value;
			DataType         = DataType.Undefined;
		}

		public SqlParameter(Type systemType, string name, object value, Func<object,object> valueConverter)
			: this(systemType, name, value)
		{
			_valueConverter = valueConverter;
		}

		public string   Name             { get; set; }
		public Type     SystemType       { get; set; }
		public bool     IsQueryParameter { get; set; }
		public DataType DataType         { get; set; }
		public int      DbSize           { get; set; }
		public string   LikeStart        { get; set; }
		public string   LikeEnd          { get; set; }
		public bool     ReplaceLike      { get; set; }

		private object _value;
		public  object  Value
		{
			get
			{
				var value = _value;

				if (ReplaceLike && value != null)
				{
					value = value.ToString().Replace("[", "[[]");
				}

				if (LikeStart != null)
				{
					if (value != null)
					{
						return value.ToString().IndexOfAny(new[] { '%', '_' }) < 0 ?
							LikeStart + value + LikeEnd :
							LikeStart + EscapeLikeText(value.ToString()) + LikeEnd;
					}
				}

				var valueConverter = ValueConverter;
				return valueConverter == null? value: valueConverter(value);
			}

			set { _value = value; }
		}

		internal object RawValue
		{
			get { return _value; }
		}

		#region Value Converter

		internal List<int>  TakeValues;

		private Func<object,object> _valueConverter;
		public  Func<object,object>  ValueConverter
		{
			get
			{
				if (_valueConverter == null && TakeValues != null)
					foreach (var take in TakeValues.ToArray())
						SetTakeConverter(take);

				return _valueConverter;
			}

			set { _valueConverter = value; }
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
				_valueConverter = v => v == null ? null : (object) ((int) conv(v) + take);
		}

		static string EscapeLikeText(string text)
		{
			if (text.IndexOfAny(new[] { '%', '_' }) < 0)
				return text;

			var builder = new StringBuilder(text.Length);

			foreach (var ch in text)
			{
				switch (ch)
				{
					case '%':
					case '_':
					case '~':
						builder.Append('~');
						break;
				}

				builder.Append(ch);
			}

			return builder.ToString();
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

		public int Precedence
		{
			get { return PrecedenceLevel.Primary; }
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

			var p = other as SqlParameter;
			return (object)p != null && Name != null && p.Name != null && Name == p.Name && SystemType == p.SystemType;
		}

		#endregion

		#region ISqlExpression Members

		public bool CanBeNull
		{
			get
			{
				if (SystemType == null && _value == null)
					return true;

				return SqlDataType.TypeCanBeNull(SystemType ?? _value.GetType());
				
			}
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
			{
				var p = new SqlParameter(SystemType, Name, _value, _valueConverter)
					{
						IsQueryParameter = IsQueryParameter,
						DataType         = DataType,
						DbSize           = DbSize,
						LikeStart        = LikeStart,
						LikeEnd          = LikeEnd,
						ReplaceLike      = ReplaceLike,
					};

				objectTree.Add(this, clone = p);
			}

			return clone;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType { get { return QueryElementType.SqlParameter; } }

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			return sb
				.Append('@')
				.Append(Name ?? "parameter")
				.Append('[')
				.Append(Value ?? "NULL")
				.Append(']');
		}

		#endregion
	}
}
