using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.DataProvider.Access;
using LinqToDB.Internal.DataProvider.Access.Translation;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

using OdbcType = LinqToDB.Internal.DataProvider.OdbcProviderAdapter.OdbcType;
using OleDbType = LinqToDB.Internal.DataProvider.OleDbProviderAdapter.OleDbType;

namespace LinqToDB.Internal.DataProvider.Access
{
#pragma warning disable MA0048 // File name must match type name
	sealed class AccessJetOleDbDataProvider() : AccessDataProvider(ProviderName.AccessJetOleDb, AccessVersion.Jet, AccessProvider.OleDb);
	sealed class AccessAceOleDbDataProvider() : AccessDataProvider(ProviderName.AccessAceOleDb, AccessVersion.Ace, AccessProvider.OleDb);
	sealed class AccessJetODBCDataProvider()  : AccessDataProvider(ProviderName.AccessJetOdbc , AccessVersion.Jet, AccessProvider.ODBC );
	sealed class AccessAceODBCDataProvider()  : AccessDataProvider(ProviderName.AccessAceOdbc , AccessVersion.Ace, AccessProvider.ODBC );
#pragma warning restore MA0048 // File name must match type name

	public abstract class AccessDataProvider : DynamicDataProviderBase<AccessProviderAdapter>
	{
		protected AccessDataProvider(string name, AccessVersion version, AccessProvider provider)
			: base(name, MappingSchemaInstance.Get(version, provider), AccessProviderAdapter.GetInstance(provider))
		{
			Version  = version;
			Provider = provider;

			SqlProviderFlags.AcceptsTakeAsParameter                   = false;
			SqlProviderFlags.IsSkipSupported                          = false;
			SqlProviderFlags.IsInsertOrUpdateSupported                = false;
			SqlProviderFlags.IsSubQuerySkipSupported                  = false;
			SqlProviderFlags.IsSupportsJoinWithoutCondition           = false;
			SqlProviderFlags.TakeHintsSupported                       = TakeHints.Percent;
			SqlProviderFlags.IsCrossJoinSupported                     = false;
			SqlProviderFlags.IsDistinctSetOperationsSupported         = false;
			// should be: provider == AccessProvider.ODBC
			// but OleDb provider has some issues with complex queries
			// see TestPositionedParameters test
			SqlProviderFlags.IsParameterOrderDependent                = true;
			SqlProviderFlags.IsUpdateFromSupported                    = false;
			SqlProviderFlags.IsWindowFunctionsSupported               = false;
			SqlProviderFlags.SupportedCorrelatedSubqueriesLevel       = 1;
			SqlProviderFlags.DefaultMultiQueryIsolationLevel          = IsolationLevel.Unspecified;
			SqlProviderFlags.IsOuterJoinSupportsInnerJoin             = false;
			SqlProviderFlags.IsMultiTablesSupportsJoins               = false;
			SqlProviderFlags.IsAccessBuggyLeftJoinConstantNullability = true;
			SqlProviderFlags.SupportsPredicatesComparison             = true;

			SqlProviderFlags.IsCountDistinctSupported                     = false;
			SqlProviderFlags.IsAggregationDistinctSupported               = false;

			if (provider == AccessProvider.OleDb)
			{
				SetCharField("DBTYPE_WCHAR", (r, i) => r.GetString(i).TrimEnd(' '));
				SetCharFieldToType<char>("DBTYPE_WCHAR", DataTools.GetCharExpression);
			}
			else
			{
				SetCharField("CHAR", (r, i) => r.GetString(i).TrimEnd(' '));
				SetCharFieldToType<char>("CHAR", DataTools.GetCharExpression);

				SetToType<DbDataReader, sbyte,  int>("INTEGER",    (r, i) => unchecked((sbyte)r.GetInt32(i)));
				SetToType<DbDataReader, uint,   int>("INTEGER",    (r, i) => unchecked((uint)r.GetInt32(i)));
				SetToType<DbDataReader, ulong,  int>("INTEGER",    (r, i) => unchecked((ulong)(uint)r.GetInt32(i)));
				SetToType<DbDataReader, ushort, short>("SMALLINT", (r, i) => unchecked((ushort)r.GetInt16(i)));
			}

			SetProviderField<DbDataReader, TimeSpan, DateTime>((r, i) => r.GetDateTime(i) - new DateTime(1899, 12, 30));

			_sqlOptimizer = new AccessSqlOptimizer(SqlProviderFlags);
		}

		private  AccessVersion  Version  { get; }
		internal AccessProvider Provider { get; }

		public override TableOptions SupportedTableOptions => TableOptions.None;

		protected override IMemberTranslator CreateMemberTranslator()
		{
			return Version == AccessVersion.Jet
				? new AccessJetMemberTranslator()
				: new AccessMemberTranslator();
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return Provider == AccessProvider.OleDb
				? new AccessOleDbSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags)
				: new AccessODBCSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions)
		{
			return _sqlOptimizer;
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return Provider == AccessProvider.OleDb
				? new AccessOleDbSchemaProvider(this)
				: new AccessODBCSchemaProvider();
		}

		public override IQueryParametersNormalizer GetQueryParameterNormalizer()
		{
			return Provider == AccessProvider.OleDb
				? base.GetQueryParameterNormalizer()
				: NoopQueryParametersNormalizer.Instance;
		}

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, in DbDataType dataType, object? value)
		{
#if SUPPORTS_DATEONLY
			if (value is DateOnly d)
				value = d.ToDateTime(TimeOnly.MinValue);
#endif

			if (Provider == AccessProvider.ODBC)
			{
				switch (dataType.DataType)
				{
					case DataType.SByte:
						if (value is sbyte sbyteVal)
							value = unchecked((byte)sbyteVal);
						break;
					case DataType.UInt16:
						if (value is ushort ushortVal)
							value = unchecked((short)ushortVal);
						break;
					case DataType.UInt32:
						if (value is uint uintVal)
							value = unchecked((int)uintVal);
						break;
					case DataType.Int64:
						if (value is long longValue)
							value = checked((int)longValue);
						break;
					case DataType.UInt64:
						if (value is ulong ulongValue)
							value = unchecked((int)checked((uint)ulongValue));
						break;
				}
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, in DbDataType dataType)
		{
			if (Provider == AccessProvider.OleDb)
			{
				OleDbType? type = null;
				switch (dataType.DataType)
				{
					case DataType.DateTime:
					case DataType.DateTime2: type = OleDbType.Date; break;
					case DataType.Text: type = OleDbType.LongVarChar; break;
					case DataType.NText: type = OleDbType.LongVarWChar; break;
				}

				if (type != null)
				{
					var param = TryGetProviderParameter(dataConnection, parameter);
					if (param != null)
					{
						Adapter.SetOleDbDbType!(param, type.Value);
						return;
					}
				}

				switch (dataType.DataType)
				{
					// "Data type mismatch in criteria expression" fix for culture-aware number decimal separator
					// unfortunately, regular fix using ExecuteScope=>InvariantCultureRegion
					// doesn't work for all situations
					case DataType.Decimal:
					case DataType.VarNumeric: parameter.DbType = DbType.AnsiString; return;
					case DataType.DateTime:
					case DataType.DateTime2: parameter.DbType = DbType.DateTime; return;
					case DataType.Text: parameter.DbType = DbType.AnsiString; return;
					case DataType.NText: parameter.DbType = DbType.String; return;
				}
			}
			else
			{
				// https://docs.microsoft.com/en-us/sql/odbc/microsoft/microsoft-access-data-types?view=sql-server-ver15
				// https://docs.microsoft.com/en-us/sql/odbc/microsoft/data-type-limitations?view=sql-server-ver15
				OdbcType? type = null;
				switch (dataType.DataType)
				{
					case DataType.Variant: type = OdbcType.Binary; break;
				}

				if (type != null)
				{
					var param = TryGetProviderParameter(dataConnection, parameter);
					if (param != null)
					{
						Adapter.SetOdbcDbType!(param, type.Value);
						return;
					}
				}

				switch (dataType.DataType)
				{
					case DataType.SByte: parameter.DbType = DbType.Byte; return;
					case DataType.UInt16: parameter.DbType = DbType.Int16; return;
					case DataType.UInt32:
					case DataType.UInt64:
					case DataType.Int64: parameter.DbType = DbType.Int32; return;
					case DataType.Money:
					case DataType.SmallMoney:
					case DataType.VarNumeric:
					case DataType.Decimal: parameter.DbType = DbType.AnsiString; return;
					// fallback
					case DataType.Variant: parameter.DbType = DbType.Binary; return;
				}
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		#region BulkCopy

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			return new AccessBulkCopy().BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(AccessOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new AccessBulkCopy().BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(AccessOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table,
			IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			return new AccessBulkCopy().BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(AccessOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		#endregion

		static class MappingSchemaInstance
		{
			public static readonly MappingSchema JetOleDbMappingSchema  = new AccessMappingSchema.JetOleDbMappingSchema();
			public static readonly MappingSchema JetOdbcDbMappingSchema = new AccessMappingSchema.JetOdbcDbMappingSchema();
			public static readonly MappingSchema AceOleDbMappingSchema  = new AccessMappingSchema.AceOleDbMappingSchema();
			public static readonly MappingSchema AceOdbcDbMappingSchema = new AccessMappingSchema.AceOdbcDbMappingSchema();

			public static MappingSchema Get(AccessVersion version, AccessProvider provider)
			{
				return (version, provider) switch
				{
					(AccessVersion.Jet, AccessProvider.OleDb) => JetOleDbMappingSchema,
					(AccessVersion.Ace, AccessProvider.OleDb) => AceOleDbMappingSchema,
					(AccessVersion.Jet, AccessProvider.ODBC)  => JetOdbcDbMappingSchema,
					(AccessVersion.Ace, AccessProvider.ODBC)  => AceOdbcDbMappingSchema,
					_                                         => throw new InvalidOperationException()
				};
			}
		}
	}
}
