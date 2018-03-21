using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
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
			: this(null, (MappingSchema[])null)
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
		/// <see cref="ProviderName"/> for standard names.</param>
		public MappingSchema(string configuration/* ??? */)
			: this(configuration, null)
		{
		}

		/// <summary>
		/// Creates mapping schema with specified configuration name and base mapping schemas.
		/// </summary>
		/// <param name="configuration">Mapping schema configuration name.
		/// <see cref="ProviderName"/> for standard names.</param>
		/// <param name="schemas">Base mapping schemas.</param>
		public MappingSchema(string configuration, params MappingSchema[] schemas)
		{
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

				Schemas             = schemaList.OrderBy(_ => _.Value).Select(_ => _.Key).ToArray();
				ValueToSqlConverter = new ValueToSqlConverter(baseConverters.OrderBy(_ => _.Value).Select(_ => _.Key).ToArray());
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
		public object GetDefaultValue(Type type)
		{
			foreach (var info in Schemas)
			{
				var o = info.GetDefaultValue(type);
				if (o.IsSome)
					return o.Value;
			}

			if (type.IsEnumEx())
			{
				var mapValues = GetMapValues(type);

				if (mapValues != null)
				{
					var fields =
						from f in mapValues
						where f.MapValues.Any(a => a.Value == null)
						select f.OrigValue;

					var value = fields.FirstOrDefault();

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
		public void SetDefaultValue(Type type, object value)
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

			if (type.IsEnumEx())
			{
				var mapValues = GetMapValues(type);

				if (mapValues != null)
				{
					var fields =
						from f in mapValues
						where f.MapValues.Any(a => a.Value == null)
						select f.OrigValue;

					var value = fields.FirstOrDefault();

					if (value != null)
					{
						SetCanBeNull(type, true);
						return true;
					}
				}
			}

			return type.IsClassEx() || type.IsNullable();
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
			return Schemas.Aggregate(false, (cur, info) => cur || info.InitGenericConvertProvider(types, this));
		}

		/// <summary>
		/// Adds generic type conversions provider.
		/// Type converter must implement <see cref="IGenericInfoProvider"/> interface.
		/// <see cref="IGenericInfoProvider"/> for more details and examples.
		/// </summary>
		/// <param name="type">Generic type conversions provider.</param>
		public void SetGenericConvertProvider(Type type)
		{
			if (!type.IsGenericTypeDefinitionEx())
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
		public T ChangeTypeTo<T>(object value)
		{
			return Converter.ChangeTypeTo<T>(value, this);
		}

		/// <summary>
		/// Converts value to specified type.
		/// </summary>
		/// <param name="value">Value to convert.</param>
		/// <param name="conversionType">Target type.</param>
		/// <returns>Converted value.</returns>
		public object ChangeType(object value, Type conversionType)
		{
			return Converter.ChangeType(value, conversionType, this);
		}

		/// <summary>
		/// Converts enum value to database value.
		/// </summary>
		/// <param name="value">Enum value.</param>
		/// <returns>Database value.</returns>
		public object EnumToValue(Enum value)
		{
			var toType = ConvertBuilder.GetDefaultMappingFromEnumType(this, value.GetType());
			return Converter.ChangeType(value, toType, this);
		}

		/// <summary>
		/// Returns custom value conversion expression from <paramref name="from"/> type to <paramref name="to"/> type if it
		/// is defined in mapping schema, or <c>null</c> otherwise.
		/// </summary>
		/// <param name="from">Source type.</param>
		/// <param name="to">Target type.</param>
		/// <returns>Conversion expression or <c>null</c>, if conversion is not defined.</returns>
		public virtual LambdaExpression TryGetConvertExpression(Type @from, Type to)
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
		public Expression<Func<TFrom,TTo>> GetConvertExpression<TFrom,TTo>(bool checkNull = true, bool createDefault = true)
		{
			return (Expression<Func<TFrom, TTo>>)GetConvertExpression(typeof(TFrom), typeof(TTo), checkNull, createDefault);
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
		public LambdaExpression GetConvertExpression(Type from, Type to, bool checkNull = true, bool createDefault = true)
		{
			var li = GetConverter(from, to, createDefault);
			return li == null ? null : (LambdaExpression)ReduceDefaultValue(checkNull ? li.CheckNullLambda : li.Lambda);
		}

		/// <summary>
		/// Returns conversion delegate for conversion from <typeparamref name="TFrom"/> type to <typeparamref name="TTo"/> type.
		/// </summary>
		/// <typeparam name="TFrom">Source type.</typeparam>
		/// <typeparam name="TTo">Target type.</typeparam>
		/// <returns>Conversion delegate.</returns>
		public Func<TFrom,TTo> GetConverter<TFrom,TTo>()
		{
			var li = GetConverter(typeof(TFrom), typeof(TTo), true);

			if (li.Delegate == null)
			{
				var rex = (Expression<Func<TFrom,TTo>>)ReduceDefaultValue(li.CheckNullLambda);
				var l   = rex.Compile();

				Schemas[0].SetConvertInfo(typeof(TFrom), typeof(TTo), new ConvertInfo.LambdaInfo(li.CheckNullLambda, null, l, li.IsSchemaSpecific));

				return l;
			}

			return (Func<TFrom,TTo>)li.Delegate;
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
			[JetBrains.Annotations.NotNull] Type fromType,
			[JetBrains.Annotations.NotNull] Type toType,
			[JetBrains.Annotations.NotNull] LambdaExpression expr,
			bool addNullCheck = true)
		{
			if (fromType == null) throw new ArgumentNullException("fromType");
			if (toType   == null) throw new ArgumentNullException("toType");
			if (expr     == null) throw new ArgumentNullException("expr");

			var ex = addNullCheck && expr.Find(Converter.IsDefaultValuePlaceHolder) == null?
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
			[JetBrains.Annotations.NotNull] Expression<Func<TFrom,TTo>> expr,
			bool addNullCheck = true)
		{
			if (expr == null) throw new ArgumentNullException("expr");

			var ex = addNullCheck && expr.Find(Converter.IsDefaultValuePlaceHolder) == null?
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
			[JetBrains.Annotations.NotNull] Expression<Func<TFrom,TTo>> checkNullExpr,
			[JetBrains.Annotations.NotNull] Expression<Func<TFrom,TTo>> expr)
		{
			if (expr == null) throw new ArgumentNullException("expr");

			Schemas[0].SetConvertInfo(typeof(TFrom), typeof(TTo), new ConvertInfo.LambdaInfo(checkNullExpr, expr, null, false));
		}

		/// <summary>
		/// Specify conversion delegate for conversion from <typeparamref name="TFrom"/> type to <typeparamref name="TTo"/> type.
		/// </summary>
		/// <typeparam name="TFrom">Source type.</typeparam>
		/// <typeparam name="TTo">Target type.</typeparam>
		/// <param name="func">Conversion delegate.</param>
		public void SetConverter<TFrom,TTo>([JetBrains.Annotations.NotNull] Func<TFrom,TTo> func)
		{
			if (func == null) throw new ArgumentNullException("func");

			var p  = Expression.Parameter(typeof(TFrom), "p");
			var ex = Expression.Lambda<Func<TFrom,TTo>>(Expression.Invoke(Expression.Constant(func), p), p);

			Schemas[0].SetConvertInfo(typeof(TFrom), typeof(TTo), new ConvertInfo.LambdaInfo(ex, null, func, false));
		}

		LambdaExpression AddNullCheck(LambdaExpression expr)
		{
			var p = expr.Parameters[0];

			if (p.Type.IsNullable())
				return Expression.Lambda(
					Expression.Condition(
						Expression.PropertyOrField(p, "HasValue"),
						expr.Body,
						new DefaultValueExpression(this, expr.Body.Type)),
					expr.Parameters);

			if (p.Type.IsClassEx())
				return Expression.Lambda(
					Expression.Condition(
						Expression.NotEqual(p, Expression.Constant(null, p.Type)),
						expr.Body,
						new DefaultValueExpression(this, expr.Body.Type)),
					expr.Parameters);

			return expr;
		}

		ConvertInfo.LambdaInfo GetConverter(Type from, Type to, bool create)
		{
			for (var i = 0; i < Schemas.Length; i++)
			{
				var info = Schemas[i];
				var li   = info.GetConvertInfo(@from, to);

				if (li != null && (i == 0 || !li.IsSchemaSpecific))
					return i == 0 ? li : new ConvertInfo.LambdaInfo(li.CheckNullLambda, li.Lambda, null, false);
			}

			var isFromGeneric = from.IsGenericTypeEx() && !from.IsGenericTypeDefinitionEx();
			var isToGeneric   = to.  IsGenericTypeEx() && !to.  IsGenericTypeDefinitionEx();

			if (isFromGeneric || isToGeneric)
			{
				var fromGenericArgs = isFromGeneric ? from.GetGenericArgumentsEx() : Array<Type>.Empty;
				var toGenericArgs   = isToGeneric   ? to.  GetGenericArgumentsEx() : Array<Type>.Empty;

				var args = fromGenericArgs.SequenceEqual(toGenericArgs) ?
					fromGenericArgs : fromGenericArgs.Concat(toGenericArgs).ToArray();

				if (InitGenericConvertProvider(args))
					return GetConverter(from, to, create);
			}

			if (create)
			{
				var ufrom = from.ToNullableUnderlying();
				var uto   = to.  ToNullableUnderlying();

				LambdaExpression ex;
				bool             ss = false;

				if (from != ufrom)
				{
					var li = GetConverter(ufrom, to, false);

					if (li != null)
					{
						var b  = li.CheckNullLambda.Body;
						var ps = li.CheckNullLambda.Parameters;

						// For int? -> byte try to find int -> byte and convert int to int?
						//
						var p = Expression.Parameter(from, ps[0].Name);

						ss = li.IsSchemaSpecific;
						ex = Expression.Lambda(
							b.Transform(e => e == ps[0] ? Expression.Convert(p, ufrom) : e),
							p);
					}
					else if (to != uto)
					{
						li = GetConverter(ufrom, uto, false);

						if (li != null)
						{
							var b  = li.CheckNullLambda.Body;
							var ps = li.CheckNullLambda.Parameters;

							// For int? -> byte? try to find int -> byte and convert int to int? and result to byte?
							//
							var p = Expression.Parameter(from, ps[0].Name);

							ss = li.IsSchemaSpecific;
							ex = Expression.Lambda(
								Expression.Convert(
									b.Transform(e => e == ps[0] ? Expression.Convert(p, ufrom) : e),
									to),
								p);
						}
						else
							ex = null;
					}
					else
						ex = null;
				}
				else if (to != uto)
				{
					// For int? -> byte? try to find int -> byte and convert int to int? and result to byte?
					//
					var li = GetConverter(from, uto, false);

					if (li != null)
					{
						var b  = li.CheckNullLambda.Body;
						var ps = li.CheckNullLambda.Parameters;

						ss = li.IsSchemaSpecific;
						ex = Expression.Lambda(Expression.Convert(b, to), ps);
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
			return expr.Transform(e =>
				Converter.IsDefaultValuePlaceHolder(e) ?
					Expression.Constant(GetDefaultValue(e.Type), e.Type) :
					e);
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
			SetConvertExpression((SByte     v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((SByte?    v) =>           v.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>             SByte.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>     (SByte?)SByte.Parse(s, info.NumberFormat));

			SetConvertExpression((Int16     v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((Int16?    v) =>           v.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>             Int16.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>     (Int16?)Int16.Parse(s, info.NumberFormat));

			SetConvertExpression((Int32     v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((Int32?    v) =>           v.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>             Int32.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>     (Int32?)Int32.Parse(s, info.NumberFormat));

			SetConvertExpression((Int64     v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((Int64?    v) =>           v.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>             Int64.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>     (Int64?)Int64.Parse(s, info.NumberFormat));

			SetConvertExpression((Byte      v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((Byte?     v) =>           v.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>              Byte.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>       (Byte?)Byte.Parse(s, info.NumberFormat));

			SetConvertExpression((UInt16    v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((UInt16?   v) =>           v.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>            UInt16.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>   (UInt16?)UInt16.Parse(s, info.NumberFormat));

			SetConvertExpression((UInt32    v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((UInt32?   v) =>           v.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>            UInt32.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>   (UInt32?)UInt32.Parse(s, info.NumberFormat));

			SetConvertExpression((UInt64    v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((UInt64?   v) =>           v.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>            UInt64.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>   (UInt64?)UInt64.Parse(s, info.NumberFormat));

			SetConvertExpression((Single    v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((Single?   v) =>           v.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>            Single.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>   (Single?)Single.Parse(s, info.NumberFormat));

			SetConvertExpression((Double    v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((Double?   v) =>           v.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>            Double.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) =>   (Double?)Double.Parse(s, info.NumberFormat));

			SetConvertExpression((Decimal   v) =>           v.      ToString(info.NumberFormat));
			SetConvertExpression((Decimal?  v) =>           v.Value.ToString(info.NumberFormat));
			SetConvertExpression((string    s) =>           Decimal.Parse(s, info.NumberFormat));
			SetConvertExpression((string    s) => (Decimal?)Decimal.Parse(s, info.NumberFormat));

			SetConvertExpression((DateTime  v) =>                       v.      ToString(info.DateTimeFormat));
			SetConvertExpression((DateTime? v) =>                       v.Value.ToString(info.DateTimeFormat));
			SetConvertExpression((string    s) =>                      DateTime.Parse(s, info.DateTimeFormat));
			SetConvertExpression((string    s) =>           (DateTime?)DateTime.Parse(s, info.DateTimeFormat));

			SetConvertExpression((DateTimeOffset  v) =>                 v.      ToString(info.DateTimeFormat));
			SetConvertExpression((DateTimeOffset? v) =>                 v.Value.ToString(info.DateTimeFormat));
			SetConvertExpression((string  s) =>                  DateTimeOffset.Parse(s, info.DateTimeFormat));
			SetConvertExpression((string  s) => (DateTimeOffset?)DateTimeOffset.Parse(s, info.DateTimeFormat));
		}

		#endregion

		#region MetadataReader

		readonly object _metadataReadersSyncRoot = new object();

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

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
		/// <summary>
		/// Gets or sets metadata attributes provider for current schema.
		/// Metadata providers, shipped with LINQ to DB:
		/// - <see cref="LinqToDB.Metadata.MetadataReader"/> - aggregation metadata provider over collection of other providers;
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
		/// - <see cref="XmlAttributeReader"/> - XML-based mappings metadata provider.
		/// </summary>
#endif
		public IMetadataReader MetadataReader
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

		IMetadataReader[] _metadataReaders;
		IMetadataReader[]  MetadataReaders
		{
			get
			{
				if (_metadataReaders == null)
					lock (_metadataReadersSyncRoot)
						if (_metadataReaders == null)
							InitMetadataReaders();

				return _metadataReaders;
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
			var q =
				from mr in MetadataReaders
				from a in mr.GetAttributes<T>(type, inherit)
				select a;

			return q.ToArray();
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
			var q =
				from mr in MetadataReaders
				from a in mr.GetAttributes<T>(type, memberInfo, inherit)
				select a;

			return q.ToArray();
		}

		/// <summary>
		/// Gets attribute of specified type, associated with specified type.
		/// </summary>
		/// <typeparam name="T">Attribute type.</typeparam>
		/// <param name="type">Attribute owner type.</param>
		/// <param name="inherit">If <c>true</c> - include inherited attribute.</param>
		/// <returns>First found attribute of specified type or <c>null</c>, if no attributes found.</returns>
		public T GetAttribute<T>(Type type, bool inherit = true)
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
		public T GetAttribute<T>(Type type, MemberInfo memberInfo, bool inherit = true)
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
		/// <returns>Attributes of specified type.</returns>
		public T[] GetAttributes<T>(Type type, Func<T,string> configGetter, bool inherit = true)
			where T : Attribute
		{
			var list  = new List<T>();
			var attrs = GetAttributes<T>(type, inherit);

			foreach (var c in ConfigurationList)
				foreach (var a in attrs)
					if (configGetter(a) == c)
						list.Add(a);

			return list.Concat(attrs.Where(a => string.IsNullOrEmpty(configGetter(a)))).ToArray();
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
		/// <returns>Attributes of specified type.</returns>
		public T[] GetAttributes<T>(Type type, MemberInfo memberInfo, Func<T,string> configGetter, bool inherit = true)
			where T : Attribute
		{
			var list  = new List<T>();
			var attrs = GetAttributes<T>(type, memberInfo, inherit);

			foreach (var c in ConfigurationList)
				foreach (var a in attrs)
					if (configGetter(a) == c)
						list.Add(a);

			return list.Concat(attrs.Where(a => string.IsNullOrEmpty(configGetter(a)))).ToArray();
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
		public T GetAttribute<T>(Type type, Func<T,string> configGetter, bool inherit = true)
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
		public T GetAttribute<T>(Type type, MemberInfo memberInfo, Func<T,string> configGetter, bool inherit = true)
			where T : Attribute
		{
			var attrs = GetAttributes(type, memberInfo, configGetter, inherit);
			return attrs.Length == 0 ? null : attrs[0];
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

		private string _configurationID;
		// TODO: V2 - make internal
		/// <summary>
		/// Unique schema configuration identifier. For internal use only.
		/// </summary>
		public  string  ConfigurationID
		{
			get { return _configurationID ?? (_configurationID = string.Join(".", ConfigurationList)); }
		}

		private string[] _configurationList;
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
						if (!string.IsNullOrEmpty(s.Configuration) && hash.Add(s.Configuration))
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
				AddScalarType(typeof(char),            DataType.NChar);
				AddScalarType(typeof(char?),           DataType.NChar);
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

				ValueToSqlConverter.SetDefauls();
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

			var attr = GetAttribute<ScalarTypeAttribute>(type, a => a.Configuration);
			var ret  = false;

			if (attr != null)
			{
				ret = attr.IsScalar;
			}
			else
			{
				type = type.ToNullableUnderlying();

				if (type.IsEnumEx() || type.IsPrimitiveEx() || (Configuration.IsStructIsScalarType && type.IsValueTypeEx()))
					ret = true;
			}

			SetScalarType(type, ret);

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
		public void AddScalarType(Type type, object defaultValue, DataType dataType = DataType.Undefined)
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
		public void AddScalarType(Type type, object defaultValue, bool canBeNull, DataType dataType = DataType.Undefined)
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
		public SqlDataType GetUnderlyingDataType(Type type, ref bool canBeNull)
		{
			int? length = null;

			var underlyingType = type.ToNullableUnderlying();

			if (underlyingType.IsEnumEx())
			{
				var attrs =
				(
					from f in underlyingType.GetFieldsEx()
					where (f.Attributes & EnumField) == EnumField
					from attr in GetAttributes<MapValueAttribute>(underlyingType, f, a => a.Configuration).Select(attr => attr)
					orderby attr.IsDefault ? 0 : 1
					select attr
				).ToList();

				if (attrs.Count == 0)
				{
					underlyingType = Enum.GetUnderlyingType(underlyingType);
				}
				else
				{
					var  minLen    = 0;
					Type valueType = null;

					foreach (var attr in attrs)
					{
						if (attr.Value == null)
						{
							canBeNull = true;
						}
						else
						{
							if (valueType == null)
								valueType = attr.Value.GetType();

							if (attr.Value is string)
							{
								var len = attr.Value.ToString().Length;

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

					if (dt.DataType == DataType.NVarChar && minLen == length)
						return new SqlDataType(DataType.NChar, valueType, length.Value);

					if (length.HasValue && dt.IsCharDataType)
						return new SqlDataType(dt.DataType, valueType, length.Value);

					return dt;
				}
			}

			if (underlyingType != type)
				return GetDataType(underlyingType);

			return SqlDataType.Undefined;
		}


		#endregion

		#region GetMapValues

		ConcurrentDictionary<Type,MapValue[]> _mapValues;

		/// <summary>
		/// Returns enum type mapping information or <c>null</c> for non-enum types.
		/// </summary>
		/// <param name="type">Mapped type.</param>
		/// <returns>Mapping values for enum type and <c>null</c> for non-enum types.</returns>
		public virtual MapValue[] GetMapValues([JetBrains.Annotations.NotNull] Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			if (_mapValues == null)
				_mapValues = new ConcurrentDictionary<Type,MapValue[]>();

			MapValue[] mapValues;

			if (_mapValues.TryGetValue(type, out mapValues))
				return mapValues;

			var underlyingType = type.ToNullableUnderlying();

			if (underlyingType.IsEnumEx())
			{
				var fields =
				(
					from f in underlyingType.GetFieldsEx()
					where (f.Attributes & EnumField) == EnumField
					let attrs = GetAttributes<MapValueAttribute>(underlyingType, f, a => a.Configuration)
					select new MapValue(Enum.Parse(underlyingType, f.Name, false), attrs)
				).ToArray();

				if (fields.Any(f => f.MapValues.Length > 0))
					mapValues = fields;
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
						.Select        (s => s.ColumnNameComparer)
						.FirstOrDefault(s => s != null)
						??
						StringComparer.Ordinal;
				}

				return Schemas[0].ColumnNameComparer;
			}

			set => Schemas[0].ColumnNameComparer = value;
		}

		#endregion

		#region EntityDescriptor

		/// <summary>
		/// Returns mapped entity descriptor.
		/// </summary>
		/// <param name="type">Mapped type.</param>
		/// <returns>Mapping descriptor.</returns>
		public EntityDescriptor GetEntityDescriptor(Type type)
		{
			return Schemas[0].GetEntityDescriptor(this, type);
		}

		// TODO: V2 - do we need it??
		/// <summary>
		/// Returns types for cached <see cref="EntityDescriptor" />s.
		/// </summary>
		/// <seealso cref="GetEntityDescriptor(Type)" />
		/// <returns>
		/// Mapping types.
		/// </returns>
		public Type[] GetEntites()
		{
			return Schemas[0].GetEntites();
		}

		internal void ResetEntityDescriptor(Type type)
		{
			Schemas[0].ResetEntityDescriptor(type);
		}

		#endregion

		#region Enum

		/// <summary>
		/// Returns type, to which provided enumeration type is mapped or <c>null</c>, if type is not configured.
		/// See <see cref="SetDefaultFromEnumType(Type, Type)"/>.
		/// </summary>
		/// <param name="enumType">Enumeration type.</param>
		/// <returns>Mapped type or <c>null</c>.</returns>
		public Type GetDefaultFromEnumType(Type enumType)
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
	}
}
