using System;
using System.Collections.Generic;
using System.Text;
using LinqToDB.Common;
using LinqToDB.DataProvider;

namespace LinqToDB.SqlQuery
{
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

		private SqlParameter(DbDataType type, string? name, object? value, Func<object?, object?>? valueConverter)
			: this(type, name, value)
		{
			_valueConverter = valueConverter;
		}

#if DEBUG
		readonly int _paramNumber;
		static   int _paramCounter;
#endif

		// meh, nullable...
		public string?    Name             { get; set; }
		public DbDataType Type             { get; set; }
		public bool       IsQueryParameter { get; set; }
		public string?    LikeStart        { get; set; }
		public string?    LikeEnd          { get; set; }
		public bool       ReplaceLike      { get; set; }
		internal int?     AccessorId       { get; set; }

		Type ISqlExpression.SystemType => Type.SystemType;

		private object? _value;
		public  object?  Value
		{
			get
			{
				var value = _value;

				if (ReplaceLike)
				{
					value = DataTools.EscapeUnterminatedBracket(value?.ToString());
				}

				if (LikeStart != null)
				{
					if (value != null)
					{
						return value.ToString()!.IndexOfAny(new[] { '%', '_' }) < 0 ?
							LikeStart + value + LikeEnd :
							LikeStart + EscapeLikeText(value.ToString()!) + LikeEnd;
					}
				}

				var valueConverter = ValueConverter;
				return valueConverter == null ? value : valueConverter(value);
			}

			set => _value = value;
		}

		internal object? RawValue => _value;

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
			if (this == other)
				return true;

			return
				other is SqlParameter p
				&& Name == p.Name
				&& Type.Equals(p.Type)
				&& AccessorId == p.AccessorId;
		}

		public override int GetHashCode()
		{
			var hashCode = Type.GetHashCode();

			if (Name != null)
				hashCode = unchecked(hashCode + (hashCode * 397) ^ Name.GetHashCode());

			return hashCode;
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
			if (!doClone(this))
				return this;

			if (!objectTree.TryGetValue(this, out var clone))
			{
				var p = new SqlParameter(Type, Name, _value, _valueConverter)
				{
					IsQueryParameter = IsQueryParameter,
					LikeStart        = LikeStart,
					LikeEnd          = LikeEnd,
					ReplaceLike      = ReplaceLike,
					AccessorId       = AccessorId
				};

				objectTree.Add(this, clone = p);
			}

			return clone;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.SqlParameter;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			sb
				.Append('@')
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
