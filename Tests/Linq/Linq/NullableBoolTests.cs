using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class NullableBoolTests : TestBase
	{
		record NullableBoolClass
		{
			[PrimaryKey] public int Id;
			public bool? Value;

			public static readonly NullableBoolClass[] Data = new NullableBoolClass[]
			{
				new () { Id = 1, Value = null },
				new () { Id = 2, Value = true },
				new () { Id = 3, Value = false },
			};
		}

		const string ProvidersThatDoNotSupportNullableBool = $"{TestProvName.AllAccess},{TestProvName.AllSybase}";

		[Test]
		public void TrueTest([DataSources(ProvidersThatDoNotSupportNullableBool)] string context)
		{
			using var db = GetDataContext(context);
			using var tt = db.CreateLocalTable(NullableBoolClass.Data);

			AreEqual(
				[NullableBoolClass.Data[1] ]
				,
				from t in tt
				where t.Value == true
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				[NullableBoolClass.Data[1] ]
				,
				from t in tt
				where Sql.AsNotNull(t.Value) == true
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				[NullableBoolClass.Data[0], NullableBoolClass.Data[2] ]
				,
				from t in tt
				where t.Value != true
				select t);

			Assert.That(LastQuery, Contains.Substring("IS NULL"));

			AreEqual(
				[NullableBoolClass.Data[2] ]
				,
				from t in tt
				where Sql.AsNotNull(t.Value) != true
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));
		}

		[Test]
		public void FalseTest([DataSources(ProvidersThatDoNotSupportNullableBool)] string context)
		{
			using var db = GetDataContext(context);
			using var tt = db.CreateLocalTable(NullableBoolClass.Data);

			AreEqual(
				[NullableBoolClass.Data[2] ]
				,
				from t in tt
				where t.Value == false
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				[NullableBoolClass.Data[2] ]
				,
				from t in tt
				where Sql.AsNotNull(t.Value) == false
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				[NullableBoolClass.Data[0], NullableBoolClass.Data[1] ]
				,
				from t in tt
				where t.Value != false
				select t);

			Assert.That(LastQuery, Contains.Substring("IS NULL"));

			AreEqual(
				[NullableBoolClass.Data[1] ]
				,
				from t in tt
				where Sql.AsNotNull(t.Value) != false
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));
		}

		[Test]
		public void NullTest([DataSources(ProvidersThatDoNotSupportNullableBool)] string context)
		{
			using var db = GetDataContext(context);
			using var tt = db.CreateLocalTable(NullableBoolClass.Data);

			AreEqual(
				[NullableBoolClass.Data[0] ]
				,
				from t in tt
				where t.Value == null
				select t);

			//TODO: weird test, we should not check for NULL in this case
			/*AreEqual(
				[ NullableBoolClass.Data[0] ]
				,
				from t in tt
				where Sql.AsNotNull(t.Value) == null
				select t);*/

			AreEqual(
				[NullableBoolClass.Data[1], NullableBoolClass.Data[2] ]
				,
				from t in tt
				where t.Value != null
				select t);

			//TODO: weird test, we should not check for NULL in this case
			/*AreEqual(
				[ NullableBoolClass.Data[1], NullableBoolClass.Data[2] ]
				,
				from t in tt
				where Sql.AsNotNull(t.Value) != null
				select t);*/
		}

		[Test]
		public void ValueTest([DataSources(ProvidersThatDoNotSupportNullableBool)] string context, [Values] bool? value)
		{
			using var db = GetDataContext(context);
			using var tt = db.CreateLocalTable(NullableBoolClass.Data);

			AreEqual(
				[NullableBoolClass.Data[value switch { null => 0, true => 1, false => 2 }] ]
				,
				from t in tt
				where t.Value == value
				select t);

			if (value is not null)
				Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				value is null ? [] : [NullableBoolClass.Data[value switch { true => 1, false => 2 }] ]
				,
				from t in tt
				where Sql.AsNotNull(t.Value) == value
				select t, allowEmpty: true);

			if (value is not null)
				Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				value switch { null => [NullableBoolClass.Data[1], NullableBoolClass.Data[2]], true => [NullableBoolClass.Data[0], NullableBoolClass.Data[2]], false => [NullableBoolClass.Data[0], NullableBoolClass.Data[1]] }
				,
				from t in tt
				where t.Value != value
				select t);

			if (value is not null)
				Assert.That(LastQuery, Contains.Substring("IS NULL"));

			AreEqual(
				value switch { null => NullableBoolClass.Data, true => [NullableBoolClass.Data[2]], false => [NullableBoolClass.Data[1]] }
				,
				from t in tt
				where Sql.AsNotNull(t.Value) != value
				select t);

			if (value is not null)
				Assert.That(LastQuery, Is.Not.Contains(" NULL"));
		}

		record NotNullableBoolClass
		{
			[PrimaryKey] public int Id;
			public bool Value;

			public static readonly NotNullableBoolClass[] Data = new NotNullableBoolClass[]
			{
				new () { Id = 2, Value = true },
				new () { Id = 3, Value = false },
			};

		}

		[Test]
		public void NotNullableTest([DataSources(ProvidersThatDoNotSupportNullableBool)] string context, [Values] bool compareNullsAsValues)
		{
			var data = NotNullableBoolClass.Data;

			using var db = GetDataContext(context, o => o.UseCompareNulls(compareNullsAsValues ? CompareNulls.LikeClr : CompareNulls.LikeSql));
			using var tt = db.CreateLocalTable(data);

			AreEqual(
				[ data[0] ]
				,
				from t in tt
				where t.Value
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				[ data[0] ]
				,
				from t in tt
				where t.Value == true
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				[ data[1] ]
				,
				from t in tt
				where t.Value != true
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				[ data[1] ]
				,
				from t in tt
				where !t.Value
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				[ data[1] ]
				,
				from t in tt
				where t.Value == false
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				[ data[0] ]
				,
				from t in tt
				where t.Value != false
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));
		}
	}
}
