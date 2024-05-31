﻿using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public class SqlTableLikeSource : SqlSourceBase
	{
		public SqlTableLikeSource()
		{
		}

		internal SqlTableLikeSource(
			int                   id,
			SqlValuesTable?       sourceEnumerable,
			SelectQuery?          sourceQuery,
			IEnumerable<SqlField> sourceFields) : base(id)
		{
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
		public override ISqlTableSource  Source => (ISqlTableSource?)SourceQuery ?? SourceEnumerable!;

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

		#region SqlSourceBase overrides

		public override QueryElementType       ElementType => QueryElementType.SqlTableLikeSource;
		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			return writer.AppendElement(Source);
		}

		public override SqlTableType SqlTableType => SqlTableType.MergeSource;

		SqlField?                _all;
		public override SqlField All => _all ??= SqlField.All(this);

		#endregion

		#region SqlExpressionBase overrides

		public override IList<ISqlExpression> GetKeys(bool allIfEmpty) => throw new NotImplementedException();
		public override bool CanBeNullable(NullabilityContext nullability) => throw new NotImplementedException();
		public override int Precedence => throw new NotImplementedException();
		public override Type SystemType => throw new NotImplementedException();
		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer) => throw new NotImplementedException();
		public override bool Equals(ISqlExpression? other) => throw new NotImplementedException();

		#endregion

	}
}
