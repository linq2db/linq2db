using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.NHibernate.Tests.Models.UserTypes;

using NHibernate;

using NUnit.Framework;

using Shouldly;

namespace LinqToDB.NHibernate.Tests
{
	/// <summary>
	/// Exercises the single-column <see cref="global::NHibernate.UserTypes.IUserType"/> bridge: a property mapped
	/// through a user type is exposed to linq2db as a value converter, so linq2db can materialize it, write it, and
	/// use it as a query parameter.
	/// </summary>
	[TestFixture]
	public class UserTypeConversionTests : NHTestBase
	{
		static void SeedPayments(ISessionFactory sf)
		{
			using var session = sf.OpenSession();
			using var tx      = session.BeginTransaction();

			session.GetTable<Payment>().Delete();

			session.Save(new Payment { Id = 1, Amount = new Money(10.5m), Priority = Priority.Low  });
			session.Save(new Payment { Id = 2, Amount = new Money(20.25m), Priority = Priority.High });

			tx.Commit();
		}

		[Test]
		public void UserType_MaterializesThroughConverter(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedPayments(sf);

			using var session = sf.OpenSession();

			var payment = session.GetTable<Payment>().Single(p => p.Id == 2);

			payment.Amount.ShouldBe(new Money(20.25m));
			payment.Priority.ShouldBe(Priority.High);

			// NHibernate's own provider must agree.
			var nhPayment = session.Query<Payment>().Single(p => p.Id == 2);
			nhPayment.Amount.ShouldBe(payment.Amount);
			nhPayment.Priority.ShouldBe(payment.Priority);
		}

		[Test]
		public void UserType_RoundTripsAsQueryParameter(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedPayments(sf);

			using var session = sf.OpenSession();

			// The converted value has to reach SQL as the provider-side value ('H'), not the model value.
			var ids = session.GetTable<Payment>()
				.Where(p => p.Priority == Priority.High)
				.Select(p => p.Id)
				.ToList();

			ids.ShouldBe(new[] { 2 });

			var byAmount = session.GetTable<Payment>()
				.Where(p => p.Amount == new Money(10.5m))
				.Select(p => p.Id)
				.ToList();

			byAmount.ShouldBe(new[] { 1 });
		}

		[Test]
		public void UserType_WritesThroughConverter(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);
			SeedPayments(sf);

			using var session = sf.OpenSession();

			session.GetTable<Payment>()
				.Where(p => p.Id == 1)
				.Set(p => p.Priority, Priority.High)
				.Set(p => p.Amount,   new Money(99.75m))
				.Update();

			// Read back through NHibernate: proves the stored value is what NHibernate itself expects.
			session.Clear();
			var reloaded = session.Query<Payment>().Single(p => p.Id == 1);

			reloaded.Priority.ShouldBe(Priority.High);
			reloaded.Amount.ShouldBe(new Money(99.75m));
		}

		[Test]
		public void UserType_SurvivesBulkCopy(
			[NHIncludeDataSources] string provider,
			[Values(BulkCopyType.RowByRow, BulkCopyType.MultipleRows)] BulkCopyType bulkCopyType)
		{
			var sf = GetSessionFactory(provider);

			using var session = sf.OpenSession();

			session.GetTable<Payment>().Delete();

			var rows = new[]
			{
				new Payment { Id = 11, Amount = new Money(1.25m), Priority = Priority.High },
				new Payment { Id = 12, Amount = new Money(2.5m),  Priority = Priority.Low  },
			};

			session.BulkCopy(new BulkCopyOptions { BulkCopyType = bulkCopyType }, rows);

			session.Clear();

			// Read back through NHibernate: bulk-copied values must be stored the way its user types expect
			// (a Priority written as its enum ordinal rather than 'H'/'L' would come back as Low here).
			var loaded = session.Query<Payment>().OrderBy(p => p.Id).ToList();

			loaded.Count.ShouldBe(2);
			loaded[0].Priority.ShouldBe(Priority.High);
			loaded[0].Amount.ShouldBe(new Money(1.25m));
			loaded[1].Priority.ShouldBe(Priority.Low);
			loaded[1].Amount.ShouldBe(new Money(2.5m));
		}

		[Test]
		public void UserType_EmitsValueConverterMetadata(
			[NHIncludeDataSources] string provider)
		{
			var sf = GetSessionFactory(provider);

			var reader = LinqToDBForNHibernateTools.GetMetadataReader(sf)!;

			var member = typeof(Payment).GetProperty(nameof(Payment.Priority))!;
			var attr   = reader.GetAttributes(typeof(Payment), member).OfType<ValueConverterAttribute>().SingleOrDefault();

			attr.ShouldNotBeNull();
			attr!.ValueConverter.ShouldNotBeNull();

			// The converter maps the model value to the provider value the user type writes.
			var toProvider = attr.ValueConverter!.ToProviderExpression.Compile();
			toProvider.DynamicInvoke(Priority.High).ShouldBe("H");
		}
	}
}
