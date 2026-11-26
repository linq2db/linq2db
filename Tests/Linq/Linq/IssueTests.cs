using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class IssueTests : TestBase
	{
		// https://github.com/linq2db/linq2db/issues/60
		//
		[Test]
		public void Issue60Test([IncludeDataSources(TestProvName.AllSqlServer, ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataConnection(context);
			var sp       = db.DataProvider.GetSchemaProvider();
			var dbSchema = sp.GetSchema(db);

			var q =
					from t in dbSchema.Tables
					from c in t.Columns
					where c.ColumnType!.StartsWith("tinyint") && c.MemberType.StartsWith("sbyte")
					select c;

			var column = q.FirstOrDefault();

			Assert.That(column, Is.Null);
		}

		public class PersonWrapper
		{
			public int    ID;
			public string FirstName  = null!;
			public string SecondName = null!;
		}

		[Test]
		public void Issue535Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
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

			Assert.That(q.FirstOrDefault(), Is.Not.Null);
		}

		[Table]
		sealed class CustomerBase
		{
			[PrimaryKey, Identity] public int        Id           { get; set; }
			[Column, NotNull]      public ClientType ClientType   { get; set; }
			[Column, Nullable]     public string?    Name         { get; set; }
			[Column, Nullable]     public string?    ContactEmail { get; set; }
			[Column, Nullable]     public bool?      Enabled      { get; set; }
		}

		public class PersonBase
		{
			public   int     Id              { get; set; }
			public   string? Name            { get; set; }
			internal string? CompositeEmails { get; set; }
		}

		public class PersonCustomer : PersonBase
		{
			public List<string>? Emails { get; set; }
			public bool          IsEnabled { get; set; }
		}

		enum ClientType
		{
			[MapValue("Client")]
			Client
		}

		[Test]
		public void Issue535Test2([DataSources(TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<CustomerBase>();
			var query = from cb in table
						where cb.ClientType == ClientType.Client
						//orderby cb.Name
						select new PersonCustomer
						{
							Id = cb.Id,
							Name = cb.Name,
							CompositeEmails = cb.ContactEmail,
							IsEnabled = cb.Enabled ?? false
						};

			var filter = "test";

			query = from q in query where q.Name!.Contains(filter) || q.CompositeEmails!.Contains(filter) select q;

			query.ToList();
		}

		[Test]
		public void Issue535Test3([DataSources(TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<CustomerBase>();
			var query = from cb in table
						where cb.ClientType == ClientType.Client
						select new
						{
							Id              = cb.Id,
							Name            = cb.Name,
							CompositeEmails = cb.ContactEmail,
							IsEnabled       = cb.Enabled ?? false
						};

			query.ToList();
		}

		[Table(Name = "Person")]
		public class Person376 //: Person
		{
			[SequenceName(ProviderName.Firebird, "PersonID")]
			[Column("PersonID"), Identity, PrimaryKey]
			public int ID;
			[NotNull] public string FirstName { get; set; } = null!;
			[NotNull] public string LastName = null!;
			[Nullable] public string? MiddleName;

			[Association(ThisKey = nameof(ID), OtherKey = nameof(Model.Doctor.PersonID), CanBeNull = true)]
			public Doctor? Doctor { get; set; }
		}

		public class PersonDto
		{
			public int    Id;
			public string Name = null!;

			public DoctorDto? Doc;
		}

		public class DoctorDto
		{
			public int    PersonId;
			public string Taxonomy = null!;
		}

		[ExpressionMethod("MapToDtoExpr1")]
		private static PersonDto MapToDto(Person376 person)
		{
			return MapToDtoExpr1().CompileExpression()(person);
		}

		[ExpressionMethod("MapToDtoExpr2")]
		private static DoctorDto MapToDto(Doctor doctor)
		{
			return MapToDtoExpr2().CompileExpression()(doctor);
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

		[Test]
		public void Issue376([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var l = db
					.GetTable<Person376>()
					.Where(_ => _.Doctor!.Taxonomy.Length >= 0 || _.Doctor.Taxonomy == null)
					.Select(_ => MapToDto(_)).ToList();

			Assert.That(l, Is.Not.Empty);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(l.Where(_ => _.Doc == null), Is.Not.Empty);
				Assert.That(l.Where(_ => _.Doc != null), Is.Not.Empty);
			}
		}

		[Table("Person", IsColumnAttributeRequired = false)]
		public class Person88
		{
			[SequenceName(ProviderName.Firebird, "PersonID")]
			[Column("PersonID"), Identity, PrimaryKey] public int     ID;
			[NotNull]                                  public string  FirstName { get; set; } = null!;
			[NotNull]                                  public string  LastName = null!;
			[Nullable]                                 public string? MiddleName;
			                                           public char    Gender;
		}

		[Test]
		public void Issue88([DataSources] string context)
		{
			using var db = GetDataContext(context);
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
			using (Assert.EnterMultipleScope())
			{
				Assert.That(llc, Is.Not.Empty);
				Assert.That(lrc, Is.Not.Empty);
				Assert.That(llp, Is.Not.Empty);
				Assert.That(lrp, Is.Not.Empty);
			}

		}

		[Test]
		public void Issue173([DataSources] string context)
		{
			using var db = GetDataContext(context);
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

		[Test]
		public void Issue2823Guid([IncludeDataSources(false, TestProvName.AllFirebird)] string context, [Values] bool inlineParameters)
		{
			using var db = (DataConnection)GetDataContext(context);
			using var table = db.CreateLocalTable<TableWithGuid>();
			Assert.That(db.LastQuery!, Does.Contain("\"String\"   CHAR(38)"));
			Assert.That(db.LastQuery!, Does.Contain("\"StringN\"  CHAR(38)"));

			if (context.IsAnyOf(TestProvName.AllFirebirdLess4))
			{
				Assert.That(db.LastQuery!, Does.Contain("\"Default\"  CHAR(16) CHARACTER SET OCTETS"));
				Assert.That(db.LastQuery!, Does.Contain("\"Binary\"   CHAR(16) CHARACTER SET OCTETS"));
				Assert.That(db.LastQuery!, Does.Contain("\"DefaultN\" CHAR(16) CHARACTER SET OCTETS"));
				Assert.That(db.LastQuery!, Does.Contain("\"BinaryN\"  CHAR(16) CHARACTER SET OCTETS"));
			}
			else
			{
				Assert.That(db.LastQuery!, Does.Contain("\"Default\"  BINARY(16)"));
				Assert.That(db.LastQuery!, Does.Contain("\"Binary\"   BINARY(16)"));
				Assert.That(db.LastQuery!, Does.Contain("\"DefaultN\" BINARY(16)"));
				Assert.That(db.LastQuery!, Does.Contain("\"BinaryN\"  BINARY(16)"));
			}

			db.InlineParameters = inlineParameters;

			table.Insert(() => new TableWithGuid
			{
				Default = TestData.Guid1,
				Binary = TestData.Guid2,
				String = TestData.Guid3,
				DefaultN = TestData.Guid4,
				BinaryN = TestData.Guid5,
				StringN = TestData.Guid6,
			});

			var data = table.ToArray();
			Assert.That(data, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(data[0].Default, Is.EqualTo(TestData.Guid1));
				Assert.That(data[0].Binary, Is.EqualTo(TestData.Guid2));
				Assert.That(data[0].String, Is.EqualTo(TestData.Guid3));
				Assert.That(data[0].DefaultN, Is.EqualTo(TestData.Guid4));
				Assert.That(data[0].BinaryN, Is.EqualTo(TestData.Guid5));
				Assert.That(data[0].StringN, Is.EqualTo(TestData.Guid6));

				Assert.That(table.Where(x => x.Default == TestData.Guid1).Count(), Is.EqualTo(1));
				Assert.That(table.Where(x => x.Binary == TestData.Guid2).Count(), Is.EqualTo(1));
				Assert.That(table.Where(x => x.String == TestData.Guid3).Count(), Is.EqualTo(1));
				Assert.That(table.Where(x => x.DefaultN == TestData.Guid4).Count(), Is.EqualTo(1));
				Assert.That(table.Where(x => x.BinaryN == TestData.Guid5).Count(), Is.EqualTo(1));
				Assert.That(table.Where(x => x.StringN == TestData.Guid6).Count(), Is.EqualTo(1));
			}
		}

		[Table]
		sealed class TableWithGuid
		{
			[Column                           ] public Guid Default   { get; set; }
			[Column(DataType = DataType.Guid) ] public Guid Binary    { get; set; }
			[Column(DataType = DataType.Char) ] public Guid String    { get; set; }

			[Column                           ] public Guid? DefaultN { get; set; }
			[Column(DataType = DataType.Guid) ] public Guid? BinaryN  { get; set; }
			[Column(DataType = DataType.NChar)] public Guid? StringN  { get; set; }
		}

		#region Issue 5972 - PostgreSQL typed literal for integer MinValue/MaxValue

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		public void Issue5972_SByte([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.InlineParameters = true;

			var min = sbyte.MinValue;
			var max = sbyte.MaxValue;

			db.Select(() => Sql.AsSql(sbyte.MinValue)).ShouldBe(sbyte.MinValue);
			db.Select(() => Sql.AsSql(sbyte.MaxValue)).ShouldBe(sbyte.MaxValue);
			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		[ActiveIssue(5973, Configuration = ProviderName.InformixDB2,   ErrorMessage = "Unable to cast object of type 'System.SByte' to type 'System.Byte[]'")]
		[ActiveIssue(5973, Configuration = TestProvName.AllSapHana,    ErrorMessage = "No mapping exists from DbType SByte to a known")]
		[ActiveIssue(5973, Configuration = TestProvName.AllAccessOdbc, ErrorMessage = "Value was either too large or too small")]
		public void Issue5972_SByteParameter([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var min = sbyte.MinValue;
			var max = sbyte.MaxValue;

			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		public void Issue5972_Byte([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.InlineParameters = true;

			var min = byte.MinValue;
			var max = byte.MaxValue;

			db.Select(() => Sql.AsSql(byte.MinValue)).ShouldBe(byte.MinValue);
			db.Select(() => Sql.AsSql(byte.MaxValue)).ShouldBe(byte.MaxValue);
			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		public void Issue5972_ByteParameter([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var min = byte.MinValue;
			var max = byte.MaxValue;

			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		public void Issue5972_Int16([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.InlineParameters = true;

			var min = short.MinValue;
			var max = short.MaxValue;

			db.Select(() => Sql.AsSql(short.MinValue)).ShouldBe(short.MinValue);
			db.Select(() => Sql.AsSql(short.MaxValue)).ShouldBe(short.MaxValue);
			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		[ActiveIssue(5973, Configuration = ProviderName.InformixDB2)]
		public void Issue5972_Int16Parameter([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var min = short.MinValue;
			var max = short.MaxValue;

			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		public void Issue5972_UInt16([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.InlineParameters = true;

			var min = ushort.MinValue;
			var max = ushort.MaxValue;

			db.Select(() => Sql.AsSql(ushort.MinValue)).ShouldBe(ushort.MinValue);
			db.Select(() => Sql.AsSql(ushort.MaxValue)).ShouldBe(ushort.MaxValue);
			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		[ActiveIssue(5973, Configuration = ProviderName.SapHanaNative, ErrorMessage = "Index was outside the bounds of the array")]
		[ActiveIssue(5973, Configuration = ProviderName.SapHanaOdbc,   ErrorMessage = "No mapping exists from DbType UInt16 to a known OdbcType")]
		[ActiveIssue(5973, Configuration = TestProvName.AllAccessOdbc, ErrorMessage = "Value was either too large or too small")]
		public void Issue5972_UInt16Parameter([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var min = ushort.MinValue;
			var max = ushort.MaxValue;

			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		public void Issue5972_Int32([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.InlineParameters = true;

			var min = int.MinValue;
			var max = int.MaxValue;

			db.Select(() => Sql.AsSql(int.MinValue)).ShouldBe(int.MinValue);
			db.Select(() => Sql.AsSql(int.MaxValue)).ShouldBe(int.MaxValue);
			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		[ActiveIssue(5973, Configuration = ProviderName.InformixDB2)]
		public void Issue5972_Int32Parameter([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var min = int.MinValue;
			var max = int.MaxValue;

			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		public void Issue5972_UInt32([DataSources] string context)
		{
			using var db = GetDataContext(context);

			db.InlineParameters = true;

			var min = uint.MinValue;
			var max = uint.MaxValue;

			db.Select(() => Sql.AsSql(uint.MinValue)).ShouldBe(uint.MinValue);
			db.Select(() => Sql.AsSql(uint.MaxValue)).ShouldBe(uint.MaxValue);
			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		[ActiveIssue(5973, Configuration = TestProvName.AllAccessOdbc, ErrorMessage = "Value was either too large or too small")]
		public void Issue5972_UInt32Parameter([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var min = uint.MinValue;
			var max = uint.MaxValue;

			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		public void Issue5972_Int64([DataSources(TestProvName.AllNativeAccess)] string context)
		{
			using var db = GetDataContext(context);

			db.InlineParameters = true;

			var min = long.MinValue;
			var max = long.MaxValue;

			db.Select(() => Sql.AsSql(long.MinValue)).ShouldBe(long.MinValue);
			db.Select(() => Sql.AsSql(long.MaxValue)).ShouldBe(long.MaxValue);
			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		[ActiveIssue(5973, Configuration = ProviderName.InformixDB2)]
		public void Issue5972_Int64Parameter([DataSources(TestProvName.AllNativeAccess)] string context)
		{
			using var db = GetDataContext(context);

			var min = long.MinValue;
			var max = long.MaxValue;

			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		[ActiveIssue(5973, Configuration = TestProvName.AllSQLite,        ErrorMessage = "Arithmetic operation resulted in an overflow")]
		[ActiveIssue(5973, Configuration = TestProvName.AllSqlServer,     ErrorMessage = "Arithmetic overflow error converting numeric to data type numeric")]
		[ActiveIssue(5973, Configuration = TestProvName.AllSybase,        ErrorMessage = "Arithmetic overflow during explicit conversion")]
		[ActiveIssue(5973, Configuration = TestProvName.AllFirebird4Plus, ErrorMessage = "numeric overflow")]
		[ActiveIssue(5973, Configuration = TestProvName.AllFirebirdLess4, ErrorMessage = "Token unknown")]
		[ActiveIssue(5973, Configuration = TestProvName.AllPostgreSQL,    BaselinesMismatch = true)]
		public void Issue5972_UInt64([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			db.InlineParameters = true;

			var min = ulong.MinValue;
			var max = ulong.MaxValue;

			db.Select(() => Sql.AsSql(ulong.MinValue)).ShouldBe(ulong.MinValue);
			db.Select(() => Sql.AsSql(ulong.MaxValue)).ShouldBe(ulong.MaxValue);
			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5972")]
		[ActiveIssue(5973, Configuration = ProviderName.InformixDB2)]
		[ActiveIssue(5973, Configuration = TestProvName.AllFirebird, ErrorMessage = "Value was either too large or too small for an Int64")]
		[ActiveIssue(5973, Configuration = ProviderName.SQLiteMS,    ErrorMessage = "Arithmetic operation resulted in an overflow")]
		public void Issue5972_UInt64Parameter([DataSources(TestProvName.AllNativeAccess)] string context)
		{
			using var db = GetDataContext(context);

			var min = ulong.MinValue;
			var max = ulong.MaxValue;

			db.Select(() => Sql.AsSql(min)).ShouldBe(min);
			db.Select(() => Sql.AsSql(max)).ShouldBe(max);
		}

		#endregion
	}
}
