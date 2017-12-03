using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LinqToDB.Mapping;

namespace LinqToDB.SqlQuery
{
	[DebuggerDisplay("SQL = {" + nameof(SqlText) + "}")]
	public abstract class SqlStatement: IQueryElement, ISqlExpressionWalkable, ICloneableElement
	{
		public string SqlText =>
			((IQueryElement) this).ToString(new StringBuilder(), new Dictionary<IQueryElement, IQueryElement>())
			.ToString();

		public abstract QueryType QueryType { get; }

		public abstract List<SqlParameter> Parameters { get; }

		public abstract bool IsParameterDependent { get; set; }
		
		public virtual SqlStatement ProcessParameters(MappingSchema mappingSchema)
		{
			return this;
		}

		public abstract SelectQuery SelectQuery { get; set; }


		#region IQueryElement

		public abstract QueryElementType ElementType { get; }
		public abstract StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic);

		#endregion

		#region IEquatable<ISqlExpression>

		public abstract ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func);

		#endregion

		#region ICloneableElement

		public abstract ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree,
			Predicate<ICloneableElement> doClone);

		#endregion

	}
}
