using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools;

using NodaTime;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2560Tests : TestBase
	{
		[Table]
		class DataClass
		{
			[Column] public int           Id    { get; set; }
			[Column] public LocalDateTime Value { get; set; }
		}

		[Test]
		public void NodaTimeInsertTest1([DataSources] string context)
		{
			var ms = new MappingSchema();

			ms.UseNodaTime(/*typeof(LocalDateTime)*/);

			using var db  = GetDataContext(context, ms);
			using var tmp = db.CreateLocalTable<DataClass>();

			var item = new DataClass
			{
				Value = LocalDateTime.FromDateTime(TestData.GetMaxScaleDateTime(context)),
			};

			db.Insert(item);

			var list = tmp.ToList();

			Assert.AreEqual(1, list.Count);
			Assert.That(list[0].Value, Is.EqualTo(item.Value));
		}

		[Test]
		public void NodaTimeInsertTest2([DataSources] string context)
		{
			var ms = new MappingSchema();

			ms.UseNodaTime(typeof(LocalDateTime));

			using var db  = GetDataContext(context, ms);
			using var tmp = db.CreateLocalTable<DataClass>();

			var item = new DataClass
			{
				Value = LocalDateTime.FromDateTime(TestData.GetMaxScaleDateTime(context)),
			};

			db.Insert(item);

			var list = tmp.ToList();

			Assert.AreEqual(1, list.Count);
			Assert.That(list[0].Value, Is.EqualTo(item.Value));
		}
	}
}
