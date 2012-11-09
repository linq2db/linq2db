using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.Common
{
	using Linq;
	using Expressions;
	using Extensions;
	using Mapping;

	static class ConverterMaker
	{
		static readonly MethodInfo _defaultConverter =
			ReflectionHelper.Expressor<object>.MethodOf(o => Convert.ChangeType(null, typeof(int)));

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
				case TypeCode.Char    : return true;
				default               : return false;
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

		static Expression GetParseEnum(Type from, Type to, Expression p)
		{
			if (from == typeof(string) && to.IsEnum)
			{
				var values = Enum.GetValues(to);
				var names  = Enum.GetNames(to);

				var dic = new Dictionary<string,object>();

				for (var i = 0; i < values.Length; i++)
				{
					var val = values.GetValue(i);
					var lv  = (long)Convert.ChangeType(val, typeof(long));

					dic[lv.ToString()] = val;

					if (lv > 0)
						dic["+" + lv.ToString()] = val;
				}

				for (var i = 0; i < values.Length; i++)
					dic[names[i].ToLowerInvariant()] = values.GetValue(i);

				for (var i = 0; i < values.Length; i++)
					dic[names[i]] = values.GetValue(i);

				var cases =
					from v in dic
					group v.Key by v.Value
					into g
					select Expression.SwitchCase(Expression.Constant(g.Key), g.Select(Expression.Constant));

				var expr = Expression.Switch(
					p,
					Expression.Convert(
						Expression.Call(_defaultConverter, Expression.Convert(p, typeof(string)), Expression.Constant(to)),
						to),
					cases.ToArray());

				return expr;
			}

			return null;
		}

		const FieldAttributes EnumField = FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal;

		static Expression GetToEnum(Type @from, Type to, Expression expression, MappingSchema mappingSchema)
		{
			if (to.IsEnum)
			{
			}

			return null;
		}

		static object ThrowLinqToDBException(string text)
		{
			throw new LinqToDBException(text);
		}

		static readonly MethodInfo _throwLinqToDBException =
			ReflectionHelper.Expressor<object>.MethodOf(o => ThrowLinqToDBException(null));

		static Expression GetFromEnum(Type @from, Type to, Expression expression, MappingSchema mappingSchema)
		{
			if (from.IsEnum)
			{
				var fromFields = @from.GetFields()
					.Where (f => (f.Attributes & EnumField) == EnumField)
					.Select(f => new { f, attrs = mappingSchema.GetAttributes<MapValueAttribute>(f, a => a.Configuration) })
					.ToList();

				var toTypeFields = fromFields
					.Select(f => new { f.f, attr = f.attrs.FirstOrDefault(a => a.Value == null || a.Value.GetType() == to) })
					.ToList();

				if (toTypeFields.All(f => f.attr != null))
				{
					var cases =
						from f in toTypeFields
						select Expression.SwitchCase(
							Expression.Constant(f.attr.Value ?? mappingSchema.GetDefaultValue(to), to),
							Expression.Constant(Enum.Parse(@from, f.f.Name)));

					var expr = Expression.Switch(
						expression,
						Expression.Convert(
							Expression.Call(_defaultConverter, Expression.Convert(expression, typeof(object)), Expression.Constant(to)),
							to),
						cases.ToArray());

					return expr;
				}

				if (toTypeFields.Any(f => f.attr != null))
				{
					var field = toTypeFields.First(f => f.attr == null);

					return Expression.Convert(
						Expression.Call(
							_throwLinqToDBException,
							Expression.Constant(
								string.Format("Inconsistent mapping. '{0}.{1}' does not have MapValue(<{2}>) attribute.",
									from.FullName, field.f.Name, to.FullName))),
							to);
				}
			}

			return null;
		}

		static Expression GetConverter(MappingSchema mappingSchema, Type from, Type to, Expression p)
		{
			if (from == to)
				return p;

			return
				GetFromEnum  (from, to, p, mappingSchema) ??
				GetToEnum    (from, to, p, mappingSchema) ??
				GetCtor      (from, to, p) ??
				GetValue     (from, to, p) ??
				GetOperator  (from, to, p) ??
				GetConvertion(from, to, p) ??
				GetParse     (from, to, p) ??
				GetToString  (from, to, p) ??
				GetKnownTypes(from, to, p) ??
				GetParseEnum (from, to, p);
		}

		public static LambdaExpression GetConverter(MappingSchema mappingSchema, Type from, Type to)
		{
			if (mappingSchema == null)
				mappingSchema = MappingSchema.Default;

			var p = Expression.Parameter(from, "p");

			if (from == to)
				return Expression.Lambda(p, p);

			if (to == typeof(object))
				return Expression.Lambda(Expression.Convert(p, typeof(object)), p);

			var ex = GetConverter(mappingSchema, from, to, p);

			if (ex == null)
			{
				var uto   = to.  ToUnderlying();
				var ufrom = from.ToUnderlying();

				if (from != ufrom)
				{
					var cp = Expression.Convert(p, ufrom);

					ex = GetConverter(mappingSchema, ufrom, to, cp);

					if (ex == null && to != uto)
					{
						ex = GetConverter(mappingSchema, ufrom, uto, cp);

						if (ex != null)
							ex = Expression.Convert(ex, to);
					}
				}

				if (ex == null && to != uto)
				{
					ex = GetConverter(mappingSchema, from, uto, p);

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

			return Expression.Lambda(
				Expression.Call(_defaultConverter, Expression.Convert(p, typeof(object)), Expression.Constant(to)),
				p);
		}
	}
}
