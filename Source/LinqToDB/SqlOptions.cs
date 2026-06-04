using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Options;

namespace LinqToDB
{
	/// <param name="EnableConstantExpressionInOrderBy">
	/// If <see langword="true"/>, linq2db will allow any constant expressions in ORDER BY clause.
	/// Default value: <see langword="false"/>.
	/// </param>
	/// <param name="GenerateFinalAliases">
	/// Indicates whether SQL Builder should generate aliases for final projection.
	/// It is not required for correct query processing but simplifies SQL analysis.
	/// <para>
	/// Default value: <see langword="false"/>.
	/// </para>
	/// <example>
	/// For the query
	/// <code>
	/// var query = from child in db.Child
	///	   select new
	///	   {
	///       TrackId = child.ChildID,
	///	   };
	/// </code>
	/// When property is <see langword="true"/>
	/// <code>
	/// SELECT
	///	   [child].[ChildID] as [TrackId]
	/// FROM
	///	   [Child] [child]
	/// </code>
	/// Otherwise alias will be removed
	/// <code>
	/// SELECT
	///	   [child].[ChildID]
	/// FROM
	///	   [Child] [child]
	/// </code>
	/// </example>
	/// </param>
	public sealed record SqlOptions
	(
		bool EnableConstantExpressionInOrderBy = false,
		bool GenerateFinalAliases              = false
	)
		: IOptionSet
	{
		public SqlOptions() : this(false)
		{
		}

		SqlOptions(SqlOptions original)
		{
			EnableConstantExpressionInOrderBy = original.EnableConstantExpressionInOrderBy;
			GenerateFinalAliases              = original.GenerateFinalAliases;
			DefaultNullsPosition              = original.DefaultNullsPosition;
		}

		/// <summary>
		/// Default position of <c>NULL</c> values in an <c>ORDER BY</c> clause for ordering keys that do not
		/// specify a <see cref="Sql.NullsPosition"/> explicitly. When set to <see cref="Sql.NullsPosition.First"/>
		/// or <see cref="Sql.NullsPosition.Last"/>, it is applied to every such key (and emulated for providers
		/// without native <c>NULLS FIRST</c>/<c>NULLS LAST</c> support). A position specified explicitly on a key
		/// always takes precedence — including an explicit <see cref="Sql.NullsPosition.None"/>, which opts that
		/// key out of the default and uses the provider's natural null ordering.
		/// Default value: <see cref="Sql.NullsPosition.None"/> (use the provider's default null ordering).
		/// </summary>
		public Sql.NullsPosition DefaultNullsPosition { get; init; } = Sql.NullsPosition.None;

		int? _configurationID;
		int IConfigurationID.ConfigurationID
		{
			get
			{
				if (_configurationID == null)
				{
					using var idBuilder = new IdentifierBuilder();
					_configurationID = idBuilder
						.Add(EnableConstantExpressionInOrderBy)
						.Add(GenerateFinalAliases)
						.Add((int)DefaultNullsPosition)
						.CreateID();
				}

				return _configurationID.Value;
			}
		}

		#region Default Options

		/// <summary>
		/// Gets default <see cref="SqlOptions"/> instance.
		/// </summary>
		public static SqlOptions Default
		{
			get;
			set
			{
				field = value;
				DataConnection.ResetDefaultOptions();
				DataConnection.ConnectionOptionsByConfigurationString.Clear();
			}
		} = new();

		/// <inheritdoc />
		IOptionSet IOptionSet.Default => Default;

		#endregion

		#region IEquatable implementation

		public bool Equals(SqlOptions? other)
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
