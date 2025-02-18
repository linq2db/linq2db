using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.SapHana.Translation;
using LinqToDB.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;

namespace LinqToDB.DataProvider.SapHana
{
	sealed class SapHanaNativeDataProvider : SapHanaDataProvider { public SapHanaNativeDataProvider() : base(ProviderName.SapHanaNative, SapHanaProvider.Unmanaged) { } }
	sealed class SapHanaOdbcDataProvider   : SapHanaDataProvider { public SapHanaOdbcDataProvider  () : base(ProviderName.SapHanaOdbc  , SapHanaProvider.ODBC     ) { } }

	public abstract class SapHanaDataProvider : DynamicDataProviderBase<SapHanaProviderAdapter>
	{
		protected SapHanaDataProvider(string name, SapHanaProvider provider) : base(name, MappingSchemaInstance.Get(provider), SapHanaProviderAdapter.GetInstance(provider))
		{
			Provider = provider;

			SqlProviderFlags.IsParameterOrderDependent         = true;
			//Exception: Sap.Data.Hana.HanaException
			//Message: single-row query returns more than one row
			//when expression returns more than 1 row
			//mark this as supported, it's better to throw exception
			//instead of replace with left join, in which case returns incorrect data
			SqlProviderFlags.IsCorrelatedSubQueryTakeSupported = false;
			SqlProviderFlags.IsInsertOrUpdateSupported         = false;
			SqlProviderFlags.IsUpdateFromSupported             = false;
			SqlProviderFlags.IsApplyJoinSupported              = true;
			SqlProviderFlags.IsCrossApplyJoinSupportsCondition = true;
			SqlProviderFlags.IsOuterApplyJoinSupportsCondition = true;
			SqlProviderFlags.IsCommonTableExpressionsSupported = true;
			SqlProviderFlags.SupportsBooleanType               = false;

			_sqlOptimizer = new SapHanaSqlOptimizer(SqlProviderFlags);
		}

		private SapHanaProvider Provider { get; }

		protected override IMemberTranslator CreateMemberTranslator()
		{
			return new SapHanaMemberTranslator();
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return Provider == SapHanaProvider.Unmanaged ? new SapHanaSchemaProvider() : new SapHanaOdbcSchemaProvider();
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary                |
			TableOptions.IsGlobalTemporaryStructure |
			TableOptions.IsLocalTemporaryStructure  |
			TableOptions.IsLocalTemporaryData;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return Provider switch
			{
			    SapHanaProvider.Unmanaged =>
			        new SapHanaSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags),
				_ =>
				    new SapHanaOdbcSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags),
			};
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions) => _sqlOptimizer;

		public override Type ConvertParameterType(Type type, DbDataType dataType)
		{
			if (type.IsNullable())
				type = type.ToUnderlying();

#if NET6_0_OR_GREATER
			if (Provider == SapHanaProvider.Unmanaged && type == typeof(DateOnly))
				type = typeof(DateTime);
#endif

			switch (dataType.DataType)
			{
				case DataType.NChar:
				case DataType.Char:
					if (Provider == SapHanaProvider.Unmanaged)
						type = typeof (string);
					break;
				case DataType.Boolean: if (type == typeof(bool)) return typeof(byte);   break;
				case DataType.Guid   : if (type == typeof(Guid)) return typeof(string); break;
			}

			return base.ConvertParameterType(type, dataType);
		}

		public override void SetParameter(DataConnection dataConnection, DbParameter parameter, string name, DbDataType dataType, object? value)
		{
#if NET6_0_OR_GREATER
			if (value is DateOnly d)
				value = d.ToDateTime(TimeOnly.MinValue);
#endif
			switch (dataType.DataType)
			{
				case DataType.Boolean:
					dataType = dataType.WithDataType(DataType.Byte);
					if (value is bool b)
						value = b ? (byte)1 : (byte)0;
					break;
				case DataType.Guid:
					if (value != null)
						value = string.Format(CultureInfo.InvariantCulture, "{0}", value);
					dataType = dataType.WithDataType(DataType.Char);
					parameter.Size = 36;
					break;
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			if (parameter is BulkCopyReader.Parameter)
				return;

			if (Provider == SapHanaProvider.Unmanaged)
			{
				SapHanaProviderAdapter.HanaDbType? type = null;
				switch (dataType.DataType)
				{
					case DataType.Text : type = SapHanaProviderAdapter.HanaDbType.Text; break;
					case DataType.Image: type = SapHanaProviderAdapter.HanaDbType.Blob; break;
				}

				if (type != null)
				{
					var param = TryGetProviderParameter(dataConnection, parameter);
					if (param != null)
					{
						Adapter.SetDbType!(param, type.Value);
						return;
					}
				}

				switch (dataType.DataType)
				{
					// fallback types
					case DataType.Text : parameter.DbType  = DbType.String; return;
					case DataType.Image: parameter.DbType  = DbType.Binary; return;
					case DataType.NText : parameter.DbType = DbType.Xml;    return;
					case DataType.Binary: parameter.DbType = DbType.Binary; return;
				}
			}
			else
			{
				switch (dataType.DataType)
				{
					case DataType.Boolean  : parameter.DbType = DbType.Byte;     return;
					case DataType.DateTime2: parameter.DbType = DbType.DateTime; return;
				}
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		public override BulkCopyRowsCopied BulkCopy<T>(DataOptions options, ITable<T> table, IEnumerable<T> source)
		{
			if (Provider == SapHanaProvider.ODBC)
				return base.BulkCopy(options, table, source);

			return new SapHanaBulkCopy(this).BulkCopy(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(SapHanaOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (Provider == SapHanaProvider.ODBC)
				return base.BulkCopyAsync(options, table, source, cancellationToken);

			return new SapHanaBulkCopy(this).BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(SapHanaOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override Task<BulkCopyRowsCopied> BulkCopyAsync<T>(DataOptions options, ITable<T> table, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			if (Provider == SapHanaProvider.ODBC)
				return base.BulkCopyAsync(options, table, source, cancellationToken);

			return new SapHanaBulkCopy(this).BulkCopyAsync(
				options.BulkCopyOptions.BulkCopyType == BulkCopyType.Default ?
					options.FindOrDefault(SapHanaOptions.Default).BulkCopyType :
					options.BulkCopyOptions.BulkCopyType,
				table,
				options,
				source,
				cancellationToken);
		}

		public override bool? IsDBNullAllowed(DataOptions options, DbDataReader reader, int idx)
		{
			if (Provider == SapHanaProvider.Unmanaged)
			{
				// provider fails to set AllowDBNull for some results
				return true;
			}

			try
			{
				return base.IsDBNullAllowed(options, reader, idx);
			}
			catch (OverflowException)
			{
				// https://github.com/dotnet/runtime/issues/40654
				return true;
			}
		}

		public override IExecutionScope? ExecuteScope(DataConnection dataConnection) => Provider == SapHanaProvider.ODBC ? new InvariantCultureRegion(null) : null;

		public override DbCommand InitCommand(DataConnection dataConnection, DbCommand command, CommandType commandType, string commandText, DataParameter[]? parameters, bool withParameters)
		{
			if (Provider == SapHanaProvider.ODBC && commandType == CommandType.StoredProcedure)
			{
				commandText = $"{{ CALL {commandText} ({string.Join(",", (parameters ?? []).Select(x => "?"))}) }}";
				commandType = CommandType.Text;
			}

			return base.InitCommand(dataConnection, command, commandType, commandText, parameters, withParameters);
		}

		public override IQueryParametersNormalizer GetQueryParameterNormalizer() => Provider == SapHanaProvider.ODBC ? NoopQueryParametersNormalizer.Instance : base.GetQueryParameterNormalizer();

		static class MappingSchemaInstance
		{
			public static readonly MappingSchema NativeMappingSchema = new SapHanaMappingSchema.NativeMappingSchema();
			public static readonly MappingSchema OdbcMappingSchema   = new SapHanaMappingSchema.OdbcMappingSchema();

			public static MappingSchema Get(SapHanaProvider provider) => provider == SapHanaProvider.Unmanaged ? NativeMappingSchema : OdbcMappingSchema;
		}
	}
}
