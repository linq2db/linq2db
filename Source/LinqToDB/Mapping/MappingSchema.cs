﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

using JetBrains.Annotations;

namespace LinqToDB.Mapping
{
	using Common;
	using Expressions;
	using Extensions;
	using Metadata;
	using SqlProvider;
	using SqlQuery;
	using Common.Internal.Cache;

	/// <summary>
	/// Mapping schema.
	/// </summary>
	[PublicAPI]
	public class MappingSchema
	{
		#region Init

		/// <summary>
		/// Creates mapping schema instance.
		/// </summary>
		public MappingSchema()
			: this(null, (MappingSchema[]?)null)
		{
		}

		/// <summary>
		/// Creates mapping schema, derived from other mapping schemas.
		/// </summary>
		/// <param name="schemas">Base mapping schemas.</param>
		public MappingSchema(params MappingSchema[] schemas)
			: this(null, schemas)
		{
		}

		/// <summary>
		/// Creates mapping schema for specified configuration name.
		/// </summary>
		/// <param name="configuration">Mapping schema configuration name.
		/// <see cref="ProviderName"/> for standard names.
		/// </param>
		/// <remarks>Schema name should be unique for mapping schemas with different mappings.
		/// Using same name could lead to incorrect mapping used when mapping schemas with same name define different
		/// mappings for same type.</remarks>
		public MappingSchema(string? configuration)
			: this(configuration, null)
		{
		}

		static long _configurationCounter;

		/// <summary>
		/// Creates mapping schema with specified configuration name and base mapping schemas.
		/// </summary>
		/// <param name="configuration">Mapping schema configuration name.
		/// <see cref="ProviderName"/> for standard names.</param>
		/// <param name="schemas">Base mapping schemas.</param>
		/// <remarks>Schema name should be unique for mapping schemas with different mappings.
		/// Using same name could lead to incorrect mapping used when mapping schemas with same name define different
		/// mappings for same type.</remarks>
		public MappingSchema(string? configuration, params MappingSchema[]? schemas)
		{
			if (configuration.IsNullOrEmpty() && (schemas == null || schemas.Length == 0))
				configuration = "auto_" + Interlocked.Increment(ref _configurationCounter);

			var schemaInfo = new MappingSchemaInfo(configuration);

			if (schemas == null || schemas.Length == 0)
			{
				Schemas = new[] { schemaInfo, Default.Schemas[0] };

				ValueToSqlConverter = new ValueToSqlConverter(Default.ValueToSqlConverter);
			}
			else if (schemas.Length == 1)
			{
				Schemas = new MappingSchemaInfo[1 + schemas[0].Schemas.Length];
				Schemas[0] = schemaInfo;
				Array.Copy(schemas[0].Schemas, 0, Schemas, 1, schemas[0].Schemas.Length);

				var baseConverters = new ValueToSqlConverter[1 + schemas[0].ValueToSqlConverter.BaseConverters.Length];
				baseConverters[0] = schemas[0].ValueToSqlConverter;
				Array.Copy(schemas[0].ValueToSqlConverter.BaseConverters, 0, baseConverters, 1, schemas[0].ValueToSqlConverter.BaseConverters.Length);

				ValueToSqlConverter = new ValueToSqlConverter(baseConverters);
			}
			else
			{
				var schemaList     = new Dictionary<MappingSchemaInfo,   int>(schemas.Length);
				var baseConverters = new Dictionary<ValueToSqlConverter, int>(10);

				var i = 0;
				var j = 0;

				schemaList[schemaInfo] = i++;

				foreach (var schema in schemas)
				{
					foreach (var sc in schema.Schemas)
						schemaList[sc] = i++;

					baseConverters[schema.ValueToSqlConverter] = j++;

					foreach (var bc in schema.ValueToSqlConverter.BaseConverters)
						baseConverters[bc] = j++;
				}

				Schemas             = schemaList.OrderBy(static _ => _.Value).Select(static _ => _.Key).ToArray();
				ValueToSqlConverter = new ValueToSqlConverter(baseConverters.OrderBy(static _ => _.Value).Select(static _ => _.Key).ToArray());
			}
		}

		internal readonly MappingSchemaInfo[] Schemas;

		#endregion

		#region ValueToSqlConverter

		/// <summary>
		/// Gets value to SQL (usually literal) converter.
		/// </summary>
		public ValueToSqlConverter ValueToSqlConverter { get; private set; }

		/// <summary>
		/// Sets value to SQL converter action for specific value type.
		/// </summary>
		/// <param name="type">Value type.</param>
		/// <param name="converter">Converter action. Action accepts three parameters:
		/// - SQL string builder to write generated value SQL to;
		/// - value SQL type descriptor;
		/// - value.
		/// </param>
		public void SetValueToSqlConverter(Type type, Action<StringBuilder,SqlDataType,object> converter)
		{
			ValueToSqlConverter.SetConverter(type, converter);
		}

		#endregion

		#region Default Values

		const FieldAttributes EnumField = FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal;

		/// <summary>
		/// Returns default value for specified type.
		/// Default value is a value, used instead of <c>NULL</c> value, read from database.
		/// </summary>
		/// <param name="type">Value type.</param>
		/// <returns>Returns default value for type.</returns>
		public object? GetDefaultValue(Type type)
		{
			foreach (var info in Schemas)
			{
				var o = info.GetDefaultValue(type);
				if (o.IsSome)
					return o.Value;
			}

			if (type.IsEnum)
			{
				var mapValues = GetMapValues(type);

				if (mapValues != null)
				{
					object? value = null;

					foreach (var f in mapValues)
						if (f.MapValues.Any(static a => a.Value == null))
							value = f.OrigValue;

					if (value != null)
					{
						SetDefaultValue(type, value);
						return value;
					}
				}
			}

			return DefaultValue.GetValue(type, this);
		}

		/// <summary>
		/// Sets default value for specific type.
		/// Default value is a value, used instead of <c>NULL</c> value, read from database.
		/// </summary>
		/// <param name="type">Value type.</param>
		/// <param name="value">Default value.</param>
		public void SetDefaultValue(Type type, object? value)
		{
			Schemas[0].SetDefaultValue(type, value);
		}

		#endregion

		#region CanBeNull

		/// <summary>
		/// Returns <c>true</c>, if value of specified type could contain <c>null</c>.
		/// </summary>
		/// <param name="type">Value type.</param>
		/// <returns>Returns <c>true</c> if specified type supports <c>null</c> values.</returns>
		public bool GetCanBeNull(Type type)
		{
			foreach (var info in Schemas)
			{
				var o = info.GetCanBeNull(type);
				if (o.IsSome)
					return o.Value;
			}

			if (type.IsEnum)
			{
				var mapValues = GetMapValues(type);

				if (mapValues != null)
				{
					object? value = null;

					foreach (var f in mapValues)
						if (f.MapValues.Any(static a => a.Value == null))
							value = f.OrigValue;

					if (value != null)
					{
						SetCanBeNull(type, true);
						return true;
					}
				}
			}

			return type.IsClass || type.IsNullable();
		}

		/// <summary>
		/// Sets <c>null</c> value support flag for specified type.
		/// </summary>
		/// <param name="type">Value type.</param>
		/// <param name="value">If <c>true</c>, specified type value could contain <c>null</c>.</param>
		public void SetCanBeNull(Type type, bool value)
		{
			Schemas[0].SetCanBeNull(type, value);
		}

		#endregion

		#region GenericConvertProvider

		/// <summary>
		/// Initialize generic conversions for specific type parameter.
		/// </summary>
		/// <typeparam name="T">Generic type parameter, for which converters should be initialized.</typeparam>
		public void InitGenericConvertProvider<T>()
		{
			InitGenericConvertProvider(typeof(T));
		}

		/// <summary>
		/// Initialize generic conversions for specific type parameters.
		/// </summary>
		/// <param name="types">Generic type parameters.</param>
		/// <returns>Returns <c>true</c> if new generic type conversions could have added to mapping schema.</returns>
		public bool InitGenericConvertProvider(params Type[] types)
		{
			foreach (var schema in Schemas)
				if (schema.InitGenericConvertProvider(types, this))
					return true;

			return false;
		}

		/// <summary>
		/// Adds generic type conversions provider.
		/// Type converter must implement <see cref="IGenericInfoProvider"/> interface.
		/// <see cref="IGenericInfoProvider"/> for more details and examples.
		/// </summary>
		/// <param name="type">Generic type conversions provider.</param>
		public void SetGenericConvertProvider(Type type)
		{
			if (!type.IsGenericTypeDefinition)
				throw new LinqToDBException($"'{type}' must be a generic type.");

			if (!typeof(IGenericInfoProvider).IsSameOrParentOf(type))
				throw new LinqToDBException($"'{type}' must inherit from '{nameof(IGenericInfoProvider)}'.");

			Schemas[0].SetGenericConvertProvider(type);
		}

		#endregion

		#region Convert

		/// <summary>
		/// Converts value to specified type.
		/// </summary>
		/// <typeparam name="T">Target type.</typeparam>
		/// <param name="value">Value to convert.</param>
		/// <returns>Converted value.</returns>
		public T ChangeTypeTo<T>(object? value)
		{
			return Converter.ChangeTypeTo<T>(value, this);
		}

		/// <summary>
		/// Converts value to specified type.
		/// </summary>
		/// <param name="value">Value to convert.</param>
		/// <param name="conversionType">Target type.</param>
		/// <returns>Converted value.</returns>
		public object? ChangeType(object? value, Type conversionType)
		{
			return Converter.ChangeType(value, conversionType, this);
		}

		/// <summary>
		/// Converts enum value to database value.
		/// </summary>
		/// <param name="value">Enum value.</param>
		/// <returns>Database value.</returns>
		public object? EnumToValue(Enum value)
		{
			var toType = ConvertBuilder.GetDefaultMappingFromEnumType(this, value.GetType())!;
			return Converter.ChangeType(value, toType, this);
		}

		/// <summary>
		/// Returns custom value conversion expression from <paramref name="from"/> type to <paramref name="to"/> type if it
		/// is defined in mapping schema, or <c>null</c> otherwise.
		/// </summary>
		/// <param name="from">Source type.</param>
		/// <param name="to">Target type.</param>
		/// <returns>Conversion expression or <c>null</c>, if conversion is not defined.</returns>
		public virtual LambdaExpression? TryGetConvertExpression(Type @from, Type to)
		{
			return null;
		}

		internal ConcurrentDictionary<object,Func<object,object>> Converters
		{
			get { return Schemas[0].Converters; }
		}

		/// <summary>
		/// Returns conversion expression from <typeparamref name="TFrom"/> type to <typeparamref name="TTo"/> type.
		/// </summary>
		/// <typeparam name="TFrom">Source type.</typeparam>
		/// <typeparam name="TTo">Target type.</typeparam>
		/// <param name="checkNull">If <c>true</c>, and source type could contain <c>null</c>, conversion expression will check converted value for <c>null</c> and replace it with default value.
		/// <see cref="SetDefaultValue(Type, object)"/> for more details.
		/// </param>
		/// <param name="createDefault">Create new conversion expression, if conversion is not defined.</param>
		/// <returns>Conversion expression or <c>null</c>, if there is no such conversion and <paramref name="createDefault"/> is <c>false</c>.</returns>
		public Expression<Func<TFrom,TTo>>? GetConvertExpression<TFrom,TTo>(bool checkNull = true, bool createDefault = true)
		{
			return (Expression<Func<TFrom, TTo>>?)GetConvertExpression(typeof(TFrom), typeof(TTo), checkNull, createDefault);
		}

		/// <summary>
		/// Returns conversion expression from <paramref name="from"/> type to <paramref name="to"/> type.
		/// </summary>
		/// <param name="from">Source type.</param>
		/// <param name="to">Target type.</param>
		/// <param name="checkNull">If <c>true</c>, and source type could contain <c>null</c>, conversion expression will check converted value for <c>null</c> and replace it with default value.
		/// <see cref="SetDefaultValue(Type, object)"/> for more details.
		/// </param>
		/// <param name="createDefault">Create new conversion expression, if conversion is not defined.</param>
		/// <returns>Conversion expression or <c>null</c>, if there is no such conversion and <paramref name="createDefault"/> is <c>false</c>.</returns>
		public LambdaExpression? GetConvertExpression(Type from, Type to, bool checkNull = true, bool createDefault = true)
		{
			return GetConvertExpression(new DbDataType(from), new DbDataType(to), checkNull, createDefault);
		}

		/// <summary>
		/// Returns conversion expression from <paramref name="from"/> type to <paramref name="to"/> type.
		/// </summary>
		/// <param name="from">Source type.</param>
		/// <param name="to">Target type.</param>
		/// <param name="checkNull">If <c>true</c>, and source type could contain <c>null</c>, conversion expression will check converted value for <c>null</c> and replace it with default value.
		/// <see cref="SetDefaultValue(Type, object)"/> for more details.
		/// </param>
		/// <param name="createDefault">Create new conversion expression, if conversion is not defined.</param>
		/// <returns>Conversion expression or <c>null</c>, if there is no such conversion and <paramref name="createDefault"/> is <c>false</c>.</returns>
		public LambdaExpression? GetConvertExpression(DbDataType from, DbDataType to, bool checkNull = true, bool createDefault = true)
		{
			var li = GetConverter(from, to, createDefault);
			return li == null ? null : (LambdaExpression)ReduceDefaultValue(checkNull ? li.CheckNullLambda : li.Lambda);
		}


		/// <summary>
		/// Returns conversion delegate for conversion from <typeparamref name="TFrom"/> type to <typeparamref name="TTo"/> type.
		/// </summary>
		/// <typeparam name="TFrom">Source type.</typeparam>
		/// <typeparam name="TTo">Target type.</typeparam>
		/// <returns>Conversion delegate or <c>null</c> if conversion is not defined.</returns>
		public Func<TFrom,TTo>? GetConverter<TFrom,TTo>()
		{
			var from = new DbDataType(typeof(TFrom));
			var to   = new DbDataType(typeof(TTo));
			var li   = GetConverter(from, to, true);

			if (li == null)
				return null;

			if (li.Delegate == null)
			{
				var rex = (Expression<Func<TFrom,TTo>>)ReduceDefaultValue(li.CheckNullLambda);
				var l   = rex.CompileExpression();

				Schemas[0].SetConvertInfo(from, to, new ConvertInfo.LambdaInfo(li.CheckNullLambda, null, l, li.IsSchemaSpecific));

				return l;
			}

			return (Func<TFrom,TTo>?)li.Delegate;
		}

		/// <summary>
		/// Specify conversion expression for conversion from <paramref name="fromType"/> type to <paramref name="toType"/> type.
		/// </summary>
		/// <param name="fromType">Source type.</param>
		/// <param name="toType">Target type.</param>
		/// <param name="expr">Conversion expression.</param>
		/// <param name="addNullCheck">If <c>true</c>, conversion expression will be wrapped with default value substitution logic for <c>null</c> values.
		/// Wrapper will be added only if source type can have <c>null</c> values and conversion expression doesn't use
		/// default value provider.
		/// See <see cref="DefaultValue{T}"/> and <see cref="DefaultValue"/> types for more details.
		/// </param>
		public void SetConvertExpression(
			Type fromType,
			Type toType,
			LambdaExpression expr,
			bool addNullCheck = true)
		{
			if (fromType == null) throw new ArgumentNullException(nameof(fromType));
			if (toType   == null) throw new ArgumentNullException(nameof(toType));
			if (expr     == null) throw new ArgumentNullException(nameof(expr));

			var ex = addNullCheck && Converter.IsDefaultValuePlaceHolderVisitor.Find(expr) == null?
				AddNullCheck(expr) :
				expr;

			Schemas[0].SetConvertInfo(new DbDataType(fromType), new DbDataType(toType), new ConvertInfo.LambdaInfo(ex, expr, null, false));
		}

		/// <summary>
		/// Specify conversion expression for conversion from <paramref name="fromType"/> type to <paramref name="toType"/> type.
		/// </summary>
		/// <param name="fromType">Source type.</param>
		/// <param name="toType">Target type.</param>
		/// <param name="expr">Conversion expression.</param>
		/// <param name="addNullCheck">If <c>true</c>, conversion expression will be wrapped with default value substitution logic for <c>null</c> values.
		/// Wrapper will be added only if source type can have <c>null</c> values and conversion expression doesn't use
		/// default value provider.
		/// See <see cref="DefaultValue{T}"/> and <see cref="DefaultValue"/> types for more details.
		/// </param>
		public void SetConvertExpression(
			DbDataType                      fromType,
			DbDataType                      toType,
			LambdaExpression                expr,
			bool                            addNullCheck = true)
		{
			if (expr == null) throw new ArgumentNullException(nameof(expr));

			var ex = addNullCheck && Converter.IsDefaultValuePlaceHolderVisitor.Find(expr) == null?
				AddNullCheck(expr) :
				expr;

			Schemas[0].SetConvertInfo(fromType, toType, new ConvertInfo.LambdaInfo(ex, expr, null, false));
		}

		/// <summary>
		/// Specify conversion expression for conversion from <typeparamref name="TFrom"/> type to <typeparamref name="TTo"/> type.
		/// </summary>
		/// <typeparam name="TFrom">Source type.</typeparam>
		/// <typeparam name="TTo">Target type.</typeparam>
		/// <param name="expr">Conversion expression.</param>
		/// <param name="addNullCheck">If <c>true</c>, conversion expression will be wrapped with default value substitution logic for <c>null</c> values.
		/// Wrapper will be added only if source type can have <c>null</c> values and conversion expression doesn't use
		/// default value provider.
		/// See <see cref="DefaultValue{T}"/> and <see cref="DefaultValue"/> types for more details.
		/// </param>
		public void SetConvertExpression<TFrom,TTo>(
			Expression<Func<TFrom,TTo>> expr,
			bool addNullCheck = true)
		{
			if (expr == null) throw new ArgumentNullException(nameof(expr));

			var ex = addNullCheck && Converter.IsDefaultValuePlaceHolderVisitor.Find(expr) == null?
				AddNullCheck(expr) :
				expr;

			Schemas[0].SetConvertInfo(typeof(TFrom), typeof(TTo), new ConvertInfo.LambdaInfo(ex, expr, null, false));
		}

		/// <summary>
		/// Specify conversion expression for conversion from <typeparamref name="TFrom"/> type to <typeparamref name="TTo"/> type.
		/// </summary>
		/// <typeparam name="TFrom">Source type.</typeparam>
		/// <typeparam name="TTo">Target type.</typeparam>
		/// <param name="checkNullExpr"><c>null</c> values conversion expression.</param>
		/// <param name="expr">Conversion expression.</param>
		public void SetConvertExpression<TFrom,TTo>(
			Expression<Func<TFrom,TTo>> checkNullExpr,
			Expression<Func<TFrom,TTo>> expr)
		{
			if (expr == null) throw new ArgumentNullException(nameof(expr));

			Schemas[0].SetConvertInfo(typeof(TFrom), typeof(TTo), new ConvertInfo.LambdaInfo(checkNullExpr, expr, null, false));
		}

		/// <summary>
		/// Specify conversion delegate for conversion from <typeparamref name="TFrom"/> type to <typeparamref name="TTo"/> type.
		/// </summary>
		/// <typeparam name="TFrom">Source type.</typeparam>
		/// <typeparam name="TTo">Target type.</typeparam>
		/// <param name="func">Conversion delegate.</param>
		public void SetConverter<TFrom,TTo>(Func<TFrom,TTo> func)
		{
			if (func == null) throw new ArgumentNullException(nameof(func));

			var p  = Expression.Parameter(typeof(TFrom), "p");
			var ex = Expression.Lambda<Func<TFrom,TTo>>(Expression.Invoke(Expression.Constant(func), p), p);

			Schemas[0].SetConvertInfo(typeof(TFrom), typeof(TTo), new ConvertInfo.LambdaInfo(ex, null, func, false));
		}


		/// <summary>
		/// Specify conversion delegate for conversion from <typeparamref name="TFrom"/> type to <typeparamref name="TTo"/> type.
		/// </summary>
		/// <typeparam name="TFrom">Source type.</typeparam>
		/// <typeparam name="TTo">Target type.</typeparam>
		/// <param name="func">Conversion delegate.</param>
		/// <param name="from">Source type detalization</param>
		/// <param name="to">Target type detalization</param>
		public void SetConverter<TFrom,TTo>(Func<TFrom,TTo> func, DbDataType from, DbDataType to)
		{
			if (func == null) throw new ArgumentNullException(nameof(func));

			if (from.SystemType != typeof(TFrom))
				throw new ArgumentException($"'{nameof(from)}' parameter expects the same SystemType as in generic definition.", nameof(from));

			if (to.SystemType != typeof(TTo))
				throw new ArgumentException($"'{nameof(to)}' parameter expects the same SystemType as in generic definition.", nameof(to));

			var p  = Expression.Parameter(typeof(TFrom), "p");
			var ex = Expression.Lambda<Func<TFrom,TTo>>(Expression.Invoke(Expression.Constant(func), p), p);

			Schemas[0].SetConvertInfo(from, to, new ConvertInfo.LambdaInfo(ex, null, func, false));
		}

		internal LambdaExpression AddNullCheck(LambdaExpression expr)
		{
			var p = expr.Parameters[0];

			if (p.Type.IsNullable())
			{
				expr = Expression.Lambda(
					Expression.Condition(
						ExpressionHelper.Property(p, nameof(Nullable<int>.HasValue)),
						expr.Body,
						new DefaultValueExpression(this, expr.Body.Type)),
					expr.Parameters);
			}
			else if (p.Type.IsClass || p.Type.IsInterface)
			{
				expr = Expression.Lambda(
					Expression.Condition(
						Expression.NotEqual(p, Expression.Constant(null, p.Type)),
						expr.Body,
						new DefaultValueExpression(this, expr.Body.Type)),
					expr.Parameters);
			}
			
			return expr;
		}

		public LambdaExpression GenerateSafeConvert(Type fromType, Type type)
		{
			var param = Expression.Parameter(fromType, "v");
			var body  = (Expression)param;

			if (fromType.IsNullable())
			{
				body = Expression.Condition(
					ExpressionHelper.Property(param, nameof(Nullable<int>.HasValue)),
					Expression.Convert(body, type),
					new DefaultValueExpression(this, type));
			}
			else if (type.IsNullable())
			{
				body = Expression.Convert(param, type);
			}
			else if (fromType.IsClass || fromType.IsInterface)
			{
				body = Expression.Condition(
					Expression.NotEqual(param, Expression.Constant(null, fromType)),
					Expression.Convert(body, type),
					new DefaultValueExpression(this, type));
			}

			var expr = Expression.Lambda(body, param);
			return expr;
		}

		static bool IsSimple (ref DbDataType type)
			=> type.DataType == DataType.Undefined && string.IsNullOrEmpty(type.DbType) && type.Length == null;

		static void Simplify(ref DbDataType type)
		{
			if (!string.IsNullOrEmpty(type.DbType))
				type = type.WithDbType(null);

			if (type.DataType != DataType.Undefined)
				type = type.WithDataType(DataType.Undefined);

			if (type.Length != null)
				type = type.WithLength(null);
		}

		internal ConvertInfo.LambdaInfo? GetConverter(DbDataType from, DbDataType to, bool create)
		{
			do
			{
				for (var i = 0; i < Schemas.Length; i++)
				{
					var info = Schemas[i];
					var li   = info.GetConvertInfo(from, to);

					if (li != null && (i == 0 || !li.IsSchemaSpecific))
						return i == 0 ? li : new ConvertInfo.LambdaInfo(li.CheckNullLambda, li.Lambda, null, false);
				}

				if (!IsSimple(ref from))
					Simplify(ref from);
				else if (!IsSimple(ref to))
					Simplify(ref to);
				else break;

			} while (true);

			var isFromGeneric = from.SystemType.IsGenericType && !from.SystemType.IsGenericTypeDefinition;
			var isToGeneric   = to.SystemType.  IsGenericType && !to.SystemType.  IsGenericTypeDefinition;

			if (isFromGeneric || isToGeneric)
			{
				var fromGenericArgs = isFromGeneric ? from.SystemType.GetGenericArguments() : Array<Type>.Empty;
				var toGenericArgs   = isToGeneric   ? to.SystemType.  GetGenericArguments() : Array<Type>.Empty;

				var args = fromGenericArgs.SequenceEqual(toGenericArgs) ?
					fromGenericArgs : fromGenericArgs.Concat(toGenericArgs).ToArray();

				if (InitGenericConvertProvider(args))
					return GetConverter(from, to, create);
			}

			if (create)
			{
				var ufrom = from.SystemType.ToNullableUnderlying();
				var uto   = to.SystemType.  ToNullableUnderlying();

				LambdaExpression? ex;
				bool              ss = false;

				if (from.SystemType != ufrom)
				{
					var li = GetConverter(new DbDataType(ufrom), to, false);

					if (li != null)
					{
						var b  = li.CheckNullLambda.Body;
						var ps = li.CheckNullLambda.Parameters;

						// For int? -> byte try to find int -> byte and convert int to int?
						//
						var p = Expression.Parameter(from.SystemType, ps[0].Name);

						ss = li.IsSchemaSpecific;
						ex = Expression.Lambda(
							b.Transform((ufrom, p, ps), static (context, e) => e == context.ps[0] ? Expression.Convert(context.p, context.ufrom) : e),
							p);
					}
					else if (to.SystemType != uto)
					{
						li = GetConverter(new DbDataType(ufrom), new DbDataType(uto), false);

						if (li != null)
						{
							var b  = li.CheckNullLambda.Body;
							var ps = li.CheckNullLambda.Parameters;

							// For int? -> byte? try to find int -> byte and convert int to int? and result to byte?
							//
							var p = Expression.Parameter(from.SystemType, ps[0].Name);

							ss = li.IsSchemaSpecific;
							ex = Expression.Lambda(
								Expression.Convert(
									b.Transform((ufrom, p, ps), static (context, e) => e == context.ps[0] ? Expression.Convert(context.p, context.ufrom) : e),
									to.SystemType),
								p);
						}
						else
							ex = null;
					}
					else
						ex = null;
				}
				else if (to.SystemType != uto)
				{
					// For int? -> byte? try to find int -> byte and convert int to int? and result to byte?
					//
					var li = GetConverter(from, new DbDataType(uto), false);

					if (li != null)
					{
						var b  = li.CheckNullLambda.Body;
						var ps = li.CheckNullLambda.Parameters;

						ss = li.IsSchemaSpecific;
						ex = Expression.Lambda(Expression.Convert(b, to.SystemType), ps);
					}
					else
						ex = null;
				}
				else
					ex = null;

				if (ex != null)
					return new ConvertInfo.LambdaInfo(AddNullCheck(ex), ex, null, ss);

				var d = ConvertInfo.Default.Get(from, to);

				if (d == null || d.IsSchemaSpecific)
					d = ConvertInfo.Default.Create(this, from, to);

				return new ConvertInfo.LambdaInfo(d.CheckNullLambda, d.Lambda, null, d.IsSchemaSpecific);
			}

			return null;
		}

		Expression ReduceDefaultValue(Expression expr)
		{
			return (_reduceDefaultValueTransformer ??= TransformVisitor<MappingSchema>.Create(this, static (ctx, e) => ctx.ReduceDefaultValueTransformer(e)))
				.Transform(expr);
		}

		private TransformVisitor<MappingSchema>? _reduceDefaultValueTransformer;
		private Expression ReduceDefaultValueTransformer(Expression e)
		{
			return Converter.IsDefaultValuePlaceHolder(e) ?
				Expression.Constant(GetDefaultValue(e.Type), e.Type) :
				e;
		}

		/// <summary>
		/// Set conversion expressions for conversion from and to <c>string</c> for basic types
		/// (<c>byte</c>, <c>sbyte</c>, <c>short</c>, <c>ushort</c>, <c>int</c>, <c>uint</c>, <c>long</c>, <c>ulong</c>
		/// , <c>float</c>, <c>double</c>, <c>decimal</c>, <c>DateTime</c>, <c>DateTimeOffset</c>)
		/// using provided culture format providers.
		/// </summary>
		/// <param name="info">Culture with format providers for conversions.</param>
		public void SetCultureInfo(CultureInfo info)
		{
			SetConvertExpression((sbyte     v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((sbyte?    v) =>           v!.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>             sbyte.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>     (sbyte?)sbyte.Parse(s, info.NumberFormat));

			SetConvertExpression((short     v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((short?    v) =>           v!.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>             short.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>     (short?)short.Parse(s, info.NumberFormat));

			SetConvertExpression((int       v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((int?      v) =>           v!.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>               int.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>         (int?)int.Parse(s, info.NumberFormat));

			SetConvertExpression((long      v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((long?     v) =>           v!.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>              long.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>       (long?)long.Parse(s, info.NumberFormat));

			SetConvertExpression((byte      v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((byte?     v) =>           v!.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>              byte.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>       (byte?)byte.Parse(s, info.NumberFormat));

			SetConvertExpression((ushort    v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((ushort?   v) =>           v!.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>            ushort.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>   (ushort?)ushort.Parse(s, info.NumberFormat));

			SetConvertExpression((uint      v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((uint?     v) =>           v!.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>              uint.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>       (uint?)uint.Parse(s, info.NumberFormat));

			SetConvertExpression((ulong     v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((ulong?    v) =>           v!.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>             ulong.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>     (ulong?)ulong.Parse(s, info.NumberFormat));

			SetConvertExpression((float     v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((float?    v) =>           v!.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>             float.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>     (float?)float.Parse(s, info.NumberFormat));

			SetConvertExpression((double    v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((double?   v) =>           v!.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>            double.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>   (double?)double.Parse(s, info.NumberFormat));

			SetConvertExpression((decimal   v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((decimal?  v) =>           v!.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>           decimal.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) => (decimal?)decimal.Parse(s, info.NumberFormat));

			SetConvertExpression((DateTime  v) =>                       v.      ToString(info.DateTimeFormat));
			SetConvertExpression((DateTime? v) =>                       v!.Value.ToString(info.DateTimeFormat));
			SetConvertExpression((string    s) =>                      DateTime.Parse(s, info.DateTimeFormat));
			SetConvertExpression((string    s) =>           (DateTime?)DateTime.Parse(s, info.DateTimeFormat));

			SetConvertExpression((DateTimeOffset  v) =>                 v.      ToString(info.DateTimeFormat));
			SetConvertExpression((DateTimeOffset? v) =>                 v!.Value.ToString(info.DateTimeFormat));
			SetConvertExpression((string  s) =>                  DateTimeOffset.Parse(s, info.DateTimeFormat));
			SetConvertExpression((string  s) => (DateTimeOffset?)DateTimeOffset.Parse(s, info.DateTimeFormat));
		}

		#endregion

		#region MetadataReader

		readonly object _metadataReadersSyncRoot = new ();

		void InitMetadataReaders()
		{
			var list = new List   <IMetadataReader>(Schemas.Length);
			var hash = new HashSet<IMetadataReader>();

			for (var i = 0; i < Schemas.Length; i++)
			{
				var s = Schemas[i];
				if (s.MetadataReader != null && hash.Add(s.MetadataReader))
					list.Add(s.MetadataReader);
			}

			_metadataReaders = list.ToArray();
		}

#if NETFRAMEWORK
		/// <summary>
		/// Gets or sets metadata attributes provider for current schema.
		/// Metadata providers, shipped with LINQ to DB:
		/// - <see cref="Metadata.MetadataReader"/> - aggregation metadata provider over collection of other providers;
		/// - <see cref="AttributeReader"/> - .NET attributes provider;
		/// - <see cref="FluentMetadataReader"/> - fluent mappings metadata provider;
		/// - <see cref="SystemDataLinqAttributeReader"/> - metadata provider that converts <see cref="System.Data.Linq.Mapping"/> attributes to LINQ to DB mapping attributes;
		/// - <see cref="SystemDataSqlServerAttributeReader"/> - metadata provider that converts <see cref="Microsoft.SqlServer.Server"/> attributes to LINQ to DB mapping attributes;
		/// - <see cref="XmlAttributeReader"/> - XML-based mappings metadata provider.
		/// </summary>
#else
		/// <summary>
		/// Gets or sets metadata attributes provider for current schema.
		/// Metadata providers, shipped with LINQ to DB:
		/// - <see cref="LinqToDB.Metadata.MetadataReader"/> - aggregation metadata provider over collection of other providers;
		/// - <see cref="AttributeReader"/> - .NET attributes provider;
		/// - <see cref="FluentMetadataReader"/> - fluent mappings metadata provider;
		/// - <see cref="SystemDataSqlServerAttributeReader"/> - metadata provider that converts Microsoft.SqlServer.Server attributes to LINQ to DB mapping attributes;
		/// - <see cref="XmlAttributeReader"/> - XML-based mappings metadata provider.
		/// </summary>
#endif
		public IMetadataReader? MetadataReader
		{
			get { return Schemas[0].MetadataReader; }
			set
			{
				lock (_metadataReadersSyncRoot)
				{
					Schemas[0].MetadataReader = value;
					InitMetadataReaders();
				}
			}
		}

		/// <summary>
		/// Adds additional metadata attributes provider to current schema.
		/// </summary>
		/// <param name="reader">Metadata attributes provider.</param>
		public void AddMetadataReader(IMetadataReader reader)
		{
			lock (_metadataReadersSyncRoot)
			{
				var currentReader = MetadataReader;
				if (currentReader is MetadataReader metadataReader)
				{
					metadataReader.AddReader(reader);
					return;
				}

				MetadataReader = currentReader == null ? reader : new MetadataReader(reader, currentReader);
			}
		}

		IMetadataReader[]? _metadataReaders;
		IMetadataReader[]  MetadataReaders
		{
			get
			{
				if (_metadataReaders == null)
					lock (_metadataReadersSyncRoot)
						if (_metadataReaders == null)
							InitMetadataReaders();

				return _metadataReaders!;
			}
		}

		/// <summary>
		/// Gets attributes of specified type, associated with specified type.
		/// </summary>
		/// <typeparam name="T">Attribute type.</typeparam>
		/// <param name="type">Attributes owner type.</param>
		/// <param name="inherit">If <c>true</c> - include inherited attributes.</param>
		/// <returns>Attributes of specified type.</returns>
		public T[] GetAttributes<T>(Type type, bool inherit = true)
			where T : Attribute
		{
			if (MetadataReaders.Length == 0)
				return Array<T>.Empty;
			if (MetadataReaders.Length == 1)
				return MetadataReaders[0].GetAttributes<T>(type, inherit);

			var length = 0;
			var attrs = new T[MetadataReaders.Length][];

			for (var i = 0; i < MetadataReaders.Length; i++)
			{
				attrs[i] = MetadataReaders[i].GetAttributes<T>(type, inherit);
				length += attrs[i].Length;
			}

			var attributes = new T[length];
			length = 0;
			for (var i = 0; i < attrs.Length; i++)
			{
				if (attrs[i].Length > 0)
				{
					Array.Copy(attrs[i], 0, attributes, length, attrs[i].Length);
					length += attrs[i].Length;
				}
			}

			return attributes;
		}

		/// <summary>
		/// Gets attributes of specified type, associated with specified type member.
		/// </summary>
		/// <typeparam name="T">Attribute type.</typeparam>
		/// <param name="type">Member's owner type.</param>
		/// <param name="memberInfo">Attributes owner member.</param>
		/// <param name="inherit">If <c>true</c> - include inherited attributes.</param>
		/// <returns>Attributes of specified type.</returns>
		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit = true)
			where T : Attribute
		{
			if (MetadataReaders.Length == 0)
				return Array<T>.Empty;
			if (MetadataReaders.Length == 1)
				return MetadataReaders[0].GetAttributes<T>(type, memberInfo, inherit);

			var attrs = new T[MetadataReaders.Length][];
			var length = 0;

			for (var i = 0; i < MetadataReaders.Length; i++)
			{
				attrs[i] = MetadataReaders[i].GetAttributes<T>(type, memberInfo, inherit);
				length += attrs[i].Length;
			}

			var attributes = new T[length];
			length = 0;
			for (var i = 0; i < attrs.Length; i++)
			{
				if (attrs[i].Length > 0)
				{
					Array.Copy(attrs[i], 0, attributes, length, attrs[i].Length);
					length += attrs[i].Length;
				}
			}

			return attributes;
		}

		/// <summary>
		/// Gets attribute of specified type, associated with specified type.
		/// </summary>
		/// <typeparam name="T">Attribute type.</typeparam>
		/// <param name="type">Attribute owner type.</param>
		/// <param name="inherit">If <c>true</c> - include inherited attribute.</param>
		/// <returns>First found attribute of specified type or <c>null</c>, if no attributes found.</returns>
		public T? GetAttribute<T>(Type type, bool inherit = true)
			where T : Attribute
		{
			var attrs = GetAttributes<T>(type, inherit);
			return attrs.Length == 0 ? null : attrs[0];
		}

		/// <summary>
		/// Gets attribute of specified type, associated with specified type member.
		/// </summary>
		/// <typeparam name="T">Attribute type.</typeparam>
		/// <param name="type">Member's owner type.</param>
		/// <param name="memberInfo">Attribute owner member.</param>
		/// <param name="inherit">If <c>true</c> - include inherited attribute.</param>
		/// <returns>First found attribute of specified type or <c>null</c>, if no attributes found.</returns>
		public T? GetAttribute<T>(Type type, MemberInfo memberInfo, bool inherit = true)
			where T : Attribute
		{
			var attrs = GetAttributes<T>(type, memberInfo, inherit);
			return attrs.Length == 0 ? null : attrs[0];
		}

		/// <summary>
		/// Gets attributes of specified type, associated with specified type.
		/// Attributes filtered by schema's configuration names (see  <see cref="ConfigurationList"/>).
		/// </summary>
		/// <typeparam name="T">Attribute type.</typeparam>
		/// <param name="type">Attributes owner type.</param>
		/// <param name="configGetter">Attribute configuration name provider.</param>
		/// <param name="inherit">If <c>true</c> - include inherited attributes.</param>
		/// <param name="exactForConfiguration">If <c>true</c> - only associated to configuration attributes will be returned.</param>
		/// <returns>Attributes of specified type.</returns>
		public T[] GetAttributes<T>(Type type, Func<T,string?> configGetter, bool inherit = true, 
			bool exactForConfiguration = false)
			where T : Attribute
		{
			var list  = new List<T>();
			var attrs = GetAttributes<T>(type, inherit);

			foreach (var c in ConfigurationList)
			{
				foreach (var a in attrs)
					if (configGetter(a) == c)
						list.Add(a);
				if (exactForConfiguration && list.Count > 0)
					return list.ToArray();
			}

			foreach (var attribute in attrs)
				if (string.IsNullOrEmpty(configGetter(attribute)))
					list.Add(attribute);

			return list.ToArray();
		}

		/// <summary>
		/// Gets attributes of specified type, associated with specified type member.
		/// Attributes filtered by schema's configuration names (see  <see cref="ConfigurationList"/>).
		/// </summary>
		/// <typeparam name="T">Attribute type.</typeparam>
		/// <param name="type">Member's owner type.</param>
		/// <param name="memberInfo">Attributes owner member.</param>
		/// <param name="configGetter">Attribute configuration name provider.</param>
		/// <param name="inherit">If <c>true</c> - include inherited attributes.</param>
		/// <param name="exactForConfiguration">If <c>true</c> - only associated to configuration attributes will be returned.</param>
		/// <returns>Attributes of specified type.</returns>
		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, Func<T,string?> configGetter, bool inherit = true, 
			bool exactForConfiguration = false)
			where T : Attribute
		{
			var list  = new List<T>();
			var attrs = GetAttributes<T>(type, memberInfo, inherit);

			foreach (var c in ConfigurationList)
			{
				foreach (var a in attrs)
					if (configGetter(a) == c)
						list.Add(a);
				if (exactForConfiguration && list.Count > 0)
					return list.ToArray();
			}

			foreach (var attribute in attrs)
				if (string.IsNullOrEmpty(configGetter(attribute)))
					list.Add(attribute);

			return list.ToArray();
		}

		/// <summary>
		/// Gets attribute of specified type, associated with specified type.
		/// Attributes filtered by schema's configuration names (see  <see cref="ConfigurationList"/>).
		/// </summary>
		/// <typeparam name="T">Attribute type.</typeparam>
		/// <param name="type">Attribute owner type.</param>
		/// <param name="configGetter">Attribute configuration name provider.</param>
		/// <param name="inherit">If <c>true</c> - include inherited attribute.</param>
		/// <returns>First found attribute of specified type or <c>null</c>, if no attributes found.</returns>
		public T? GetAttribute<T>(Type type, Func<T,string?> configGetter, bool inherit = true)
			where T : Attribute
		{
			var attrs = GetAttributes(type, configGetter, inherit);
			return attrs.Length == 0 ? null : attrs[0];
		}

		/// <summary>
		/// Gets attribute of specified type, associated with specified type member.
		/// Attributes filtered by schema's configuration names (see  <see cref="ConfigurationList"/>).
		/// </summary>
		/// <typeparam name="T">Attribute type.</typeparam>
		/// <param name="type">Member's owner type.</param>
		/// <param name="memberInfo">Attribute owner member.</param>
		/// <param name="configGetter">Attribute configuration name provider.</param>
		/// <param name="inherit">If <c>true</c> - include inherited attribute.</param>
		/// <returns>First found attribute of specified type or <c>null</c>, if no attributes found.</returns>
		public T? GetAttribute<T>(Type type, MemberInfo memberInfo, Func<T,string?> configGetter, bool inherit = true)
			where T : Attribute
		{
			var attrs = GetAttributes(type, memberInfo, configGetter, inherit);
			return attrs.Length == 0 ? null : attrs[0];
		}

		/// <summary>
		/// Gets the dynamic columns defined on given type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>All dynamic columns defined on given type.</returns>
		public MemberInfo[] GetDynamicColumns(Type type)
		{
			var result = new List<MemberInfo>();

			foreach (var reader in MetadataReaders)
				result.AddRange(reader.GetDynamicColumns(type));

			return result.ToArray();
		}

		/// <summary>
		/// Gets fluent mapping builder for current schema.
		/// </summary>
		/// <returns>Fluent mapping builder.</returns>
		public FluentMappingBuilder GetFluentMappingBuilder()
		{
			return new FluentMappingBuilder(this);
		}

		#endregion

		#region Configuration

		private string? _configurationID;
		/// <summary>
		/// Unique schema configuration identifier. For internal use only.
		/// </summary>
		internal  string  ConfigurationID
		{
			get { return _configurationID ??= string.Join(".", ConfigurationList); }
		}

		private string[]? _configurationList;
		/// <summary>
		/// Gets configurations, associated with current mapping schema.
		/// </summary>
		public  string[]  ConfigurationList
		{
			get
			{
				if (_configurationList == null)
				{
					var hash = new HashSet<string>();
					var list = new List<string>();

					foreach (var s in Schemas)
						if (!s.Configuration.IsNullOrEmpty() && hash.Add(s.Configuration))
							list.Add(s.Configuration);

					_configurationList = list.ToArray();
				}

				return _configurationList;
			}
		}

		#endregion

		#region DefaultMappingSchema

		internal MappingSchema(MappingSchemaInfo mappingSchemaInfo)
		{
			Schemas = new[] { mappingSchemaInfo };

			ValueToSqlConverter = new ValueToSqlConverter();
		}

		/// <summary>
		/// Default mapping schema, used by LINQ to DB, when more specific mapping schema not provided.
		/// </summary>
		public static MappingSchema Default = new DefaultMappingSchema();

		class DefaultMappingSchema : MappingSchema
		{
			public DefaultMappingSchema()
				: base(new MappingSchemaInfo("") { MetadataReader = Metadata.MetadataReader.Default })
			{
				AddScalarType(typeof(char),            new SqlDataType(DataType.NChar, typeof(char),  1, null, null, null));
				AddScalarType(typeof(char?),           new SqlDataType(DataType.NChar, typeof(char?), 1, null, null, null));
				AddScalarType(typeof(string),          DataType.NVarChar);
				AddScalarType(typeof(decimal),         DataType.Decimal);
				AddScalarType(typeof(decimal?),        DataType.Decimal);
				AddScalarType(typeof(DateTime),        DataType.DateTime2);
				AddScalarType(typeof(DateTime?),       DataType.DateTime2);
				AddScalarType(typeof(DateTimeOffset),  DataType.DateTimeOffset);
				AddScalarType(typeof(DateTimeOffset?), DataType.DateTimeOffset);
				AddScalarType(typeof(TimeSpan),        DataType.Time);
				AddScalarType(typeof(TimeSpan?),       DataType.Time);
				AddScalarType(typeof(byte[]),          DataType.VarBinary);
				AddScalarType(typeof(Binary),          DataType.VarBinary);
				AddScalarType(typeof(Guid),            DataType.Guid);
				AddScalarType(typeof(Guid?),           DataType.Guid);
				AddScalarType(typeof(object),          DataType.Variant);
				AddScalarType(typeof(XmlDocument),     DataType.Xml);
				AddScalarType(typeof(XDocument),       DataType.Xml);
				AddScalarType(typeof(bool),            DataType.Boolean);
				AddScalarType(typeof(bool?),           DataType.Boolean);
				AddScalarType(typeof(sbyte),           DataType.SByte);
				AddScalarType(typeof(sbyte?),          DataType.SByte);
				AddScalarType(typeof(short),           DataType.Int16);
				AddScalarType(typeof(short?),          DataType.Int16);
				AddScalarType(typeof(int),             DataType.Int32);
				AddScalarType(typeof(int?),            DataType.Int32);
				AddScalarType(typeof(long),            DataType.Int64);
				AddScalarType(typeof(long?),           DataType.Int64);
				AddScalarType(typeof(byte),            DataType.Byte);
				AddScalarType(typeof(byte?),           DataType.Byte);
				AddScalarType(typeof(ushort),          DataType.UInt16);
				AddScalarType(typeof(ushort?),         DataType.UInt16);
				AddScalarType(typeof(uint),            DataType.UInt32);
				AddScalarType(typeof(uint?),           DataType.UInt32);
				AddScalarType(typeof(ulong),           DataType.UInt64);
				AddScalarType(typeof(ulong?),          DataType.UInt64);
				AddScalarType(typeof(float),           DataType.Single);
				AddScalarType(typeof(float?),          DataType.Single);
				AddScalarType(typeof(double),          DataType.Double);
				AddScalarType(typeof(double?),         DataType.Double);

				AddScalarType(typeof(BitArray),        DataType.BitArray);

				SetConverter<DBNull, object?>(static _ => null);

				ValueToSqlConverter.SetDefaults();
			}
		}

		#endregion

		#region Scalar Types

		/// <summary>
		/// Returns <c>true</c>, if provided type mapped to scalar database type in current schema.
		/// </summary>
		/// <param name="type">Type to check.</param>
		/// <returns><c>true</c>, if type mapped to scalar database type.</returns>
		public bool IsScalarType(Type type)
		{
			foreach (var info in Schemas)
			{
				var o = info.GetScalarType(type);
				if (o.IsSome)
					return o.Value;
			}

			var attr = GetAttribute<ScalarTypeAttribute>(type, static a => a.Configuration);
			var ret  = false;

			if (attr != null)
			{
				ret = attr.IsScalar;
			}
			else
			{
				type = type.ToNullableUnderlying();

				if (type.IsEnum || type.IsPrimitive || (Configuration.IsStructIsScalarType && type.IsValueType))
					ret = true;
			}

			return ret;
		}

		/// <summary>
		/// Configure how provided type should be handled during mapping to database - as scalar value or composite type.
		/// </summary>
		/// <param name="type">Type to configure.</param>
		/// <param name="isScalarType"><c>true</c>, if provided type should be mapped to scalar database value.</param>
		public void SetScalarType(Type type, bool isScalarType = true)
		{
			Schemas[0].SetScalarType(type, isScalarType);
		}

		/// <summary>
		/// Configure provided type mapping to scalar database type.
		/// </summary>
		/// <param name="type">Type to configure.</param>
		/// <param name="defaultValue">Default value. See <see cref="SetDefaultValue(Type, object)"/> for more details.</param>
		/// <param name="dataType">Optional scalar data type.</param>
		public void AddScalarType(Type type, object? defaultValue, DataType dataType = DataType.Undefined)
		{
			SetScalarType  (type);
			SetDefaultValue(type, defaultValue);

			if (dataType != DataType.Undefined)
				SetDataType(type, dataType);
		}

		/// <summary>
		/// Configure provided type mapping to scalar database type.
		/// </summary>
		/// <param name="type">Type to configure.</param>
		/// <param name="defaultValue">Default value. See <see cref="SetDefaultValue(Type, object)"/> for more details.</param>
		/// <param name="canBeNull">Set <c>null</c> value support flag. See <see cref="SetCanBeNull(Type, bool)"/> for more details.</param>
		/// <param name="dataType">Optional scalar data type.</param>
		public void AddScalarType(Type type, object? defaultValue, bool canBeNull, DataType dataType = DataType.Undefined)
		{
			SetScalarType  (type);
			SetDefaultValue(type, defaultValue);
			SetCanBeNull   (type, canBeNull);

			if (dataType != DataType.Undefined)
				SetDataType(type, dataType);
		}

		/// <summary>
		/// Configure provided type mapping to scalar database type.
		/// </summary>
		/// <param name="type">Type to configure.</param>
		/// <param name="dataType">Optional scalar data type.</param>
		public void AddScalarType(Type type, DataType dataType = DataType.Undefined)
		{
			SetScalarType(type);

			if (dataType != DataType.Undefined)
				SetDataType(type, dataType);
		}

		/// <summary>
		/// Configure provided type mapping to scalar database type.
		/// </summary>
		/// <param name="type">Type to configure.</param>
		/// <param name="dataType">Database data type.</param>
		public void AddScalarType(Type type, SqlDataType dataType)
		{
			SetScalarType(type);

			SetDataType(type, dataType);
		}

		#endregion

		#region DataTypes

		/// <summary>
		/// Returns database type mapping information for specified type.
		/// </summary>
		/// <param name="type">Mapped type.</param>
		/// <returns>Database type information.</returns>
		public SqlDataType GetDataType(Type type)
		{
			foreach (var info in Schemas)
			{
				var o = info.GetDataType(type);
				if (o.IsSome)
					return o.Value;
			}

			return SqlDataType.Undefined;
		}

		/// <summary>
		/// Associate specified type with LINQ to DB data type.
		/// </summary>
		/// <param name="type">Mapped type.</param>
		/// <param name="dataType">LINQ to DB data type.</param>
		public void SetDataType(Type type, DataType dataType)
		{
			Schemas[0].SetDataType(type, dataType);
		}

		/// <summary>
		/// Associate specified type with database data type.
		/// </summary>
		/// <param name="type">Mapped type.</param>
		/// <param name="dataType">Database data type.</param>
		public void SetDataType(Type type, SqlDataType dataType)
		{
			Schemas[0].SetDataType(type, dataType);
		}

		/// <summary>
		/// Returns scalar database type mapping information for provided type.
		/// </summary>
		/// <param name="type">Mapped type.</param>
		/// <param name="canBeNull">Returns <c>true</c>, if <paramref name="type"/> type is enum with mapping to <c>null</c> value.
		/// Initial parameter value, passed to this method is not used.</param>
		/// <returns>Scalar database type information.</returns>
		public SqlDataType GetUnderlyingDataType(Type type, out bool canBeNull)
		{
			canBeNull   = false;
			int? length = null;

			var underlyingType = type.ToNullableUnderlying();

			if (underlyingType.IsEnum)
			{
				var attrs = new List<MapValueAttribute>();

				foreach (var f in underlyingType.GetFields())
					if ((f.Attributes & EnumField) == EnumField)
						attrs.AddRange(GetAttributes<MapValueAttribute>(underlyingType, f, static a => a.Configuration));

				if (attrs.Count == 0)
				{
					underlyingType = Enum.GetUnderlyingType(underlyingType);
				}
				else
				{
					var   minLen    = 0;
					Type? valueType = null;

					foreach (var attr in attrs.OrderBy(static a => a.IsDefault ? 0 : 1))
					{
						if (attr.Value == null)
						{
							canBeNull = true;
						}
						else
						{
							if (valueType == null)
								valueType = attr.Value.GetType();

							if (attr.Value is string strVal)
							{
								var len = strVal.Length;

								if (length == null)
								{
									length = minLen = len;
								}
								else
								{
									if (minLen > len) minLen = len;
									if (length < len) length = len;
								}
							}
						}
					}

					if (valueType == null)
						return SqlDataType.Undefined;

					var dt = GetDataType(valueType);

					if (dt.Type.DataType == DataType.NVarChar && minLen == length)
						return new SqlDataType(DataType.NChar, valueType, length.Value);

					if (length.HasValue && dt.IsCharDataType)
						return new SqlDataType(dt.Type.DataType, valueType, length.Value);

					return dt;
				}
			}

			if (underlyingType != type)
				return GetDataType(underlyingType);

			return SqlDataType.Undefined;
		}


		#endregion

		#region GetMapValues

		ConcurrentDictionary<Type,MapValue[]?>? _mapValues;

		/// <summary>
		/// Returns enum type mapping information or <c>null</c> for non-enum types.
		/// </summary>
		/// <param name="type">Mapped type.</param>
		/// <returns>Mapping values for enum type and <c>null</c> for non-enum types.</returns>
		public virtual MapValue[]? GetMapValues(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			if (_mapValues == null)
				_mapValues = new ConcurrentDictionary<Type,MapValue[]?>();

			if (_mapValues.TryGetValue(type, out var mapValues))
				return mapValues;

			var underlyingType = type.ToNullableUnderlying();

			if (underlyingType.IsEnum)
			{
				var fields = new List<MapValue>();

				foreach (var f in underlyingType.GetFields())
					if ((f.Attributes & EnumField) == EnumField)
					{
						var attrs = GetAttributes<MapValueAttribute>(underlyingType, f, static a => a.Configuration);
						fields.Add(new MapValue(Enum.Parse(underlyingType, f.Name, false), attrs));
					}

				if (fields.Any(static f => f.MapValues.Length > 0))
					mapValues = fields.ToArray();
			}

			_mapValues[type] = mapValues;

			return mapValues;
		}

		#endregion

		#region Options

		/// <summary>
		/// Gets or sets column name comparison rules for comparison of column names in mapping with column name,
		///  returned by provider's data reader.
		/// </summary>
		public StringComparer ColumnNameComparer
		{
			get
			{
				if (Schemas[0].ColumnNameComparer == null)
				{
					Schemas[0].ColumnNameComparer = Schemas
						.Select        (static s => s.ColumnNameComparer)
						.FirstOrDefault(static s => s != null)
						??
						StringComparer.Ordinal;
				}

				return Schemas[0].ColumnNameComparer!;
			}

			set => Schemas[0].ColumnNameComparer = value;
		}

		#endregion

		#region EntityDescriptor

		/// <summary>
		/// Gets or sets action, called when the EntityDescriptor is created.
		/// Could be used to adjust created descriptor before use.
		/// </summary>
		public Action<MappingSchema, IEntityChangeDescriptor>? EntityDescriptorCreatedCallback { get; set; }

		internal static MemoryCache<(Type entityType, string schemaId)> EntityDescriptorsCache { get; } = new (new ());

		/// <summary>
		/// Returns mapped entity descriptor.
		/// </summary>
		/// <param name="type">Mapped type.</param>
		/// <returns>Mapping descriptor.</returns>
		public EntityDescriptor GetEntityDescriptor(Type type)
		{
			var ed = EntityDescriptorsCache.GetOrCreate(
				(entityType: type, ConfigurationID),
				this,
				static (o, context) =>
				{
					o.SlidingExpiration = Configuration.Linq.CacheSlidingExpiration;
					var edNew = new EntityDescriptor(context, o.Key.entityType);
					context.EntityDescriptorCreatedCallback?.Invoke(context, edNew);
					return edNew;
				});

			return ed;
		}

		/// <summary>
		/// Enumerates types registered by FluentMetadataBuilder.
		/// </summary>
		/// <returns>
		/// Returns array with all types, mapped by fluent mappings.
		/// </returns>
		public Type[] GetDefinedTypes()
		{
			return Schemas.SelectMany(static s => s.GetRegisteredTypes()).ToArray();
		}

		/// <summary>
		/// Clears EntityDescriptor cache.
		/// </summary>
		public static void ClearCache()
		{
			EntityDescriptorsCache.Clear();
		}

		internal void ResetEntityDescriptor(Type type)
		{
			EntityDescriptorsCache.Remove((type, ConfigurationID));
		}

		#endregion

		#region Enum

		/// <summary>
		/// Returns type, to which provided enumeration type is mapped or <c>null</c>, if type is not configured.
		/// See <see cref="SetDefaultFromEnumType(Type, Type)"/>.
		/// </summary>
		/// <param name="enumType">Enumeration type.</param>
		/// <returns>Mapped type or <c>null</c>.</returns>
		public Type? GetDefaultFromEnumType(Type enumType)
		{
			foreach (var info in Schemas)
			{
				var type = info.GetDefaultFromEnumType(enumType);
				if (type != null)
					return type;
			}
			return null;
		}

		/// <summary>
		/// Sets type, to which provided enumeration type should be mapped.
		/// </summary>
		/// <param name="enumType">Enumeration type.</param>
		/// <param name="defaultFromType">Mapped type.</param>
		public void SetDefaultFromEnumType(Type enumType, Type defaultFromType)
		{
			Schemas[0].SetDefaultFromEnumType(enumType, defaultFromType);
		}

		#endregion

		internal IEnumerable<T> SortByConfiguration<T>(Func<T, string?> configGetter, IEnumerable<T> values)
		{
			var orderedValues = new List<Tuple<T, int>>();

			foreach (var value in values)
			{
				var config = configGetter(value);
				var index  = Array.IndexOf(ConfigurationList, config);
				var order  = index == -1 ? ConfigurationList.Length : index;
				orderedValues.Add(Tuple.Create(value, order));
			}

			return orderedValues
				.OrderBy(static _ => _.Item2)
				.Select (static _ => _.Item1);
		}
	}
}
