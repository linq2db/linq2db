using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlMergeStatement : SqlStatement
	{
		public override QueryType QueryType => QueryType.Merge;
		public override QueryElementType ElementType => QueryElementType.MergeStatement;

		private static string TargetAlias = "Target";

		public SqlMergeStatement(SqlTable target)
		{
			Target = new SqlTableSource(target, TargetAlias);
		}

		public SqlMergeStatement(SqlTable target, SelectQuery source)
			: this(target)
		{
			SourceQuery = source;
		}

		public override ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			throw new NotImplementedException();
		}

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			sb.Append("MERGE INTO ");

			((IQueryElement)Target).ToString(sb, dic);

			sb
				.AppendLine()
				.Append("USING (");

			if (SourceQuery != null)
				((IQueryElement)SourceQuery).ToString(sb, dic);
			else
				sb.Append("<TODO:List>");

			sb
				.AppendLine(")")
				.Append("ON ");

			((IQueryElement)On).ToString(sb, dic);

			sb.AppendLine();

			foreach (var operation in Operations)
			{
				((IQueryElement)operation).ToString(sb, dic);
				sb.AppendLine();
			}

			return sb;
		}

		public override ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
		{
			Target.Walk(skipColumns, func);
			SourceQuery?.Walk(skipColumns, func);

			((ISqlExpressionWalkable)On).Walk(skipColumns, func);

			for (var i = 0; i < Operations.Count; i++)
				((ISqlExpressionWalkable)Operations[i]).Walk(skipColumns, func);

			return null;
		}

		public string Hint { get; internal set; }

		public string SourceName { get; set; } = "Source";

		public IDictionary<string, SqlField> SourceFields { get; } = new Dictionary<string, SqlField>();

		public void RegisterSourceFieldMapping(SqlField field)
		{
			if (!SourceFields.ContainsKey(field.PhysicalName))
			{
				SourceFields.Add(field.PhysicalName, new SqlField(field));
			}
		}

		public SqlTableSource Target { get; }
		public SelectQuery SourceQuery { get; internal set; }

		public IEnumerable SourceEnumerable { get; internal set; }

		public SqlSearchCondition On { get; private set; } = new SqlSearchCondition();

		public IList<SqlMergeOperationClause> Operations { get; } = new List<SqlMergeOperationClause>();

		public override bool IsParameterDependent
		{
			get => SourceQuery?.IsParameterDependent ?? false;
			set => SourceQuery.IsParameterDependent = value;
		}

		public override SelectQuery SelectQuery
		{
			get => null;
			set => throw new InvalidOperationException();
		}

		public override ISqlTableSource GetTableSource(ISqlTableSource table)
		{
			if (Target.Source == table)
				return Target;

			return SourceQuery?.GetTableSource(table);
		}

		public override IEnumerable<IQueryElement> EnumClauses()
		{
			throw new NotImplementedException();
		}

		public override void WalkQueries(Func<SelectQuery, SelectQuery> func)
		{
			if (SourceQuery != null)
				SourceQuery = func(SourceQuery);
		}
	}
}
