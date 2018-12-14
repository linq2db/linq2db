using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq;

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

		[Test, DataContextSource]
		public void Test_ComplexNavigation(string context)
		{
			using (var db = GetDataContext(context))
			using (var tbl = db.CreateLocalTable<Table>())
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

		[Test, DataContextSource]
		public void Test_ObjectNavigation(string context)
		{
			using (var db = GetDataContext(context))
			using (var tbl = db.CreateLocalTable<Table>())
			{
				db.Insert(new Table() { ID = 5 });

				var obj = new Test()
				{
					Child = new Test()
					{
						Id = 5
					}
				};

				db.GetTable<Table>().Where(_ => _.ID == obj.Child.Id).Single();
			}
		}

		[Test, DataContextSource]
		public void Test_MethodWithProperty(string context)
		{
			using (var db = GetDataContext(context))
			using (var tbl = db.CreateLocalTable<Table>())
			{
				db.Insert(new Table() { ID = 5 });

				db.GetTable<Table>().Where(_ => _.ID == GetTuple().Item1).Single();
			}
		}

		[Test, DataContextSource]
		public void Test_Method(string context)
		{
			using (var db = GetDataContext(context))
			using (var tbl = db.CreateLocalTable<Table>())
			{
				db.Insert(new Table() { ID = 5 });

				db.GetTable<Table>().Where(_ => _.ID == GetValue()).Single();
			}
		}

		[Test, DataContextSource]
		public void Test_Linq(string context)
		{
			using (var db = GetDataContext(context))
			using (var tbl = db.CreateLocalTable<Table>())
			{
				db.Insert(new Table() { ID = 5 });

				var ids = new[] { 1, 2, 3, 4, 5, 6 };

				db.GetTable<Table>().Where(_ => ids.Where(id => id > 3).Contains(_.ID)).Single();
			}
		}
	}
}
