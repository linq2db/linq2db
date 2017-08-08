using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security;

namespace LinqToDB.Configuration
{
	/// <summary>
	/// Implementation of custom configuration section.
	/// </summary>
	public class LinqToDBSection : ConfigurationSection, ILinqToDBSettings
	{
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
		/// <summary>
		/// linq2db configuration section.
		/// </summary>
		public  static LinqToDBSection  Instance
		{
			get
			{
				if (_instance == null)
				{
					try
					{
						_instance = (LinqToDBSection)ConfigurationManager.GetSection("linq2db")
							?? new LinqToDBSection();
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

		/// <summary>
		/// Gets list of data providers configuration elements.
		/// </summary>
		public DataProviderElementCollection DataProviders
		{
			get { return (DataProviderElementCollection) base[_propDataProviders]; }
		}

		/// <summary>
		/// Gets default connection configuration name.
		/// </summary>
		public string DefaultConfiguration { get { return (string)base[_propDefaultConfiguration]; } }
		/// <summary>
		/// Gets default data provider configuration name.
		/// </summary>
		public string DefaultDataProvider  { get { return (string)base[_propDefaultDataProvider];  } }

		IEnumerable<IConnectionStringSettings> ILinqToDBSettings.ConnectionStrings
		{
			get
			{
				foreach (ConnectionStringSettings css in ConfigurationManager.ConnectionStrings)
					yield return new ConnectionStringEx(css);
			}
		}

		IEnumerable<IDataProviderSettings> ILinqToDBSettings.DataProviders
		{
			get { return DataProviders.OfType<DataProviderElement>(); }
		}

		class ConnectionStringEx : IConnectionStringSettings
		{
			private readonly ConnectionStringSettings _css;

			public ConnectionStringEx(ConnectionStringSettings css)
			{
				_css = css;
			}

			public string ConnectionString { get { return _css.ConnectionString; } }
			public string Name { get { return _css.Name; } }
			public string ProviderName { get { return _css.ProviderName; } }
			public bool IsGlobal { get { return IsMachineConfig(_css); } }
		}

		internal static bool IsMachineConfig(ConnectionStringSettings css)
		{
			string source;

			try
			{
				source = css.ElementInformation.Source;
			}
			catch (Exception)
			{
				source = "";
			}

			return source == null || source.EndsWith("machine.config", StringComparison.OrdinalIgnoreCase);
		}
	}
}
