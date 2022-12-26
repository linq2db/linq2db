using System.Linq.Expressions;
using LinqToDB.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	sealed class AnchorContext : SequenceContextBase, IEquatable<AnchorContext>
	{
		public SqlAnchor.AnchorKindEnum AnchorKind { get; }

		public AnchorContext(IBuildContext? parent, IBuildContext sequence, SqlAnchor.AnchorKindEnum anchorKind) : base(parent, sequence, null)
		{
			AnchorKind = anchorKind;
		}

		public override Expression BuildExpression(Expression? expression, int level, bool         enforceServerSide)
		{
			throw new NotImplementedException();
		}

		public override SqlInfo[]  ConvertToSql(Expression?    expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		public override SqlInfo[]  ConvertToIndex(Expression?  expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			var converted = base.MakeExpression(path, flags);

			converted = converted.Transform(e =>
			{
				if (e is SqlPlaceholderExpression paceholder)
				{
					if (paceholder.Sql is not SqlAnchor)
					{
						return paceholder.WithSql(new SqlAnchor(paceholder.Sql, AnchorKind));
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

		public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			throw new NotImplementedException();
		}

		public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
		{
			return null;
		}

		public bool Equals(AnchorContext? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return AnchorKind == other.AnchorKind && Equals(Sequence, other.Sequence);
		}

		public override bool Equals(object? obj)
		{
			return ReferenceEquals(this, obj) || obj is AnchorContext other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((int)AnchorKind * 397) ^ Sequence.GetHashCode();
			}
		}

	}

}
