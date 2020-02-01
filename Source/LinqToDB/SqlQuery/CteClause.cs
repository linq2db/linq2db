#nullable disable
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
		Dictionary<ISqlExpression, SqlField> FieldsByExpression { get; } = new Dictionary<ISqlExpression, SqlField>();
		Dictionary<string, SqlField>         FieldByName        { get; } = new Dictionary<string, SqlField>();

		SqlField[] _fields = new SqlField[0];

		public static int CteIDCounter;

		public SqlField[] Fields
		{
			get => _fields;
			private set => _fields = value;
		}

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
			bool isRecursive,
			string name)
		{
			Body        = body;
			Name        = name;
			ObjectType  = objectType;
			IsRecursive = isRecursive;

			Fields      = fields.ToArray();
		}

		internal CteClause(
			[JetBrains.Annotations.CanBeNull] Type objectType,
			bool isRecursive,
			string name)
		{
			Name        = name;
			ObjectType  = objectType;
			IsRecursive = isRecursive;
		}

		internal void Init(
			[JetBrains.Annotations.CanBeNull] SelectQuery body,
			[JetBrains.Annotations.NotNull]   ICollection<SqlField> fields)
		{
			Body       = body;
			Fields     = fields.ToArray();
		}

		public QueryElementType ElementType => QueryElementType.CteClause;

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return sb.Append(Name ?? "CTE");
		}

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			var newClause = new CteClause((SelectQuery) Body.Clone(objectTree, doClone), ObjectType, IsRecursive, Name);
			newClause.Fields = Fields?.Select(f => (SqlField)f.Clone(objectTree, doClone)).ToArray();
			return newClause;
		}

		public ISqlExpression Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
		{
			Body = Body?.Walk(options, func) as SelectQuery;

			return null;
		}

		public SqlField RegisterFieldMapping(ISqlExpression baseExpression, ISqlExpression expression, int index, Func<SqlField> fieldFactory)
		{
			if (Fields.Length > index && Fields[index] != null)
				return Fields[index];

			if (expression != null && FieldsByExpression.TryGetValue(expression, out var value))
				return value;

			var baseField = baseExpression as SqlField;
			if (baseField != null && FieldByName.TryGetValue(baseField.Name, out value))
				return value;

			var newField = fieldFactory();

			Utils.MakeUniqueNames(new[] { newField }, Fields.Where(f => f != null).Select(t => t.Name), f => f.Name, (f, n) =>
			{
				f.Name = n;
				f.PhysicalName = n;
			}, f => (string.IsNullOrEmpty(f.Name) ? "cte_field" : f.Name) + "_1");

			if (Fields.Length < index + 1)
				Array.Resize(ref _fields, index + 1);

			Fields[index] = newField;

			if (expression != null && !FieldsByExpression.ContainsKey(expression))
				FieldsByExpression.Add(expression, newField);
			if (baseField != null)
				FieldByName.Add(baseField.Name, newField);
			else
			{
				if (expression is SqlField field && !FieldByName.ContainsKey(field.Name))
					FieldByName.Add(field.Name, newField);
			}
			return newField;

		}
	}
}
