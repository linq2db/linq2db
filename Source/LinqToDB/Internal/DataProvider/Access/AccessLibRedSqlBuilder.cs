using System.Text;

using LinqToDB.DataProvider;
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

		protected override bool IsCaseExpressionSupported => true;
		protected override bool IsCommentSupported        => true;

		#region Skip / Take Support

		// with a skip the row count goes to FETCH NEXT, not TOP
		protected override void BuildSkipFirst(SelectQuery selectQuery)
		{
			if (selectQuery.Select.SkipValue == null)
				base.BuildSkipFirst(selectQuery);
		}

		protected override string? LimitFormat(SelectQuery selectQuery)
		{
			return selectQuery.Select.SkipValue != null ? "FETCH NEXT {0} ROWS ONLY" : null;
		}

		protected override string OffsetFormat(SelectQuery selectQuery)
		{
			return "OFFSET {0} ROWS";
		}

		protected override bool OffsetFirst => true;

		#endregion

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
	}
}
