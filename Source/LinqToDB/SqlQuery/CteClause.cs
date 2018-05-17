using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace LinqToDB.SqlQuery
{
	[DebuggerDisplay("CTE({CteID}, {Name})")]
	public class CteClause : IQueryElement, ICloneableElement, ISqlExpressionWalkable
	{
		public static int CteIDCounter;

		public int                          CteID  { get; } = Interlocked.Increment(ref CteIDCounter);
		public Dictionary<string, SqlField> Fields { get; } = new Dictionary<string, SqlField>();

		public string      Name        { get; set; }
		public SelectQuery Body        { get; set; }
		public Type        ObjectType  { get; set; }
		public bool        IsRecursive { get; set; }

		public CteClause(
			[JetBrains.Annotations.CanBeNull] SelectQuery body,
			[JetBrains.Annotations.NotNull]   Type        objectType,
			                                  bool        isRecursive,
			                                  string      name)
		{
			ObjectType = objectType ?? throw new ArgumentNullException(nameof(objectType));
			Body       = body;
			IsRecursive = isRecursive;
			Name       = name;
		}

		internal CteClause(
			[JetBrains.Annotations.CanBeNull] SelectQuery           body,
			[JetBrains.Annotations.NotNull]   ICollection<SqlField> fields,
			[JetBrains.Annotations.CanBeNull] Type                  objectType,
			string name)
		{
			Body       = body;
			Name       = name;
			ObjectType = objectType;

			foreach (var field in fields)
			{
				Fields.Add(field.PhysicalName, field);
			}
		}

		public QueryElementType ElementType => QueryElementType.CteClause;

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return sb.Append(Name ?? "CTE");
		}

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			return new CteClause((SelectQuery) Body.Clone(objectTree, doClone), ObjectType, IsRecursive, Name);
		}

		public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			Body = Body?.Walk(skipColumns, func) as SelectQuery;

			return null;
		}

		public void RegisterFieldMapping(SqlField field)
		{
			if (!Fields.ContainsKey(field.PhysicalName))
			{
				Fields.Add(field.PhysicalName, new SqlField(field));
			}
		}
	}
}
