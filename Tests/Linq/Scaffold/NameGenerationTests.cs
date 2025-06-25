using System.Collections.Generic;

using LinqToDB.Naming;
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
			// https://github.com/linq2db/linq2db/issues/4061
			new TestCaseData(
				true,
				new SqlObjectName("OfferedItemCustomValue", Schema: "dbo"), new SqlObjectName("OfferedItemCustomColumn", Schema: "dbo"),
				new[]{ "OfferId", "GroupPosition", "ColumnName", "ColumnType" },
				new[]{ "OfferId", "GroupPosition", "ColumnName", "ColumnType" },
				"FK_OfferedItemCustomValue_OfferedItemCustomColumn",
				NameTransformation.Association,
				new HashSet<string>() { "dbo" },
				"OfferedItemCustomColumn", "OfferedItemCustomValue"),
			new TestCaseData(
				true,
				new SqlObjectName("Offer", Schema: "dbo"), new SqlObjectName("OfferTemplate", Schema: "dbo"),
				new[]{ "CompanyId", "OfferTemplateId" },
				new[]{ "CompanyId", "OfferTemplateId" },
				"FK_Offer_OfferTemplate",
				NameTransformation.Association,
				new HashSet<string>() { "dbo" },
				"OfferTemplate", "Offer"),
			new TestCaseData(
				false,
				new SqlObjectName("OfferedItemCustomValue", Schema: "dbo"), new SqlObjectName("OfferedItemCustomColumn", Schema: "dbo"),
				new[]{ "OfferId", "GroupPosition", "ColumnName", "ColumnType" },
				new[]{ "OfferId", "GroupPosition", "ColumnName", "ColumnType" },
				"FK_OfferedItemCustomValue_OfferedItemCustomColumn",
				NameTransformation.Association,
				new HashSet<string>() { "dbo" },
				"OfferedItemCustomColumn", "OfferedItemCustomValue"),
			new TestCaseData(
				false,
				new SqlObjectName("Offer", Schema: "dbo"), new SqlObjectName("OfferTemplate", Schema: "dbo"),
				new[]{ "CompanyId", "OfferTemplateId" },
				new[]{ "CompanyId", "OfferTemplateId" },
				"FK_Offer_OfferTemplate",
				NameTransformation.Association,
				new HashSet<string>() { "dbo" },
				"OfferTemplate", "Offer"),
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
			using (Assert.EnterMultipleScope())
			{
				Assert.That(sourceSideAssociationName, Is.EqualTo(expectedSourceName));
				Assert.That(targetSideAssociationName, Is.EqualTo(expectedTargetName));
			}
		}
	}
}
