using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	class BuildInfo
	{
		public BuildInfo(IBuildContext parent, Expression expression, SelectQuery selectQuery)
		{
			Parent      = parent;
			Expression  = expression;
			SelectQuery = selectQuery;
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

		public BuildInfo            SequenceInfo   { get; set; }
		public IBuildContext        Parent         { get; set; }
		public Expression           Expression     { get; set; }
		public SelectQuery          SelectQuery    { get; set; }
		public bool                 CopyTable      { get; set; }
		public bool                 CreateSubQuery { get; set; }
		public SelectQuery.JoinType JoinType       { get; set; }

		public bool          IsSubQuery   { get { return Parent != null; } }

		private bool _isAssociationBuilt;
		public  bool  IsAssociationBuilt
		{
			get { return _isAssociationBuilt; }
			set
			{
				_isAssociationBuilt = value;

				if (SequenceInfo != null)
					SequenceInfo.IsAssociationBuilt = value;
			}
		}
	}
}
