using System;
using System.Data.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.Common
{
	using Data.Linq;
	using Extensions;

	static class ConverterMaker
	{
		static Expression GetCtor(Type from, Type to, Expression p)
		{
			var ctor = to.GetConstructor(new[] { from });
			return ctor != null ? Expression.New(ctor, new[]  { p }) : null;
		}

		static Expression GetValue(Type from, Type to, Expression p)
		{
			var pi = from.GetProperty("Value");

			if (pi == null)
			{
				var fi = from.GetField("Value");

				if (fi != null && fi.FieldType == to)
					return Expression.Field(p, fi);
				return null;
			}

			return pi.PropertyType == to ? Expression.Property(p, pi) : null;
		}

		static Expression GetOperator(Type from, Type to, Expression p)
		{
			var op =
				to.GetMethod("op_Implicit", BindingFlags.Static | BindingFlags.Public, null, new[] { from }, null) ??
				to.GetMethod("op_Explicit", BindingFlags.Static | BindingFlags.Public, null, new[] { from }, null);

			return op != null ? Expression.Convert(p, to, op) : null;
		}

		static bool IsConvertible(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Boolean :
				case TypeCode.Byte    :
				case TypeCode.SByte   :
				case TypeCode.Int16   :
				case TypeCode.Int32   :
				case TypeCode.Int64   :
				case TypeCode.UInt16  :
				case TypeCode.UInt32  :
				case TypeCode.UInt64  :
				case TypeCode.Single  :
				case TypeCode.Double  :
				case TypeCode.Char    :
					return true;
				default :
					return false;
			}
		}

		static Expression GetConvertion(Type from, Type to, Expression p)
		{
			if (IsConvertible(from) && IsConvertible(to) && to != typeof(bool) || from.IsAssignableFrom(to) && to.IsAssignableFrom(from))
				return Expression.ConvertChecked(p, to);
		 	return null;
		}

		static Expression GetParse(Type from, Type to, Expression p)
		{
			if (from == typeof(string))
			{
				var mi = to.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new[] { from }, null);
				return mi != null ? Expression.Convert(p, to, mi) : null;
			}

			return null;
		}

		static Expression GetToString(Type from, Type to, Expression p)
		{
			if (to == typeof(string))
			{
				var mi = from.GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null);
				return mi != null ? Expression.Call(p, mi) : null;
			}

			return null;
		}

		static Expression GetKnownTypes(Type from, Type to, Expression p)
		{
			LambdaExpression le;

			if (from == typeof(string) && to == typeof(Binary))
			{
				Expression<Func<string,Binary>> l = s => new Binary(Encoding.UTF8.GetBytes(s));
				le = l;
			}
			else if (from == typeof(Binary) && to == typeof(byte[]))
			{
				Expression<Func<Binary,byte[]>> l = b => b.ToArray();
				le = l;
			}
			else
				return null;

			return le.Body.Transform(e => e == le.Parameters[0] ? p : e);
		}

		static Expression GetConverter(Type from, Type to, Expression p)
		{
			if (from == to)
				return p;

			return
				GetCtor      (from, to, p) ??
				GetValue     (from, to, p) ??
				GetOperator  (from, to, p) ??
				GetConvertion(from, to, p) ??
				GetParse     (from, to, p) ??
				GetToString  (from, to, p) ??
				GetKnownTypes(from, to, p);
		}

		public static LambdaExpression GetConverter(Type from, Type to)
		{
			var p = Expression.Parameter(from, "p");

			if (from == to)
				return Expression.Lambda(p, p);

			if (to == typeof(object))
				return Expression.Lambda(Expression.Convert(p, typeof(object)), p);

			var ex = GetConverter(from, to, p);

			if (ex == null)
			{
				var uto   = to.  ToUnderlying();
				var ufrom = from.ToUnderlying();

				if (from != ufrom)
				{
					var cp = Expression.Convert(p, ufrom);

					ex = GetConverter(ufrom, to, cp);

					if (ex == null && to != uto)
					{
						ex = GetConverter(ufrom, uto, cp);

						if (ex != null)
							ex = Expression.Convert(ex, to);
					}
				}

				if (ex == null && to != uto)
				{
					ex = GetConverter(from, uto, p);

					if (ex != null)
						ex = Expression.Convert(ex, to);
				}
			}

			if (ex != null)
			{
				if (from.IsNullable())
					ex = Expression.Condition(Expression.PropertyOrField(p, "HasValue"), ex, new DefaultValueExpression(to));
				else if (from.IsClass)
					ex = Expression.Condition(Expression.NotEqual(p, Expression.Constant(null, from)), ex, new DefaultValueExpression(to));
			}

			if (ex != null)
				return Expression.Lambda(ex, p);

			var mi = ReflectionHelper.Expressor<object>.MethodExpressor(o => System.Convert.ChangeType(null, to));

			return Expression.Lambda(
				Expression.Call(mi, Expression.Convert(p, typeof(object)), Expression.Constant(to)),
				p);
		}
	}
}
