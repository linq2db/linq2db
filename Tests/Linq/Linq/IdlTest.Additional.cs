using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	partial class IdlTest
	{
		partial class GenericQueryBase
		{
			protected IQueryable<IdlPatient> AllPatients2
			{
				get { return m_ds.Patients(); }
			}
		}

		public class GenericConcatQuery1 : GenericQueryBase
		{
			private String @p1;
			private Int32  @p2;

			public GenericConcatQuery1(ITestDataContext ds, object[] args)
				: base(ds)
			{
				@p1 = (String)args[0];
				@p2 = (Int32) args[1];
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
		public void TestMono03Mono()
		{
			ForMySqlProvider(
				db => Assert.That(new GenericConcatQuery1(db, new object[] { "A", 1 }).Query().ToList(), Is.Not.Null));
		}
	}
}
