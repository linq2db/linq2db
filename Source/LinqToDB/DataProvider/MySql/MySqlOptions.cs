using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.MySql
{
	using Common;
	using Common.Internal;
	using Data;

	/// <param name="BulkCopyType">
	/// Default bulk copy mode, used for MySql by <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, IEnumerable{T})"/>
	/// methods, if mode is not specified explicitly.
	/// Default value: <see cref="BulkCopyType.MultipleRows"/>.
	/// </param>
	public sealed record MySqlOptions
	(
		BulkCopyType         BulkCopyType              = BulkCopyType.MultipleRows,
		char                 ParameterSymbol           = '@',
		bool                 TryConvertParameterSymbol = default,
		string?              CommandParameterPrefix    = default,
		string?              SprocParameterPrefix      = default,
		IReadOnlyList<char>? ConvertParameterSymbols   = default
	)
		: DataProviderOptions<MySqlOptions>(BulkCopyType)
	{
		public MySqlOptions() : this(BulkCopyType.MultipleRows)
		{
		}

		MySqlOptions(MySqlOptions original) : base(original)
		{
			ParameterSymbol           = original.ParameterSymbol;
			TryConvertParameterSymbol = original.TryConvertParameterSymbol;
			CommandParameterPrefix    = original.CommandParameterPrefix;
			SprocParameterPrefix      = original.SprocParameterPrefix;
			ConvertParameterSymbols   = original.ConvertParameterSymbols;
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
			.Add(ParameterSymbol)
			.Add(TryConvertParameterSymbol)
			.Add(CommandParameterPrefix)
			.Add(SprocParameterPrefix)
			.Add(ConvertParameterSymbols)
			;

		#region IEquatable implementation

		public bool Equals(MySqlOptions? other)
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
