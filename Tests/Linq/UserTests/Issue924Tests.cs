using System.Linq;
using LinqToDB.Mapping;
using LinqToDB.Metadata;
using NUnit.Framework;

namespace Tests.UserTests
{
    [TestFixture]
    public class Issue924Tests : TestBase
    {
        class FluentBase
        {
            public int Id { get; set; }
        }

        class FluentDerived : FluentBase
        {
            public string StringValue { get; set; }
        }

        MappingSchema SetFluentMappings()
        {
            var ms = new MappingSchema();
            var fluentBuilder = ms.GetFluentMappingBuilder();

            fluentBuilder.Entity<FluentBase>()
                .HasTableName("FluentBase")
                .Property(x => x.Id).IsColumn().IsNullable(false).HasColumnName("Id").IsPrimaryKey();


            fluentBuilder.Entity<FluentDerived>()
                .HasTableName("FluentDerived")
                .Property(x => x.Id).IsColumn().IsNullable(false).HasColumnName("Id").IsPrimaryKey();

            return ms;
        }

        [Test]
        public void TestITypeListMetadataReader()
        {
            var ms = SetFluentMappings();

            var tmr = ms.MetadataReader as ITypeListMetadataReader;

            var types = tmr.GetMappedTypes();

            Assert.AreEqual(2, types.Count());
        }
    }
}
