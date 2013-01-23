using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace LinqToDB.SqlBuilder
{
	using Mapping;

	public class SqlParameter : ISqlExpression, IValueContainer
	{
		public SqlParameter(Type systemType, string name, object value, MappingSchemaOld mappingSchema)
		{
			IsQueryParameter = true;
			Name             = name;
			SystemType       = systemType;
			_value           = value;
			DbType           = DbType.Object;

			if (systemType != null && mappingSchema != null && systemType.IsEnum)
			{
				
			}
		}

		public SqlParameter(Type systemType, string name, object value, Converter<object,object> valueConverter)
			: this(systemType, name, value, (MappingSchemaOld)null)
		{
			_valueConverter = valueConverter;
		}

		public string Name             { get; set; }
		public Type   SystemType       { get; set; }
		public bool   IsQueryParameter { get; set; }
		public DbType DbType           { get; set; }
		public int    DbSize           { get; set; }

		private object _value;
		public  object  Value
		{
			get
			{
				var valueConverter = ValueConverter;
				return valueConverter == null? _value: valueConverter(_value);
			}

			set { _value = value; }
		}

		#region Value Converter

		internal List<Type> EnumTypes;
		internal List<int>  TakeValues;
		internal string     LikeStart, LikeEnd;

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
					else if (LikeStart != null)
						SetLikeConverter(LikeStart, LikeEnd);
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

		public void SetLikeConverter(string start, string end)
		{
			LikeStart = start;
			LikeEnd   = end;
			_valueConverter = GetLikeEscaper(start, end);
		}

		static Converter<object,object> GetLikeEscaper(string start, string end)
		{
			return (object value) =>
			{
				if (value == null)
#if DEBUG
					value = "";
#else
					throw new SqlException("NULL cannot be used as a LIKE predicate parameter.");
#endif

				var text = value.ToString();

				if (text.IndexOfAny(new[] { '%', '_', '[' }) < 0)
					return start + text + end;

				var sb = new StringBuilder(start, text.Length + start.Length + end.Length);

				foreach (var c in text)
				{
					if (c == '%' || c == '_' || c == '[')
					{
						sb.Append('[');
						sb.Append(c);
						sb.Append(']');
					}
					else
						sb.Append(c);
				}

				return (object)sb.ToString();
			};
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
				var p = new SqlParameter(SystemType, Name, _value, _valueConverter) { IsQueryParameter = IsQueryParameter, DbType = DbType, DbSize = DbSize };

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
