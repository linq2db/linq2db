using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Stores enum mapping information for single enum value.
	/// </summary>
	public class MapValue
	{
		/// <summary>
		/// Creates instance of class.
		/// </summary>
		/// <param name="origValue">Mapped enum value.</param>
		/// <param name="mapValues">Enum value mappings.</param>
		public MapValue(object origValue, params MapValueAttribute[] mapValues)
		{
			OrigValue = origValue;
			MapValues = mapValues;
		}

		/// <summary>
		/// Gets enum value.
		/// </summary>
		public object              OrigValue { get; private set; }

		/// <summary>
		/// Gets enum value mappings.
		/// </summary>
		public MapValueAttribute[] MapValues { get; private set; }
	}
}
