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
		public virtual int    IsDeleted { get; set; }
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

	// Filter with NO default condition; the condition is supplied per-entity via ApplyFilter&lt;T&gt;("...").
	public class ArchivedFilter : FilterDefinition
	{
		public ArchivedFilter()
		{
			WithName("archived");
		}
	}

	// Condition whose string literal contains '{'/'}' braces — these must be escaped so Sql.Expr treats them as
	// literal text, not argument placeholders (otherwise query build throws a FormatException).
	public class LiteralGuardFilter : FilterDefinition
	{
		public LiteralGuardFilter()
		{
			WithName("literalGuard").WithCondition("Title <> 'x{y}z'");
		}
	}

	public class DocumentMap : ClassMap<Document>
	{
		public DocumentMap()
		{
			Table("Documents");
			Id(x => x.Id).GeneratedBy.Native().Column("Id");
			Map(x => x.Title).Column("Title").Not.Nullable();
			Map(x => x.IsDeleted).Column("is_deleted").Not.Nullable();
			Map(x => x.TenantId).Column("tenant_id").Not.Nullable();

			ApplyFilter<SoftDeleteFilter>();
			ApplyFilter<TenantFilter>();
			ApplyFilter<ArchivedFilter>("is_deleted = 0");
			ApplyFilter<LiteralGuardFilter>();
		}
	}
}
