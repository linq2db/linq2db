using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	sealed class AnchorContext : SequenceContextBase
	{
		public SqlAnchor.AnchorKindEnum AnchorKind { get; }

		public AnchorContext(IBuildContext? parent, IBuildContext sequence, SqlAnchor.AnchorKindEnum anchorKind) : base(parent, sequence, null)
		{
			AnchorKind = anchorKind;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (!flags.IsSqlOrExpression())
				return path;

			var correctedPath = SequenceHelper.CorrectExpression(path, this, Sequence);

			var converted = Builder.BuildSqlExpression(Sequence, correctedPath);

			converted = converted.Transform(this, static (ctx, e) =>
			{
				if (e is SqlPlaceholderExpression { Sql: not SqlAnchor } placeholder)
				{
					return placeholder.WithSql(new SqlAnchor(placeholder.Sql, ctx.AnchorKind));
				}

				return e;
			});

			var remapped = SequenceHelper.CorrectTrackingPath(Builder, converted, path);
			remapped = SequenceHelper.ReplacePlaceholdersPathByTrackingPath(remapped);

			return remapped;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new AnchorContext(Parent, context.CloneContext(Sequence), AnchorKind);
		}
	}
}
