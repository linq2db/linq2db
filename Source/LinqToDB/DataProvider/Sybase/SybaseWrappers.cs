using System;
using System.Data;

namespace LinqToDB.DataProvider.Sybase
{
	using System.Data.Common;
	using System.Reflection;
	using LinqToDB.Expressions;

	internal static class SybaseWrappers
	{
		public const string NativeAssemblyName  = "Sybase.AdoNet45.AseClient";
		public const string ManagedAssemblyName = "AdoNetCore.AseClient";

		private static object _nativeSyncRoot   = new object();
		private static object _managedSyncRoot  = new object();

		private static ISybaseWrapper? _nativeWrapper;
		private static ISybaseWrapper? _managedWrapper;

		internal interface ISybaseWrapper
		{
			Type ParameterType     { get; }
			Type DataReaderType    { get; }
			Type ConnectionType    { get; }
			Type TransactionType   { get; }

			Action<IDbDataParameter, AseDbType> TypeSetter { get; }
			Func<IDbDataParameter, AseDbType>   TypeGetter { get; }

			IBulkCopyWrapper? BulkCopy { get; }
		}

		internal interface IBulkCopyWrapper
		{
			AseBulkCopy                CreateBulkCopy             (IDbConnection connection, AseBulkCopyOptions options, IDbTransaction? transaction);
			AseBulkCopyColumnMapping   CreateBulkCopyColumnMapping(string source, string destination);
		}

		class BulkCopyWrapper : IBulkCopyWrapper
		{
			private readonly TypeMapper _typeMapper;

			internal BulkCopyWrapper(TypeMapper typeMapper)
			{
				_typeMapper = typeMapper;
			}

			AseBulkCopy IBulkCopyWrapper.CreateBulkCopy(IDbConnection connection, AseBulkCopyOptions options, IDbTransaction? transaction)
				=> _typeMapper!.CreateAndWrap(() => new AseBulkCopy((AseConnection)connection, options, (AseTransaction?)transaction))!;
			AseBulkCopyColumnMapping IBulkCopyWrapper.CreateBulkCopyColumnMapping(string source, string destination)
				=> _typeMapper!.CreateAndWrap(() => new AseBulkCopyColumnMapping(source, destination))!;
		}

		class SybaseWrapper : ISybaseWrapper
		{
			private readonly Type _connectionType;
			private readonly Type _transactionTypeType;
			private readonly Type _dataReaderType;
			private readonly Type _parameterType;

			private readonly Action<IDbDataParameter, AseDbType> _typeSetter;
			private readonly Func<IDbDataParameter, AseDbType>   _typeGetter;

			private readonly IBulkCopyWrapper? _bulkCopy;

			SybaseWrapper(
				Type connectionType,
				Type parameterType,
				Type dataReaderType,
				Type transactionTypeType,
				Action<IDbDataParameter, AseDbType> typeSetter,
				Func<IDbDataParameter, AseDbType>   typeGetter,
				IBulkCopyWrapper? bulkCopy)
			{
				_connectionType      = connectionType;
				_dataReaderType      = dataReaderType;
				_transactionTypeType = transactionTypeType;
				_parameterType       = parameterType;
				_typeSetter          = typeSetter;
				_typeGetter          = typeGetter;
				_bulkCopy            = bulkCopy;

			}

			IBulkCopyWrapper? ISybaseWrapper.BulkCopy => _bulkCopy;

			Type ISybaseWrapper.ConnectionType    => _connectionType;
			Type ISybaseWrapper.TransactionType   => _transactionTypeType;
			Type ISybaseWrapper.DataReaderType    => _dataReaderType;
			Type ISybaseWrapper.ParameterType     => _parameterType;

			Action<IDbDataParameter, AseDbType> ISybaseWrapper.TypeSetter => _typeSetter;
			Func<IDbDataParameter, AseDbType> ISybaseWrapper.TypeGetter   => _typeGetter;

			internal static ISybaseWrapper Initialize(bool native)
			{
				var clientNamespace = native ? "Sybase.Data.AseClient" : "AdoNetCore.AseClient";

				Assembly assembly;
#if !NETSTANDARD2_0 && !NETCOREAPP2_1
				if (native)
				{
					assembly = Type.GetType($"{clientNamespace}.AseConnection, {NativeAssemblyName}", false)?.Assembly
							?? DbProviderFactories.GetFactory("Sybase.Data.AseClient").GetType().Assembly;
				}
				else
#endif
				{
					assembly = Type.GetType($"{clientNamespace}.AseConnection, {ManagedAssemblyName}", true).Assembly;
				}

				var connectionType  = assembly.GetType($"{clientNamespace}.AseConnection", true);
				var parameterType   = assembly.GetType($"{clientNamespace}.AseParameter", true);
				var dataReaderType  = assembly.GetType($"{clientNamespace}.AseDataReader", true);
				var transactionType = assembly.GetType($"{clientNamespace}.AseTransaction", true);
				var dbType          = assembly.GetType($"{clientNamespace}.AseDbType", true);

				IBulkCopyWrapper? bulkCopy = null;
				TypeMapper typeMapper;
				if (native)
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

					bulkCopy = new BulkCopyWrapper(typeMapper);
				}
				else
					typeMapper = new TypeMapper(connectionType, parameterType, transactionType, dbType);

				typeMapper.RegisterWrapper<AseConnection>();
				typeMapper.RegisterWrapper<AseParameter>();
				typeMapper.RegisterWrapper<AseDbType>();
				typeMapper.RegisterWrapper<AseTransaction>();

				if (native)
				{
					// bulk copy types
					typeMapper.RegisterWrapper<AseBulkCopy>();
					typeMapper.RegisterWrapper<AseBulkCopyOptions>();
					typeMapper.RegisterWrapper<AseRowsCopiedEventHandler>();
					typeMapper.RegisterWrapper<AseBulkCopyColumnMapping>();
					typeMapper.RegisterWrapper<AseBulkCopyColumnMappingCollection>();
					typeMapper.RegisterWrapper<AseRowsCopiedEventArgs>();
				}

				var paramMapper   = typeMapper.Type<AseParameter>();
				var dbTypeBuilder = paramMapper.Member(p => p.AseDbType);


				return new SybaseWrapper(
					connectionType,
					parameterType,
					dataReaderType,
					transactionType,
					dbTypeBuilder.BuildSetter<IDbDataParameter>(),
					dbTypeBuilder.BuildGetter<IDbDataParameter>(),
					bulkCopy);
			}
		}

		internal static ISybaseWrapper Initialize(SybaseDataProvider provider)
		{
			if (provider.Name == ProviderName.Sybase)
			{
				if (_nativeWrapper == null)
				{
					lock (_nativeSyncRoot)
					{
						if (_nativeWrapper == null)
						{
							_nativeWrapper = SybaseWrapper.Initialize(true);
						}
					}
				}

				return _nativeWrapper;
			}
			else
			{
				if (_managedWrapper == null)
				{
					lock (_managedSyncRoot)
					{
						if (_managedWrapper == null)
						{
							_managedWrapper = SybaseWrapper.Initialize(false);
						}
					}
				}

				return _managedWrapper;
			}
		}

		[Wrapper]
		internal class AseParameter
		{
			public AseDbType AseDbType { get; set; }
		}

		[Wrapper]
		internal enum AseDbType
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
		internal class AseConnection
		{
		}

		[Wrapper]
		internal class AseTransaction
		{
		}

		#region BulkCopy
		[Wrapper]
		internal class AseBulkCopy : TypeWrapper, IDisposable
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
		internal delegate void AseRowsCopiedEventHandler(object sender, AseRowsCopiedEventArgs e);

		[Wrapper]
		internal class AseBulkCopyColumnMappingCollection : TypeWrapper
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
		internal class AseBulkCopyColumnMapping : TypeWrapper
		{
			public AseBulkCopyColumnMapping(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public AseBulkCopyColumnMapping(int    source, int    destination) => throw new NotImplementedException();

			public AseBulkCopyColumnMapping(string source, string destination) => throw new NotImplementedException();
		}

		#endregion
	}
}
