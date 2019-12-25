using System;
using System.Data;

namespace LinqToDB.DataProvider.MySql
{
	using LinqToDB.Expressions;
	using LinqToDB.Mapping;

	internal static class MySqlWrappers
	{
		public static string MySqlConnectorAssemblyName = "MySqlConnector";
		public static string MySqlDataAssemblyName      = "MySql.Data";

		internal interface IMySqlWrapper
		{
			Type ParameterType { get; }

			Type DataReaderType { get; }

			/// <summary>
			/// Returns object, because both providers use different enums and we anyway don't need typed value.
			/// </summary>
			object GetParameterType(IDbDataParameter parameter);

			/// <summary>
			/// Not supported by MySqlConnector.
			/// </summary>
			Type? MySqlDecimalType { get; }

			Type MySqlDateTimeType { get; }


			/// <summary>
			/// Not supported by MySqlConnector.
			/// </summary>
			Func<object, decimal>? MySqlDecimalGetter { get; }

			/// <summary>
			/// Not supported by MySqlConnector.
			/// </summary>
			string? GetMySqlDecimalMethodName { get; }

			string GetMySqlDateTimeMethodName { get; }
		}

		class MySqlData : IMySqlWrapper
		{
			private static readonly object _syncRoot = new object();
			private static IMySqlWrapper? _wrapper;

			private readonly Type _dataReaderType;
			private readonly Type _parameterType;
			private readonly Type _mySqlDecimalType;
			private readonly Type _mySqlDateTimeType;
			private readonly Func<IDbDataParameter, MySqlDbType> _typeGetter;
			private readonly Func<object, decimal>  _decimalGetter;

			MySqlData(
				Type dataReaderType,
				Type parameterType,
				Type mySqlDecimalType,
				Type mySqlDateTimeType,
				Func<IDbDataParameter, MySqlDbType> typeGetter,
				Func<object, decimal>               decimalGetter)
			{
				_parameterType     = parameterType;
				_dataReaderType    = dataReaderType;
				_mySqlDecimalType  = mySqlDecimalType;
				_mySqlDateTimeType = mySqlDateTimeType;

				_typeGetter     = typeGetter;
				_decimalGetter  = decimalGetter;
			}

			Type IMySqlWrapper.DataReaderType    => _dataReaderType;

			Type IMySqlWrapper.ParameterType     => _parameterType;
			Type IMySqlWrapper.MySqlDecimalType  => _mySqlDecimalType;
			Type IMySqlWrapper.MySqlDateTimeType => _mySqlDateTimeType;

			Func<object, decimal>  IMySqlWrapper.MySqlDecimalGetter  => _decimalGetter;

			string IMySqlWrapper.GetMySqlDecimalMethodName  => "GetMySqlDecimal";
			string IMySqlWrapper.GetMySqlDateTimeMethodName => "GetMySqlDateTime";

			object IMySqlWrapper.GetParameterType(IDbDataParameter parameter)
			{
				return _typeGetter(parameter);
			}

			internal static IMySqlWrapper Initialize(MappingSchema mappingSchema)
			{
				if (_wrapper == null)
				{
					lock (_syncRoot)
					{
						if (_wrapper == null)
						{
							var connectionType    = Type.GetType("MySql.Data.MySqlClient.MySqlConnection, MySql.Data", true);
							var dataReaderType    = connectionType.Assembly.GetType("MySql.Data.MySqlClient.MySqlDataReader", true);
							var parameterType     = connectionType.Assembly.GetType("MySql.Data.MySqlClient.MySqlParameter", true);
							var dbType            = connectionType.Assembly.GetType("MySql.Data.MySqlClient.MySqlDbType", true);
							var mySqlDecimalType  = connectionType.Assembly.GetType("MySql.Data.Types.MySqlDecimal", true);
							var mySqlDateTimeType = connectionType.Assembly.GetType("MySql.Data.Types.MySqlDateTime", true);

							var typeMapper = new TypeMapper(connectionType, parameterType, dbType, mySqlDateTimeType, mySqlDecimalType);

							var dbTypeGetter      = typeMapper.Type<MySqlParameter>().Member(p => p.MySqlDbType).BuildGetter<IDbDataParameter>();
							var decimalGetter     = typeMapper.Type<MySqlDecimal>().Member(p => p.Value).BuildGetter<object>();
							var dateTimeConverter = typeMapper.MapLambda((MySqlDateTime dt) => dt.GetDateTime());

							mappingSchema.SetDataType(mySqlDecimalType, DataType.Decimal);
							mappingSchema.SetDataType(mySqlDateTimeType, DataType.DateTime2);
							mappingSchema.SetConvertExpression(mySqlDateTimeType, typeof(DateTime), dateTimeConverter);

							_wrapper = new MySqlData(
								dataReaderType,
								parameterType,
								mySqlDecimalType,
								mySqlDateTimeType,
								dbTypeGetter,
								decimalGetter);
						}
					}
				}

				return _wrapper;
			}

			[Wrapper]
			internal class MySqlDateTime
			{
				public DateTime GetDateTime() => throw new NotImplementedException();
			}

			[Wrapper]
			internal class MySqlDecimal
			{
				public decimal Value { get; }
			}

			[Wrapper]
			internal class MySqlParameter
			{
				public MySqlDbType MySqlDbType { get; set; }
			}

			[Wrapper]
			public enum MySqlDbType
			{
				Binary,
				Bit,
				Blob,
				Byte,
				Date,
				Datetime,
				DateTime,
				Decimal,
				Double,
				Enum,
				Float,
				Geometry,
				Guid,
				Int16,
				Int24,
				Int32,
				Int64,
				JSON,
				LongBlob,
				LongText,
				MediumBlob,
				MediumText,
				Newdate,
				NewDecimal,
				Set,
				String,
				Text,
				Time,
				Timestamp,
				TinyBlob,
				TinyText,
				UByte,
				UInt16,
				UInt24,
				UInt32,
				UInt64,
				VarBinary,
				VarChar,
				VarString,
				Year
			}
		}

		class MySqlConnector : IMySqlWrapper
		{
			private static readonly object _syncRoot = new object();
			private static IMySqlWrapper? _wrapper;

			private readonly Type _dataReaderType;
			private readonly Type _parameterType;
			private readonly Type _mySqlDateTimeType;

			private readonly Func<IDbDataParameter, MySqlDbType> _typeGetter;

			MySqlConnector(
				Type dataReaderType,
				Type parameterType,
				Type mySqlDateTimeType,
				Func<IDbDataParameter, MySqlDbType> typeGetter)
			{
				_dataReaderType    = dataReaderType;
				_parameterType     = parameterType;
				_mySqlDateTimeType = mySqlDateTimeType;

				_typeGetter        = typeGetter;
			}

			Type  IMySqlWrapper.DataReaderType    => _dataReaderType;

			Type  IMySqlWrapper.ParameterType     => _parameterType;
			Type? IMySqlWrapper.MySqlDecimalType  => null;
			Type  IMySqlWrapper.MySqlDateTimeType => _mySqlDateTimeType;

			object IMySqlWrapper.GetParameterType(IDbDataParameter parameter) => _typeGetter(parameter);

			Func<object, decimal>? IMySqlWrapper.MySqlDecimalGetter  => null;

			string? IMySqlWrapper.GetMySqlDecimalMethodName => null;
			string IMySqlWrapper.GetMySqlDateTimeMethodName => "GetMySqlDateTime";

			internal static IMySqlWrapper Initialize(MappingSchema mappingSchema)
			{
				if (_wrapper == null)
				{
					lock (_syncRoot)
					{
						if (_wrapper == null)
						{
							var connectionType    = Type.GetType("MySql.Data.MySqlClient.MySqlConnection, MySqlConnector", true);
							var dataReaderType    = connectionType.Assembly.GetType("MySql.Data.MySqlClient.MySqlDataReader", true);
							var parameterType     = connectionType.Assembly.GetType("MySql.Data.MySqlClient.MySqlParameter", true);
							var dbType            = connectionType.Assembly.GetType("MySql.Data.MySqlClient.MySqlDbType", true);
							var mySqlDateTimeType = connectionType.Assembly.GetType("MySql.Data.Types.MySqlDateTime", true);

							var typeMapper = new TypeMapper(connectionType, parameterType, dbType, mySqlDateTimeType);

							var dbTypeBuilder     = typeMapper.Type<MySqlParameter>().Member(p => p.MySqlDbType);
							var typeGetter        = dbTypeBuilder.BuildGetter<IDbDataParameter>();
							var dateTimeConverter = typeMapper.MapLambda((MySqlDateTime dt) => dt.GetDateTime());

							mappingSchema.SetDataType(mySqlDateTimeType, DataType.DateTime2);
							mappingSchema.SetConvertExpression(mySqlDateTimeType, typeof(DateTime), dateTimeConverter);

							_wrapper = new MySqlConnector(
								dataReaderType,
								parameterType,
								mySqlDateTimeType,
								typeGetter);
						}
					}
				}

				return _wrapper;
			}

			[Wrapper]
			internal class MySqlDateTime
			{
				public DateTime GetDateTime() => throw new NotImplementedException();
			}

			[Wrapper]
			internal class MySqlParameter
			{
				public MySqlDbType MySqlDbType { get; set; }
			}

			[Wrapper]
			public enum MySqlDbType
			{
				Bool,
				Decimal,
				Byte,
				Int16,
				Int32,
				Float,
				Double,
				Null,
				Timestamp,
				Int64,
				Int24,
				Date,
				Time,
				DateTime,
				Datetime,
				Year,
				Newdate,
				VarString,
				Bit,
				JSON,
				NewDecimal,
				Enum,
				Set,
				TinyBlob,
				MediumBlob,
				LongBlob,
				Blob,
				VarChar,
				String,
				Geometry,
				UByte,
				UInt16,
				UInt32,
				UInt64,
				UInt24,
				Binary,
				VarBinary,
				TinyText,
				MediumText,
				LongText,
				Text,
				Guid
			}
		}

		internal static IMySqlWrapper Initialize(MySqlDataProvider provider)
		{
			if (provider.Name == ProviderName.MySqlConnector)
				return MySqlConnector.Initialize(provider.MappingSchema);
			else
				return MySqlData.Initialize(provider.MappingSchema);
		}
	}
}
