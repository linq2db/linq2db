using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlCreateTableStatement : SqlStatement
	{
		public SqlCreateTableStatement(SqlTable sqlTable)
		{
			Table = sqlTable;
		}

		public SqlTable        Table           { get; private set; }
		public string?         StatementHeader { get; set; }
		public string?         StatementFooter { get; set; }
		public DefaultNullable DefaultNullable { get; set; }

		public override QueryType        QueryType   => QueryType.CreateTable;
		public override QueryElementType ElementType => QueryElementType.CreateTableStatement;

		public override bool             IsParameterDependent
		{
			get => false;
			set {}
		}

		public override SelectQuery? SelectQuery { get => null; set {}}

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			sb.Append("CREATE TABLE ");

			((IQueryElement?)Table)?.ToString(sb, dic);

			sb.AppendLine();

			return sb;
		}

		public override ISqlExpression? Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
		{
			Table = (SqlTable)((ISqlExpressionWalkable)Table).Walk(options, func)!;

			return null;
		}

		public override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			var clone = new SqlCreateTableStatement((SqlTable)Table.Clone(objectTree, doClone));

			objectTree.Add(this, clone);

			return clone;
		}

		public override ISqlTableSource? GetTableSource(ISqlTableSource table)
		{
			return null;
		}

		public override void WalkQueries(Func<SelectQuery, SelectQuery> func)
		{
			if (SelectQuery != null)
			{
				var newQuery = func(SelectQuery);
				if (!ReferenceEquals(newQuery, SelectQuery))
					SelectQuery = newQuery;
			}
		}
	}
}
