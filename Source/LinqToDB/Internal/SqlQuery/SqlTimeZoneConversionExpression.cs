using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// A temporal value read in, or moved to, a named time zone.
	/// </summary>
	/// <remarks>
	/// One node with a <see cref="SqlTimeZoneConversionKind"/> discriminator rather than three nodes: all three kinds
	/// carry the same operand pair, and on every provider with an infix <c>AT TIME ZONE</c> they render with the same
	/// operator, differing only in an outer cast.
	/// <para>
	/// <see cref="Zone"/> is an expression rather than a string so that it is visited, serialized and
	/// nullability-tracked like any other child. A provider whose grammar demands a literal folds it itself and
	/// refuses by name when it cannot.
	/// </para>
	/// </remarks>
	public sealed class SqlTimeZoneConversionExpression : SqlExpressionBase
	{
		public SqlTimeZoneConversionExpression(ISqlExpression value, ISqlExpression zone, SqlTimeZoneConversionKind kind, DbDataType type)
		{
			Value = value ?? throw new ArgumentNullException(nameof(value));
			Zone  = zone  ?? throw new ArgumentNullException(nameof(zone));
			Kind  = kind;
			Type  = type;
		}

		/// <summary>
		/// The temporal value being converted.
		/// </summary>
		public ISqlExpression            Value { get; private set; }

		/// <summary>
		/// The target time zone. Interpreted by the database, so the accepted spelling is the server's - a Windows
		/// identifier on SQL Server, an IANA identifier elsewhere.
		/// </summary>
		public ISqlExpression            Zone  { get; private set; }

		public SqlTimeZoneConversionKind Kind  { get; private set; }

		public DbDataType                Type  { get; private set; }

		public override int              Precedence  => LinqToDB.SqlQuery.Precedence.Unknown;
		public override Type?            SystemType  => Type.SystemType;
		public override QueryElementType ElementType => QueryElementType.SqlTimeZoneConversion;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.DebugAppendUniqueId(this)
				.Append(Kind switch
				{
					SqlTimeZoneConversionKind.AttachZone  => "ATTACH_ZONE(",
					SqlTimeZoneConversionKind.ConvertZone => "CONVERT_ZONE(",
					_                                     => "WALL_TIME(",
				})
				.AppendElement(Value)
				.Append(", ")
				.AppendElement(Zone)
				.Append(')');

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(Type);
			hash.Add(Kind);
			hash.Add(Value.GetElementHashCode());
			hash.Add(Zone.GetElementHashCode());
			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (ReferenceEquals(other, this))
				return true;

			// Kind must participate: AttachZone and ToWallTime over the same operand and zone denote different
			// instants, so a kind-insensitive comparison would make one equal to the other.
			return other is SqlTimeZoneConversionExpression otherConversion
				&& Type.Equals(otherConversion.Type)
				&& Kind.Equals(otherConversion.Kind)
				&& Value.Equals(otherConversion.Value, comparer)
				&& Zone.Equals(otherConversion.Zone, comparer);
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlTimeZoneConversionExpression(this);

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return Value.CanBeNullable(nullability) || Zone.CanBeNullable(nullability);
		}

		public void Modify(ISqlExpression value, ISqlExpression zone, SqlTimeZoneConversionKind kind, DbDataType type)
		{
			Value = value;
			Zone  = zone;
			Kind  = kind;
			Type  = type;
		}
	}
}
