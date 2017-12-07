using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlDeleteStatement : SqlStatement
	{
		public override QueryType        QueryType   => QueryType.Delete;
		public override QueryElementType ElementType => QueryElementType.DeleteStatement;

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			throw new NotImplementedException();
		}

		public override ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
		{
			throw new NotImplementedException();
		}

		public override ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			throw new NotImplementedException();
		}

		#region DeleteClause

		private SqlDeleteClause _delete;
		public  SqlDeleteClause  Delete
		{
			get => _delete ?? (_delete = new SqlDeleteClause());
			set => _delete = value;
		}

		#endregion
	}
}
