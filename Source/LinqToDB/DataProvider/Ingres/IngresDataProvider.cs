using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Extensions;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider.Ingres
{
	using Common;
	using Data;
	using LinqToDB.SchemaProvider;
	using Mapping;
	using SqlProvider;

	public class IngresDataProvider : DynamicDataProviderBase<IngresProviderAdapter>
    {
        public IngresDataProvider(string name)
            : this(name, MappingSchemaInstance)
        {
        }

        protected IngresDataProvider(string name, MappingSchema mappingSchema)
            : base(name, mappingSchema, IngresProviderAdapter.GetInstance())
        {
            SqlProviderFlags.AcceptsTakeAsParameter = false;
            SqlProviderFlags.IsSkipSupported = false;
            SqlProviderFlags.IsCountSubQuerySupported = false;
            SqlProviderFlags.IsInsertOrUpdateSupported = false;
            SqlProviderFlags.TakeHintsSupported = TakeHints.Percent;
            SqlProviderFlags.IsCrossJoinSupported = false;
            SqlProviderFlags.IsInnerJoinAsCrossSupported = false;
            SqlProviderFlags.IsDistinctOrderBySupported = false;
            SqlProviderFlags.IsDistinctSetOperationsSupported = false;
            SqlProviderFlags.IsParameterOrderDependent = true;
            SqlProviderFlags.IsUpdateFromSupported = false;
            SqlProviderFlags.DefaultMultiQueryIsolationLevel = IsolationLevel.Unspecified;

            _sqlOptimizer = new IngresSqlOptimzer(SqlProviderFlags);
        }

        public override TableOptions SupportedTableOptions => TableOptions.None;

        public override ISqlBuilder CreateSqlBuilder(MappingSchema mappingSchema)
        {
            return new IngresSqlBuilder(this, mappingSchema, GetSqlOptimizer(), SqlProviderFlags);
        }

		readonly ISqlOptimizer _sqlOptimizer;

        public override ISqlOptimizer GetSqlOptimizer()
        {
            return _sqlOptimizer;
        }

        public override ISchemaProvider GetSchemaProvider()
        {
            return new IngresSchemaProvider(this);
        }

		public override void SetParameter(DataConnection dataConnection, IDbDataParameter parameter, string name, DbDataType dataType, object? value)
		{
			switch (dataType.DataType)
			{
				case DataType.Boolean:
					dataType = dataType.WithDataType(DataType.Byte);
					if (value is bool boolValue)
						value = boolValue ? (byte)1 : (byte)0;
					break;
				case DataType.Guid:
					if (value is Guid guid) value = guid.ToByteArray();
					break;
			}

			base.SetParameter(dataConnection, parameter, name, dataType, value);
		}

		public override Type ConvertParameterType(Type type, DbDataType dataType)
		{
			if (type.IsNullable())
				type = type.ToUnderlying();

			switch (dataType.DataType)
			{
				case DataType.Boolean        : if (type == typeof(bool))           return typeof(byte);                  break;
				case DataType.Guid           : if (type == typeof(Guid))           return typeof(byte[]);                break;
			}

			return base.ConvertParameterType(type, dataType);
		}

		protected override void SetParameterType(DataConnection dataConnection, IDbDataParameter parameter, DbDataType dataType)
		{
			switch (dataType.DataType)
			{
				case DataType.Guid      : parameter.DbType = DbType.Binary;  return;
			}

			base.SetParameterType(dataConnection, parameter, dataType);
		}

		private static readonly MappingSchema MappingSchemaInstance = new IngresMappingSchema();
    }
}
