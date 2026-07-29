using FluentNHibernate.Mapping;
using LinqToDB.NHibernate.Tests.Models.Northwind;

namespace LinqToDB.NHibernate.Tests.Models.ClassMaps
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
