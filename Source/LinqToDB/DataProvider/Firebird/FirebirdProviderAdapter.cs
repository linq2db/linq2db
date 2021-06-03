using System;
using System.Data;

namespace LinqToDB.DataProvider.Firebird
{
	using System.Globalization;
	using System.Text;
	using LinqToDB.Common;
	using LinqToDB.Expressions;
	using LinqToDB.Mapping;
	using LinqToDB.SqlQuery;

	public class FirebirdProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _syncRoot = new ();
		private static FirebirdProviderAdapter? _instance;

		public const string AssemblyName    = "FirebirdSql.Data.FirebirdClient";
		public const string ClientNamespace = "FirebirdSql.Data.FirebirdClient";
		public const string TypesNamespace  = "FirebirdSql.Data.Types";

		private FirebirdProviderAdapter(
			Type                               connectionType,
			Type                               dataReaderType,
			Type                               parameterType,
			Type                               commandType,
			Type                               transactionType,
			Type?                              fbDecFloatType,
			Type?                              fbZonedDateTimeType,
			Type?                              fbZonedTimeType,
			MappingSchema?                     mappingSchema,
			Action<IDbDataParameter, FbDbType> dbTypeSetter,
			Func<IDbDataParameter, FbDbType>   dbTypeGetter,
			Action                             clearAllPulls)
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

			SetDbType = dbTypeSetter;
			GetDbType = dbTypeGetter;
			ClearAllPools = clearAllPulls;
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

		public string? ProviderTypesNamespace => FbDecFloatType != null || FbZonedDateTimeType != null || FbZonedTimeType != null ? TypesNamespace : null;

		public MappingSchema? MappingSchema { get; }

		public Action<IDbDataParameter, FbDbType> SetDbType { get; }
		public Func<IDbDataParameter, FbDbType>   GetDbType { get; }

		public Action ClearAllPools { get; }

		public static FirebirdProviderAdapter GetInstance()
		{
			if (_instance == null)
				lock (_syncRoot)
					if (_instance == null)
					{
						var assembly = Tools.TryLoadAssembly(AssemblyName, null);
						if (assembly == null)
							throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");

						var connectionType  = assembly.GetType($"{ClientNamespace}.FbConnection" , true)!;
						var dataReaderType  = assembly.GetType($"{ClientNamespace}.FbDataReader" , true)!;
						var parameterType   = assembly.GetType($"{ClientNamespace}.FbParameter"  , true)!;
						var commandType     = assembly.GetType($"{ClientNamespace}.FbCommand"    , true)!;
						var transactionType = assembly.GetType($"{ClientNamespace}.FbTransaction", true)!;
						var dbType          = assembly.GetType($"{ClientNamespace}.FbDbType"     , true)!;

						var fbDecFloatType  = assembly.GetType($"{TypesNamespace}.FbDecFloat"       , false);
						var fbZonedDateTime = assembly.GetType($"{TypesNamespace}.FbZonedDateTime"  , false);
						var fbZonedTimeType = assembly.GetType($"{TypesNamespace}.FbZonedTime"      , false);
						var decimalTypeType = assembly.GetType("FirebirdSql.Data.Common.DecimalType", false)!;

						var typeMapper = new TypeMapper();

						typeMapper.RegisterTypeWrapper<FbConnection>(connectionType);
						typeMapper.RegisterTypeWrapper<FbParameter>(parameterType);
						typeMapper.RegisterTypeWrapper<FbDbType>(dbType);

						MappingSchema? mappingSchema = new MappingSchema();

						// we don't provide default mappings to non-provider types
						// as it looks like there is no suitable .net types
						// such mappings could be added by user manually
						if (fbDecFloatType != null)
						{
							typeMapper.RegisterTypeWrapper<FbDecFloat>(fbDecFloatType);
							mappingSchema ??= new MappingSchema();

							mappingSchema.SetDataType(fbDecFloatType, new SqlDataType(DataType.DecFloat, fbDecFloatType, "DECFLOAT"));
							// we don't register literal generation for decfloat as it looks like special values (inf, (s)nan are not supported in literals)
						}
						if (fbZonedDateTime != null)
						{
							typeMapper.RegisterTypeWrapper<FbZonedDateTime>(fbZonedDateTime);
							mappingSchema ??= new MappingSchema();
							mappingSchema.SetDataType(fbZonedDateTime, new SqlDataType(DataType.DateTimeOffset, fbZonedDateTime, "TIMESPAN WITH TIME ZONE"));
						}
						if (fbZonedTimeType != null)
						{
							typeMapper.RegisterTypeWrapper<FbZonedTime>(fbZonedTimeType);
							mappingSchema ??= new MappingSchema();
							mappingSchema.SetDataType(fbZonedTimeType, new SqlDataType(DataType.TimeTZ, fbZonedTimeType, "TIME WITH TIME ZONE"));
						}

						typeMapper.FinalizeMappings();

						var dbTypeBuilder = typeMapper.Type<FbParameter>().Member(p => p.FbDbType);
						var clearAllPools = typeMapper.BuildAction(typeMapper.MapActionLambda(() => FbConnection.ClearAllPools()));

						_instance = new FirebirdProviderAdapter(
							connectionType,
							dataReaderType,
							parameterType,
							commandType,
							transactionType,
							fbDecFloatType,
							fbZonedDateTime,
							fbZonedTimeType,
							mappingSchema,
							dbTypeBuilder.BuildSetter<IDbDataParameter>(),
							dbTypeBuilder.BuildGetter<IDbDataParameter>(),
							clearAllPools);
					}

			return _instance;
		}

		#region Wrappers
		[Wrapper]
		private class FbDecFloat
		{
		}

		[Wrapper]
		private class FbZonedDateTime
		{
		}

		[Wrapper]
		private class FbZonedTime
		{
		}

		[Wrapper]
		private class FbConnection
		{
			public static void ClearAllPools() => throw new NotImplementedException();
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

			// new in 7.10.0
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
