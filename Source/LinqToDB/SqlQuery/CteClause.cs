using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class CteClause : IQueryElement, ICloneableElement, ISqlExpressionWalkable
	{
		public Dictionary<string, SqlField> Fields { get; } = new Dictionary<string, SqlField>();

		public string      Name       { get; set; }
		public SelectQuery Body       { get; set; }
		public Type        ObjectType { get; set; }

		public CteClause(
			[JetBrains.Annotations.NotNull] SelectQuery   body,
			[JetBrains.Annotations.NotNull] Type          objectType, 
			string name)
		{
			ObjectType = objectType ?? throw new ArgumentNullException(nameof(objectType));
			Body       = body ?? throw new ArgumentNullException(nameof(body));
			Name       = name;
		}

		public QueryElementType ElementType => QueryElementType.CteClause;

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return sb.Append(Name ?? "CTE");
		}

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			return new CteClause((SelectQuery) Body.Clone(objectTree, doClone), ObjectType, Name);
		}

		public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
		{
			Body = Body?.Walk(skipColumns, func) as SelectQuery;

			return null;
		}
	}
}
