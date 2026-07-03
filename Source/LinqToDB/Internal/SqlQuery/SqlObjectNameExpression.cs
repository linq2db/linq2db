using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// A database object name (table / sequence / trigger / field) that renders with a specific
	/// <see cref="ConvertType"/>. Lets a <see cref="SqlFragment"/> — and thus a <see cref="SqlFragmentStatement"/> —
	/// carry a correctly-quoted derived name (e.g. an identity sequence <c>SIDENTITY_&lt;table&gt;</c>) whose quoting
	/// is only available in the SQL builder.
	/// </summary>
	public sealed class SqlObjectNameExpression : SqlExpressionBase
	{
		public SqlObjectNameExpression(SqlObjectName name, ConvertType convertType, TableOptions tableOptions = TableOptions.NotSet)
		{
			Name         = name;
			ConvertType  = convertType;
			TableOptions = tableOptions;
		}

		public SqlObjectName Name         { get; }
		public ConvertType   ConvertType  { get; }
		public TableOptions  TableOptions { get; }

		public override QueryElementType ElementType => QueryElementType.SqlObjectNameExpression;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.DebugAppendUniqueId(this);
			return writer.Append(Name.Name);
		}

		public override string ToString() => this.ToDebugString();

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(Name);
			hash.Add(ConvertType);
			hash.Add(TableOptions);
			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(this, other))
				return true;

			return other is SqlObjectNameExpression expr
				&& Name.Equals(expr.Name)
				&& ConvertType == expr.ConvertType
				&& TableOptions == expr.TableOptions
				&& comparer(this, expr);
		}

		public override bool CanBeNullable(NullabilityContext nullability) => false;

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlObjectNameExpression(this);

		public override int   Precedence => LinqToDB.SqlQuery.Precedence.Primary;
		public override Type? SystemType => null;
	}
}
