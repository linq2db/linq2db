using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Linq;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

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

		public object GetDefaultValue(Type type)
		{
			foreach (var info in _schemas)
			{
				var o = info.GetDefaultValue(type);
				if (o.IsSome)
					return o.Value;
			}

			return DefaultValue.GetValue(type);
		}

		public void SetDefaultValue(Type type, object value)
		{
			_schemas[0].SetDefaultValue(type, value);
		}

		#endregion

		#region Convert

		internal ConcurrentDictionary<object,Func<object,object>> Converters
		{
			get { return _schemas[0].Converters; }
		}

		public Expression<Func<TFrom,TTo>> GetConvertExpression<TFrom,TTo>()
		{
			var li = GetConverter(typeof(TFrom), typeof(TTo), true);
			return (Expression<Func<TFrom,TTo>>)ReduceDefaultValue(li.CheckNullLambda);
		}

		public LambdaExpression GetConvertExpression(Type from, Type to, bool checkNull = true)
		{
			var li = GetConverter(from, to, true);
			return (LambdaExpression)ReduceDefaultValue(checkNull ? li.CheckNullLambda : li.Lambda);
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

		static LambdaExpression AddNullCheck(LambdaExpression expr)
		{
			var p = expr.Parameters[0];

			if (p.Type.IsNullable())
				return Expression.Lambda(
					Expression.Condition(
						Expression.PropertyOrField(p, "HasValue"),
						expr.Body,
						new DefaultValueExpression(expr.Body.Type)),
					expr.Parameters);

			if (p.Type.IsClass)
				return Expression.Lambda(
					Expression.Condition(
						Expression.NotEqual(p, Expression.Constant(null, p.Type)),
						expr.Body,
						new DefaultValueExpression(expr.Body.Type)),
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
			get { return _schemas[0].MetadataReader;  }
			set { _schemas[0].MetadataReader = value; }
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

		public T[] GetAttributes<T>(Type type)
			where T : Attribute
		{
			var q =
				from mr in MetadataReaders
				from a in mr.GetAttributes<T>(type)
				select a;

			return q.ToArray();
		}

		public T[] GetAttributes<T>(MemberInfo memberInfo)
			where T : Attribute
		{
			var q =
				from mr in MetadataReaders
				from a in mr.GetAttributes<T>(memberInfo)
				select a;

			return q.ToArray();
		}

		public T GetAttribute<T>(Type type)
			where T : Attribute
		{
			var attrs = GetAttributes<T>(type);
			return attrs.Length == 0 ? null : attrs[0];
		}

		public T GetAttribute<T>(MemberInfo memberInfo)
			where T : Attribute
		{
			var attrs = GetAttributes<T>(memberInfo);
			return attrs.Length == 0 ? null : attrs[0];
		}

		public T[] GetAttributes<T>(Type type, Func<T,string> configGetter)
			where T : Attribute
		{
			var list  = new List<T>();
			var attrs = GetAttributes<T>(type);

			foreach (var c in ConfigurationList)
				foreach (var a in attrs)
					if (configGetter(a) == c)
						list.Add(a);

			return list.Concat(attrs.Where(a => string.IsNullOrEmpty(configGetter(a)))).ToArray();
		}

		public T[] GetAttributes<T>(MemberInfo memberInfo, Func<T,string> configGetter)
			where T : Attribute
		{
			var list  = new List<T>();
			var attrs = GetAttributes<T>(memberInfo);

			foreach (var c in ConfigurationList)
				foreach (var a in attrs)
					if (configGetter(a) == c)
						list.Add(a);

			return list.Concat(attrs.Where(a => string.IsNullOrEmpty(configGetter(a)))).ToArray();
		}

		public T GetAttribute<T>(Type type, Func<T,string> configGetter)
			where T : Attribute
		{
			var attrs = GetAttributes(type, configGetter);
			return attrs.Length == 0 ? null : attrs[0];
		}
		
		public T GetAttribute<T>(MemberInfo memberInfo, Func<T,string> configGetter)
			where T : Attribute
		{
			var attrs = GetAttributes(memberInfo, configGetter);
			return attrs.Length == 0 ? null : attrs[0];
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

		MappingSchema(MappingSchemaInfo mappingSchemaInfo)
		{
			_schemas = new[] { mappingSchemaInfo };
		}

		public static MappingSchema Default = new DefaultMappingSchema();

		class DefaultMappingSchema : MappingSchema
		{
			public DefaultMappingSchema()
				: base(new MappingSchemaInfo("") { MetadataReader = Metadata.MetadataReader.Default })
			{
				AddScalarType(typeof(char),           DataType.NChar);
				AddScalarType(typeof(string),         DataType.NVarChar);
				AddScalarType(typeof(decimal),        DataType.Decimal);
				AddScalarType(typeof(DateTime),       DataType.DateTime2);
				AddScalarType(typeof(DateTimeOffset), DataType.DateTimeOffset);
				AddScalarType(typeof(TimeSpan),       DataType.Time);
				AddScalarType(typeof(byte[]),         DataType.VarBinary);
				AddScalarType(typeof(Binary),         DataType.VarBinary);
				AddScalarType(typeof(Guid),           DataType.Guid);
				AddScalarType(typeof(object),         DataType.Variant);
				AddScalarType(typeof(XmlDocument),    DataType.Xml);
				AddScalarType(typeof(XDocument),      DataType.Xml);
				AddScalarType(typeof(bool),           DataType.Boolean);
				AddScalarType(typeof(sbyte),          DataType.SByte);
				AddScalarType(typeof(short),          DataType.Int16);
				AddScalarType(typeof(int),            DataType.Int32);
				AddScalarType(typeof(long),           DataType.Int64);
				AddScalarType(typeof(byte),           DataType.Byte);
				AddScalarType(typeof(ushort),         DataType.UInt16);
				AddScalarType(typeof(uint),           DataType.UInt32);
				AddScalarType(typeof(ulong),          DataType.UInt64);
				AddScalarType(typeof(float),          DataType.Single);
				AddScalarType(typeof(double),         DataType.Double);
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

				if (type.IsEnum || type.IsPrimitive || (Configuration.IsStructIsScalarType && type.IsValueType))
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

		public void AddScalarType(Type type, DataType dataType = DataType.Undefined)
		{
			SetScalarType  (type);

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
	}
}
