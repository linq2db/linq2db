using System;
using System.Collections.Generic;
using System.Data;

namespace LinqToDB.DataProvider.SapHana
{

    using Common;
	using Data;
	using Extensions;
	using Mapping;
    using SqlProvider;


    public class SapHanaDataProvider : DynamicDataProviderBase
	{
		public SapHanaDataProvider()
            : this(ProviderName.SapHana, new SapHanaMappingSchema())
		{
		}

		protected SapHanaDataProvider(string name, MappingSchema mappingSchema)
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

		    SqlProviderFlags.IsTakeSupported = true;


            //testing

            //not supported flags
            SqlProviderFlags.IsSubQueryTakeSupported = false;

		    SqlProviderFlags.IsApplyJoinSupported = false;

            SqlProviderFlags.IsInsertOrUpdateSupported = false;

			_sqlOptimizer = new SapHanaSqlOptimizer(SqlProviderFlags);
		}

		public    override string ConnectionNamespace { get { return "Sap.Data.Hana"; } }

	    protected override string ConnectionTypeName
	    {
	        get
	        {
	            return "{0}.{1}, {2}".Args(ConnectionNamespace, "HanaConnection", SapHanaTools.AssemblyName);
	        }
	    }

	    protected override string DataReaderTypeName
	    {
	        get
	        {
                return "{0}.{1}, {2}".Args(ConnectionNamespace, "HanaDataReader", SapHanaTools.AssemblyName);
	        }
	    }

        static Action<IDbDataParameter> _setSingle;
        static Action<IDbDataParameter> _setDouble;
        static Action<IDbDataParameter> _setText;
        static Action<IDbDataParameter> _setNText;
        static Action<IDbDataParameter> _setBlob;
        static Action<IDbDataParameter> _setVarBinary;



		protected override void OnConnectionTypeCreated(Type connectionType)
		{
            const String paramTypeName = "HanaParameter";
		    const String dataTypeName = "HanaDbType";
            _setSingle = GetSetParameter(connectionType, paramTypeName, dataTypeName, dataTypeName, "Real");
            _setDouble = GetSetParameter(connectionType, paramTypeName, dataTypeName, dataTypeName, "Double");
            _setText = GetSetParameter(connectionType, paramTypeName, dataTypeName, dataTypeName, "Text");
            _setNText = GetSetParameter(connectionType, paramTypeName, dataTypeName, dataTypeName, "NClob");
            _setBlob = GetSetParameter(connectionType, paramTypeName, dataTypeName, dataTypeName, "Blob");
            _setVarBinary = GetSetParameter(connectionType, paramTypeName, dataTypeName, dataTypeName, "VarBinary");

		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
            return new SapHanaSchemaProvider();
		}

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new SapHanaSqlBuilder(GetSqlOptimizer(), SqlProviderFlags);            
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
                case DataType.Boolean: if (type == typeof(bool)) return typeof(byte); break;
                case DataType.Guid: if (type == typeof(Guid)) return typeof(string); break;
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
		        case DataType.Boolean:parameter.DbType = DbType.Byte;break;

                case DataType.Single: _setSingle(parameter); break;
                case DataType.Double: _setDouble(parameter); break;
                case DataType.Text: _setText(parameter); break;
                case DataType.Image: _setBlob(parameter);break;
                case DataType.NText: _setNText(parameter); break;
                case DataType.Binary: 
                case DataType.VarBinary: _setVarBinary(parameter); break;
		    }
            base.SetParameterType(parameter, dataType);
		}

	    private SapHanaBulkCopy _bulkCopy;

        public override BulkCopyRowsCopied BulkCopy<T>(
            [JetBrains.Annotations.NotNull] DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
        {
            if (_bulkCopy == null)
                _bulkCopy = new SapHanaBulkCopy(this, GetConnectionType());

            return _bulkCopy.BulkCopy(
                options.BulkCopyType == BulkCopyType.Default ? SapHanaTools.DefaultBulkCopyType : options.BulkCopyType,
                dataConnection,
                options,
                source);
        }

	}
}
