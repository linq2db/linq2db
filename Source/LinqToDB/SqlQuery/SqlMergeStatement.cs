using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlMergeStatement : SqlStatementWithQueryBase
	{
		private const string TargetAlias = "Target";

		public SqlMergeStatement(SqlTable target) : base(null)
		{
			Target = new SqlTableSource(target, TargetAlias);
		}

		internal SqlMergeStatement(
			SqlWithClause?                       with,
			string?                              hint,
			SqlTableSource                       target,
			SqlTableLikeSource                   source,
			SqlSearchCondition                   on,
			IEnumerable<SqlMergeOperationClause> operations)
			: base(null)
		{
			With = with;
			Hint = hint;
			Target = target;
			Source = source;
			On = on;

			foreach (var operation in operations)
				Operations.Add(operation);
		}

		public string?                       Hint       { get; internal set; }
		public SqlTableSource                Target     { get; }
		public SqlTableLikeSource            Source     { get; internal set; } = null!;
		public SqlSearchCondition            On         { get; }               = new();
		public List<SqlMergeOperationClause> Operations { get; }               = new();
		public SqlOutputClause?              Output     { get; set; }

		public bool                          HasIdentityInsert => Operations.Any(o => o.OperationType == MergeOperationType.Insert && o.Items.Any(item => item.Column is SqlField field && field.IsIdentity));
		public override QueryType            QueryType         => QueryType.Merge;
		public override QueryElementType     ElementType       => QueryElementType.MergeStatement;

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			if (With != null)
				With.ToString(sb, dic);

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

		public override ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			Target.Walk(options, context, func);
			Source.Walk(options, context, func);

			((ISqlExpressionWalkable)On).Walk(options, context, func);

			for (var i = 0; i < Operations.Count; i++)
				((ISqlExpressionWalkable)Operations[i]).Walk(options, context, func);

			return null;
		}


		public override bool IsParameterDependent
		{
			get => Source.IsParameterDependent;
			set => Source.IsParameterDependent = value;
		}

		[NotNull]
		public override SelectQuery? SelectQuery
		{
			get => base.SelectQuery;
			set => throw new InvalidOperationException();
		}

		public override ISqlTableSource? GetTableSource(ISqlTableSource table)
		{
			if (Target.Source == table)
				return Target;

			if (Source == table)
				return Source;

			return null;
		}

		public override void WalkQueries<TContext>(TContext context, Func<TContext, SelectQuery, SelectQuery> func)
		{
			Source.WalkQueries(context, func);
			With?.WalkQueries(context, func);
		}
	}
}
