using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace LinqToDB.SqlBuilder
{
	using Mapping;

	public class SqlParameter : ISqlExpression, IValueContainer
	{
		public SqlParameter(Type systemType, string name, object value)
		{
			IsQueryParameter = true;
			Name             = name;
			SystemType       = systemType;
			_value           = value;
			DbType           = DbType.Object;
		}

		public SqlParameter(Type systemType, string name, object value, Converter<object,object> valueConverter)
			: this(systemType, name, value)
		{
			_valueConverter = valueConverter;
		}

		public string Name             { get; set; }
		public Type   SystemType       { get; set; }
		public bool   IsQueryParameter { get; set; }
		public DbType DbType           { get; set; }
		public int    DbSize           { get; set; }
		public string LikeStart        { get; set; }
		public string LikeEnd          { get; set; }

		private object _value;
		public  object  Value
		{
			get
			{
				if (LikeStart != null)
				{
					if (_value != null)
					{
						return _value.ToString().IndexOfAny(new[] { '%', '_' }) < 0 ?
							LikeStart + _value + LikeEnd :
							LikeStart + EscapeLikeText(_value.ToString()) + LikeEnd;
					}
				}

				var valueConverter = ValueConverter;
				return valueConverter == null? _value: valueConverter(_value);
			}

			set { _value = value; }
		}

		internal object RawValue
		{
			get { return _value; }
		}

		#region Value Converter

		internal List<Type> EnumTypes;
		internal List<int>  TakeValues;

		private Converter<object,object> _valueConverter;
		public  Converter<object,object>  ValueConverter
		{
			get
			{
				if (_valueConverter == null)
				{
					if (EnumTypes != null)
						foreach (var type in EnumTypes.ToArray())
							SetEnumConverter(type, Map.DefaultSchema);
					else if (TakeValues != null)
						foreach (var take in TakeValues.ToArray())
							SetTakeConverter(take);
				}

				return _valueConverter;
			}

			set { _valueConverter = value; }
		}

		bool _isEnumConverterSet;

		internal void SetEnumConverter(Type type, MappingSchemaOld ms)
		{
			if (!_isEnumConverterSet)
			{
				_isEnumConverterSet = true;

				if (EnumTypes == null)
					EnumTypes = new List<Type>();

				EnumTypes.Add(type);

				SetEnumConverterInternal(type, ms);
			}
		}

		void SetEnumConverterInternal(Type type, MappingSchemaOld ms)
		{
			if (_valueConverter == null)
			{
				_valueConverter = o => ms.MapEnumToValue(o, type, true);
			}
			else
			{
				var converter = _valueConverter;
				_valueConverter = o => ms.MapEnumToValue(converter(o), type, true);
			}
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
			get { return SqlBuilder.Precedence.Primary; }
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

		public bool CanBeNull()
		{
			if (SystemType == null && _value == null)
				return true;

			return SqlDataType.CanBeNull(SystemType ?? _value.GetType());
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
						DbType           = DbType,
						DbSize           = DbSize,
						LikeStart        = LikeStart,
						LikeEnd          = LikeEnd,
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
