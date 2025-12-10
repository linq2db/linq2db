using System.Collections.Generic;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	// ReSharper disable once TestClassNameDoesNotMatchFileNameWarning
	partial class IdlTests
	{
		partial class GenericQueryBase
		{
			protected IQueryable<IdlPatient> AllPatients2 => m_ds.Patients();
		}

		public class GenericConcatQuery1 : GenericQueryBase
		{
			private string @p1;
			private int    @p2;

			public GenericConcatQuery1(ITestDataContext ds, object[] args)
				: base(ds)
			{
				@p1 = (string)args[0];
				@p2 = (int)   args[1];
			}

			public override IEnumerable<object> Query()
			{
				//return Queryable.Concat(
				return Concat2(
					from y in AllPersons select y.Name,
					from x in AllPersons
					from z in AllPatients
					where (x.Name == @p1 || z.Id == new ObjectId { Value = @p2 })
					select x.Name);
			}
		}

		[Test]
		public void TestMono03Mono([IncludeDataSources(TestProvName.AllMySql, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(new GenericConcatQuery1(db, new object[] { "A", 1 }).Query().ToList(), Is.Not.Null);
		}
	}
}
