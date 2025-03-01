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
			SequenceInfo         = buildInfo;
			CreateSubQuery       = buildInfo.CreateSubQuery;
		}

		public BuildInfo(BuildInfo buildInfo, Expression expression, SelectQuery selectQuery)
			: this(buildInfo.Parent, expression, selectQuery)
		{
			SequenceInfo         = buildInfo;
			CreateSubQuery       = buildInfo.CreateSubQuery;
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

		bool _isAssociationBuilt;
		public bool   IsAssociationBuilt
		{
			get => _isAssociationBuilt;
			set
			{
				_isAssociationBuilt = value;

				if (SequenceInfo != null)
					SequenceInfo.IsAssociationBuilt = value;
			}
		}

		SourceCardinality _sourceCardinality;
		public SourceCardinality SourceCardinality
		{
			get
			{
				if (SequenceInfo == null)
					return _sourceCardinality;
				var parent = SequenceInfo.SourceCardinality;
				if (parent == SourceCardinality.Unknown)
					return _sourceCardinality;
				return parent;
			}

			set => _sourceCardinality = value;
		}

		bool _isAggregation;

		public bool IsAggregation
		{
			get
			{
				if (_isAggregation || SequenceInfo == null)
					return _isAggregation;
				return SequenceInfo.IsAggregation;
			}

			set => _isAggregation = value;
		}

		public bool IsSubqueryExpression { get; set; }

	}
}
