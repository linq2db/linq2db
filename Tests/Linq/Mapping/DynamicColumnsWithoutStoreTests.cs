using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Mapping
{
	/// <summary>
	/// Materializing an entity that has dynamic columns but no <see cref="DynamicColumnsStoreAttribute"/> member.
	/// Referencing a dynamic column in a query is legal without a store; materializing one is not, because the
	/// value has nowhere to go.
	/// </summary>
	[TestFixture]
	public class DynamicColumnsWithoutStoreTests : TestBase
	{
		[Table]
		sealed class NoStoreChild
		{
			[Column] public int Id       { get; set; }
			[Column] public int ParentId { get; set; }
		}

		[Table]
		sealed class NoStoreWithAssociation
		{
			[Column] public int Id { get; set; }

			NoStoreChild? _child;

			// Storage-backed: the only shape that produces an association assignment during construction.
			[Association(ThisKey = nameof(Id), OtherKey = nameof(NoStoreChild.ParentId), CanBeNull = true, Storage = nameof(_child))]
			public NoStoreChild? Child => _child;
		}

		[Table]
		sealed class NoStorePlain
		{
			[Column] public int Id { get; set; }
		}

		static MappingSchema WithDynamicColumn<TEntity>()
			where TEntity : class
		{
			var ms = new MappingSchema();
			var fb = new FluentMappingBuilder(ms);

			fb.Entity<TEntity>().Property(x => Sql.Property<string>(x, "Extra"));
			fb.Build();

			return ms;
		}

		[Test]
		public void AssociationAndDynamicColumnWithoutStore([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context, WithDynamicColumn<NoStoreWithAssociation>());
			using var t1 = db.CreateLocalTable<NoStoreWithAssociation>();
			using var t2 = db.CreateLocalTable<NoStoreChild>();

			// Previously a NullReferenceException from EntityConstructorBase: the association filled
			// additionalSteps, which entered the block that then dereferenced the null dynamic-column setter.
			Action act = () => db.GetTable<NoStoreWithAssociation>().LoadWith(x => x.Child).ToList();

			act.ShouldThrow<LinqToDBException>().Message.ShouldContain(nameof(DynamicColumnsStoreAttribute));
		}

		[Test]
		public void DynamicColumnWithoutStore([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context, WithDynamicColumn<NoStorePlain>());
			using var t  = db.CreateLocalTable<NoStorePlain>();

			// Previously the dynamic column was dropped silently and the query returned rows missing it.
			Action act = () => db.GetTable<NoStorePlain>().ToList();

			act.ShouldThrow<LinqToDBException>().Message.ShouldContain(nameof(DynamicColumnsStoreAttribute));
		}
	}
}
