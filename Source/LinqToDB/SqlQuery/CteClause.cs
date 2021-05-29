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
	public class CteClause : IQueryElement, ISqlExpressionWalkable
	{
		SqlField[]? _fields = Array<SqlField>.Empty;

		public static int CteIDCounter;

		public SqlField[]? Fields
		{
			get => _fields;
			internal set => _fields = value;
		}

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

			Fields      = fields.ToArray();
		}

		internal CteClause(
			Type    objectType,
			bool    isRecursive,
			string? name)
		{
			Name        = name;
			ObjectType  = objectType;
			IsRecursive = isRecursive;
		}

		internal void Init(
			SelectQuery?          body,
			ICollection<SqlField> fields)
		{
			Body       = body;
			Fields     = fields.ToArray();
		}

		public QueryElementType ElementType => QueryElementType.CteClause;

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return sb.Append($"CTE({CteID}, {Name})");
		}

		public ISqlExpression? Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
		{
			Body = Body?.Walk(options, func) as SelectQuery;

			return null;
		}

		public SqlField RegisterFieldMapping(int index, Func<SqlField> fieldFactory)
		{
			if (Fields!.Length > index && Fields[index] != null)
				return Fields[index];

			var newField = fieldFactory();

			Utils.MakeUniqueNames(new[] { newField }, Fields.Where(f => f != null).Select(t => t.Name), f => f.Name, (f, n, a) =>
			{
				f.Name = n;
				f.PhysicalName = n;
			}, f => (string.IsNullOrEmpty(f.Name) ? "cte_field" : f.Name) + "_1");

			if (Fields.Length < index + 1)
				Array.Resize(ref _fields, index + 1);

			Fields[index] = newField;

			return newField;
		}
	}
}
