using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	public class BuildInfo
	{
		internal BuildInfo(IBuildContext? parent, Expression expression, SelectQuery selectQuery)
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

		internal BuildInfo?           SequenceInfo             { get; set; }
		internal IBuildContext?       Parent                   { get; set; }
		public   Expression           Expression               { get; set; }
		internal SelectQuery          SelectQuery              { get; set; }
		internal bool                 CopyTable                { get; set; }
		internal bool                 CreateSubQuery           { get; set; }
		internal bool                 AssociationsAsSubQueries { get; set; }
		internal JoinType             JoinType                 { get; set; }
		internal bool                 IsSubQuery => Parent != null;

		private bool _isAssociationBuilt;
		internal bool  IsAssociationBuilt
		{
			get => _isAssociationBuilt;
			set
			{
				_isAssociationBuilt = value;

				if (SequenceInfo != null)
					SequenceInfo.IsAssociationBuilt = value;
			}
		}
	}
}
