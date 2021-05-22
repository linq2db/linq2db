using LinqToDB.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LinqToDB.SqlQuery
{
	public class SqlTableLikeSource : ISqlTableSource
	{
		public SqlTableLikeSource()
		{
			SourceID = Interlocked.Increment(ref SelectQuery.SourceIDCounter);
		}

		internal SqlTableLikeSource(
			int id,
			SqlValuesTable sourceEnumerable,
			SelectQuery sourceQuery,
			IEnumerable<SqlField> sourceFields)
		{
			SourceID         = id;
			SourceEnumerable = sourceEnumerable;
			SourceQuery      = sourceQuery;

			foreach (var field in sourceFields)
				AddField(field);
		}

		public string          Name   => "Source";

		public List<SqlField> SourceFields { get; } = new List<SqlField>();

		private void AddField(SqlField field)
		{
			field.Table = this;
			SourceFields.Add(field);
		}

		public SqlValuesTable?  SourceEnumerable { get; internal set; }
		public SelectQuery?     SourceQuery      { get; internal set; }
		public ISqlTableSource  Source => (ISqlTableSource?)SourceQuery ?? SourceEnumerable!;

		public void WalkQueries(Func<SelectQuery, SelectQuery> func)
		{
			if (SourceQuery != null)
				SourceQuery = func(SourceQuery);
		}

		public bool IsParameterDependent
		{
			// enumerable source allways parameter-dependent
			get => SourceQuery?.IsParameterDependent ?? true;
			set
			{
				if (SourceQuery != null)
					SourceQuery.IsParameterDependent = value;
			}
		}

		private readonly IDictionary<SqlField, Tuple<SqlField, int>>       _sourceFieldsByBase       = new Dictionary<SqlField, Tuple<SqlField, int>>();
		private readonly IDictionary<ISqlExpression, Tuple<SqlField, int>> _sourceFieldsByExpression = new Dictionary<ISqlExpression, Tuple<SqlField, int>>();

		internal SqlField RegisterSourceField(ISqlExpression baseExpression, ISqlExpression expression, int index, Func<SqlField> fieldFactory)
		{
			var baseField = baseExpression as SqlField;

			if (baseField != null && _sourceFieldsByBase.TryGetValue(baseField, out var value))
				return value.Item1;

			if (baseField == null && expression != null && _sourceFieldsByExpression.TryGetValue(expression, out value))
				return value.Item1;

			var newField = fieldFactory();

			Utils.MakeUniqueNames(new[] { newField }, _sourceFieldsByExpression.Values.Select(t => t.Item1.Name), f => f.Name, (f, n, a) =>
			{
				f.Name = n;
				f.PhysicalName = n;
			}, f => "source_field");

			SourceFields.Insert(index, newField);

			if (expression != null && !_sourceFieldsByExpression.ContainsKey(expression))
				_sourceFieldsByExpression.Add(expression, Tuple.Create(newField, index));

			if (baseField != null)
				_sourceFieldsByBase.Add(baseField, Tuple.Create(newField, index));

			return newField;
		}

		#region IQueryElement

		QueryElementType IQueryElement.ElementType => QueryElementType.MergeSourceTable;

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return Source.ToString(sb, dic);
		}

		#endregion

		#region ISqlTableSource

		SqlTableType ISqlTableSource.SqlTableType => SqlTableType.MergeSource;

		private SqlField? _all;
		SqlField ISqlTableSource.All => _all ??= SqlField.All(this);


		public int SourceID { get; }

		IList<ISqlExpression> ISqlTableSource.GetKeys(bool allIfEmpty) => throw new NotImplementedException();

		#endregion

		#region ISqlExpressionWalkable

		public ISqlExpression? Walk(WalkOptions options, Func<ISqlExpression, ISqlExpression> func)
		{
			return SourceQuery?.Walk(options, func);
		}

		#endregion

		#region ISqlExpression

		bool ISqlExpression.CanBeNull => throw new NotImplementedException();

		int ISqlExpression.Precedence => throw new NotImplementedException();

		Type ISqlExpression.SystemType => throw new NotImplementedException();

		bool ISqlExpression.Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer) => throw new NotImplementedException();
		
		#endregion

		#region IEquatable

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other) => throw new NotImplementedException();

		#endregion

	}
}
