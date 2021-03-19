using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlMultiInsertStatement : SqlStatement
	{
		public MultiInsertType InsertType { get; }
		public SqlTableLikeSource Source { get; }

		public SqlMultiInsertStatement(MultiInsertType type, SqlTableLikeSource source)
		{ 
			InsertType = type;
			Source = source;
		}

		public override QueryType          QueryType   => QueryType.MultiInsert;
		public override QueryElementType   ElementType => QueryElementType.MultiInsertStatement;

		#region WhenInsertClauses

		public List<SqlSearchCondition?> Whens { get; set; } = new ();

		public SqlSearchCondition AddWhen()
		{
			var when = new SqlSearchCondition();
			Whens.Add(when);
			return when;
		}

		public void AddElse() 
		{
			Whens.Add(null);
		}

		public List<SqlInsertClause> Inserts { get; set; } = new ();

		public SqlInsertClause AddInsert()
		{
			var insert = new SqlInsertClause();
			Inserts.Add(insert);
			return insert;
		}

		#endregion

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{			
			sb.AppendLine(InsertType == MultiInsertType.First ? "INSERT FIRST " : "INSERT ALL ");
			foreach (IQueryElement insert in Inserts)
				insert.ToString(sb, dic);
			Source.ToString(sb, dic);
			return sb;
		}

		public override ISqlExpression? Walk(WalkOptions options, Func<ISqlExpression, ISqlExpression> func)
		{
			Source.Walk(options, func);
			foreach (ISqlExpressionWalkable? when in Whens)
				when?.Walk(options, func);
			foreach (ISqlExpressionWalkable insert in Inserts)
				insert.Walk(options, func);
			return null;
		}

		public override bool IsParameterDependent 
		{ 
			get => Source.IsParameterDependent; 
			set => Source.IsParameterDependent = value; 
		}

		public override SelectQuery? SelectQuery 
		{ 
			get => null; 
			set => throw new InvalidOperationException();
		}
		
		public override ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
			=> throw new NotImplementedException();

		public override IEnumerable<IQueryElement> EnumClauses() 
		{
			foreach (var insert in Inserts)
				yield return insert;
			
			yield return Source;
		}

		public override ISqlTableSource? GetTableSource(ISqlTableSource table)
		{
			if (Source == table)
				return Source;

			foreach (var insert in Inserts)
			{
				if (insert.Into == table)
					return table;
			}

			return null;
		}

		public override void WalkQueries(Func<SelectQuery, SelectQuery> func)
			=> Source.WalkQueries(func);
	}
}
