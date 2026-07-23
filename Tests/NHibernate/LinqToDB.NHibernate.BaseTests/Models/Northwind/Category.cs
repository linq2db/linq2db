using System.Collections.Generic;
using LinqToDB.NHibernateExtension.BaseTests.Models.Northwind;

namespace LinqToDB.NHibernateExtension.BaseTests.Models.Northwind
{
    public class Category : BaseEntity
    {
        public Category()
        {
            Products = new HashSet<Product>();
        }

        public virtual int CategoryId { get; set; }
        public virtual string CategoryName   { get; set; } = null!;
        public virtual string? Description    { get; set; }
        public virtual byte[]? Picture        { get; set; }

        public virtual ICollection<Product> Products { get; set; }
    }
}
