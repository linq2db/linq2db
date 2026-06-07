#if !NETFRAMEWORK
using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.ManyToMany
{
	public abstract class ManyToManyContextBase : DbContext
	{
		public DbSet<MmStudent> Students { get; set; } = null!;
		public DbSet<MmCourse>  Courses  { get; set; } = null!;

		public DbSet<MmOrder>   Orders   { get; set; } = null!;
		public DbSet<MmProduct> Products { get; set; } = null!;

		public DbSet<MmProject> Projects { get; set; } = null!;
		public DbSet<MmMember>  Members  { get; set; } = null!;

		public DbSet<MmPerson>  People   { get; set; } = null!;

		public DbSet<MmUser>    Users    { get; set; } = null!;
		public DbSet<MmTeam>    Teams    { get; set; } = null!;

		public DbSet<MmDoc>     Docs     { get; set; } = null!;
		public DbSet<MmLabel>   Labels   { get; set; } = null!;

		public DbSet<MmAccount> Accounts { get; set; } = null!;
		public DbSet<MmRole>    Roles    { get; set; } = null!;

		public DbSet<MmArticle> Articles { get; set; } = null!;
		public DbSet<MmTag>     Tags     { get; set; } = null!;

		protected ManyToManyContextBase(DbContextOptions options) : base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			// 1. Implicit many-to-many, single-column keys.
			modelBuilder.Entity<MmStudent>(b =>
			{
				b.Property(e => e.Id).ValueGeneratedNever();

				b.HasMany(s => s.Courses).WithMany(c => c.Students)
					.UsingEntity(j => j.HasData(
						new { CoursesId = 1, StudentsId = 1 },
						new { CoursesId = 2, StudentsId = 1 },
						new { CoursesId = 2, StudentsId = 2 }));

				b.HasData(
					new MmStudent { Id = 1, Name = "Alice" },
					new MmStudent { Id = 2, Name = "Bob"   },
					new MmStudent { Id = 3, Name = "Carol" });
			});
			modelBuilder.Entity<MmCourse>(b =>
			{
				b.Property(e => e.Id).ValueGeneratedNever();
				b.HasData(
					new MmCourse { Id = 1, Title = "Math"    },
					new MmCourse { Id = 2, Title = "Physics" },
					new MmCourse { Id = 3, Title = "History" });
			});

			// 2. Explicit join entity with payload.
			modelBuilder.Entity<MmOrder>(b =>
			{
				b.Property(e => e.Id).ValueGeneratedNever();

				b.HasMany(o => o.Products).WithMany(p => p.Orders)
					.UsingEntity<MmOrderProduct>(
						r => r.HasOne(op => op.Product).WithMany().HasForeignKey(op => op.ProductId),
						l => l.HasOne(op => op.Order).WithMany().HasForeignKey(op => op.OrderId),
						j => j.HasData(
							new MmOrderProduct { OrderId = 1, ProductId = 1, Qty = 2 },
							new MmOrderProduct { OrderId = 1, ProductId = 2, Qty = 1 },
							new MmOrderProduct { OrderId = 2, ProductId = 3, Qty = 5 }));

				b.HasData(
					new MmOrder { Id = 1, Number = "O-1" },
					new MmOrder { Id = 2, Number = "O-2" });
			});
			modelBuilder.Entity<MmProduct>(b =>
			{
				b.Property(e => e.Id).ValueGeneratedNever();
				b.HasData(
					new MmProduct { Id = 1, Name = "Apple"  },
					new MmProduct { Id = 2, Name = "Banana" },
					new MmProduct { Id = 3, Name = "Cherry" });
			});

			// 3. Composite-key many-to-many via an explicit join entity.
			modelBuilder.Entity<MmProject>(b =>
			{
				b.HasKey(p => new { p.OrgId, p.Code });
				b.Property(e => e.OrgId).ValueGeneratedNever();
				b.Property(e => e.Code).ValueGeneratedNever();

				b.HasMany(p => p.Members).WithMany(m => m.Projects)
					.UsingEntity<MmProjectMember>(
						r => r.HasOne(pm => pm.Member).WithMany().HasForeignKey(pm => pm.MemberId),
						l => l.HasOne(pm => pm.Project).WithMany().HasForeignKey(pm => new { pm.OrgId, pm.Code }),
						j => j.HasData(
							new MmProjectMember { OrgId = 1, Code = 10, MemberId = 1 },
							new MmProjectMember { OrgId = 1, Code = 20, MemberId = 1 },
							new MmProjectMember { OrgId = 1, Code = 20, MemberId = 2 }));

				b.HasData(
					new MmProject { OrgId = 1, Code = 10, Name = "Alpha" },
					new MmProject { OrgId = 1, Code = 20, Name = "Beta"  });
			});
			modelBuilder.Entity<MmMember>(b =>
			{
				b.Property(e => e.Id).ValueGeneratedNever();
				b.HasData(
					new MmMember { Id = 1, Name = "Dan" },
					new MmMember { Id = 2, Name = "Eve" });
			});

			// 4. Self-referencing many-to-many via an explicit join entity.
			modelBuilder.Entity<MmPerson>(b =>
			{
				b.Property(e => e.Id).ValueGeneratedNever();

				b.HasMany(p => p.Friends).WithMany(p => p.FriendsOf)
					.UsingEntity<MmFriendship>(
						r => r.HasOne(f => f.Friend).WithMany().HasForeignKey(f => f.FriendId),
						l => l.HasOne(f => f.Person).WithMany().HasForeignKey(f => f.PersonId),
						j => j.HasData(
							new MmFriendship { PersonId = 1, FriendId = 2 },
							new MmFriendship { PersonId = 1, FriendId = 3 },
							new MmFriendship { PersonId = 2, FriendId = 3 }));

				b.HasData(
					new MmPerson { Id = 1, Name = "Alice" },
					new MmPerson { Id = 2, Name = "Bob"   },
					new MmPerson { Id = 3, Name = "Carol" });
			});

			// 5. Two distinct many-to-many relationships between the same entity pair (explicit joins).
			modelBuilder.Entity<MmUser>(b =>
			{
				b.Property(e => e.Id).ValueGeneratedNever();

				b.HasMany(u => u.Teams).WithMany(t => t.Members)
					.UsingEntity<MmMembership>(
						r  => r.HasOne<MmTeam>().WithMany().HasForeignKey(x => x.TeamId),
						lb => lb.HasOne<MmUser>().WithMany().HasForeignKey(x => x.UserId),
						j  => j.HasData(
							new MmMembership { UserId = 1, TeamId = 1 },
							new MmMembership { UserId = 2, TeamId = 2 }));

				b.HasMany(u => u.LedTeams).WithMany(t => t.Leaders)
					.UsingEntity<MmLeadership>(
						r  => r.HasOne<MmTeam>().WithMany().HasForeignKey(x => x.TeamId),
						lb => lb.HasOne<MmUser>().WithMany().HasForeignKey(x => x.UserId),
						j  => j.HasData(
							new MmLeadership { UserId = 2, TeamId = 1 },
							new MmLeadership { UserId = 1, TeamId = 2 }));

				b.HasData(
					new MmUser { Id = 1, Name = "Alice" },
					new MmUser { Id = 2, Name = "Bob"   });
			});
			modelBuilder.Entity<MmTeam>(b =>
			{
				b.Property(e => e.Id).ValueGeneratedNever();
				b.HasData(
					new MmTeam { Id = 1, Name = "Team1" },
					new MmTeam { Id = 2, Name = "Team2" });
			});

			// 6. Two implicit many-to-many relationships between the same entity pair (unsupported -> clear error).
			modelBuilder.Entity<MmDoc>(b =>
			{
				b.Property(e => e.Id).ValueGeneratedNever();
				b.HasMany(d => d.PrimaryLabels).WithMany(l => l.PrimaryDocs);
				b.HasMany(d => d.SecondaryLabels).WithMany(l => l.SecondaryDocs);
				b.HasData(new MmDoc { Id = 1, Title = "Doc1" });
			});
			modelBuilder.Entity<MmLabel>(b =>
			{
				b.Property(e => e.Id).ValueGeneratedNever();
				b.HasData(new MmLabel { Id = 1, Name = "L1" });
			});

			// 7. Key mapped to a field (no CLR property), with a renamed column.
			modelBuilder.Entity<MmAccount>(b =>
			{
				b.Property<int>("AccountId").ValueGeneratedNever().HasColumnName("account_id_col");
				b.HasKey("AccountId");

				b.HasMany(a => a.Roles).WithMany(r => r.Accounts)
					.UsingEntity(j => j.HasData(
						new { AccountsAccountId = 1, RolesId = 1 },
						new { AccountsAccountId = 2, RolesId = 2 }));

				b.HasData(
					new { AccountId = 1, Name = "Acc1" },
					new { AccountId = 2, Name = "Acc2" });
			});
			modelBuilder.Entity<MmRole>(b =>
			{
				b.Property(e => e.Id).ValueGeneratedNever();
				b.HasData(
					new MmRole { Id = 1, Name = "Admin" },
					new MmRole { Id = 2, Name = "User"  });
			});

			// 8. Shadow primary key (no CLR member), with a renamed column.
			modelBuilder.Entity<MmArticle>(b =>
			{
				b.Property(e => e.Id).ValueGeneratedNever();

				b.HasMany(a => a.Tags).WithMany(t => t.Articles)
					.UsingEntity(j => j.HasData(
						new { ArticlesId = 1, TagsTagId = 1 },
						new { ArticlesId = 2, TagsTagId = 2 }));

				b.HasData(
					new MmArticle { Id = 1, Title = "Art1" },
					new MmArticle { Id = 2, Title = "Art2" });
			});
			modelBuilder.Entity<MmTag>(b =>
			{
				b.Property<int>("TagId").ValueGeneratedNever().HasColumnName("tag_id_col");
				b.HasKey("TagId");
				b.HasData(
					new { TagId = 1, Label = "news" },
					new { TagId = 2, Label = "tech" });
			});
		}
	}
}
#endif
