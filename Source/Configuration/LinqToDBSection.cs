using System;
using System.Configuration;
using System.Security;

namespace LinqToDB.Configuration
{
	/// <summary>
	/// Implementation of custom configuration section.
	/// </summary>
	internal class LinqToDBSection : ConfigurationSection
	{
		private const string SectionName = "LinqToDB";
		private static readonly ConfigurationPropertyCollection _properties =
			new ConfigurationPropertyCollection();

		private static readonly ConfigurationProperty           _propDataProviders = 
			new ConfigurationProperty("dataProviders",           typeof(DataProviderElementCollection),
			new DataProviderElementCollection(),                 ConfigurationPropertyOptions.None);
		private static readonly ConfigurationProperty           _propDefaultConfiguration =
			new ConfigurationProperty("defaultConfiguration",    typeof(string),
			null,                                                ConfigurationPropertyOptions.None);

		static LinqToDBSection()
		{
			_properties.Add(_propDataProviders);
			_properties.Add(_propDefaultConfiguration);
		}

		public static LinqToDBSection Instance
		{
			get
			{
				try
				{
					return (LinqToDBSection)ConfigurationManager.GetSection(SectionName);
				}
				catch (SecurityException)
				{
					return null;
				}
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get { return _properties; }
		}

		public DataProviderElementCollection DataProviders
		{
			get { return (DataProviderElementCollection) base[_propDataProviders]; }
		}

		public string DefaultConfiguration
		{
			get { return (string)base[_propDefaultConfiguration]; }
		}
	}
}
