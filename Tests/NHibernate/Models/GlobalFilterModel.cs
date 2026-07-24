using FluentNHibernate.Mapping;

using NHibernate;

namespace LinqToDB.NHibernate.Tests.Models.GlobalFilter
{
	// An entity carrying a soft-delete flag and a tenant discriminator, with two NHibernate filters applied.
	// Column names in the filter conditions (is_deleted, tenant_id) deliberately differ from the member names
	// to exercise the column->member resolution in the filter bridge.
	public class Document
	{
		public virtual int    Id        { get; set; }
		public virtual string Title     { get; set; } = null!;
		public virtual bool   IsDeleted { get; set; }
		public virtual int    TenantId  { get; set; }
	}

	// Parameterless filter: "is_deleted = 0".
	public class SoftDeleteFilter : FilterDefinition
	{
		public SoftDeleteFilter()
		{
			WithName("softDelete").WithCondition("is_deleted = 0");
		}
	}

	// Parameterized filter: "tenant_id = :tenantId".
	public class TenantFilter : FilterDefinition
	{
		public TenantFilter()
		{
			WithName("tenant").AddParameter("tenantId", NHibernateUtil.Int32).WithCondition("tenant_id = :tenantId");
		}
	}

	public class DocumentMap : ClassMap<Document>
	{
		public DocumentMap()
		{
			Table("Documents");
			Id(x => x.Id).GeneratedBy.Identity().Column("Id");
			Map(x => x.Title).Column("Title").Not.Nullable();
			Map(x => x.IsDeleted).Column("is_deleted").Not.Nullable();
			Map(x => x.TenantId).Column("tenant_id").Not.Nullable();

			ApplyFilter<SoftDeleteFilter>();
			ApplyFilter<TenantFilter>();
		}
	}
}
