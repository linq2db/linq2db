#if !NETSTANDARD2_0
using System;
using System.Data;

namespace LinqToDB.DataProvider.SapHana
{
	using System.Data.Common;
	using LinqToDB.Expressions;

	public class SapHanaProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _syncRoot = new object();
		private static SapHanaProviderAdapter? _instance;

#if NET45 || NET46
		public const string AssemblyName        = "Sap.Data.Hana.v4.5";
#endif
#if NETCOREAPP2_1
		public const string AssemblyName        = "Sap.Data.Hana.Core.v2.1";
#endif

		public const string ClientNamespace     = "Sap.Data.Hana";
		public const string ProviderFactoryName = "Sap.Data.Hana";

		private SapHanaProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,

			Action<IDbDataParameter, HanaDbType> dbTypeSetter,

			Func<IDbConnection, HanaBulkCopyOptions, IDbTransaction?, HanaBulkCopy> bulkCopyCreator,
			Func<int, string, HanaBulkCopyColumnMapping> bulkCopyColumnMappingCreator)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;

			SetDbType = dbTypeSetter;

			CreateBulkCopy              = bulkCopyCreator;
			CreateBulkCopyColumnMapping = bulkCopyColumnMappingCreator;
		}

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		public Action<IDbDataParameter, HanaDbType> SetDbType { get; }

		public Func<IDbConnection, HanaBulkCopyOptions, IDbTransaction?, HanaBulkCopy> CreateBulkCopy              { get; }
		public Func<int, string, HanaBulkCopyColumnMapping>                            CreateBulkCopyColumnMapping { get; }

		internal static SapHanaProviderAdapter GetInstance()
		{
			if (_instance == null)
				lock (_syncRoot)
					if (_instance == null)
					{
						var assembly = Common.Tools.TryLoadAssembly(AssemblyName, ProviderFactoryName);
						if (assembly == null)
							throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");

						var connectionType  = assembly.GetType($"{ClientNamespace}.HanaConnection" , true);
						var dataReaderType  = assembly.GetType($"{ClientNamespace}.HanaDataReader" , true);
						var parameterType   = assembly.GetType($"{ClientNamespace}.HanaParameter"  , true);
						var commandType     = assembly.GetType($"{ClientNamespace}.HanaCommand"    , true);
						var transactionType = assembly.GetType($"{ClientNamespace}.HanaTransaction", true);
						var dbType          = assembly.GetType($"{ClientNamespace}.HanaDbType"     , true);

						var bulkCopyType                    = assembly.GetType($"{ClientNamespace}.HanaBulkCopy"                       , true);
						var bulkCopyOptionsType             = assembly.GetType($"{ClientNamespace}.HanaBulkCopyOptions"                , true);
						var bulkCopyColumnMappingType       = assembly.GetType($"{ClientNamespace}.HanaBulkCopyColumnMapping"          , true);
						var rowsCopiedEventHandlerType      = assembly.GetType($"{ClientNamespace}.HanaRowsCopiedEventHandler"         , true);
						var rowsCopiedEventArgs             = assembly.GetType($"{ClientNamespace}.HanaRowsCopiedEventArgs"            , true);
						var bulkCopyColumnMappingCollection = assembly.GetType($"{ClientNamespace}.HanaBulkCopyColumnMappingCollection", true);

						var typeMapper = new TypeMapper(connectionType, parameterType, dbType, transactionType,
							bulkCopyType, bulkCopyOptionsType, rowsCopiedEventHandlerType, rowsCopiedEventArgs, bulkCopyColumnMappingCollection, bulkCopyColumnMappingType);

						typeMapper.RegisterWrapper<HanaConnection>();
						typeMapper.RegisterWrapper<HanaTransaction>();
						typeMapper.RegisterWrapper<HanaParameter>();
						typeMapper.RegisterWrapper<HanaDbType>();

						// bulk copy types
						typeMapper.RegisterWrapper<HanaBulkCopy>();
						typeMapper.RegisterWrapper<HanaRowsCopiedEventArgs>();
						typeMapper.RegisterWrapper<HanaRowsCopiedEventHandler>();
						typeMapper.RegisterWrapper<HanaBulkCopyColumnMappingCollection>();
						typeMapper.RegisterWrapper<HanaBulkCopyOptions>();
						typeMapper.RegisterWrapper<HanaBulkCopyColumnMapping>();

						var typeSetter = typeMapper.Type<HanaParameter>().Member(p => p.HanaDbType).BuildSetter<IDbDataParameter>();

						_instance = new SapHanaProviderAdapter(
							connectionType,
							dataReaderType,
							parameterType,
							commandType,
							transactionType,
							typeSetter,
							(IDbConnection connection, HanaBulkCopyOptions options, IDbTransaction? transaction) => typeMapper.CreateAndWrap(() => new HanaBulkCopy((HanaConnection)connection, (HanaBulkCopyOptions)options, (HanaTransaction?)transaction))!,
							(int source, string destination) => typeMapper.CreateAndWrap(() => new HanaBulkCopyColumnMapping(source, destination))!);
					}

			return _instance;
		}

		[Wrapper]
		public class HanaTransaction
		{
		}

		[Wrapper]
		public class HanaConnection
		{
		}

		[Wrapper]
		internal class HanaParameter
		{
			public HanaDbType HanaDbType { get; set; }
		}

		[Wrapper]
		public enum HanaDbType
		{
			AlphaNum     = 1,
			BigInt       = 2,
			Blob         = 3,
			Boolean      = 4,
			Clob         = 5,
			Date         = 6,
			Decimal      = 7,
			Double       = 8,
			Integer      = 9,
			NClob        = 10,
			NVarChar     = 11,
			Real         = 12,
			SecondDate   = 13,
			ShortText    = 14,
			SmallDecimal = 15,
			SmallInt     = 16,
			TableType    = 23,
			Text         = 17,
			Time         = 18,
			TimeStamp    = 19,
			TinyInt      = 20,
			VarBinary    = 21,
			VarChar      = 22
		}

		#region BulkCopy
		[Wrapper]
		public class HanaBulkCopy : TypeWrapper, IDisposable
		{
			public HanaBulkCopy(object instance, TypeMapper mapper) : base(instance, mapper)
			{
				this.WrapEvent<HanaBulkCopy, HanaRowsCopiedEventHandler>(nameof(HanaRowsCopied));
			}

			public HanaBulkCopy(HanaConnection connection, HanaBulkCopyOptions options, HanaTransaction? transaction) => throw new NotImplementedException();

			public void Dispose() => this.WrapAction(t => t.Dispose());

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

			public HanaBulkCopyColumnMappingCollection ColumnMappings
			{
				get => this.Wrap(t => t.ColumnMappings);
			}

			public event HanaRowsCopiedEventHandler HanaRowsCopied
			{
				add => Events.AddHandler(nameof(HanaRowsCopied), value);
				remove => Events.RemoveHandler(nameof(HanaRowsCopied), value);
			}
		}

		[Wrapper]
		public class HanaRowsCopiedEventArgs : TypeWrapper
		{
			public HanaRowsCopiedEventArgs(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public long RowsCopied
			{
				get => this.Wrap(t => t.RowsCopied);
			}

			public bool Abort
			{
				get => this.Wrap(t => t.Abort);
				set => this.SetPropValue(t => t.Abort, value);
			}
		}

		[Wrapper]
		public delegate void HanaRowsCopiedEventHandler(object sender, HanaRowsCopiedEventArgs e);

		[Wrapper]
		public class HanaBulkCopyColumnMappingCollection : TypeWrapper
		{
			public HanaBulkCopyColumnMappingCollection(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public HanaBulkCopyColumnMapping Add(HanaBulkCopyColumnMapping bulkCopyColumnMapping) => this.Wrap(t => t.Add(bulkCopyColumnMapping));
		}

		[Wrapper, Flags]
		public enum HanaBulkCopyOptions
		{
			Default                = 0,
			KeepIdentity           = 1,
			TableLock              = 2,
			UseInternalTransaction = 4
		}

		[Wrapper]
		public class HanaBulkCopyColumnMapping : TypeWrapper
		{
			public HanaBulkCopyColumnMapping(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public HanaBulkCopyColumnMapping(int source, string destination) => throw new NotImplementedException();
		}

		#endregion
	}
}
#endif
