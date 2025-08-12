using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Linq;
using System.Globalization;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.Internal.DataProvider.ClickHouse.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

namespace LinqToDB.Internal.DataProvider.ClickHouse
{
#pragma warning disable MA0048 // File name must match type name
	sealed class ClickHouseOctonicaDataProvider : ClickHouseDataProvider { public ClickHouseOctonicaDataProvider() : base(ProviderName.ClickHouseOctonica, ClickHouseProvider.Octonica        ) { } }
	sealed class ClickHouseDriverDataProvider   : ClickHouseDataProvider { public ClickHouseDriverDataProvider() : base(ProviderName.ClickHouseDriver, ClickHouseProvider.ClickHouseDriver) { } }
	sealed class ClickHouseMySqlDataProvider    : ClickHouseDataProvider { public ClickHouseMySqlDataProvider   () : base(ProviderName.ClickHouseMySql   , ClickHouseProvider.MySqlConnector  ) { } }
#pragma warning restore MA0048 // File name must match type name

	public abstract class ClickHouseDataProvider : DynamicDataProviderBase<ClickHouseProviderAdapter>
	{
		protected ClickHouseDataProvider(string name, ClickHouseProvider provider)
			: base(name, GetMappingSchema(provider), ClickHouseProviderAdapter.GetInstance(provider))
		{
			Provider = provider;

			// CH tuples could be used to emulate SQL ROW partially but they handle NULL differently:
			// 1. IsNull: IS NULL applied to tuple itself, not to elements
			// 2. Equality/Comparisons: partially supported - NULL values handled differently
			// 3. CompareToSelect: supported, but requires Equality, which lacks NULL support
			SqlProviderFlags.RowConstructorSupport             = RowFeature.Between | RowFeature.In;

			SqlProviderFlags.TakeHintsSupported                = TakeHints.WithTies;

			// we enable InsertOrUpdate deliberately here and then throw exception from SqlBuilder
			// as emulation doesn't work properly due to missing rowcount functionality
			SqlProviderFlags.IsInsertOrUpdateSupported         = true;

			SqlProviderFlags.IsUpdateFromSupported                     = false;
			SqlProviderFlags.IsCommonTableExpressionsSupported         = true;
			SqlProviderFlags.IsSubQueryOrderBySupported                = true;
			SqlProviderFlags.SupportedCorrelatedSubqueriesLevel        = 0;
			SqlProviderFlags.IsAllSetOperationsSupported               = true;
			SqlProviderFlags.IsNestedJoinsSupported                    = false;
			SqlProviderFlags.IsSupportedSimpleCorrelatedSubqueries     = true;
			SqlProviderFlags.SupportsPredicatesComparison              = true;

			// unconfigured flags
			// 1. ClickHouse doesn't support correlated subqueries at all so this flag's value doesn't make difference
			//SqlProviderFlags.AcceptsOuterExpressionInAggregate = false;
			// 2. not tested as we don't support parameters currently
			//SqlProviderFlags.AcceptsTakeAsParameter = true;

			if (Adapter.GetSByteReaderMethod          != null) SetProviderField<sbyte         >(Adapter.GetSByteReaderMethod,          Adapter.DataReaderType);
			if (Adapter.GetUInt16ReaderMethod         != null) SetProviderField<ushort        >(Adapter.GetUInt16ReaderMethod,         Adapter.DataReaderType);
			if (Adapter.GetUInt32ReaderMethod         != null) SetProviderField<uint          >(Adapter.GetUInt32ReaderMethod,         Adapter.DataReaderType);
			if (Adapter.GetUInt64ReaderMethod         != null) SetProviderField<ulong         >(Adapter.GetUInt64ReaderMethod,         Adapter.DataReaderType);
			if (Adapter.GetBigIntegerReaderMethod     != null) SetProviderField<BigInteger    >(Adapter.GetBigIntegerReaderMethod,     Adapter.DataReaderType);
			if (Adapter.GetIPAddressReaderMethod      != null) SetProviderField<IPAddress     >(Adapter.GetIPAddressReaderMethod,      Adapter.DataReaderType);
			if (Adapter.GetDateTimeOffsetReaderMethod != null) SetProviderField<DateTimeOffset>(Adapter.GetDateTimeOffsetReaderMethod, Adapter.DataReaderType);

#if SUPPORTS_DATEONLY
			if (Adapter.GetDateOnlyReaderMethod != null) SetProviderField<DateOnly>(Adapter.GetDateOnlyReaderMethod, Adapter.DataReaderType);
#endif

			if (Provider == ClickHouseProvider.Octonica)
			{
				// FixedString binary readers and string fallback for other target types
				// read as binary only for binary mappings
				SetProviderField<DbDataReader, byte[], byte[]>((rd, idx) => rd.GetFieldValue<byte[]>(idx));
				SetProviderField<DbDataReader, Binary, byte[]>((rd, idx) => new Binary(rd.GetFieldValue<byte[]>(idx)));
				// for other target types read as string for better compatibility with string-based conversions
				ReaderExpressions[new ReaderInfo { ProviderFieldType = typeof(byte[]) }] = (DbDataReader rd, int idx) => Encoding.UTF8.GetString(rd.GetFieldValue<byte[]>(idx));

				// String as binary data
				SetProviderField<DbDataReader, byte[], string>((rd, idx) => rd.GetFieldValue<byte[]>(idx));
				SetProviderField<DbDataReader, Binary, string>((rd, idx) => new Binary(rd.GetFieldValue<byte[]>(idx)));
			}

			if (Provider == ClickHouseProvider.MySqlConnector)
			{
				if (Adapter.GetMySqlDecimalReaderMethod != null)
				{
					var dataReaderParameter = Expression.Parameter(DataReaderType, "r");
					var indexParameter      = Expression.Parameter(typeof(int), "i");

					// rd.GetMySqlDecimal(i).ToString()
					var body = Expression.Call(
						Expression.Call(
							dataReaderParameter,
							Adapter.GetMySqlDecimalReaderMethod,
							[],
							indexParameter),
						"ToString",
						[]);

					ReaderExpressions[new ReaderInfo
					{
						ToType            = typeof(string),
						ProviderFieldType = typeof(decimal),
						DataReaderType    = Adapter.DataReaderType,
						DataTypeName      = "DECIMAL",
						FieldType         = typeof(decimal),
					}]                    = Expression.Lambda(body, dataReaderParameter, indexParameter);
				}
			}
		}

		protected override IMemberTranslator CreateMemberTranslator()
		{
			return new ClickHouseMemberTranslator();
		}

		public ClickHouseProvider Provider { get; }

		#region Overrides

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary               |
			TableOptions.IsLocalTemporaryStructure |
			TableOptions.IsLocalTemporaryData      |
			TableOptions.CreateIfNotExists         |
			TableOptions.DropIfExists;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return new ClickHouseSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions) => new ClickHouseSqlOptimizer(SqlProviderFlags, dataOptions);

		public override ISchemaProvider GetSchemaProvider() => new ClickHouseSchemaProvider();

		public override bool? IsDBNullAllowed(DataOptions options, DbDataReader reader, int idx)
		{
			// https://github.com/Octonica/ClickHouseClient/issues/55
			if (Provider == ClickHouseProvider.Octonica)
				return true;

			var st = reader.GetSchemaTable();
			if (st == null || st.Rows[idx].IsNull("AllowDBNull"))
				return true;

			// https://github.com/DarkWanderer/ClickHouse.Client/issues/128
			var value = st.Rows[idx]["AllowDBNull"];
			if (value is bool allowDbNull)
				return allowDbNull;
			if (value is "False")
				return false;

			return true;
		}

		public override IQueryParametersNormalizer GetQueryParameterNormalizer() => throw new NotImplementedException($"Parameters not supported by ClickHouse provider. Create issue if you hit this exception from LINQ query.");

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, in DbDataType dataType, object? value)
		{
			if (parameter is BulkCopyReader.Parameter)
			{
				// bulk copy for Octonica/Client providers requires value of specific .net type
				// for pre-configured type mappings with non-default types we perform conversion here
				value = (Provider, dataType.DataType, value) switch
				{
					// OCTONICA provider
					(ClickHouseProvider.Octonica, DataType.Byte, bool val)                                                                                                => (byte)(val ? 1 : 0),
					// use ticks to avoid exceptions due to Local kind
					(ClickHouseProvider.Octonica, DataType.DateTime or DataType.DateTime64/* or DataType.DateTime2*/, DateTime val)                                       => new DateTimeOffset(val.Ticks, default),
					(ClickHouseProvider.Octonica, DataType.VarChar or DataType.NVarChar, Guid val)                                                                        => val.ToString("D"),
					(ClickHouseProvider.Octonica, DataType.Char or DataType.NChar, Guid val)                                                                              => Encoding.UTF8.GetBytes(val.ToString("D")),
#if SUPPORTS_DATEONLY
					(ClickHouseProvider.Octonica, DataType.Date32 or DataType.Date, DateTime val)                                                                         => DateOnly.FromDateTime(val),
					(ClickHouseProvider.Octonica, DataType.Date32 or DataType.Date, DateTimeOffset val)                                                                   => DateOnly.FromDateTime(val.Date),
#else
					(ClickHouseProvider.Octonica, DataType.Date32 or DataType.Date, DateTimeOffset val)                                                                   => val.Date,
#endif
					// https://github.com/Octonica/ClickHouseClient/issues/28
					(ClickHouseProvider.Octonica, DataType.Decimal or DataType.Decimal32 or DataType.Decimal64 or DataType.Decimal128 or DataType.Decimal256, string val) => decimal.Parse(val, CultureInfo.InvariantCulture),
					(ClickHouseProvider.Octonica, DataType.IPv4, uint val)                                                                                                => new IPAddress(new byte[] { (byte)((val >> 24) & 0xFF), (byte)((val >> 16) & 0xFF), (byte)((val >> 8) & 0xFF), (byte)(val & 0xFF) }),
					(ClickHouseProvider.Octonica, DataType.IPv4 or DataType.IPv6, string val)                                                                             => IPAddress.Parse(val),
					(ClickHouseProvider.Octonica, DataType.IPv6, byte[] val)                                                                                              => new IPAddress(val),

					// CLIENT provider
#if SUPPORTS_DATEONLY
					(ClickHouseProvider.ClickHouseDriver, DataType.Date or  DataType.Date32, DateOnly val)      => val.ToDateTime(default),
#endif
					(ClickHouseProvider.ClickHouseDriver, DataType.Date or DataType.Date32, DateTimeOffset val) => val.Date,
					// https://github.com/DarkWanderer/ClickHouse.Client/issues/138
					(ClickHouseProvider.ClickHouseDriver, DataType.VarBinary or DataType.Binary, byte[] val)    => Encoding.UTF8.GetString(val),
					(ClickHouseProvider.ClickHouseDriver, DataType.IPv4, uint val)                              => new IPAddress(new byte[] { (byte)((val >> 24) & 0xFF), (byte)((val >> 16) & 0xFF), (byte)((val >> 8) & 0xFF), (byte)(val & 0xFF) }).ToString(),
					// https://github.com/DarkWanderer/ClickHouse.Client/issues/145
					(ClickHouseProvider.ClickHouseDriver, DataType.IPv6, IPAddress val)                         => val.AddressFamily == AddressFamily.InterNetworkV6 ? val : val.MapToIPv6(),
					// https://github.com/DarkWanderer/ClickHouse.Client/issues/145
					(ClickHouseProvider.ClickHouseDriver, DataType.IPv6, string val)                            => IPAddress.Parse(val).MapToIPv6(),
					(ClickHouseProvider.ClickHouseDriver, DataType.IPv6, byte[] val)                            => new IPAddress(val).MapToIPv6(),

					_ => value
				};
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

#endregion

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			return new ClickHouseBulkCopy(this).BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(ClickHouseOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			DataOptions options, ITable<T> table, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new ClickHouseBulkCopy(this).BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(ClickHouseOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			DataOptions options, ITable<T> table, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new ClickHouseBulkCopy(this).BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(ClickHouseOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		#endregion

		private static MappingSchema GetMappingSchema(ClickHouseProvider provider)
		{
			return provider switch
			{
				ClickHouseProvider.ClickHouseDriver => new ClickHouseMappingSchema.ClientMappingSchema  (),
				ClickHouseProvider.MySqlConnector   => new ClickHouseMappingSchema.MySqlMappingSchema   (),
				_                                   => new ClickHouseMappingSchema.OctonicaMappingSchema()
			};
		}
	}
}
