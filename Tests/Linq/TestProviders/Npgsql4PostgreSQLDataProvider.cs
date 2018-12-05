using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;
using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Reflection;

namespace Tests
{
	/*
	 * IN SHORT: this abomination shows power of linq2db
	 * 
	 * MORE DETAILS:
	 * This provider allows us to reference npgsql3 in tests and at the same time use this provider to test npgsql4 too.
	 * How it works:
	 * - npgsql3 used as project reference
	 * - npgsql4 loaded explicitly from redist using LoadFrom method
	 * Because test mappings use npgsql3 types and provider also cache some types from npgsql3
	 * we need:
	 * - override this part of provider to use types from npgsql4 assembly
	 * - configure mappings from npgsql3 to npgsql4 types when they passed to provider from tests
	 * - configure mappings from npgsql4 to npgsql3 types when provider returns them to tests
	 */
	internal class Npgsql4PostgreSQLDataProvider : PostgreSQLDataProvider
	{
		public static void Init()
		{
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
			var path = "npgsql4/net451/npgsql.dll";
#elif NETSTANDARD2_0
			var path = "npgsql4/netstandard2.0/npgsql.dll";
#endif
#if !NETSTANDARD1_6
			if (File.Exists(path))
			{
				DataConnection.AddDataProvider(new Npgsql4PostgreSQLDataProvider(path));
			}
#endif
		}

		private Type _dataReaderType;
		volatile Type _connectionType;

		private readonly Assembly _assembly;

		protected override string ConnectionTypeName { get { return "Npgsql.NpgsqlConnection"; } }
		protected override string DataReaderTypeName { get { return "Npgsql.NpgsqlDataReader"; } }


		public Npgsql4PostgreSQLDataProvider(string path)
		: base(
			TestProvName.PostgreSQLLatest,
			new Npgsql4PostgreSQLMappingSchema(),
			PostgreSQLVersion.v95)
		{
#if !NETSTANDARD1_6
			_assembly = Assembly.LoadFrom(Path.GetFullPath(path));
#endif
		}

		protected override IDbConnection CreateConnectionInternal(string connectionString)
		{
			// fill base mapping schema
			using (PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v95).CreateConnection(connectionString))
			{
			}

			return base.CreateConnectionInternal(connectionString);
		}

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			base.OnConnectionTypeCreated(connectionType);

			var baseProvider = (PostgreSQLDataProvider)PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v95);

			// configure mapping from npgsql3 type to npgsql4 type though DataParameter.Value
			SetConverterToParameter(baseProvider.NpgsqlDateTimeType      , NpgsqlDateTimeType);
			SetConverterToParameter(baseProvider.NpgsqlDateType          , NpgsqlDateType);
			SetConverterToParameter(baseProvider.NpgsqlIntervalType      , NpgsqlIntervalType);
			SetConverterToParameter(baseProvider.NpgsqlPointType         , NpgsqlPointType);
			SetConverterToParameter(baseProvider.NpgsqlLSegType          , NpgsqlLSegType);
			SetConverterToParameter(baseProvider.NpgsqlBoxType           , NpgsqlBoxType);
			SetConverterToParameter(baseProvider.NpgsqlPathType          , NpgsqlPathType);
			SetConverterToParameter(baseProvider.NpgsqlPolygonType       , NpgsqlPolygonType);
			SetConverterToParameter(baseProvider.NpgsqlCircleType        , NpgsqlCircleType);
			SetConverterToParameter(baseProvider.NpgsqlLineType          , NpgsqlLineType);
			SetConverterToParameterNpgsqlInet(baseProvider.NpgsqlInetType, NpgsqlInetType);

			// configure mapping from npgsql4 type to npgsql3 type
			SetConverterToV3(baseProvider.NpgsqlDateTimeType      , NpgsqlDateTimeType);
			SetConverterToV3(baseProvider.NpgsqlDateType          , NpgsqlDateType);
			SetConverterToV3(baseProvider.NpgsqlIntervalType      , NpgsqlIntervalType);
			SetConverterToV3(baseProvider.NpgsqlPointType         , NpgsqlPointType);
			SetConverterToV3(baseProvider.NpgsqlLSegType          , NpgsqlLSegType);
			SetConverterToV3(baseProvider.NpgsqlBoxType           , NpgsqlBoxType);
			SetConverterToV3(baseProvider.NpgsqlPathType          , NpgsqlPathType);
			SetConverterToV3(baseProvider.NpgsqlPolygonType       , NpgsqlPolygonType);
			SetConverterToV3(baseProvider.NpgsqlCircleType        , NpgsqlCircleType);
			SetConverterToV3(baseProvider.NpgsqlLineType          , NpgsqlLineType);
			SetConverterToV3NpgsqlInet(baseProvider.NpgsqlInetType, NpgsqlInetType);

			_setMoney = GetSetParameter(connectionType    , "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Money");
			_setVarBinary = GetSetParameter(connectionType, "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Bytea");
			_setBoolean = GetSetParameter(connectionType  , "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Boolean");
			_setXml = GetSetParameter(connectionType      , "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Xml");
			_setText = GetSetParameter(connectionType     , "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Text");
			_setBit = GetSetParameter(connectionType      , "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Bit");
			_setHstore = GetSetParameter(connectionType   , "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Hstore");
			_setJson = GetSetParameter(connectionType     , "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Json");
			_setJsonb = GetSetParameter(connectionType    , "NpgsqlParameter", "NpgsqlDbType", NpgsqlDbType, "Jsonb");
		}

		static Action<IDbDataParameter> _setMoney;
		static Action<IDbDataParameter> _setVarBinary;
		static Action<IDbDataParameter> _setBoolean;
		static Action<IDbDataParameter> _setXml;
		static Action<IDbDataParameter> _setText;
		static Action<IDbDataParameter> _setBit;
		static Action<IDbDataParameter> _setHstore;
		static Action<IDbDataParameter> _setJsonb;
		static Action<IDbDataParameter> _setJson;

		private void SetConverterToV3NpgsqlInet(Type from, Type to)
		{
			var p = Expression.Parameter(from, "p");

			MappingSchema.SetConvertExpression(from, to,
				Expression.Lambda(
					Expression.New(
						to.GetConstructor(new[] { typeof(string) }),
						Expression.Call(p, from.GetMethod("ToString"))),
					p
				));

			var nullableTo = to.AsNullable();
			if (nullableTo != to)
			{
				MappingSchema.SetConvertExpression(from, nullableTo,
					Expression.Lambda(
						Expression.Convert(
							Expression.New(
								to.GetConstructor(new[] { typeof(string) }),
								Expression.Call(p, from.GetMethod("ToString"))),
							nullableTo),
						p
					));
			}
		}

		private void SetConverterToV3(Type to, Type from)
		{
			var p = Expression.Parameter(from, "p");

			MappingSchema.SetConvertExpression(from, to,
				Expression.Lambda(
					Expression.Call(
						to.GetMethod("Parse"),
						Expression.Call(p, from.GetMethod("ToString"))),
					p
				));

			var nullableTo = to.AsNullable();
			if (nullableTo != to)
			{
				MappingSchema.SetConvertExpression(from, nullableTo,
					Expression.Lambda(
						Expression.Convert(
						Expression.Call(
							to.GetMethod("Parse"),
							Expression.Call(p, from.GetMethod("ToString"))),
						nullableTo),
						p
					));
			}
		}

		private void SetConverterToParameterNpgsqlInet(Type from, Type to)
		{
			var p = Expression.Parameter(from, "p");

			MappingSchema.SetConvertExpression(from, typeof(DataParameter),
				Expression.Lambda(
					Expression.MemberInit(
						Expression.New(typeof(DataParameter)),
						Expression.Bind(
							typeof(DataParameter).GetProperty("Value"),
							Expression.Convert(Expression.New(
								to.GetConstructor(new[] { typeof(string) }),
								Expression.Call(p, from.GetMethod("ToString"))), typeof(object)))),
					p
				));

			var nullableFrom = from.AsNullable();
			if (nullableFrom != from)
			{
				var pn = Expression.Parameter(nullableFrom, "p");
				MappingSchema.SetConvertExpression(nullableFrom, typeof(DataParameter),
					Expression.Lambda(
						Expression.MemberInit(
							Expression.New(typeof(DataParameter)),
							Expression.Bind(
								typeof(DataParameter).GetProperty("Value"),
								Expression.Convert(Expression.New(
									to.GetConstructor(new[] { typeof(string) }),
									Expression.Call(Expression.Property(pn, "Value"), from.GetMethod("ToString"))), typeof(object)))),
						pn
					));
			}
		}

		private void SetConverterToParameter(Type from, Type to)
		{
			var p = Expression.Parameter(from, "p");

			MappingSchema.SetConvertExpression(from, typeof(DataParameter),
				Expression.Lambda(
					Expression.MemberInit(
						Expression.New(typeof(DataParameter)),
						Expression.Bind(
							typeof(DataParameter).GetProperty("Value"),
							Expression.Convert(Expression.Call(
								to.GetMethod("Parse"),
								Expression.Call(p, from.GetMethod("ToString"))), typeof(object)))),
					p
				));

			var nullableFrom = from.AsNullable();
			if (nullableFrom != from)
			{
				var pn = Expression.Parameter(nullableFrom, "p");
				MappingSchema.SetConvertExpression(nullableFrom, typeof(DataParameter),
					Expression.Lambda(
						Expression.MemberInit(
							Expression.New(typeof(DataParameter)),
							Expression.Bind(
								typeof(DataParameter).GetProperty("Value"),
								Expression.Convert(Expression.Call(
									to.GetMethod("Parse"),
									Expression.Call(Expression.Property(pn, "Value"), from.GetMethod("ToString"))), typeof(object)))),
						pn
					));
			}
		}

#if NETSTANDARD1_6 || NETSTANDARD2_0
		public override Type DataReaderType => _dataReaderType ?? (_dataReaderType = _assembly.GetType(DataReaderTypeName, true));

		protected override Type GetConnectionType()
		{
			if (_connectionType == null)
				lock (SyncRoot)
					if (_connectionType == null)
					{
						_connectionType = _assembly.GetType(ConnectionTypeName, true);

						OnConnectionTypeCreated(_connectionType);
					}

			return _connectionType;
		}
#else
		public override Type DataReaderType
		{
			get
			{
				if (_dataReaderType != null)
					return _dataReaderType;

				if (DbFactoryProviderName == null)
					return _dataReaderType = _assembly.GetType(DataReaderTypeName, true);

				_dataReaderType = _assembly.GetType(DataReaderTypeName, false);

				if (_dataReaderType == null)
				{
					var assembly = DbProviderFactories.GetFactory(DbFactoryProviderName).GetType().Assembly;

					var idx = 0;
					var dataReaderTypeName = (idx = DataReaderTypeName.IndexOf(',')) != -1 ? DataReaderTypeName.Substring(0, idx) : DataReaderTypeName;
					_dataReaderType = assembly.GetType(dataReaderTypeName, true);
				}

				return _dataReaderType;
			}
		}

		protected override Type GetConnectionType()
		{
			if (_connectionType == null)
				lock (SyncRoot)
					if (_connectionType == null)
					{
						if (DbFactoryProviderName == null)
							_connectionType = _assembly.GetType(ConnectionTypeName, true);
						else
						{
							_connectionType = _assembly.GetType(ConnectionTypeName, false);

							if (_connectionType == null)
								using (var db = DbProviderFactories.GetFactory(DbFactoryProviderName).CreateConnection())
									_connectionType = db.GetType();
						}

						OnConnectionTypeCreated(_connectionType);
					}

			return _connectionType;
		}
#endif

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new Npgsql4PostgreSQLSqlBuilder(this, GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.Money     : _setMoney(parameter)    ; return;
				case DataType.Image     :
				case DataType.Binary    :
				case DataType.VarBinary : _setVarBinary(parameter); return;
				case DataType.Boolean   : _setBoolean(parameter)  ; return;
				case DataType.Xml       : _setXml(parameter)      ; return;
				case DataType.Text      :
				case DataType.NText     : _setText(parameter)     ; return;
				case DataType.BitArray  : _setBit(parameter)      ; return;
				case DataType.Dictionary: _setHstore(parameter)   ; return;
				case DataType.Json      : _setJson(parameter)     ; return;
				case DataType.BinaryJson: _setJsonb(parameter)    ; return;
			}

			base.SetParameterType(parameter, dataType);
		}

#if !NETSTANDARD1_6
		public override ISchemaProvider GetSchemaProvider()
		{
			return new Npgsql4PostgreSQLSchemaProvider(this);
		}
#endif
	}

#if !NETSTANDARD1_6
	internal class Npgsql4PostgreSQLSchemaProvider : PostgreSQLSchemaProvider
	{
		private readonly Assembly _assembly;

		public Npgsql4PostgreSQLSchemaProvider(Npgsql4PostgreSQLDataProvider provider)
			: base(provider)
		{
			// return npgsql3 types when npgsql4 type requested
			_assembly = Assembly.Load("npgsql");
		}

		protected override Type GetSystemType(string dataType, string columnType, DataTypeInfo dataTypeInfo, long? length, int? precision, int? scale)
		{
			if (dataTypeInfo != null && dataTypeInfo.DataType != null)
			{
				var idx = 0;
				var typeName = (idx = dataTypeInfo.DataType.IndexOf(',')) != -1 ? dataTypeInfo.DataType.Substring(0, idx) : dataTypeInfo.DataType;
				var systemType = _assembly.GetType(typeName, false);

				if (systemType != null)
				{
					if (length == 1 && !GenerateChar1AsString && systemType == typeof(string))
						systemType = typeof(char);

					return systemType;
				}
			}

			return base.GetSystemType(dataType, columnType, dataTypeInfo, length, precision, scale);
		}
	}
#endif

	internal class Npgsql4PostgreSQLMappingSchema : MappingSchema
	{
		public Npgsql4PostgreSQLMappingSchema() : base(PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v95).MappingSchema)
		{
		}
	}

	internal class Npgsql4PostgreSQLSqlBuilder : PostgreSQLSqlBuilder
	{
		private readonly Npgsql4PostgreSQLDataProvider _provider;
		private readonly PostgreSQLDataProvider _baseProvider = (PostgreSQLDataProvider)PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v95);

		public Npgsql4PostgreSQLSqlBuilder(Npgsql4PostgreSQLDataProvider provider, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(provider, sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
			_provider = provider;
		}

		// used by linq service
		public Npgsql4PostgreSQLSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new Npgsql4PostgreSQLSqlBuilder(_provider, SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override void BuildDataType(SqlDataType type, bool createDbType)
		{
			if (type.Type != null)
			{
				switch (type.DataType)
				{
					case DataType.Udt:
						var udtType = type.Type.ToNullableUnderlying();

						if (     udtType == _baseProvider.NpgsqlPointType)    StringBuilder.Append("point");
						else if (udtType == _baseProvider.NpgsqlLineType)     StringBuilder.Append("line");
						else if (udtType == _baseProvider.NpgsqlBoxType)      StringBuilder.Append("box");
						else if (udtType == _baseProvider.NpgsqlLSegType)     StringBuilder.Append("lseg");
						else if (udtType == _baseProvider.NpgsqlCircleType)   StringBuilder.Append("circle");
						else if (udtType == _baseProvider.NpgsqlPolygonType)  StringBuilder.Append("polygon");
						else if (udtType == _baseProvider.NpgsqlPathType)     StringBuilder.Append("path");
						else if (udtType == _baseProvider.NpgsqlIntervalType) StringBuilder.Append("interval");
						else if (udtType == _baseProvider.NpgsqlDateType)     StringBuilder.Append("date");
						else if (udtType == _baseProvider.NpgsqlDateTimeType) StringBuilder.Append("timestamp");
						else if (udtType == typeof(PhysicalAddress)
							&& !_baseProvider.HasMacAddr8)                    StringBuilder.Append("macaddr");
						else break;
						return;
				}
			}

			base.BuildDataType(type, createDbType);
		}
	}
}

