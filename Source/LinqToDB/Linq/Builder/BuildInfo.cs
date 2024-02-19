using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	internal sealed class BuildInfo
	{
		public BuildInfo(IBuildContext? parent, Expression expression, SelectQuery selectQuery)
		{
			Parent            = parent;
			Expression        = expression;
			SelectQuery       = selectQuery;
			SourceCardinality = SourceCardinality.ZeroOrMany;
		}

		public BuildInfo(BuildInfo buildInfo, Expression expression)
			: this(buildInfo.Parent, expression, buildInfo.SelectQuery)
		{
			SequenceInfo   = buildInfo;
			CreateSubQuery = buildInfo.CreateSubQuery;
		}

		public BuildInfo(BuildInfo buildInfo, Expression expression, SelectQuery selectQuery)
			: this(buildInfo.Parent, expression, selectQuery)
		{
			SequenceInfo   = buildInfo;
			CreateSubQuery = buildInfo.CreateSubQuery;
		}

		public BuildInfo?     SequenceInfo             { get; set; }
		public IBuildContext? Parent                   { get; set; }
		public Expression     Expression               { get; set; }
		public SelectQuery    SelectQuery              { get; set; }
		public bool           CopyTable                { get; set; }
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
				return SequenceInfo.SourceCardinality;
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

		bool _aggregationTest;

		public bool AggregationTest
		{
			get
			{
				if (_aggregationTest || SequenceInfo == null)
					return _aggregationTest;
				return SequenceInfo.AggregationTest;
			}

			set => _aggregationTest = value;
		}

		bool _isTest;

		public bool IsTest
		{
			get
			{
				if (_isTest || SequenceInfo == null)
					return _isTest;
				return SequenceInfo.IsTest;
			}

			set => _isTest = value;
		}

		public ProjectFlags GetFlags()
		{
			return GetFlags(ProjectFlags.SQL);
		}

		public ProjectFlags GetFlags(ProjectFlags withFlag)
		{
			if (IsTest)
				return withFlag | ProjectFlags.Test;
			return withFlag;
		}

	}
}
