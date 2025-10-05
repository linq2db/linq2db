using System;
using System.Collections.Concurrent;
using System.Linq;

using JetBrains.Annotations;

using LinqToDB.Internal.Extensions;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Default value provider.
	/// Default value used for mapping from NULL database value to C# value.
	/// </summary>
	[PublicAPI]
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

		static readonly ConcurrentDictionary<Type,object?> _values = new ConcurrentDictionary<Type,object?>();

		/// <summary>
		/// Returns default value for provided type.
		/// </summary>
		/// <param name="type">Type, for which default value requested.</param>
		/// <param name="mappingSchema">Optional mapping schema to provide mapping information for enum type.</param>
		/// <returns>Default value for specific type.</returns>
		public static object? GetValue(Type type, MappingSchema? mappingSchema = null)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (_values.TryGetValue(type, out var value))
				return value;

			if (type.IsEnum)
			{
				var ms = mappingSchema ?? MappingSchema.Default;
				var mapValues = ms.GetMapValues(type);

				if (mapValues != null)
					value = mapValues.FirstOrDefault(f => f.MapValues.Any(a => a.Value == null))?.OrigValue;
			}

			if (value == null && !type.IsNullableType())
				value = ReflectionExtensions.GetDefaultValue(type);

			_values[type] = value;

			return value;
		}

		/// <summary>
		/// Returns default value for provided type.
		/// </summary>
		/// <typeparam name="T">Type, for which default value requested.</typeparam>
		/// <returns>Default value for specific type.</returns>
		public static T GetValue<T>()
		{
			if (_values.TryGetValue(typeof(T), out var value))
				return (T)value!;

			_values[typeof(T)] = default(T)!;

			return default(T)!;
		}

		/// <summary>
		/// Sets default value for provided type.
		/// </summary>
		/// <typeparam name="T">Type, for which default value set.</typeparam>
		/// <param name="value">Default value for specific type.</param>
		public static void SetValue<T>(T value)
		{
			_values[typeof(T)] = value;
		}
	}

	/// <summary>
	/// Default value provider for specific type.
	/// Default value used for mapping from NULL database value to C# value.
	/// </summary>
	/// <typeparam name="T">Type parameter.</typeparam>
	[PublicAPI]
	public static class DefaultValue<T>
	{
		/// <summary>
		/// Gets or sets default value for specific type.
		/// </summary>
		public static T Value
		{
			get;
			set => DefaultValue.SetValue(field = value);
		} = DefaultValue.GetValue<T>();
	}
}
