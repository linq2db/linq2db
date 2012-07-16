using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Common
{
	using Data.Linq;
	using Extensions;

	public static class DefaultValue
	{
		static readonly Dictionary<Type,object> _values = new Dictionary<Type,object>()
		{
			{ typeof(int),            default(int)            },
			{ typeof(uint),           default(uint)           },
			{ typeof(byte),           default(byte)           },
			{ typeof(char),           default(char)           },
			{ typeof(bool),           default(bool)           },
			{ typeof(sbyte),          default(sbyte)          },
			{ typeof(short),          default(short)          },
			{ typeof(long),           default(long)           },
			{ typeof(ushort),         default(ushort)         },
			{ typeof(ulong),          default(ulong)          },
			{ typeof(float),          default(float)          },
			{ typeof(double),         default(double)         },
			{ typeof(decimal),        default(decimal)        },
			{ typeof(DateTime),       default(DateTime)       },
			{ typeof(TimeSpan),       default(TimeSpan)       },
			{ typeof(DateTimeOffset), default(DateTimeOffset) },
			{ typeof(Guid),           default(Guid)           },
			{ typeof(string),         default(string)         },
		};

		public static object GetValue([NotNull] Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			object value;

			if (_values.TryGetValue(type, out value))
				return value;

			if (type.IsClass || type.IsNullable())
				value = null;
			else
			{
				var mi = ReflectionHelper.Expressor<int>.MethodExpressor(o => GetValue<int>());

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
