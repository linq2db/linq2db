using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Mapping
{
	[TestFixture]
	public class DynamicStoreTests : TestBase
	{
		[Table("DynamicColumnsTestTable")]
		sealed class DynamicColumnsTestFullTable
		{
			[Column]
			public int Id { get; set; }

			[Column]
			public string? Name { get; set; }
		}

		[Table("DynamicColumnsTestTable")]
		sealed class FluentMetadataBasedStore
		{
			public int Id { get; set; }

			public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

			public Dictionary<string, object> SQLiteValues { get; set; } = new Dictionary<string, object>();
		}

		[Table("DynamicColumnsTestTable")]
		sealed class AttributeMetadataBasedStore
		{
			[Column]
			public int Id { get; set; }

			[DynamicColumnsStore]
			public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

			[DynamicColumnsStore(Configuration = ProviderName.SQLite)]
			public Dictionary<string, object> SQLiteValues { get; set; } = new Dictionary<string, object>();
		}

		[Table("DynamicColumnsTestTable")]
		class CustomSetterGetterBase
		{
			[Column]
			public int Id { get; set; }

			public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

			public Dictionary<string, object> SQLiteValues { get; set; } = new Dictionary<string, object>();
		}

		[DynamicColumnAccessor(GetterMethod = nameof(GetProperty), SetterMethod = nameof(SetProperty))]
		sealed class InstanceGetterSetterMethods : CustomSetterGetterBase
		{
			private object GetProperty(string name, object defaultValue)
			{
				if (!Values.TryGetValue(name, out var value))
					value = defaultValue;

				return value;
			}

			public void SetProperty(string name, object value)
			{
				Values[name] = value;
			}
		}

		[DynamicColumnAccessor(GetterMethod = nameof(GetProperty), SetterMethod = nameof(SetProperty))]
		sealed class StaticGetterSetterMethods : CustomSetterGetterBase
		{
			public static Dictionary<int, Dictionary<string, object>> InstanceValues { get; set; } = new Dictionary<int, Dictionary<string, object>>();

			public static object GetProperty(StaticGetterSetterMethods instance, string name, object defaultValue)
			{
				if (!InstanceValues.TryGetValue(instance.Id, out var values))
					return defaultValue;

				if (!values.TryGetValue(name, out var value))
					value = defaultValue;

				return value;
			}

			private static void SetProperty(StaticGetterSetterMethods instance, string name, object value)
			{
				if (!InstanceValues.TryGetValue(instance.Id, out var values))
				{
					InstanceValues.Add(instance.Id, values = new Dictionary<string, object>());
				}

				values[name] = value;
			}
		}

		[DynamicColumnAccessor(GetterExpressionMethod = nameof(GetPropertyExpression), SetterExpressionMethod = nameof(SetPropertyExpression))]
		sealed class InstanceGetterSetterExpressionMethods : CustomSetterGetterBase
		{
			public static Expression<Func<InstanceGetterSetterExpressionMethods, string, object, object>> GetPropertyExpression
			{
				get
				{
					return (instance, name, defaultValue) => instance.GetProperty(name, defaultValue);
				}
			}

			public static Expression<Action<InstanceGetterSetterExpressionMethods, string, object>> SetPropertyExpression()
			{
				return (instance, name, value) => instance.SetProperty(name, value);
			}

			public object GetProperty(string name, object defaultValue)
			{
				if (!Values.TryGetValue(name, out var value))
					value = defaultValue;

				return value;
			}

			public void SetProperty(string name, object value)
			{
				Values[name] = value;
			}
		}

		[DynamicColumnAccessor(GetterExpressionMethod = nameof(GetPropertyExpression), SetterExpressionMethod = nameof(SetPropertyExpression))]
		sealed class StaticGetterSetterExpressionMethods : CustomSetterGetterBase
		{
			public static Dictionary<int, Dictionary<string, object>> InstanceValues { get; set; } = new Dictionary<int, Dictionary<string, object>>();

			public static Expression<Func<StaticGetterSetterExpressionMethods, string, object, object>> GetPropertyExpression()
			{
				return (instance, name, defaultValue) => GetProperty(instance, name, defaultValue);
			}

			public static Expression<Action<StaticGetterSetterExpressionMethods, string, object>> SetPropertyExpression
			{
				get
				{
					return (instance, name, value) => SetProperty(instance, name, value);
				}
			}

			public static object GetProperty(StaticGetterSetterExpressionMethods instance, string name, object defaultValue)
			{
				if (!InstanceValues.TryGetValue(instance.Id, out var values))
					return defaultValue;

				if (!values.TryGetValue(name, out var value))
					value = defaultValue;

				return value;
			}

			public static void SetProperty(StaticGetterSetterExpressionMethods instance, string name, object value)
			{
				if (!InstanceValues.TryGetValue(instance.Id, out var values))
				{
					InstanceValues.Add(instance.Id, values = new Dictionary<string, object>());
				}

				values[name] = value;
			}
		}

		[DynamicColumnAccessor(GetterMethod = nameof(GetProperty), SetterMethod = nameof(SetProperty))]
		[DynamicColumnAccessor(GetterMethod = nameof(GetSQLiteProperty), SetterMethod = nameof(SetSQLiteProperty), Configuration = ProviderName.SQLite)]
		sealed class SQLiteInstanceGetterSetterMethods : CustomSetterGetterBase
		{
			public object GetSQLiteProperty(string name, object defaultValue)
			{
				if (!SQLiteValues.TryGetValue(name, out var value))
					value = defaultValue;

				return value;
			}

			public void SetSQLiteProperty(string name, object value)
			{
				SQLiteValues[name] = value;
			}

			public object GetProperty(string name, object defaultValue)
			{
				if (!Values.TryGetValue(name, out var value))
					value = defaultValue;

				return value;
			}

			public void SetProperty(string name, object value)
			{
				Values[name] = value;
			}
		}

		[Table("DynamicColumnsTestTable")]
		[DynamicColumnAccessor(GetterMethod = nameof(GetSQLiteProperty), SetterMethod = nameof(SetSQLiteProperty), Configuration = ProviderName.SQLite)]
		sealed class GetterSetterVsStorageMethods1
		{
			[Column]
			public int Id { get; set; }

			[DynamicColumnsStore]
			public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

			public Dictionary<string, object> SQLiteValues { get; set; } = new Dictionary<string, object>();

			public object GetSQLiteProperty(string name, object defaultValue)
			{
				if (!SQLiteValues.TryGetValue(name, out var value))
					value = defaultValue;

				return value;
			}

			public void SetSQLiteProperty(string name, object value)
			{
				SQLiteValues[name] = value;
			}
		}

		[Table("DynamicColumnsTestTable")]
		[DynamicColumnAccessor(GetterMethod = nameof(GetProperty), SetterMethod = nameof(SetProperty))]
		sealed class GetterSetterVsStorageMethods2
		{
			[Column]
			public int Id { get; set; }

			[DynamicColumnsStore(Configuration = "Unknown")]
			public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

			[DynamicColumnsStore(Configuration = ProviderName.SQLite)]
			public Dictionary<string, object> SQLiteValues { get; set; } = new Dictionary<string, object>();

			public object GetProperty(string name, object defaultValue)
			{
				if (!Values.TryGetValue(name, out var value))
					value = defaultValue;

				return value;
			}

			public void SetProperty(string name, object value)
			{
				Values[name] = value;
			}
		}

		[Table("DynamicColumnsTestTable")]
		[DynamicColumnAccessor(GetterMethod = nameof(GetProperty), SetterMethod = nameof(SetProperty))]
		sealed class GetterSetterVsStorageMethodsConflict
		{
			[Column]
			public int Id { get; set; }

			[DynamicColumnsStore]
			public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

			public object GetProperty(string name, object defaultValue)
			{
				if (!Values.TryGetValue(name, out var value))
					value = defaultValue;

				return value;
			}

			public void SetProperty(string name, object value)
			{
				Values[name] = value;
			}
		}

		[Table("DynamicColumnsTestTable")]
		[DynamicColumnAccessor(
			GetterMethod = nameof(GetProperty), SetterMethod = nameof(SetProperty),
			GetterExpressionMethod = nameof(GetPropertyExpression), SetterExpressionMethod = nameof(SetPropertyExpression))]
		sealed class MultipleGetterSetterMethods
		{
			[Column]
			public int Id { get; set; }

			[DynamicColumnsStore]
			public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

			public object GetProperty(string name, object defaultValue)
			{
				if (!Values.TryGetValue(name, out var value))
					value = defaultValue;

				return value;
			}

			public void SetProperty(string name, object value)
			{
				Values[name] = value;
			}
			public static Expression<Func<InstanceGetterSetterExpressionMethods, string, object, object>> GetPropertyExpression()
			{
				return (instance, name, defaultValue) => instance.GetProperty(name, defaultValue);
			}

			public static Expression<Action<InstanceGetterSetterMethods, string, object>> SetPropertyExpression()
			{
				return (instance, name, value) => instance.SetProperty(name, value);
			}
		}

		[Table("DynamicColumnsTestTable")]
		[DynamicColumnAccessor]
		sealed class NoGetterSetterMethods
		{
			[Column]
			public int Id { get; set; }

			[DynamicColumnsStore]
			public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

			public object GetProperty(string name, object defaultValue)
			{
				if (!Values.TryGetValue(name, out var value))
					value = defaultValue;

				return value;
			}

			public void SetProperty(string name, object value)
			{
				Values[name] = value;
			}
			public static Expression<Func<InstanceGetterSetterExpressionMethods, string, object, object>> GetPropertyExpression()
			{
				return (instance, name, defaultValue) => instance.GetProperty(name, defaultValue);
			}

			public static Expression<Action<InstanceGetterSetterMethods, string, object>> SetPropertyExpression()
			{
				return (instance, name, value) => instance.SetProperty(name, value);
			}
		}

		[Test]
		public void TestDynamicColumnStoreFromMetadataReader([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();

			var builder = new FluentMappingBuilder(ms);
			builder.Entity<FluentMetadataBasedStore>()
				.HasColumn(e => e.Id)
				.Property(x => Sql.Property<string>(x, "Name"))
				.Member(e => e.Values).HasAttribute(new DynamicColumnsStoreAttribute())
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new FluentMetadataBasedStore { Id = 5 };
				obj.Values.Add("Name", "test_name");
				db.Insert(obj);

				var data = db.GetTable<FluentMetadataBasedStore>().ToList();
				Assert.That(data, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(5));
					Assert.That(data[0].Values, Has.Count.EqualTo(1));
					Assert.That(data[0].Values.Keys.Single(), Is.EqualTo("Name"));
					Assert.That(data[0].Values["Name"], Is.EqualTo("test_name"));
				}
			}
		}

		[Test]
		public void TestDynamicColumnStoreFluentExtension([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();

			var builder = new FluentMappingBuilder(ms);
			builder.Entity<FluentMetadataBasedStore>()
				.HasColumn(e => e.Id)
				.DynamicColumnsStore(e => e.Values)
				.Property(x => Sql.Property<string>(x, "Name"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new FluentMetadataBasedStore { Id = 5 };
				obj.Values.Add("Name", "test_name");
				db.Insert(obj);

				var data = db.GetTable<FluentMetadataBasedStore>().ToList();
				Assert.That(data, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(5));
					Assert.That(data[0].Values, Has.Count.EqualTo(1));
					Assert.That(data[0].Values.Keys.Single(), Is.EqualTo("Name"));
					Assert.That(data[0].Values["Name"], Is.EqualTo("test_name"));
				}
			}
		}

		[Test]
		public void TestDynamicColumnStoreFluentWithConfiguration([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			var builder = new FluentMappingBuilder(ms);
			builder.Entity<FluentMetadataBasedStore>()
				.HasColumn(e => e.Id)
				.DynamicColumnsStore(e => e.Values)
				.Property(x => Sql.Property<string>(x, "Name"))
				.Entity<FluentMetadataBasedStore>(ProviderName.SQLite)
				.DynamicColumnsStore(e => e.SQLiteValues)
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new FluentMetadataBasedStore { Id = 5 };
				obj.SQLiteValues.Add("Name", "test_name");
				db.Insert(obj);

				var data = db.GetTable<FluentMetadataBasedStore>().ToList();
				Assert.That(data, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(5));
					Assert.That(data[0].Values, Is.Empty);
					Assert.That(data[0].SQLiteValues, Has.Count.EqualTo(1));
					Assert.That(data[0].SQLiteValues.Keys.Single(), Is.EqualTo("Name"));
					Assert.That(data[0].SQLiteValues["Name"], Is.EqualTo("test_name"));
				}
			}
		}

		[Test]
		public void TestDynamicColumnStoreAttributeWithConfiguration([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<AttributeMetadataBasedStore>()
				.Property(x => Sql.Property<string>(x, "Name"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new AttributeMetadataBasedStore { Id = 5 };
				obj.SQLiteValues.Add("Name", "test_name");
				db.Insert(obj);

				var data = db.GetTable<AttributeMetadataBasedStore>().ToList();
				Assert.That(data, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(5));
					Assert.That(data[0].Values, Is.Empty);
					Assert.That(data[0].SQLiteValues, Has.Count.EqualTo(1));
					Assert.That(data[0].SQLiteValues.Keys.Single(), Is.EqualTo("Name"));
					Assert.That(data[0].SQLiteValues["Name"], Is.EqualTo("test_name"));
				}
			}
		}

		[Test]
		public void DynamicColumnStoreIssue1521([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();

			var builder = new FluentMappingBuilder(ms);
			builder.Entity<FluentMetadataBasedStore>()
				.HasColumn(e => e.Id)
				.DynamicColumnsStore(e => e.Values)
				.Property(x => Sql.Property<string>(x, "Name"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new FluentMetadataBasedStore { Id = 5 };
				obj.Values.Add("Name", "test_name");
				db.Insert(obj);

				var data = db.GetTable<FluentMetadataBasedStore>().ToList();
				Assert.That(data, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(5));
					Assert.That(data[0].Values, Has.Count.EqualTo(1));
					Assert.That(data[0].Values.Keys.Single(), Is.EqualTo("Name"));
					Assert.That(data[0].Values["Name"], Is.EqualTo("test_name"));
				}
			}
		}

		[Test]
		public void TestDynamicColumnStoreInstanceAccessors([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<InstanceGetterSetterMethods>()
				.Property(x => Sql.Property<string>(x, "Name"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new InstanceGetterSetterMethods { Id = 5 };
				obj.Values.Add("Name", "test_name");
				db.Insert(obj);

				var data = db.GetTable<InstanceGetterSetterMethods>().ToList();
				Assert.That(data, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(5));
					Assert.That(data[0].SQLiteValues, Is.Empty);
					Assert.That(data[0].Values, Has.Count.EqualTo(1));
					Assert.That(data[0].Values.Keys.Single(), Is.EqualTo("Name"));
					Assert.That(data[0].Values["Name"], Is.EqualTo("test_name"));
				}
			}
		}

		[Test]
		public void TestDynamicColumnStoreStaticAccessors([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			StaticGetterSetterMethods.InstanceValues.Clear();
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<StaticGetterSetterMethods>()
				.Property(x => Sql.Property<string>(x, "Name"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new StaticGetterSetterMethods { Id = 5 };
				StaticGetterSetterMethods.InstanceValues.Add(5, new Dictionary<string, object>());
				StaticGetterSetterMethods.InstanceValues[5].Add("Name", "test_name");
				db.Insert(obj);

				StaticGetterSetterMethods.InstanceValues.Clear();

				var data = db.GetTable<StaticGetterSetterMethods>().ToList();
				Assert.That(data, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(5));
					Assert.That(data[0].SQLiteValues, Is.Empty);
					Assert.That(data[0].Values, Is.Empty);
					Assert.That(StaticGetterSetterMethods.InstanceValues, Has.Count.EqualTo(1));
					Assert.That(StaticGetterSetterMethods.InstanceValues.Keys.Single(), Is.EqualTo(5));
					Assert.That(StaticGetterSetterMethods.InstanceValues[5], Has.Count.EqualTo(1));
					Assert.That(StaticGetterSetterMethods.InstanceValues[5].Keys.Single(), Is.EqualTo("Name"));
					Assert.That(StaticGetterSetterMethods.InstanceValues[5]["Name"], Is.EqualTo("test_name"));
				}
			}
		}

		[Test]
		public void TestDynamicColumnStoreInstanceExpressionAccessors([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<InstanceGetterSetterExpressionMethods>()
				.Property(x => Sql.Property<string>(x, "Name"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new InstanceGetterSetterExpressionMethods { Id = 5 };
				obj.Values.Add("Name", "test_name");
				db.Insert(obj);

				var data = db.GetTable<InstanceGetterSetterExpressionMethods>().ToList();
				Assert.That(data, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(5));
					Assert.That(data[0].SQLiteValues, Is.Empty);
					Assert.That(data[0].Values, Has.Count.EqualTo(1));
					Assert.That(data[0].Values.Keys.Single(), Is.EqualTo("Name"));
					Assert.That(data[0].Values["Name"], Is.EqualTo("test_name"));
				}
			}
		}

		[Test]
		public void TestDynamicColumnStoreStaticExpressionAccessors([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			StaticGetterSetterExpressionMethods.InstanceValues.Clear();

			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<StaticGetterSetterExpressionMethods>()
				.Property(x => Sql.Property<string>(x, "Name"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new StaticGetterSetterExpressionMethods { Id = 5 };
				StaticGetterSetterExpressionMethods.InstanceValues.Add(5, new Dictionary<string, object>());
				StaticGetterSetterExpressionMethods.InstanceValues[5].Add("Name", "test_name");
				db.Insert(obj);

				StaticGetterSetterExpressionMethods.InstanceValues.Clear();

				var data = db.GetTable<StaticGetterSetterExpressionMethods>().ToList();
				Assert.That(data, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(5));
					Assert.That(data[0].SQLiteValues, Is.Empty);
					Assert.That(data[0].Values, Is.Empty);
					Assert.That(StaticGetterSetterExpressionMethods.InstanceValues, Has.Count.EqualTo(1));
					Assert.That(StaticGetterSetterExpressionMethods.InstanceValues.Keys.Single(), Is.EqualTo(5));
					Assert.That(StaticGetterSetterExpressionMethods.InstanceValues[5], Has.Count.EqualTo(1));
					Assert.That(StaticGetterSetterExpressionMethods.InstanceValues[5].Keys.Single(), Is.EqualTo("Name"));
					Assert.That(StaticGetterSetterExpressionMethods.InstanceValues[5]["Name"], Is.EqualTo("test_name"));
				}
			}
		}

		[Test]
		public void TestDynamicColumnStoreInstanceConfigurationAccessors([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<SQLiteInstanceGetterSetterMethods>()
				.Property(x => Sql.Property<string>(x, "Name"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new SQLiteInstanceGetterSetterMethods { Id = 5 };
				obj.SQLiteValues.Add("Name", "test_name");
				db.Insert(obj);

				var data = db.GetTable<SQLiteInstanceGetterSetterMethods>().ToList();
				Assert.That(data, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(5));
					Assert.That(data[0].Values, Is.Empty);
					Assert.That(data[0].SQLiteValues, Has.Count.EqualTo(1));
					Assert.That(data[0].SQLiteValues.Keys.Single(), Is.EqualTo("Name"));
					Assert.That(data[0].SQLiteValues["Name"], Is.EqualTo("test_name"));
				}
			}
		}

		[Test]
		public void TestDynamicColumnStoreGetterSetterVsStorageMethods1([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<GetterSetterVsStorageMethods1>()
				.Property(x => Sql.Property<string>(x, "Name"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new GetterSetterVsStorageMethods1 { Id = 5 };
				obj.SQLiteValues.Add("Name", "test_name");
				db.Insert(obj);

				var data = db.GetTable<GetterSetterVsStorageMethods1>().ToList();
				Assert.That(data, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(5));
					Assert.That(data[0].Values, Is.Empty);
					Assert.That(data[0].SQLiteValues, Has.Count.EqualTo(1));
					Assert.That(data[0].SQLiteValues.Keys.Single(), Is.EqualTo("Name"));
					Assert.That(data[0].SQLiteValues["Name"], Is.EqualTo("test_name"));
				}
			}
		}

		[Test]
		public void TestDynamicColumnStoreGetterSetterVsStorageMethods2([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<GetterSetterVsStorageMethods2>()
				.Property(x => Sql.Property<string>(x, "Name"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new GetterSetterVsStorageMethods2 { Id = 5 };
				obj.SQLiteValues.Add("Name", "test_name");
				db.Insert(obj);

				var data = db.GetTable<GetterSetterVsStorageMethods2>().ToList();
				Assert.That(data, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(5));
					Assert.That(data[0].Values, Is.Empty);
					Assert.That(data[0].SQLiteValues, Has.Count.EqualTo(1));
					Assert.That(data[0].SQLiteValues.Keys.Single(), Is.EqualTo("Name"));
					Assert.That(data[0].SQLiteValues["Name"], Is.EqualTo("test_name"));
				}
			}
		}

		[Test]
		public void TestDynamicColumnStoreGetterSetterVsStorageMethodsConflict([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<GetterSetterVsStorageMethodsConflict>()
				.Property(x => Sql.Property<string>(x, "Name"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new GetterSetterVsStorageMethodsConflict { Id = 5 };
				obj.Values.Add("Name", "test_name");

				Assert.Throws<LinqToDBException>(() => db.Insert(obj));
				Assert.Throws<LinqToDBException>(() => db.GetTable<GetterSetterVsStorageMethodsConflict>().ToList());
			}
		}

		[Test]
		public void TestDynamicColumnStoreExpressions([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var storage = new Dictionary<int, Dictionary<string, object?>>();

			var ms = new MappingSchema();
			var builder = new FluentMappingBuilder(ms);

			builder.Entity<CustomSetterGetterBase>()
				.DynamicPropertyAccessors(
					(instance, property, defaultValue) => Getter(storage, instance, property, defaultValue),
					(instance, property, value) => Setter(storage, instance, property, value))
				.HasColumn(e => e.Id)
				.Property(x => Sql.Property<string>(x, "Name"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new CustomSetterGetterBase { Id = 5 };
				storage.Add(5, new Dictionary<string, object?>());
				storage[5].Add("Name", "test_name");
				db.Insert(obj);

				var data = db.GetTable<CustomSetterGetterBase>().ToList();
				Assert.That(data, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(5));
					Assert.That(data[0].SQLiteValues, Is.Empty);
					Assert.That(data[0].Values, Is.Empty);
					Assert.That(storage, Has.Count.EqualTo(1));
					Assert.That(storage.Keys.Single(), Is.EqualTo(5));
					Assert.That(storage[5], Has.Count.EqualTo(1));
					Assert.That(storage[5].Keys.Single(), Is.EqualTo("Name"));
					Assert.That(storage[5]["Name"], Is.EqualTo("test_name"));
				}
			}
		}

		static object? Getter(IDictionary<int, Dictionary<string, object?>> storage, CustomSetterGetterBase instance, string property, object? defaultValue)
		{
			if (!storage.TryGetValue(instance.Id, out var values))
				return defaultValue;

			if (!values.TryGetValue(property, out var value))
				value = defaultValue;

			return value;
		}

		static void Setter(IDictionary<int, Dictionary<string, object?>> storage, CustomSetterGetterBase instance, string property, object? value)
		{
			if (!storage.TryGetValue(instance.Id, out var values))
				storage.Add(instance.Id, values = new Dictionary<string, object?>());

			values[property] = value;
		}

		[Test]
		public void TestDynamicColumnStoreAttributeDefaultValue([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();
			ms.SetDefaultValue(typeof(string), "me_default");

			new FluentMappingBuilder(ms)
				.Entity<AttributeMetadataBasedStore>()
				.Property(x => Sql.Property<string>(x, "Name"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new AttributeMetadataBasedStore { Id = 5 };
				db.Insert(obj);

				var data = db.GetTable<AttributeMetadataBasedStore>().ToList();
				Assert.That(data, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(5));
					Assert.That(data[0].Values, Is.Empty);
					Assert.That(data[0].SQLiteValues, Has.Count.EqualTo(1));
					Assert.That(data[0].SQLiteValues.Keys.Single(), Is.EqualTo("Name"));
					Assert.That(data[0].SQLiteValues["Name"], Is.EqualTo("me_default"));
				}
			}
		}

		[Test]
		public void TestDynamicColumnStoreAccessorsDefaultValue([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();
			ms.SetDefaultValue(typeof(string), "accessor_def");

			new FluentMappingBuilder(ms)
				.Entity<InstanceGetterSetterMethods>()
				.Property(x => Sql.Property<string>(x, "Name"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new InstanceGetterSetterMethods { Id = 5 };
				db.Insert(obj);

				var data = db.GetTable<InstanceGetterSetterMethods>().ToList();
				Assert.That(data, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(data[0].Id, Is.EqualTo(5));
					Assert.That(data[0].SQLiteValues, Is.Empty);
					Assert.That(data[0].Values, Has.Count.EqualTo(1));
					Assert.That(data[0].Values.Keys.Single(), Is.EqualTo("Name"));
					Assert.That(data[0].Values["Name"], Is.EqualTo("accessor_def"));
				}
			}
		}

		[Test]
		public void TestDynamicColumnStoreMultipleGetterSetters([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<MultipleGetterSetterMethods>()
				.Property(x => Sql.Property<string>(x, "Name"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new MultipleGetterSetterMethods { Id = 5 };
				obj.Values.Add("Name", "test_name");

				Assert.Throws<LinqToDBException>(() => db.Insert(obj));
				Assert.Throws<LinqToDBException>(() => db.GetTable<MultipleGetterSetterMethods>().ToList());
			}
		}

		[Test]
		public void TestDynamicColumnStoreNoGetterSetters([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<NoGetterSetterMethods>()
				.Property(x => Sql.Property<string>(x, "Name"))
				.Build();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DynamicColumnsTestFullTable>())
			{
				var obj = new NoGetterSetterMethods { Id = 5 };
				obj.Values.Add("Name", "test_name");

				Assert.Throws<LinqToDBException>(() => db.Insert(obj));
				Assert.Throws<LinqToDBException>(() => db.GetTable<NoGetterSetterMethods>().ToList());
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3859")]
		public void Issue3859Test1([DataSources] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<FluentMetadataBasedStore>()
					.DynamicColumnsStore(x => x.Values)
					.Property(x => x.Id).IsColumn()
				.Build();

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<FluentMetadataBasedStore>();

			tb.Select(x => new
			{
				Value1 = x.Values["All"],
				Value2 = x.Id
			}).ToArray();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3859")]
		public void Issue3859Test2([DataSources] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<FluentMetadataBasedStore>()
					.DynamicColumnsStore(x => x.Values)
					.Property(x => x.Id).IsColumn()
				.Build();

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<FluentMetadataBasedStore>();

			var query = tb
				.Select(x => new
				{
					Value1 = x.Values["All"],
					Value2 = x.Id
				})
				.GroupBy(x => x.Value1,
				(g, e) => new
				{
					Value1 = g,
					Value2 = e.Count()
				});

			Assert.That(() => query.ToArray(), Throws.Exception.InstanceOf<LinqToDBException>());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3859")]
		public void Issue3859Test3([DataSources] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<FluentMetadataBasedStore>()
					.DynamicColumnsStore(x => x.Values)
					.Property(x => x.Id).IsColumn()
				.Build();

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<FluentMetadataBasedStore>();

			var query = tb
				.Select(x => new
				{
					Value1 = x.Values["All"],
					Value2 = x.Id
				})
				.AsEnumerable()
				.GroupBy(x => x.Value1,
				(g, e) => new
				{
					Value1 = g,
					Value2 = e.Count()
				});

			query.ToArray();
		}

		#region Issue 2953
		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2953")]
		public void Issue2953Test1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var result = db.FromSql<DbEntity>("SELECT * FROM Person WHERE PersonID = 1").ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Properties.ContainsKey("FirstName"), Is.True);
				Assert.That(result[0].Properties["FirstName"], Is.EqualTo("John"));
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2953")]
		public void Issue2953Test2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataConnection(context);

			var result = db.Query<DbEntity>("SELECT * FROM Person WHERE PersonID = 1").ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].Properties.ContainsKey("FirstName"), Is.True);
				Assert.That(result[0].Properties["FirstName"], Is.EqualTo("John"));
			}
		}

		[Table("Person")]
		sealed class DbEntity
		{
			[Column] public int PersonID { get; set; }

			[DynamicColumnsStore]
			public Dictionary<string, object> Properties { get; set; } = new();
		}
		#endregion
	}
}
