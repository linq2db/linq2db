using System;

using LinqToDB.Data;
using LinqToDB.Data.DataProvider;

using NUnit.Framework;

namespace Data.Linq.ProviderSpecific
{
	using Model;

	[TestFixture]
	[Category("MySql")]
	public class MySqlSprocParameterPrefixTests
	{
		[Test]
		public void ParameterPrefixTest()
		{
			var oldPrefix = MySqlDataProvider.SprocParameterPrefix;
			MySqlDataProvider.SprocParameterPrefix = "_";

			try
			{
				using (var db = new DbManager(ProviderName.MySql))
				{
					var person = db.SetSpCommand("GetPersonById", db.Parameter("?ID", 1)).ExecuteObject<Person>();
					Assert.IsNotNull(person);

					var person2 = db.SetSpCommand("GetPersonByName", db.CreateParameters(person)).ExecuteObject<Person>();
					Assert.IsNotNull(person2);

					Assert.AreEqual(person, person2);
				}
			}
			finally
			{
				MySqlDataProvider.SprocParameterPrefix = oldPrefix;
			}
		}

		[Test]
		public void SetCommandWorksCorrectlyWithSprocParameterPrefixSet()
		{
			var oldPrefix = MySqlDataProvider.SprocParameterPrefix;
			MySqlDataProvider.SprocParameterPrefix = "_";

			try
			{
				using (var db = new DbManager(ProviderName.MySql))
				{
					var person = db.SetCommand(
							"SELECT * FROM Person WHERE PersonID = ?PersonID",
							db.Parameter("?PersonID", 1))
						.ExecuteObject<Person>();

					Assert.IsNotNull(person);
					Assert.AreEqual(1, person.ID);

					var person2 = db.SetCommand(
							"SELECT * FROM Person WHERE FirstName = ?firstName AND LastName = ?lastName",
							db.CreateParameters(person))
						.ExecuteObject<Person>();

					Assert.IsNotNull(person2);
					Assert.AreEqual(person, person2);
				}
			}
			finally
			{
				MySqlDataProvider.SprocParameterPrefix = oldPrefix;
			}
		}

		[Test]
		public void SprocParameterPrefixShouldBeSpecifiedManuallyWhenItIsNotSet()
		{
			using (var db = new DbManager(ProviderName.MySql))
			{
				var person = db.SetSpCommand("GetPersonById", db.Parameter("?_ID", 1)).ExecuteObject<Person>();
				Assert.IsNotNull(person);

				var person2 = db.SetSpCommand(
						"GetPersonByName", 
						db.Parameter("?_lastName", person.LastName),
						db.Parameter("?_firstName", person.FirstName))
					.ExecuteObject<Person>();
				Assert.IsNotNull(person2);
			}
		}

		[Test]
		public void ParameterSymbolMayBeOmitedForSpCommand()
		{
			// I am not sure this is a good thing though
			// Maybe we need to be more strict on syntax and on the library users
			// and do not allow different syntax (omiting parameter symbol)?

			using (var db = new DbManager(ProviderName.MySql))
			{
				var person = db.SetSpCommand("GetPersonById", db.Parameter("_ID", 1)).ExecuteObject<Person>();
				Assert.IsNotNull(person);

				var person2 = db.SetSpCommand(
						"GetPersonByName",
						db.Parameter("_lastName", person.LastName),
						db.Parameter("_firstName", person.FirstName))
					.ExecuteObject<Person>();
				Assert.IsNotNull(person2);
				Assert.AreEqual(person, person2);

				var oldPrefix = MySqlDataProvider.SprocParameterPrefix;
				MySqlDataProvider.SprocParameterPrefix = "_";

				try
				{
					var person3 = db.SetSpCommand("GetPersonById", db.Parameter("ID", 1)).ExecuteObject<Person>();
					Assert.AreEqual(person, person3);
				}
				finally
				{
					MySqlDataProvider.SprocParameterPrefix = oldPrefix;
				}
			}
		}

		[Test]
		public void SpecifyingParameterPrefixManuallyIsAlsoOk()
		{
			var oldPrefix = MySqlDataProvider.SprocParameterPrefix;
			MySqlDataProvider.SprocParameterPrefix = "_";

			try
			{
				using (var db = new DbManager(ProviderName.MySql))
				{
					// we specify parameter name with a prefix, though SprocParameterPrefix is specified
					// in this case additional parameter prefix will not be added, so everything will be ok
					var person = db.SetSpCommand("GetPersonById", db.Parameter("?_ID", 1)).ExecuteObject<Person>();
					Assert.IsNotNull(person);
				}
			}
			finally
			{
				MySqlDataProvider.SprocParameterPrefix = oldPrefix;
			}
		}

		[Test]
		public void PrefixIsAddedWhenRetrievingParameterFromDbManager()
		{
			var oldPrefix = MySqlDataProvider.SprocParameterPrefix;
			MySqlDataProvider.SprocParameterPrefix = "_";

			try
			{
				using (var db = new DbManager(ProviderName.MySql))
				{
					db.SetSpCommand("GetPersonById", db.Parameter("?ID", 1)).Prepare();

					foreach (var personID in new[] { 1, 2 })
					{
						// prefix is not specified but it will be added internally before retrieving parameter from
						// command parameters
						db.Parameter("?ID").Value = personID;
						var person = db.ExecuteObject<Person>();
						Assert.IsNotNull(person);
						Assert.AreEqual(personID, person.ID);

						// specifying prefix is also ok
						db.Parameter("?_ID").Value = personID;
						person = db.ExecuteObject<Person>();
						Assert.IsNotNull(person);
						Assert.AreEqual(personID, person.ID);
					}
				}
			}
			finally
			{
				MySqlDataProvider.SprocParameterPrefix = oldPrefix;
			}
		}
	}
}
