using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace LinqToDB.Common
{
	using Extensions;

	public static class Converter
	{
		static readonly ConcurrentDictionary<object,Func<object,object>> _converters = new ConcurrentDictionary<object,Func<object,object>>();

		public static object ChangeType(object value, Type conversionType)
		{
			if (value == null)
				return DefaultValue.GetValue(conversionType);

			if (value.GetType() == conversionType)
				return value;

			var from = value.GetType();
			var to   = conversionType;
			var key  = new { from, to };

			Func<object,object> l;

			if (!_converters.TryGetValue(key, out l))
			{
				var li = ConvertInfo.Default.Get(value.GetType(), to);
				var b  = li.Lambda.Body;
				var ps = li.Lambda.Parameters;

				var p  = Expression.Parameter(typeof(object), "p");
				var ex = Expression.Lambda<Func<object,object>>(
					Expression.Convert(
						b.Transform(e =>
							e == ps[0] ?
								Expression.Convert(p, e.Type) :
							IsDefaultValuePlaceHolder(e) ?
								Expression.Constant(DefaultValue.GetValue(e.Type)) :
								e),
						typeof(object)),
					p);

				l = ex.Compile();

				_converters[key] = l;
			}

			return l(value);
		}

		static class ExprHolder<T>
		{
			public static readonly ConcurrentDictionary<Type,Func<object,T>> Converters = new ConcurrentDictionary<Type, Func<object, T>>();
		}

		public static object ChangeTypeTo<T>(object value)
		{
			if (value == null)
				return DefaultValue<T>.Value;

			if (value.GetType() == typeof(T))
				return (T)value;

			var from = value.GetType();
			var to   = typeof(T);

			Func<object,T> l;

			if (!ExprHolder<T>.Converters.TryGetValue(from, out l))
			{
				var li = ConvertInfo.Default.Get(value.GetType(), to);
				var b  = li.Lambda.Body;
				var ps = li.Lambda.Parameters;

				var p  = Expression.Parameter(typeof(object), "p");
				var ex = Expression.Lambda<Func<object,T>>(
					b.Transform(e =>
						e == ps[0] ?
							Expression.Convert (p, e.Type) :
						IsDefaultValuePlaceHolder(e) ?
							Expression.Constant(DefaultValue.GetValue(e.Type)) :
							e),
					p);

				l = ex.Compile();

				ExprHolder<T>.Converters[from] = l;
			}

			return l(value);
		}

		internal static bool IsDefaultValuePlaceHolder(Expression expr)
		{
			if (expr is MemberExpression)
			{
				var me = (MemberExpression)expr;

				if (me.Member.Name == "Value" && me.Member.DeclaringType.IsGenericType)
					return me.Member.DeclaringType.GetGenericTypeDefinition() == typeof(DefaultValue<>);
			}

			return expr is DefaultValueExpression;
		}
	}
}
