using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class Issue1554Tests : TestBase
	{
	    // Model
	    public class PersonCache
	    {
	        public int Id { get; set; }

	        public string PhoneNumber { get; set; }

	        public KeyTypes ClaimedKeyType { get; set; }

	        public DateTimeOffset Updated { get; set; }
	    }

	    [Flags]
	    public enum KeyTypes : byte
	    {
	        RSA = 1,
	        EC = 2
	    }

		[Test]
		public void SampleUpdateTest([DataSources] string context)
		{
			var ms = new MappingSchema();
			var builder = ms.GetFluentMappingBuilder();

			    // Fluent
			builder.Entity<PersonCache>()
                .HasTableName("PersonCaches")
                .Property(p => p.Id)
                    .IsPrimaryKey()
                    .IsIdentity()
                .Property(p => p.PhoneNumber)
                    .IsNullable()
                .Property(p => p.ClaimedKeyType)
                .Property(p => p.Updated);

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<PersonCache>())
			{
				var claimedKeyType = KeyTypes.EC;
				var now = DateTimeOffset.Now;

				db.GetTable<PersonCache>().Where(p => p.Id == 0).AsUpdatable()
					.Set(p => p.ClaimedKeyType, claimedKeyType)
					.Set(p => p.Updated, now)
					.Update();
			}
		}
	}
}
