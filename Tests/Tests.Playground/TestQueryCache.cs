using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class TestQueryCache : TestBase
	{
		[Table]
		class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		private IQueryable<SampleClass> TesId(IDataContext dc, int id)
		{
			return dc.GetTable<SampleClass>().Where(c => c.Id == id);
		}

		[Test, Combinatorial]
		public void SampleSelectTest([IncludeDataSources(false, ProviderName.SQLiteMS)] string context)
		{
			var mappingSchema = new MappingSchema();
			mappingSchema.SetConverter<int, DataParameter>(i => new DataParameter("", i, DataType.Int32));

			using (var db = GetDataContext(context, mappingSchema))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				var result1 = TesId(db, 1).ToString();
				var result2 = TesId(db, 2).ToString();

				Assert.AreNotEqual(result1, result2);
			}
		}

	}
}
