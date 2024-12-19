using System.Diagnostics;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;

	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	abstract class SequenceContextBase : BuildContextBase
	{
		protected SequenceContextBase(IBuildContext? parent, IBuildContext[] sequences, LambdaExpression? lambda)
			: base(sequences[0].Builder, sequences[0].ElementType, sequences[0].SelectQuery)
		{
			Parent          = parent;
			Sequences       = sequences;
			Body            = lambda == null ? null : SequenceHelper.PrepareBody(lambda, sequences);
			Sequence.Parent = this;
		}

		protected SequenceContextBase(IBuildContext? parent, IBuildContext sequence, LambdaExpression? lambda)
			: this(parent, new[] { sequence }, lambda)
		{
		}

		public          IBuildContext[] Sequences     { get; set; }
		public          Expression?     Body          { get; set; }
		public          IBuildContext   Sequence      => Sequences[0];
		public override MappingSchema   MappingSchema => Sequence.MappingSchema;

		public override Expression? Expression => Body;

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			var newPath = SequenceHelper.CorrectExpression(path, this, Sequence);
			var result  = Builder.BuildExpression(Sequence, newPath);

			if (ExpressionEqualityComparer.Instance.Equals(newPath, result))
				return path;

			if (flags.IsTable())
				return result;

			result = SequenceHelper.CorrectExpression(result, Sequence, this);
			return result;
		}

		public override void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

			QueryRunner.SetRunQuery(query, mapper);
		}

		public override SqlStatement GetResultStatement()
		{
			return Sequence.GetResultStatement();
		}

		public override void SetAlias(string? alias)
		{
			if (SelectQuery.Select.Columns.Count == 1)
			{
				SelectQuery.Select.Columns[0].Alias = alias;
			}

			if (SelectQuery.From.Tables.Count > 0)
				SelectQuery.From.Tables[SelectQuery.From.Tables.Count - 1].Alias = alias;
		}
	}
}
