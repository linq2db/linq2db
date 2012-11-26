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
			MemberHelper.MethodOf(() => Convert.ChangeType(null, typeof(int)));

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

		static object ThrowLinqToDBException(string text)
		{
			throw new LinqToDBException(text);
		}

		static readonly MethodInfo _throwLinqToDBException = MemberHelper.MethodOf(() => ThrowLinqToDBException(null));

		static Expression GetToEnum(Type @from, Type to, Expression expression, MappingSchema mappingSchema)
		{
			if (to.IsEnum)
			{
				var toFields = to.GetFields()
					.Where (f => (f.Attributes & EnumField) == EnumField)
					.Select(f => new { f, attrs = mappingSchema.GetAttributes<MapValueAttribute>(f, a => a.Configuration) })
					.ToList();

				var fromTypeFields = toFields
					.Select(f => new { f.f, attrs = f.attrs.Where(a => a.Value == null || a.Value.GetType() == @from).ToList() })
					.ToList();

				if (fromTypeFields.All(f => f.attrs.Count != 0))
				{
					var cases = fromTypeFields
						.Select(f => new
							{
								value = Enum.Parse(to, f.f.Name),
								attrs = f.attrs
									.Where (a => a.Configuration == f.attrs[0].Configuration)
									.Select(a => a.Value ?? mappingSchema.GetDefaultValue(@from))
									.ToList()
							})
						.ToList();

					var ambiguityMappings =
						from c in cases
						from a in c.attrs
						group c by a into g
						where g.Count() > 1
						select g;

					var ambiguityMapping = ambiguityMappings.FirstOrDefault();

					if (ambiguityMapping != null)
					{
						var enums = ambiguityMapping.ToArray();

						return Expression.Convert(
							Expression.Call(
								_throwLinqToDBException,
								Expression.Constant(
									string.Format("Mapping ambiguity. MapValue({0}) attribute is defined for both '{1}.{2}' and '{1}.{3}'.",
										ambiguityMapping.Key, to.FullName, enums[0].value, enums[1].value))),
								to);
					}

					var expr = Expression.Switch(
						expression,
						Expression.Convert(
							Expression.Call(_defaultConverter, Expression.Convert(expression, typeof(object)), Expression.Constant(to)),
							to),
						cases
							.Select(f =>
								Expression.SwitchCase(
									Expression.Constant(f.value),
									f.attrs.Select(a => Expression.Constant(a, @from))))
							.ToArray());

					return expr;
				}

				if (fromTypeFields.Any(f => f.attrs.Count != 0))
				{
					var field = fromTypeFields.First(f => f.attrs == null);

					return Expression.Convert(
						Expression.Call(
							_throwLinqToDBException,
							Expression.Constant(
								string.Format("Inconsistent mapping. '{0}.{1}' does not have MapValue(<{2}>) attribute.",
									to.FullName, field.f.Name, from.FullName))),
							to);
				}
			}

			return null;
		}

		static Expression GetFromEnum(Type @from, Type to, Expression expression, MappingSchema mappingSchema)
		{
			if (from.IsEnum)
			{
				var fromFields = @from.GetFields()
					.Where (f => (f.Attributes & EnumField) == EnumField)
					.Select(f => new { f, attrs = mappingSchema.GetAttributes<MapValueAttribute>(f, a => a.Configuration) })
					.ToList();

				{
					var toTypeFields = fromFields
						.Select(f => new { f.f, attr = f.attrs
							.OrderByDescending(a => a.IsDefault)
							.ThenBy(a => a.Value == null)
							.FirstOrDefault(a => a.Value == null || a.Value.GetType() == to) })
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

				if (to.IsEnum)
				{
					var toFields = to.GetFields()
						.Where (f => (f.Attributes & EnumField) == EnumField)
						.Select(f => new { f, attrs = mappingSchema.GetAttributes<MapValueAttribute>(f, a => a.Configuration) })
						.ToList();

					foreach (var toField in toFields)
					{
						if (toField.attrs == null || toField.attrs.Length == 0)
							return null;

						var toAttr = toField.attrs.First();

						toAttr = toField.attrs.FirstOrDefault(a => a.Configuration == toAttr.Configuration && a.IsDefault) ?? toAttr;


					}
				}
			}

			return null;
		}

		static Tuple<Expression,bool> GetConverter(MappingSchema mappingSchema, Expression expr, Type @from, Type to)
		{
			if (from == to)
				return Tuple.Create(expr, false);

			var ex =
				GetFromEnum  (from, to, expr, mappingSchema) ??
				GetToEnum    (from, to, expr, mappingSchema);

			if (ex != null)
				return Tuple.Create(ex, true);

			ex =
				GetCtor      (from, to, expr) ??
				GetValue     (from, to, expr) ??
				GetOperator  (from, to, expr) ??
				GetConvertion(from, to, expr) ??
				GetParse     (from, to, expr) ??
				GetToString  (from, to, expr) ??
				GetKnownTypes(from, to, expr) ??
				GetParseEnum (from, to, expr);

			return ex != null ? Tuple.Create(ex, false) : null;
		}

		static Tuple<Expression,bool> ConvertUnderlying(
			MappingSchema mappingSchema,
			Expression    expr,
			Type from, Type ufrom,
			Type to,   Type uto)
		{
			Tuple<Expression,bool> ex = null;

			if (from != ufrom)
			{
				var cp = Expression.Convert(expr, ufrom);

				ex = GetConverter(mappingSchema, cp, ufrom, to);

				if (ex == null && to != uto)
				{
					ex = GetConverter(mappingSchema, cp, ufrom, uto);

					if (ex != null)
						ex = Tuple.Create(Expression.Convert(ex.Item1, to) as Expression, ex.Item2);
				}
			}

			if (ex == null && to != uto)
			{
				ex = GetConverter(mappingSchema, expr, @from, uto);

				if (ex != null)
					ex = Tuple.Create(Expression.Convert(ex.Item1, to) as Expression, ex.Item2);
			}

			return ex;
		}

		public static Tuple<LambdaExpression,bool> GetConverter(MappingSchema mappingSchema, Type from, Type to)
		{
			if (mappingSchema == null)
				mappingSchema = MappingSchema.Default;

			var p = Expression.Parameter(from, "p");

			if (from == to)
				return Tuple.Create(Expression.Lambda(p, p), false);

			if (to == typeof(object))
				return Tuple.Create(Expression.Lambda(Expression.Convert(p, typeof(object)), p), false);

			var ex =
				GetConverter     (mappingSchema, p, @from, to) ??
				ConvertUnderlying(mappingSchema, p, @from, @from.ToNullableUnderlying(), to, to.ToNullableUnderlying()) ??
				ConvertUnderlying(mappingSchema, p, @from, @from.ToUnderlying(),         to, to.ToUnderlying());

			if (ex != null)
			{
				if (from.IsNullable())
					ex = Tuple.Create(
						Expression.Condition(Expression.PropertyOrField(p, "HasValue"), ex.Item1, new DefaultValueExpression(to)) as Expression,
						ex.Item2);
				else if (from.IsClass)
					ex = Tuple.Create(
						Expression.Condition(Expression.NotEqual(p, Expression.Constant(null, from)), ex.Item1, new DefaultValueExpression(to)) as Expression,
						ex.Item2);
			}

			if (ex != null)
				return Tuple.Create(Expression.Lambda(ex.Item1, p), ex.Item2);

			return Tuple.Create(
				Expression.Lambda(
					Expression.Call(_defaultConverter, Expression.Convert(p, typeof(object)), Expression.Constant(to)),
					p),
				false);
		}
	}
}
