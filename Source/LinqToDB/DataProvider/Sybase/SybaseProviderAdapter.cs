using System;
using System.Data;

namespace LinqToDB.DataProvider.Sybase
{
	using System.Data.Common;
	using LinqToDB.Expressions;

	public class SybaseProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _nativeSyncRoot = new object();
		private static readonly object _managedSyncRoot = new object();

		private static SybaseProviderAdapter? _nativeInstance;
		private static SybaseProviderAdapter? _managedInstance;

		public const string NativeAssemblyName  = "Sybase.AdoNet45.AseClient";
		public const string ManagedAssemblyName = "AdoNetCore.AseClient";

		private SybaseProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			Action<IDbDataParameter, AseDbType> dbTypeSetter,
			Func  <IDbDataParameter, AseDbType> dbTypeGetter,
			BulkCopyAdapter? bulkCopy)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

			GetDbType = dbTypeGetter;
			SetDbType = dbTypeSetter;

			BulkCopy = bulkCopy;
		}

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		public Action<IDbDataParameter, AseDbType> SetDbType { get; }
		public Func  <IDbDataParameter, AseDbType> GetDbType { get; }

		public BulkCopyAdapter? BulkCopy { get; }

		public class BulkCopyAdapter
		{
			internal BulkCopyAdapter(
				Func<IDbConnection, AseBulkCopyOptions, IDbTransaction?, AseBulkCopy> bulkCopyCreator,
				Func<string, string, AseBulkCopyColumnMapping>                        bulkCopyColumnMappingCreator)
			{
				Create              = bulkCopyCreator;
				CreateColumnMapping = bulkCopyColumnMappingCreator;
			}

			public Func<IDbConnection, AseBulkCopyOptions, IDbTransaction?, AseBulkCopy> Create              { get; }
			public Func<string, string, AseBulkCopyColumnMapping>                        CreateColumnMapping { get; }
		}

		public static SybaseProviderAdapter GetInstance(string name)
		{
			if (name == ProviderName.Sybase)
			{
				if (_nativeInstance == null)
				{
					lock (_nativeSyncRoot)
					{
						if (_nativeInstance == null)
						{
							_nativeInstance = CreateAdapter(NativeAssemblyName, "Sybase.Data.AseClient", "Sybase.Data.AseClient", true);
						}
					}
				}

				return _nativeInstance;
			}
			else
			{
				if (_managedInstance == null)
				{
					lock (_managedSyncRoot)
					{
						if (_managedInstance == null)
						{
							_managedInstance = CreateAdapter(ManagedAssemblyName, "AdoNetCore.AseClient", null, false);
						}
					}
				}

				return _managedInstance;
			}
		}

		private static SybaseProviderAdapter CreateAdapter(string assemblyName, string clientNamespace, string? dbFactoryName, bool supportsBulkCopy)
		{
			var assembly = Type.GetType($"{clientNamespace}.AseConnection, {assemblyName}", false)?.Assembly
#if !NETSTANDARD2_0
							?? (dbFactoryName != null ? DbProviderFactories.GetFactory(dbFactoryName).GetType().Assembly : null)
#endif
							;

			if (assembly == null)
				throw new InvalidOperationException($"Cannot load assembly {assemblyName}");

			var connectionType  = assembly.GetType($"{clientNamespace}.AseConnection", true);
			var commandType     = assembly.GetType($"{clientNamespace}.AseCommand", true);
			var parameterType   = assembly.GetType($"{clientNamespace}.AseParameter", true);
			var dataReaderType  = assembly.GetType($"{clientNamespace}.AseDataReader", true);
			var transactionType = assembly.GetType($"{clientNamespace}.AseTransaction", true);
			var dbType          = assembly.GetType($"{clientNamespace}.AseDbType", true);

			TypeMapper       typeMapper;
			BulkCopyAdapter? bulkCopy = null;

			if (supportsBulkCopy)
			{
				var bulkCopyType                        = assembly.GetType($"{clientNamespace}.AseBulkCopy", true);
				var bulkCopyOptionsType                 = assembly.GetType($"{clientNamespace}.AseBulkCopyOptions", true);
				var bulkRowsCopiedEventHandlerType      = assembly.GetType($"{clientNamespace}.AseRowsCopiedEventHandler", true);
				var bulkCopyColumnMappingType           = assembly.GetType($"{clientNamespace}.AseBulkCopyColumnMapping", true);
				var bulkCopyColumnMappingCollectionType = assembly.GetType($"{clientNamespace}.AseBulkCopyColumnMappingCollection", true);
				var rowsCopiedEventArgsType             = assembly.GetType($"{clientNamespace}.AseRowsCopiedEventArgs", true);

				typeMapper = new TypeMapper(
					connectionType, parameterType, transactionType, dbType,
					bulkCopyType, bulkCopyOptionsType, bulkRowsCopiedEventHandlerType, bulkCopyColumnMappingType, bulkCopyColumnMappingCollectionType, rowsCopiedEventArgsType);
			}
			else
				typeMapper = new TypeMapper(connectionType, parameterType, transactionType, dbType);

			typeMapper.RegisterWrapper<AseConnection>();
			typeMapper.RegisterWrapper<AseParameter>();
			typeMapper.RegisterWrapper<AseDbType>();
			typeMapper.RegisterWrapper<AseTransaction>();

			if (supportsBulkCopy)
			{
				// bulk copy types
				typeMapper.RegisterWrapper<AseBulkCopy>();
				typeMapper.RegisterWrapper<AseBulkCopyOptions>();
				typeMapper.RegisterWrapper<AseRowsCopiedEventHandler>();
				typeMapper.RegisterWrapper<AseBulkCopyColumnMapping>();
				typeMapper.RegisterWrapper<AseBulkCopyColumnMappingCollection>();
				typeMapper.RegisterWrapper<AseRowsCopiedEventArgs>();

				bulkCopy = new BulkCopyAdapter(
					(IDbConnection connection, AseBulkCopyOptions options, IDbTransaction? transaction)
						=> typeMapper.CreateAndWrap(() => new AseBulkCopy((AseConnection)connection, options, (AseTransaction?)transaction))!,
					(string source, string destination)
						=> typeMapper.CreateAndWrap(() => new AseBulkCopyColumnMapping(source, destination))!);
			}

			var paramMapper   = typeMapper.Type<AseParameter>();
			var dbTypeBuilder = paramMapper.Member(p => p.AseDbType);

			if (supportsBulkCopy)
			{
			}

			return new SybaseProviderAdapter(
				connectionType,
				dataReaderType,
				parameterType,
				commandType,
				transactionType,
				dbTypeBuilder.BuildSetter<IDbDataParameter>(),
				dbTypeBuilder.BuildGetter<IDbDataParameter>(),
				bulkCopy);
		}

		[Wrapper]
		internal class AseParameter
		{
			public AseDbType AseDbType { get; set; }
		}

		[Wrapper]
		public enum AseDbType
		{
			BigDateTime      = 93,
			BigInt           = -5,
			Binary           = -2,
			Bit              = -7,
			Char             = 1,
			Date             = 91,
			DateTime         = 93,
			Decimal          = 3,
			Double           = 8,
			Image            = -4,
			Integer          = 4,
			Money            = -200,
			NChar            = -204,
			Numeric          = 2,
			NVarChar         = -205,
			Real             = 7,
			SmallDateTime    = -202,
			SmallInt         = 5,
			SmallMoney       = -201,
			Text             = -1,
			Time             = 92,
			TimeStamp        = -203,
			TinyInt          = -6,
			UniChar          = -8,
			Unitext          = -10,
			UniVarChar       = -9,
			UnsignedBigInt   = -208,
			UnsignedInt      = -207,
			UnsignedSmallInt = -206,
			Unsupported      = 0,
			VarBinary        = -3,
			VarChar          = 12
		}

		[Wrapper]
		public class AseConnection
		{
		}

		[Wrapper]
		public class AseTransaction
		{
		}

		#region BulkCopy
		[Wrapper]
		public class AseBulkCopy : TypeWrapper, IDisposable
		{
			public AseBulkCopy(object instance, TypeMapper mapper) : base(instance, mapper)
			{
				this.WrapEvent<AseBulkCopy, AseRowsCopiedEventHandler>(nameof(AseRowsCopied));
			}

			public AseBulkCopy(AseConnection connection, AseBulkCopyOptions options, AseTransaction? transaction) => throw new NotImplementedException();

			void IDisposable.Dispose() => this.WrapAction(t => ((IDisposable)t).Dispose());

			public void WriteToServer(IDataReader dataReader) => this.WrapAction(t => t.WriteToServer(dataReader));

			public int NotifyAfter
			{
				get => this.Wrap(t => t.NotifyAfter);
				set => this.SetPropValue(t => t.NotifyAfter, value);
			}

			public int BatchSize
			{
				get => this.Wrap(t => t.BatchSize);
				set => this.SetPropValue(t => t.BatchSize, value);
			}

			public int BulkCopyTimeout
			{
				get => this.Wrap(t => t.BulkCopyTimeout);
				set => this.SetPropValue(t => t.BulkCopyTimeout, value);
			}

			public string? DestinationTableName
			{
				get => this.Wrap(t => t.DestinationTableName);
				set => this.SetPropValue(t => t.DestinationTableName, value);
			}

			public AseBulkCopyColumnMappingCollection ColumnMappings
			{
				get => this.Wrap(t => t.ColumnMappings);
			}

			public event AseRowsCopiedEventHandler AseRowsCopied
			{
				add => Events.AddHandler(nameof(AseRowsCopied), value);
				remove => Events.RemoveHandler(nameof(AseRowsCopied), value);
			}
		}

		[Wrapper]
		public class AseRowsCopiedEventArgs : TypeWrapper
		{
			public AseRowsCopiedEventArgs(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			// sic! Row, not Rows
			public int RowCopied
			{
				get => this.Wrap(t => t.RowCopied);
				set => this.SetPropValue(t => t.RowCopied, value);
			}

			public bool Abort
			{
				get => this.Wrap(t => t.Abort);
				set => this.SetPropValue(t => t.Abort, value);
			}
		}

		[Wrapper]
		public delegate void AseRowsCopiedEventHandler(object sender, AseRowsCopiedEventArgs e);

		[Wrapper]
		public class AseBulkCopyColumnMappingCollection : TypeWrapper
		{
			public AseBulkCopyColumnMappingCollection(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public AseBulkCopyColumnMapping Add(AseBulkCopyColumnMapping bulkCopyColumnMapping) => this.Wrap(t => t.Add(bulkCopyColumnMapping));
		}

		[Wrapper, Flags]
		public enum AseBulkCopyOptions
		{
			Default                = 0,
			CheckConstraints       = 1,
			FireTriggers           = 2,
			KeepIdentity           = 4,
			KeepNulls              = 8,
			TableLock              = 16,
			UseInternalTransaction = 32,
			EnableBulkLoad_0       = 64,
			EnableBulkLoad_1       = 128,
			EnableBulkLoad_2       = 256
		}

		[Wrapper]
		public class AseBulkCopyColumnMapping : TypeWrapper
		{
			public AseBulkCopyColumnMapping(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public AseBulkCopyColumnMapping(int source, int destination) => throw new NotImplementedException();

			public AseBulkCopyColumnMapping(string source, string destination) => throw new NotImplementedException();
		}

		#endregion
	}
}
