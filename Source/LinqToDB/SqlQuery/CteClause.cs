using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using LinqToDB.Common;

namespace LinqToDB.SqlQuery
{
	[DebuggerDisplay("CTE({CteID}, {Name})")]
	public class CteClause : IQueryElement, ICloneableElement, ISqlExpressionWalkable
	{
		Dictionary<ISqlExpression, Tuple<SqlField, int>> FieldIndexes { get; } = new Dictionary<ISqlExpression, Tuple<SqlField, int>>();

		public static int CteIDCounter;

		public List<SqlField>               Fields { get; } = new List<SqlField>();
		public int                          CteID  { get; } = Interlocked.Increment(ref CteIDCounter);

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
			ObjectType  = objectType ?? throw new ArgumentNullException(nameof(objectType));
			Body        = body;
			IsRecursive = isRecursive;
			Name        = name;
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
				Fields.Add(field);
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

		public SqlField RegisterFieldMapping(ISqlExpression expression, int index, Func<SqlField> fieldFactory)
		{
			if (FieldIndexes.TryGetValue(expression, out var value))
				return value.Item1;

			var newField = fieldFactory();

			Utils.MakeUniqueNames(new[] { newField }, FieldIndexes.Values.Select(t => t.Item1.Name), f => f.Name, (f, n) =>
			{
				f.Name = n;
				f.PhysicalName = n;
			}, f => "cte_field");

			Fields.Insert(index, newField);

			FieldIndexes.Add(expression, Tuple.Create(newField, index));
			return newField;

		}
	}
}
