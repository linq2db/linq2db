using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class ValueConversionTests : TestBase
	{
		[Table]
		class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column(DataType = DataType.NVarChar)] public JToken Value { get; set; }
		}

		[Test]
		public void SampleSelectTest([DataSources] string context)
		{
			var ms = new MappingSchema();
			var builder = ms.GetFluentMappingBuilder();

			builder.Entity<SampleClass>()
				.Property(e => e.Value)

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				var result = table.ToArray();
			}
		}
	}
}
