namespace LinqToDB.Configuration
{
	using System.Collections.Generic;

	public interface IDataProviderSettings
	{
		/// <summary>
		/// Gets or sets an assembly qualified type name of this data provider.
		/// </summary>
		string TypeName { get; }

		/// <summary>
		/// Gets or sets a name of this data provider.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets a value indicating whether the provider is default.
		/// </summary>
		bool Default { get; }

		/// <summary>
		/// Attibutes collection
		/// </summary>
		IEnumerable<NamedValue> Attributes { get; }
	}
}