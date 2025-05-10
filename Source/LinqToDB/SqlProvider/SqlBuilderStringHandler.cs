using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

using LinqToDB.SqlQuery;

namespace LinqToDB.SqlProvider
{
	public partial class BasicSqlBuilder
	{
		protected struct FormatIndent
		{
			public int Level;
		}

		protected struct FormatPrecedence
		{
			public int Precedence;
			public ISqlExpression Expr;
		}

		protected struct FormatIdent
		{
			public string? Schema;
			public string Name;
			public ConvertType Type;
		}

		[InterpolatedStringHandler]
		protected struct SqlBuilderStringHandler
		{
			private BasicSqlBuilder sql;
			private StringBuilder builder;

			// literalLength and formattedCount are unused, but part of InterpolatedStringHandler contract
			public SqlBuilderStringHandler(int literalLength, int formattedCount, BasicSqlBuilder sql)
			{
				this.sql = sql;
				builder = sql.StringBuilder;
			}

			public void AppendLiteral(string literal) => builder.Append(literal);

			// We don't want to format arbitrary types and values into our SQL query: only specific overloads can be used.
			// public void AppendFormatted<T>(T value) { }

			public void AppendFormatted(string value) => builder.Append(value);

			public void AppendFormatted(char value) => builder.Append(value);

			public void AppendFormatted(int? value)
			{
				// TODO: can we just call builder.Append(value) please?
				if (value != null) builder.Append(value.Value.ToString(CultureInfo.InvariantCulture));	
			}
			
			public void AppendFormatted(FormatIndent indent)
			{
				if (indent.Level > 0)
					builder.Append('\t', indent.Level);
			}

			public void AppendFormatted(FormatPrecedence value)
			{
				var expr = value.Expr;
				var wrap = Wrap(GetPrecedence(expr), value.Precedence);
				bool addAlias = false;

				if (wrap) builder.Append('(');
				sql.BuildExpression(expr, true, true, null, ref addAlias);
				if (wrap) builder.Append(')');
			}

			public void AppendFormatted(ISqlTableSource table)
				=> sql.BuildPhysicalTable(table, null);
			
			public void AppendFormatted(ISqlExpression expr)
			{
				bool addAlias = false;
				sql.BuildExpression(expr, true, true, null, ref addAlias);
			}

			public void AppendFormatted(SqlComment? comment)
			{
				if (comment != null) sql.BuildSqlComment(builder, comment);
			}

			public void AppendFormatted(FormatIdent ident)
			{
				if (ident.Schema is {} schema)
				{
					sql.Convert(builder, schema, ConvertType.NameToSchema);
					builder.Append('.');
				}

				sql.Convert(builder, ident.Name, ident.Type);
			}
		}
	}
}
