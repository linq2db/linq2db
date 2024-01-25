using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class NullableBoolTests : TestBase
	{
		record NullableBoolClass
		{
			public bool? Value;
		}

		[Test]
		public void TrueTest([DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var data = new NullableBoolClass[]
			{
				new () { Value = null },
				new () { Value = true },
				new () { Value = false },
			};

			using var db = GetDataContext(context);
			using var tt = db.CreateLocalTable(data);

			AreEqual(
				[ data[1] ]
				,
				from t in tt
				where t.Value == true
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				[ data[1] ]
				,
				from t in tt
				where Sql.AsNotNull(t.Value) == true
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				[ data[0], data[2] ]
				,
				from t in tt
				where t.Value != true
				select t);

			Assert.That(LastQuery, Contains.Substring("IS NULL"));

			AreEqual(
				[ data[2] ]
				,
				from t in tt
				where Sql.AsNotNull(t.Value) != true
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));
		}

		[Test]
		public void FalseTest([DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var data = new NullableBoolClass[]
			{
				new () { Value = null },
				new () { Value = true },
				new () { Value = false },
			};

			using var db = GetDataContext(context);
			using var tt = db.CreateLocalTable(data);

			AreEqual(
				[ data[2] ]
				,
				from t in tt
				where t.Value == false
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				[ data[2] ]
				,
				from t in tt
				where Sql.AsNotNull(t.Value) == false
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				[ data[0], data[1] ]
				,
				from t in tt
				where t.Value != false
				select t);

			Assert.That(LastQuery, Contains.Substring("IS NULL"));

			AreEqual(
				[ data[1] ]
				,
				from t in tt
				where Sql.AsNotNull(t.Value) != false
				select t);

			Assert.That(LastQuery, Is.Not.Contains(" NULL"));
		}


		[Test]
		public void NullTest([DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var data = new NullableBoolClass[]
			{
				new () { Value = null },
				new () { Value = true },
				new () { Value = false },
			};

			using var db = GetDataContext(context);
			using var tt = db.CreateLocalTable(data);

			AreEqual(
				[ data[0] ]
				,
				from t in tt
				where t.Value == null
				select t);

			AreEqual(
				[ data[0] ]
				,
				from t in tt
				where Sql.AsNotNull(t.Value) == null
				select t);

			AreEqual(
				[ data[1], data[2] ]
				,
				from t in tt
				where t.Value != null
				select t);

			AreEqual(
				[ data[1], data[2] ]
				,
				from t in tt
				where Sql.AsNotNull(t.Value) != null
				select t);
		}

		[Test]
		public void ValueTest([DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context, [Values] bool? value)
		{
			var data = new NullableBoolClass[]
			{
				new () { Value = null },
				new () { Value = true },
				new () { Value = false },
			};

			using var db = GetDataContext(context);
			using var tt = db.CreateLocalTable(data);

			AreEqual(
				[ data[value switch { null => 0, true => 1, false => 2 }] ]
				,
				from t in tt
				where t.Value == value
				select t);

			if (value is not null)
				Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				[ data[value switch { null => 0, true => 1, false => 2 }] ]
				,
				from t in tt
				where Sql.AsNotNull(t.Value) == value
				select t);

			if (value is not null)
				Assert.That(LastQuery, Is.Not.Contains(" NULL"));

			AreEqual(
				value switch { null => [data[1], data[2]], true => [data[0], data[2]], false => [data[0], data[1]] }
				,
				from t in tt
				where t.Value != value
				select t);

			if (value is not null)
				Assert.That(LastQuery, Contains.Substring("IS NULL"));

			AreEqual(
				value switch { null => [data[1], data[2]], true => [data[2]], false => [data[1]] }
				,
				from t in tt
				where Sql.AsNotNull(t.Value) != value
				select t);

			if (value is not null)
				Assert.That(LastQuery, Is.Not.Contains(" NULL"));
		}

		record NotNullableBoolClass
		{
			public bool Value;
		}

		[Test]
		public void NotNullableTest([DataSources(TestProvName.AllAccess, TestProvName.AllSybase)] string context)
		{
			var data = new NotNullableBoolClass[]
			{
				new () { Value = true  },
				new () { Value = false },
			};

			using var db = GetDataContext(context);
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
