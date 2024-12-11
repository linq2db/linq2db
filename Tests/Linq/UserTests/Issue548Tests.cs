using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Mapping;
using LinqToDB.Metadata;

using NUnit.Framework;

namespace Tests.UserTests
{
	/// <summary>
	/// Test checks for race conditions in <see cref="MappingSchema"/> around <see cref="LinqToDB.Metadata.IMetadataReader"/>
	/// https://github.com/linq2db/linq2db/issues/548
	/// </summary>
	[TestFixture]
	public class Issue548Tests
	{
		public class TestEntity
		{
			public int Id { get; set; }

			public int Value { get; set; }
		}

		[Test]
		public void Test1()
		{
			var ms      = new MappingSchema();

			Assert.That(ms.GetAttributes<PrimaryKeyAttribute>(typeof(TestEntity), typeof(TestEntity).GetProperty("Id")!), Is.Empty);

			const int taskCount = 10;

			using (var semaphore = new Semaphore(0, taskCount))
			{
				var tasks = new Task[10];

				for (var i = 0; i < taskCount; i++)
					if (i % 2 == 0)
						tasks[i] = new Task(() => Test1Internal1(ms, semaphore));
					else
						tasks[i] = new Task(() => Test1Internal2(ms, semaphore));

				for (var i = 0; i < taskCount; i++)
					tasks[i].Start();

				Thread.Sleep(100);
				semaphore.Release(taskCount);

				Task.WaitAll(tasks);
			}
		}

		[Test]
		public void Test2()
		{
			var ms      = new MappingSchema();

			Assert.Multiple(() =>
			{
				Assert.That(ms.GetAttributes<ColumnAttribute>(typeof(TestEntity), typeof(TestEntity).GetProperty("Id")!), Is.Empty);
				Assert.That(ms.GetAttributes<ColumnAttribute>(typeof(TestEntity), typeof(TestEntity).GetProperty("Value")!), Is.Empty);
			});

			const int taskCount = 2;

			using (var semaphore1 = new Semaphore(0, taskCount))
			using (var semaphore2 = new Semaphore(0, taskCount))
			{
				var tasks = new Task[taskCount];

				for (var i = 0; i < taskCount; i++)
					if (i % 2 == 0)
						tasks[i] = new Task(() => Test2Internal1(ms, semaphore1, semaphore2));
					else
						tasks[i] = new Task(() => Test2Internal2(ms, semaphore1, semaphore2));

				for (var i = 0; i < taskCount; i++)
					tasks[i].Start();

				Thread.Sleep(100);
				semaphore1.Release(taskCount);
				Thread.Sleep(100);
				semaphore2.Release(taskCount);

				Task.WaitAll(tasks);
			}
		}

		/// <summary>
		/// This will reset MappingSchema.MetadataReaders
		/// </summary>
		/// <param name="ms"></param>
		/// <param name="semaphore"></param>
		private void Test1Internal1(MappingSchema ms, Semaphore semaphore)
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
		private void Test1Internal2(MappingSchema ms, Semaphore semaphore)
		{
			try
			{
				semaphore.WaitOne();

				Assert.That(ms.GetAttributes<PrimaryKeyAttribute>(typeof(TestEntity), typeof(TestEntity).GetProperty("Id")!), Is.Empty);
			}
			finally
			{
				semaphore.Release();
			}

		}

		/// <summary>
		/// <see cref="Test2Internal1(MappingSchema, Semaphore, Semaphore)"/> and <see cref="Test2Internal2(MappingSchema, Semaphore, Semaphore)"/>
		/// are creating two instances of <see cref="FluentMappingBuilder"/> and have a chance to race in <see cref="MappingSchema.AddMetadataReader(LinqToDB.Metadata.IMetadataReader)"/>
		/// one <see cref="LinqToDB.Metadata.IMetadataReader"/> could be lost
		/// </summary>
		/// <param name="ms"></param>
		/// <param name="semaphore1"></param>
		/// <param name="semaphore2"></param>
		private void Test2Internal1(MappingSchema ms, Semaphore semaphore1, Semaphore semaphore2)
		{
			try
			{
				semaphore1.WaitOne();
				var builder = new FluentMappingBuilder(ms);

				builder.Entity<TestEntity>().Property(_ => _.Id).IsColumn().Build();
				semaphore2.WaitOne();

#pragma warning disable NUnit2045 // Use Assert.Multiple
				Assert.That(ms.GetAttributes<ColumnAttribute>(typeof(TestEntity), typeof(TestEntity).GetProperty("Id")!), Is.Not.Empty);
#pragma warning restore NUnit2045 // Use Assert.Multiple
				Assert.That(ms.GetAttributes<ColumnAttribute>(typeof(TestEntity), typeof(TestEntity).GetProperty("Value")!), Is.Not.Empty);
			}
			finally
			{
				semaphore1.Release();
				semaphore2.Release();
			}

		}

		/// <summary>
		/// <see cref="Test2Internal2(MappingSchema, Semaphore, Semaphore)"/>
		/// </summary>
		/// <param name="ms"></param>
		/// <param name="semaphore1"></param>
		/// <param name="semaphore2"></param>
		private void Test2Internal2(MappingSchema ms, Semaphore semaphore1, Semaphore semaphore2)
		{
			try
			{
				semaphore1.WaitOne();
				var builder = new FluentMappingBuilder(ms);

				builder.Entity<TestEntity>().Property(_ => _.Value).IsColumn().Build();
				semaphore2.WaitOne();

#pragma warning disable NUnit2045 // Use Assert.Multiple
				Assert.That(ms.GetAttributes<ColumnAttribute>(typeof(TestEntity), typeof(TestEntity).GetProperty("Id")!), Is.Not.Empty);
#pragma warning restore NUnit2045 // Use Assert.Multiple
				Assert.That(ms.GetAttributes<ColumnAttribute>(typeof(TestEntity), typeof(TestEntity).GetProperty("Value")!), Is.Not.Empty);
			}
			finally
			{
				semaphore1.Release();
				semaphore2.Release();
			}

		}
	}
}
