using System;
using System.Data;

namespace LinqToDB.DataProvider.Firebird
{
	using System.Data.Common;
	using LinqToDB.Expressions;

	public class FirebirdProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _syncRoot = new object();
		private static FirebirdProviderAdapter? _instance;

		public const string AssemblyName    = "FirebirdSql.Data.FirebirdClient";
		public const string ClientNamespace = "FirebirdSql.Data.FirebirdClient";

		private FirebirdProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			Func<DbParameter, FbDbType> dbTypeGetter,
			Action clearAllPulls)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

			GetDbType     = dbTypeGetter;
			ClearAllPools = clearAllPulls;
		}

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		public Func<DbParameter, FbDbType> GetDbType { get; }
		public Action ClearAllPools { get; }

		public static FirebirdProviderAdapter GetInstance()
		{
			if (_instance == null)
				lock (_syncRoot)
					if (_instance == null)
					{
						var assembly = Common.Tools.TryLoadAssembly(AssemblyName, null);
						if (assembly == null)
							throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");

						var connectionType  = assembly.GetType($"{ClientNamespace}.FbConnection" , true)!;
						var dataReaderType  = assembly.GetType($"{ClientNamespace}.FbDataReader" , true)!;
						var parameterType   = assembly.GetType($"{ClientNamespace}.FbParameter"  , true)!;
						var commandType     = assembly.GetType($"{ClientNamespace}.FbCommand"    , true)!;
						var transactionType = assembly.GetType($"{ClientNamespace}.FbTransaction", true)!;
						var dbType          = assembly.GetType($"{ClientNamespace}.FbDbType"     , true)!;

						var typeMapper = new TypeMapper();

						typeMapper.RegisterTypeWrapper<FbConnection>(connectionType);
						typeMapper.RegisterTypeWrapper<FbParameter>(parameterType);
						typeMapper.RegisterTypeWrapper<FbDbType>(dbType);

						typeMapper.FinalizeMappings();

						var typeGetter    = typeMapper.Type<FbParameter>().Member(p => p.FbDbType).BuildGetter<DbParameter>();
						var clearAllPools = typeMapper.BuildAction(typeMapper.MapActionLambda(() => FbConnection.ClearAllPools()));

						_instance = new FirebirdProviderAdapter(
							connectionType,
							dataReaderType,
							parameterType,
							commandType,
							transactionType,
							typeGetter,
							clearAllPools);
					}

			return _instance;
		}

		#region Wrappers
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
			VarChar   = 16
		}
		#endregion
	}
}
