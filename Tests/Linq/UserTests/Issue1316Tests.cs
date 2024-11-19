using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1316Tests : TestBase
	{
		[Table("Issue1316Tests")]
		public class Table
		{
			[Column(IsPrimaryKey = true)]
			public int ID { get; set; }
		}

		private static int GetValue()
		{
			return 5;
		}

		private static Tuple<int, int> GetTuple()
		{
			return Tuple.Create(5, 3);
		}

		public class Test
		{
			public Test? Child;

			public int Id;

			public int GetId() => Id;
			public Test? GetChild() => Child;
		}

		[Test]
		public void Test_ComplexNavigation([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var __ = db.CreateLocalTable<Table>())
			{
				db.Insert(new Table { ID = 5 }, __.TableName);

				var obj = new Test()
				{
					Child = new Test()
					{
						Child = new Test()
						{
							Child = new Test()
							{
								Id = 5
							}
						}
					}
				};

				db.GetTable<Table>().TableName(__.TableName).Where(_ => _.ID == obj.Child.GetChild()!.Child!.GetId()).Single();
				db.GetTable<Table>().TableName(__.TableName).Where(_ => _.ID == obj.Child.GetChild()!.Child!.Id).Single();
			}
		}

		[Test]
		public void Test_ObjectNavigation([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var __ = db.CreateLocalTable<Table>())
			{
				db.Insert(new Table { ID = 5 }, __.TableName);

				var obj = new Test
				{
					Child = new Test
					{
						Id = 5
					}
				};

				var ___ = db.GetTable<Table>()
					.TableName(__.TableName)
					.Where(_ => _.ID == obj.Child.Id)
					.Single();
			}
		}

		[Test]
		public void Test_MethodWithProperty([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var __ = db.CreateLocalTable<Table>())
			{
				db.Insert(new Table { ID = 5 }, __.TableName);

				var ___ = db.GetTable<Table>()
					.TableName(__.TableName)
					.Where(_ => _.ID == GetTuple().Item1)
					.Single();
			}
		}

		[Test]
		public void Test_Method([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var __ = db.CreateLocalTable<Table>())
			{
				db.Insert(new Table { ID = 5 }, __.TableName);

				var ___ = db.GetTable<Table>()
					.TableName(__.TableName)
					.Where(_ => _.ID == GetValue())
					.Single();
			}
		}

		[Test]
		public void Test_Linq([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var __ = db.CreateLocalTable<Table>())
			{
				db.Insert(new Table { ID = 5 }, __.TableName);

				var ids = new[] { 1, 2, 3, 4, 5, 6 };

				var ___ = db.GetTable<Table>()
					.TableName(__.TableName)
					.Where(_ => ids.Where(id => id > 3).Contains(_.ID))
					.Single();
			}
		}
	}
}
