namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Which of the three time zone conversions a <see cref="SqlTimeZoneConversionExpression"/> performs.
	/// </summary>
	/// <remarks>
	/// Stated rather than inferred from the operand's type, because the operand's <see cref="DbDataType"/> can be
	/// <see cref="DataType.Undefined"/> and the SQL builder does not have the mapping schema cheaply. The distinction
	/// is also not cosmetic: <see cref="AttachZone"/> and <see cref="ToWallTime"/> over the same operand denote
	/// different instants, which is why the kind participates in equality.
	/// <para>
	/// <see cref="ConvertZone"/> is instant-preserving and may therefore be dropped by a consumer that only reads the
	/// instant. <see cref="AttachZone"/> never may - it defines the instant rather than re-labelling one.
	/// </para>
	/// </remarks>
	public enum SqlTimeZoneConversionKind
	{
		/// <summary>
		/// A wall-clock reading understood as being in the zone, giving the instant it denotes and carrying that
		/// zone's offset. The <c>DateTime</c> to <c>DateTimeOffset</c> direction.
		/// </summary>
		AttachZone,

		/// <summary>
		/// An instant re-expressed with the zone's offset instead of its own. The instant is unchanged.
		/// </summary>
		ConvertZone,

		/// <summary>
		/// The wall-clock reading an instant shows in the zone, carrying no offset. The <c>DateTimeOffset</c> to
		/// <c>DateTime</c> direction.
		/// </summary>
		ToWallTime,
	}
}
