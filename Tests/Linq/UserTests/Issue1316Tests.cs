using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	/// <summary>
	/// Test fixes to Issue #1305.
	/// Before fix fields in derived tables were added first in the column order by <see cref="DataExtensions.CreateTable{T}(IDataContext, string, string, string, string, string, LinqToDB.SqlQuery.DefaultNullable)"/>.
	/// </summary>
	[TestFixture]
	public class Issue1316Tests : TestBase
	{
		[Table("Issue1316Tests")]
		public class Table
		{
			[Column(IsPrimaryKey = true)]
			public int ID { get; set; }
		}

		public static int GetValue()
		{
			return 5;
		}

		public static Tuple<int, int> GetTuple()
		{
			return Tuple.Create(5, 3);
		}

		public class Test
		{
			public Test Child;

			public int Id;

			public int GetId() => Id;
			public Test GetChild() => Child;
		}

		[Test]
		public void Test_ComplexNavigation([DataSources] string context)
		{
			using (var db  = GetDataContext(context))
			using (var __ = db.CreateLocalTable<Table>())
			{
				db.Insert(new Table() { ID = 5 });

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

				db.GetTable<Table>().Where(_ => _.ID == obj.Child.GetChild().Child.GetId()).Single();
				db.GetTable<Table>().Where(_ => _.ID == obj.Child.GetChild().Child.Id).Single();
			}
		}

		[Test]
		public void Test_ObjectNavigation([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var __ = db.CreateLocalTable<Table>())
			{
				db.Insert(new Table() { ID = 5 });

				var obj = new Test()
				{
					Child = new Test()
					{
						Id = 5
					}
				};

				var ___ = db.GetTable<Table>().Where(_ => _.ID == obj.Child.Id).Single();
			}
		}

		[Test]
		public void Test_MethodWithProperty([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var __ = db.CreateLocalTable<Table>())
			{
				db.Insert(new Table() { ID = 5 });

				var ___ = db.GetTable<Table>().Where(_ => _.ID == GetTuple().Item1).Single();
			}
		}

		[Test]
		public void Test_Method([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var __ = db.CreateLocalTable<Table>())
			{
				db.Insert(new Table() { ID = 5 });

				var ___ = db.GetTable<Table>().Where(_ => _.ID == GetValue()).Single();
			}
		}

		[Test]
		public void Test_Linq([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var __ = db.CreateLocalTable<Table>())
			{
				db.Insert(new Table() { ID = 5 });

				var ids = new[] { 1, 2, 3, 4, 5, 6 };

				var ___ = db.GetTable<Table>().Where(_ => ids.Where(id => id > 3).Contains(_.ID)).Single();
			}
		}
	}
}
