using FluentNHibernate.Mapping;
using LinqToDB.NHibernateExtension.BaseTests.Models.Northwind;

namespace LinqToDB.NHibernateExtension.BaseTests.Models.ClassMaps
{
	public class CategoriesMap : ClassMap<Category>
	{
		public CategoriesMap()
		{
			Table("Categories");
			Id(x => x.CategoryId).GeneratedBy.Identity().Column("CategoryID");
			Map(x => x.CategoryName).Column("CategoryName").Not.Nullable();
			Map(x => x.Description).Column("Description");
			Map(x => x.Picture).Column("Picture");
			Map(x => x.IsDeleted).Column("IsDeleted").Not.Nullable();
		}
	}
}
