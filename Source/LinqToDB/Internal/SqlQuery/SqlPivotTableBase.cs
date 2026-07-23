using System;
using System.Collections.Generic;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Shared base for the PIVOT / UNPIVOT table-source operators. Wraps an inner
	/// <see cref="ISqlTableSource"/> and exposes a new set of output fields.
	/// </summary>
	public abstract class SqlPivotTableBase : SqlSourceBase
	{
		protected SqlPivotTableBase(ISqlTableSource source)
		{
			PivotSource = source;
		}

		protected SqlPivotTableBase(int id, ISqlTableSource source) : base(id)
		{
			PivotSource = source;
		}

		public ISqlTableSource PivotSource { get; internal set; }

		/// <summary>
		/// Columns exposed by the operator (passthrough source columns plus the synthesized ones).
		/// </summary>
		public List<SqlField> OutputFields { get; } = new();

		public void AddField(SqlField field)
		{
			field.Table = this;
			OutputFields.Add(field);
		}

		public override ISqlTableSource Source => PivotSource;

		SqlField? _all;
		public override SqlField All => _all ??= SqlField.All(this);

		#region SqlExpressionBase overrides (not a value expression)

		public override IList<ISqlExpression> GetKeys(bool allIfEmpty)                              => throw new NotSupportedException();
		public override bool                  CanBeNullable(NullabilityContext nullability)         => throw new NotSupportedException();
		public override int                   Precedence                                            => throw new NotSupportedException();
		public override Type                  SystemType                                            => throw new NotSupportedException();
		public override bool                  Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer) => throw new NotSupportedException();
		public override bool                  Equals(ISqlExpression? other)                         => throw new NotSupportedException();

		#endregion
	}
}
