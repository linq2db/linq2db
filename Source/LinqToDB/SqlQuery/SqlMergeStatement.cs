using System;
using System.Collections.Generic;
using System.Linq;
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

		public bool HasIdentityInsert => Operations.Any(o => o.OperationType == MergeOperationType.Insert && o.Items.Any(item => item.Column is SqlField field && field.IsIdentity));

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

			Source.ToString(sb, dic);

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

		public override ISqlExpression Walk(WalkOptions options, Func<ISqlExpression, ISqlExpression> func)
		{
			Target.Walk(options, func);
			Source.Walk(options, func);

			((ISqlExpressionWalkable)On).Walk(options, func);

			for (var i = 0; i < Operations.Count; i++)
				((ISqlExpressionWalkable)Operations[i]).Walk(options, func);

			return null;
		}

		public string Hint { get; internal set; }


		public SqlTableSource Target { get; }

		public SqlMergeSourceTable Source { get; internal set; }

		public void SetSourceQuery(SelectQuery source)
		{
		}

		public SqlSearchCondition On { get; private set; } = new SqlSearchCondition();

		public IList<SqlMergeOperationClause> Operations { get; } = new List<SqlMergeOperationClause>();

		public override bool IsParameterDependent
		{
			get => Source.IsParameterDependent;
			set => Source.IsParameterDependent = value;
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

			if (Source == table)
				return Source;

			return null;
		}

		public override IEnumerable<IQueryElement> EnumClauses()
		{
			yield return Target;
			yield return Source;
			yield return On;

			foreach (var operation in Operations)
			{
				yield return operation;
			}
		}

		public override void WalkQueries(Func<SelectQuery, SelectQuery> func)
		{
			Source.WalkQueries(func);
		}
	}
}
