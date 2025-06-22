using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public class SqlInsertClause : IQueryElement
	{
		public SqlInsertClause()
		{
			Items        = new List<SqlSetExpression>();
		}

		public List<SqlSetExpression> Items        { get; set; }
		public SqlTable?              Into         { get; set; }
		public bool                   WithIdentity { get; set; }

		public void Modify(SqlTable? into)
		{
			Into  = into;
		}

		#region Overrides

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return this.ToDebugString();
			}

#endif

		#endregion

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		public QueryElementType ElementType => QueryElementType.InsertClause;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			writer
				.Append("INSERT ")
				.AppendElement(Into)
				.Append(" VALUES ")
				.AppendLine();

			using (writer.IndentScope())
			{
				for (var index = 0; index < Items.Count; index++)
				{
					var e = Items[index];
					writer.AppendElement(e);
					if (index < Items.Count - 1)
						writer.AppendLine();
				}
			}

			return writer;
		}

		public int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(Into);
			hash.Add(WithIdentity);
			foreach (var item in Items)
			{
				hash.Add(item.GetElementHashCode());
			}
			return hash.ToHashCode();

		}

		#endregion
	}
}
