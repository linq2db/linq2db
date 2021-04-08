using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlDropTableStatement : SqlStatement
	{
		public SqlDropTableStatement(SqlTable table)
		{
			Table = table;
		}

		public SqlTable Table { get; }

		public override QueryType        QueryType    => QueryType.DropTable;
		public override QueryElementType ElementType  => QueryElementType.DropTableStatement;
		public override bool             IsParameterDependent { get => false; set {} }
		public override SelectQuery?     SelectQuery          { get => null;  set {} }

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			sb.Append("DROP TABLE ");

			((IQueryElement?)Table)?.ToString(sb, dic);

			sb.AppendLine();

			return sb;
		}

		public override ISqlExpression? Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
		{
			((ISqlExpressionWalkable?)Table)?.Walk(options, func);

			return null;
		}

		public override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			var clone = new SqlDropTableStatement((SqlTable)Table.Clone(objectTree, doClone));

			if (Tag != null)
				clone.Tag = (SqlComment)Tag.Clone(objectTree, doClone);

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
