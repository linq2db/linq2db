using LinqToDB.EntityFrameworkCore;

using Npgsql;

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

#if NET8_0_OR_GREATER
		// TODO: use https://github.com/npgsql/efcore.pg/issues/2542 after npgsql 9 migration
#pragma warning disable CS0618 // Type or member is obsolete
		NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
#pragma warning restore CS0618 // Type or member is obsolete
#endif
	}

	[OneTimeTearDown]
	public void TestAssemblyTeardown()
	{
	}
}
