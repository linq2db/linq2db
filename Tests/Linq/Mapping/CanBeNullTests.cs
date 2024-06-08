using FluentAssertions;
using LinqToDB.Common;
using LinqToDB.Mapping;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Tests.Mapping
{
	public class CanBeNullTests : TestBase
	{
		[TearDown]
		public void ResetNullableOptions()
		{
			Configuration.UseNullableTypesMetadata = false;
			MappingSchema.ClearCache();
		}

		#nullable disable

		sealed class Disabled
		{
			public int             A  { get; set; }
			public string          S  { get; set; } = null!;
			[Association(ThisKey = "Fake", OtherKey = "Id")]
			public Related         R  { get; set; } = null!;
			[Association(ThisKey = "Fake", OtherKey = "Id")]
			public IList<Related>  C1 { get; set; } = null!;
		}
		
		#nullable enable

		sealed class Enabled
		{
			public int             A  { get; set; }
			public int?            NA { get; set; }
			public string          S  { get; set; } = null!;
			public string?         NS { get; set; }
			[Association(ThisKey = "Fake", OtherKey = "Id")]
			public Related         R  { get; set; } = null!;
			[Association(ThisKey = "Fake", OtherKey = "Id")]
			public Related?        NR  { get; set; }
			// Collections CanBeNull cannot be inferred from nullability
			[Association(ThisKey = "Fake", OtherKey = "Id")]
			public IList<Related>  C1 { get; set; } = null!;
			[Association(ThisKey = "Fake", OtherKey = "Id")]
			public IList<Related>? C2 { get; set; } = null!;
		}
		
		sealed class Override
		{
			[Column(CanBeNull = true)]
			public int             A1 { get; set; }
			[Nullable]
			public int             A2 { get; set; }
			[Column(CanBeNull = false)]
			public int?            NA { get; set; }
			[Column(CanBeNull = true)]
			public string          S1 { get; set; } = null!;
			[Nullable]
			public string          S2 { get; set; } = null!;
			[Column(CanBeNull = false)]
			public string?         NS { get; set; }
			[Association(CanBeNull = true, ThisKey = "Fake", OtherKey = "Id")]
			public Related         R  { get; set; } = null!;
			[Association(CanBeNull = false, ThisKey = "Fake", OtherKey = "Id")]
			public Related?        NR  { get; set; }
			// Collections CanBeNull cannot be inferred from nullability
			[Association(CanBeNull = false, ThisKey = "Fake", OtherKey = "Id")]
			public IList<Related>  C1 { get; set; } = null!;
			[Association(CanBeNull = false, ThisKey = "Fake", OtherKey = "Id")]
			public IList<Related>? C2 { get; set; } = null!;
		}

		sealed class Related
		{
			public int Id { get; set; }
		}

		[Test]
		public void InferFromMetadata()
		{
			Configuration.UseNullableTypesMetadata = true;
			var ms = new MappingSchema();
			var e = ms.GetEntityDescriptor(typeof(Enabled));
			Check(e, new[] 
			{ 
				("A",  'C', false),
				("NA", 'C', true),
				("S",  'C', false),
				("NS", 'C', true),
				("R",  'A', false),
				("NR", 'A', true),
				("C1", 'A', true),
				("C2", 'A', true),
			});
		}

		[Test]
		public void OverrideMetadata()
		{
			Configuration.UseNullableTypesMetadata = true;
			var ms = new MappingSchema();
			var e = ms.GetEntityDescriptor(typeof(Override));
			Check(e, new[] 
			{ 
				("A1", 'C', true),
				("A2", 'C', true),
				("NA", 'C', false),
				("S1", 'C', true),
				("S2", 'C', true),
				("NS", 'C', false),
				("R",  'A', true),
				("NR", 'A', false),
				("C1", 'A', false),
				("C2", 'A', false),
			});
		}

		[Test]
		public void IgnoreNullableDisabledModel() 
		{
			Configuration.UseNullableTypesMetadata = true;
			var ms = new MappingSchema();
			var e = ms.GetEntityDescriptor(typeof(Disabled));
			Check(e, new[]
			{ 
				("A",  'C', false),
				("S",  'C', true),
				("R",  'A', true),
				("C1", 'A', true),
			});
		}

		[Test]
		public void DefaultValues()
		{
			var ms = new MappingSchema();
			var e = ms.GetEntityDescriptor(typeof(Enabled));
			Check(e, new[] 
			{ 
				("A",  'C', false),
				("NA", 'C', true),
				("S",  'C', true),
				("NS", 'C', true),
				("R",  'A', true),
				("NR", 'A', true),
				("C1", 'A', true),
				("C2", 'A', true),
			});
		}

		private void Check(EntityDescriptor e, (string, char, bool)[] props)
		{
			foreach (var (key, isCol, expected) in props)
			{
				if (isCol == 'C')
					e.Columns.Single(c => c.MemberName == key)
					 .CanBeNull.Should().Be(expected, "column {0}", key);
				else
					e.Associations.Single(a => a.MemberInfo.Name == key)
					 .CanBeNull.Should().Be(expected, "association {0}", key);
			}
		}
	}
}
