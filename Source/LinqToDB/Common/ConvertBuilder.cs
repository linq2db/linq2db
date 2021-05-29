﻿using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace LinqToDB.Common
{
	using Expressions;
	using Extensions;
	using Mapping;

	static class ConvertBuilder
	{
		static readonly MethodInfo _defaultConverter = MemberHelper.MethodOf(() => ConvertDefault(null!, typeof(int)));

		static object ConvertDefault(object value, Type conversionType)
		{
			try
			{
				return Convert.ChangeType(value, conversionType, Thread.CurrentThread.CurrentCulture);
			}
			catch (Exception ex)
			{
				throw new LinqToDBConvertException($"Cannot convert value '{value}' to type '{conversionType.FullName}'", ex);
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

				if (oppt.IsNullable() && !pt.IsNullable())
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

				if (oppt.IsNullable() && !pt.IsNullable())
					p = GetCtor(pt, oppt, p)!;

				return Expression.Convert(p, to, op);
			}

			return null;
		}

		static bool IsConvertible(Type type)
		{
			if (type.IsEnum)
				return false;

			switch (type.GetTypeCodeEx())
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
				case TypeCode.Decimal :
				case TypeCode.Char    : return true;
				default               : return false;
			}
		}

		static Expression? GetConvertion(Type from, Type to, Expression p)
		{
			if (IsConvertible(from) && IsConvertible(to) && to != typeof(bool) ||
				from.IsAssignableFrom(to) && to.IsAssignableFrom(from))
				return Expression.ConvertChecked(p, to);

		 	return null;
		}

		static Expression? GetParse(Type from, Type to, Expression p)
		{
			if (from == typeof(string))
			{
				var mi = to.GetMethodEx("Parse", from);

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

		static Expression? GetToString(Type from, Type to, Expression p)
		{
			if (to == typeof(string) && !from.IsNullable())
			{
				var mi = from.GetMethodEx("ToString", Array<Type>.Empty);
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

					dic[lv.ToString()] = val;

					if (lv > 0)
						dic["+" + lv.ToString()] = val;
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
						Expression.Call(_defaultConverter,
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
				if (fromType.IsNullable())
					fromType = fromType.ToNullableUnderlying();

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
							Expression.Call(_defaultConverter,
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

		class EnumValues
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
					.Select(f => new EnumValues { Field = f, Attrs = mappingSchema.GetAttributes<MapValueAttribute>(from, f, a => a.Configuration) })
					.ToList();

				{
					var valueType = to;
					if (valueType.IsNullable())
						valueType = valueType.ToNullableUnderlying();


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
							Expression.Constant(f.Attrs.Value ?? mappingSchema.GetDefaultValue(to), to),
							Expression.Constant(Enum.Parse(from, f.Field.Name, false))));

						var expr = Expression.Switch(
							expression,
							Expression.Convert(
								Expression.Call(_defaultConverter,
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
						.Select(f => new EnumValues { Field = f, Attrs = mappingSchema.GetAttributes<MapValueAttribute>(to, f, a => a.Configuration) })
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
								select new {
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
										string.Format("Mapping ambiguity. '{0}.{1}' can be mapped to either '{2}.{3}' or '{2}.{4}'.",
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
								Expression.Call(_defaultConverter,
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

		static Tuple<Expression,bool>? GetConverter(MappingSchema mappingSchema, Expression expr, Type from, Type to)
		{
			if (from == to)
				return Tuple.Create(expr, false);

			var le = Converter.GetConverter(from, to);

			if (le != null)
				return Tuple.Create(le.GetBody(expr), false);

			var lex = mappingSchema.TryGetConvertExpression(from, to);

			if (lex != null)
				return Tuple.Create(lex.GetBody(expr), true);

			var cex = mappingSchema.GetConvertExpression(from, to, false, false);

			if (cex != null)
				return Tuple.Create(cex.GetBody(expr), true);

			var ex =
				GetFromEnum  (from, to, expr, mappingSchema) ??
				GetToEnum    (from, to, expr, mappingSchema);

			if (ex != null)
				return Tuple.Create(ex, true);

			ex =
				GetConvertion(from, to, expr) ??
				GetCtor      (from, to, expr) ??
				GetValue     (from, to, expr) ??
				GetOperator  (from, to, expr) ??
				GetParse     (from, to, expr) ??
				GetToString  (from, to, expr) ??
				GetParseEnum (from, to, expr);

			return ex != null ? Tuple.Create(ex, false) : null;
		}

		static Tuple<Expression,bool>? ConvertUnderlying(
			MappingSchema mappingSchema,
			Expression    expr,
			Type from, Type ufrom,
			Type to,   Type uto)
		{
			Tuple<Expression,bool>? ex = null;

			if (from != ufrom)
			{
				var cp = Expression.Convert(expr, ufrom);

				ex = GetConverter(mappingSchema, cp, ufrom, to);
			}

			if (ex == null && to != uto)
			{
				ex = GetConverter(mappingSchema, expr, from, uto);

				if (ex != null)
					ex = Tuple.Create(Expression.Convert(ex.Item1, to) as Expression, ex.Item2);
			}

			if (ex == null && from != ufrom && to != uto)
			{
				var cp = Expression.Convert(expr, ufrom);

				ex = GetConverter(mappingSchema, cp, ufrom, uto);

				if (ex != null)
					ex = Tuple.Create(Expression.Convert(ex.Item1, to) as Expression, ex.Item2);
			}

			return ex;
		}

		public static Tuple<LambdaExpression,LambdaExpression?,bool> GetConverter(MappingSchema? mappingSchema, Type from, Type to)
		{
			if (mappingSchema == null)
				mappingSchema = MappingSchema.Default;

			var p  = Expression.Parameter(from, "p");
			var ne = null as LambdaExpression;

			if (from == to)
				return Tuple.Create(Expression.Lambda(p, p), ne, false);

			if (to == typeof(object))
				return Tuple.Create(Expression.Lambda(Expression.Convert(p, typeof(object)), p), ne, false);

			var ex =
				GetConverter     (mappingSchema, p, from, to) ??
				ConvertUnderlying(mappingSchema, p, from, from.ToNullableUnderlying(), to, to.ToNullableUnderlying()) ??
				ConvertUnderlying(mappingSchema, p, from, from.ToUnderlying(),         to, to.ToUnderlying());

			if (ex != null)
			{
				ne = Expression.Lambda(ex.Item1, p);

				if (from.IsNullable())
					ex = Tuple.Create(
						Expression.Condition(ExpressionHelper.Property(p, nameof(Nullable<int>.HasValue)), ex.Item1, new DefaultValueExpression(mappingSchema, to)) as Expression,
						ex.Item2);
				else if (from.IsClass)
					ex = Tuple.Create(
						Expression.Condition(Expression.NotEqual(p, Expression.Constant(null, from)), ex.Item1, new DefaultValueExpression(mappingSchema, to)) as Expression,
						ex.Item2);
			}

			if (ex != null)
				return Tuple.Create(Expression.Lambda(ex.Item1, p), ne, ex.Item2);

			if (to.IsNullable())
			{
				var uto = to.ToNullableUnderlying();

				var defex = Expression.Call(_defaultConverter,
					Expression.Convert(p, typeof(object)),
					Expression.Constant(uto)) as Expression;

				if (defex.Type != uto)
					defex = Expression.Convert(defex, uto);

				defex = GetCtor(uto, to, defex)!;

				return Tuple.Create(Expression.Lambda(defex, p), ne, false);
			}
			else
			{
				var defex = Expression.Call(_defaultConverter,
					Expression.Convert(p, typeof(object)),
					Expression.Constant(to)) as Expression;

				if (defex.Type != to)
					defex = Expression.Convert(defex, to);

				return Tuple.Create(Expression.Lambda(defex, p), ne, false);
			}
		}

		#region Default Enum Mapping Type

		public static Type? GetDefaultMappingFromEnumType(MappingSchema mappingSchema, Type enumType)
		{
			var type = enumType.ToNullableUnderlying();

			if (!type.IsEnum)
				return null;

			var fields =
			(
				from f in type.GetFields()
				where (f.Attributes & EnumField) == EnumField
				let attrs = mappingSchema.GetAttributes<MapValueAttribute>(type, f, a => a.Configuration)
				select
				(
					from a in attrs
					where a.Configuration == attrs[0].Configuration
					orderby !a.IsDefault
					select a
				).ToList()
			).ToList();

			Type? defaultType = null;

			if (fields.All(attrs => attrs.Count != 0))
			{
				var attr = fields.FirstOrDefault(attrs => attrs[0].Value != null);

				if (attr != null)
				{
					var valueType = attr[0].Value!.GetType();

					if (fields.All(attrs => attrs[0].Value == null || attrs[0].Value!.GetType() == valueType))
						defaultType = valueType;
				}
			}

			if (defaultType == null)
				defaultType =
					   mappingSchema.GetDefaultFromEnumType(enumType)
					?? mappingSchema.GetDefaultFromEnumType(typeof(Enum))
					?? Enum.GetUnderlyingType(type);

			if ((enumType.IsNullable() || fields.Any(attrs => attrs.Count != 0 && attrs[0].Value == null)) && !defaultType.IsClass && !defaultType.IsNullable())
				defaultType = typeof(Nullable<>).MakeGenericType(defaultType);

			return defaultType;
		}

		#endregion
	}
}
