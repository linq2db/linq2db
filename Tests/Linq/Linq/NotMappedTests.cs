using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class NotMappedTests : TestBase
	{
		[Table]
		sealed class SuperClass
		{
			[Column, PrimaryKey] public int Id { get; set; }
			[Column] public string? Value      { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Subclass1.ParentId))]
			public Subclass1? Association1     { get; set; }
		}

		[Table]
		sealed class Subclass1
		{
			[Column, PrimaryKey] public int Id { get; set; }
			[Column] public int? ParentId      { get; set; }
			[Column] public string? Value      { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(Subclass2.ParentId))]
			public Subclass2? Association2     { get; set; }
		}

		[Table]
		sealed class Subclass2
		{
			[Column, PrimaryKey] public int Id  { get; set; }
			[Column] public int? ParentId       { get; set; }
			[Column] public string? Value       { get; set; }
			[NotColumn] public Type? NotMapped  { get; set; }
		}

		static Tuple<SuperClass[], Subclass1[], Subclass2[]> GenerateTestData()
		{
			var items1 = Enumerable.Range(1, 10).Select(i => new SuperClass
			{
				Id = i,
				Value = "Super " + i
			}).ToArray();

			var items2 = Enumerable.Range(1, 10).Select(i => new Subclass1
			{
				Id = i * 10,
				ParentId = i % 2 == 0 ? (int?)i : null,
				Value = "Sub1 " + i
			}).ToArray();

			var items3 = Enumerable.Range(1, 10).Select(i => new Subclass2
			{
				Id = i * 100,
				ParentId = i % 4 == 0 ? (int?)(i * 10) : null,
				Value = "Sub2 " + i
			}).ToArray();

			return Tuple.Create(items1, items2, items3);
		}

		[Test]
		public void TestAutomapperGenerated([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			{
				var query = from t in db.GetTable<SuperClass>()
					select new SuperClass
					{
						Id = t.Id,
						Value = t.Value,
						Association1 = t.Association1 == null
							? null
							: new Subclass1
							{
								Id = t.Association1.Id,
								Value = t.Association1.Value,
								ParentId = t.Association1.ParentId,
								Association2 = t.Association1.Association2 == null
									? null
									: new Subclass2
									{
										Id = t.Association1.Association2.Id,
										Value = t.Association1.Association2.Value,
										ParentId = t.Association1.Association2.ParentId,
										NotMapped = t.Association1.Association2.NotMapped
									}
							}
					};

				var result = query.ToArray();
			}
		}
		
		[Test]
		public void TestViaSelect([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var testData = GenerateTestData();

			using (var db = GetDataContext(context, o => o.OmitUnsupportedCompareNulls(context)))
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			{
				var query1 = from t in db.GetTable<SuperClass>()
					select new SuperClass
					{
						Id = t.Id,
						Value = t.Value,
					};
				
				var query2 = from q in query1
					from s in db.GetTable<Subclass1>().LeftJoin(s => s.ParentId == q.Id) 
					select new SuperClass
					{
						Id = q.Id,
						Value = q.Value,
						Association1 = s == null ? null : new Subclass1
						{
							Id = s.Id,
							ParentId = s.ParentId,
							Value = s.Value
						}
					};

				var query3 = from q in query2
					from s in db.GetTable<Subclass2>().LeftJoin(s => s.ParentId == q.Association1!.Id)
					select new SuperClass
					{
						Id = q.Id,
						Value = q.Value,
						Association1 = q.Association1 == null
							? null
							: new Subclass1
							{
								Id = q.Association1.Id,
								ParentId = q.Association1.ParentId,
								Value = q.Association1.Value,
								Association2 = s
							}
					};
				
				var query = from t in query3
					select new SuperClass
					{
						Id = t.Id,
						Value = t.Value,
						Association1 = t.Association1 == null
							? null
							: new Subclass1
							{
								Id = t.Association1.Id,
								Value = t.Association1.Value,
								ParentId = t.Association1.ParentId,
								Association2 = t.Association1.Association2 == null
									? null
									: new Subclass2
									{
										Id = t.Association1.Association2.Id,
										Value = t.Association1.Association2.Value,
										ParentId = t.Association1.Association2.ParentId,
										NotMapped = t.Association1.Association2.NotMapped
									}
							}
					};

				var result = query.ToArray();
			}
		}
		
	}
}
