using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Tests.Model;
using LinqToDB.SchemaProvider;
using LinqToDB.SqlProvider;
using System.Data;

namespace Tests.Merge
{
	public partial class MergeTests
	{
		public static IEnumerable<TestCaseData> _validationNegativeCases
		{
			get
			{
				var merge = new FakeTable<Child>().Merge().Using(new Child[0]).OnTargetKey();

				return new object[][]
				{
					// operation count limit
					new object[] { new ValidationTestMergeBuilder(merge.DeleteWhenMatched().UpdateWhenMatched().InsertWhenNotMatched()).WithLimit(2), "Merge cannot contain more than 2 operations for TestProvider provider." },

					// operation types support
					new object[] { new ValidationTestMergeBuilder(merge.DeleteWhenMatched()).WithoutDelete(), "Merge Delete operation is not supported by TestProvider provider." },
					new object[] { new ValidationTestMergeBuilder(merge.DeleteWhenNotMatchedBySource()), "Merge Delete By Source operation is not supported by TestProvider provider." },
					new object[] { new ValidationTestMergeBuilder(merge.UpdateWhenNotMatchedBySource(_ => _)), "Merge Update By Source operation is not supported by TestProvider provider." },

					// operation conditions support
					new object[] { new ValidationTestMergeBuilder(merge.InsertWhenNotMatchedAnd(_ => true)).WithoutConditions(), "Merge operation conditions are not supported by TestProvider provider." },
					new object[] { new ValidationTestMergeBuilder(merge.UpdateWhenMatchedAnd((t, s) => true)).WithoutConditions(), "Merge operation conditions are not supported by TestProvider provider." },
					new object[] { new ValidationTestMergeBuilder(merge.DeleteWhenMatchedAnd((t, s) => true)).WithoutConditions(), "Merge operation conditions are not supported by TestProvider provider." },
					new object[] { new ValidationTestMergeBuilder(merge.DeleteWhenNotMatchedBySourceAnd(_ => true)).WithoutConditions().WithBySource(), "Merge operation conditions are not supported by TestProvider provider." },
					new object[] { new ValidationTestMergeBuilder(merge.UpdateWhenNotMatchedBySourceAnd(_ => true, _ => _)).WithoutConditions().WithBySource(), "Merge operation conditions are not supported by TestProvider provider." },

					// more than one command of the same type not allowed
					new object[] { new ValidationTestMergeBuilder(merge.InsertWhenNotMatchedAnd(_ => true).InsertWhenNotMatched()).WithoutDuplicates(), "Multiple operations of the same type are not supported by TestProvider provider." },
					new object[] { new ValidationTestMergeBuilder(merge.UpdateWhenMatchedAnd((t, s) => true).UpdateWhenMatched()).WithoutDuplicates(), "Multiple operations of the same type are not supported by TestProvider provider." },
					new object[] { new ValidationTestMergeBuilder(merge.DeleteWhenMatchedAnd((t, s) => true).DeleteWhenMatched()).WithoutDuplicates(), "Multiple operations of the same type are not supported by TestProvider provider." },
					new object[] { new ValidationTestMergeBuilder(merge.DeleteWhenNotMatchedBySourceAnd(_ => true).DeleteWhenNotMatchedBySource()).WithoutDuplicates().WithBySource(), "Multiple operations of the same type are not supported by TestProvider provider." },
					new object[] { new ValidationTestMergeBuilder(merge.UpdateWhenNotMatchedBySourceAnd(_ => true, _ => _).UpdateWhenNotMatchedBySource(_ => _)).WithoutDuplicates().WithBySource(), "Multiple operations of the same type are not supported by TestProvider provider." },

					// unconditional operations from same match groups
					new object[] { new ValidationTestMergeBuilder(merge.UpdateWhenMatched().DeleteWhenMatched()).WithoutDuplicates(), "Multiple unconditional Merge operations not allowed within the same match group." },
					new object[] { new ValidationTestMergeBuilder(merge.UpdateWhenNotMatchedBySource(_ => _).DeleteWhenNotMatchedBySource()).WithoutDuplicates().WithBySource(), "Multiple unconditional Merge operations not allowed within the same match group." },

					// conditional operations after unconditional in the same match group
					new object[] { new ValidationTestMergeBuilder(merge.InsertWhenNotMatched().InsertWhenNotMatchedAnd(_ => true)), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
					new object[] { new ValidationTestMergeBuilder(merge.DeleteWhenMatched().DeleteWhenMatchedAnd((t, s) => true)), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
					new object[] { new ValidationTestMergeBuilder(merge.UpdateWhenMatched().DeleteWhenMatchedAnd((t, s) => true)), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
					new object[] { new ValidationTestMergeBuilder(merge.DeleteWhenMatched().UpdateWhenMatchedAnd((t, s) => true)), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
					new object[] { new ValidationTestMergeBuilder(merge.UpdateWhenMatched().UpdateWhenMatchedAnd((t, s) => true)), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
					new object[] { new ValidationTestMergeBuilder(merge.DeleteWhenNotMatchedBySource().DeleteWhenNotMatchedBySourceAnd(_ => true)).WithBySource(), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
					new object[] { new ValidationTestMergeBuilder(merge.UpdateWhenNotMatchedBySource(_ => _).DeleteWhenNotMatchedBySourceAnd(_ => true)).WithBySource(), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
					new object[] { new ValidationTestMergeBuilder(merge.DeleteWhenNotMatchedBySource().UpdateWhenNotMatchedBySourceAnd(_ => true, _ => _)).WithBySource(), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },
					new object[] { new ValidationTestMergeBuilder(merge.UpdateWhenNotMatchedBySource(_ => _).UpdateWhenNotMatchedBySourceAnd(_ => true, _ => _)).WithBySource(), "Unconditional Merge operation cannot be followed by operation with condition within the same match group." },

					// UpdateWithDelete related validations
					new object[] { new ValidationTestMergeBuilder(merge.UpdateWhenMatchedAnd((t, s) => true).DeleteWhenMatchedAnd((t, s) => true)).WithUpdateWithDelete(), "Delete and Update operations in the same Merge command not supported by TestProvider provider." },
					new object[] { new ValidationTestMergeBuilder(merge.UpdateWhenMatchedAndThenDelete((t, s) => true, (t, s) => true)), "UpdateWithDelete operation not supported by TestProvider provider." },
					new object[] { new ValidationTestMergeBuilder(merge.UpdateWhenMatchedAndThenDelete((t, s) => true, (t, s) => true).UpdateWhenMatched((t, s) => t)).WithUpdateWithDelete(), "Update operation with UpdateWithDelete operation in the same Merge command not supported by TestProvider provider." },
					new object[] { new ValidationTestMergeBuilder(merge.UpdateWhenMatchedAndThenDelete((t, s) => true, (t, s) => true).DeleteWhenMatchedAnd((t, s) => true)).WithUpdateWithDelete(), "Delete operation with UpdateWithDelete operation in the same Merge command not supported by TestProvider provider." },
				}.Select((data, i) => new TestCaseData(data).SetName($"Merge.Validation.Negative.{i}"));
			}
		}

		public static IEnumerable<TestCaseData> _validationPositiveCases
		{
			get
			{
				var merge = new FakeTable<Child>().Merge().Using(new Child[0]).OnTargetKey();

				return new BasicMergeBuilder<Child, Child>[]
				{
					// operation count limit
					new ValidationTestMergeBuilder(merge.DeleteWhenMatchedAnd((t, s) => true).UpdateWhenMatched().InsertWhenNotMatched()),
					new ValidationTestMergeBuilder(merge.DeleteWhenMatched().InsertWhenNotMatched()).WithLimit(2),

					// operation types support
					new ValidationTestMergeBuilder(merge.DeleteWhenMatched()),
					new ValidationTestMergeBuilder(merge.UpdateWhenMatched()),
					new ValidationTestMergeBuilder(merge.InsertWhenNotMatched()),
					new ValidationTestMergeBuilder(merge.DeleteWhenNotMatchedBySource()).WithBySource(),
					new ValidationTestMergeBuilder(merge.UpdateWhenNotMatchedBySource(_ => _)).WithBySource(),

					// operation conditions support
					new ValidationTestMergeBuilder(merge.InsertWhenNotMatchedAnd(_ => true)),
					new ValidationTestMergeBuilder(merge.UpdateWhenMatchedAnd((t, s) => true)),
					new ValidationTestMergeBuilder(merge.DeleteWhenMatchedAnd((t, s) => true)),
					new ValidationTestMergeBuilder(merge.DeleteWhenNotMatchedBySourceAnd(_ => true)).WithBySource(),
					new ValidationTestMergeBuilder(merge.UpdateWhenNotMatchedBySourceAnd(_ => true, _ => _)).WithBySource(),

					// more than one command of the same type allowed
					new ValidationTestMergeBuilder(merge.InsertWhenNotMatchedAnd(_ => true).InsertWhenNotMatched()),
					new ValidationTestMergeBuilder(merge.DeleteWhenMatchedAnd((t, s) => true).DeleteWhenMatched()),
					new ValidationTestMergeBuilder(merge.UpdateWhenMatchedAnd((t, s) => true).UpdateWhenMatched()),
					new ValidationTestMergeBuilder(merge.DeleteWhenNotMatchedBySourceAnd(_ => true).DeleteWhenNotMatchedBySource()).WithBySource(),
					new ValidationTestMergeBuilder(merge.UpdateWhenNotMatchedBySourceAnd(_ => true, _ => _).UpdateWhenNotMatchedBySource(_ => _)).WithBySource(),

					// unconditional operations from different match groups
					new ValidationTestMergeBuilder(merge.InsertWhenNotMatched().DeleteWhenMatched().DeleteWhenNotMatchedBySource()).WithBySource(),
					new ValidationTestMergeBuilder(merge.InsertWhenNotMatched().UpdateWhenMatched().UpdateWhenNotMatchedBySource(_ => _)).WithBySource(),

					// unconditional operations after conditional in the same match group
					new ValidationTestMergeBuilder(merge.UpdateWhenMatchedAnd((t, s) => true).DeleteWhenMatched()).WithBySource(),
					new ValidationTestMergeBuilder(merge.DeleteWhenMatchedAnd((t, s) => true).UpdateWhenMatched()).WithBySource(),
					new ValidationTestMergeBuilder(merge.DeleteWhenMatchedAnd((t, s) => true).DeleteWhenMatched()).WithBySource(),
					new ValidationTestMergeBuilder(merge.UpdateWhenMatchedAnd((t, s) => true).UpdateWhenMatched()).WithBySource(),
					new ValidationTestMergeBuilder(merge.InsertWhenNotMatchedAnd(_ => true).InsertWhenNotMatched()).WithBySource(),
					new ValidationTestMergeBuilder(merge.UpdateWhenNotMatchedBySourceAnd(_ => true, _ => _).UpdateWhenNotMatchedBySource(_ => _)).WithBySource(),
					new ValidationTestMergeBuilder(merge.DeleteWhenNotMatchedBySourceAnd(_ => true).UpdateWhenNotMatchedBySource(_ => _)).WithBySource(),
					new ValidationTestMergeBuilder(merge.UpdateWhenNotMatchedBySourceAnd(_ => true, _ => _).DeleteWhenNotMatchedBySource()).WithBySource(),
					new ValidationTestMergeBuilder(merge.DeleteWhenNotMatchedBySourceAnd(_ => true).DeleteWhenNotMatchedBySource()).WithBySource(),

					// UpdateWithDelete validation
					new ValidationTestMergeBuilder(merge.UpdateWhenMatchedAnd((t, s) => true).DeleteWhenMatched()),
					new ValidationTestMergeBuilder(merge.UpdateWhenMatchedThenDelete((t, s) => true)).WithUpdateWithDelete(),
				}.Select((data, i) => new TestCaseData(data).SetName($"Merge.Validation.Positive.{i}"));
			}
		}

		[TestCaseSource(nameof(_validationNegativeCases))]
		public void MergeOperationsValidationNegative(ValidationTestMergeBuilder validator, string error)
		{
			Assert.That(() => validator.Validate(), Throws.TypeOf<LinqToDBException>().With.Message.EqualTo(error));
		}

		[TestCaseSource(nameof(_validationPositiveCases))]
		public void MergeOperationsValidationPositive(ValidationTestMergeBuilder validator)
		{
			validator.Validate();
		}

		public class ValidationTestMergeBuilder : BasicMergeBuilder<Child, Child>
		{
			private class FakeDataProvider : IDataProvider
			{
				string IDataProvider.ConnectionNamespace
				{
					get
					{
						throw new NotImplementedException();
					}
				}

				Type IDataProvider.DataReaderType
				{
					get
					{
						throw new NotImplementedException();
					}
				}

				MappingSchema IDataProvider.MappingSchema
				{
					get
					{
						return null;
					}
				}

				string IDataProvider.Name
				{
					get
					{
						return "TestProvider";
					}
				}

				SqlProviderFlags IDataProvider.SqlProviderFlags
				{
					get
					{
						throw new NotImplementedException();
					}
				}

				BulkCopyRowsCopied IDataProvider.BulkCopy<T>(DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
				{
					throw new NotImplementedException();
				}

				Type IDataProvider.ConvertParameterType(Type type, DataType dataType)
				{
					throw new NotImplementedException();
				}

				IDbConnection IDataProvider.CreateConnection(string connectionString)
				{
					throw new NotImplementedException();
				}

				ISqlBuilder IDataProvider.CreateSqlBuilder()
				{
					throw new NotImplementedException();
				}

				void IDataProvider.DisposeCommand(DataConnection dataConnection)
				{
					throw new NotImplementedException();
				}

				CommandBehavior IDataProvider.GetCommandBehavior(CommandBehavior commandBehavior)
				{
					throw new NotImplementedException();
				}

				object IDataProvider.GetConnectionInfo(DataConnection dataConnection, string parameterName)
				{
					throw new NotImplementedException();
				}

				Expression IDataProvider.GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
				{
					throw new NotImplementedException();
				}

#if !NETSTANDARD
				ISchemaProvider IDataProvider.GetSchemaProvider()
				{
					throw new NotImplementedException();
				}
#endif

				ISqlOptimizer IDataProvider.GetSqlOptimizer()
				{
					throw new NotImplementedException();
				}

				void IDataProvider.InitCommand(DataConnection dataConnection, CommandType commandType, string commandText, DataParameter[] parameters)
				{
					throw new NotImplementedException();
				}

				bool IDataProvider.IsCompatibleConnection(IDbConnection connection)
				{
					throw new NotImplementedException();
				}

				bool? IDataProvider.IsDBNullAllowed(IDataReader reader, int idx)
				{
					throw new NotImplementedException();
				}

				int IDataProvider.Merge<T>(DataConnection dataConnection, Expression<Func<T, bool>> predicate, bool delete, IEnumerable<T> source, string tableName, string databaseName, string schemaName)
				{
					throw new NotImplementedException();
				}

				int IDataProvider.Merge<TTarget, TSource>(DataConnection dataConnection, IMergeable<TTarget, TSource> merge)
				{
					throw new NotImplementedException();
				}

				void IDataProvider.SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
				{
					throw new NotImplementedException();
				}
			}

			private bool _bySourceOperationsSupported = false;

			private bool _conditionsSupported = true;

			private bool _deleteOperationSupported = true;

			private bool _duplicateSameTypeOperations = true;

			private bool _updateWithDeleteOperationSupported = false;

			// initialize with SQL:2008 defaults (same as base class)
			private int _operationsLimit = 0;

			public ValidationTestMergeBuilder(IMergeable<Child, Child> merge)
				: base(new DataConnection(new FakeDataProvider(), string.Empty), merge)
			{
			}

			protected override bool BySourceOperationsSupported
			{
				get
				{
					return _bySourceOperationsSupported;
				}
			}

			protected override bool DeleteOperationSupported
			{
				get
				{
					return _deleteOperationSupported;
				}
			}

			protected override int MaxOperationsCount
			{
				get
				{
					return _operationsLimit;
				}
			}

			protected override bool OperationPredicateSupported
			{
				get
				{
					return _conditionsSupported;
				}
			}

			protected override bool SameTypeOperationsAllowed
			{
				get
				{
					return _duplicateSameTypeOperations;
				}
			}

			protected override bool UpdateWithDeleteOperationSupported
			{
				get
				{
					return _updateWithDeleteOperationSupported;
				}
			}

			public ValidationTestMergeBuilder WithUpdateWithDelete()
			{
				_updateWithDeleteOperationSupported = true;
				return this;
			}

			public ValidationTestMergeBuilder WithBySource()
			{
				_bySourceOperationsSupported = true;
				return this;
			}

			public ValidationTestMergeBuilder WithLimit(int limit)
			{
				_operationsLimit = limit;
				return this;
			}

			public ValidationTestMergeBuilder WithoutConditions()
			{
				_conditionsSupported = false;
				return this;
			}

			public ValidationTestMergeBuilder WithoutDelete()
			{
				_deleteOperationSupported = false;
				return this;
			}

			public ValidationTestMergeBuilder WithoutDuplicates()
			{
				_duplicateSameTypeOperations = false;
				return this;
			}
		}

		[DataContextSource(false, ProviderName.DB2, ProviderName.Firebird, TestProvName.Firebird3,
			ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative, ProviderName.Sybase,
			ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014,
			TestProvName.SqlAzure, ProviderName.Informix, ProviderName.SapHana,
			ProviderName.SqlServer2000, ProviderName.SqlServer2005)]
		public void NotSupportedProviders(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var table = GetTarget(db);

				Assert.Throws<LinqToDBException>(() => table.Merge().Using(GetSource1(db)).OnTargetKey().InsertWhenNotMatched().Merge());
			}
		}
	}
}
