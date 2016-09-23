using System;
using System.Collections.Generic;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using NUnit.Framework;

namespace Tests.NetCore
{
	[TestFixture]
    public class ConfigurationTest
    {
		[Test]
        public void Configuration()
        {
			DataConnection.DefaultSettings = new MySettings();
			var d = new DataContext();
			Assert.AreEqual("Sql", DataConnection.DefaultConfiguration);
	        var now = d.Select(() => Sql.GetDate());
			Assert.AreNotEqual(default(DateTime), now);
        }

		public class ConnectionStringSettings :IConnectionStringSettings
		{
			public string ConnectionString { get; set; }
			public string Name { get; set; }
			public string ProviderName { get; set; }
			public bool IsGlobal => false;
		}
	    public class MySettings : ILinqToDBSettings
	    {
		    public IEnumerable<IDataProviderSettings> DataProviders
		    {
			    get
			    {
				  yield break;  
			    } 
		    }

		    public string DefaultConfiguration => "Sql";
		    public string DefaultDataProvider => "SqlServer";

		    public IEnumerable<IConnectionStringSettings> ConnectionStrings
		    {
			    get
			    {
				    yield return new ConnectionStringSettings {Name = "Sql", ProviderName = "SqlServer", ConnectionString = "Server=.;Default Catalog=TestData;Integrated Security=SSPI"};
			    }
		    }
	    }
    }
}
