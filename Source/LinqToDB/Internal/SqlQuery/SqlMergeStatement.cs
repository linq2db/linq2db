using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlMergeStatement : SqlStatementWithQueryBase
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
		public SqlTableSource                Target     { get; private  set; }
		public SqlTableLikeSource            Source     { get; internal set; } = null!;
		public SqlSearchCondition            On         { get; private  set; } = new();
		public List<SqlMergeOperationClause> Operations { get; private  set; } = new();
		public SqlOutputClause?              Output     { get; set; }

		public bool                          HasIdentityInsert => Operations.Any(o => o.OperationType == MergeOperationType.Insert && o.Items.Any(item => item.Column is SqlField sqlField && sqlField.IsIdentity));
		public override QueryType            QueryType         => QueryType.Merge;
		public override QueryElementType     ElementType       => QueryElementType.MergeStatement;

		public void Modify(SqlTableSource target, SqlTableLikeSource source, SqlSearchCondition on, SqlOutputClause? output)
		{
			Target = target;
			Source = source;
			On     = on;
			Output = output;
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.AppendElement(With)
				.Append("MERGE INTO ")
				.AppendElement(Target)
				.AppendLine()
				.Append("USING (")
				.AppendElement(Source)
				.AppendLine(")")
				.Append("ON ")
				.AppendElement(On)
				.AppendLine();

			foreach (var operation in Operations)
			{
				writer
					.AppendElement(operation)
					.AppendLine();
			}

			if (Output?.HasOutput == true)
				writer.AppendElement(Output);
			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(base.GetElementHashCode());
			hash.Add(Hint);
			hash.Add(Target.GetElementHashCode());
			hash.Add(Source.GetElementHashCode());
			hash.Add(On.GetElementHashCode());

			foreach (var operation in Operations)
				hash.Add(operation.GetElementHashCode());

			hash.Add(Output?.GetElementHashCode());

			return hash.ToHashCode();
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

		public override ISqlTableSource? GetTableSource(ISqlTableSource table, out bool noAlias)
		{
			noAlias = false;

			if (Target.Source == table)
				return Target;

			if (Source == table)
				return Source;

			return null;
		}
	}
}
