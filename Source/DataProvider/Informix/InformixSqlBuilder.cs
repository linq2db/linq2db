using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.Informix
{
	using SqlQuery;
	using SqlProvider;

	public class InformixSqlBuilder : BasicSqlBuilder
	{
		public InformixSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(sqlOptimizer, sqlProviderFlags)
		{
		}

		public override int CommandCount(SelectQuery selectQuery)
		{
			return selectQuery.IsInsert && selectQuery.Insert.WithIdentity ? 2 : 1;
		}

		protected override void BuildCommand(int commandNumber, StringBuilder sb)
		{
			sb.AppendLine("SELECT DBINFO('sqlca.sqlerrd1') FROM systables where tabid = 1");
		}

		protected override ISqlBuilder CreateSqlProvider()
		{
			return new InformixSqlBuilder(SqlOptimizer, SqlProviderFlags);
		}

		public override void BuildSql(int commandNumber, SelectQuery selectQuery, StringBuilder sb, int indent, bool skipAlias)
		{
			base.BuildSql(commandNumber, selectQuery, sb, indent, skipAlias);

			sb
				.Replace("NULL IS NOT NULL", "1=0")
				.Replace("NULL IS NULL",     "1=1");
		}

		protected override void BuildSelectClause(StringBuilder sb)
		{
			if (SelectQuery.From.Tables.Count == 0)
			{
				AppendIndent(sb).Append("SELECT FIRST 1").AppendLine();
				BuildColumns(sb);
				AppendIndent(sb).Append("FROM SYSTABLES").AppendLine();
			}
			else
				base.BuildSelectClause(sb);
		}

		protected override string FirstFormat { get { return "FIRST {0}"; } }
		protected override string SkipFormat  { get { return "SKIP {0}";  } }

		protected override void BuildLikePredicate(StringBuilder sb, SelectQuery.Predicate.Like predicate)
		{
			if (predicate.IsNot)
				sb.Append("NOT ");

			var precedence = GetPrecedence(predicate);

			BuildExpression(sb, precedence, predicate.Expr1);
			sb.Append(" LIKE ");
			BuildExpression(sb, precedence, predicate.Expr2);

			if (predicate.Escape != null)
			{
				sb.Append(" ESCAPE ");
				BuildExpression(sb, precedence, predicate.Escape);
			}
		}

		protected override void BuildFunction(StringBuilder sb, SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(sb, func);
		}

		public virtual object ConvertBooleanValue(bool value)
		{
			return value ? 't' : 'f';
		}

		public override void BuildValue(StringBuilder sb, object value)
		{
			if (value is bool)
				sb.Append("'").Append(ConvertBooleanValue((bool)value)).Append("'");
			else
				base.BuildValue(sb, value);
		}

		protected override void BuildDataType(StringBuilder sb, SqlDataType type, bool createDbType = false)
		{
			switch (type.DataType)
			{
				case DataType.SByte      :
				case DataType.Byte       : sb.Append("SmallInt");        break;
				case DataType.SmallMoney : sb.Append("Decimal(10,4)");   break;
				default                  : base.BuildDataType(sb, type); break;
			}
		}

		protected override void BuildFromClause(StringBuilder sb)
		{
			if (!SelectQuery.IsUpdate)
				base.BuildFromClause(sb);
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter   : return "?";
				case ConvertType.NameToCommandParameter :
				case ConvertType.NameToSprocParameter   : return ":" + value;
				case ConvertType.SprocParameterToName   :
					if (value != null)
					{
						var str = value.ToString();
						return (str.Length > 0 && str[0] == ':')? str.Substring(1): str;
					}

					break;
			}

			return value;
		}

		protected override void BuildCreateTableFieldType(StringBuilder sb, SqlField field)
		{
			if (field.IsIdentity)
			{
				if (field.DataType == DataType.Int32)
				{
					sb.Append("SERIAL");
					return;
				}

				if (field.DataType == DataType.Int64)
				{
					sb.Append("SERIAL8");
					return;
				}
			}

			base.BuildCreateTableFieldType(sb, field);
		}

		protected override void BuildCreateTablePrimaryKey(StringBuilder sb, string pkName, IEnumerable<string> fieldNames)
		{
			sb.Append("PRIMARY KEY (");
			sb.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
			sb.Append(")");
		}
	}
}
