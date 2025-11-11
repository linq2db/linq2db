using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class IdentifierTests : TestBase
	{
		#region Parameter Name Testing

		// to set parameter name we use two tricks:
		// - for parameter used against column linq2db use property/field name
		// - for parameter used against nested column linq2db use column name
		[Table("testparams")]
		public sealed class Table
		{
			[PrimaryKey] public int Id { get; set; }
			public Nested Component { get; set; } = null!;

			public sealed class Nested
			{
				public int Column1 { get; set; }
			}

			// long ascii identifier (60 chars, we trim parameters to <= 50)
			public int A123456789b123456789c123456789d123456789e123456789f123456789 { get; set; }
		}

		private static readonly IReadOnlyCollection<string> _names = new[]
		{
			// test '-'
			"Test-Name",
			// test non-ascii characters
			"TestИмя",
			// test keywords
			"from",
			// test _
			"p_p",
			// test leading _
			"_p",
			// test leading number
			"1p",
		};

		[Test]
		public void TestParameterCharactersTrimming([DataSources] string context, [ValueSource(nameof(_names))] string identifier)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<Table>()
					.Property(t => t.Component.Column1).HasColumnName(identifier)
					.Build();

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<Table>();

			tb
				.Where(t => t.Component.Column1 == 1)
				.Set(t => t.Component.Column1, 2)
				.Update();
		}

		[Test]
		public void TestParameterLength([DataSources] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<Table>()
					.Property(t => t.Component.Column1)
					.Property(t => t.A123456789b123456789c123456789d123456789e123456789f123456789)
						.HasColumnName("col1")
					.Build();

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<Table>();

			tb
				.Where(t => t.Component.Column1 == 1)
				.Set(t => t.A123456789b123456789c123456789d123456789e123456789f123456789, 2)
				.Update();
		}
		#endregion

		#region Length Tests
		/*
		 * 1. we use identifiers as base name for alias/parameter
		 * 2. .NET metadata limits identifier length to 1023 bytes in UTF-8
		 * 1+2 means => there is no reason for us to test longer names for now till users find a way to generate even longer names :)
		 * 3. we test both latin and non-latin based names as db behavior could differ for them
		 * 4. extended plane characters (surrogate pairs) not tested as Roslyn currently cannot handle them in idetifiers despite being allowed by language spec
		 * https://github.com/dotnet/roslyn/issues/13474 and referenced issues
		 */
		[Test]
		public void TestParameterLengthName_Latin([DataSources] string context)
		{
			var  abcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabc = 1;
			using var db = GetDataContext(context);
			db.Person
				.Where(r => r.ID == abcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabc)
				.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2798")]
		public void TestAliasLengthName_Latin([DataSources] string context)
		{
			using var db = GetDataContext(context);
			db.Person
				.Where(abcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabc => abcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabcdefghijabc.ID == 1)
				.ToArray();
		}

		[Test]
		public void TestParameterLengthName_Localized([DataSources] string context)
		{
			var абвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиаz = 1;
			using var db = GetDataContext(context);
			db.Person
				.Where(r => r.ID == абвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиаz)
				.ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2798")]
		public void TestAliasLengthName_Localized([DataSources] string context)
		{
			using var db = GetDataContext(context);
			db.Person
				.Where(абвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиаz => абвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиабвгдеёжзиаz.ID == 1)
				.ToArray();
		}
		#endregion
	}
}
