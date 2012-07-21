using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlBuilder;

	public class BuildInfo
	{
		public BuildInfo(IBuildContext parent, Expression expression, SqlQuery sqlQuery)
		{
			Parent     = parent;
			Expression = expression;
			SqlQuery   = sqlQuery;
		}

		public BuildInfo(BuildInfo buildInfo, Expression expression)
			: this(buildInfo.Parent, expression, buildInfo.SqlQuery)
		{
			SequenceInfo = buildInfo;
		}

		public BuildInfo(BuildInfo buildInfo, Expression expression, SqlQuery sqlQuery)
			: this(buildInfo.Parent, expression, sqlQuery)
		{
			SequenceInfo = buildInfo;
		}

		public BuildInfo     SequenceInfo { get; set; }
		public IBuildContext Parent       { get; set; }
		public Expression    Expression   { get; set; }
		public SqlQuery      SqlQuery     { get; set; }

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
