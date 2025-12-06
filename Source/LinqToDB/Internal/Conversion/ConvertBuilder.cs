using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Conversion
{
	public static class ConvertBuilder
	{
		internal static readonly MethodInfo DefaultConverter = MemberHelper.MethodOf(() => ConvertDefault(null!, typeof(int)));

		static object ConvertDefault(object value, Type conversionType)
		{
			try
			{
				return Convert.ChangeType(value, conversionType, Thread.CurrentThread.CurrentCulture);
			}
			catch (Exception ex)
			{
				throw new LinqToDBConvertException($"Cannot convert value '{value}: {value.GetType().FullName}' to type '{conversionType.FullName}'", ex);
			}
		}

		static Expression? GetCtor(Type from, Type to, Expression p)
		{
			var ctor = to.GetConstructor(new[] { from });

			if (ctor == null)
				return null;

			var ptype = ctor.GetParameters()[0].ParameterType;

			if (ptype != from)
				p = Expression.Convert(p, ptype);

			return Expression.New(ctor, p);
		}

		static Expression? GetValueOrDefault(Type from, Type to, Expression p)
		{
			if (!from.IsNullableType || to != from.UnwrapNullableType())
				return null;

			var mi = from.GetMethod("GetValueOrDefault", BindingFlags.Instance | BindingFlags.Public, null, [], null);

			if (mi == null)
				return null;

			return Expression.Call(p, mi);
		}

		static Expression? GetValue(Type from, Type to, Expression p)
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

		static Expression? GetOperator(Type from, Type to, Expression p)
		{
			var op =
				to.GetMethodEx("op_Implicit", from) ??
				to.GetMethodEx("op_Explicit", from);

			if (op != null)
			{
				Type oppt = op.GetParameters()[0].ParameterType;
				Type pt   = p.Type;

				if (oppt.IsNullableType && !pt.IsNullableType)
					p = GetCtor(pt, oppt, p)!;

				return Expression.Convert(p, to, op);
			}

			op =
				from.GetMethodEx(to, "op_Implicit", from) ??
				from.GetMethodEx(to, "op_Explicit", from);

			if (op != null)
			{
				Type oppt = op.GetParameters()[0].ParameterType;
				Type pt   = p.Type;

				if (oppt.IsNullableType && !pt.IsNullableType)
					p = GetCtor(pt, oppt, p)!;

				return Expression.Convert(p, to, op);
			}

			return null;
		}

		static bool IsConvertible(Type type)
		{
			if (type.IsEnum)
				return false;

			return type.TypeCode
				is TypeCode.Boolean
				or TypeCode.Byte    
				or TypeCode.SByte   
				or TypeCode.Int16   
				or TypeCode.Int32   
				or TypeCode.Int64   
				or TypeCode.UInt16  
				or TypeCode.UInt32  
				or TypeCode.UInt64  
				or TypeCode.Single  
				or TypeCode.Double  
				or TypeCode.Decimal 
				or TypeCode.Char;
		}

		static Expression? GetConversion(Type from, Type to, Expression p)
		{
			if (IsConvertible(from) && IsConvertible(to) && to != typeof(bool) ||
				from.IsAssignableFrom(to) && to.IsAssignableFrom(from))
				return Expression.ConvertChecked(p, to);

		 	return null;
		}

		static readonly Type[] ParseParameters = new[] { typeof(string), typeof(IFormatProvider) };

		static Expression? GetParse(Type from, Type to, Expression p)
		{
			if (from == typeof(string))
			{
				var mi = to.GetMethodEx("Parse", ParseParameters);

				if (mi != null)
					return Expression.Call(mi, p, Expression.Property(null, typeof(CultureInfo), nameof(CultureInfo.InvariantCulture)));

				mi = to.GetMethodEx("Parse", from);

				if (mi != null)
				{
					return Expression.Convert(p, to, mi);
				}

				mi = to.GetMethodEx("Parse", typeof(SqlString));

				if (mi != null)
				{
					p = GetCtor(from, typeof(SqlString), p)!;
					return Expression.Convert(p, to, mi);
				}

				return null;
			}

			return null;
		}

		static readonly Type[] ToStringInvariantArgTypes = new[]{ typeof(IFormatProvider) };

		static Expression? GetToStringInvariant(Type from, Type to, Expression p)
		{
			if (to == typeof(string) && !from.IsNullableType)
			{
				var mi = from.GetMethodEx("ToString", ToStringInvariantArgTypes);
				return mi != null ? Expression.Call(p, mi, Expression.Property(null, typeof(CultureInfo), nameof(CultureInfo.InvariantCulture))) : null;
			}

			return null;
		}

		static Expression? GetToString(Type from, Type to, Expression p)
		{
			if (to == typeof(string) && !from.IsNullableType)
			{
				var mi = from.GetMethodEx("ToString", []);
				return mi != null ? Expression.Call(p, mi) : null;
			}

			return null;
		}

		static Expression? GetParseEnum(Type from, Type to, Expression p)
		{
			if (from == typeof(string) && to.IsEnum)
			{
				var values = Enum.GetValues(to);
				var names  = Enum.GetNames (to);

				var dic = new Dictionary<string,object>();

				for (var i = 0; i < values.Length; i++)
				{
					var val = values.GetValue(i)!;
					var lv  = (long)Convert.ChangeType(val, typeof(long), Thread.CurrentThread.CurrentCulture)!;
					var lvs = lv.ToString(NumberFormatInfo.InvariantInfo);

					dic[lvs] = val;

					if (lv > 0)
						dic["+" + lvs] = val;
				}

				for (var i = 0; i < values.Length; i++)
					dic[names[i].ToLowerInvariant()] = values.GetValue(i)!;

				for (var i = 0; i < values.Length; i++)
					dic[names[i]] = values.GetValue(i)!;

				var cases =
					from v in dic
					group v.Key by v.Value
					into g
					select Expression.SwitchCase(Expression.Constant(g.Key), g.Select(Expression.Constant));

				var expr = Expression.Switch(
					p,
					Expression.Convert(
						Expression.Call(DefaultConverter,
							Expression.Convert(p, typeof(string)),
							Expression.Constant(to)),
						to),
					cases.ToArray());

				return expr;
			}

			return null;
		}

		const FieldAttributes EnumField = FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal;

		static object ThrowLinqToDBException(string text)
		{
			throw new LinqToDBConvertException(text);
		}

		static readonly MethodInfo _throwLinqToDBConvertException = MemberHelper.MethodOf(() => ThrowLinqToDBException(null!));

		static Expression? GetToEnum(Type from, Type to, Expression expression, MappingSchema mappingSchema)
		{
			if (to.IsEnum)
			{
				var toFields = mappingSchema.GetMapValues(to);

				if (toFields == null)
					return null;

				var fromType = from;

				if (fromType.IsNullableType)
					fromType = fromType.UnwrapNullableType();

				var fromTypeFields = toFields
					.Select(f => new { f.OrigValue, attrs = f.MapValues.Where(a => a.Value == null || a.Value.GetType() == fromType).ToList() })
					.ToList();

				if (fromTypeFields.All(f => f.attrs.Count != 0))
				{
					var cases = fromTypeFields
						.Select(f => new
							{
								value = f.OrigValue,
								attrs = f.attrs
									.Where (a => a.Configuration == f.attrs[0].Configuration)
									.Select(a => a.Value ?? mappingSchema.GetDefaultValue(from))
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
						var enums = ambiguityMapping.ToList();

						return Expression.Convert(
							Expression.Call(
								_throwLinqToDBConvertException,
								Expression.Constant(
									$"Mapping ambiguity. MapValue({ambiguityMapping.Key}) attribute is defined for both '{to.FullName}.{enums[0].value}' and '{to.FullName}.{enums[1].value}'.")),
								to);
					}

					var expr = Expression.Switch(
						expression,
						Expression.Convert(
							Expression.Call(DefaultConverter,
								Expression.Convert(expression, typeof(object)),
								Expression.Constant(to)),
							to),
						cases
							.Select(f =>
								Expression.SwitchCase(
									Expression.Constant(f.value),
									f.attrs.Select(a => Expression.Constant(a, from))))
							.ToArray());

					return expr;
				}

				if (fromTypeFields.Any(f => f.attrs.Any(a => a.Value != null)))
				{
					var field = fromTypeFields.First(f => f.attrs.Count == 0);

					return Expression.Convert(
						Expression.Call(
							_throwLinqToDBConvertException,
							Expression.Constant(
								$"Inconsistent mapping. '{to.FullName}.{field.OrigValue}' does not have MapValue(<{from.FullName}>) attribute.")),
							to);
				}
			}

			return null;
		}

		sealed class EnumValues
		{
			public FieldInfo           Field = null!;
			public MapValueAttribute[] Attrs = null!;
		}

		static Expression? GetFromEnum(Type from, Type to, Expression expression, MappingSchema mappingSchema)
		{
			if (from.IsEnum)
			{
				var fromFields = from.GetFields()
					.Where (f => (f.Attributes & EnumField) == EnumField)
					.Select(f => new EnumValues { Field = f, Attrs = mappingSchema.GetAttributes<MapValueAttribute>(from, f) })
					.ToList();

				{
					var valueType = to;
					if (valueType.IsNullableType)
						valueType = valueType.UnwrapNullableType();

					var toTypeFields = fromFields
						.Select(f => new { f.Field, Attrs = f.Attrs
							.OrderBy(a =>
							{
								var idx = a.Configuration == null ?
									int.MaxValue :
									Array.IndexOf(mappingSchema.ConfigurationList, a.Configuration);
								return idx < 0 ? int.MaxValue : idx;
							})
							.ThenBy(a => !a.IsDefault)
							.ThenBy(a => a.Value == null)
							.FirstOrDefault(a => a.Value == null || a.Value.GetType() == valueType) })
						.ToList();

					if (toTypeFields.All(f => f.Attrs != null))
					{
						var cases = toTypeFields.Select(f => Expression.SwitchCase(
							Expression.Constant(f.Attrs!.Value ?? mappingSchema.GetDefaultValue(to), to),
							Expression.Constant(Enum.Parse(from, f.Field.Name, false))));

						var expr = Expression.Switch(
							expression,
							Expression.Convert(
								Expression.Call(DefaultConverter,
									Expression.Convert(expression, typeof(object)),
									Expression.Constant(to)),
								to),
							cases.ToArray());

						return expr;
					}

					if (toTypeFields.Any(f => f.Attrs != null))
					{
						var field = toTypeFields.First(f => f.Attrs == null);

						return Expression.Convert(
							Expression.Call(
								_throwLinqToDBConvertException,
								Expression.Constant(
									$"Inconsistent mapping. '{from.FullName}.{field.Field.Name}' does not have MapValue(<{to.FullName}>) attribute.")),
								to);
					}
				}

				if (to.IsEnum)
				{
					var toFields = to.GetFields()
						.Where (f => (f.Attributes & EnumField) == EnumField)
						.Select(f => new EnumValues { Field = f, Attrs = mappingSchema.GetAttributes<MapValueAttribute>(to, f) })
						.ToList();

					var dic = new Dictionary<EnumValues,EnumValues>();
					var cl  = mappingSchema.ConfigurationList.Concat(new[] { "", null }).Select((c,i) => (c, i)).ToArray();

					foreach (var toField in toFields)
					{
						if (toField.Attrs == null || toField.Attrs.Length == 0)
							return null;

						var toAttr = toField.Attrs.First();

						toAttr = toField.Attrs.FirstOrDefault(a => a.Configuration == toAttr.Configuration && a.IsDefault) ?? toAttr;

						var fromAttrs = fromFields.Where(f => f.Attrs.Any(a =>
							a.Value?.Equals(toAttr.Value) ?? toAttr.Value == null)).ToList();

						if (fromAttrs.Count == 0)
							return null;

						if (fromAttrs.Count > 1)
						{
							var fattrs =
								from f in fromAttrs
								select new
								{
									f,
									a = f.Attrs.First(a => a.Value?.Equals(toAttr.Value) ?? toAttr.Value == null)
								} into fa
								from c in cl
								where fa.a.Configuration == c.c
								orderby c.i
								select fa.f;

							fromAttrs = fattrs.Take(1).ToList();
						}

						var prev = dic
							.Where (a => a.Value.Field == fromAttrs[0].Field)
							.Select(pair => new { To = pair.Key, From = pair.Value })
							.FirstOrDefault();

						if (prev != null)
						{
							return Expression.Convert(
								Expression.Call(
									_throwLinqToDBConvertException,
									Expression.Constant(
										string.Format(
											CultureInfo.InvariantCulture,
											"Mapping ambiguity. '{0}.{1}' can be mapped to either '{2}.{3}' or '{2}.{4}'.",
											from.FullName, fromAttrs[0].Field.Name,
											to.FullName,
											prev.To.Field.Name,
											toField.Field.Name))),
									to);
						}

						dic.Add(toField, fromAttrs[0]);
					}

					if (dic.Count > 0)
					{
						var cases = dic.Select(f => Expression.SwitchCase(
							Expression.Constant(Enum.Parse(to,   f.Key.  Field.Name, false)),
							Expression.Constant(Enum.Parse(from, f.Value.Field.Name, false))));

						var expr = Expression.Switch(
							expression,
							Expression.Convert(
								Expression.Call(DefaultConverter,
									Expression.Convert(expression, typeof(object)),
									Expression.Constant(to)),
								to),
							cases.ToArray());

						return expr;
					}
				}
			}

			return null;
		}

		record struct Conversion(Expression Expression, bool IsLambda);

		static Conversion? GetConverter(MappingSchema mappingSchema, Expression expr, Type from, Type to)
		{
			if (from == to)
				return new(expr, false);

			var le = Converter.GetConverter(from, to);

			if (le != null)
				return new(le.GetBody(expr), false);

			var lex = mappingSchema.TryGetConvertExpression(from, to);

			if (lex != null)
				return new(lex.GetBody(expr), true);

			var cex = mappingSchema.GetConvertExpression(from, to, false, false);

			if (cex != null)
				return new(cex.GetBody(expr), true);

			var ex =
				GetFromEnum  (from, to, expr, mappingSchema) ??
				GetToEnum    (from, to, expr, mappingSchema);

			if (ex != null)
				return new(ex, true);

			ex =
				GetConversion       (from, to, expr) ??
				GetCtor             (from, to, expr) ??
				GetValueOrDefault   (from, to, expr) ??
				GetValue            (from, to, expr) ??
				GetOperator         (from, to, expr) ??
				GetParse            (from, to, expr) ??
				GetToStringInvariant(from, to, expr) ??
				GetToString         (from, to, expr) ??
				GetParseEnum        (from, to, expr);

			return ex != null ? new(ex, false) : null;
		}

		static Conversion? ConvertUnderlying(
			MappingSchema mappingSchema,
			Expression    expr,
			Type from, Type ufrom,
			Type to,   Type uto)
		{
			Conversion? ex = null;

			if (from != ufrom)
			{
				var cp = Expression.Convert(expr, ufrom);

				ex = GetConverter(mappingSchema, cp, ufrom, to);
			}

			if (ex == null && to != uto)
			{
				ex = GetConverter(mappingSchema, expr, from, uto);

				if (ex != null)
					ex = new(Expression.Convert(ex.Value.Expression, to), ex.Value.IsLambda);
			}

			if (ex == null && from != ufrom && to != uto)
			{
				var cp = Expression.Convert(expr, ufrom);

				ex = GetConverter(mappingSchema, cp, ufrom, uto);

				if (ex != null)
					ex = new(Expression.Convert(ex.Value.Expression, to) as Expression, ex.Value.IsLambda);
			}

			return ex;
		}

		public static ConverterLambda GetConverter(MappingSchema? mappingSchema, Type from, Type to)
		{
			mappingSchema ??= MappingSchema.Default;

			var p  = Expression.Parameter(from, "p");

			if (from == to)
				return new(Expression.Lambda(p, p), null, false);

			if (to == typeof(object))
				return new(Expression.Lambda(Expression.Convert(p, typeof(object)), p), null, false);

			var ex =
				GetConverter     (mappingSchema, p, from, to) ??
				ConvertUnderlying(mappingSchema, p, from, from.UnwrapNullableType(), to, to.UnwrapNullableType()) ??
				ConvertUnderlying(mappingSchema, p, from, from.ToUnderlying(),       to, to.ToUnderlying());

			LambdaExpression? ne = null;

			if (ex != null)
			{
				ne = Expression.Lambda(ex.Value.Expression, p);

				if (from.IsNullableType)
					ex = new(
						Expression.Condition(ExpressionHelper.Property(p, nameof(Nullable<>.HasValue)), ex.Value.Expression, new DefaultValueExpression(mappingSchema, to)) as Expression,
						ex.Value.IsLambda);
				else if (from.IsClass)
					ex = new(
						Expression.Condition(Expression.NotEqual(p, Expression.Constant(null, from)), ex.Value.Expression, new DefaultValueExpression(mappingSchema, to)) as Expression,
						ex.Value.IsLambda);
			}

			if (ex != null)
				return new(Expression.Lambda(ex.Value.Expression, p), ne, ex.Value.IsLambda);

			if (to.IsNullableType)
			{
				var uto = to.UnwrapNullableType();

				var defex = Expression.Call(DefaultConverter,
					Expression.Convert(p, typeof(object)),
					Expression.Constant(uto)) as Expression;

				if (defex.Type != uto)
					defex = Expression.Convert(defex, uto);

				defex = GetCtor(uto, to, defex)!;

				return new(Expression.Lambda(defex, p), ne, false);
			}
			else
			{
				var defex = Expression.Call(DefaultConverter,
					Expression.Convert(p, typeof(object)),
					Expression.Constant(to)) as Expression;

				if (defex.Type != to)
					defex = Expression.Convert(defex, to);

				return new(Expression.Lambda(defex, p), ne, false);
			}
		}

		#region Default Enum Mapping Type

		public static Type? GetDefaultMappingFromEnumType(MappingSchema mappingSchema, Type enumType)
		{
			var type = enumType.UnwrapNullableType();

			if (!type.IsEnum)
				return null;

			var allFieldsMapped      = true;
			Type? valuesType         = null;
			var allValuesHasSameType = true;
			var hasNullValue         = false;

			foreach (var field in type.GetFields())
			{
				if ((field.Attributes & EnumField) == EnumField)
				{
					var attrs = mappingSchema.GetAttributes<MapValueAttribute>(type, field);

					if (attrs.Length == 0)
						allFieldsMapped = false;
					else
					{
						// we don't just take first attribute to not break previous implementation
						// which prefered IsDefault=true value if many values specified
						var isDefault = false;

						// look for default value
						foreach (var attr in attrs)
						{
							if (attr.IsDefault)
							{
								if (attr.Value != null)
								{
									if (valuesType == null)
										valuesType = attr.Value.GetType();
									else if (valuesType != attr.Value.GetType())
										allValuesHasSameType = false;
								}
								else
									hasNullValue = true;

								isDefault = true;
								break;
							}
						}

						if (!isDefault)
						{
							var attr = attrs[0];
							if (attr.Value != null)
							{
								if (valuesType == null)
									valuesType = attr.Value.GetType();
								else if (valuesType != attr.Value.GetType())
									allValuesHasSameType = false;
							}
							else
								hasNullValue = true;
						}
					}
				}
			}

			Type defaultType;

			if (allFieldsMapped && valuesType != null && allValuesHasSameType)
				defaultType = valuesType;
			else
				defaultType =
					   mappingSchema.GetDefaultFromEnumType(enumType)
					?? mappingSchema.GetDefaultFromEnumType(typeof(Enum))
					?? Enum.GetUnderlyingType(type);

			if ((enumType.IsNullableType || hasNullValue) && !defaultType.IsNullableOrReferenceType())
				defaultType = defaultType.AsNullable();

			return defaultType;
		}

		#endregion
	}
}
