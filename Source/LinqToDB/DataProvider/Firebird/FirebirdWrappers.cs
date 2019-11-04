using System;
using System.Data;

namespace LinqToDB.DataProvider.Firebird
{
	using LinqToDB.Expressions;

	internal static class FirebirdWrappers
	{
		private static readonly object      _syncRoot = new object();
		private static          TypeMapper? _typeMapper;

		internal static Type?                             ParameterType;
		internal static Type?                             ConnectionType;
		internal static Func<IDbDataParameter, FbDbType>? TypeGetter;
		internal static Action?                           ClearAllPools;

		internal static void Initialize(Type connectionType)
		{
			if (_typeMapper == null)
			{
				lock (_syncRoot)
				{
					if (_typeMapper == null)
					{
						ConnectionType = connectionType;
						ParameterType  = connectionType.Assembly.GetType("FirebirdSql.Data.FirebirdClient.FbParameter", true);
						var dbType     = connectionType.Assembly.GetType("FirebirdSql.Data.FirebirdClient.FbDbType", true);

						_typeMapper = new TypeMapper(ConnectionType, ParameterType, dbType);

						var dbTypeBuilder = _typeMapper.Type<FbParameter>().Member(p => p.FbDbType);
						TypeGetter        = dbTypeBuilder.BuildGetter<IDbDataParameter>();

						ClearAllPools = _typeMapper.BuildAction(_typeMapper.MapActionLambda(() => FbConnection.ClearAllPools()));
					}
				}
			}
		}


		[Wrapper]
		internal class FbConnection
		{
			public static void ClearAllPools() => throw new NotImplementedException();
		}

		[Wrapper]
		internal class FbParameter
		{
			public FbDbType FbDbType { get; set; }
		}

		[Wrapper]
		internal enum FbDbType
		{
			Array,
			BigInt,
			Binary,
			Boolean,
			Char,
			Date,
			Decimal,
			Double,
			Float,
			Guid,
			Integer,
			Numeric,
			SmallInt,
			Text,
			Time,
			TimeStamp,
			VarChar
		}
	}
}
