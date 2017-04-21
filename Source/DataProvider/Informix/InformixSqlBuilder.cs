using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LinqToDB.DataProvider.Informix
{
	using Common;
	using SqlQuery;
	using SqlProvider;

	class InformixSqlBuilder : BasicSqlBuilder
	{
		public InformixSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		public override int CommandCount(SelectQuery selectQuery)
		{
			return selectQuery.IsInsert && selectQuery.Insert.WithIdentity ? 2 : 1;
		}

		protected override void BuildCommand(int commandNumber)
		{
			StringBuilder.AppendLine("SELECT DBINFO('sqlca.sqlerrd1') FROM systables where tabid = 1");
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new InformixSqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override void BuildSql(int commandNumber, SelectQuery selectQuery, StringBuilder sb, int indent, bool skipAlias)
		{
			base.BuildSql(commandNumber, selectQuery, sb, indent, skipAlias);

			sb
				.Replace("NULL IS NOT NULL", "1=0")
				.Replace("NULL IS NULL",     "1=1");
		}

		protected override bool ParenthesizeJoin(List<SelectQuery.JoinedTable> joins)
		{
			return joins.Any(j => j.JoinType == SelectQuery.JoinType.Inner && j.Condition.Conditions.IsNullOrEmpty());
		}

		protected override void BuildSelectClause()
		{
			if (SelectQuery.From.Tables.Count == 0)
			{
				AppendIndent().Append("SELECT FIRST 1").AppendLine();
				BuildColumns();
				AppendIndent().Append("FROM SYSTABLES").AppendLine();
			}
			else if (SelectQuery.Select.IsDistinct)
			{
				AppendIndent();
				StringBuilder.Append("SELECT");
				BuildSkipFirst();
				StringBuilder.Append(" DISTINCT");
				StringBuilder.AppendLine();
				BuildColumns();
			}
			else
				base.BuildSelectClause();
		}

		protected override string FirstFormat { get { return "FIRST {0}"; } }
		protected override string SkipFormat  { get { return "SKIP {0}";  } }

		protected override void BuildLikePredicate(SelectQuery.Predicate.Like predicate)
		{
			if (predicate.IsNot)
				StringBuilder.Append("NOT ");

			var precedence = GetPrecedence(predicate);

			BuildExpression(precedence, predicate.Expr1);
			StringBuilder.Append(" LIKE ");
			BuildExpression(precedence, predicate.Expr2);

			if (predicate.Escape != null)
			{
				StringBuilder.Append(" ESCAPE ");
				BuildExpression(precedence, predicate.Escape);
			}
		}

		protected override void BuildFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(func);
		}

		protected override void BuildDataType(SqlDataType type, bool createDbType = false)
		{
			switch (type.DataType)
			{
				case DataType.DateTime  : StringBuilder.Append("datetime year to second");   break;
				case DataType.DateTime2 : StringBuilder.Append("datetime year to fraction"); break;
				case DataType.SByte      :
				case DataType.Byte       : StringBuilder.Append("SmallInt");      break;
				case DataType.SmallMoney : StringBuilder.Append("Decimal(10,4)"); break;
				default                  : base.BuildDataType(type);              break;
			}
		}

		protected override void BuildFromClause()
		{
			if (!SelectQuery.IsUpdate)
				base.BuildFromClause();
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

		protected override void BuildCreateTableFieldType(SqlField field)
		{
			if (field.IsIdentity)
			{
				if (field.DataType == DataType.Int32)
				{
					StringBuilder.Append("SERIAL");
					return;
				}

				if (field.DataType == DataType.Int64)
				{
					StringBuilder.Append("SERIAL8");
					return;
				}
			}

			base.BuildCreateTableFieldType(field);
		}

		protected override void BuildCreateTablePrimaryKey(string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("PRIMARY KEY (");
			StringBuilder.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
			StringBuilder.Append(")");
		}

#if !SILVERLIGHT

		protected override string GetProviderTypeName(IDbDataParameter parameter)
		{
			dynamic p = parameter;
			return p.IfxType.ToString();
		}

#endif
	}
}
