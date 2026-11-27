using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Expressions;
using LinqToDB.Internal.Conversion;
using LinqToDB.Mapping;
using LinqToDB.Metadata;

using NUnit.Framework;

using Shouldly;

namespace Tests.Mapping
{
	public class MappingSchemaTests : TestBase
	{
		[Test]
		public void DefaultValue1()
		{
			var ms = new MappingSchema();

			ms.SetDefaultValue(typeof(int), -1);

			var c = ms.GetConverter<int?,int>()!;

			Assert.That(c(null), Is.EqualTo(-1));
		}

		[Test]
		public void DefaultValue2()
		{
			var ms1 = new MappingSchema();
			var ms2 = new MappingSchema(ms1);

			ms1.SetConvertExpression<int?,int>(i => i.HasValue ? i.Value * 2 : DefaultValue<int>.Value, false);
			ms2.SetDefaultValue(typeof(int), -1);

			var c1 = ms1.GetConverter<int?,int>()!;
			var c2 = ms2.GetConverter<int?,int>()!;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(c1(2), Is.EqualTo(4));
				Assert.That(c1(null), Is.Zero);
				Assert.That(c2(2), Is.EqualTo(4));
				Assert.That(c2(null), Is.EqualTo(-1));
			}
		}

		[Test]
		public void DefaultValue3()
		{
			var ms1 = new MappingSchema();
			var ms2 = new MappingSchema(ms1);

			ms1.SetConvertExpression<int?,int>(i => i!.Value * 2);
			ms2.SetDefaultValue(typeof(int), -1);

			var c1 = ms1.GetConverter<int?,int>()!;
			var c2 = ms2.GetConverter<int?,int>()!;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(c1(2), Is.EqualTo(4));
				Assert.That(c1(null), Is.Zero);
				Assert.That(c2(2), Is.EqualTo(4));
				Assert.That(c2(null), Is.EqualTo(-1));
			}
		}

		[Test]
		public void BaseSchema1()
		{
			var ms1 = new MappingSchema();
			var ms2 = new MappingSchema(ms1);

			ms1.SetDefaultValue(typeof(int), -1);
			ms2.SetDefaultValue(typeof(int), -2);

			var c1 = ms1.GetConverter<int?,int>()!;
			var c2 = ms2.GetConverter<int?,int>()!;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(c1(null), Is.EqualTo(-1));
				Assert.That(c2(null), Is.EqualTo(-2));
			}
		}

		// NonParallelizable: mutates the process-global Common.Convert<TFrom,TTo> converter, which
		// Sql.Convert / ConvertTo read across all tests - would corrupt concurrent conversion-sensitive
		// tests under parallel execution. Restored in a finally so a failed assertion can't leak it.
		[Test, NonParallelizable]
		public void BaseSchema2()
		{
			var ms1 = new MappingSchema();
			var ms2 = new MappingSchema(ms1);

			try
			{
				Convert<DateTime,string>.Lambda = d => d.ToString(DateTimeFormatInfo.InvariantInfo);

				ms1.SetConverter<DateTime,string>(d => d.ToString("M\\/d\\/yyyy h:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
				ms2.SetConverter<DateTime,string>(d => d.ToString("dd.MM.yyyy HH:mm:ss",  System.Globalization.CultureInfo.InvariantCulture));

				{
					var c0 = Convert<DateTime,string>.Lambda;
					var c1 = ms1.GetConverter<DateTime,string>()!;
					var c2 = ms2.GetConverter<DateTime,string>()!;
					using (Assert.EnterMultipleScope())
					{
						Assert.That(c0(new DateTime(2012, 1, 20, 16, 30, 40, 50, DateTimeKind.Utc)), Is.EqualTo("01/20/2012 16:30:40"));
						Assert.That(c1(new DateTime(2012, 1, 20, 16, 30, 40, 50, DateTimeKind.Utc)), Is.EqualTo("1/20/2012 4:30:40"));
						Assert.That(c2(new DateTime(2012, 1, 20, 16, 30, 40, 50, DateTimeKind.Utc)), Is.EqualTo("20.01.2012 16:30:40"));
					}
				}

				Convert<string,DateTime>.Expression = s => DateTime.Parse(s, DateTimeFormatInfo.InvariantInfo);

				ms1.SetConvertExpression<string,DateTime>(s => DateTime.Parse(s, new CultureInfo("en-US", false).DateTimeFormat));
				ms2.SetConvertExpression<string,DateTime>(s => DateTime.Parse(s, new CultureInfo("ru-RU", false).DateTimeFormat));

				{
					var c0 = Convert<string,DateTime>.Lambda;
					var c1 = ms1.GetConverter<string,DateTime>()!;
					var c2 = ms2.GetConverter<string,DateTime>()!;
					using (Assert.EnterMultipleScope())
					{
						Assert.That(c0("01/20/2012 16:30:40"), Is.EqualTo(new DateTime(2012, 1, 20, 16, 30, 40)));
						Assert.That(c1("1/20/2012 4:30:40 PM"), Is.EqualTo(new DateTime(2012, 1, 20, 16, 30, 40)));
						Assert.That(c2("20.01.2012 16:30:40"), Is.EqualTo(new DateTime(2012, 1, 20, 16, 30, 40)));
					}
				}
			}
			finally
			{
				// Restore the process-global converters this test overrode; otherwise they leak to every
				// later test (e.g. ConvertTests.ToSqlTime would resolve string->DateTime via DateTime.Parse).
				Convert<DateTime,string>.Lambda     = null;
				Convert<string,DateTime>.Expression = null;
			}
		}

		[Test]
		public void CultureInfo()
		{
			var ms = new MappingSchema();

			var ci = (CultureInfo)new CultureInfo("ru-RU", false).Clone();

			ci.DateTimeFormat.FullDateTimePattern = "dd.MM.yyyy HH:mm:ss";
			ci.DateTimeFormat.LongDatePattern = "dd.MM.yyyy";
			ci.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
			ci.DateTimeFormat.LongTimePattern = "HH:mm:ss";
			ci.DateTimeFormat.ShortTimePattern = "HH:mm:ss";

			ms.SetCultureInfo(ci);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(ms.GetConverter<DateTime, string>()!(new DateTime(2012, 1, 20, 16, 30, 40)), Is.EqualTo("20.01.2012 16:30:40"));
				Assert.That(ms.GetConverter<string, DateTime>()!("20.01.2012 16:30:40"), Is.EqualTo(new DateTime(2012, 1, 20, 16, 30, 40)));
				Assert.That(ms.GetConverter<decimal, string>()!(100000.999m), Is.EqualTo("100000,999"));
				Assert.That(ms.GetConverter<string, decimal>()!("100000,999"), Is.EqualTo(100000.999m));
				//Assert.AreEqual(100000.999m,                           ConvertTo<decimal>.From("100000.999")); this will fail if System Locale is ru-RU
				Assert.That(ms.GetConverter<double, string>()!(100000.999), Is.EqualTo("100000,999"));
				Assert.That(ms.GetConverter<string, double>()!("100000,999"), Is.EqualTo(100000.999));
			}
		}

		sealed class AttrTest
		{
			[MapValue(Value = 1)]
			[MapValue(Value = 2, Configuration = "2")]
			[MapValue(Value = 3, Configuration = "3")]
			public int Field1;
		}

		[Test]
		public void AttributeTest1()
		{
			var ms = new MappingSchema("2");

			var attrs = ms.GetAttributes<MapValueAttribute>(
				typeof(AttrTest),
				MemberHelper.FieldOf<AttrTest>(a => a.Field1));

			Assert.That(attrs, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(attrs[0].Value, Is.EqualTo(2));
				Assert.That(attrs[1].Value, Is.EqualTo(1));
			}
		}

		[Test]
		public void AttributeTest2()
		{
			var ms = new MappingSchema("2", new MappingSchema("3"));

			var attrs = ms.GetAttributes<MapValueAttribute>(
				typeof(AttrTest),
				MemberHelper.FieldOf<AttrTest>(a => a.Field1));

			Assert.That(attrs, Has.Length.EqualTo(3));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(attrs[0].Value, Is.EqualTo(2));
				Assert.That(attrs[1].Value, Is.EqualTo(3));
				Assert.That(attrs[2].Value, Is.EqualTo(1));
			}
		}

		[Test]
		public void AttributeTest3()
		{
			var ms = new MappingSchema("3", new MappingSchema("2"));

			var attrs = ms.GetAttributes<MapValueAttribute>(
				typeof(AttrTest),
				MemberHelper.FieldOf<AttrTest>(a => a.Field1));

			Assert.That(attrs, Has.Length.EqualTo(3));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(attrs[0].Value, Is.EqualTo(3));
				Assert.That(attrs[1].Value, Is.EqualTo(2));
				Assert.That(attrs[2].Value, Is.EqualTo(1));
			}
		}

		[Test]
		public void AttributeTest4()
		{
			var attrs = MappingSchema.Default.GetAttributes<MapValueAttribute>(
				typeof(AttrTest),
				MemberHelper.FieldOf<AttrTest>(a => a.Field1));

			Assert.That(attrs, Has.Length.EqualTo(1));
		}

		[Test]
		public void AttributeTest5()
		{
			var attrs = MappingSchema.Default.GetAttributes<MapValueAttribute>(
				typeof(AttrTest),
				MemberHelper.FieldOf<AttrTest>(a => a.Field1));

			Assert.That(attrs, Has.Length.EqualTo(1));
			Assert.That(attrs[0].Value, Is.EqualTo(1));
		}

		const string Data =
			@"<?xml version='1.0' encoding='utf-8' ?>
			<Types>
				<Type Name='AttrTest'>
					<Member Name='Field1'>
						<MapValueAttribute>
							<Configuration Value='3' />
							<Value Value='30' Type='System.Int32' />
						</MapValueAttribute>
					</Member>
				</Type>
			</Types>";

		[Test]
		public void AttributeTest6()
		{
			var ms3 = new MappingSchema("3");
			ms3.AddMetadataReader(new XmlAttributeReader(new MemoryStream(Encoding.UTF8.GetBytes(Data))));

			var ms = new MappingSchema("2", ms3);

			var attrs = ms.GetAttributes<MapValueAttribute>(
				typeof(AttrTest),
				MemberHelper.FieldOf<AttrTest>(a => a.Field1));

			Assert.That(attrs, Has.Length.EqualTo(4));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(attrs[0].Value, Is.EqualTo(2));
				Assert.That(attrs[1].Value, Is.EqualTo(30));
				Assert.That(attrs[2].Value, Is.EqualTo(3));
				Assert.That(attrs[3].Value, Is.EqualTo(1));
			}
		}

		[Test]
		public void AttributeTest7()
		{
			var ms3 = new MappingSchema("3");
			ms3.AddMetadataReader(new XmlAttributeReader(new MemoryStream(Encoding.UTF8.GetBytes(Data))));

			var ms = new MappingSchema("2", ms3);

			var attrs = ms.GetAttributes<MapValueAttribute>(
				typeof(AttrTest),
				MemberHelper.FieldOf<AttrTest>(a => a.Field1));

			Assert.That(attrs, Has.Length.EqualTo(4));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(attrs[0].Value, Is.EqualTo(2));
				Assert.That(attrs[1].Value, Is.EqualTo(30));
				Assert.That(attrs[2].Value, Is.EqualTo(3));
				Assert.That(attrs[3].Value, Is.EqualTo(1));
			}
		}

		[Test]
		public void AttributeTest8()
		{
			var ms = new MappingSchema("3", new MappingSchema("2"));
			ms.AddMetadataReader(new XmlAttributeReader(new MemoryStream(Encoding.UTF8.GetBytes(Data))));

			var attrs = ms.GetAttributes<MapValueAttribute>(
				typeof(AttrTest),
				MemberHelper.FieldOf<AttrTest>(a => a.Field1));

			Assert.That(attrs, Has.Length.EqualTo(4));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(attrs[1].Value, Is.EqualTo(3));
				Assert.That(attrs[0].Value, Is.EqualTo(30));
				Assert.That(attrs[2].Value, Is.EqualTo(2));
				Assert.That(attrs[3].Value, Is.EqualTo(1));
			}
		}

		enum Enum1
		{
			[MapValue("1", 1), MapValue(2)] Value1,
			[MapValue(1), MapValue("1", 2)] Value2,
		}

		[Test]
		public void ConvertEnum1()
		{
			var conv = MappingSchema.Default.GetConverter<int, Enum1>()!;
			Assert.That(conv(2), Is.EqualTo(Enum1.Value1));
		}

		[Test]
		public void ConvertEnum2()
		{
			var conv = new MappingSchema("1").GetConverter<int,Enum1>()!;
			Assert.That(conv(2), Is.EqualTo(Enum1.Value2));
		}

		[Test]
		public void ConvertNullableEnum()
		{
			var schema = new MappingSchema("2");
			Assert.That(schema.GetDefaultValue(typeof(Enum1?)), Is.Null);
			var mapType = ConvertBuilder.GetDefaultMappingFromEnumType(schema, typeof(Enum1?))!;
			Assert.That(mapType, Is.EqualTo(typeof(int?)));
			var convertedValue = Converter.ChangeType(null, mapType, schema);
			Assert.That(convertedValue, Is.Null);
		}

		public class PkTable
		{
			[PrimaryKey, Identity]
			[DataType(DataType.DateTime)]
			public int Id;
		}

		[Column("ParentId", "Parent.Id")]
		public class FkTable
		{
			[PrimaryKey]
			public int Id;

			public PkTable? Parent;
		}

		[Test]
		public void DoNotUseComplexAttributes()
		{
			// local schema (not MappingSchema.Default) so a concurrent Default mutation can't perturb the read
			var ed = new MappingSchema().GetEntityDescriptor(typeof(FkTable));
			var c  = ed.Columns.Single(_ => _.ColumnName == "ParentId");
			using (Assert.EnterMultipleScope())
			{
				Assert.That(c.IsPrimaryKey, Is.False);
				Assert.That(c.IsIdentity, Is.False);
				Assert.That(c.DataType, Is.EqualTo(DataType.DateTime));
			}
		}

		[Repeat(100)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3312")]
		public void ConcurrentGetConvertExpression()
		{
			var ms = new MappingSchema();

			var tasks = new Task[10];

			for (var i = 0; i < tasks.Length; i++)
				tasks[i] = Task.Run(() => ms.GetConvertExpression(typeof(string), typeof(int)));

			Task.WaitAll(tasks);
		}

		// Every <Provider>Tools.GetDataProvider(<enums>…) combination, so a new version or client value is covered without editing this list.
		static IEnumerable<TestCaseData> ProviderFactoryCases()
		{
			var toolsTypes = typeof(ProviderName).Assembly
				.GetExportedTypes()
				.Where(t => t is { IsAbstract: true, IsSealed: true }
					&& t.Namespace?.StartsWith("LinqToDB.DataProvider.", StringComparison.Ordinal) == true
					&& t.Name.EndsWith("Tools", StringComparison.Ordinal))
				.OrderBy(t => t.Name, StringComparer.Ordinal);

			foreach (var type in toolsTypes)
			{
				var factory = type.GetMethod("GetDataProvider", BindingFlags.Public | BindingFlags.Static);

				if (factory == null)
					continue;

				var parameters = factory.GetParameters();
				var enumValues = parameters
					.TakeWhile(p => p.ParameterType.IsEnum)
					.Select(p => Enum
						.GetValues(p.ParameterType)
						.Cast<object>()
						.Where(v => !string.Equals(v.ToString(), "AutoDetect", StringComparison.Ordinal))
						.ToArray())
					.ToList();

				IEnumerable<object[]> combinations = [[]];

				foreach (var values in enumValues)
					combinations = combinations.SelectMany(c => values.Select(v => (object[])[..c, v]));

				foreach (var combination in combinations)
				{
					var arguments = combination
						.Concat(parameters.Skip(combination.Length).Select(p => p.DefaultValue))
						.ToArray();

					yield return new TestCaseData(factory, arguments)
						.SetArgDisplayNames([type.Name, ..combination.Select(v => v.ToString()!)]);
				}
			}
		}

		static string? MissingDriver(Exception? ex)
		{
			for (; ex != null; ex = ex.InnerException)
				if ((ex is InvalidOperationException && ex.Message.StartsWith("Cannot load assembly", StringComparison.Ordinal))
					|| ex is BadImageFormatException or DllNotFoundException or FileNotFoundException or FileLoadException)
					return ex.Message;

			return null;
		}

		[Table]
		sealed class ProviderScopedTable
		{
			[Column] public int Id { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6137")]
		[TestCaseSource(nameof(ProviderFactoryCases))]
		public void ProviderNameScopedMappingApplies(MethodInfo factory, object?[] arguments)
		{
			IDataProvider provider;

			try
			{
				provider = (IDataProvider)factory.Invoke(null, arguments)!;
			}
			catch (TargetInvocationException ex) when (MissingDriver(ex) is { } missing)
			{
				// Which clients load depends on the runtime and on native installs; each runs where its driver does.
				Assert.Ignore(missing);
				return;
			}

			provider.MappingSchema
				.ConfigurationList
				.ShouldContain(provider.Name);

			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ProviderScopedTable>(provider.Name)
				.HasTableName("scoped")
				.Build();

			using var db = new DataConnection(new DataOptions()
				.UseConnectionString(provider, "Server=localhost")
				.UseMappingSchema(ms));

			db.MappingSchema
				.GetEntityDescriptor(typeof(ProviderScopedTable))
				.Name
				.Name
				.ShouldBe("scoped");
		}

		public interface IJsonConvertedValue;

		public class JsonConvertedValue : IJsonConvertedValue
		{
			public int A;
		}

		[Table(Name = "JsonConvertedValueTable")]
		public class JsonConvertedValueTable
		{
			[Column(), NotNull] public int Id { get; set; }

			[Column(DataType = DataType.VarChar), NotNull] public IJsonConvertedValue F { get; set; } = null!;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2000")]
		public void SetConverterForInterfaceType([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();
			ms.SetConverter<IJsonConvertedValue, string>(favs => JsonSerializer.Serialize(favs));
			ms.SetConverter<IJsonConvertedValue, DataParameter>(obj =>
				new DataParameter { Value = JsonSerializer.Serialize(obj), DataType = DataType.NVarChar });
			ms.SetConverter<string, IJsonConvertedValue>(favs => { return JsonSerializer.Deserialize<IJsonConvertedValue>(favs)!; });

			using var db = GetDataContext(context, ms);
			using var table = db.CreateLocalTable<JsonConvertedValueTable>();
			table
				.Value(x => x.Id, 2)
				.Value(x => x.F, new JsonConvertedValue() { A = 5 })
				.Insert();
		}

		public class ConcurrentMappingEntity
		{
			public int Id { get; set; }

			public int Value { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/548")]
		public void ConcurrentAddMetadataReaderAndReadAttributes()
		{
			var ms      = new MappingSchema();

			Assert.That(ms.GetAttributes<PrimaryKeyAttribute>(typeof(ConcurrentMappingEntity), typeof(ConcurrentMappingEntity).GetProperty("Id")!), Is.Empty);

			const int taskCount = 10;

			using var semaphore = new Semaphore(0, taskCount);
			var tasks = new Task[10];

			for (var i = 0; i < taskCount; i++)
				if (i % 2 == 0)
					tasks[i] = new Task(() => ResetMetadataReaders(ms, semaphore));
				else
					tasks[i] = new Task(() => ReadAttributesDuringMetadataReset(ms, semaphore));

			for (var i = 0; i < taskCount; i++)
				tasks[i].Start();

			Thread.Sleep(100);
			semaphore.Release(taskCount);

			Task.WaitAll(tasks);
		}

		[Repeat(100)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/548")]
		public void ConcurrentFluentBuildersKeepAllColumns()
		{
			var ms      = new MappingSchema();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(ms.GetAttributes<ColumnAttribute>(typeof(ConcurrentMappingEntity), typeof(ConcurrentMappingEntity).GetProperty("Id")!), Is.Empty);
				Assert.That(ms.GetAttributes<ColumnAttribute>(typeof(ConcurrentMappingEntity), typeof(ConcurrentMappingEntity).GetProperty("Value")!), Is.Empty);
			}

			const int taskCount = 2;

			using var semaphore1 = new Semaphore(0, taskCount);
			using var semaphore2 = new Semaphore(0, taskCount);
			var tasks = new Task[taskCount];
			var events = new EventWaitHandle[taskCount];

			for (var i = 0; i < taskCount; i++)
			{
				var evt = events[i] = new AutoResetEvent(false);
				if (i % 2 == 0)
					tasks[i] = new Task(() => BuildIdColumnConcurrently(ms, semaphore1, semaphore2, evt));
				else
					tasks[i] = new Task(() => BuildValueColumnConcurrently(ms, semaphore1, semaphore2, evt));
			}

			for (var i = 0; i < taskCount; i++)
				tasks[i].Start();

			WaitHandle.WaitAll(events);
			semaphore1.Release(taskCount);
			WaitHandle.WaitAll(events);
			semaphore2.Release(taskCount);

			Task.WaitAll(tasks);
		}

		/// <summary>
		/// This will reset MappingSchema.MetadataReaders
		/// </summary>
		/// <param name="ms"></param>
		/// <param name="semaphore"></param>
		private void ResetMetadataReaders(MappingSchema ms, Semaphore semaphore)
		{
			try
			{
				semaphore.WaitOne();

				ms.AddMetadataReader(new FluentMetadataReader(new Dictionary<Type, List<MappingAttribute>>(), new Dictionary<MemberInfo, List<MappingAttribute>>(), new List<MemberInfo>()));
			}
			finally
			{
				semaphore.Release();
			}

		}

		/// <summary>
		/// This will iterate through MappingSchema.MetadataReaders
		/// and had a chance to fail on MappingSchema._metadataReaders == null
		/// </summary>
		/// <param name="ms"></param>
		/// <param name="semaphore"></param>
		private void ReadAttributesDuringMetadataReset(MappingSchema ms, Semaphore semaphore)
		{
			try
			{
				semaphore.WaitOne();

				Assert.That(ms.GetAttributes<PrimaryKeyAttribute>(typeof(ConcurrentMappingEntity), typeof(ConcurrentMappingEntity).GetProperty("Id")!), Is.Empty);
			}
			finally
			{
				semaphore.Release();
			}

		}

		/// <summary>
		/// <see cref="BuildIdColumnConcurrently(MappingSchema, Semaphore, Semaphore, EventWaitHandle)"/> and <see cref="BuildValueColumnConcurrently(MappingSchema, Semaphore, Semaphore, EventWaitHandle)"/>
		/// are creating two instances of <see cref="FluentMappingBuilder"/> and have a chance to race in <see cref="MappingSchema.AddMetadataReader(LinqToDB.Metadata.IMetadataReader)"/>
		/// one <see cref="LinqToDB.Metadata.IMetadataReader"/> could be lost
		/// </summary>
		/// <param name="ms"></param>
		/// <param name="semaphore1"></param>
		/// <param name="semaphore2"></param>
		private void BuildIdColumnConcurrently(MappingSchema ms, Semaphore semaphore1, Semaphore semaphore2, EventWaitHandle done)
		{
			done.Set();
			semaphore1.WaitOne();
			var builder = new FluentMappingBuilder(ms);

			builder.Entity<ConcurrentMappingEntity>().Property(_ => _.Id).IsColumn().Build();
			done.Set();
			semaphore2.WaitOne();

#pragma warning disable NUnit2045 // Use Assert.Multiple
			Assert.That(ms.GetAttributes<ColumnAttribute>(typeof(ConcurrentMappingEntity), typeof(ConcurrentMappingEntity).GetProperty("Id")!), Is.Not.Empty);
#pragma warning restore NUnit2045 // Use Assert.Multiple
			Assert.That(ms.GetAttributes<ColumnAttribute>(typeof(ConcurrentMappingEntity), typeof(ConcurrentMappingEntity).GetProperty("Value")!), Is.Not.Empty);

		}

		/// <summary>
		/// <see cref="BuildValueColumnConcurrently(MappingSchema, Semaphore, Semaphore, EventWaitHandle)"/>
		/// </summary>
		/// <param name="ms"></param>
		/// <param name="semaphore1"></param>
		/// <param name="semaphore2"></param>
		private void BuildValueColumnConcurrently(MappingSchema ms, Semaphore semaphore1, Semaphore semaphore2, EventWaitHandle done)
		{
			done.Set();
			semaphore1.WaitOne();
			var builder = new FluentMappingBuilder(ms);

			builder.Entity<ConcurrentMappingEntity>().Property(_ => _.Value).IsColumn().Build();
			done.Set();
			semaphore2.WaitOne();

#pragma warning disable NUnit2045 // Use Assert.Multiple
			Assert.That(ms.GetAttributes<ColumnAttribute>(typeof(ConcurrentMappingEntity), typeof(ConcurrentMappingEntity).GetProperty("Id")!), Is.Not.Empty);
#pragma warning restore NUnit2045 // Use Assert.Multiple
			Assert.That(ms.GetAttributes<ColumnAttribute>(typeof(ConcurrentMappingEntity), typeof(ConcurrentMappingEntity).GetProperty("Value")!), Is.Not.Empty);
		}
	}
}
