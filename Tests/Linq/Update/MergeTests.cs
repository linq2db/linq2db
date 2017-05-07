using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq;
using Tests.Model;
using LinqToDB.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToDB.DataProvider;

namespace Tests.xUpdate
{
	[TestFixture]
	public partial class MergeTests : TestBase
	{
		class FakeTable<TEntity> : ITable<TEntity>
		{
			IDataContextInfo IExpressionQuery.DataContextInfo
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			Type IQueryable.ElementType
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			Expression IExpressionQuery.Expression
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			Expression IQueryable.Expression
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			Expression IExpressionQuery<TEntity>.Expression
			{
				get
				{
					throw new NotImplementedException();
				}

				set
				{
					throw new NotImplementedException();
				}
			}

			IQueryProvider IQueryable.Provider
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			string IExpressionQuery.SqlText
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			IQueryable IQueryProvider.CreateQuery(Expression expression)
			{
				throw new NotImplementedException();
			}

			IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
			{
				throw new NotImplementedException();
			}

			object IQueryProvider.Execute(Expression expression)
			{
				throw new NotImplementedException();
			}

			TResult IQueryProvider.Execute<TResult>(Expression expression)
			{
				throw new NotImplementedException();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				throw new NotImplementedException();
			}

			IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
			{
				throw new NotImplementedException();
			}
		}

		class FakeMergeSource<TTarget, TSource> : IMergeSource<TTarget, TSource>
		{
		}

		class FakeMergeSource<TEntity> : IMergeSource<TEntity>
		{
		}

		public static IEnumerable<TestCaseData> _nullParameterCases
		{
			get
			{
				return new TestDelegate[]
				{
					() => MergeExtensions.From<Child, Child>(null, new Child[0], (t, s) => true),
					() => MergeExtensions.From<Child, Child>(new FakeTable<Child>(), (IEnumerable<Child>)null, (t, s) => true),
					() => MergeExtensions.From<Child, Child>(new FakeTable<Child>(), new Child[0], null),

					() => MergeExtensions.From<Child, Child>(null, new Child[0].AsQueryable(), (t, s) => true),
					() => MergeExtensions.From<Child, Child>(new FakeTable<Child>(), (IQueryable<Child>)null, (t, s) => true),
					() => MergeExtensions.From<Child, Child>(new FakeTable<Child>(), new Child[0].AsQueryable(), null),

					() => MergeExtensions.From<Child>(null, new Child[0]),
					() => MergeExtensions.From<Child>(new FakeTable<Child>(), (IEnumerable<Child>)null),

					() => MergeExtensions.From<Child>(null, new Child[0].AsQueryable()),
					() => MergeExtensions.From<Child>(new FakeTable<Child>(), (IQueryable<Child>)null),

					() => MergeExtensions.From<Child>(null, new Child[0], (t, s) => true),
					() => MergeExtensions.From<Child>(new FakeTable<Child>(), (IEnumerable<Child>)null, (t, s) => true),
					() => MergeExtensions.From<Child>(new FakeTable<Child>(), new Child[0], null),

					() => MergeExtensions.From<Child>(null, new Child[0].AsQueryable(), (t, s) => true),
					() => MergeExtensions.From<Child>(new FakeTable<Child>(), (IQueryable<Child>)null, (t, s) => true),
					() => MergeExtensions.From<Child>(new FakeTable<Child>(), new Child[0].AsQueryable(), null),

					() => MergeExtensions.Insert<Child>(null),

					() => MergeExtensions.Insert<Child>(null, c => true),
					() => MergeExtensions.Insert<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, bool>>)null),

					() => MergeExtensions.Insert<Child>(null, c => c),
					() => MergeExtensions.Insert<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, Child>>)null),

					() => MergeExtensions.Insert<Child>(null, c => true, c => c),
					() => MergeExtensions.Insert<Child>(new FakeMergeSource<Child>(), null, c => c),
					() => MergeExtensions.Insert<Child>(new FakeMergeSource<Child>(), c => true, null),

					() => MergeExtensions.Insert<Child, Child>(null, c => c),
					() => MergeExtensions.Insert<Child, Child>(new FakeMergeSource<Child, Child>(), (Expression<Func<Child, Child>>)null),

					() => MergeExtensions.Insert<Child, Child>(null, c => true, c => c),
					() => MergeExtensions.Insert<Child, Child>(new FakeMergeSource<Child, Child>(), null, c => c),
					() => MergeExtensions.Insert<Child, Child>(new FakeMergeSource<Child, Child>(), c => true, null),

					() => MergeExtensions.Update<Child>(null),

					() => MergeExtensions.Update<Child>(null, (t, s) => true),
					() => MergeExtensions.Update<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, Child, bool>>)null),

					() => MergeExtensions.Update<Child>(null, (t, s) => t),
					() => MergeExtensions.Update<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, Child, Child>>)null),

					() => MergeExtensions.Update<Child>(null, (t, s) => true, (t, s) => t),
					() => MergeExtensions.Update<Child>(new FakeMergeSource<Child>(), null, (t, s) => t),
					() => MergeExtensions.Update<Child>(new FakeMergeSource<Child>(), (t, s) => true, null),

					() => MergeExtensions.Update<Child, Child>(null, (t, s) => t),
					() => MergeExtensions.Update<Child, Child>(new FakeMergeSource<Child, Child>(), (Expression<Func<Child, Child, Child>>)null),

					() => MergeExtensions.Update<Child, Child>(null, (t, s) => true, (t, s) => t),
					() => MergeExtensions.Update<Child, Child>(new FakeMergeSource<Child, Child>(), null, (t, s) => t),
					() => MergeExtensions.Update<Child, Child>(new FakeMergeSource<Child, Child>(), (t, s) => true, null),

					() => MergeExtensions.Delete<Child>(null),

					() => MergeExtensions.Delete<Child>(null, (t, s) => true),
					() => MergeExtensions.Delete<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, Child, bool>>)null),

					() => MergeExtensions.Delete<Child, Child>(null),

					() => MergeExtensions.Delete<Child, Child>(null, (t, s) => true),
					() => MergeExtensions.Delete<Child, Child>(new FakeMergeSource<Child, Child>(), (Expression<Func<Child, Child, bool>>)null),

					() => MergeExtensions.UpdateBySource<Child>(null, t => t),
					() => MergeExtensions.UpdateBySource<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, Child>>)null),

					() => MergeExtensions.UpdateBySource<Child>(null, t => true, t => t),
					() => MergeExtensions.UpdateBySource<Child>(new FakeMergeSource<Child>(), null, t => t),
					() => MergeExtensions.UpdateBySource<Child>(new FakeMergeSource<Child>(), t => true, null),

					() => MergeExtensions.UpdateBySource<Child, Child>(null, t => t),
					() => MergeExtensions.UpdateBySource<Child, Child>(new FakeMergeSource<Child, Child>(), (Expression<Func<Child, Child>>)null),

					() => MergeExtensions.UpdateBySource<Child, Child>(null, t => true, t => t),
					() => MergeExtensions.UpdateBySource<Child, Child>(new FakeMergeSource<Child, Child>(), null, t => t),
					() => MergeExtensions.UpdateBySource<Child, Child>(new FakeMergeSource<Child, Child>(), t => true, null),

					() => MergeExtensions.DeleteBySource<Child>(null),

					() => MergeExtensions.DeleteBySource<Child>(null, t => true),
					() => MergeExtensions.DeleteBySource<Child>(new FakeMergeSource<Child>(), (Expression<Func<Child, bool>>)null),

					() => MergeExtensions.DeleteBySource<Child, Child>(null),

					() => MergeExtensions.DeleteBySource<Child, Child>(null, t => true),
					() => MergeExtensions.DeleteBySource<Child, Child>(new FakeMergeSource<Child, Child>(), (Expression<Func<Child, bool>>)null),

					() => MergeExtensions.Merge<Child>(null),

					() => MergeExtensions.Merge<Child, Child>(null)
				}.Select((data, i) => new TestCaseData(data).SetName($"Merge.API.Null Parameters.{i}"));
			}
		}

		[TestCaseSource(nameof(_nullParameterCases))]
		public void MergeApiNullParameter(TestDelegate action)
		{
			Assert.Throws<ArgumentNullException>(action);
		}

		class TestMergeBuilder : BasicMergeBuilder
		{
			private readonly int _operationsLimit;
			private readonly bool _bySourceOperationsSupported;
			private readonly bool _deleteOperationSupported;
			private readonly bool _conditionsSupported;
			private readonly bool _duplicateSameTypeOperations;

			protected override int MaxOperationsCount => _operationsLimit;

			protected override bool BySourceOperationsSupported => _bySourceOperationsSupported;

			protected override bool DeleteOperationSupported => _deleteOperationSupported;

			protected override bool OperationPerdicateSupported => _conditionsSupported;

			protected override bool SameTypeOperationsAllowed => _duplicateSameTypeOperations;

			public TestMergeBuilder(
				int operationsLimit,
				bool bySourceOperationsSupported,
				bool deleteOperationSupported,
				bool conditionsSupported,
				bool duplicateSameTypeOperations)
			{
				_operationsLimit = operationsLimit;
				_bySourceOperationsSupported = bySourceOperationsSupported;
				_deleteOperationSupported = deleteOperationSupported;
				_conditionsSupported = conditionsSupported;
				_duplicateSameTypeOperations = duplicateSameTypeOperations;
			}
		}

		public static IEnumerable<TestCaseData> _validationPositiveCases
		{
			get
			{
				var target = new FakeTable<Child>();
				var source = new Child[0];
				var merge = target.From(source);

				var defaultValidator = new BasicMergeBuilder();
				var withBySourceValidator = new TestMergeBuilder(0, true, true, true, true);

				return new object[][]
				{
					// operation count limit
					new object[] { defaultValidator, merge.Delete((t, s) => true).Update().Insert() },
					new object[] { new TestMergeBuilder(2, false, true, true, true), merge.Delete().Insert() },

					// operation types support
					new object[] { defaultValidator, merge.Delete() },
					new object[] { defaultValidator, merge.Update() },
					new object[] { defaultValidator, merge.Insert() },
					new object[] { withBySourceValidator, merge.DeleteBySource() },
					new object[] { withBySourceValidator, merge.UpdateBySource(_ => _) },

					// operation conditions support
					new object[] { defaultValidator, merge.Insert(_ => true) },
					new object[] { defaultValidator, merge.Update((t, s) => true) },
					new object[] { defaultValidator, merge.Delete((t, s) => true) },
					new object[] { withBySourceValidator, merge.DeleteBySource(_ => true) },
					new object[] { withBySourceValidator, merge.UpdateBySource(_ => true, _ => _) },

					// more than one command of the same type allowed
					new object[] { defaultValidator, merge.Insert(_ => true).Insert() },
					new object[] { defaultValidator, merge.Delete((t, s) => true).Delete() },
					new object[] { defaultValidator, merge.Update((t, s) => true).Update() },
					new object[] { withBySourceValidator, merge.DeleteBySource(_ => true).DeleteBySource() },
					new object[] { withBySourceValidator, merge.UpdateBySource(_ => true, _ => _).UpdateBySource(_ => _) },

					// unconditional operations from different match groups
					new object[] { withBySourceValidator, merge.Insert().Delete().DeleteBySource() },
					new object[] { withBySourceValidator, merge.Insert().Update().UpdateBySource(_ => _) },

					// unconditional operations after conditional in the same match group
					new object[] { withBySourceValidator, merge.Update((t, s) => true).Delete() },
					new object[] { withBySourceValidator, merge.Delete((t, s) => true).Update() },
					new object[] { withBySourceValidator, merge.Delete((t, s) => true).Delete() },
					new object[] { withBySourceValidator, merge.Update((t, s) => true).Update() },
					new object[] { withBySourceValidator, merge.Insert(_ => true).Insert() },
					new object[] { withBySourceValidator, merge.UpdateBySource(_ => true, _ => _).UpdateBySource(_ => _) },
					new object[] { withBySourceValidator, merge.DeleteBySource(_ => true).UpdateBySource(_ => _) },
					new object[] { withBySourceValidator, merge.UpdateBySource(_ => true, _ => _).DeleteBySource() },
					new object[] { withBySourceValidator, merge.DeleteBySource(_ => true).DeleteBySource() }
				}.Select((data, i) => new TestCaseData(data).SetName($"Merge.Validation.Positive.{i}"));
			}
		}

		public static IEnumerable<TestCaseData> _validationNegativeCases
		{
			get
			{
				var target = new FakeTable<Child>();
				var source = new Child[0];
				var merge = target.From(source);

				var defaultValidator = new BasicMergeBuilder();
				var noDeleteValidator = new TestMergeBuilder(0, false, false, true, true);
				var noConditionsValidator = new TestMergeBuilder(0, true, true, false, true);
				var noDuplicateOperations = new TestMergeBuilder(0, true, true, true, false);
				var withBySourceValidator = new TestMergeBuilder(0, true, true, true, true);

				return new object[][]
				{
					// operation count limit
					new object[]
					{
						new TestMergeBuilder(2, false, true, true, true),
						merge.Delete().Update().Insert(),
						"Merge cannot contain more than 2 operations for TestProvider provider."
					},

					// operation types support
					new object[] { noDeleteValidator, merge.Delete(), "Merge Delete operation is not supported by TestProvider provider." },
					new object[] { defaultValidator, merge.DeleteBySource(), "Merge Delete By Source operation is not supported by TestProvider provider." },
					new object[] { defaultValidator, merge.UpdateBySource(_ => _), "Merge Update By Source operation is not supported by TestProvider provider." },

					// operation conditions support
					new object[] { noConditionsValidator, merge.Insert(_ => true), "Merge operation conditions are not supported by TestProvider provider." },
					new object[] { noConditionsValidator, merge.Update((t, s) => true), "Merge operation conditions are not supported by TestProvider provider." },
					new object[] { noConditionsValidator, merge.Delete((t, s) => true), "Merge operation conditions are not supported by TestProvider provider." },
					new object[] { noConditionsValidator, merge.DeleteBySource(_ => true), "Merge operation conditions are not supported by TestProvider provider." },
					new object[] { noConditionsValidator, merge.UpdateBySource(_ => true, _ => _), "Merge operation conditions are not supported by TestProvider provider." },

					// more than one command of the same type not allowed
					new object[] { noDuplicateOperations, merge.Insert(_ => true).Insert(), "Multiple operations of the same type are not supported by TestProvider provider." },
					new object[] { noDuplicateOperations, merge.Update((t, s) => true).Update(), "Multiple operations of the same type are not supported by TestProvider provider." },
					new object[] { noDuplicateOperations, merge.Delete((t, s) => true).Delete(), "Multiple operations of the same type are not supported by TestProvider provider." },
					new object[] { noDuplicateOperations, merge.DeleteBySource(_ => true).DeleteBySource(), "Multiple operations of the same type are not supported by TestProvider provider." },
					new object[] { noDuplicateOperations, merge.UpdateBySource(_ => true, _ => _).UpdateBySource(_ => _), "Multiple operations of the same type are not supported by TestProvider provider." },

					// unconditional operations from same match groups
					new object[] { noDuplicateOperations, merge.Update().Delete(), "Multiple unconditional Merge operations not allowed within the same match group." },
					new object[] { noDuplicateOperations, merge.UpdateBySource(_ => _).DeleteBySource(), "Multiple unconditional Merge operations not allowed within the same match group." },

					// conditional operations after unconditional in the same match group
					new object[] { defaultValidator, merge.Insert().Insert(_ => true), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
					new object[] { defaultValidator, merge.Delete().Delete((t, s) => true), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
					new object[] { defaultValidator, merge.Update().Delete((t, s) => true), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
					new object[] { defaultValidator, merge.Delete().Update((t, s) => true), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
					new object[] { defaultValidator, merge.Update().Update((t, s) => true), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
					new object[] { withBySourceValidator, merge.DeleteBySource().DeleteBySource(_ => true), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
					new object[] { withBySourceValidator, merge.UpdateBySource(_ => _).DeleteBySource(_ => true), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
					new object[] { withBySourceValidator, merge.DeleteBySource().UpdateBySource(_ => true, _ => _), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
					new object[] { withBySourceValidator, merge.UpdateBySource(_ => _).UpdateBySource(_ => true, _ => _), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
				}.Select((data, i) => new TestCaseData(data).SetName($"Merge.Validation.Negative.{i}"));
			}
		}

		[TestCaseSource(nameof(_validationPositiveCases))]
		public void MergeOperationsValidationPositive(BasicMergeBuilder validator, MergeDefinition<Child, Child> command)
		{
			validator.Validate(command, "TestProvider");
		}

		[TestCaseSource(nameof(_validationNegativeCases))]
		public void MergeOperationsValidationNegative(BasicMergeBuilder validator, MergeDefinition<Child, Child> command, string error)
		{
			Assert.That(() => validator.Validate(command, "TestProvider"), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(error));
		}
	}
}
