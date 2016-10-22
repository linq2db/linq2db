using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Expressions;
using NUnit.Framework;
using LinqToDB.Extensions;

namespace Tests
{
	[TestFixture]
	public class ConfigurationTest
	{
		public class ConnectionStringSettings : IConnectionStringSettings
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
				get { yield break; }
			}

			public string DefaultConfiguration => "SqLite";
			public string DefaultDataProvider => "SqlServer";

			public IEnumerable<IConnectionStringSettings> ConnectionStrings
			{
				get
				{
					yield return
						new ConnectionStringSettings
						{
							Name = "SQLite",
							ProviderName = "SQLite",
							ConnectionString = @"Data Source=..\..\..\..\..\Data\TestData.sqlite"
						};
				}
			}
		}

		[Test]
		public void Configuration()
		{
			DataConnection.DefaultSettings = new MySettings();
			var d = new DataContext();
			Assert.AreEqual("SQLite", DataConnection.DefaultConfiguration);

			var now = d.Select(() => Sql.GetDate());
			var one = d.Select(() => 1);

			Assert.AreNotEqual(default(DateTime), now);
			Assert.AreEqual   (1,                 one);
		}

		[Test]
		public void CurrentDirectory()
		{
			var oldPath = System.IO.Directory.GetCurrentDirectory();
			Console.WriteLine(oldPath);

			var path = typeof(ConfigurationTest).AssemblyEx().CodeBase.Substring("file:///".Length);
			path = System.IO.Path.GetDirectoryName(path);
			Console.WriteLine(path);

			System.IO.Directory.SetCurrentDirectory(path);
			Assert.AreEqual(path, System.IO.Directory.GetCurrentDirectory());

			System.IO.Directory.SetCurrentDirectory(oldPath);

		}
	}

	[TestFixture]
	public class OverrideTests : TestBase
	{
		[Test]
		public void InheritanceMappingIssueTest()
		{
			var t = new Tests.Linq.InheritanceTests();
			t.InheritanceMappingIssueTest();
		}
	}
}