﻿using System;
using System.Data;
using System.Data.Common;

namespace LinqToDB.DataProvider.Firebird
{
	using Common;
	using Expressions;
	using Mapping;
	using SqlQuery;

	public class FirebirdProviderAdapter : IDynamicProviderAdapter
	{
		public const string AssemblyName    = "FirebirdSql.Data.FirebirdClient";
		public const string ClientNamespace = "FirebirdSql.Data.FirebirdClient";
		public const string TypesNamespace  = "FirebirdSql.Data.Types";

		FirebirdProviderAdapter()
		{
			var assembly = Tools.TryLoadAssembly(AssemblyName, null);

			if (assembly == null)
				throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");

			ConnectionType      = assembly.GetType($"{ClientNamespace}.FbConnection" , true)!;
			DataReaderType      = assembly.GetType($"{ClientNamespace}.FbDataReader" , true)!;
			ParameterType       = assembly.GetType($"{ClientNamespace}.FbParameter"  , true)!;
			CommandType         = assembly.GetType($"{ClientNamespace}.FbCommand"    , true)!;
			TransactionType     = assembly.GetType($"{ClientNamespace}.FbTransaction", true)!;

			var dbType          = assembly.GetType($"{ClientNamespace}.FbDbType"     , true)!;

			FbDecFloatType      = assembly.GetType($"{TypesNamespace}.FbDecFloat"       , false);
			FbZonedDateTimeType = assembly.GetType($"{TypesNamespace}.FbZonedDateTime"  , false);
			FbZonedTimeType     = assembly.GetType($"{TypesNamespace}.FbZonedTime"      , false);

			var decimalTypeType = assembly.GetType("FirebirdSql.Data.Common.DecimalType", false)!;

			var typeMapper = new TypeMapper();

			typeMapper.RegisterTypeWrapper<FbConnection>(ConnectionType);
			typeMapper.RegisterTypeWrapper<FbParameter>(ParameterType);
			typeMapper.RegisterTypeWrapper<FbDbType>(dbType);

			MappingSchema = new FirebirdAdapterMappingSchema();

			// we don't provide default mappings to non-provider types
			// as it looks like there is no suitable .net types
			// such mappings could be added by user manually
			if (FbDecFloatType != null)
			{
				typeMapper.RegisterTypeWrapper<FbDecFloat>(FbDecFloatType);
				MappingSchema.SetDataType(FbDecFloatType, new SqlDataType(DataType.DecFloat, FbDecFloatType, "DECFLOAT"));
				// we don't register literal generation for decfloat as it looks like special values (inf, (s)nan are not supported in literals)
			}

			if (FbZonedDateTimeType != null)
			{
				typeMapper.RegisterTypeWrapper<FbZonedDateTime>(FbZonedDateTimeType);
				MappingSchema.SetDataType(FbZonedDateTimeType, new SqlDataType(DataType.DateTimeOffset, FbZonedDateTimeType, "TIMESPAN WITH TIME ZONE"));
			}

			if (FbZonedTimeType != null)
			{
				typeMapper.RegisterTypeWrapper<FbZonedTime>(FbZonedTimeType);
				MappingSchema.SetDataType(FbZonedTimeType, new SqlDataType(DataType.TimeTZ, FbZonedTimeType, "TIME WITH TIME ZONE"));
			}

			typeMapper.FinalizeMappings();

			var dbTypeBuilder = typeMapper.Type<FbParameter>().Member(p => p.FbDbType);

			SetDbType = dbTypeBuilder.BuildSetter<IDbDataParameter>();
			GetDbType = dbTypeBuilder.BuildGetter<IDbDataParameter>();

			ClearAllPools = typeMapper.BuildAction(typeMapper.MapActionLambda(() => FbConnection.ClearAllPools()));

			IsDateOnlySupported = assembly.GetName().Version >= MinDateOnlyVersion;
		}

		static readonly Lazy<FirebirdProviderAdapter> _lazy    = new (() => new ());
		internal static FirebirdProviderAdapter       Instance => _lazy.Value;

		sealed class FirebirdAdapterMappingSchema : LockedMappingSchema
		{
			public FirebirdAdapterMappingSchema() : base("FirebirdAdapter")
			{
			}
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

		public MappingSchema MappingSchema { get; }

		public Action<DbParameter, FbDbType> SetDbType { get; }
		public Func<DbParameter  , FbDbType> GetDbType { get; }

		public Action ClearAllPools { get; }

		public bool IsDateOnlySupported { get; }

		private static readonly Version MinDateOnlyVersion = new (9, 0, 0);

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
			Array     = 0,
			BigInt    = 1,
			Binary    = 2,
			Boolean   = 3,
			Char      = 4,
			Date      = 5,
			Decimal   = 6,
			Double    = 7,
			Float     = 8,
			Guid      = 9,
			Integer   = 10,
			Numeric   = 11,
			SmallInt  = 12,
			Text      = 13,
			Time      = 14,
			TimeStamp = 15,
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
