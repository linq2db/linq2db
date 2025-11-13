using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Internal.Expressions.Types;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	/*
	 * Misc notes:
	 * - supported default isolation levels: Unspecified/Serializable (same behavior) === TxMode.SerializableRw
	 * 
	 * Optional/future features:
	 * - TODO: add provider-specific retry policy to support YdbException.IsTransientWhenIdempotent
	 * - TODO: add support for BeginTransaction(TxMode mode)
	 */
	public sealed class YdbProviderAdapter : IDynamicProviderAdapter
	{
		public  const string AssemblyName         = "Ydb.Sdk";
		public  const string ClientNamespace      = "Ydb.Sdk.Ado";

		private const string ProtosAssemblyName   = "Ydb.Protos";

		// custom reader methods
		internal const string GetBytes        = "GetBytes";
		internal const string GetSByte        = "GetSByte";
		internal const string GetUInt16       = "GetUInt16";
		internal const string GetUInt32       = "GetUInt32";
		internal const string GetUInt64       = "GetUInt64";
		internal const string GetInterval     = "GetInterval";

		YdbProviderAdapter()
		{
			var assembly = Common.Tools.TryLoadAssembly(AssemblyName, null)
				?? throw new InvalidOperationException($"Cannot load assembly {AssemblyName}.");
			var protosAsembly = Common.Tools.TryLoadAssembly(ProtosAssemblyName, null)
				?? throw new InvalidOperationException($"Cannot load assembly {ProtosAssemblyName}.");

			ConnectionType  = assembly.GetType($"{ClientNamespace}.YdbConnection",  true)!;
			CommandType     = assembly.GetType($"{ClientNamespace}.YdbCommand",     true)!;
			ParameterType   = assembly.GetType($"{ClientNamespace}.YdbParameter",   true)!;
			DataReaderType  = assembly.GetType($"{ClientNamespace}.YdbDataReader",  true)!;
			TransactionType = assembly.GetType($"{ClientNamespace}.YdbTransaction", true)!;

			var parameterType = assembly.GetType($"{ClientNamespace}.YdbParameter"           , true)!;
			var dbType        = assembly.GetType($"{ClientNamespace}.YdbType.YdbDbType"      , true)!;
			var bulkCopy      = assembly.GetType("Ydb.Sdk.Ado.BulkUpsert.IBulkUpsertImporter", true)!;

			var ydbValue     = assembly.GetType("Ydb.Sdk.Value.YdbValue", true)!;
			var protoValue   = protosAsembly.GetType("Ydb.Value", true)!;
			var protoType    = protosAsembly.GetType("Ydb.Type", true)!;
			var decimalType  = protosAsembly.GetType("Ydb.DecimalType", true)!;

			var typeMapper = new TypeMapper();

			typeMapper.RegisterTypeWrapper<YdbConnection>(ConnectionType);
			typeMapper.RegisterTypeWrapper<YdbValue>(ydbValue);
			typeMapper.RegisterTypeWrapper<ProtoValue>(protoValue);
			typeMapper.RegisterTypeWrapper<ProtoType>(protoType);
			typeMapper.RegisterTypeWrapper<DecimalType>(decimalType);
			typeMapper.RegisterTypeWrapper<IBulkUpsertImporter>(bulkCopy);
			typeMapper.RegisterTypeWrapper<YdbParameter>(parameterType);
			typeMapper.RegisterTypeWrapper<YdbDbType>(dbType);

			typeMapper.FinalizeMappings();

			_connectionFactory = typeMapper.BuildTypedFactory<string, YdbConnection, DbConnection>(connectionString => new YdbConnection(connectionString));
			ClearAllPools      = typeMapper.BuildFunc<Task>(typeMapper.MapLambda(() => YdbConnection.ClearAllPools()));
			ClearPool          = typeMapper.BuildFunc<DbConnection, Task>(typeMapper.MapLambda((YdbConnection connection) => YdbConnection.ClearPool(connection)));

			var paramMapper   = typeMapper.Type<YdbParameter>();
			var dbTypeBuilder = paramMapper.Member(p => p.YdbDbType);
			SetDbType         = dbTypeBuilder.BuildSetter<DbParameter>();
			GetDbType         = dbTypeBuilder.BuildGetter<DbParameter>();

			var makeDecimal = typeMapper.BuildFunc<DecimalValue, object>(
				typeMapper.MapLambda((DecimalValue value) => new YdbValue(
					new ProtoType()
					{
						DecimalType = new DecimalType()
						{
							Precision = value.Precision,
							Scale     = value.Scale
						}
					},
					new ProtoValue()
					{
						Low128  = value.Low,
						High128 = value.High
					})));

			MakeDecimalFromString = (value, p, s) => makeDecimal(MakeDecimalValue(value, p, s));

			var pConnection = Expression.Parameter(typeof(DbConnection));
			var pName       = Expression.Parameter(typeof(string));
			var pColumns    = Expression.Parameter(typeof(IReadOnlyList<string>));
			var pToken      = Expression.Parameter(typeof(CancellationToken));

			BeginBulkCopy = Expression.Lambda<Func<DbConnection, string, IReadOnlyList<string>, CancellationToken, IBulkUpsertImporter>>(
				typeMapper.MapExpression((DbConnection conn, string name, IReadOnlyList<string> columns, CancellationToken cancellationToken) => typeMapper.Wrap<IBulkUpsertImporter>(((YdbConnection)(object)conn).BeginBulkUpsertImport(name, columns, cancellationToken)), pConnection, pName, pColumns, pToken),
				pConnection, pName, pColumns, pToken)
				.CompileExpression();
		}

		record struct DecimalValue(ulong Low, ulong High, uint Precision, uint Scale);

		private static DecimalValue MakeDecimalValue(string value, int precision, int scale)
		{
			var valuePrecision = value.Count(char.IsDigit);
			var dot = value.IndexOf('.');
			var valueScale = dot == -1 ? 0 : value.Length - dot - 1;

			if (valueScale < scale)
			{
				if (dot == -1)
					value += ".";

				value += new string('0', scale - valueScale);
				valuePrecision += scale - valueScale;
			}

#if SUPPORTS_INT128
			var raw128 = Int128.Parse(value.Replace(".", ""), CultureInfo.InvariantCulture);

			var low64 = (ulong)(raw128 & 0xFFFFFFFFFFFFFFFF);
			var high64 = (ulong)(raw128 >> 64);
#else
			var raw128 = BigInteger.Parse(value.Replace(".", ""), CultureInfo.InvariantCulture);
			var bytes = raw128.ToByteArray();
			var raw = new byte[16];

			if (raw128 < BigInteger.Zero && bytes.Length < raw.Length)
			{
				for (var i = bytes.Length; i < raw.Length; i++)
					raw[i] = 0xFF;
			}

			Array.Copy(bytes, raw, bytes.Length > 16 ? 16 : bytes.Length);
			var low64 = BitConverter.ToUInt64(raw, 0);
			var high64 = BitConverter.ToUInt64(raw, 8);
#endif

			return new DecimalValue(low64, high64, (uint)precision, (uint)scale);
		}

		static readonly Lazy<YdbProviderAdapter> _lazy    = new (() => new ());
		internal static YdbProviderAdapter Instance => _lazy.Value;

		#region IDynamicProviderAdapter

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		readonly Func<string, DbConnection> _connectionFactory;
		public DbConnection CreateConnection(string connectionString) => _connectionFactory(connectionString);

		#endregion

		public Func<Task>               ClearAllPools { get; }
		public Func<DbConnection, Task> ClearPool     { get; }

		public Action<DbParameter, YdbDbType> SetDbType { get; }
		public Func  <DbParameter, YdbDbType> GetDbType { get; }

		internal Func<string, int, int, object> MakeDecimalFromString { get; }

		internal Func<DbConnection, string, IReadOnlyList<string>, CancellationToken, IBulkUpsertImporter> BeginBulkCopy { get; }

		#region wrappers
		[Wrapper]
		internal sealed class YdbConnection
		{
			public YdbConnection(string connectionString) => throw new NotImplementedException();

			public IBulkUpsertImporter BeginBulkUpsertImport(string name, IReadOnlyList<string> columns, CancellationToken cancellationToken) => throw new NotImplementedException();

			public static Task ClearAllPools() => throw new NotImplementedException();

			public static Task ClearPool(YdbConnection connection) => throw new NotImplementedException();
		}

		[Wrapper]
		internal sealed class IBulkUpsertImporter : TypeWrapper
		{
			[SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "Used from reflection")]
			private static LambdaExpression[] Wrappers { get; } =
{
				// [0]: AddRowAsync
				(Expression<Func<IBulkUpsertImporter, object?[], ValueTask>>)((this_, row) => this_.AddRowAsync(row)),
				// [1]: FlushAsync
				(Expression<Func<IBulkUpsertImporter, ValueTask>>)(this_ => this_.FlushAsync()),
			};

			public IBulkUpsertImporter(object instance, Delegate[] wrappers) : base(instance, wrappers)
			{
			}

			public ValueTask AddRowAsync(object?[] row) => ((Func<IBulkUpsertImporter, object?[], ValueTask>)CompiledWrappers[0])(this, row);

			public ValueTask FlushAsync() => ((Func<IBulkUpsertImporter, ValueTask>)CompiledWrappers[1])(this);
		}

		[Wrapper]
		internal sealed class YdbValue
		{
			// access internal .ctor
			[WrappedBindingFlags(BindingFlags.Instance | BindingFlags.NonPublic)]
			public YdbValue(ProtoType type, ProtoValue value) => throw new NotImplementedException();
		}

		[Wrapper("Value")]
		internal sealed class ProtoValue
		{
			public ProtoValue() => throw new NotImplementedException();

			public ulong High128
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			public ulong Low128
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}
		}

		[Wrapper("Type")]
		internal sealed class ProtoType
		{
			public ProtoType() => throw new NotImplementedException();

			public DecimalType DecimalType
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}
		}

		[Wrapper]
		internal sealed class DecimalType
		{
			public DecimalType() => throw new NotImplementedException();

			public uint Scale
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			public uint Precision
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}
		}

		[Wrapper]
		private sealed class YdbParameter
		{
			public YdbDbType YdbDbType { get; set; }
		}

		[Wrapper]
		public enum YdbDbType
		{
			Unspecified  = 1,
			Bool         = 2,
			Int8         = 3,
			Int16        = 4,
			Int32        = 5,
			Int64        = 6,
			Uint8        = 7,
			Uint16       = 8,
			Uint32       = 9,
			Uint64       = 10,
			Float        = 11,
			Double       = 12,
			Decimal      = 13,
			Bytes        = 14,
			Text         = 15,
			Yson         = 16,
			Json         = 17,
			JsonDocument = 18,
			Uuid         = 19,
			Date         = 20,
			Datetime     = 21,
			Timestamp    = 22,
			Interval     = 23,
			Date32       = 24,
			Datetime64   = 25,
			Timestamp64  = 26,
			Interval64   = 27,
			List         = int.MinValue

			// missing simple types:
			// DyNumber

			// missing simple non-column types:
			// TzDate
			// TzDateTime
			// TzTimestamp
		}

		#endregion
	}
}
