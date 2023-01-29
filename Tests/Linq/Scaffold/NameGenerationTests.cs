using System.Collections.Generic;
using LinqToDB.Naming;
using LinqToDB.Reflection;
using LinqToDB.Scaffold.Internal;
using LinqToDB.SqlQuery;
using NUnit.Framework;

namespace Tests.Scaffold
{
	[TestFixture]
	public class NameGenerationTests : TestBase
	{
		private static readonly TestCaseData[] _associationTestCases = new[]
		{
			// https://github.com/linq2db/linq2db/issues/3728
			new TestCaseData(
				true,
				new SqlObjectName("Provider", Schema: "dbo"), new SqlObjectName("Member", Schema: "dbo"),
				new[]{ "ProviderId" }, new[]{ "MemberId" },
				"FK_Provider_Member",
				NameTransformation.Association,
				new HashSet<string>() { "dbo" },
				"Member", "Provider"),
		};

		[TestCaseSource(nameof(_associationTestCases))]
		public void TestForeignKeyToAssociationNameGeneration(
			bool oneToOne,
			SqlObjectName sourceTable,
			SqlObjectName targetTable,
			string[] sourceColumns,
			string[] targetColumns,
			string fkName,
			NameTransformation transformation,
			ISet<string> defaultSchemas,
			string expectedSourceName,
			string expectedTargetName)
		{
			var sourceSideAssociationName = NameGenerationServices.GenerateAssociationName(
				(_,_) => oneToOne,
				sourceTable,
				targetTable,
				false,
				sourceColumns,
				fkName,
				transformation,
				defaultSchemas);

			var targetSideAssociationName = NameGenerationServices.GenerateAssociationName(
				(_,_) => true,
				targetTable,
				sourceTable,
				true,
				targetColumns,
				fkName,
				transformation,
				defaultSchemas);

			Assert.AreEqual(expectedSourceName, sourceSideAssociationName);
			Assert.AreEqual(expectedTargetName, targetSideAssociationName);
		}
	}
}
