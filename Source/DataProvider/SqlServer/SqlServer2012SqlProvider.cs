using System;
using System.Text;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlBuilder;
	using SqlProvider;

	public class SqlServer2012SqlProvider : SqlServerSqlProvider
	{
		public SqlServer2012SqlProvider(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		protected override string LimitFormat         { get { return SqlQuery.Select.SkipValue != null ? "FETCH NEXT {0} ROWS ONLY" : null; } }
		protected override string OffsetFormat        { get { return "OFFSET {0} ROWS"; } }
		protected override bool   OffsetFirst         { get { return true;              } }
		protected override bool   BuildAlternativeSql { get { return false;             } }

		protected override ISqlProvider CreateSqlProvider()
		{
			return new SqlServer2012SqlProvider(SqlProviderFlags);
		}

		protected override void BuildSql(StringBuilder sb)
		{
			if (NeedSkip && SqlQuery.OrderBy.IsEmpty)
			{
				for (var i = 0; i < SqlQuery.Select.Columns.Count; i++)
					SqlQuery.OrderBy.ExprAsc(new SqlValue(i + 1));
			}

			base.BuildSql(sb);
		}

		protected override void BuildInsertOrUpdateQuery(StringBuilder sb)
		{
			BuildInsertOrUpdateQueryAsMerge(sb, null);
			sb.AppendLine(";");
		}

		public override string  Name
		{
			get { return ProviderName.SqlServer2012; }
		}
	}
}
