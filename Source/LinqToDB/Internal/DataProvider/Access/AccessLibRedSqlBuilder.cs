using System.Collections.Generic;
using System.Text;

using LinqToDB.DataProvider;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Access
{
	public class AccessLibRedSqlBuilder : AccessSqlBuilderBase
	{
		public AccessLibRedSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		AccessLibRedSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new AccessLibRedSqlBuilder(this);
		}

		// LibRed's grammar has no qualified-name production, so unlike the Microsoft flavours it cannot take
		// the database component AccessSqlBuilderBase emits: every spelling gives "mismatched input '.'".
		// Same shape as SqlCeSqlBuilder - a file database addresses one database and never names it.
		public override StringBuilder BuildObjectName(
			StringBuilder sb,
			SqlObjectName name,
			ConvertType objectType = ConvertType.NameToQueryTable,
			bool escape = true,
			TableOptions tableOptions = TableOptions.NotSet,
			bool withoutSuffix = false
		)
		{
			return escape ? Convert(sb, name.Name, objectType) : sb.Append(name.Name);
		}

		// Access takes IDENTITY as a column attribute on top of the type; LibRed wants the COUNTER type
		// instead ("extraneous input 'IDENTITY' expecting {')', ','}"), which is the form Access.sql uses
		protected override void BuildCreateTableFieldType(SqlField field)
		{
			if (field.IsIdentity)
				StringBuilder.Append("COUNTER");
			else
				base.BuildCreateTableFieldType(field);
		}

		protected override void BuildCreateTableIdentityAttribute2(SqlField field)
		{
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			// LibRed's grammar has no CLUSTERED: "extraneous input 'CLUSTERED' expecting '('"
			AppendIndent();
			StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY (");
			StringBuilder.AppendJoinStrings(InlineComma, fieldNames);
			StringBuilder.Append(')');
		}
	}
}
