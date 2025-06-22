using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Mapping;

namespace Tests.Model
{
	public class TestDataCustomConnection : ITestDataContext
	{
		protected TestDataConnection Connection { get; }

		public TestDataCustomConnection(DataOptions options)
		{
			Connection = new TestDataConnection(options);
		}

		public TestDataCustomConnection(Func<DataOptions, DataOptions> optionsSetter)
		{
			Connection = new TestDataConnection(optionsSetter);
		}

		public TestDataCustomConnection(string configString)
		{
			Connection = new TestDataConnection(configString);
		}

		public TestDataCustomConnection()
		{
			Connection = new TestDataConnection();
		}

		public ITable<Person> Person => Connection.Person;

		public ITable<ComplexPerson> ComplexPerson => Connection.ComplexPerson;

		public ITable<Patient> Patient => Connection.Patient;

		public ITable<Doctor> Doctor => Connection.Doctor;

		public ITable<Parent> Parent => Connection.Parent;

		public ITable<Parent1> Parent1 => Connection.Parent1;

		public ITable<IParent> Parent2 => Connection.Parent2;

		public ITable<Parent4> Parent4 => Connection.Parent4;

		public ITable<Parent5> Parent5 => Connection.Parent5;

		public ITable<ParentInheritanceBase> ParentInheritance => Connection.ParentInheritance;

		public ITable<ParentInheritanceBase2> ParentInheritance2 => Connection.ParentInheritance2;

		public ITable<ParentInheritanceBase3> ParentInheritance3 => Connection.ParentInheritance3;

		public ITable<ParentInheritanceBase4> ParentInheritance4 => Connection.ParentInheritance4;

		public ITable<ParentInheritance1> ParentInheritance1 => Connection.ParentInheritance1;

		public ITable<ParentInheritanceValue> ParentInheritanceValue => Connection.ParentInheritanceValue;

		public ITable<Child> Child => Connection.Child;

		public ITable<GrandChild> GrandChild => Connection.GrandChild;

		public ITable<GrandChild1> GrandChild1 => Connection.GrandChild1;

		public ITable<LinqDataTypes> Types => Connection.Types;

		public ITable<LinqDataTypes2> Types2 => Connection.Types2;

		public ITable<TestIdentity> TestIdentity => Connection.TestIdentity;

		public ITable<InheritanceParentBase> InheritanceParent => Connection.InheritanceParent;

		public ITable<InheritanceChildBase> InheritanceChild => Connection.InheritanceChild;

		public string ContextName => ((IDataContext)Connection).ContextName;

		public Func<ISqlBuilder> CreateSqlBuilder => ((IDataContext)Connection).CreateSqlBuilder;

		public Func<DataOptions, ISqlOptimizer> GetSqlOptimizer => ((IDataContext)Connection).GetSqlOptimizer;

		public SqlProviderFlags SqlProviderFlags => ((IDataContext)Connection).SqlProviderFlags;

		public TableOptions SupportedTableOptions => ((IDataContext)Connection).SupportedTableOptions;

		public Type DataReaderType => ((IDataContext)Connection).DataReaderType;

		public MappingSchema MappingSchema => ((IDataContext)Connection).MappingSchema;

		public bool InlineParameters { get => ((IDataContext)Connection).InlineParameters; set => ((IDataContext)Connection).InlineParameters = value; }

		public List<string> QueryHints => ((IDataContext)Connection).QueryHints;

		public List<string> NextQueryHints => ((IDataContext)Connection).NextQueryHints;

		public bool CloseAfterUse { get => ((IDataContext)Connection).CloseAfterUse; set => ((IDataContext)Connection).CloseAfterUse = value; }

		public DataOptions Options => ((IDataContext)Connection).Options;

		public string? ConfigurationString => ((IDataContext)Connection).ConfigurationString;

		public void AddMappingSchema(MappingSchema mappingSchema) {}

		public int ConfigurationID => ((IConfigurationID)Connection).ConfigurationID;

		public void AddInterceptor(IInterceptor interceptor)
		{
			((IDataContext)Connection).AddInterceptor(interceptor);
		}

		public void Close()
		{
			((IDataContext)Connection).Close();
		}

		public Task CloseAsync()
		{
			return ((IDataContext)Connection).CloseAsync();
		}

		public void Dispose()
		{
			Connection.Dispose();
		}

		public ValueTask DisposeAsync()
		{
			return ((IAsyncDisposable)Connection).DisposeAsync();
		}

		public ITable<Parent> GetParentByID(int? id)
		{
			return Connection.GetParentByID(id);
		}

		public Expression GetReaderExpression(DbDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			return ((IDataContext)Connection).GetReaderExpression(reader, idx, readerExpression, toType);
		}

		public bool? IsDBNullAllowed(DbDataReader reader, int idx)
		{
			return ((IDataContext)Connection).IsDBNullAllowed(reader, idx);
		}

		public void RemoveInterceptor(IInterceptor interceptor)
		{
			((IDataContext)Connection).RemoveInterceptor(interceptor);
		}

		IQueryRunner IDataContext.GetQueryRunner(Query query, IDataContext parametersContext, int queryNumber, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles)
		{
			return ((IDataContext)Connection).GetQueryRunner(query, parametersContext, queryNumber, expressions, parameters, preambles);
		}
	}
}
