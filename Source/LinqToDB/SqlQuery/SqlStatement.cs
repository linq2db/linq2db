using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LinqToDB.Mapping;

namespace LinqToDB.SqlQuery
{
	[DebuggerDisplay("SQL = {SqlText}")]
	public abstract class SqlStatement: IQueryElement, IEquatable<ISqlExpression>, ISqlExpressionWalkable, ICloneableElement
	{
		public string SqlText =>
			((IQueryElement) this).ToString(new StringBuilder(), new Dictionary<IQueryElement, IQueryElement>())
			.ToString();

		public List<SqlParameter> Parameters { get; } = new List<SqlParameter>();

		public bool IsParameterDependent { get; set; }
		public abstract QueryType QueryType { get; }

		public virtual SqlStatement ProcessParameters(MappingSchema mappingSchema)
		{
			return this;
		}

		#region IQueryElement

		public abstract QueryElementType ElementType { get; }
		public abstract StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic);

		#endregion

		#region IEquatable<ISqlExpression>

		public abstract bool Equals(ISqlExpression other);
		public abstract ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func);

		#endregion

		#region ICloneableElement

		public abstract ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree,
			Predicate<ICloneableElement> doClone);

		#endregion
	}
}
