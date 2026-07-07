using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Options;

namespace LinqToDB.DataProvider.Oracle
{
	/// <param name="BulkCopyType">
	/// Default bulk copy mode, used for oracle by <see cref="DataContextExtensions.BulkCopy{T}(IDataContext, IEnumerable{T})"/>
	/// methods, if mode is not specified explicitly.
	/// Default value: <see cref="BulkCopyType.MultipleRows"/>.
	/// </param>
	/// <param name="AlternativeBulkCopy">
	/// Defines type of multi-row INSERT operation to generate for <see cref="BulkCopyType.RowByRow"/> bulk copy mode.
	/// </param>
	/// <param name="DontEscapeLowercaseIdentifiers">
	/// Gets or sets flag to tell LinqToDB to quote identifiers, if they contain lowercase letters.
	/// Default value: <see langword="false"/>.
	/// This flag is added for backward compatibility and not recommended for use with new applications.
	/// </param>
	/// <param name="MaxStringParameterLength">
	/// <para>
	/// Maximum string length threshold, measured in .NET characters, for regular string parameter binding.
	/// Undefined string parameters with length greater than or equal to this value are bound as NCLOB.
	/// </para>
	/// <para>
	/// The default value is 4000, which corresponds to Oracle's default MAX_STRING_SIZE limit
	/// for VARCHAR2 values in the common single-byte case.
	/// </para>
	/// <para>
	/// This setting is a user-configurable heuristic and does not perform byte-exact validation
	/// against the database character set.
	/// </para>
	/// <para>
	/// Set to <see langword="null"/> to disable automatic NCLOB inference.
	/// </para>
	/// <para>
	/// See Oracle documentation for VARCHAR2 limits:
	/// <see href="https://docs.oracle.com/en/database/oracle/oracle-database/23/sqlrf/Data-Types.html"/>.
	/// </para>
	/// </param>
	public sealed record OracleOptions
	(
		BulkCopyType        BulkCopyType                   = BulkCopyType.MultipleRows,
		AlternativeBulkCopy AlternativeBulkCopy            = AlternativeBulkCopy.InsertAll,
		bool                DontEscapeLowercaseIdentifiers = false,
		int?                MaxStringParameterLength       = 4000
		// If you add another parameter here, don't forget to update
		// OracleOptions copy constructor and CreateID method.
	)
		: DataProviderOptions<OracleOptions>(BulkCopyType)
	{
		public OracleOptions() : this(BulkCopyType.MultipleRows)
		{
		}

		OracleOptions(OracleOptions original) : base(original)
		{
			AlternativeBulkCopy            = original.AlternativeBulkCopy;
			DontEscapeLowercaseIdentifiers = original.DontEscapeLowercaseIdentifiers;
			MaxStringParameterLength       = original.MaxStringParameterLength;
		}

		/// <summary>
		/// Binary-compatibility overload of the record's positional constructor: mirrors the
		/// public ctor signature as it was before <see cref="MaxStringParameterLength"/> was added,
		/// so assemblies compiled against the previous linq2db release continue to load.
		/// </summary>
		// TODO: remove in v7
		[EditorBrowsable(EditorBrowsableState.Never)]
		public OracleOptions(
			BulkCopyType        BulkCopyType,
			AlternativeBulkCopy AlternativeBulkCopy,
			bool                DontEscapeLowercaseIdentifiers)
			: this(
				BulkCopyType,
				AlternativeBulkCopy,
				DontEscapeLowercaseIdentifiers,
				MaxStringParameterLength: 4000)
		{
		}

		/// <summary>
		/// Binary-compatibility overload of the record's <c>Deconstruct</c>: mirrors the
		/// method signature as it was before <see cref="MaxStringParameterLength"/> was added.
		/// </summary>
		// TODO: remove in v7
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void Deconstruct(
			out BulkCopyType        BulkCopyType,
			out AlternativeBulkCopy AlternativeBulkCopy,
			out bool                DontEscapeLowercaseIdentifiers)
		{
			Deconstruct(
				out BulkCopyType,
				out AlternativeBulkCopy,
				out DontEscapeLowercaseIdentifiers,
				out _);
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
			.Add(AlternativeBulkCopy)
			.Add(DontEscapeLowercaseIdentifiers)
			.Add(MaxStringParameterLength)
			;

		#region IEquatable implementation

		public bool Equals([NotNullWhen(true)] OracleOptions? other)
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
