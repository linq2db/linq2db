using System.Collections.Generic;

namespace LinqToDB.EntityFrameworkCore.BaseTests.Models.Northwind
{
    public class CustomerDemographics : BaseEntity
    {
        public CustomerDemographics()
        {
            CustomerCustomerDemo = new HashSet<CustomerCustomerDemo>();
        }

        public string  CustomerTypeId { get; set; } = null!;
        public string? CustomerDesc { get; set; }

        public ICollection<CustomerCustomerDemo> CustomerCustomerDemo { get; set; }
    }
}
