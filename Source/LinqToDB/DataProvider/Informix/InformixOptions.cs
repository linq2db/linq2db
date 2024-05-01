using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.Informix
{
	using Common;
	using Common.Internal;
	using Data;

	/// <param name="BulkCopyType">
	/// Default bulk copy mode, used for Informix by <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, IEnumerable{T})"/>
	/// methods, if mode is not specified explicitly.
	/// Default value: <see cref="BulkCopyType.ProviderSpecific"/>.
	/// </param>
	/// <param name="ExplicitFractionalSecondsSeparator">
	/// Enables use of explicit fractional seconds separator in datetime values. Must be enabled for Informix starting from v11.70.xC8 and v12.10.xC2.
	/// More details at: https://www.ibm.com/support/knowledgecenter/SSGU8G_12.1.0/com.ibm.po.doc/new_features_ce.htm#newxc2__xc2_datetime
	/// </param>
	public sealed record InformixOptions
	(
		BulkCopyType BulkCopyType                       = BulkCopyType.ProviderSpecific,
		bool         ExplicitFractionalSecondsSeparator = true
		// If you add another parameter here, don't forget to update
		// InformixOptions copy constructor and CreateID method.
	)
		: DataProviderOptions<InformixOptions>(BulkCopyType)
	{
		public InformixOptions() : this(BulkCopyType.ProviderSpecific)
		{
		}

		InformixOptions(InformixOptions original) : base(original)
		{
			ExplicitFractionalSecondsSeparator = original.ExplicitFractionalSecondsSeparator;
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
			.Add(ExplicitFractionalSecondsSeparator)
			;

		#region IEquatable implementation

		public bool Equals(InformixOptions? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return ((IOptionSet)this).ConfigurationID == ((IOptionSet)other).ConfigurationID;
		}

		public override int GetHashCode()
		{
			return ((IOptionSet)this).ConfigurationID;
		}

		#endregion
	}
}
