using System.Linq.Expressions;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	internal sealed class BuildInfo
	{
		public BuildInfo(IBuildContext? parent, Expression expression, SelectQuery selectQuery)
		{
			Parent            = parent;
			Expression        = expression;
			SelectQuery       = selectQuery;
		}

		public BuildInfo(BuildInfo buildInfo, Expression expression)
			: this(buildInfo.Parent, expression, buildInfo.SelectQuery)
		{
			SequenceInfo   = buildInfo;
			CreateSubQuery = buildInfo.CreateSubQuery;
			IgnoreOrderBy  = buildInfo.IgnoreOrderBy;
		}

		public BuildInfo(BuildInfo buildInfo, Expression expression, SelectQuery selectQuery)
			: this(buildInfo.Parent, expression, selectQuery)
		{
			SequenceInfo   = buildInfo;
			CreateSubQuery = buildInfo.CreateSubQuery;
			IgnoreOrderBy  = buildInfo.IgnoreOrderBy;
		}

		public BuildInfo?     SequenceInfo             { get; set; }
		public IBuildContext? Parent                   { get; set; }
		public Expression     Expression               { get; set; }
		public SelectQuery    SelectQuery              { get; set; }
		public bool           CreateSubQuery           { get; set; }
		public bool           IsAssociation            { get; set; }
		public JoinType       JoinType                 { get; set; }
		public bool           IsSubQuery               => Parent != null;
		public bool           IgnoreOrderBy            { get; set; }

		SourceCardinality? _sourceCardinality;

		public SourceCardinality SourceCardinality
		{
			get => _sourceCardinality ?? SequenceInfo?.SourceCardinality ?? SourceCardinality.Unknown;
			set => _sourceCardinality = value;
		}

		public bool IsAggregation
		{
			get
			{
				if (field || SequenceInfo == null)
					return field;
				return SequenceInfo.IsAggregation;
			}

			set;
		}

		public bool IsSubqueryExpression { get; set; }

	}
}
