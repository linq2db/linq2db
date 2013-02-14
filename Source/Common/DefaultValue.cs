using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Common
{
	using Expressions;
	using Extensions;
	using Mapping;

	public static class DefaultValue
	{
		static DefaultValue()
		{
			_values[typeof(int)]            = default(int);
			_values[typeof(uint)]           = default(uint);
			_values[typeof(byte)]           = default(byte);
			_values[typeof(char)]           = default(char);
			_values[typeof(bool)]           = default(bool);
			_values[typeof(sbyte)]          = default(sbyte);
			_values[typeof(short)]          = default(short);
			_values[typeof(long)]           = default(long);
			_values[typeof(ushort)]         = default(ushort);
			_values[typeof(ulong)]          = default(ulong);
			_values[typeof(float)]          = default(float);
			_values[typeof(double)]         = default(double);
			_values[typeof(decimal)]        = default(decimal);
			_values[typeof(DateTime)]       = default(DateTime);
			_values[typeof(TimeSpan)]       = default(TimeSpan);
			_values[typeof(DateTimeOffset)] = default(DateTimeOffset);
			_values[typeof(Guid)]           = default(Guid);
			_values[typeof(string)]         = default(string);
		}

		const FieldAttributes EnumField = FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal;

		static readonly ConcurrentDictionary<Type,object> _values = new ConcurrentDictionary<Type,object>();

		public static object GetValue([JetBrains.Annotations.NotNull] Type type, MappingSchema mappingSchema = null)
		{
			if (type == null) throw new ArgumentNullException("type");

			var ms = mappingSchema ?? MappingSchema.Default;

			object value;

			if (_values.TryGetValue(type, out value))
				return value;

			if (type.IsEnum)
			{
				var fields =
					from f in type.GetFields()
					where (f.Attributes & EnumField) == EnumField
					let attrs = ms.GetAttributes<MapValueAttribute>(f, a => a.Configuration).Where(a => a.Value == null).ToList()
					where attrs.Count > 0
					select Enum.Parse(type, f.Name);

				value = fields.FirstOrDefault();
			}

			if (value == null && !type.IsClass && !type.IsNullable())
			{
				var mi = MemberHelper.MethodOf(() => GetValue<int>());

				value =
					Expression.Lambda<Func<object>>(
						Expression.Convert(
							Expression.Call(mi.GetGenericMethodDefinition().MakeGenericMethod(type)),
							typeof(object)))
						.Compile()();
			}

			_values[type] = value;

			return value;
		}

		public static T GetValue<T>()
		{
			object value;

			if (_values.TryGetValue(typeof(T), out value))
				return (T)value;

			_values[typeof(T)] = default(T);

			return default(T);
		}

		public static void SetValue<T>(T value)
		{
			_values[typeof(T)] = value;
		}
	}

	public static class DefaultValue<T>
	{
		static T _value = DefaultValue.GetValue<T>();

		public static T Value
		{
			get { return _value; }
			set { DefaultValue.SetValue(_value = value); }
		}
	}
}
