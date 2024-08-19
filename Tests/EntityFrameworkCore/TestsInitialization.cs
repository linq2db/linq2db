using LinqToDB.EntityFrameworkCore;

using NUnit.Framework;

/// <summary>
/// 1. Don't add namespace to this class! It's intentional
/// 2. This class implements test assembly setup/teardown methods.
/// </summary>
[SetUpFixture]
public class TestsInitialization
{
	[OneTimeSetUp]
	public void TestAssemblySetup()
	{
		LinqToDBForEFTools.Initialize();
	}

	[OneTimeTearDown]
	public void TestAssemblyTeardown()
	{
	}
}
