using System;
using System.Text;

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

		public List<SqlField> SourceFields { get; } = new ();

		void AddField(SqlField field)
		{
			field.Table = this;
			SourceFields.Add(field);
		}

		public SqlValuesTable?  SourceEnumerable { get; internal set; }
		public SelectQuery?     SourceQuery      { get; internal set; }
		public ISqlTableSource  Source => (ISqlTableSource?)SourceQuery ?? SourceEnumerable!;

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

		#region IQueryElement

		QueryElementType IQueryElement.ElementType => QueryElementType.SqlTableLikeSource;

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return Source.ToString(sb, dic);
		}

		#endregion

		#region ISqlTableSource

		SqlTableType ISqlTableSource.SqlTableType => SqlTableType.MergeSource;

		SqlField? _all;
		SqlField ISqlTableSource.All => _all ??= SqlField.All(this);


		public int SourceID { get; }

		IList<ISqlExpression> ISqlTableSource.GetKeys(bool allIfEmpty) => throw new NotImplementedException();

		#endregion

		#region ISqlExpressionWalkable

		public ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			return SourceQuery?.Walk(options, context, func);
		}

		#endregion

		#region ISqlExpression

		public bool CanBeNullable(NullabilityContext nullability) => throw new NotImplementedException();

		int ISqlExpression.Precedence => throw new NotImplementedException();

		Type ISqlExpression.SystemType => throw new NotImplementedException();

		bool ISqlExpression.Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer) => throw new NotImplementedException();
		
		#endregion

		#region IEquatable

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other) => throw new NotImplementedException();

		#endregion

	}
}
