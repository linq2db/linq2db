using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	[DebuggerDisplay("CTE({CteID}, {Name})")]
	public sealed class CteClause : QueryElement
	{
		internal static int CteIDCounter;

		public List<SqlCteField> Fields { get; internal set; }

		public int          CteID       { get; } = Interlocked.Increment(ref CteIDCounter);

		public string?      Name        { get; set; }
		public SelectQuery? Body        { get; set; }
		public Type         ObjectType  { get; set; }
		public bool         IsRecursive { get; set; }

		/// <summary>
		/// Data-modifying statement whose <c>RETURNING</c>/<c>OUTPUT</c> rows are the CTE rows. When set, <see cref="Body"/>
		/// is the projection of the inserted rows: its columns are output expressions over the target table, wrapped in
		/// <see cref="SqlAnchor.AnchorKindEnum.Inserted"/> anchors, and are not rendered as a <c>SELECT</c>. The optimizer copies the
		/// remaining body columns into the statement output clause, which renders as the CTE body.
		/// Gated by <see cref="SqlProvider.SqlProviderFlags.IsInsertOutputQuerySupported"/>.
		/// </summary>
		public SqlInsertStatement? DataModification { get; set; }

		/// <summary>
		/// Open-ended metadata bag for provider-specific CTE hints (e.g. PostgreSQL <c>MATERIALIZED</c>).
		/// Providers that do not recognize an annotation name ignore it.
		/// </summary>
		public Annotatable  Annotations { get; } = new();

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
			SelectQuery?              body,
			IEnumerable<SqlCteField>  fields,
			Type                      objectType,
			bool                      isRecursive,
			string?                   name)
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
			SelectQuery?                body,
			IReadOnlyList<SqlCteField>  fields)
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
			hash.Add(DataModification?.GetElementHashCode());
			hash.Add(ObjectType);

			foreach (var field in Fields)
				hash.Add(field.GetElementHashCode());

			foreach (var annotation in Annotations.GetAnnotations())
			{
				hash.Add(annotation.Name);
				hash.Add(annotation.Value);
			}

			return hash.ToHashCode();
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitCteClause(this);
	}
}
