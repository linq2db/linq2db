﻿// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using Microsoft.SqlServer.Types;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Default.SqlServer
{
	public partial class TestDataDB : DataConnection
	{
		#region Schemas
		public void InitSchemas()
		{
			TestSchema = new TestSchemaSchema.DataContext(this);
		}

		public TestSchemaSchema.DataContext TestSchema { get; set; } = null!;
		#endregion

		public TestDataDB()
		{
			InitSchemas();
			InitDataContext();
		}

		public TestDataDB(string configuration)
			: base(configuration)
		{
			InitSchemas();
			InitDataContext();
		}

		public TestDataDB(DataContextOptions<TestDataDB> options)
			: base(options)
		{
			InitSchemas();
			InitDataContext();
		}

		partial void InitDataContext();

		public ITable<DataType>                DataTypes                => this.GetTable<DataType>();
		public ITable<CollatedTable>           CollatedTables           => this.GetTable<CollatedTable>();
		public ITable<InheritanceParent>       InheritanceParents       => this.GetTable<InheritanceParent>();
		public ITable<InheritanceChild>        InheritanceChildren      => this.GetTable<InheritanceChild>();
		public ITable<Person>                  People                   => this.GetTable<Person>();
		public ITable<Doctor>                  Doctors                  => this.GetTable<Doctor>();
		public ITable<Patient>                 Patients                 => this.GetTable<Patient>();
		public ITable<AllType>                 AllTypes                 => this.GetTable<AllType>();
		public ITable<AllTypes2>               AllTypes2                => this.GetTable<AllTypes2>();
		/// <summary>
		/// This is Parent table
		/// </summary>
		public ITable<Parent>                  Parents                  => this.GetTable<Parent>();
		public ITable<Child>                   Children                 => this.GetTable<Child>();
		public ITable<CreateIfNotExistsTable>  CreateIfNotExistsTables  => this.GetTable<CreateIfNotExistsTable>();
		public ITable<GrandChild>              GrandChildren            => this.GetTable<GrandChild>();
		public ITable<LinqDataType>            LinqDataTypes            => this.GetTable<LinqDataType>();
		public ITable<TestIdentity>            TestIdentities           => this.GetTable<TestIdentity>();
		public ITable<IndexTable>              IndexTables              => this.GetTable<IndexTable>();
		public ITable<IndexTable2>             IndexTable2              => this.GetTable<IndexTable2>();
		public ITable<NameTest>                NameTests                => this.GetTable<NameTest>();
		public ITable<GuidId>                  GuidIds                  => this.GetTable<GuidId>();
		public ITable<GuidId2>                 GuidId2                  => this.GetTable<GuidId2>();
		public ITable<DecimalOverflow>         DecimalOverflows         => this.GetTable<DecimalOverflow>();
		public ITable<SqlType>                 SqlTypes                 => this.GetTable<SqlType>();
		public ITable<TestMerge1>              TestMerge1               => this.GetTable<TestMerge1>();
		public ITable<TestMerge2>              TestMerge2               => this.GetTable<TestMerge2>();
		public ITable<TestMergeIdentity>       TestMergeIdentities      => this.GetTable<TestMergeIdentity>();
		public ITable<TestSchemaX>             TestSchemaX              => this.GetTable<TestSchemaX>();
		public ITable<TestSchemaY>             TestSchemaY              => this.GetTable<TestSchemaY>();
		public ITable<Issue1144>               Issue1144                => this.GetTable<Issue1144>();
		public ITable<SameTableName>           SameTableNames           => this.GetTable<SameTableName>();
		public ITable<TestSchemaSameTableName> TestSchemaSameTableNames => this.GetTable<TestSchemaSameTableName>();
		public ITable<Issue1115>               Issue1115                => this.GetTable<Issue1115>();
		public ITable<ParentView>              ParentViews              => this.GetTable<ParentView>();
		public ITable<ParentChildView>         ParentChildViews         => this.GetTable<ParentChildView>();
	}

	public static partial class ExtensionMethods
	{
		#region Table Extensions
		public static InheritanceParent? Find(this ITable<InheritanceParent> table, int inheritanceParentId)
		{
			return table.FirstOrDefault(e => e.InheritanceParentId == inheritanceParentId);
		}

		public static Task<InheritanceParent?> FindAsync(this ITable<InheritanceParent> table, int inheritanceParentId, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.InheritanceParentId == inheritanceParentId, cancellationToken);
		}

		public static InheritanceChild? Find(this ITable<InheritanceChild> table, int inheritanceChildId)
		{
			return table.FirstOrDefault(e => e.InheritanceChildId == inheritanceChildId);
		}

		public static Task<InheritanceChild?> FindAsync(this ITable<InheritanceChild> table, int inheritanceChildId, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.InheritanceChildId == inheritanceChildId, cancellationToken);
		}

		public static Person? Find(this ITable<Person> table, int personId)
		{
			return table.FirstOrDefault(e => e.PersonId == personId);
		}

		public static Task<Person?> FindAsync(this ITable<Person> table, int personId, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.PersonId == personId, cancellationToken);
		}

		public static Doctor? Find(this ITable<Doctor> table, int personId)
		{
			return table.FirstOrDefault(e => e.PersonId == personId);
		}

		public static Task<Doctor?> FindAsync(this ITable<Doctor> table, int personId, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.PersonId == personId, cancellationToken);
		}

		public static Patient? Find(this ITable<Patient> table, int personId)
		{
			return table.FirstOrDefault(e => e.PersonId == personId);
		}

		public static Task<Patient?> FindAsync(this ITable<Patient> table, int personId, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.PersonId == personId, cancellationToken);
		}

		public static AllType? Find(this ITable<AllType> table, int id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<AllType?> FindAsync(this ITable<AllType> table, int id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}

		public static AllTypes2? Find(this ITable<AllTypes2> table, int id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<AllTypes2?> FindAsync(this ITable<AllTypes2> table, int id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}

		public static Parent? Find(this ITable<Parent> table, int id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<Parent?> FindAsync(this ITable<Parent> table, int id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}

		public static Child? Find(this ITable<Child> table, int id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<Child?> FindAsync(this ITable<Child> table, int id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}

		public static GrandChild? Find(this ITable<GrandChild> table, int id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<GrandChild?> FindAsync(this ITable<GrandChild> table, int id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}

		public static LinqDataType? Find(this ITable<LinqDataType> table, int id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<LinqDataType?> FindAsync(this ITable<LinqDataType> table, int id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}

		public static TestIdentity? Find(this ITable<TestIdentity> table, int id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<TestIdentity?> FindAsync(this ITable<TestIdentity> table, int id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}

		public static IndexTable? Find(this ITable<IndexTable> table, int pkField2, int pkField1)
		{
			return table.FirstOrDefault(e => e.PkField2 == pkField2 && e.PkField1 == pkField1);
		}

		public static Task<IndexTable?> FindAsync(this ITable<IndexTable> table, int pkField2, int pkField1, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.PkField2 == pkField2 && e.PkField1 == pkField1, cancellationToken);
		}

		public static IndexTable2? Find(this ITable<IndexTable2> table, int pkField2, int pkField1)
		{
			return table.FirstOrDefault(e => e.PkField2 == pkField2 && e.PkField1 == pkField1);
		}

		public static Task<IndexTable2?> FindAsync(this ITable<IndexTable2> table, int pkField2, int pkField1, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.PkField2 == pkField2 && e.PkField1 == pkField1, cancellationToken);
		}

		public static GuidId? Find(this ITable<GuidId> table, Guid id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<GuidId?> FindAsync(this ITable<GuidId> table, Guid id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}

		public static GuidId2? Find(this ITable<GuidId2> table, Guid id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<GuidId2?> FindAsync(this ITable<GuidId2> table, Guid id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}

		public static DecimalOverflow? Find(this ITable<DecimalOverflow> table, decimal decimal1)
		{
			return table.FirstOrDefault(e => e.Decimal1 == decimal1);
		}

		public static Task<DecimalOverflow?> FindAsync(this ITable<DecimalOverflow> table, decimal decimal1, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Decimal1 == decimal1, cancellationToken);
		}

		public static SqlType? Find(this ITable<SqlType> table, int id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<SqlType?> FindAsync(this ITable<SqlType> table, int id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}

		public static TestMerge1? Find(this ITable<TestMerge1> table, int id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<TestMerge1?> FindAsync(this ITable<TestMerge1> table, int id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}

		public static TestMerge2? Find(this ITable<TestMerge2> table, int id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<TestMerge2?> FindAsync(this ITable<TestMerge2> table, int id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}

		public static TestMergeIdentity? Find(this ITable<TestMergeIdentity> table, int id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<TestMergeIdentity?> FindAsync(this ITable<TestMergeIdentity> table, int id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}

		public static TestSchemaX? Find(this ITable<TestSchemaX> table, int testSchemaXid)
		{
			return table.FirstOrDefault(e => e.TestSchemaXid == testSchemaXid);
		}

		public static Task<TestSchemaX?> FindAsync(this ITable<TestSchemaX> table, int testSchemaXid, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.TestSchemaXid == testSchemaXid, cancellationToken);
		}

		public static Issue1144? Find(this ITable<Issue1144> table, int id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<Issue1144?> FindAsync(this ITable<Issue1144> table, int id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}

		public static Issue1115? Find(this ITable<Issue1115> table, SqlHierarchyId id)
		{
			return table.FirstOrDefault(e => (bool)(e.Id == id));
		}

		public static Task<Issue1115?> FindAsync(this ITable<Issue1115> table, SqlHierarchyId id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => (bool)(e.Id == id), cancellationToken);
		}
		#endregion
	}
}