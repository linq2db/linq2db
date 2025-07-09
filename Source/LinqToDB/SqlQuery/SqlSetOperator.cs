using System;

namespace LinqToDB.SqlQuery
{
	public class SqlSetOperator : QueryElement
	{
		public SqlSetOperator(SelectQuery selectQuery, SetOperation operation)
		{
			SelectQuery = selectQuery;
			Operation   = operation;
		}

		public SelectQuery  SelectQuery { get; private set; }
		public SetOperation Operation   { get; }

		public override QueryElementType ElementType => QueryElementType.SetOperator;

		public void Modify(SelectQuery selectQuery)
		{
			SelectQuery = selectQuery;
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.AppendLine(" ");

			switch (Operation)
			{
				case SetOperation.Union        : writer.Append("UNION");         break;
				case SetOperation.UnionAll     : writer.Append("UNION ALL");     break;
				case SetOperation.Except       : writer.Append("EXCEPT");        break;
				case SetOperation.ExceptAll    : writer.Append("EXCEPT ALL");    break;
				case SetOperation.Intersect    : writer.Append("INTERSECT");     break;
				case SetOperation.IntersectAll : writer.Append("INTERSECT ALL"); break;
			}

			writer.AppendLine();
			writer.AppendElement(SelectQuery);

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(ElementType);
			hash.Add(SelectQuery.GetElementHashCode());
			hash.Add(Operation);
			return hash.ToHashCode();
		}
	}
}
