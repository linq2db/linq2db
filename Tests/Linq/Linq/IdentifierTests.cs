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
	}
}
