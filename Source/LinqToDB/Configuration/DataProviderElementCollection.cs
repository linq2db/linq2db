using System;
using System.Configuration;

namespace LinqToDB.Configuration
{
	/// <summary>
	/// Collection of data provider configuration elements.
	/// </summary>
	[ConfigurationCollection(typeof(DataProviderElement))]
	public class DataProviderElementCollection : ElementCollectionBase<DataProviderElement>
	{
		protected override object GetElementKey(DataProviderElement element)
		{
			// element.Name is optional and may be omitted.
			// element.TypeName is required, but is not unique.
			//
			return string.Concat(element.Name, "/", element.TypeName);
		}
	}
}
