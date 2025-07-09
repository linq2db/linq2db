using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace LinqToDB.SqlQuery
{
	[DebuggerDisplay("CTE({CteID}, {Name})")]
	public class CteClause : QueryElement
	{
		public static int CteIDCounter;

		public List<SqlField> Fields { get; internal set; }

		public int          CteID       { get; } = Interlocked.Increment(ref CteIDCounter);

		public string?      Name        { get; set; }
		public SelectQuery? Body        { get; set; }
		public Type         ObjectType  { get; set; }
		public bool         IsRecursive { get; set; }

		public CteClause(
			SelectQuery? body,
			Type         objectType,
			bool         isRecursive,
			string?      name)
		{
			ObjectType  = objectType ?? throw new ArgumentNullException(nameof(objectType));
			Body        = body;
			IsRecursive = isRecursive;
			Name        = name;
			Fields      = new ();
		}

		internal CteClause(
			SelectQuery?          body,
			IEnumerable<SqlField> fields,
			Type                  objectType,
			bool                  isRecursive,
			string?               name)
		{
			Body        = body;
			Name        = name;
			ObjectType  = objectType;
			IsRecursive = isRecursive;

			Fields      = fields.ToList();
		}

		internal CteClause(
			Type    objectType,
			bool    isRecursive,
			string? name)
		{
			Name        = name;
			ObjectType  = objectType;
			IsRecursive = isRecursive;
			Fields      = new ();
		}

		internal void Init(
			SelectQuery?            body,
			IReadOnlyList<SqlField> fields)
		{
			Body       = body;
			Fields     = fields.ToList();
		}

		public override QueryElementType ElementType => QueryElementType.CteClause;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			return writer
				.DebugAppendUniqueId(this)
				.Append("CTE(")
				.Append(CteID)
				.Append(", \"")
				.Append(Name)
				.Append("\")");
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(Name);
			hash.Add(ElementType);
			hash.Add(IsRecursive);
			hash.Add(Body?.GetElementHashCode());
			hash.Add(ObjectType);

			foreach (var field in Fields)
				hash.Add(field.GetElementHashCode());

			return hash.ToHashCode();
		}
	}
}
