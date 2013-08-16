using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.SQLite
{
	using SqlQuery;
	using SqlProvider;

	class SQLiteSqlBuilder : BasicSqlBuilder
	{
		public SQLiteSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(sqlOptimizer, sqlProviderFlags)
		{
		}

		public override int CommandCount(SelectQuery selectQuery)
		{
			return selectQuery.IsInsert && selectQuery.Insert.WithIdentity ? 2 : 1;
		}

		protected override void BuildCommand(int commandNumber, StringBuilder sb)
		{
			sb.AppendLine("SELECT last_insert_rowid()");
		}

		protected override ISqlBuilder CreateSqlProvider()
		{
			return new SQLiteSqlBuilder(SqlOptimizer, SqlProviderFlags);
		}

		protected override string LimitFormat  { get { return "LIMIT {0}";  } }
		protected override string OffsetFormat { get { return "OFFSET {0}"; } }

		public override bool IsNestedJoinSupported { get { return false; } }

		protected override void BuildFromClause(StringBuilder sb)
		{
			if (!SelectQuery.IsUpdate)
				base.BuildFromClause(sb);
		}

		protected override void BuildValue(StringBuilder sb, object value)
		{
			if (value is Guid)
			{
				var s = ((Guid)value).ToString("N");

				sb
					.Append("Cast(x'")
					.Append(s.Substring( 6,  2))
					.Append(s.Substring( 4,  2))
					.Append(s.Substring( 2,  2))
					.Append(s.Substring( 0,  2))
					.Append(s.Substring(10,  2))
					.Append(s.Substring( 8,  2))
					.Append(s.Substring(14,  2))
					.Append(s.Substring(12,  2))
					.Append(s.Substring(16, 16))
					.Append("' as blob)");
			}
			else
				base.BuildValue(sb, value);
		}

		protected override void BuildDateTime(StringBuilder sb, object value)
		{
			sb
				.Append(string.Format("'{0:yyyy-MM-dd HH:mm:ss.fff}", value).TrimEnd('0'))
				.Append('\'');
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return "@" + value;

				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
					{
						var name = value.ToString();

						if (name.Length > 0 && name[0] == '[')
							return value;
					}

					return "[" + value + "]";

				case ConvertType.NameToDatabase:
				case ConvertType.NameToOwner:
				case ConvertType.NameToQueryTable:
					if (value != null)
					{
						var name = value.ToString();

						if (name.Length > 0 && name[0] == '[')
							return value;

						if (name.IndexOf('.') > 0)
							value = string.Join("].[", name.Split('.'));

						return "[" + value + "]";
					}

					break;

				case ConvertType.SprocParameterToName:
					{
						var name = (string)value;
						return name.Length > 0 && name[0] == '@'? name.Substring(1): name;
					}
			}

			return value;
		}

		protected override void BuildDataType(StringBuilder sb, SqlDataType type, bool createDbType = false)
		{
			switch (type.DataType)
			{
				case DataType.Int32 : sb.Append("INTEGER");      break;
				default             : base.BuildDataType(sb, type); break;
			}
		}

		protected override void BuildCreateTableIdentityAttribute2(StringBuilder sb, SqlField field)
		{
			sb.Append("PRIMARY KEY AUTOINCREMENT");
		}

		protected override void BuildCreateTablePrimaryKey(StringBuilder sb, string pkName, IEnumerable<string> fieldNames)
		{
			if (SelectQuery.CreateTable.Table.Fields.Values.Any(f => f.IsIdentity))
			{
				while (sb[sb.Length - 1] != ',')
					sb.Length--;
				sb.Length--;
			}
			else
			{
				sb.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY (");
				//sb.Append("PRIMARY KEY (");
				sb.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
				sb.Append(")");
			}
		}
	}
}
