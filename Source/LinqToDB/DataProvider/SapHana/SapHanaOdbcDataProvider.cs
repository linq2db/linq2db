using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;

namespace LinqToDB.DataProvider.SapHana
{
	using Common;
	using Data;
	using Extensions;
	using Mapping;
	using SqlProvider;

	public class SapHanaOdbcDataProvider : DynamicDataProviderBase<OdbcProviderAdapter>
	{
		public SapHanaOdbcDataProvider() : base(ProviderName.SapHanaOdbc, MappingSchemaInstance, OdbcProviderAdapter.GetInstance())
		{
			SqlProviderFlags.IsParameterOrderDependent         = true;
			//Exception: Sap.Data.Hana.HanaException
			//Message: single-row query returns more than one row
			//when expression returns more than 1 row
			//mark this as supported, it's better to throw exception
			//then replace with left join, in which case returns incorrect data
			SqlProviderFlags.IsSubQueryColumnSupported         = true;
			SqlProviderFlags.IsDistinctOrderBySupported        = false;
			SqlProviderFlags.IsSubQueryTakeSupported           = false;
			SqlProviderFlags.IsInsertOrUpdateSupported         = false;
			SqlProviderFlags.AcceptsOuterExpressionInAggregate = false;

			_sqlOptimizer = new SapHanaSqlOptimizer(SqlProviderFlags);
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new SapHanaOdbcSchemaProvider();
		}

		public override DbCommand InitCommand(DataConnection dataConnection, DbCommand command, CommandType commandType, string commandText, DataParameter[]? parameters, bool withParameters)
		{
			if (commandType == CommandType.StoredProcedure)
			{
				commandText = $"{{ CALL {commandText} ({string.Join(",", (parameters ?? []).Select(x => "?"))}) }}";
				commandType = CommandType.Text;
			}

			return base.InitCommand(dataConnection, command, commandType, commandText, parameters, withParameters);
		}

		public override TableOptions SupportedTableOptions =>
			TableOptions.IsTemporary                |
			TableOptions.IsGlobalTemporaryStructure |
			TableOptions.IsLocalTemporaryStructure  |
			TableOptions.IsLocalTemporaryData;

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema, DataOptions dataOptions)
		{
			return new SapHanaOdbcSqlBuilder(this, mappingSchema, dataOptions, GetSqlOptimizer(dataOptions), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer(DataOptions dataOptions)
		{
			return _sqlOptimizer;
		}

		public override Type ConvertParameterType(Type type, DbDataType dataType)
		{
			if (type.IsNullable())
				type = type.ToUnderlying();

			switch (dataType.DataType)
			{
				case DataType.Boolean: if (type == typeof(bool))     return typeof(byte);     break;
				case DataType.Guid   : if (type == typeof(Guid))     return typeof(string);   break;
			}

			return base.ConvertParameterType(type, dataType);
		}

		public override IQueryParametersNormalizer GetQueryParameterNormalizer() => NoopQueryParametersNormalizer.Instance;

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
					value          = value == null ? null : string.Format(CultureInfo.InvariantCulture, "{0}", value);
					dataType       = dataType.WithDataType(DataType.Char);
					parameter.Size = 36;
					break;
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		public override IExecutionScope ExecuteScope(DataConnection dataConnection) => new InvariantCultureRegion(null);

		protected override void SetParameterType(DataConnection dataConnection, DbParameter parameter, DbDataType dataType)
		{
			if (parameter is BulkCopyReader.Parameter)
				return;

			switch (dataType.DataType)
			{
				case DataType.Boolean  : parameter.DbType = DbType.Byte;     return;
				case DataType.DateTime2: parameter.DbType = DbType.DateTime; return;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		private static readonly MappingSchema MappingSchemaInstance = new SapHanaMappingSchema.OdbcMappingSchema();

		public override bool? IsDBNullAllowed(DataOptions options, DbDataReader reader, int idx)
		{
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
	}
}
