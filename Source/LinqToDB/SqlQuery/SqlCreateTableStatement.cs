using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlCreateTableStatement : SqlStatement
	{
		public SqlTable       Table           { get; set; }
		public string         StatementHeader { get; set; }
		public string         StatementFooter { get; set; }
		public DefaulNullable DefaulNullable  { get; set; }

		public override QueryType QueryType          => QueryType.CreateTable;
		public override QueryElementType ElementType => QueryElementType.CreateTableStatement;

		public override bool               IsParameterDependent
		{
			get => false;
			set {}
		}
		
		private         List<SqlParameter> _parameters;
		public override List<SqlParameter> Parameters => _parameters ?? (_parameters = new List<SqlParameter>());
		
		public override SelectQuery SelectQuery { get => null; set {}}

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			sb.Append("CREATE TABLE ");

			((IQueryElement)Table)?.ToString(sb, dic);

			sb.AppendLine();

			return sb;
		}

		public override ISqlExpression Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			((ISqlExpressionWalkable)Table)?.Walk(skipColumns, func);

			return null;
		}

		public override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			var clone = new SqlCreateTableStatement();

			if (Table != null)
				clone.Table = (SqlTable)Table.Clone(objectTree, doClone);

			objectTree.Add(this, clone);

			return clone;
		}

		public override ISqlTableSource GetTableSource(ISqlTableSource table)
		{
			return null;
		}
	}
}
