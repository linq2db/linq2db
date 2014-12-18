using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

#if !SILVERLIGHT && !NETFX_CORE
using System.Xml;
#endif

namespace LinqToDB.Mapping
{
	using Common;
	using Expressions;
	using Extensions;
	using Metadata;

	public class MappingSchema
	{
		#region Init

		public MappingSchema()
			: this(null, (MappingSchema[])null)
		{
		}

		public MappingSchema(params MappingSchema[] schemas)
			: this(null, schemas)
		{
		}

		public MappingSchema(string configuration/* ??? */)
			: this(configuration, null)
		{
		}

		public MappingSchema(string configuration, params MappingSchema[] schemas)
		{
			MappingSchemaInfo[] ss;

			if (schemas == null)
				ss = Default._schemas;
			else if (schemas.Length == 0)
				ss = Array<MappingSchemaInfo>.Empty;
			else if (schemas.Length == 1)
				ss = schemas[0]._schemas;
			else
				ss = schemas.Where(s => s != null).SelectMany(s => s._schemas).Distinct().ToArray();

			_schemas    = new MappingSchemaInfo[ss.Length + 1];
			_schemas[0] = new MappingSchemaInfo(configuration);

			Array.Copy(ss, 0, _schemas, 1, ss.Length);
		}

		readonly MappingSchemaInfo[] _schemas;

		#endregion

		#region Default Values

		const FieldAttributes EnumField = FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal;

		public object GetDefaultValue(Type type)
		{
			foreach (var info in _schemas)
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

		public void SetDefaultValue(Type type, object value)
		{
			_schemas[0].SetDefaultValue(type, value);
		}

		#endregion

		#region CanBeNull

		public bool GetCanBeNull(Type type)
		{
			foreach (var info in _schemas)
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

		public void SetCanBeNull(Type type, bool value)
		{
			_schemas[0].SetCanBeNull(type, value);
		}

		#endregion

		#region GenericConvertProvider

		public void InitGenericConvertProvider<T>()
		{
			InitGenericConvertProvider(typeof(T));
		}

		public bool InitGenericConvertProvider(params Type[] types)
		{
			return _schemas.Aggregate(false, (cur, info) => cur || info.InitGenericConvertProvider(types, this));
		}

		public void SetGenericConvertProvider(Type type)
		{
			if (!type.IsGenericTypeDefinitionEx())
				throw new LinqToDBException("'{0}' must be a generic type.".Args(type));

			if (!typeof(IGenericInfoProvider).IsSameOrParentOf(type))
				throw new LinqToDBException("'{0}' must inherit from 'IGenericInfoProvider'.".Args(type));

			_schemas[0].SetGenericConvertProvider(type);
		}

		#endregion

		#region Convert

		public T ChangeTypeTo<T>(object value)
		{
			return Converter.ChangeTypeTo<T>(value, this);
		}

		public object ChangeType(object value, Type conversionType)
		{
			return Converter.ChangeType(value, conversionType, this);
		}

		public object EnumToValue(Enum value)
		{
			var toType = ConvertBuilder.GetDefaultMappingFromEnumType(this, value.GetType());
			return Converter.ChangeType(value, toType, this);
		}

		public virtual LambdaExpression TryGetConvertExpression(Type @from, Type to)
		{
			return null;
		}

		internal ConcurrentDictionary<object,Func<object,object>> Converters
		{
			get { return _schemas[0].Converters; }
		}

		public Expression<Func<TFrom,TTo>> GetConvertExpression<TFrom,TTo>()
		{
			var li = GetConverter(typeof(TFrom), typeof(TTo), true);
			return (Expression<Func<TFrom,TTo>>)ReduceDefaultValue(li.CheckNullLambda);
		}

		public LambdaExpression GetConvertExpression(Type from, Type to, bool checkNull = true, bool createDefault = true)
		{
			var li = GetConverter(from, to, createDefault);
			return li == null ? null : (LambdaExpression)ReduceDefaultValue(checkNull ? li.CheckNullLambda : li.Lambda);
		}

		public Func<TFrom,TTo> GetConverter<TFrom,TTo>()
		{
			var li = GetConverter(typeof(TFrom), typeof(TTo), true);

			if (li.Delegate == null)
			{
				var rex = (Expression<Func<TFrom,TTo>>)ReduceDefaultValue(li.CheckNullLambda);
				var l   = rex.Compile();

				_schemas[0].SetConvertInfo(typeof(TFrom), typeof(TTo), new ConvertInfo.LambdaInfo(li.CheckNullLambda, null, l, li.IsSchemaSpecific));

				return l;
			}

			return (Func<TFrom,TTo>)li.Delegate;
		}

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

			_schemas[0].SetConvertInfo(fromType, toType, new ConvertInfo.LambdaInfo(ex, expr, null, false));
		}

		public void SetConvertExpression<TFrom,TTo>(
			[JetBrains.Annotations.NotNull] Expression<Func<TFrom,TTo>> expr,
			bool addNullCheck = true)
		{
			if (expr == null) throw new ArgumentNullException("expr");

			var ex = addNullCheck && expr.Find(Converter.IsDefaultValuePlaceHolder) == null?
				AddNullCheck(expr) :
				expr;

			_schemas[0].SetConvertInfo(typeof(TFrom), typeof(TTo), new ConvertInfo.LambdaInfo(ex, expr, null, false));
		}

		public void SetConvertExpression<TFrom,TTo>(
			[JetBrains.Annotations.NotNull] Expression<Func<TFrom,TTo>> checkNullExpr,
			[JetBrains.Annotations.NotNull] Expression<Func<TFrom,TTo>> expr)
		{
			if (expr == null) throw new ArgumentNullException("expr");

			_schemas[0].SetConvertInfo(typeof(TFrom), typeof(TTo), new ConvertInfo.LambdaInfo(checkNullExpr, expr, null, false));
		}

		public void SetConverter<TFrom,TTo>([JetBrains.Annotations.NotNull] Func<TFrom,TTo> func)
		{
			if (func == null) throw new ArgumentNullException("func");

			var p  = Expression.Parameter(typeof(TFrom), "p");
			var ex = Expression.Lambda<Func<TFrom,TTo>>(Expression.Invoke(Expression.Constant(func), p), p);

			_schemas[0].SetConvertInfo(typeof(TFrom), typeof(TTo), new ConvertInfo.LambdaInfo(ex, null, func, false));
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
			for (var i = 0; i < _schemas.Length; i++)
			{
				var info = _schemas[i];
				var li   = info.GetConvertInfo(@from, to);

				if (li != null && (i == 0 || !li.IsSchemaSpecific))
					return i == 0 ? li : new ConvertInfo.LambdaInfo(li.CheckNullLambda, li.CheckNullLambda, null, false);
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

		public IMetadataReader MetadataReader
		{
			get { return _schemas[0].MetadataReader; }
			set
			{
				_schemas[0].MetadataReader = value;
				_metadataReaders = null;
			}
		}

		public void AddMetadataReader(IMetadataReader reader)
		{
			MetadataReader = MetadataReader == null ? reader : new MetadataReader(reader, MetadataReader);
		}

		IMetadataReader[] _metadataReaders;
		IMetadataReader[]  MetadataReaders
		{
			get
			{
				if (_metadataReaders == null)
				{
					var hash = new HashSet<IMetadataReader>();
					var list = new List<IMetadataReader>();

					foreach (var s in _schemas)
						if (s.MetadataReader != null && hash.Add(s.MetadataReader))
							list.Add(s.MetadataReader);

					_metadataReaders = list.ToArray();
				}

				return _metadataReaders;
			}
		}

		public T[] GetAttributes<T>(Type type, bool inherit = true)
			where T : Attribute
		{
			var q =
				from mr in MetadataReaders
				from a in mr.GetAttributes<T>(type, inherit)
				select a;

			return q.ToArray();
		}

		public T[] GetAttributes<T>(MemberInfo memberInfo, bool inherit = true)
			where T : Attribute
		{
			var q =
				from mr in MetadataReaders
				from a in mr.GetAttributes<T>(memberInfo, inherit)
				select a;

			return q.ToArray();
		}

		public T GetAttribute<T>(Type type, bool inherit = true)
			where T : Attribute
		{
			var attrs = GetAttributes<T>(type, inherit);
			return attrs.Length == 0 ? null : attrs[0];
		}

		public T GetAttribute<T>(MemberInfo memberInfo, bool inherit = true)
			where T : Attribute
		{
			var attrs = GetAttributes<T>(memberInfo, inherit);
			return attrs.Length == 0 ? null : attrs[0];
		}

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

		public T[] GetAttributes<T>(MemberInfo memberInfo, Func<T,string> configGetter, bool inherit = true)
			where T : Attribute
		{
			var list  = new List<T>();
			var attrs = GetAttributes<T>(memberInfo, inherit);

			foreach (var c in ConfigurationList)
				foreach (var a in attrs)
					if (configGetter(a) == c)
						list.Add(a);

			return list.Concat(attrs.Where(a => string.IsNullOrEmpty(configGetter(a)))).ToArray();
		}

		public T GetAttribute<T>(Type type, Func<T,string> configGetter, bool inherit = true)
			where T : Attribute
		{
			var attrs = GetAttributes(type, configGetter, inherit);
			return attrs.Length == 0 ? null : attrs[0];
		}
		
		public T GetAttribute<T>(MemberInfo memberInfo, Func<T,string> configGetter, bool inherit = true)
			where T : Attribute
		{
			var attrs = GetAttributes(memberInfo, configGetter, inherit);
			return attrs.Length == 0 ? null : attrs[0];
		}

		public FluentMappingBuilder GetFluentMappingBuilder()
		{
			return new FluentMappingBuilder(this);
		}

		#endregion

		#region Configuration

		private string _configurationID;
		public  string  ConfigurationID
		{
			get { return _configurationID ?? (_configurationID = string.Join(".", ConfigurationList)); }
		}

		private string[] _configurationList;
		public  string[]  ConfigurationList
		{
			get
			{
				if (_configurationList == null)
				{
					var hash = new HashSet<string>();
					var list = new List<string>();

					foreach (var s in _schemas)
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
			_schemas = new[] { mappingSchemaInfo };
		}

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
#if !SILVERLIGHT && !NETFX_CORE
				AddScalarType(typeof(XmlDocument),     DataType.Xml);
#endif
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
			}
		}

		#endregion

		#region Scalar Types

		public bool IsScalarType(Type type)
		{
			foreach (var info in _schemas)
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

		public void SetScalarType(Type type, bool isScalarType = true)
		{
			_schemas[0].SetScalarType(type, isScalarType);
		}

		public void AddScalarType(Type type, object defaultValue, DataType dataType = DataType.Undefined)
		{
			SetScalarType  (type);
			SetDefaultValue(type, defaultValue);

			if (dataType != DataType.Undefined)
				SetDataType(type, dataType);
		}

		public void AddScalarType(Type type, object defaultValue, bool canBeNull, DataType dataType = DataType.Undefined)
		{
			SetScalarType  (type);
			SetDefaultValue(type, defaultValue);
			SetCanBeNull   (type, canBeNull);

			if (dataType != DataType.Undefined)
				SetDataType(type, dataType);
		}

		public void AddScalarType(Type type, DataType dataType = DataType.Undefined)
		{
			SetScalarType(type);

			if (dataType != DataType.Undefined)
				SetDataType(type, dataType);
		}

		#endregion

		#region DataTypes

		public DataType GetDataType(Type type)
		{
			foreach (var info in _schemas)
			{
				var o = info.GetDataType(type);
				if (o.IsSome)
					return o.Value;
			}

			return DataType.Undefined;
		}

		public void SetDataType(Type type, DataType dataType)
		{
			_schemas[0].SetDataType(type, dataType);
		}

		#endregion

		#region GetMapValues

		ConcurrentDictionary<Type,MapValue[]> _mapValues;

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
					let attrs = GetAttributes<MapValueAttribute>(f, a => a.Configuration)
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

		public StringComparison ColumnComparisonOption
		{
			get
			{
				if (_schemas[0].ColumnComparisonOption == null)
				{
					_schemas[0].ColumnComparisonOption = _schemas
						.Select        (s => s.ColumnComparisonOption)
						.FirstOrDefault(s => s != null)
						??
						StringComparison.Ordinal;
				}

				return _schemas[0].ColumnComparisonOption.Value;
			}

			set { _schemas[0].ColumnComparisonOption = value; }
		}

		#endregion

		#region EntityDescriptor

		ConcurrentDictionary<Type,EntityDescriptor> _entityDescriptors;

		public EntityDescriptor GetEntityDescriptor(Type type)
		{
			if (_entityDescriptors == null)
				_entityDescriptors = new ConcurrentDictionary<Type,EntityDescriptor>();

            return _entityDescriptors.GetOrAdd(type, t =>
            {                
                var descr = new EntityDescriptor(this, t);
                //initialize before returing
                descr.InitInheritanceMapping();
                return descr;
            });
		}

		#endregion
	}
}
