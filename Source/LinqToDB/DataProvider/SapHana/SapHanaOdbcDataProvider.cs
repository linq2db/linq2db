using System;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.SapHana
{
	using Common;
	using Data;
	using Extensions;
	using Mapping;
	using SqlProvider;

	public class SapHanaOdbcDataProvider : DynamicDataProviderBase
	{
		public SapHanaOdbcDataProvider()
			: this(ProviderName.SapHanaOdbc, new SapHanaMappingSchema())
		{
		}

		protected SapHanaOdbcDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			//supported flags
			SqlProviderFlags.IsParameterOrderDependent = true;

			//supported flags
			SqlProviderFlags.IsCountSubQuerySupported  = true;

			//Exception: Sap.Data.Hana.HanaException
			//Message: single-row query returns more than one row
			//when expression returns more than 1 row
			//mark this as supported, it's better to throw exception
			//then replace with left join, in which case returns incorrect data
			SqlProviderFlags.IsSubQueryColumnSupported  = true;
			SqlProviderFlags.IsTakeSupported            = true;
			SqlProviderFlags.IsDistinctOrderBySupported = false;

			//not supported flags
			SqlProviderFlags.IsSubQueryTakeSupported     = false;
			SqlProviderFlags.IsApplyJoinSupported        = false;
			SqlProviderFlags.IsInsertOrUpdateSupported   = false;

			_sqlOptimizer = new SapHanaSqlOptimizer(SqlProviderFlags);
		}

#if !NET45 && !NET46
		public string AssemblyName => "System.Data.Odbc";
#else
		public string AssemblyName => "System.Data";
#endif

		public override string ConnectionNamespace   => "System.Data.Odbc";
		protected override string ConnectionTypeName => $"{ConnectionNamespace}.OdbcConnection, {AssemblyName}";
		protected override string DataReaderTypeName => $"{ConnectionNamespace}.OdbcDataReader, {AssemblyName}";

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			// noop as we don't need any Odbc-specific API
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new SapHanaOdbcSchemaProvider();
		}

		public override void InitCommand(DataConnection dataConnection, CommandType commandType, string commandText, DataParameter[] parameters, bool withParameters)
		{
			if (commandType == CommandType.StoredProcedure)
			{
				commandText = $"{{ CALL {commandText} ({string.Join(",", parameters.Select(x => "?"))}) }}";
				commandType = CommandType.Text;
			}

			base.InitCommand(dataConnection, commandType, commandText, parameters, withParameters);
		}

		public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
		{
			return new SapHanaOdbcSqlBuilder(mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override Type ConvertParameterType(Type type, DbDataType dataType)
		{
			if (type.IsNullable())
				type = type.ToUnderlying();

			switch (dataType.DataType)
			{
				case DataType.Boolean: if (type == typeof(bool)) return typeof(byte);   break;
				case DataType.Guid   : if (type == typeof(Guid)) return typeof(string); break;
			}

			return base.ConvertParameterType(type, dataType);
		}

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object value)
		{
			switch (dataType.DataType)
			{
				case DataType.Boolean:
					dataType = dataType.WithDataType(DataType.Byte);
					if (value is bool)
						value = (bool)value ? (byte)1 : (byte)0;
					break;
				case DataType.Guid:
					if (value != null)
						value = value.ToString();
					dataType = dataType.WithDataType(DataType.Char);
					parameter.Size = 36;
					break;
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		public override IDisposable ExecuteScope(DataConnection dataConnection)
		{
			// shame!
			return new InvariantCultureRegion();
		}

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
		{
			if (parameter is BulkCopyReader.Parameter)
				return;

			switch (dataType.DataType)
			{
				case DataType.Boolean  : parameter.DbType = DbType.Byte;     return;
				case DataType.DateTime2: parameter.DbType = DbType.DateTime; break;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}
	}
}
