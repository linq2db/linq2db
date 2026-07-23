using System.Collections.Generic;

namespace LinqToDB.NHibernateExtension.BaseTests.Models.Northwind
{
    public class CustomerDemographics : BaseEntity
    {
        public CustomerDemographics()
        {
            CustomerCustomerDemo = new HashSet<CustomerCustomerDemo>();
        }

        public virtual string  CustomerTypeId { get; set; } = null!;
        public virtual string? CustomerDesc { get; set; }

        public virtual ICollection<CustomerCustomerDemo> CustomerCustomerDemo { get; set; }
    }
}
