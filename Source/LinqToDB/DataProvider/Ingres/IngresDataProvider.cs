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

        private static readonly MappingSchema MappingSchemaInstance = new IngresMappingSchema.ODBCMappingSchema();
    }
}
