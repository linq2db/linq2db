using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Options;

namespace LinqToDB
{
	/// <param name="EnableConstantExpressionInOrderBy">
	/// If <c>true</c>, linq2db will allow any constant expressions in ORDER BY clause.
	/// Default value: <c>false</c>.
	/// </param>
	/// <param name="GenerateFinalAliases">
	/// Indicates whether SQL Builder should generate aliases for final projection.
	/// It is not required for correct query processing but simplifies SQL analysis.
	/// <para>
	/// Default value: <c>false</c>.
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
	/// When property is <c>true</c>
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
	/// <param name="DisableBuiltInTimeSpanConversion">
	/// If <c>true</c>, it disables built-in TimeSpan member access conversions in ExposeExpressionVisitor to allow external conversion via ExtensionAttribute.
	/// Default value: <c>false</c>.
	/// </param>
	public sealed record SqlOptions
	(
		bool EnableConstantExpressionInOrderBy = false,
		bool GenerateFinalAliases              = false,
		bool DisableBuiltInTimeSpanConversion  = false
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
			DisableBuiltInTimeSpanConversion  = original.DisableBuiltInTimeSpanConversion;
		}

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
