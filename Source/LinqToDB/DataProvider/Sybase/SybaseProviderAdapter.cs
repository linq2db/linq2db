using System;
using System.Data;

namespace LinqToDB.DataProvider.Sybase
{
	using System.Linq.Expressions;
	using LinqToDB.Expressions;

	public class SybaseProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _nativeSyncRoot = new object();
		private static readonly object _managedSyncRoot = new object();

		private static SybaseProviderAdapter? _nativeInstance;
		private static SybaseProviderAdapter? _managedInstance;

		public const string NativeAssemblyName        = "Sybase.AdoNet45.AseClient";
		public const string NativeClientNamespace     = "Sybase.Data.AseClient";
		public const string NativeProviderFactoryName = "Sybase.Data.AseClient";

		public const string ManagedAssemblyName    = "AdoNetCore.AseClient";
		public const string ManagedClientNamespace = "AdoNetCore.AseClient";

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
					lock (_nativeSyncRoot)
						if (_nativeInstance == null)
							_nativeInstance = CreateAdapter(NativeAssemblyName, NativeClientNamespace, NativeProviderFactoryName, true);

				return _nativeInstance;
			}
			else
			{
				if (_managedInstance == null)
					lock (_managedSyncRoot)
						if (_managedInstance == null)
							_managedInstance = CreateAdapter(ManagedAssemblyName, ManagedClientNamespace, null, false);

				return _managedInstance;
			}
		}

		private static SybaseProviderAdapter CreateAdapter(string assemblyName, string clientNamespace, string? dbFactoryName, bool supportsBulkCopy)
		{
			var assembly = Common.Tools.TryLoadAssembly(assemblyName, dbFactoryName);
			if (assembly == null)
				throw new InvalidOperationException($"Cannot load assembly {assemblyName}");

			var connectionType  = assembly.GetType($"{clientNamespace}.AseConnection" , true);
			var commandType     = assembly.GetType($"{clientNamespace}.AseCommand"    , true);
			var parameterType   = assembly.GetType($"{clientNamespace}.AseParameter"  , true);
			var dataReaderType  = assembly.GetType($"{clientNamespace}.AseDataReader" , true);
			var transactionType = assembly.GetType($"{clientNamespace}.AseTransaction", true);
			var dbType          = assembly.GetType($"{clientNamespace}.AseDbType"     , true);

			var typeMapper = new TypeMapper();
			typeMapper.RegisterTypeWrapper<AseConnection>(connectionType);
			typeMapper.RegisterTypeWrapper<AseParameter>(parameterType);
			typeMapper.RegisterTypeWrapper<AseDbType>(dbType);
			typeMapper.RegisterTypeWrapper<AseTransaction>(transactionType);

			BulkCopyAdapter? bulkCopy = null;

			if (supportsBulkCopy)
			{
				var bulkCopyType                        = assembly.GetType($"{clientNamespace}.AseBulkCopy"                       , true);
				var bulkCopyOptionsType                 = assembly.GetType($"{clientNamespace}.AseBulkCopyOptions"                , true);
				var bulkRowsCopiedEventHandlerType      = assembly.GetType($"{clientNamespace}.AseRowsCopiedEventHandler"         , true);
				var bulkCopyColumnMappingType           = assembly.GetType($"{clientNamespace}.AseBulkCopyColumnMapping"          , true);
				var bulkCopyColumnMappingCollectionType = assembly.GetType($"{clientNamespace}.AseBulkCopyColumnMappingCollection", true);
				var rowsCopiedEventArgsType             = assembly.GetType($"{clientNamespace}.AseRowsCopiedEventArgs"            , true);

				typeMapper.RegisterTypeWrapper<AseBulkCopy>(bulkCopyType);
				typeMapper.RegisterTypeWrapper<AseBulkCopyOptions>(bulkCopyOptionsType);
				typeMapper.RegisterTypeWrapper<AseRowsCopiedEventHandler>(bulkRowsCopiedEventHandlerType);
				typeMapper.RegisterTypeWrapper<AseBulkCopyColumnMapping>(bulkCopyColumnMappingType);
				typeMapper.RegisterTypeWrapper<AseBulkCopyColumnMappingCollection>(bulkCopyColumnMappingCollectionType);
				typeMapper.RegisterTypeWrapper<AseRowsCopiedEventArgs>(rowsCopiedEventArgsType);
				typeMapper.FinalizeMappings();

				bulkCopy = new BulkCopyAdapter(
					typeMapper.BuildWrappedFactory((IDbConnection connection, AseBulkCopyOptions options, IDbTransaction? transaction) => new AseBulkCopy((AseConnection)connection, options, (AseTransaction?)transaction)),
					typeMapper.BuildWrappedFactory((string source, string destination) => new AseBulkCopyColumnMapping(source, destination)));
			}
			else
				typeMapper.FinalizeMappings();

			var paramMapper   = typeMapper.Type<AseParameter>();
			var dbTypeBuilder = paramMapper.Member(p => p.AseDbType);

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
		private class AseParameter
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
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: Dispose
				(Expression<Action<AseBulkCopy>>)((AseBulkCopy this_) => ((IDisposable)this_).Dispose()),
				// [1]: WriteToServer
				(Expression<Action<AseBulkCopy, IDataReader>>)((AseBulkCopy this_, IDataReader dataReader) => this_.WriteToServer(dataReader)),
				// [2]: get NotifyAfter
				(Expression<Func<AseBulkCopy, int>>)((AseBulkCopy this_) => this_.NotifyAfter),
				// [3]: get BatchSize
				(Expression<Func<AseBulkCopy, int>>)((AseBulkCopy this_) => this_.BatchSize),
				// [4]: get BulkCopyTimeout
				(Expression<Func<AseBulkCopy, int>>)((AseBulkCopy this_) => this_.BulkCopyTimeout),
				// [5]: get DestinationTableName
				(Expression<Func<AseBulkCopy, string?>>)((AseBulkCopy this_) => this_.DestinationTableName),
				// [6]: get ColumnMappings
				(Expression<Func<AseBulkCopy, AseBulkCopyColumnMappingCollection>>)((AseBulkCopy this_) => this_.ColumnMappings),
				// [7]: set NotifyAfter
				PropertySetter((AseBulkCopy this_) => this_.NotifyAfter),
				// [8]: set BatchSize
				PropertySetter((AseBulkCopy this_) => this_.BatchSize),
				// [9]: set BulkCopyTimeout
				PropertySetter((AseBulkCopy this_) => this_.BulkCopyTimeout),
				// [10]: set DestinationTableName
				PropertySetter((AseBulkCopy this_) => this_.DestinationTableName),
			};

			private static string[] Events { get; }
				= new[]
			{
				nameof(AseRowsCopied)
			};

			public AseBulkCopy(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public AseBulkCopy(AseConnection connection, AseBulkCopyOptions options, AseTransaction? transaction) => throw new NotImplementedException();

			void IDisposable.Dispose() => ((Action<AseBulkCopy>)CompiledWrappers[0])(this);

			public void WriteToServer(IDataReader dataReader) => ((Action<AseBulkCopy, IDataReader>)CompiledWrappers[1])(this, dataReader);

			public int NotifyAfter
			{
				get => ((Func<AseBulkCopy, int>)CompiledWrappers[2])(this);
				set => ((Action<AseBulkCopy, int>)CompiledWrappers[7])(this, value);
			}

			public int BatchSize
			{
				get => ((Func<AseBulkCopy, int>)CompiledWrappers[3])(this);
				set => ((Action<AseBulkCopy, int>)CompiledWrappers[8])(this, value);
			}

			public int BulkCopyTimeout
			{
				get => ((Func<AseBulkCopy, int>)CompiledWrappers[4])(this);
				set => ((Action<AseBulkCopy, int>)CompiledWrappers[9])(this, value);
			}

			public string? DestinationTableName
			{
				get => ((Func<AseBulkCopy, string?>)CompiledWrappers[5])(this);
				set => ((Action<AseBulkCopy, string?>)CompiledWrappers[10])(this, value);
			}

			public AseBulkCopyColumnMappingCollection ColumnMappings => ((Func<AseBulkCopy, AseBulkCopyColumnMappingCollection>)CompiledWrappers[6])(this);

			private      AseRowsCopiedEventHandler? _AseRowsCopied;
			public event AseRowsCopiedEventHandler   AseRowsCopied
			{
				add    => _AseRowsCopied = (AseRowsCopiedEventHandler)Delegate.Combine(_AseRowsCopied, value);
				remove => _AseRowsCopied = (AseRowsCopiedEventHandler)Delegate.Remove (_AseRowsCopied, value);
			}
		}

		[Wrapper]
		public class AseRowsCopiedEventArgs : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: get RowCopied
				(Expression<Func<AseRowsCopiedEventArgs, int>>)((AseRowsCopiedEventArgs this_) => this_.RowCopied),
				// [1]: get Abort
				(Expression<Func<AseRowsCopiedEventArgs, bool>>)((AseRowsCopiedEventArgs this_) => this_.Abort),
				// [2]: set RowCopied
				PropertySetter((AseRowsCopiedEventArgs this_) => this_.RowCopied),
				// [3]: set Abort
				PropertySetter((AseRowsCopiedEventArgs this_) => this_.Abort),
			};

			public AseRowsCopiedEventArgs(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			// sic! Row, not Rows
			public int RowCopied
			{
				get => ((Func<AseRowsCopiedEventArgs, int>)CompiledWrappers[0])(this);
				set => ((Action<AseRowsCopiedEventArgs, int>)CompiledWrappers[2])(this, value);
			}

			public bool Abort
			{
				get => ((Func<AseRowsCopiedEventArgs, bool>)CompiledWrappers[1])(this);
				set => ((Action<AseRowsCopiedEventArgs, bool>)CompiledWrappers[3])(this, value);
			}
		}

		[Wrapper]
		public delegate void AseRowsCopiedEventHandler(object sender, AseRowsCopiedEventArgs e);

		[Wrapper]
		public class AseBulkCopyColumnMappingCollection : TypeWrapper
		{
			private static LambdaExpression[] Wrappers { get; }
				= new LambdaExpression[]
			{
				// [0]: Add
				(Expression<Func<AseBulkCopyColumnMappingCollection, AseBulkCopyColumnMapping, AseBulkCopyColumnMapping>>)((AseBulkCopyColumnMappingCollection this_, AseBulkCopyColumnMapping column) => this_.Add(column)),
			};

			public AseBulkCopyColumnMappingCollection(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public AseBulkCopyColumnMapping Add(AseBulkCopyColumnMapping bulkCopyColumnMapping) => ((Func<AseBulkCopyColumnMappingCollection, AseBulkCopyColumnMapping, AseBulkCopyColumnMapping>)CompiledWrappers[0])(this, bulkCopyColumnMapping);
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
			public AseBulkCopyColumnMapping(object instance) : base(instance, null)
			{
			}

			public AseBulkCopyColumnMapping(string source, string destination) => throw new NotImplementedException();
		}

		#endregion
	}
}
