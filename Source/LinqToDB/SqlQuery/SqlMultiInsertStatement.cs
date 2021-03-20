using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlMultiInsertStatement : SqlStatement
	{
		public SqlTableLikeSource         Source { get; }
		public List<SqlConditionalInsert> Inserts { get; set; } = new ();
		public MultiInsertType            InsertType { get; set; }

		public SqlMultiInsertStatement(SqlTableLikeSource source)
		{ 
			Source = source;
		}

		public void Add(SqlSearchCondition? when, SqlInsertClause insert)
			=> Inserts.Add(new SqlConditionalInsert { When = when, Insert = insert });

		public override QueryType          QueryType   => QueryType.MultiInsert;
		public override QueryElementType   ElementType => QueryElementType.MultiInsertStatement;

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{			
			sb.AppendLine(InsertType == MultiInsertType.First ? "INSERT FIRST " : "INSERT ALL ");
			foreach (var insert in Inserts)
				((IQueryElement)insert.Insert).ToString(sb, dic);
			Source.ToString(sb, dic);
			return sb;
		}

		public override ISqlExpression? Walk(WalkOptions options, Func<ISqlExpression, ISqlExpression> func)
		{
			Source.Walk(options, func);
			foreach (var (when, insert) in Inserts)
			{
				((ISqlExpressionWalkable?)when) ?.Walk(options, func);
				((ISqlExpressionWalkable )insert).Walk(options, func);
			}
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
				yield return insert.Insert;
			
			yield return Source;
		}

		public override ISqlTableSource? GetTableSource(ISqlTableSource table)
		{
			if (Source == table)
				return Source;

			foreach (var insert in Inserts)
			{
				if (insert.Insert.Into == table)
					return table;
			}

			return null;
		}

		public override void WalkQueries(Func<SelectQuery, SelectQuery> func)
			=> Source.WalkQueries(func);
	}
}
