using System;
using System.Data;
using System.Data.Odbc;

namespace LinqToDB.DataProvider.SapHana
{
    using System.Linq;
    using Data;

    using Extensions;
	using Mapping;
	using SqlProvider;

	public class SapHanaOdbcDataProvider : DataProviderBase
	{
		public SapHanaOdbcDataProvider()
			: this(ProviderName.SapHana, new SapHanaMappingSchema())
		{
		}

		protected SapHanaOdbcDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			//supported flags
			SqlProviderFlags.IsCountSubQuerySupported = true;
			
			//Exception: Sap.Data.Hana.HanaException
			//Message: single-row query returns more than one row
			//when expression returns more than 1 row
			//mark this as supported, it's better to throw exception 
			//then replace with left join, in which case returns incorrect data
			SqlProviderFlags.IsSubQueryColumnSupported = true;
			SqlProviderFlags.IsTakeSupported           = true;

			//testing

			//not supported flags
			SqlProviderFlags.IsSubQueryTakeSupported   = false;
			SqlProviderFlags.IsApplyJoinSupported      = false;
			SqlProviderFlags.IsInsertOrUpdateSupported = false;

			_sqlOptimizer = new SapHanaSqlOptimizer(SqlProviderFlags);
		}

		public override string ConnectionNamespace { get { return typeof(OdbcConnection).Namespace; } }
		public override Type   DataReaderType      { get { return typeof(OdbcDataReader); } }

		public override bool IsCompatibleConnection(IDbConnection connection)
		{
			return typeof(OdbcConnection).IsSameOrParentOf(connection.GetType());
		}

		protected override IDbConnection CreateConnectionInternal(string connectionString)
		{
			return new OdbcConnection(connectionString);
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new SapHanaOdbcSchemaProvider();
		}

	    public override void PrepareCommandInfo(CommandInfo commandInfo)
	    {
	        if (commandInfo.CommandType == CommandType.StoredProcedure)
	        {
	            commandInfo.CommandText = String.Format("{{ CALL {0} ({1}) }}",
	                commandInfo.CommandText,
	                String.Join(",", commandInfo.Parameters.Select(x => "?")));
	            commandInfo.CommandType = CommandType.Text;
	        }
            base.PrepareCommandInfo(commandInfo);
	    }

	    public override ISqlBuilder CreateSqlBuilder()
		{
			return new SapHanaOdbcSqlBuilder(GetSqlOptimizer(), SqlProviderFlags, MappingSchema.ValueToSqlConverter);            
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override Type ConvertParameterType(Type type, DataType dataType)
		{
			if (type.IsNullable())
				type = type.ToUnderlying();

			switch (dataType)
			{
				case DataType.Boolean: if (type == typeof(bool)) return typeof(byte);   break;
				case DataType.Guid   : if (type == typeof(Guid)) return typeof(string); break;
			}

			return base.ConvertParameterType(type, dataType);
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.Boolean:
					dataType = DataType.Byte;
					if (value is bool)
						value = (bool)value ? (byte)1 : (byte)0;
					break;
				case DataType.Guid:
					if (value != null)
						value = value.ToString();
					dataType = DataType.Char;
					parameter.Size = 36;
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			if (parameter is BulkCopyReader.Parameter)
				return;
			switch (dataType)
			{
				case DataType.Boolean:
					parameter.DbType = DbType.Byte;
					return;
				case DataType.Date:
					((OdbcParameter)parameter).OdbcType = OdbcType.Date;
					return;
				case DataType.DateTime2: ((OdbcParameter)parameter).OdbcType = OdbcType.DateTime;
					return;
			}
			base.SetParameterType(parameter, dataType);
		}
	}
}
