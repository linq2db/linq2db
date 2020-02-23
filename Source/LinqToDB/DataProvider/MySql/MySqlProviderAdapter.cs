using System;
using System.Data;

namespace LinqToDB.DataProvider.MySql
{
	using LinqToDB.Expressions;
	using LinqToDB.Mapping;

	public class MySqlProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _mysqlDataSyncRoot      = new object();
		private static readonly object _mysqlConnectorSyncRoot = new object();

		private static MySqlProviderAdapter? _mysqlDataInstance;
		private static MySqlProviderAdapter? _mysqlConnectorInstance;

		public const string MySqlConnectorAssemblyName    = "MySqlConnector";
		public const string MySqlDataAssemblyName         = "MySql.Data";

		// shared by both providers
		public const string ClientNamespace = "MySql.Data.MySqlClient";
		public const string TypesNamespace  = "MySql.Data.Types";


		private MySqlProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,

			Type? mySqlDecimalType,
			Type  mySqlDateTimeType,
			Type  mySqlGeometryType,

			Func<object, decimal>? mySqlDecimalGetter,

			Func<IDbDataParameter, object> dbTypeGetter,

			string? getMySqlDecimalMethodName,
			string? getDateTimeOffsetMethodName,
			string  getMySqlDateTimeMethodName,
			MappingSchema mappingSchema)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

			MySqlDecimalType  = mySqlDecimalType;
			MySqlDateTimeType = mySqlDateTimeType;
			MySqlGeometryType = mySqlGeometryType;

			MySqlDecimalGetter = mySqlDecimalGetter;

			GetDbType = dbTypeGetter;

			GetMySqlDecimalMethodName   = getMySqlDecimalMethodName;
			GetDateTimeOffsetMethodName = getDateTimeOffsetMethodName;
			GetMySqlDateTimeMethodName  = getMySqlDateTimeMethodName;

			MappingSchema = mappingSchema;
		}

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		public MappingSchema MappingSchema { get; }

		/// <summary>
		/// Not supported by MySqlConnector.
		/// </summary>
		public Type? MySqlDecimalType { get; }
		public Type  MySqlDateTimeType { get; }
		public Type  MySqlGeometryType { get; }

		/// <summary>
		/// Not supported by MySqlConnector.
		/// </summary>
		public Func<object, decimal>? MySqlDecimalGetter { get; }

		/// <summary>
		/// Not supported by MySqlConnector.
		/// </summary>
		public string? GetMySqlDecimalMethodName { get; }

		/// <summary>
		/// MySqlConnector-only.
		/// </summary>
		public string? GetDateTimeOffsetMethodName { get; }

		public string GetMySqlDateTimeMethodName   { get; }

		public string ProviderTypesNamespace => TypesNamespace;

		/// <summary>
		/// Returns object, because both providers use different enums and we anyway don't need typed value.
		/// </summary>
		public Func<IDbDataParameter, object> GetDbType { get; }

		public static MySqlProviderAdapter GetInstance(string name)
		{
			if (name == ProviderName.MySqlConnector)
			{
				if (_mysqlConnectorInstance == null)
					lock (_mysqlConnectorSyncRoot)
						if (_mysqlConnectorInstance == null)
							_mysqlConnectorInstance = MySqlConnector.CreateAdapter();

				return _mysqlConnectorInstance;
			}
			else
			{
				if (_mysqlDataInstance == null)
					lock (_mysqlDataSyncRoot)
						if (_mysqlDataInstance == null)
							_mysqlDataInstance = MySqlData.CreateAdapter();

				return _mysqlDataInstance;
			}
		}

		private class MySqlData
		{
			internal static MySqlProviderAdapter CreateAdapter()
			{
				var assembly = Common.Tools.TryLoadAssembly(MySqlDataAssemblyName, null);
				if (assembly == null)
					throw new InvalidOperationException($"Cannot load assembly {MySqlDataAssemblyName}");

				var connectionType    = assembly.GetType($"{ClientNamespace}.MySqlConnection" , true);
				var dataReaderType    = assembly.GetType($"{ClientNamespace}.MySqlDataReader" , true);
				var parameterType     = assembly.GetType($"{ClientNamespace}.MySqlParameter"  , true);
				var commandType       = assembly.GetType($"{ClientNamespace}.MySqlCommand"    , true);
				var transactionType   = assembly.GetType($"{ClientNamespace}.MySqlTransaction", true);
				var dbType            = assembly.GetType($"{ClientNamespace}.MySqlDbType"     , true);
				var mySqlDecimalType  = assembly.GetType($"{TypesNamespace}.MySqlDecimal"     , true);
				var mySqlDateTimeType = assembly.GetType($"{TypesNamespace}.MySqlDateTime"    , true);
				var mySqlGeometryType = assembly.GetType($"{TypesNamespace}.MySqlGeometry"    , true);

				var typeMapper = new TypeMapper();
				typeMapper.RegisterTypeWrapper<MySqlParameter>(parameterType);
				typeMapper.RegisterTypeWrapper<MySqlDbType>(dbType);
				typeMapper.RegisterTypeWrapper<MySqlDateTime>(mySqlDateTimeType);
				typeMapper.RegisterTypeWrapper<MySqlDecimal>(mySqlDecimalType);
				typeMapper.FinalizeMappings();

				var dbTypeGetter      = typeMapper.Type<MySqlParameter>().Member(p => p.MySqlDbType).BuildGetter<IDbDataParameter>();
				var decimalGetter     = typeMapper.Type<MySqlDecimal>().Member(p => p.Value).BuildGetter<object>();
				var dateTimeConverter = typeMapper.MapLambda((MySqlDateTime dt) => dt.GetDateTime());

				var mappingSchema = new MappingSchema();
				mappingSchema.SetDataType(mySqlDecimalType, DataType.Decimal);
				mappingSchema.SetDataType(mySqlDateTimeType, DataType.DateTime2);
				mappingSchema.SetConvertExpression(mySqlDateTimeType, typeof(DateTime), dateTimeConverter);

				return new MySqlProviderAdapter(
					connectionType,
					dataReaderType,
					parameterType,
					commandType,
					transactionType,
					mySqlDecimalType,
					mySqlDateTimeType,
					mySqlGeometryType,
					decimalGetter,
					p => dbTypeGetter(p),
					"GetMySqlDecimal",
					null,
					"GetMySqlDateTime",
					mappingSchema);
			}

			[Wrapper]
			private class MySqlDateTime
			{
				public DateTime GetDateTime() => throw new NotImplementedException();
			}

			[Wrapper]
			private class MySqlDecimal
			{
				public decimal Value { get; }
			}

			[Wrapper]
			private class MySqlParameter
			{
				public MySqlDbType MySqlDbType { get; set; }
			}

			[Wrapper]
			internal enum MySqlDbType
			{
				Binary     = 754,
				Bit        = 16,
				Blob       = 252,
				Byte       = 1,
				Date       = 10,
				Datetime   = 12,
				DateTime   = 12,
				Decimal    = 0,
				Double     = 5,
				Enum       = 247,
				Float      = 4,
				Geometry   = 255,
				Guid       = 854,
				Int16      = 2,
				Int24      = 9,
				Int32      = 3,
				Int64      = 8,
				JSON       = 245,
				LongBlob   = 251,
				LongText   = 751,
				MediumBlob = 250,
				MediumText = 750,
				Newdate    = 14,
				NewDecimal = 246,
				Set        = 248,
				String     = 254,
				Text       = 752,
				Time       = 11,
				Timestamp  = 7,
				TinyBlob   = 249,
				TinyText   = 749,
				UByte      = 501,
				UInt16     = 502,
				UInt24     = 509,
				UInt32     = 503,
				UInt64     = 508,
				VarBinary  = 753,
				VarChar    = 253,
				VarString  = 15,
				Year       = 13
			}
		}

		private class MySqlConnector
		{
			internal static MySqlProviderAdapter CreateAdapter()
			{
				var assembly = Common.Tools.TryLoadAssembly(MySqlConnectorAssemblyName, null);
				if (assembly == null)
					throw new InvalidOperationException($"Cannot load assembly {MySqlConnectorAssemblyName}");

				var connectionType    = assembly.GetType($"{ClientNamespace}.MySqlConnection" , true);
				var dataReaderType    = assembly.GetType($"{ClientNamespace}.MySqlDataReader" , true);
				var parameterType     = assembly.GetType($"{ClientNamespace}.MySqlParameter"  , true);
				var commandType       = assembly.GetType($"{ClientNamespace}.MySqlCommand"    , true);
				var transactionType   = assembly.GetType($"{ClientNamespace}.MySqlTransaction", true);
				var dbType            = assembly.GetType($"{ClientNamespace}.MySqlDbType"     , true);
				var mySqlDateTimeType = assembly.GetType($"{TypesNamespace}.MySqlDateTime"    , true);
				var mySqlGeometryType = assembly.GetType($"{TypesNamespace}.MySqlGeometry"    , true);

				var typeMapper = new TypeMapper();
				typeMapper.RegisterTypeWrapper<MySqlParameter>(parameterType);
				typeMapper.RegisterTypeWrapper<MySqlDbType>(dbType);
				typeMapper.RegisterTypeWrapper<MySqlDateTime>(mySqlDateTimeType);
				typeMapper.FinalizeMappings();

				var typeGetter        = typeMapper.Type<MySqlParameter>().Member(p => p.MySqlDbType).BuildGetter<IDbDataParameter>();
				var dateTimeConverter = typeMapper.MapLambda((MySqlDateTime dt) => dt.GetDateTime());

				var mappingSchema = new MappingSchema();
				mappingSchema.SetDataType(mySqlDateTimeType, DataType.DateTime2);
				mappingSchema.SetConvertExpression(mySqlDateTimeType, typeof(DateTime), dateTimeConverter);

				return new MySqlProviderAdapter(
					connectionType,
					dataReaderType,
					parameterType,
					commandType,
					transactionType,
					null,
					mySqlDateTimeType,
					mySqlGeometryType,
					null,
					p => typeGetter(p),
					null,
					"GetDateTimeOffset",
					"GetMySqlDateTime",
					mappingSchema);
			}

			[Wrapper]
			private class MySqlDateTime
			{
				public DateTime GetDateTime() => throw new NotImplementedException();
			}

			[Wrapper]
			private class MySqlParameter
			{
				public MySqlDbType MySqlDbType { get; set; }
			}

			[Wrapper]
			internal enum MySqlDbType
			{
				Binary     = 600,
				Bit        = 16,
				Blob       = 252,
				Bool       = -1,
				Byte       = 1,
				Date       = 10,
				Datetime   = 12,
				DateTime   = 12,
				Decimal    = 0,
				Double     = 5,
				Enum       = 247,
				Float      = 4,
				Geometry   = 255,
				Guid       = 800,
				Int16      = 2,
				Int24      = 9,
				Int32      = 3,
				Int64      = 8,
				JSON       = 245,
				LongBlob   = 251,
				LongText   = 751,
				MediumBlob = 250,
				MediumText = 750,
				Newdate    = 14,
				NewDecimal = 246,
				Null       = 6,
				Set        = 248,
				String     = 254,
				Text       = 752,
				Time       = 11,
				Timestamp  = 7,
				TinyBlob   = 249,
				TinyText   = 749,
				UByte      = 501,
				UInt16     = 502,
				UInt24     = 509,
				UInt32     = 503,
				UInt64     = 508,
				VarBinary  = 601,
				VarChar    = 253,
				VarString  = 15,
				Year       = 13
			}
		}
	}
}
