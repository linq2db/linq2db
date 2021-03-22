using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.Ingres
{
	using Extensions;
	using Mapping;
	using SqlProvider;
	using SqlQuery;
	using Tools;

	class IngresSqlBuilder : BasicSqlBuilder
    {
	    private static IdnMapping  _idn = new IdnMapping();
		private readonly IngresDataProvider? _provider;

        public IngresSqlBuilder(
            IngresDataProvider? provider,
            MappingSchema mappingSchema,
            ISqlOptimizer sqlOptimizer,
            SqlProviderFlags sqlProviderFlags)
            : base(mappingSchema, sqlOptimizer, sqlProviderFlags)
        {
            _provider = provider;
        }

        public IngresSqlBuilder(
            MappingSchema mappingSchema,
            ISqlOptimizer sqlOptimizer,
            SqlProviderFlags sqlProviderFlags)
            : base(mappingSchema, sqlOptimizer, sqlProviderFlags)
        {
        }

        protected override ISqlBuilder CreateSqlBuilder()
        {
            return new IngresSqlBuilder(_provider, MappingSchema, SqlOptimizer, SqlProviderFlags);
        }
		
		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
        {
            switch (convertType)
            {
	            case ConvertType.NameToQueryFieldAlias:
	            case ConvertType.NameToQueryTableAlias:
		            return sb.Append(_idn.GetAscii(value).Replace("--", ""));
				case ConvertType.NameToQueryParameter:
                case ConvertType.NameToCommandParameter:
                case ConvertType.NameToSprocParameter:
                    return sb.Append('?');
            }

            return base.Convert(sb, value, convertType);
        }

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.Type.DataType)
			{
				case DataType.DateTime2:
				case DataType.DateTime:
				case DataType.DateTimeOffset: StringBuilder.Append("date"); break;
				case DataType.Boolean: StringBuilder.Append("byte(1)"); break;
				case DataType.Guid: StringBuilder.Append("byte(16)"); break;
				default: base.BuildDataTypeFromDataType(type, forCreateTable); break;
			}
		}

		protected override string? LimitFormat(SelectQuery selectQuery)
        {
            return "FETCH FIRST {0} ROWS ONLY";
        }

        protected override string OffsetFormat(SelectQuery selectQuery)
        {
            return "OFFSET {0}";
        }

        protected override bool OffsetFirst => true;
    }
}
