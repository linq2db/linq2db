using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Tests;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.IssueModel
{
	public abstract class IssueContextBase : DbContext
	{
		public DbSet<Issue73Entity> Issue73Entities { get; set; } = null!;

		public DbSet<Patent> Patents { get; set; } = null!;

		public DbSet<Parent> Parents { get; set; } = null!;
		public DbSet<Child> Children { get; set; } = null!;
		public DbSet<GrandChild> GrandChildren { get; set; } = null!;

		public DbSet<IdentityTable> Identities { get; set; } = null!;

		public DbSet<Issue4624ItemTicketDate> Issue4624ItemTicketDates { get; set; } = null!;
		public DbSet<Issue4624Item> Issue4624Items { get; set; } = null!;

		public DbSet<Master> Masters { get; set; } = null!;
		public DbSet<Detail> Details { get; set; } = null!;

		public DbSet<TypesTable> Types { get; set; } = null!;

		public DbSet<Issue4627Container> Containers { get; set; } = null!;

		public DbSet<Issue4628Other> Issue4628Others { get; set; } = null!;

		public DbSet<Issue4629Post> Issue4629Posts { get; set; } = null!;
		public DbSet<Issue4629Tag> Issue4629Tags { get; set; } = null!;

		public DbSet<Issue340Entity> Issue340Entities { get; set; } = null!;

		public DbSet<Issue4640Table> Issue4640 { get; set; } = null!;

		public DbSet<Issue212Table> Issue212 { get; set; } = null!;

		public DbSet<Issue4642Table1> Issue4642Table1 { get; set; } = null!;
		public DbSet<Issue4642Table2> Issue4642Table2 { get; set; } = null!;

		public DbSet<Issue4644Main> Issue4644Mains { get; set; } = null!;
		public DbSet<Issue4644PricedItem> Issue4644Priced { get; set; } = null!;

		public DbSet<Issue4649Table> Issue4649 { get; set; } = null!;

		public DbSet<Issue4662Table> Issue4662 { get; set; } = null!;

#if NET8_0_OR_GREATER
		public DbSet<Issue4663Entity> Issue4663 { get; set; } = null!;
#endif

		public DbSet<Issue4666BaseEntity> Issue4666 { get; set; } = null!;
		public DbSet<Issue4668Table> Issue4668 { get; set; } = null!;
		public DbSet<IssueEnumTable> IssueEnum { get; set; } = null!;

		protected IssueContextBase(DbContextOptions options) : base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Issue73Entity>(b =>
			{
				b.HasKey(x => new { x.Id });

				b.HasOne(x => x.Parent!)
					.WithMany(x => x.Childs)
					.HasForeignKey(x => new { x.ParentId })
					.HasPrincipalKey(x => new { x.Id });

				b.HasData(
				[
					new Issue73Entity
					{
						Id = 2,
						Name = "Name1_2",
					},
					new Issue73Entity
					{
						Id = 3,
						Name = "Name1_3",
						ParentId = 2
					},
				]);
			});
			modelBuilder
				.Entity<Patent>()
				.HasOne(p => p.Assessment!)
				.WithOne(pa => pa.Patent)
				.HasForeignKey<PatentAssessment>(pa => pa.PatentId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Parent>(e =>
			{
				e.Property(e => e.Id).ValueGeneratedNever();
				e.Property(e => e.ParentId).IsRequired(false);
				e.HasMany(e => e.Children).WithOne(e => e.Parent).HasForeignKey(e => e.ParentId);
				e.HasOne(e => e.ParentsParent).WithMany(e => e.ParentChildren).HasForeignKey(e => e.ParentId).OnDelete(DeleteBehavior.NoAction);

				e.HasData(new Parent() { Id = 2 }, new Parent() { Id = 1, ParentId = 2 });
			});
			modelBuilder.Entity<Child>(e =>
			{
				e.Property(e => e.Id).ValueGeneratedNever();
				e.Property(e => e.ParentId);
				e.HasMany(e => e.GrandChildren).WithOne(e => e.Child).HasForeignKey(e => e.ChildId);

				e.HasData(new Child() { Id = 11, ParentId = 1, IsActive = true }, new Child() { Id = 12, ParentId = 2, IsActive = false });
			});
			modelBuilder.Entity<GrandChild>(e =>
			{
				e.Property(e => e.Id).ValueGeneratedNever();
				e.Property(e => e.ChildId);

				e.HasData(new GrandChild() { Id = 21, ChildId = 11 }, new GrandChild() { Id = 22, ChildId = 12 });
			});

			modelBuilder.Entity<ShadowTable>(e =>
			{
				e.Property(e => e.Id).ValueGeneratedNever();
				e.Property<bool>("IsDeleted").IsRequired();
				e.HasQueryFilter(p => !EF.Property<bool>(p, "IsDeleted"));
			});

			modelBuilder.Entity<Issue4624Item>(e =>
			{
				e.HasMany(e => e.ItemTicketDates).WithOne(e => e.Item).HasForeignKey(e => e.ItemId);
				e.HasMany(e => e.Entries).WithOne(e => e.Item).HasForeignKey(e => e.AclItemId);
			});
			modelBuilder.Entity<Issue4624Entry>();
			modelBuilder.Entity<Issue4624ItemTicketDate>();

			modelBuilder.Entity<Master>(e =>
			{
				e.HasMany(e => e.Details).WithOne(e => e.Master).HasForeignKey(e => e.MasterId);

				e.HasData(new Master() { Id = 1 });
			});
			modelBuilder.Entity<Detail>(e =>
			{
				e.HasData(new Detail() { Id = 1, MasterId = 1 }, new Detail() { Id = 2, MasterId = 1 });
			});

			modelBuilder.Entity<TypesTable>(e =>
			{
				e.Property(e => e.DateTimeOffsetWithConverter).HasConversion(new DateTimeOffsetToBinaryConverter());
				e.Property(e => e.DateTimeOffsetNWithConverter).HasConversion(new DateTimeOffsetToBinaryConverter());

				e.HasData(
					new TypesTable() { Id = 1 },
					new TypesTable()
					{
						Id = 2,
						DateTimeOffset = TestData.DateTimeOffset,
						DateTimeOffsetN = TestData.DateTimeOffset
					});
			});

			modelBuilder.Entity<Issue4627Container>(e =>
			{
				e.HasKey(x => x.Id);
				e.Property(x => x.Id).UseIdentityColumn();
			});
			modelBuilder.Entity<Issue4627Item>(e =>
			{
				e.HasKey(x => x.Id);
				e.Property(x => x.Id).UseIdentityColumn();

				e.HasOne(a => a.Container)
					.WithMany(b => b.Items)
					.IsRequired()
					.HasForeignKey(a => a.ContainerId);
			});
			modelBuilder.Entity<Issue4627ChildItem>(e =>
			{
				e.HasKey(e => e.Id);
				e.Property(e => e.Id).ValueGeneratedNever();

				// semantically each ChildItem is also an Item, hence one-to-one relationship with Id as FK
				e.HasOne(a => a.Parent)
					.WithOne(b => b.Child)
					.IsRequired()
					.HasForeignKey<Issue4627ChildItem>(a => a.Id);
			});

			modelBuilder.Entity<Issue4628Other>(e =>
			{
				e.HasData(new Issue4628Other() { Id = 1 });
			});
			modelBuilder.Entity<Issue4628Inherited>(e =>
			{
				e.HasData(new Issue4628Inherited() { Id = 11, OtherId = 1, SomeValue = "Value 11" });
			});

			modelBuilder.Entity<Issue4629Post>(e =>
			{
				e.HasMany(e => e.Tags).WithOne(e => e.Post).HasForeignKey(e => e.PostId);

				e.HasData(new Issue4629Post() { Id = 1 });
				e.HasData(new Issue4629Post() { Id = 2 });
			});
			modelBuilder.Entity<Issue4629Tag>(e =>
			{
				e.HasData(new Issue4629Tag() { Id = 1, PostId = 1, Weight = 10 });
				e.HasData(new Issue4629Tag() { Id = 2, PostId = 1, Weight = 7 });
				e.HasData(new Issue4629Tag() { Id = 3, PostId = 2, Weight = 0 });
				e.HasData(new Issue4629Tag() { Id = 4, PostId = 2, Weight = 6 });
				e.HasData(new Issue4629Tag() { Id = 5, PostId = 2, Weight = 4 });
				e.HasData(new Issue4629Tag() { Id = 6, PostId = 2, Weight = 3 });
			});

			modelBuilder.Entity<Issue340Entity>();

			modelBuilder.Entity<Issue4640Table>(e =>
			{
				e.Property(e => e.Id).ValueGeneratedNever();

				var converter = new ValueConverter<List<Issue4640Items>?, string>(
					v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
					v => JsonSerializer.Deserialize<List<Issue4640Items>>(v, (JsonSerializerOptions?)null));

				e.Property(e => e.Items)
					.HasConversion(converter)
					.Metadata
					.SetValueComparer(new ValueComparer<List<Issue4640Items>?>(
						(c1, c2) => c1!.SequenceEqual(c2!),
						c => c!.Aggregate(0, (a, v) => a ^ v.GetHashCode()),
						c => c!.ToList()));
			});

			modelBuilder.Entity<Issue212Table>(e =>
			{
				e.HasKey(x => new { x.Id, x.Value });
				e.Property(x => x.Value)
					.IsRequired(true)
					.HasMaxLength(20)
					.IsUnicode(false);
			});

			modelBuilder.Entity<Issue4642Table2>(builder =>
			{
				builder.HasKey(x => new { x.Id, x.SystemId });
				builder.Property(x => x.SystemId)
					.IsRequired(true)
					.HasMaxLength(20)
					.IsUnicode(false);
			});
			modelBuilder.Entity<Issue4642Table1>(builder =>
			{
				builder.HasKey(x => x.Id);
			});

			// disabled for now as it blocks tests execution
#if !NETFRAMEWORK
			modelBuilder.Ignore<Issue4644EntityBase>();
			modelBuilder.Entity<Issue4644BaseItem>(bb =>
			{
				bb.ToTable("Issue245MainDetails");
			});
			modelBuilder.Entity<Issue4644Main>(bb =>
			{
				bb.HasOne(m => m.Details)
					.WithOne(d => d!.Main!);
			});
			modelBuilder.Entity<Issue4644PricedItem>(bb =>
			{
				bb.ToTable("Issue245PricedDetails");
			});
#endif

			modelBuilder.Entity<Issue4649Table>(b =>
			{
				b.HasKey(e => e.Id);

				b.Property(e => e.Id)
					.UseIdentityColumn();
			});

			modelBuilder.Entity<Issue4662Table>(b =>
			{
				b.Property(e => e.Value)
					.HasConversion<string>()
					.HasMaxLength(50);
			});

#if NET8_0_OR_GREATER
			modelBuilder.Entity<Issue4663Entity>(b =>
			{
				b.ComplexProperty(
					cp => cp.Value,
					cp =>
					{
						cp.Property(p => p.Value).HasColumnName("ColumnPropA");
					});
			});
#endif

			modelBuilder.Entity<Issue4666BaseEntity>(builder =>
			{
				builder.Property(e => e.Id).ValueGeneratedNever();

				builder.HasDiscriminator(x => x.Type)
					.HasValue<Issue4666BaseEntity>(Issue4666EntityType.None)
					.HasValue<Issue4666Type1Entity>(Issue4666EntityType.Type1)
					.HasValue<Issue4666Type2Entity>(Issue4666EntityType.Type2);
			});

			modelBuilder.Entity<Issue4668TableBase>();
			modelBuilder.Entity<Issue4668Table>(builder =>
			{
				builder.HasBaseType((Type?)null);
			});

			modelBuilder.Entity<Issue4671Entity1>();

			modelBuilder.Entity<IssueEnumTable>();
		}
	}
}
