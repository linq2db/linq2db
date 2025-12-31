using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

using JetBrains.Annotations;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Internal.Cache;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Conversion;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Expressions.ExpressionVisitors;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Mapping;
using LinqToDB.Internal.Reflection;
using LinqToDB.Metadata;
using LinqToDB.SqlQuery;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Mapping schema.
	/// </summary>
	[PublicAPI]
	[DebuggerDisplay("{DisplayID}")]
	public class MappingSchema : IConfigurationID, IEquatable<MappingSchema>
	{
		static readonly MemoryCache<(MappingSchema ms1, MappingSchema ms2), MappingSchema> _combinedSchemasCache = new (new ());

		/// <summary>
		/// Internal API.
		/// <para>
		/// <b>Order of <paramref name="ms1"/> and <paramref name="ms2"/> is important:</b>
		/// the first schema (<paramref name="ms1"/>) will have higher priority than the second (<paramref name="ms2"/>).
		/// </para>
		/// </summary>
		public static MappingSchema CombineSchemas(MappingSchema ms1, MappingSchema ms2)
		{
			if (ms1.IsLockable && ms2.IsLockable)
			{
				return _combinedSchemasCache.GetOrCreate(
					(ms1, ms2),
					static entry =>
					{
						entry.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;
						return new MappingSchema(entry.Key.ms1, entry.Key.ms2);
					});
			}

			return new MappingSchema(ms1, ms2);
		}

		#region Init

		/// <summary>
		/// Creates mapping schema instance.
		/// </summary>
		public MappingSchema()
			: this(null, (MappingSchema[]?)null)
		{
		}

		/// <summary>
		/// Creates mapping schema, derived from other mapping schemata.
		/// <para>
		/// <b>Order of <paramref name="schemas"/> is important:</b>
		/// the first schema (<paramref name="schemas"/>[0]) will have higher priority than the second (<paramref name="schemas"/>[1]).
		/// </para>
		/// </summary>
		/// <param name="schemas">Base mapping schemata.</param>
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

		/// <summary>
		/// Creates mapping schema with specified configuration name and base mapping schemas.
		/// <para>
		/// <b>Order of <paramref name="schemas"/> is important:</b>
		/// the first schema (<paramref name="schemas"/>[0]) will have higher priority than the second (<paramref name="schemas"/>[1]).
		/// </para>
		/// </summary>
		/// <param name="configuration">Mapping schema configuration name.
		/// <see cref="ProviderName"/> for standard names.</param>
		/// <param name="schemas">Base mapping schemas.</param>
		/// <remarks>Schema name should be unique for mapping schemas with different mappings.
		/// Using same name could lead to incorrect mapping used when mapping schemas with same name define different
		/// mappings for same type.
		/// </remarks>
		public MappingSchema(string? configuration, params MappingSchema[]? schemas)
		{
			configuration ??= string.Empty;

#pragma warning disable CA2214 // Do not call overridable methods in constructors
			var schemaInfo = CreateMappingSchemaInfo(configuration, this);
#pragma warning restore CA2214 // Do not call overridable methods in constructors

			if (schemas == null || schemas.Length == 0)
			{
				Schemas = [schemaInfo, Default.Schemas[0]];

				if (configuration.Length == 0 && !IsLockable)
					_configurationID = schemaInfo.ConfigurationID;

				ValueToSqlConverter = new (Default.ValueToSqlConverter);
			}
			else if (schemas.Length == 1)
			{
				Schemas = new MappingSchemaInfo[1 + schemas[0].Schemas.Length];
				Schemas[0] = schemaInfo;

				Array.Copy(schemas[0].Schemas, 0, Schemas, 1, schemas[0].Schemas.Length);

				var baseConverters = new ValueToSqlConverter[1 + schemas[0].ValueToSqlConverter.BaseConverters.Length];

				baseConverters[0] = schemas[0].ValueToSqlConverter;

				Array.Copy(schemas[0].ValueToSqlConverter.BaseConverters, 0, baseConverters, 1, schemas[0].ValueToSqlConverter.BaseConverters.Length);

				ValueToSqlConverter = new (baseConverters);

				if (configuration!.Length == 0 && !IsLockable)
					_configurationID = Schemas[1].ConfigurationID;
			}
			else
			{
				var schemaList     = new Dictionary<MappingSchemaInfo,  int>(schemas.Length);
				var baseConverters = new Dictionary<ValueToSqlConverter,int>(10);

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

				Schemas             = schemaList.OrderBy(static s => s.Value).Select(static s => s.Key).ToArray();
				ValueToSqlConverter = new (baseConverters.OrderBy(static c => c.Value).Select(static c => c.Key).ToArray());
			}

			InitMetadataReaders(schemas?.Length > 1);

			(_cache, _firstOnlyCache) = CreateAttributeCaches();
		}

		readonly Lock _syncRoot = new();
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
		public MappingSchema SetValueToSqlConverter(Type type, Action<StringBuilder, SqlDataType, object> converter)
		{
			ValueToSqlConverter.SetConverter(type, (sb, dt, _, v) => converter(sb, new SqlDataType(dt), v));
			ResetID();
			return this;
		}

		/// <summary>
		/// Sets value to SQL converter action for specific value type.
		/// </summary>
		/// <param name="type">Value type.</param>
		/// <param name="converter">Converter action. Action accepts three parameters:
		/// - SQL string builder to write generated value SQL to;
		/// - value SQL type descriptor;
		/// - value.
		/// </param>
		public MappingSchema SetValueToSqlConverter(Type type, Action<StringBuilder,SqlDataType,DataOptions,object> converter)
		{
			ValueToSqlConverter.SetConverter(type, (sb, t, options, value) => converter(sb, new SqlDataType(t), options, value));
			ResetID();
			return this;
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
				if (o.HasValue)
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
						lock (_syncRoot)
						{
							Schemas[0].SetDefaultValue(type, value, resetId: false);
						}

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
			lock (_syncRoot)
			{
				Schemas[0].SetDefaultValue(type, value);
				ResetID();
			}
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
				if (o.HasValue)
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
						lock (_syncRoot)
						{
							Schemas[0].SetCanBeNull(type, true, resetId: false);
						}

						return true;
					}
				}
			}

			return type.IsNullableOrReferenceType();
		}

		/// <summary>
		/// Sets <c>null</c> value support flag for specified type.
		/// </summary>
		/// <param name="type">Value type.</param>
		/// <param name="value">If <c>true</c>, specified type value could contain <c>null</c>.</param>
		public void SetCanBeNull(Type type, bool value)
		{
			lock (_syncRoot)
			{
				Schemas[0].SetCanBeNull(type, value);
				ResetID();
			}
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
				if (schema.InitGenericConvertProvider(types))
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

		Dictionary<Type,Type>? _enumTypeMapping;

		/// <summary>
		/// Converts enum value to database value.
		/// </summary>
		/// <param name="value">Enum value.</param>
		/// <returns>Database value.</returns>
		public object? EnumToValue(Enum value)
		{
			_enumTypeMapping ??= new();

			Type? toType;
			var   fromType = value.GetType();

			lock (_enumTypeMapping)
				_enumTypeMapping.TryGetValue(fromType, out toType);

			if (toType == null)
			{
				toType = ConvertBuilder.GetDefaultMappingFromEnumType(this, value.GetType())!;
				lock (_enumTypeMapping)
					_enumTypeMapping[fromType] = toType;
			}

			return Converter.ChangeType(value, toType, this);
		}

		/// <summary>
		/// Returns custom value conversion expression from <paramref name="from"/> type to <paramref name="to"/> type if it
		/// is defined in mapping schema, or <c>null</c> otherwise.
		/// </summary>
		/// <param name="from">Source type.</param>
		/// <param name="to">Target type.</param>
		/// <returns>Conversion expression or <c>null</c>, if conversion is not defined.</returns>
		public virtual LambdaExpression? TryGetConvertExpression(Type from, Type to)
		{
			return null;
		}

		internal ConcurrentDictionary<object,Func<object,object>> Converters => Schemas[0].Converters;

		/// <summary>
		/// Returns conversion expression from <typeparamref name="TFrom"/> type to <typeparamref name="TTo"/> type.
		/// </summary>
		/// <typeparam name="TFrom">Source type.</typeparam>
		/// <typeparam name="TTo">Target type.</typeparam>
		/// <param name="checkNull">If <c>true</c>, and source type could contain <c>null</c>, conversion expression will check converted value for <c>null</c> and replace it with default value.
		/// <see cref="SetDefaultValue(Type, object)"/> for more details.
		/// </param>
		/// <param name="createDefault">Create new conversion expression, if conversion is not defined.</param>
		/// <param name="conversionType">Conversion type. See <see cref="ConversionType"/> for more details.</param>
		/// <returns>Conversion expression or <c>null</c>, if there is no such conversion and <paramref name="createDefault"/> is <c>false</c>.</returns>
		public Expression<Func<TFrom,TTo>>? GetConvertExpression<TFrom,TTo>(
			bool           checkNull      = true,
			bool           createDefault  = true,
			ConversionType conversionType = ConversionType.Common)
		{
			return (Expression<Func<TFrom, TTo>>?)GetConvertExpression(typeof(TFrom), typeof(TTo), checkNull, createDefault, conversionType);
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
		/// <param name="conversionType">Conversion type. See <see cref="ConversionType"/> for more details.</param>
		/// <returns>Conversion expression or <c>null</c>, if there is no such conversion and <paramref name="createDefault"/> is <c>false</c>.</returns>
		public LambdaExpression? GetConvertExpression(
			Type           from,
			Type           to,
			bool           checkNull      = true,
			bool           createDefault  = true,
			ConversionType conversionType = ConversionType.Common)
		{
			return GetConvertExpression(new DbDataType(from), new DbDataType(to), checkNull, createDefault, conversionType);
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
		/// <param name="conversionType">Conversion type. See <see cref="ConversionType"/> for more details.</param>
		/// <returns>Conversion expression or <c>null</c>, if there is no such conversion and <paramref name="createDefault"/> is <c>false</c>.</returns>
		public LambdaExpression? GetConvertExpression(
			DbDataType     from,
			DbDataType     to,
			bool           checkNull      = true,
			bool           createDefault  = true,
			ConversionType conversionType = ConversionType.Common)
		{
			var li = GetConverter(from, to, createDefault, conversionType);
			return li == null ? null : (LambdaExpression)ReduceDefaultValue(checkNull ? li.CheckNullLambda : li.Lambda);
		}

		/// <summary>
		/// Returns conversion delegate for conversion from <typeparamref name="TFrom"/> type to <typeparamref name="TTo"/> type.
		/// </summary>
		/// <typeparam name="TFrom">Source type.</typeparam>
		/// <typeparam name="TTo">Target type.</typeparam>
		/// <param name="conversionType">Conversion type. See <see cref="ConversionType"/> for more details.</param>
		/// <returns>Conversion delegate or <c>null</c> if conversion is not defined.</returns>
		public Func<TFrom,TTo>? GetConverter<TFrom,TTo>(ConversionType conversionType = ConversionType.Common)
		{
			var from = new DbDataType(typeof(TFrom));
			var to   = new DbDataType(typeof(TTo));
			var li   = GetConverter(from, to, true, conversionType);

			if (li == null)
				return null;

			if (li.Delegate == null)
			{
				var rex = (Expression<Func<TFrom,TTo>>)ReduceDefaultValue(li.CheckNullLambda);
				var l   = rex.CompileExpression();

				lock (_syncRoot)
				{
					Schemas[0].SetConvertInfo(from, to, conversionType, new (li.CheckNullLambda, null, l, li.IsSchemaSpecific), false);
				}

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
		/// This parameter is ignored for conversions to <see cref="DataParameter"/> and treated as <c>false</c>.
		/// </param>
		/// <param name="conversionType">Conversion type. See <see cref="ConversionType"/> for more details.</param>
		public MappingSchema SetConvertExpression(
			Type             fromType,
			Type             toType,
			LambdaExpression expr,
			bool             addNullCheck   = true,
			ConversionType   conversionType = ConversionType.Common)
		{
			if (fromType == null) throw new ArgumentNullException(nameof(fromType));
			if (toType   == null) throw new ArgumentNullException(nameof(toType));
			if (expr     == null) throw new ArgumentNullException(nameof(expr));

			var ex = addNullCheck && toType != typeof(DataParameter) && !Converter.HasDefaultValuePlaceHolder(expr)
				? AddNullCheck(expr)
				: expr;

			lock (_syncRoot)
			{
				Schemas[0].SetConvertInfo(new DbDataType(fromType), new DbDataType(toType), conversionType, new (ex, expr, null, false), true);
				ResetID();
			}

			return this;
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
		/// This parameter is ignored for conversions to <see cref="DataParameter"/> and treated as <c>false</c>.
		/// </param>
		/// <param name="conversionType">Conversion type. See <see cref="ConversionType"/> for more details.</param>
		public MappingSchema SetConvertExpression(
			DbDataType       fromType,
			DbDataType       toType,
			LambdaExpression expr,
			bool             addNullCheck   = true,
			ConversionType   conversionType = ConversionType.Common)
		{
			if (expr == null) throw new ArgumentNullException(nameof(expr));

			var ex = addNullCheck && toType.SystemType != typeof(DataParameter) && !Converter.HasDefaultValuePlaceHolder(expr)
				? AddNullCheck(expr)
				: expr;

			lock (_syncRoot)
			{
				Schemas[0].SetConvertInfo(fromType, toType, conversionType, new (ex, expr, null, false), true);
				ResetID();
			}

			return this;
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
		/// This parameter is ignored for conversions to <see cref="DataParameter"/> and treated as <c>false</c>.
		/// </param>
		/// <param name="conversionType">Conversion type. See <see cref="ConversionType"/> for more details.</param>
		public MappingSchema SetConvertExpression<TFrom,TTo>(
			Expression<Func<TFrom,TTo>> expr,
			bool                        addNullCheck   = true,
			ConversionType              conversionType = ConversionType.Common)
		{
			if (expr == null) throw new ArgumentNullException(nameof(expr));

			var ex = addNullCheck && typeof(TTo) != typeof(DataParameter) && !Converter.HasDefaultValuePlaceHolder(expr)
				? AddNullCheck(expr)
				: expr;

			lock (_syncRoot)
			{
				Schemas[0].SetConvertInfo(typeof(TFrom), typeof(TTo), conversionType, new (ex, expr, null, false));
				ResetID();
			}

			return this;
		}

		/// <summary>
		/// Specify conversion expression for conversion from <typeparamref name="TFrom"/> type to <typeparamref name="TTo"/> type.
		/// </summary>
		/// <typeparam name="TFrom">Source type.</typeparam>
		/// <typeparam name="TTo">Target type.</typeparam>
		/// <param name="checkNullExpr"><c>null</c> values conversion expression.</param>
		/// <param name="expr">Conversion expression.</param>
		/// <param name="conversionType">Conversion type. See <see cref="ConversionType"/> for more details.</param>
		public MappingSchema SetConvertExpression<TFrom,TTo>(
			Expression<Func<TFrom,TTo>> checkNullExpr,
			Expression<Func<TFrom,TTo>> expr,
			ConversionType              conversionType = ConversionType.Common)
		{
			if (expr == null) throw new ArgumentNullException(nameof(expr));

			lock (_syncRoot)
			{
				Schemas[0].SetConvertInfo(typeof(TFrom), typeof(TTo), conversionType, new (checkNullExpr, expr, null, false));
				ResetID();
			}

			return this;
		}

		/// <summary>
		/// Specify conversion delegate for conversion from <typeparamref name="TFrom"/> type to <typeparamref name="TTo"/> type.
		/// </summary>
		/// <typeparam name="TFrom">Source type.</typeparam>
		/// <typeparam name="TTo">Target type.</typeparam>
		/// <param name="func">Conversion delegate.</param>
		/// <param name="conversionType">Conversion type. See <see cref="ConversionType"/> for more details.</param>
		public MappingSchema SetConverter<TFrom,TTo>(
			Func<TFrom,TTo> func,
			ConversionType  conversionType = ConversionType.Common)
		{
			if (func == null) throw new ArgumentNullException(nameof(func));

			var p  = Expression.Parameter(typeof(TFrom), "p");
			var ex = Expression.Lambda<Func<TFrom,TTo>>(Expression.Invoke(Expression.Constant(func), p), p);

			lock (_syncRoot)
			{
				Schemas[0].SetConvertInfo(typeof(TFrom), typeof(TTo), conversionType, new (ex, null, func, false));
				ResetID();
			}

			return this;
		}

		/// <summary>
		/// Specify conversion delegate for conversion from <typeparamref name="TFrom"/> type to <typeparamref name="TTo"/> type.
		/// </summary>
		/// <typeparam name="TFrom">Source type.</typeparam>
		/// <typeparam name="TTo">Target type.</typeparam>
		/// <param name="func">Conversion delegate.</param>
		/// <param name="from">Source type detalization</param>
		/// <param name="to">Target type detalization</param>
		/// <param name="conversionType">Conversion type. See <see cref="ConversionType"/> for more details.</param>
		public MappingSchema SetConverter<TFrom,TTo>(
			Func<TFrom,TTo> func,
			DbDataType      from,
			DbDataType      to,
			ConversionType  conversionType = ConversionType.Common)
		{
			if (func == null) throw new ArgumentNullException(nameof(func));

			if (from.SystemType != typeof(TFrom))
				throw new ArgumentException($"'{nameof(from)}' parameter expects the same SystemType as in generic definition.", nameof(from));

			if (to.SystemType != typeof(TTo))
				throw new ArgumentException($"'{nameof(to)}' parameter expects the same SystemType as in generic definition.", nameof(to));

			var p  = Expression.Parameter(typeof(TFrom), "p");
			var ex = Expression.Lambda<Func<TFrom,TTo>>(Expression.Invoke(Expression.Constant(func), p), p);

			lock (_syncRoot)
			{
				Schemas[0].SetConvertInfo(from, to, conversionType, new (ex, null, func, false), true);
				ResetID();
			}

			return this;
		}

		internal LambdaExpression AddNullCheck(LambdaExpression expr)
		{
			var p = expr.Parameters[0];

			if (p.Type.IsNullableType)
			{
				expr = Expression.Lambda(
					Expression.Condition(
						ExpressionHelper.Property(p, nameof(Nullable<>.HasValue)),
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

			if (fromType.IsNullableType)
			{
				body = Expression.Condition(
					ExpressionHelper.Property(param, nameof(Nullable<>.HasValue)),
					Expression.Convert(body, type),
					new DefaultValueExpression(this, type));
			}
			else if (type.IsNullableType)
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

			if (body.Type != type)
			{
				var convertExpr = GetConvertExpression(body.Type, type);
				if (convertExpr != null)
					body = InternalExtensions.ApplyLambdaToExpression(convertExpr, body);
			}

			var expr = Expression.Lambda(body, param);
			return expr;
		}

		public Expression GenerateConvertedValueExpression(object? value, Type type)
		{
			if (value == null)
				return new DefaultValueExpression(this, type);

			var fromType  = value.GetType();
			var valueExpr = (Expression)Expression.Constant(value);
			if (fromType == type)
				return valueExpr;

			var convertLambda = GenerateSafeConvert(fromType, type);

			valueExpr = InternalExtensions.ApplyLambdaToExpression(convertLambda, valueExpr);
			return valueExpr;
		}

		static bool Simplify(ref DbDataType type)
		{
			if (!string.IsNullOrEmpty(type.DbType))
			{
				type = type.WithDbType(null);
				return true;
			}

			if (type.Precision != null || type.Scale != null)
			{
				type = type.WithScale(null).WithPrecision(null);
				return true;
			}

			if (type.Length != null)
			{
				type = type.WithLength(null);
				return true;
			}

			if (type.DataType != DataType.Undefined)
			{
				type = type.WithDataType(DataType.Undefined);
				return true;
			}

			return false;
		}

		internal ConvertInfo.LambdaInfo? GetConverter(DbDataType from, DbDataType to, bool create, ConversionType conversionType)
		{
			var conversion = TryFindExistingConversion(from, to, conversionType);

			if (conversion == null && from.SystemType.IsNullableType && to.SystemType == typeof(DataParameter))
			{
				conversion = TryFindExistingConversion(from.WithSystemType(from.SystemType.UnwrapNullableType()), to, conversionType);

				if (conversion != null)
				{
					conversion = MakeNullableDataParameterConversion(from, conversion);
					SetConvertExpression(from, to, conversion.Lambda, addNullCheck: false, conversionType: conversionType);
				}
			}

			if (conversion != null)
			{
				return conversion;
			}

			var isFromGeneric = from.SystemType is { IsGenericType: true, IsGenericTypeDefinition: false };
			var isToGeneric   = to.  SystemType is { IsGenericType: true, IsGenericTypeDefinition: false };

			if (isFromGeneric || isToGeneric)
			{
				var fromGenericArgs = isFromGeneric ? from.SystemType.GetGenericArguments() : [];
				var toGenericArgs   = isToGeneric   ? to.SystemType.  GetGenericArguments() : [];

				var args = fromGenericArgs.SequenceEqual(toGenericArgs)
					? fromGenericArgs
					: fromGenericArgs.Concat(toGenericArgs).ToArray();

				if (InitGenericConvertProvider(args))
					return GetConverter(from, to, create, conversionType);
			}

			if (create)
			{
				var ufrom = from.SystemType.UnwrapNullableType();
				var uto   = to.SystemType.  UnwrapNullableType();

				LambdaExpression? ex;
				bool              ss = false;

				if (from.SystemType != ufrom)
				{
					var li = GetConverter(new DbDataType(ufrom), to, false, conversionType);

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
						li = GetConverter(new DbDataType(ufrom), new DbDataType(uto), false, conversionType);

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
					var li = GetConverter(from, new DbDataType(uto), false, conversionType);

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

				var d = ConvertInfo.Default.Get(from, to, conversionType);

				if (d == null || d.IsSchemaSpecific)
					d = ConvertInfo.Default.Create(this, from, to, conversionType);

				return new ConvertInfo.LambdaInfo(d.CheckNullLambda, d.Lambda, null, d.IsSchemaSpecific);
			}

			return null;

			ConvertInfo.LambdaInfo? TryFindExistingConversion(DbDataType from, DbDataType to, ConversionType conversionType)
			{
				var currentFrom = from;
				do
				{
					var currentTo = to;
					do
					{
						for (var i = 0; i < Schemas.Length; i++)
						{
							var info = Schemas[i];
							var li   = info.GetConvertInfo(currentFrom, currentTo, conversionType);

							if (li != null && (i == 0 || !li.IsSchemaSpecific))
								return i == 0 ? li : new ConvertInfo.LambdaInfo(li.CheckNullLambda, li.Lambda, null, false);
						}

					} while (Simplify(ref currentTo));

				} while (Simplify(ref currentFrom));
				return null;
			}

			static ConvertInfo.LambdaInfo MakeNullableDataParameterConversion(DbDataType from, ConvertInfo.LambdaInfo conversion)
			{
				var p = Expression.Parameter(from.SystemType, conversion.Lambda.Parameters[0].Name);

				var nullableConversion = Expression.Lambda(
					Expression.Call(
						Expression.Call(
							conversion.Lambda.Body.Transform(
								(oldParam: conversion.Lambda.Parameters[0], newParam: p, defaultValue: Expression.Default(conversion.Lambda.Parameters[0].Type)),
								static (context, e) => e == context.oldParam ? Expression.Coalesce(context.newParam, context.defaultValue) : e),
							Methods.LinqToDB.DataParameter.ClearValue,
							Expression.Equal(p, Expression.Constant(null, p.Type))),
						Methods.LinqToDB.DataParameter.WithType,
						Expression.Constant(from)),
					p);

				if (conversion.Lambda != conversion.CheckNullLambda)
				{
					throw new InvalidOperationException();
				}

				return new ConvertInfo.LambdaInfo(nullableConversion, nullableConversion, null, conversion.IsSchemaSpecific);
			}
		}

		Expression ReduceDefaultValue(Expression expr)
		{
			return expr.Transform(ReduceDefaultValueTransformer);
		}

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

		void InitMetadataReaders(bool combine)
		{
			if (!combine && Schemas[0].MetadataReader == null && Schemas.Length > 1)
			{
				Schemas[0].MetadataReader = Schemas[1].MetadataReader;
			}
			else if (Schemas.Length > 1)
			{
				List<IMetadataReader>? readers = null;
				HashSet<string>?       hash    = null;

				for (var i = 0; i < Schemas.Length; i++)
				{
					var s = Schemas[i];
					if (s.MetadataReader != null)
						AddMetadataReaderInternal(s.MetadataReader);
				}

				if (readers != null)
					Schemas[0].MetadataReader = new MetadataReader(readers.ToArray());

				void AddMetadataReaderInternal(IMetadataReader reader)
				{
					if (!(hash ??= new()).Add(reader.GetObjectID()))
						return;

					if (reader is MetadataReader metadataReader)
					{
						foreach (var mr in metadataReader.Readers)
							AddMetadataReaderInternal(mr);
					}
					else
						(readers ??= new()).Add(reader);
				}
			}
		}

		/// <summary>
		/// Adds additional metadata attributes provider to current schema.
		/// </summary>
		/// <param name="reader">Metadata attributes provider.</param>
		public void AddMetadataReader(IMetadataReader reader)
		{
			lock (_syncRoot)
			{
				var currentReader = Schemas[0].MetadataReader;
				if (currentReader != null)
				{
					var readers = new IMetadataReader[currentReader.Readers.Count + 1];

					readers[0] = reader;
					for (var i = 0; i < currentReader.Readers.Count; i++)
						readers[i + 1] = currentReader.Readers[i];

					Schemas[0].MetadataReader = new MetadataReader(readers);
				}
				else
					Schemas[0].MetadataReader = new MetadataReader(reader);

				(_cache, _firstOnlyCache) = CreateAttributeCaches();

				ResetID();
			}
		}

		/// <summary>
		/// Gets attributes of specified type, associated with specified type.
		/// </summary>
		/// <typeparam name="T">Mapping attribute type (must inherit <see cref="MappingAttribute"/>).</typeparam>
		/// <param name="type">Attributes owner type.</param>
		/// <returns>Attributes of specified type.</returns>
		private T[] GetAllAttributes<T>(Type type)
			where T : MappingAttribute
		{
			return Schemas[0].MetadataReader?.GetAttributes<T>(type) ?? [];
		}

		/// <summary>
		/// Gets attributes of specified type, associated with specified type member.
		/// </summary>
		/// <typeparam name="T">Mapping attribute type (must inherit <see cref="MappingAttribute"/>).</typeparam>
		/// <param name="type">Member's owner type.</param>
		/// <param name="memberInfo">Attributes owner member.</param>
		/// <returns>Attributes of specified type.</returns>
		private T[] GetAllAttributes<T>(Type type, MemberInfo memberInfo)
			where T : MappingAttribute
		{
			return Schemas[0].MetadataReader?.GetAttributes<T>(type, memberInfo) ?? [];
		}

		private (MappingAttributesCache cache, MappingAttributesCache firstOnlyCache) CreateAttributeCaches()
		{
			var cache = new MappingAttributesCache(
				(sourceOwner, source) =>
				{
					var attrs = sourceOwner != null
						? GetAllAttributes<MappingAttribute>(sourceOwner, (MemberInfo)source)
						: GetAllAttributes<MappingAttribute>((Type)source);

					if (attrs.Length == 0)
						return attrs;

					List<MappingAttribute>? list = null;

					foreach (var c in ConfigurationList)
					{
						foreach (var a in attrs)
							if (a.Configuration == c)
								(list ??= new()).Add(a);
					}

					foreach (var attribute in attrs)
						if (string.IsNullOrEmpty(attribute.Configuration))
							(list ??= new()).Add(attribute);

					return list == null ? [] : list.ToArray();
				});

			var firstOnlyCache = new MappingAttributesCache(
				(sourceOwner, source) =>
				{
					var attrs = sourceOwner != null
						? GetAllAttributes<MappingAttribute>(sourceOwner, (MemberInfo)source)
						: GetAllAttributes<MappingAttribute>((Type)source);

					if (attrs.Length == 0)
						return attrs;

					List<MappingAttribute>? list = null;

					foreach (var c in ConfigurationList)
					{
						foreach (var a in attrs)
							if (a.Configuration == c)
								(list ??= new()).Add(a);
						if (list != null)
							return list.ToArray();
					}

					foreach (var attribute in attrs)
						if (string.IsNullOrEmpty(attribute.Configuration))
							(list ??= new()).Add(attribute);

					return list == null ? [] : list.ToArray();
				});

			return (cache, firstOnlyCache);
		}

		MappingAttributesCache _cache;
		MappingAttributesCache _firstOnlyCache;

		/// <summary>
		/// Gets attributes of specified type, associated with specified type.
		/// Attributes are filtered by schema's configuration names (see <see cref="ConfigurationList"/>).
		/// </summary>
		/// <typeparam name="T">Mapping attribute type (must inherit <see cref="MappingAttribute"/>).</typeparam>
		/// <param name="type">Attributes owner type.</param>
		/// <returns>Attributes of specified type.</returns>
		public T[] GetAttributes<T>(Type type)
			where T : MappingAttribute
			=> _cache.GetMappingAttributes<T>(type);

		/// <summary>
		/// Gets attributes of specified type, associated with specified type member.
		/// Attributes are filtered by schema's configuration names (see <see cref="ConfigurationList"/>).
		/// </summary>
		/// <typeparam name="T">Mapping attribute type (must inherit <see cref="MappingAttribute"/>).</typeparam>
		/// <param name="type">Member's owner type.</param>
		/// <param name="memberInfo">Attributes owner member.</param>
		/// <param name="forFirstConfiguration">If <c>true</c> - returns only attributes for first configuration with attributes from <see cref="ConfigurationList"/>.</param>
		/// <returns>Attributes of specified type.</returns>
		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool forFirstConfiguration = false)
			where T : MappingAttribute
		=> forFirstConfiguration
			? _firstOnlyCache.GetMappingAttributes<T>(type, memberInfo)
			: _cache         .GetMappingAttributes<T>(type, memberInfo);

		/// <summary>
		/// Gets attribute of specified type, associated with specified type.
		/// Attributes are filtered by schema's configuration names (see <see cref="ConfigurationList"/>).
		/// </summary>
		/// <typeparam name="T">Mapping attribute type (must inherit <see cref="MappingAttribute"/>).</typeparam>
		/// <param name="type">Attribute owner type.</param>
		/// <returns>First found attribute of specified type or <c>null</c>, if no attributes found.</returns>
		public T? GetAttribute<T>(Type type)
			where T : MappingAttribute
		{
			var attrs = GetAttributes<T>(type);
			return attrs.Length == 0 ? null : attrs[0];
		}

		/// <summary>
		/// Gets attribute of specified type, associated with specified type member.
		/// Attributes are filtered by schema's configuration names (see <see cref="ConfigurationList"/>).
		/// </summary>
		/// <typeparam name="T">Mapping attribute type (must inherit <see cref="MappingAttribute"/>).</typeparam>
		/// <param name="type">Member's owner type.</param>
		/// <param name="memberInfo">Attribute owner member.</param>
		/// <returns>First found attribute of specified type or <c>null</c>, if no attributes found.</returns>
		public T? GetAttribute<T>(Type type, MemberInfo memberInfo)
			where T : MappingAttribute
		{
			var attrs = GetAttributes<T>(type, memberInfo);
			return attrs.Length == 0 ? null : attrs[0];
		}

		/// <summary>
		/// Returns <c>true</c> if attribute of specified type, associated with specified type.
		/// Attributes are filtered by schema's configuration names (see <see cref="ConfigurationList"/>).
		/// </summary>
		/// <typeparam name="T">Mapping attribute type (must inherit <see cref="MappingAttribute"/>).</typeparam>
		/// <param name="type">Attribute owner type.</param>
		/// <returns>Returns <c>true</c> if attribute of specified type, associated with specified type.</returns>
		public bool HasAttribute<T>(Type type)
			where T : MappingAttribute
		{
			return GetAttributes<T>(type).Length > 0;
		}

		/// <summary>
		/// Returns <c>true</c> if attribute of specified type, associated with specified type member.
		/// Attributes are filtered by schema's configuration names (see <see cref="ConfigurationList"/>).
		/// </summary>
		/// <typeparam name="T">Mapping attribute type (must inherit <see cref="MappingAttribute"/>).</typeparam>
		/// <param name="type">Member's owner type.</param>
		/// <param name="memberInfo">Attribute owner member.</param>
		/// <returns>Returns <c>true</c> if attribute of specified type, associated with specified type member.</returns>
		public bool HasAttribute<T>(Type type, MemberInfo memberInfo)
			where T : MappingAttribute
		{
			return GetAttributes<T>(type, memberInfo).Length > 0;
		}

		/// <summary>
		/// Gets the dynamic columns defined on given type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>All dynamic columns defined on given type.</returns>
		public MemberInfo[] GetDynamicColumns(Type type)
		{
			return Schemas[0].MetadataReader?.GetDynamicColumns(type) ?? [];
		}

		#endregion

		#region Configuration

		int? _configurationID;
		/// <summary>
		/// Unique schema configuration identifier. For internal use only.
		/// </summary>
		int IConfigurationID.ConfigurationID => _configurationID ??= GenerateID();

		protected internal virtual int GenerateID()
		{
			using var idBuilder = new IdentifierBuilder();

			idBuilder
				.Add(GetType())
				.Add(ValueToSqlConverter)
				;

			lock (_syncRoot)
				foreach (var s in Schemas)
					idBuilder.Add(s.ConfigurationID);

			return idBuilder.CreateID();
		}

		internal void ResetID()
		{
#if DEBUG
			if (!IsLockable)
			{
			}

			if (_configurationID != null)
				Debug.WriteLine($"ResetID => '{DisplayID}'");
#endif

			_configurationID = null;
			Schemas[0].ResetID();
		}

		/// <summary>
		/// Gets configurations, associated with current mapping schema.
		/// </summary>
		public string[] ConfigurationList
		{
			get => field ??= Schemas
				.Select(s => s.Configuration)
				.Where(s => !string.IsNullOrEmpty(s))
				.Distinct()
				.ToArray();
		}

		public string DisplayID
		{
			get
			{
				var list = ConfigurationList.Aggregate("", static (s1, s2) => s1.Length == 0 ? s2 : s1 + "." + s2);
				return FormattableString.Invariant($"{GetType().Name} : ({_configurationID}) {list}");
			}
		}

		#endregion

		#region DefaultMappingSchema

		internal MappingSchema(MappingSchemaInfo mappingSchemaInfo)
		{
			Schemas = new[] { mappingSchemaInfo };

			ValueToSqlConverter = new ();

			InitMetadataReaders(false);

			(_cache, _firstOnlyCache) = CreateAttributeCaches();
		}

		/// <summary>
		/// Default mapping schema, used by LINQ to DB, when more specific mapping schema not provided.
		/// </summary>
		public static MappingSchema Default = new DefaultMappingSchema();

		sealed class DefaultMappingSchema : LockedMappingSchema
		{
			public DefaultMappingSchema() : base(new DefaultMappingSchemaInfo())
			{
				AddScalarType(typeof(char),            new SqlDataType(DataType.NChar, typeof(char),  1, null, null, null));
				AddScalarType(typeof(string),          DataType.NVarChar);
				AddScalarType(typeof(decimal),         DataType.Decimal);
				AddScalarType(typeof(DateTime),        DataType.DateTime2);
				AddScalarType(typeof(DateTimeOffset),  DataType.DateTimeOffset);
				AddScalarType(typeof(TimeSpan),        DataType.Time);
#if SUPPORTS_DATEONLY
				AddScalarType(typeof(DateOnly),        DataType.Date);
				AddScalarType(typeof(TimeOnly),        DataType.Time);
				AddScalarType(typeof(Int128),          DataType.Int128);
				AddScalarType(typeof(UInt128),         DataType.UInt128);
#endif
				AddScalarType(typeof(byte[]),          DataType.VarBinary);
				AddScalarType(typeof(Binary),          DataType.VarBinary);
				AddScalarType(typeof(Guid),            DataType.Guid);
				AddScalarType(typeof(object),          DataType.Variant);
				AddScalarType(typeof(XmlDocument),     DataType.Xml);
				AddScalarType(typeof(XDocument),       DataType.Xml);
				AddScalarType(typeof(bool),            DataType.Boolean);
				AddScalarType(typeof(sbyte),           DataType.SByte);
				AddScalarType(typeof(short),           DataType.Int16);
				AddScalarType(typeof(int),             DataType.Int32);
				AddScalarType(typeof(long),            DataType.Int64);
				AddScalarType(typeof(byte),            DataType.Byte);
				AddScalarType(typeof(ushort),          DataType.UInt16);
				AddScalarType(typeof(uint),            DataType.UInt32);
				AddScalarType(typeof(ulong),           DataType.UInt64);
				AddScalarType(typeof(float),           DataType.Single);
				AddScalarType(typeof(double),          DataType.Double);

				AddScalarType(typeof(BigInteger),      DataType.Decimal);
				AddScalarType(typeof(BitArray),        DataType.BitArray);

				SetConverter<DBNull, object?>(static _ => null);

				// explicitly specify old ToString client-side conversions for some types after we added support for ToString(InvariantCulture) to conversion generators
				SetConverter<DateTime, string>(static v => v.ToString("yyyy-MM-dd hh:mm:ss", DateTimeFormatInfo.InvariantInfo));
#if SUPPORTS_DATEONLY
				SetConverter<DateOnly, string>(static v => v.ToString("yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo));
#endif

				ValueToSqlConverter.SetDefaults();
			}

			protected internal override int GenerateID() => 0;

			sealed class DefaultMappingSchemaInfo : MappingSchemaInfo
			{
				public DefaultMappingSchemaInfo() : base("")
				{
					MetadataReader = MetadataReader.Default;
				}

				protected override int GenerateID()
				{
					return Default.GenerateID();
				}

				public override void ResetID()
				{
				}
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
			type = type.UnwrapNullableType();

			foreach (var info in Schemas)
			{
				var o = info.GetScalarType(type);
				if (o.HasValue)
					return o.Value;
			}

			var attr = GetAttribute<ScalarTypeAttribute>(type);
			var ret  = false;

			if (attr != null)
			{
				ret = attr.IsScalar;
			}
			else
			{
#pragma warning disable CS0618 // Type or member is obsolete
				if (type.IsEnum || type.IsPrimitive || (Common.Configuration.IsStructIsScalarType && type.IsValueType))
					ret = true;
#pragma warning restore CS0618 // Type or member is obsolete
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
			lock (_syncRoot)
			{
				Schemas[0].SetScalarType(type, isScalarType);
				ResetID();
			}
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
				if (o.HasValue)
					return o.Value;
			}

			return SqlDataType.Undefined;
		}

		/// <summary>
		/// Returns database type mapping information for specified type.
		/// </summary>
		/// <param name="type">Mapped type.</param>
		/// <returns>Database type information.</returns>
		public DbDataType GetDbDataType(Type type)
		{
			return GetDataType(type).Type;
		}

		/// <summary>
		/// Associate specified type with LINQ to DB data type.
		/// </summary>
		/// <param name="type">Mapped type.</param>
		/// <param name="dataType">LINQ to DB data type.</param>
		public void SetDataType(Type type, DataType dataType)
		{
			lock (_syncRoot)
			{
				Schemas[0].SetDataType(type, dataType);
				ResetID();
			}
		}

		/// <summary>
		/// Associate specified type with database data type.
		/// </summary>
		/// <param name="type">Mapped type.</param>
		/// <param name="dataType">Database data type.</param>
		public void SetDataType(Type type, SqlDataType dataType)
		{
			lock (_syncRoot)
			{
				Schemas[0].SetDataType(type, dataType);
				ResetID();
			}
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

			var underlyingType = type.UnwrapNullableType();

			if (underlyingType.IsEnum)
			{
				var attrs = new List<MapValueAttribute>();

				foreach (var f in underlyingType.GetFields())
					if ((f.Attributes & EnumField) == EnumField)
						attrs.AddRange(GetAttributes<MapValueAttribute>(underlyingType, f));

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
						return GetDataType(type);

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

			return (_mapValues ??= new ConcurrentDictionary<Type, MapValue[]?>())
				.GetOrAdd(
					type,
					type =>
					{
						var underlyingType = type.UnwrappedNullableType;

						if (underlyingType.IsEnum)
						{
							List<MapValue>? fields = null;

							foreach (var f in underlyingType.GetFields())
								if ((f.Attributes & EnumField) == EnumField)
								{
									var attrs = GetAttributes<MapValueAttribute>(underlyingType, f);
									(fields ??= new()).Add(new MapValue(Enum.Parse(underlyingType, f.Name, false), attrs));
								}

							if (fields?.Any(f => f.MapValues.Length > 0) == true)
								return fields.ToArray();
						}

						return null;
					}
				);
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
				return field ??= Schemas switch
				{
					[{ ColumnNameComparer: { } comparer }, ..] => comparer,
					_ => Schemas
						.Select(static s => s.ColumnNameComparer)
						.FirstOrDefault(static s => s != null)
						?? StringComparer.Ordinal
				};
			}

			set
			{
				Schemas[0].ColumnNameComparer = field = value;
				_configurationID = null;
			}
		}

		#endregion

		#region EntityDescriptor

		/// <summary>
		/// Gets or sets application-wide action, called when the EntityDescriptor is created.
		/// Could be used to adjust created descriptor before use.
		/// Not called, when connection has connection-level callback defined (<see cref="ConnectionOptions.OnEntityDescriptorCreated" />).
		/// </summary>
		public static Action<MappingSchema, IEntityChangeDescriptor>? EntityDescriptorCreatedCallback { get; set; }

		private static MemoryCache<(Type entityType, int schemaId),EntityDescriptor> EntityDescriptorsCache { get; } = new (new ());

		/// <summary>
		/// Returns mapped entity descriptor.
		/// </summary>
		/// <param name="type">Mapped type.</param>
		/// <param name="onEntityDescriptorCreated">Action, called when new descriptor instance created.
		/// When set to <c>null</c>, <see cref="EntityDescriptorCreatedCallback" /> callback used.</param>
		/// <returns>Mapping descriptor.</returns>
		public EntityDescriptor GetEntityDescriptor(Type type, Action<MappingSchema, IEntityChangeDescriptor>? onEntityDescriptorCreated = null)
		{
			var ed = EntityDescriptorsCache.GetOrCreate(
				(entityType: type, ((IConfigurationID)this).ConfigurationID),
				(mappingSchema: this, callback: onEntityDescriptorCreated ?? EntityDescriptorCreatedCallback),
				static (o, context) =>
				{
					o.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;
					var edNew = new EntityDescriptor(context.mappingSchema, o.Key.entityType, context.callback);
					context.callback?.Invoke(context.mappingSchema, edNew);
					return edNew;
				});

			return ed;
		}

		/// <summary>
		/// Enumerates types registered by <see cref="FluentMappingBuilder"/>.
		/// </summary>
		/// <returns>
		/// Returns all types, mapped by fluent mappings.
		/// </returns>
		public IEnumerable<Type> GetDefinedTypes()
		{
			return Schemas.SelectMany(static s => s.GetRegisteredTypes()).Distinct();
		}

		/// <summary>
		/// Clears <see cref="EntityDescriptor"/> cache.
		/// </summary>
		public static void ClearCache()
		{
			EntityDescriptorsCache.Clear();
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
			lock (_syncRoot)
			{
				Schemas[0].SetDefaultFromEnumType(enumType, defaultFromType);
				ResetID();
			}
		}

		#endregion

		internal IEnumerable<T> SortByConfiguration<T>(IEnumerable<T> attributes)
			where T : MappingAttribute
		{
			return attributes
				.Select(attr =>
				{
					var config = attr.Configuration;
					var index  = Array.IndexOf(ConfigurationList, config);
					var order  = index == -1 ? ConfigurationList.Length : index;
					return (attr, order);
				})
				.OrderBy(static _ => _.order)
				.Select (static _ => _.attr);
		}

		public virtual bool IsLockable => false;
		public virtual bool IsLocked   => false;

		internal virtual MappingSchemaInfo CreateMappingSchemaInfo(string configuration, MappingSchema mappingSchema)
		{
			return new (configuration);
		}

		public bool Equals(MappingSchema? other)
		{
			if (other is null)                return false;
			if (ReferenceEquals(this, other)) return true;

			return
				((IConfigurationID)this).ConfigurationID == ((IConfigurationID)other).ConfigurationID &&
				((IConfigurationID)this).ConfigurationID != -1;
		}

		public override bool Equals(object? obj)
		{
			if (obj is null)                return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;

			return Equals((MappingSchema)obj);
		}

		public override int GetHashCode()
		{
			return ((IConfigurationID)this).ConfigurationID;
		}
	}
}
