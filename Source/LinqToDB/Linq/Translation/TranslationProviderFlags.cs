using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Linq.Translation
{
	/// <summary>
	/// Read-only, translation-relevant subset of the provider's <c>SqlProviderFlags</c>, exposed to member
	/// translators via <see cref="ITranslationContext.ProviderFlags"/>. Only the flags member translators need
	/// are surfaced here, rather than the whole <c>SqlProviderFlags</c>.
	/// </summary>
	public sealed class TranslationProviderFlags
	{
		/// <summary>Initializes a new <see cref="TranslationProviderFlags"/>.</summary>
		/// <param name="defaultNullsOrdering">The provider's natural NULL placement when no <c>NULLS FIRST</c>/<c>NULLS LAST</c> is specified.</param>
		/// <param name="isNullsOrderingSupported">Whether the provider supports the <c>NULLS FIRST</c>/<c>NULLS LAST</c> keyword in <c>ORDER BY</c>.</param>
		public TranslationProviderFlags(NullsDefaultOrdering defaultNullsOrdering, bool isNullsOrderingSupported)
			: this(defaultNullsOrdering, isNullsOrderingSupported, false, false, false)
		{
		}

		/// <summary>Initializes a new <see cref="TranslationProviderFlags"/>.</summary>
		/// <param name="defaultNullsOrdering">The provider's natural NULL placement when no <c>NULLS FIRST</c>/<c>NULLS LAST</c> is specified.</param>
		/// <param name="isNullsOrderingSupported">Whether the provider supports the <c>NULLS FIRST</c>/<c>NULLS LAST</c> keyword in <c>ORDER BY</c>.</param>
		/// <param name="canLowerIntervalDifference">Whether an elapsed date difference can be lowered to a value.</param>
		/// <param name="canLowerIntervalPart">Whether a member of an elapsed date difference can be lowered.</param>
		/// <param name="canLowerIntervalShift">Whether a date shifted by an interval can be lowered.</param>
		/// <param name="intervalResolution">The finest unit the provider can resolve when measuring elapsed time.</param>
		/// <param name="canMeasureDifferenceInTicks">Whether an elapsed date difference can become a tick count.</param>
		/// <param name="canAttachZone">Whether a wall-clock reading can be given a zone's offset.</param>
		/// <param name="canConvertZone">Whether an instant can be re-expressed with another zone's offset.</param>
		/// <param name="canReadWallTime">Whether the wall-clock reading an instant shows in a zone can be produced.</param>
		/// <param name="requiresConstantTimeZone">Whether the time zone must be a constant rather than a bind.</param>
		public TranslationProviderFlags(
			NullsDefaultOrdering defaultNullsOrdering,
			bool                 isNullsOrderingSupported,
			bool                 canLowerIntervalDifference,
			bool                 canLowerIntervalPart,
			bool                 canLowerIntervalShift,
			SqlIntervalUnit      intervalResolution          = SqlIntervalUnit.Tick,
			bool                 canMeasureDifferenceInTicks = true,
			bool                 canAttachZone               = false,
			bool                 canConvertZone              = false,
			bool                 canReadWallTime             = false,
			bool                 requiresConstantTimeZone    = false)
		{
			RequiresConstantTimeZone    = requiresConstantTimeZone;
			DefaultNullsOrdering        = defaultNullsOrdering;
			IsNullsOrderingSupported    = isNullsOrderingSupported;
			CanLowerIntervalDifference  = canLowerIntervalDifference;
			CanLowerIntervalPart        = canLowerIntervalPart;
			CanLowerIntervalShift       = canLowerIntervalShift;
			IntervalResolution          = intervalResolution;
			CanMeasureDifferenceInTicks = canMeasureDifferenceInTicks;
			_canAttachZone              = canAttachZone;
			_canConvertZone             = canConvertZone;
			_canReadWallTime            = canReadWallTime;
		}

		/// <summary>
		/// Whether the provider's grammar demands a constant in the time zone position rather than a bind. Where it
		/// does, the translator demotes the zone to a constant, which also puts its value in the query cache key.
		/// </summary>
		public bool RequiresConstantTimeZone { get; }

		readonly bool _canAttachZone;
		readonly bool _canConvertZone;
		readonly bool _canReadWallTime;

		/// <summary>
		/// Whether the provider can render the given time zone conversion. Asked while the expression is still being
		/// built, so a translator that cannot have one can decline and leave the member to .NET rather than letting
		/// the SQL builder fail the whole query.
		/// </summary>
		/// <remarks>
		/// The three kinds are separate capabilities rather than one flag: a provider with no column type that
		/// carries an offset can still answer the wall-clock reading in a named zone, so it supports
		/// <see cref="SqlTimeZoneConversionKind.ToWallTime"/> alone.
		/// </remarks>
		public bool CanLowerTimeZoneConversion(SqlTimeZoneConversionKind kind) => kind switch
		{
			SqlTimeZoneConversionKind.AttachZone  => _canAttachZone,
			SqlTimeZoneConversionKind.ConvertZone => _canConvertZone,
			_                                     => _canReadWallTime,
		};

		/// <summary>The provider's natural NULL placement when no <c>NULLS FIRST</c>/<c>NULLS LAST</c> is specified.</summary>
		public NullsDefaultOrdering DefaultNullsOrdering { get; }

		/// <summary>Whether the provider supports the <c>NULLS FIRST</c>/<c>NULLS LAST</c> keyword in <c>ORDER BY</c>.</summary>
		public bool IsNullsOrderingSupported { get; }

		/// <summary>
		/// Whether an elapsed date difference can be lowered to a value. Answered by the provider's
		/// <c>SqlExpressionConvertVisitor</c>, which owns the lowering and documents the contract.
		/// </summary>
		public bool CanLowerIntervalDifference { get; }

		/// <summary>
		/// Whether a member of an elapsed date difference can be lowered. Separate from
		/// <see cref="CanLowerIntervalDifference"/> because a provider may have only this half.
		/// </summary>
		public bool CanLowerIntervalPart { get; }

		/// <summary>
		/// Whether a date shifted by an interval can be lowered. Read for a shift by a <em>declared</em> duration
		/// only: that amount is real and nothing later removes it, so a provider that cannot spend one says so while
		/// the expression is still being built and leaves a projection free to fall back to .NET. A shift by a
		/// computed difference is built regardless, because it may cancel against the difference it came from and
		/// ask the provider for nothing at all.
		/// </summary>
		public bool CanLowerIntervalShift { get; }

		/// <summary>
		/// The finest unit the provider can resolve when measuring elapsed time. A <em>component</em> asked for in
		/// a finer unit is identically zero rather than merely imprecise, so the translator declines to build it
		/// and leaves the member to .NET.
		/// </summary>
		public SqlIntervalUnit IntervalResolution { get; }

		/// <summary>
		/// Whether an elapsed date difference can become a tick count. Where it cannot, a <em>total</em> asked for
		/// in a unit finer than <see cref="IntervalResolution"/> has nowhere to come from, so the translator
		/// declines to build it and leaves the member to .NET rather than letting the SQL builder fail the whole
		/// query. A total is otherwise built: a coarser measurement quantises one without making it meaningless.
		/// </summary>
		public bool CanMeasureDifferenceInTicks { get; }
	}
}
