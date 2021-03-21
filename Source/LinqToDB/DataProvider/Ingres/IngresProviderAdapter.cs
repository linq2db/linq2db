using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.Ingres
{
	using Common;
	using Data;
	using Expressions;
	using Extensions;
	using Mapping;
	using SqlQuery;

	public class IngresProviderAdapter : IDynamicProviderAdapter
    {
		private static readonly object _syncRoot = new object();
		private static IngresProviderAdapter? _instance;

		public const string AssemblyName    = "Actian.Client";
		public const string ClientNamespace = "Ingres.Client";

		private IngresProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			
			MappingSchema mappingSchema,

			Func<string, IngresConnection> connectionCreator,

			Action<IDbDataParameter, IngresType> dbTypeSetter,
			Func<IDbDataParameter, IngresType> dbTypeGetter)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

			MappingSchema      = mappingSchema;
			_connectionCreator = connectionCreator;

			SetDbType = dbTypeSetter;
			GetDbType = dbTypeGetter;
		}

		public Type ConnectionType { get; }
		public Type DataReaderType { get; }
		public Type ParameterType { get; }
		public Type CommandType { get; }
		public Type TransactionType { get; }

		public Action<IDbDataParameter, IngresType> SetDbType { get; }
		public Func<IDbDataParameter, IngresType> GetDbType { get; }

		private readonly Func<string, IngresConnection> _connectionCreator;
		public IngresConnection CreateConnection(string connectionString) => _connectionCreator(connectionString);

		public MappingSchema MappingSchema { get; }

		public static IngresProviderAdapter GetInstance()
		{
			if (_instance == null)
				lock (_syncRoot)
					if (_instance == null)
					{
						var assembly = Tools.TryLoadAssembly(AssemblyName, null);
						if (assembly == null)
							throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");

						var connectionType  = assembly.GetType($"{ClientNamespace}.IngresConnection"  , true)!;
						var parameterType   = assembly.GetType($"{ClientNamespace}.IngresParameter"   , true)!;
						var dataReaderType  = assembly.GetType($"{ClientNamespace}.IngresDataReader"  , true)!;
						var commandType     = assembly.GetType($"{ClientNamespace}.IngresCommand"     , true)!;
						var transactionType = assembly.GetType($"{ClientNamespace}.IngresTransaction" , true)!;
						var dbType          = assembly.GetType($"{ClientNamespace}.IngresType"       , true)!;

						var typeMapper = new TypeMapper();
						typeMapper.RegisterTypeWrapper<IngresConnection>(connectionType);
						typeMapper.RegisterTypeWrapper<IngresParameter>(parameterType);
						typeMapper.RegisterTypeWrapper<IngresType>(dbType);
						typeMapper.FinalizeMappings();

						var paramMapper   = typeMapper.Type<IngresParameter>();
						var dbTypeBuilder = paramMapper.Member(p => p.IngresType);

						// create mapping schema
						var mappingSchema = new MappingSchema();

						_instance = new IngresProviderAdapter(
							connectionType,
							dataReaderType,
							parameterType,
							commandType,
							transactionType,
							
							mappingSchema,

							typeMapper.BuildWrappedFactory((string connectionString) => new IngresConnection(connectionString)),

							dbTypeBuilder.BuildSetter<IDbDataParameter>(),
							dbTypeBuilder.BuildGetter<IDbDataParameter>()
							);
					}

			return _instance;
		}

		#region Wrappers

		[Wrapper]
		private class IngresParameter
		{
			public IngresType IngresType { get; set; }
		}

		[Wrapper]
		public enum IngresType
		{
			/// <summary>
			/// Fixed length stream of binary data.
			/// </summary>
			Binary = -2,
			/// <summary>
			/// Variable length stream of binary data.
			/// </summary>
			VarBinary = -3,
			/// <summary>
			/// Binary large object.
			/// </summary>
			LongVarBinary = -4,
			/// <summary>
			/// Fixed length stream of character data.
			/// </summary>
			Char = 1,
			/// <summary>
			/// Variable length stream of character data.
			/// </summary>
			VarChar = 12,
			/// <summary>
			/// Character large object.
			/// </summary>
			LongVarChar = -1,
			/// <summary>
			/// Signed 8-bit integer data.
			/// </summary>
			TinyInt = -6,
			/// <summary>
			/// Signed 64-bit integer data.
			/// </summary>
			BigInt = -5,
			/// <summary>
			/// Exact numeric data.
			/// </summary>
			Decimal = 3,
			/// <summary>
			/// Signed 16-bit integer data.
			/// </summary>
			SmallInt = 5,
			/// <summary>
			/// Signed 32-bit integer data.
			/// </summary>
			Int = 4,
			/// <summary>
			/// Approximate numeric data.
			/// </summary>
			Real = 7,
			/// <summary>
			/// Approximate numeric data.
			/// </summary>
			Double = 8,
			/// <summary>
			/// Date and time data.
			/// </summary>
			DateTime = 93,
			/// <summary>
			/// Ingres Date and time data.
			/// </summary>
			IngresDate = 1093,
			/// <summary>
			/// Fixed length stream of Unicode data.
			/// </summary>
			NChar = -95,
			/// <summary>
			/// Variable length stream of Unicode data.
			/// </summary>
			NVarChar = -96,
			/// <summary>
			/// Unicode large object.
			/// </summary>
			LongNVarChar = -97,
			/// <summary>
			/// ANSI Date.
			/// </summary>
			Date = 91,
			/// <summary>
			/// ANSI Time.
			/// </summary>
			Time = 92,
			/// <summary>
			/// ANSI Interval Year to Month.
			/// </summary>
			IntervalYearToMonth = 107,
			/// <summary>
			/// ANSI Interval Day to Second.
			/// </summary>
			IntervalDayToSecond = 110,
			/// <summary>
			/// Boolean.
			/// </summary>
			Boolean = 0x10
		}

		[Wrapper]
		public class IngresConnection : TypeWrapper, IDisposable
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: Open
				(Expression<Action<IngresConnection>>       )((IngresConnection this_) => this_.Open()),
				// [1]: Dispose
				(Expression<Action<IngresConnection>>       )((IngresConnection this_) => this_.Dispose()),
			};

			public IngresConnection(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public IngresConnection(string connectionString) => throw new NotImplementedException();

			public void    Open()            => ((Action<IngresConnection>)CompiledWrappers[0])(this);
			public void    Dispose()         => ((Action<IngresConnection>)CompiledWrappers[1])(this);
		}

		#endregion
	}
}
