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
			SourceCardinality = SourceCardinality.Unknown;
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
		public bool           AssociationsAsSubQueries { get; set; }
		public bool           IsAssociation            { get; set; }
		public JoinType       JoinType                 { get; set; }
		public bool           IsSubQuery               => Parent != null;
		public bool           IgnoreOrderBy            { get; set; }

		public bool   IsAssociationBuilt
		{
			get;
			set
			{
				field = value;

				if (SequenceInfo != null)
					SequenceInfo.IsAssociationBuilt = value;
			}
		}

		public SourceCardinality SourceCardinality
		{
			get
			{
				if (SequenceInfo == null)
					return field;
				var parent = SequenceInfo.SourceCardinality;
				if (parent == SourceCardinality.Unknown)
					return field;
				return parent;
			}

			set;
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
