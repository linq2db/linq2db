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
		private const string SectionName = "linq2db";

		static readonly ConfigurationPropertyCollection _properties               = new ConfigurationPropertyCollection();
		static readonly ConfigurationProperty           _propDataProviders        = new ConfigurationProperty("dataProviders",        typeof(DataProviderElementCollection), new DataProviderElementCollection(), ConfigurationPropertyOptions.None);
		static readonly ConfigurationProperty           _propDefaultConfiguration = new ConfigurationProperty("defaultConfiguration", typeof(string),                        null,                                ConfigurationPropertyOptions.None);
		static readonly ConfigurationProperty           _propDefaultDataProvider  = new ConfigurationProperty("defaultDataProvider",  typeof(string),                        null,                                ConfigurationPropertyOptions.None);

		static LinqToDBSection()
		{
			_properties.Add(_propDataProviders);
			_properties.Add(_propDefaultConfiguration);
			_properties.Add(_propDefaultDataProvider);
		}

		private static LinqToDBSection _instance;
		public  static LinqToDBSection  Instance
		{
			get
			{
				if (_instance == null)
				{
					try
					{
						_instance = (LinqToDBSection)ConfigurationManager.GetSection(SectionName);
					}
					catch (SecurityException)
					{
						return null;
					}
				}

				return _instance;
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

		public string DefaultConfiguration { get { return (string)base[_propDefaultConfiguration]; } }
		public string DefaultDataProvider  { get { return (string)base[_propDefaultDataProvider];  } }
	}
}
