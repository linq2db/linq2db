using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
				ss = schemas.SelectMany(s => s._schemas).Distinct().ToArray();

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

		public void SetDefaultValue<T>(T value)
		{
			_schemas[0].SetDefaultValue(typeof(T), value);
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
			return (Expression<Func<TFrom,TTo>>)ReduceDefaultValue(li.Lambda);
		}

		public LambdaExpression GetConvertExpression(Type from, Type to)
		{
			var li = GetConverter(from, to, true);
			return (LambdaExpression)ReduceDefaultValue(li.Lambda);
		}

		public Func<TFrom,TTo> GetConverter<TFrom,TTo>()
		{
			var li = GetConverter(typeof(TFrom), typeof(TTo), true);

			if (li.Delegate == null)
			{
				var rex = (Expression<Func<TFrom,TTo>>)ReduceDefaultValue(li.Lambda);
				var l   = rex.Compile();

				_schemas[0].SetConvertInfo(typeof(TFrom), typeof(TTo), new ConvertInfo.LambdaInfo(li.Lambda, l, li.IsSchemaSpecific));

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

			_schemas[0].SetConvertInfo(typeof(TFrom), typeof(TTo), new ConvertInfo.LambdaInfo(ex, null, false));
		}

		public void SetConverter<TFrom,TTo>([JetBrains.Annotations.NotNull] Func<TFrom,TTo> func)
		{
			if (func == null) throw new ArgumentNullException("func");

			var p  = Expression.Parameter(typeof(TFrom), "p");
			var ex = Expression.Lambda<Func<TFrom,TTo>>(Expression.Invoke(Expression.Constant(func), p), p);

			_schemas[0].SetConvertInfo(typeof(TFrom), typeof(TTo), new ConvertInfo.LambdaInfo(ex, func, false));
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
					return i == 0 ? li : new ConvertInfo.LambdaInfo(li.Lambda, null, false);
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
						var b  = li.Lambda.Body;
						var ps = li.Lambda.Parameters;

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
							var b  = li.Lambda.Body;
							var ps = li.Lambda.Parameters;

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
						var b  = li.Lambda.Body;
						var ps = li.Lambda.Parameters;

						ss = li.IsSchemaSpecific;
						ex = Expression.Lambda(Expression.Convert(b, to), ps);
					}
					else
						ex = null;
				}
				else
					ex = null;

				if (ex != null)
					return new ConvertInfo.LambdaInfo(AddNullCheck(ex), null, ss);

				var d = ConvertInfo.Default.Get(from, to);

				if (d == null || d.IsSchemaSpecific)
					d = ConvertInfo.Default.Create(this, from, to);

				return new ConvertInfo.LambdaInfo(d.Lambda, null, d.IsSchemaSpecific);
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

		string[] _configurationList;
		string[]  ConfigurationList
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
			}
		}

		#endregion
	}
}
