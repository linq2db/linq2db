﻿using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;
	using LinqToDB.Expressions;

	sealed class AnchorContext : SequenceContextBase
	{
		public SqlAnchor.AnchorKindEnum AnchorKind { get; }

		public AnchorContext(IBuildContext? parent, IBuildContext sequence, SqlAnchor.AnchorKindEnum anchorKind) : base(parent, sequence, null)
		{
			AnchorKind = anchorKind;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Root))
			{
				return path;
			}

			if (!flags.HasFlag(ProjectFlags.SQL))
				return base.MakeExpression(path, flags);

			var correctedPath = SequenceHelper.CorrectExpression(path, this, Sequence);

			var converted = Builder.BuildSqlExpression(Sequence, correctedPath, flags);

			converted = converted.Transform(this, static (ctx, e) =>
			{
				if (e is SqlPlaceholderExpression placeholder)
				{
					if (placeholder.Sql is not SqlAnchor)
					{
						return placeholder.WithSql(new SqlAnchor(placeholder.Sql, ctx.AnchorKind));
					}
				}
				return e;
			});

			return converted;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new AnchorContext(Parent, context.CloneContext(Sequence), AnchorKind);
		}
	}

}
