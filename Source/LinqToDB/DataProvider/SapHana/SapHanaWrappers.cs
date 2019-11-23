using System;
using System.Data;

namespace LinqToDB.DataProvider.SapHana
{
	using System.Data.Common;
	using LinqToDB.Expressions;

	internal static class SapHanaWrappers
	{
		private static readonly object _syncRoot = new object();
		private static TypeMapper? _typeMapper;

		internal static Type ConnectionType  = null!;
		internal static Type ParameterType   = null!;
		internal static Type DataReaderType  = null!;
		internal static Type TransactionType = null!;

		internal static Action<IDbDataParameter, HanaDbType> TypeSetter = null!;
		
		internal static HanaBulkCopy              NewHanaBulkCopy(IDbConnection connection, HanaBulkCopyOptions options, IDbTransaction? transaction) => _typeMapper!.CreateAndWrap(() => new HanaBulkCopy((HanaConnection)connection, (HanaBulkCopyOptions)options, (HanaTransaction?)transaction))!;
		internal static HanaBulkCopyColumnMapping NewHanaBulkCopyColumnMapping(int source, string destination) => _typeMapper!.CreateAndWrap(() => new HanaBulkCopyColumnMapping(source, destination))!;

		internal static void Initialize()
		{
			if (_typeMapper == null)
			{
				lock (_syncRoot)
				{
					if (_typeMapper == null)
					{
#if NET45 || NET46
						const string assemblyName    = "Sap.Data.Hana.v4.5";
#else
						const string assemblyName    = "Sap.Data.Hana.Core.v2.1";
#endif
						const string clientNamespace = "Sap.Data.Hana";

#if !NETSTANDARD2_0
						var assembly = Type.GetType($"{clientNamespace}.HanaConnection, {assemblyName}", false)?.Assembly
							?? DbProviderFactories.GetFactory("Sap.Data.Hana").GetType().Assembly;
#else
						var assembly = Type.GetType($"{clientNamespace}.HanaConnection, {assemblyName}", true).Assembly;
#endif

						ConnectionType  = assembly.GetType($"{clientNamespace}.HanaConnection", true);
						ParameterType   = assembly.GetType($"{clientNamespace}.HanaParameter", true);
						DataReaderType  = assembly.GetType($"{clientNamespace}.HanaDataReader", true);
						TransactionType = assembly.GetType($"{clientNamespace}.HanaTransaction", true);
						var dbType      = assembly.GetType($"{clientNamespace}.HanaDbType", true);

						var bulkCopyType                    = assembly.GetType($"{clientNamespace}.HanaBulkCopy", true);
						var bulkCopyOptionsType             = assembly.GetType($"{clientNamespace}.HanaBulkCopyOptions", true);
						var bulkCopyColumnMappingType       = assembly.GetType($"{clientNamespace}.HanaBulkCopyColumnMapping", true);
						var rowsCopiedEventHandlerType      = assembly.GetType($"{clientNamespace}.HanaRowsCopiedEventHandler", true);
						var rowsCopiedEventArgs             = assembly.GetType($"{clientNamespace}.HanaRowsCopiedEventArgs", true);
						var bulkCopyColumnMappingCollection = assembly.GetType($"{clientNamespace}.HanaBulkCopyColumnMappingCollection", true);

						var typeMapper = new TypeMapper(ConnectionType, ParameterType, dbType, TransactionType,
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

						var dbTypeBuilder = typeMapper.Type<HanaParameter>().Member(p => p.HanaDbType);
						TypeSetter        = dbTypeBuilder.BuildSetter<IDbDataParameter>();

						_typeMapper = typeMapper;
					}
				}
			}
		}

		[Wrapper]
		internal class HanaTransaction
		{
		}

		[Wrapper]
		internal class HanaConnection
		{
		}

		[Wrapper]
		internal class HanaParameter
		{
			public HanaDbType HanaDbType { get; set; }
		}

		[Wrapper]
		internal enum HanaDbType
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
		internal class HanaBulkCopy : TypeWrapper, IDisposable
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
				get => this.Wrap        (t => t.NotifyAfter);
				set => this.SetPropValue(t => t.NotifyAfter, value);
			}

			public int BatchSize
			{
				get => this.Wrap        (t => t.BatchSize);
				set => this.SetPropValue(t => t.BatchSize, value);
			}

			public int BulkCopyTimeout
			{
				get => this.Wrap        (t => t.BulkCopyTimeout);
				set => this.SetPropValue(t => t.BulkCopyTimeout, value);
			}

			public string? DestinationTableName
			{
				get => this.Wrap        (t => t.DestinationTableName);
				set => this.SetPropValue(t => t.DestinationTableName, value);
			}

			public HanaBulkCopyColumnMappingCollection ColumnMappings
			{
				get => this.Wrap(t => t.ColumnMappings);
			}

			public event HanaRowsCopiedEventHandler HanaRowsCopied
			{
				add => Events.AddHandler      (nameof(HanaRowsCopied), value);
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
				get => this.Wrap        (t => t.Abort);
				set => this.SetPropValue(t => t.Abort, value);
			}
		}

		[Wrapper]
		internal delegate void HanaRowsCopiedEventHandler(object sender, HanaRowsCopiedEventArgs e);

		[Wrapper]
		internal class HanaBulkCopyColumnMappingCollection : TypeWrapper
		{
			public HanaBulkCopyColumnMappingCollection(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public HanaBulkCopyColumnMapping Add(HanaBulkCopyColumnMapping bulkCopyColumnMapping) => this.Wrap(t => t.Add(bulkCopyColumnMapping));
		}

		[Wrapper, Flags]
		internal enum HanaBulkCopyOptions
		{
			Default                = 0,
			KeepIdentity           = 1,
			TableLock              = 2,
			UseInternalTransaction = 4
		}

		[Wrapper]
		internal class HanaBulkCopyColumnMapping : TypeWrapper
		{
			public HanaBulkCopyColumnMapping(object instance, TypeMapper mapper) : base(instance, mapper)
			{
			}

			public HanaBulkCopyColumnMapping(int source, string destination) => throw new NotImplementedException();
		}

		#endregion
	}
}
