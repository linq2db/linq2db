using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace LinqToDB.SqlQuery
{
	[DebuggerDisplay("CTE({CteID}, {Name})")]
	public class CteClause : IQueryElement
	{
#if DEBUG
		public string DebugText => this.ToDebugString();
#endif

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
			SelectQuery?          body,
			ICollection<SqlField> fields)
		{
			Body       = body;
			Fields     = fields.ToList();
		}

		public QueryElementType ElementType => QueryElementType.CteClause;

		public QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			return writer.Append($"CTE({CteID}, {Name})");
		}

		public void UpdateIndex(int index, SqlField field)
		{
			if (index >= Fields.Count)
				throw new InvalidOperationException();

			Fields[index] = field;
		}
	}
}
