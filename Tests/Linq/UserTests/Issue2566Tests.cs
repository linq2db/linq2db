using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.Tools;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2566Tests : TestBase
	{
		[Table]
		sealed class DataClass
		{
			[Column] [PrimaryKey] public int Id { get; set; }

			private string? _value;

			[Column(DataType = DataType.NVarChar)]
			public AnredeAuswahlliste? Value
			{
				get => new AnredeAuswahlliste(_value);
				set => _value = value?.Value;
			}
		}

		public class AnredeAuswahlliste
		{
			public string? Value { get; set; }

			public const string Herr = "Herr";

			public const string Frau = "Frau";
			
			public AnredeAuswahlliste(string? value)
			{
				Value = value;
			}

			public static implicit operator AnredeAuswahlliste(string value)
				=> new AnredeAuswahlliste(value);

			public static implicit operator string?(AnredeAuswahlliste auswahlliste)
				=> auswahlliste.Value;

			public static bool operator ==(AnredeAuswahlliste? leftSide, string? rightSide)
				=> leftSide?.Value == rightSide;

			public static bool operator !=(AnredeAuswahlliste? leftSide, string? rightSide)
				=> leftSide?.Value != rightSide;

			public override bool Equals(object? obj)
			{
				return base.Equals(obj);
			}

			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}

		[Test]
		public void TestCustomType([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var items = new[]
			{
				new DataClass { Id = 1, Value = AnredeAuswahlliste.Frau },
				new DataClass { Id = 2, Value = AnredeAuswahlliste.Herr }
			};

			var ms = new MappingSchema();
			ms.SetConverter<string, AnredeAuswahlliste>(s => new AnredeAuswahlliste(s));
			ms.SetConverter<AnredeAuswahlliste, DataParameter>(v => new DataParameter("", v.Value));

			using (var db = GetDataContext(context, ms))
			using (var dataClasses = db.CreateLocalTable(items))
			{
				var simple = dataClasses
					.Where(m => m.Value == AnredeAuswahlliste.Frau)
					.ToList();

				Assert.That(simple, Has.Count.EqualTo(1));

				var inList = dataClasses
					.Where(m => m.Value!.In<AnredeAuswahlliste>(AnredeAuswahlliste.Frau, AnredeAuswahlliste.Herr))
					.ToList();

				Assert.That(inList, Has.Count.EqualTo(2));
			}
		}

		[Test]
		public void TestCustomTypeConversion([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var items = new[]
			{
				new DataClass { Id = 1, Value = AnredeAuswahlliste.Frau },
				new DataClass { Id = 2, Value = AnredeAuswahlliste.Herr }
			};

			var ms = new MappingSchema();
			var builder = new FluentMappingBuilder(ms);
			builder.Entity<DataClass>().Property(e => e.Value).HasConversion(v => v!.Value, s => new AnredeAuswahlliste(s)).Build();

			using (var db = GetDataContext(context, ms))
			using (var dataClasses = db.CreateLocalTable(items))
			{
				var simple = dataClasses
					.Where(m => m.Value == AnredeAuswahlliste.Frau)
					.ToList();

				Assert.That(simple, Has.Count.EqualTo(1));

				var inList = dataClasses
					.Where(m => m.Value!.In<AnredeAuswahlliste>(AnredeAuswahlliste.Frau, AnredeAuswahlliste.Herr))
					.ToList();

				Assert.That(inList, Has.Count.EqualTo(2));
			}
		}

	}
}
