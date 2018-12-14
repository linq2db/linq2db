using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class IssueTests : TestBase
	{
		// https://github.com/linq2db/linq2db/issues/38
		//
		[Test, DataContextSource(false)]
		public void Issue38Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from a in Child
					select new { Count = a.GrandChildren.Count() },
					from a in db.Child
					select new { Count = a.GrandChildren1.Count() });

				var sql = ((TestDataConnection)db).LastQuery;

				Assert.That(sql, Is.Not.Contains("INNER JOIN"));

				Debug.WriteLine(sql);
			}
		}

		// https://github.com/linq2db/linq2db/issues/42
		//
		[Test, DataContextSource]
		public void Issue42Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var t1 = db.Types2.First();

				t1.BoolValue = !t1.BoolValue;

				db.Update(t1);

				var t2 = db.Types2.First();

				Assert.That(t2.BoolValue, Is.EqualTo(t1.BoolValue));

				t1.BoolValue = !t1.BoolValue;

				db.Update(t1);
			}
		}
#if !NETSTANDARD1_6
		// https://github.com/linq2db/linq2db/issues/60
		//
		[Test, IncludeDataContextSource(
			ProviderName.SqlServer2000,
			ProviderName.SqlServer2005,
			ProviderName.SqlServer2008,
			ProviderName.SqlServer2012,
			ProviderName.SqlServer2014,
			TestProvName.SqlAzure,
			ProviderName.SqlCe)]
		public void Issue60Test(string context)
		{
			using (var db = new DataConnection(context))
			{
				var sp       = db.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(db, TestUtils.GetDefaultSchemaOptions(context));

				var q =
					from t in dbSchema.Tables
					from c in t.Columns
					where c.ColumnType.StartsWith("tinyint") && c.MemberType.StartsWith("sbyte")
					select c;

				var column = q.FirstOrDefault();

				Assert.That(column, Is.Null);
			}
		}
#endif
		// https://github.com/linq2db/linq2db/issues/67
		//
		[Test, DataContextSource]
		public void Issue67Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in Parent
					join c in Child on p.ParentID equals c.ParentID into ch
					select new { p.ParentID, count = ch.Count() } into t
					where t.count > 0
					select t,
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID into ch
					select new { p.ParentID, count = ch.Count() } into t
					where t.count > 0
					select t);
			}
		}

		[Test, DataContextSource()]
		public void Issue75Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.Child.Select(c => new
				{
					c.ChildID,
					c.ParentID,
					CountChildren  = db.Child.Count(c2 => c2.ParentID == c.ParentID),
					CountChildren2 = db.Child.Count(c2 => c2.ParentID == c.ParentID),
					HasChildren    = db.Child.Any  (c2 => c2.ParentID == c.ParentID),
					HasChildren2   = db.Child.Any  (c2 => c2.ParentID == c.ParentID),
					AllChildren    = db.Child.All  (c2 => c2.ParentID == c.ParentID),
					AllChildrenMin = db.Child.Where(c2 => c2.ParentID == c.ParentID).Min(c2 => c2.ChildID)
				});

				result =
					from child in result
					join parent in db.Parent on child.ParentID equals parent.ParentID
					where parent.Value1 < 7
					select child;

				var expected = Child.Select(c => new
				{
					c.ChildID,
					c.ParentID,
					CountChildren  = Child.Count(c2 => c2.ParentID == c.ParentID),
					CountChildren2 = Child.Count(c2 => c2.ParentID == c.ParentID),
					HasChildren    = Child.Any  (c2 => c2.ParentID == c.ParentID),
					HasChildren2   = Child.Any  (c2 => c2.ParentID == c.ParentID),
					AllChildren    = Child.All  (c2 => c2.ParentID == c.ParentID),
					AllChildrenMin = Child.Where(c2 => c2.ParentID == c.ParentID).Min(c2 => c2.ChildID)
				});

				expected =
					from child in expected
					join parent in Parent on child.ParentID equals parent.ParentID
					where parent.Value1 < 7
					select child;

				AreEqual(expected, result);
			}
		}

		[Test, DataContextSource]
		public void Issue115Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var qs = (from c in db.Child
						join r in db.Parent on c.ParentID equals r.ParentID
						where r.ParentID > 4
						select c
					)
					.Union(from c in db.Child
						join r in db.Parent on c.ParentID equals r.ParentID
						where r.ParentID <= 4
						select c
					);

				var ql = (from c in Child
						join r in Parent on c.ParentID equals r.ParentID
						where r.ParentID > 4
						select c
					)
					.Union(from c in Child
						join r in Parent on c.ParentID equals r.ParentID
						where r.ParentID <= 4
						select c
					);

				AreEqual(ql, qs);
			}
		}


		[Test, DataContextSource]
		public void Issue424Test1(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Parent.Distinct().OrderBy(_ => _.ParentID).Take(1),
					db.Parent.Distinct().OrderBy(_ => _.ParentID).Take(1)
					);
			}
		}

		[Test, DataContextSource]
		public void Issue424Test2(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Parent.Distinct().OrderBy(_ => _.ParentID).Skip(1).Take(1),
					db.Parent.Distinct().OrderBy(_ => _.ParentID).Skip(1).Take(1)
					);
			}
		}

		[Test, DataContextSource]
		public void Issue424Test3(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Parent.Distinct().OrderByDescending(_ => _.ParentID).Skip(1).Take(1),
					db.Parent.Distinct().OrderByDescending(_ => _.ParentID).Skip(1).Take(1)
				);
			}
		}

		// https://github.com/linq2db/linq2db/issues/498
		//
		[Test, DataContextSource()]
		public void Issue498Test(string context)
		{
			using (new WithoutJoinOptimization())
			using (var db = GetDataContext(context))
			{
				var q = from x in db.Child
					//join y in db.GrandChild on new { x.ParentID, x.ChildID } equals new { ParentID = (int)y.ParentID, ChildID = (int)y.ChildID }
					from y in x.GrandChildren1
					select x.ParentID;

				var r = from x in q
					group x by x
					into g
					select new { g.Key, Cghildren = g.Count() };

				var qq = from x in Child
					from y in x.GrandChildren
					select x.ParentID;

				var rr = from x in qq
					group x by x
					into g
					select new { g.Key, Cghildren = g.Count() };

				AreEqual(rr, r);

				var sql = r.ToString();
				Assert.Less(0, sql.IndexOf("INNER", 1), sql);
			}
		}


		[Test, DataContextSource]
		public void Issue528Test1(string context)
		{
			//using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var expected =    Person.GroupBy(_ => _.FirstName).Select(_ => new { _.Key, Data = _.ToList() });
				var result   = db.Person.GroupBy(_ => _.FirstName).Select(_ => new { _.Key, Data = _.ToList() });

				foreach(var re in result)
				{
					var ex = expected.Single(_ => _.Key == re.Key);

					AreEqual(ex.Data, re.Data);
				}
			}
		}

		[Test, DataContextSource]
		public void Issue528Test2(string context)
		{
			//using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var expected =    Person.GroupBy(_ => _.FirstName).Select(_ => new { _.Key, Data = _.ToList() }).ToList();
				var result   = db.Person.GroupBy(_ => _.FirstName).Select(_ => new { _.Key, Data = _.ToList() }).ToList();

				foreach(var re in result)
				{
					var ex = expected.Single(_ => _.Key == re.Key);

					AreEqual(ex.Data, re.Data);
				}
			}
		}

		[Test, DataContextSource]
		public void Issue528Test3(string context)
		{
			//using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var expected =    Person.GroupBy(_ => _.FirstName).Select(_ => new { _.Key, Data = _ });
				var result   = db.Person.GroupBy(_ => _.FirstName).Select(_ => new { _.Key, Data = _ });

				foreach(var re in result)
				{
					var ex = expected.Single(_ => _.Key == re.Key);

					AreEqual(ex.Data.ToList(), re.Data.ToList());
				}
			}
		}

		[Test, DataContextSource]
		public void Issue508Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = (
					from c in db.Child
					join p in db.Parent on c.ParentID equals p.ParentID
					where c.ChildID == 11
					select p.ParentID
							 ).Union(
					from c in db.Child
					where c.ChildID == 11
					select c.ParentID
								   );
				var expected = (
					from c in Child
					join p in Parent on c.ParentID equals p.ParentID
					where c.ChildID == 11
					select p.ParentID
							 ).Union(
					from c in Child
					where c.ChildID == 11
					select c.ParentID
								   );

				AreEqual(expected, query);
			}
		}

		public class PersonWrapper
		{
			public int    ID;
			public string FirstName;
			public string SecondName;
		}

		[Test, DataContextSource]
		public void Issue535Test(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person
						where p.FirstName.StartsWith("J")
						select new PersonWrapper
						{
							ID         = p.ID,
							FirstName  = p.FirstName,
							SecondName = p.LastName
						};

				q = from p in q
					where p.ID == 1 || p.SecondName == "fail"
					select p;

				Assert.IsNotNull(q.FirstOrDefault());
			}
		}

		[Table(Name = "Person")]
		public class Person376 //: Person
		{
			[SequenceName(ProviderName.Firebird, "PersonID")]
			[Column("PersonID"), Identity, PrimaryKey]
			public int ID;
			[NotNull] public string FirstName { get; set; }
			[NotNull] public string LastName;
			[Nullable] public string MiddleName;


			[Association(ThisKey = nameof(ID), OtherKey = nameof(Model.Doctor.PersonID), CanBeNull = true)]
			public Doctor Doctor { get; set; }
		}

		public class PersonDto
		{
			public int    Id;
			public string Name;

			public DoctorDto Doc;
		}

		public class DoctorDto
		{
			public int    PersonId;
			public string Taxonomy;
		}

		[ExpressionMethod("MapToDtoExpr1")]
		public static PersonDto MapToDto(Person376 person)
		{
			return MapToDtoExpr1().Compile()(person);
		}

		[ExpressionMethod("MapToDtoExpr2")]
		public static DoctorDto MapToDto(Doctor doctor)
		{
			return MapToDtoExpr2().Compile()(doctor);
		}

		private static Expression<Func<Person376, PersonDto>> MapToDtoExpr1()
		{
			return x => new PersonDto
			{

				Id   = x.ID,
				Name = x.FirstName,
				Doc  = x.Doctor != null ? MapToDto(x.Doctor) : null
			};
		}

		private static Expression<Func<Doctor, DoctorDto>> MapToDtoExpr2()
		{
			return x => new DoctorDto
			{
				PersonId = x.PersonID,
				Taxonomy = x.Taxonomy
			};
		}

		[ActiveIssue(Configuration = ProviderName.SapHana)]
		[Test, DataContextSource]
		public void Issue376(string context)
		{
			using (var db = GetDataContext(context))
			{
				var l = db
					.GetTable<Person376>()
					.Where(_ => _.Doctor.Taxonomy.Length >= 0 || _.Doctor.Taxonomy == null)
					.Select(_ => MapToDto(_)).ToList();

				Assert.IsNotEmpty(l);
				Assert.IsNotEmpty(l.Where(_ => _.Doc == null));
				Assert.IsNotEmpty(l.Where(_ => _.Doc != null));
			}
		}


		[Table("Person", IsColumnAttributeRequired = false)]
		public class Person88
		{
			[SequenceName(ProviderName.Firebird, "PersonID")]
			[Column("PersonID"), Identity, PrimaryKey] public int    ID;
			[NotNull]                                  public string FirstName { get; set; }
			[NotNull]                                  public string LastName;
			[Nullable]                                 public string MiddleName;
			                                           public char   Gender;
		}

		[Test, DataContextSource(ProviderName.SQLiteMS)]
		public void Issue88(string context)
		{
			using (var db = GetDataContext(context))
			{
				var llc = db
					.GetTable<Person88>()
					.Where(_ => _.ID == 1 && _.Gender == 'M');

				var lrc = db
					.GetTable<Person88>()
					.Where(_ => _.ID == 1 && 'M' == _.Gender);

				var gender = 'M';
				var llp = db
					.GetTable<Person88>()
					.Where(_ => _.ID == 1 && _.Gender == gender);

				var lrp = db
					.GetTable<Person88>()
					.Where(_ => _.ID == 1 && gender == _.Gender);

				Assert.IsNotEmpty(llc);
				Assert.IsNotEmpty(lrc);
				Assert.IsNotEmpty(llp);
				Assert.IsNotEmpty(lrp);
			}

		}


		[Test, DataContextSource]
		public void Issue173(string context)
		{
			using (var db = GetDataContext(context))
			using (new AllowMultipleQuery())
			{
				var result =
					from r in db.GetTable<Parent>()
					select new
					{
						id = r.ParentID,
					};
				result = result.Where(_ => _.id == 1);

				var expected =
					from r in Parent
					select new
					{
						id = r.ParentID,
					};
				expected = expected.Where(_ => _.id == 1);

				AreEqual(expected, result);
			}
		}

		[Test, DataContextSource]
		public void Issue909(string context)
		{
			using (var db = GetDataContext(context))
			{
				var values = new int?[] { 123 };

				var expected = from p in Parent
					where !values.Contains(p.Value1)
					select p;

				var actual = from p in db.GetTable<Parent>()
						where !values.Contains(p.Value1)
						select p;

				AreEqual(expected, actual);
			}
		}

		[Test, DataContextSource]
		public void Issue909Join(string context)
		{
			using (var db = GetDataContext(context))
			{
				var values = new int?[] { 123 };

				var expected = from c in Child
					from p in Parent
					where p.ParentID == c.ParentID && !values.Contains(p.Value1)
					select c;

				var actual = from c in db.GetTable<Child>()
					from p in db.GetTable<Parent>()
					where p.ParentID == c.ParentID && !values.Contains(p.Value1)
					select c;

				AreEqual(expected, actual);
			}
		}
		[Test, DataContextSource]
		public void Issue909Subquery(string context)
		{
			using (var db = GetDataContext(context))
			{
				var values = new int[] { 123 };

				var expected = from c in Child
					where (from p in Parent
						where p.ParentID == c.ParentID && (p.Value1 == null || !values.Contains(p.Value1.Value))
						select p).Any()
					select c;

				var actual = from c in db.GetTable<Child>()
					where (from p in db.GetTable<Parent>()
						where p.ParentID == c.ParentID && !values.Contains(p.Value1.Value)
						select p).Any()
					select c;

				AreEqual(expected, actual);
			}
		}

		[Table("AllTypes")]
		[Table("ALLTYPES", Configuration = ProviderName.DB2)]
		private class InsertIssueTest
		{
			[Column("smallintDataType")]
			[Column("SMALLINTDATATYPE", Configuration = ProviderName.DB2)]
			public short ID;

			[Column]
			[Column("INTDATATYPE", Configuration = ProviderName.DB2)]
			public int? intDataType;

			[Association(ThisKey = nameof(ID), OtherKey = nameof(intDataType), CanBeNull = true)]
			public IQueryable<InsertIssueTest> Association => throw new InvalidOperationException();
		}

		// Sybase: we need to get rid of bit field from AllTypes table as it creates a lot of issues with testing
		[Test, DataContextSource(ProviderName.Sybase)]
		public void InsertFromSelectWithNullableFilter(string context)
		{
			using (var db = GetDataContext(context))
			{
				Query(true);
				Query(false);

				void Query(bool isNull)
				{
					db.GetTable<InsertIssueTest>()
						.Where(_ => _.ID == GetId(isNull))
						.SelectMany(_ => _.Association)
						.Select(_ => _.ID)
						.Distinct()
						.Insert(
							db.GetTable<InsertIssueTest>(),
							_ => new InsertIssueTest()
							{
								ID = 123,
								intDataType = _
							});
				}
			}
		}

		private short? GetId(bool isNull)
		{
			return isNull ? (short?)null : 1234;
		}

	}

}
