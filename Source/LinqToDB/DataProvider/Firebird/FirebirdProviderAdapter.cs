using System;
using System.Data;

namespace LinqToDB.DataProvider.Firebird
{
	using System.Linq.Expressions;
	using System.Numerics;
	using LinqToDB.Expressions;
	using LinqToDB.Mapping;

	public class FirebirdProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _syncRoot = new object();
		private static FirebirdProviderAdapter? _instance;

		public const string AssemblyName    = "FirebirdSql.Data.FirebirdClient";
		public const string ClientNamespace = "FirebirdSql.Data.FirebirdClient";
		public const string TypesNamespace  = "FirebirdSql.Data.Types";

		private FirebirdProviderAdapter(
			Type                                    connectionType,
			Type                                    dataReaderType,
			Type                                    parameterType,
			Type                                    commandType,
			Type                                    transactionType,
			Type?                                   fbDecFloatType,
			Type?                                   fbZonedDateTimeType,
			Type?                                   fbZonedTimeType,
			MappingSchema                           mappingSchema,
			Func<IDbDataParameter, FbDbType>        dbTypeGetter,
			Action                                  clearAllPulls,
			Func<string, FbConnection>              connectionCreator,
			Func<string, FbConnectionStringBuilder> csBuilderCreator)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

			FbDecFloatType      = fbDecFloatType;
			FbZonedDateTimeType = fbZonedDateTimeType;
			FbZonedTimeType     = fbZonedTimeType;
			MappingSchema       = mappingSchema;

			GetDbType     = dbTypeGetter;
			ClearAllPools = clearAllPulls;

			_connectionCreator = connectionCreator;
			_csBuilderCreator  = csBuilderCreator;
		}

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		/// <summary>
		/// FB client 7.10.0+.
		/// </summary>
		public Type? FbDecFloatType      { get; }
		public Type? FbZonedDateTimeType { get; }
		public Type? FbZonedTimeType     { get; }

		public MappingSchema MappingSchema { get; }

		public Func<IDbDataParameter, FbDbType> GetDbType { get; }
		public Action ClearAllPools { get; }

		private readonly Func<string, FbConnection> _connectionCreator;
		internal FbConnection CreateConnection(string connectionString) => _connectionCreator(connectionString);

		private readonly Func<string, FbConnectionStringBuilder> _csBuilderCreator;
		internal FbConnectionStringBuilder CreateConnectionStringBuilder(string connectionString) => _csBuilderCreator(connectionString);

		public static FirebirdProviderAdapter GetInstance()
		{
			if (_instance == null)
				lock (_syncRoot)
					if (_instance == null)
					{
						var assembly = Common.Tools.TryLoadAssembly(AssemblyName, null);
						if (assembly == null)
							throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");

						var connectionType          = assembly.GetType($"{ClientNamespace}.FbConnection"             , true)!;
						var dataReaderType          = assembly.GetType($"{ClientNamespace}.FbDataReader"             , true)!;
						var parameterType           = assembly.GetType($"{ClientNamespace}.FbParameter"              , true)!;
						var commandType             = assembly.GetType($"{ClientNamespace}.FbCommand"                , true)!;
						var transactionType         = assembly.GetType($"{ClientNamespace}.FbTransaction"            , true)!;
						var dbType                  = assembly.GetType($"{ClientNamespace}.FbDbType"                 , true)!;
						var connectionStringBuilder = assembly.GetType($"{ClientNamespace}.FbConnectionStringBuilder", true)!;

						var fbDecFloatType  = assembly.GetType($"{TypesNamespace}.FbDecFloat"     , false);
						var fbZonedDateTime = assembly.GetType($"{TypesNamespace}.FbZonedDateTime", false);
						var fbZonedTime     = assembly.GetType($"{TypesNamespace}.FbZonedTime"    , false);

						var typeMapper = new TypeMapper();

						var mappingSchema = new MappingSchema();
						typeMapper.RegisterTypeWrapper<FbConnection>(connectionType);
						typeMapper.RegisterTypeWrapper<FbConnectionStringBuilder>(connectionStringBuilder);
						typeMapper.RegisterTypeWrapper<FbParameter>(parameterType);
						typeMapper.RegisterTypeWrapper<FbDbType>(dbType);

						if (fbDecFloatType != null)
						{
							typeMapper.RegisterTypeWrapper<FbDecFloat>(fbDecFloatType);
							//mappingSchema.SetDataType(fbDecFloatType, DataType.DecFloat);
							//mappingSchema.SetConvertExpression(fbDecFloatType, typeof(decimal), dateTimeConverter);
							//mappingSchema.SetConvertExpression(fbDecFloatType, typeof(BigInteger), dateTimeConverter);
						}
						if (fbZonedDateTime != null)
						{
							typeMapper.RegisterTypeWrapper<FbZonedDateTime>(fbZonedDateTime);
							//mappingSchema.SetDataType(fbZonedDateTime, DataType.DateTimeOffset);
							//mappingSchema.SetConvertExpression(fbDecFloatType, typeof(decimal), dateTimeConverter);
							//mappingSchema.SetConvertExpression(fbDecFloatType, typeof(BigInteger), dateTimeConverter);
						}
						if (fbZonedTime != null)
						{
							typeMapper.RegisterTypeWrapper<FbZonedTime>(fbZonedTime);
							//mappingSchema.SetDataType(fbZonedTime, DataType.TimeTZ);
							//mappingSchema.SetConvertExpression(fbDecFloatType, typeof(decimal), dateTimeConverter);
							//mappingSchema.SetConvertExpression(fbDecFloatType, typeof(BigInteger), dateTimeConverter);
						}

						typeMapper.FinalizeMappings();

						var typeGetter    = typeMapper.Type<FbParameter>().Member(p => p.FbDbType).BuildGetter<IDbDataParameter>();
						var clearAllPools = typeMapper.BuildAction(typeMapper.MapActionLambda(() => FbConnection.ClearAllPools()));

						_instance = new FirebirdProviderAdapter(
							connectionType,
							dataReaderType,
							parameterType,
							commandType,
							transactionType,
							fbDecFloatType,
							fbZonedDateTime,
							fbZonedTime,
							mappingSchema,
							typeGetter,
							clearAllPools,
							typeMapper.BuildWrappedFactory((string connectionString) => new FbConnection(connectionString)),
							typeMapper.BuildWrappedFactory((string connectionString) => new FbConnectionStringBuilder(connectionString)));
					}

			return _instance;
		}

		#region Wrappers
		[Wrapper]
		private class FbDecFloat
		{
			public BigInteger Coefficient { get; }
			public int        Exponent    { get; }
		}

		[Wrapper]
		private class FbZonedDateTime
		{
			public DateTime  DateTime { get; }
			public TimeSpan? Offset   { get; }
			public string?   TimeZone { get; }
		}

		[Wrapper]
		private class FbZonedTime
		{
			public TimeSpan  Time     { get; }
			public TimeSpan? Offset   { get; }
			public string?   TimeZone { get; }
		}

		[Wrapper]
		internal class FbConnection : TypeWrapper, IDisposable
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: CreateCommand
				(Expression<Func<FbConnection, IDbCommand>>)((FbConnection this_) => this_.CreateCommand()),
				// [1]: Open
				(Expression<Action<FbConnection>>      )((FbConnection this_) => this_.Open()),
				// [2]: Dispose
				(Expression<Action<FbConnection>>      )((FbConnection this_) => this_.Dispose()),
			};

			public FbConnection(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public FbConnection(string connectionString) => throw new NotImplementedException();

			public static void ClearAllPools() => throw new NotImplementedException();

			public IDbCommand CreateCommand() => ((Func<FbConnection, IDbCommand>)CompiledWrappers[0])(this);
			public void       Open()          => ((Action<FbConnection>)CompiledWrappers[1])(this);
			public void       Dispose()       => ((Action<FbConnection>)CompiledWrappers[2])(this);
		}

		[Wrapper]
		internal class FbConnectionStringBuilder : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get Dialect
				(Expression<Func<FbConnectionStringBuilder, int>>)((FbConnectionStringBuilder this_) => this_.Dialect),
			};

			public FbConnectionStringBuilder(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public FbConnectionStringBuilder(string connectionString) => throw new NotImplementedException();

			public int Dialect => ((Func<FbConnectionStringBuilder, int>)CompiledWrappers[0])(this);
		}

		[Wrapper]
		private class FbParameter
		{
			public FbDbType FbDbType { get; set; }
		}

		[Wrapper]
		public enum FbDbType
		{
			Array         = 0,
			BigInt        = 1,
			Binary        = 2,
			Boolean       = 3,
			Char          = 4,
			Date          = 5,
			Decimal       = 6,
			Double        = 7,
			Float         = 8,
			Guid          = 9,
			Integer       = 10,
			Numeric       = 11,
			SmallInt      = 12,
			Text          = 13,
			Time          = 14,
			TimeStamp     = 15,
			VarChar       = 16,
			// FB 4.0 types (client 7.10.0+)
			TimeStampTZ   = 17,
			TimeStampTZEx = 18,
			TimeTZ        = 19,
			TimeTZEx      = 20,
			Dec16         = 21,
			Dec34         = 22,
			Int128        = 23,
		}
		#endregion
	}
}
