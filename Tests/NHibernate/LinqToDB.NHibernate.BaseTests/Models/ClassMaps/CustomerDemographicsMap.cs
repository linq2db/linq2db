using FluentNHibernate.Mapping;
using LinqToDB.NHibernateExtension.BaseTests.Models.Northwind;

namespace LinqToDB.NHibernateExtension.BaseTests.Models.ClassMaps
{
	public class CustomerDemographicsMap : ClassMap<CustomerDemographics>
	{
		public CustomerDemographicsMap()
		{
			Table("CustomerDemographics");
			Id(x => x.CustomerTypeId).GeneratedBy.Assigned().Column("CustomerTypeID");
			Map(x => x.CustomerDesc).Column("CustomerDesc");
			Map(x => x.IsDeleted).Column("IsDeleted").Not.Nullable();
		}
	}
}
