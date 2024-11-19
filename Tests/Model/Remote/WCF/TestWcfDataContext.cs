#if NETFRAMEWORK
using System;
using System.ServiceModel;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Remote.Wcf;

namespace Tests.Model.Remote.Wcf
{
	public class TestWcfDataContext : WcfDataContext, ITestDataContext
	{
		private readonly Action? _onDispose;

		public TestWcfDataContext(int port, Action? onDispose = null, Func<DataOptions,DataOptions>? optionBuilder = null) : base(
			new NetTcpBinding(SecurityMode.None)
			{
				MaxReceivedMessageSize = 10000000,
				MaxBufferPoolSize      = 10000000,
				MaxBufferSize          = 10000000,
				CloseTimeout           = new TimeSpan(00, 01, 00),
				OpenTimeout            = new TimeSpan(00, 01, 00),
				ReceiveTimeout         = new TimeSpan(00, 10, 00),
				SendTimeout            = new TimeSpan(00, 10, 00),
			},
			new EndpointAddress("net.tcp://localhost:" + port + "/LinqOverWcf"),
			optionBuilder)
		{
			((NetTcpBinding)Binding!).ReaderQuotas.MaxStringContentLength = 1000000;
			_onDispose = onDispose;
		}

		public ITable<Person>                 Person                 => this.GetTable<Person>();
		public ITable<ComplexPerson>          ComplexPerson          => this.GetTable<ComplexPerson>();
		public ITable<Patient>                Patient                => this.GetTable<Patient>();
		public ITable<Doctor>                 Doctor                 => this.GetTable<Doctor>();
		public ITable<Parent>                 Parent                 => this.GetTable<Parent>();
		public ITable<Parent1>                Parent1                => this.GetTable<Parent1>();
		public ITable<IParent>                Parent2                => this.GetTable<IParent>();
		public ITable<Parent4>                Parent4                => this.GetTable<Parent4>();
		public ITable<Parent5>                Parent5                => this.GetTable<Parent5>();
		public ITable<ParentInheritanceBase>  ParentInheritance      => this.GetTable<ParentInheritanceBase>();
		public ITable<ParentInheritanceBase2> ParentInheritance2     => this.GetTable<ParentInheritanceBase2>();
		public ITable<ParentInheritanceBase3> ParentInheritance3     => this.GetTable<ParentInheritanceBase3>();
		public ITable<ParentInheritanceBase4> ParentInheritance4     => this.GetTable<ParentInheritanceBase4>();
		public ITable<ParentInheritance1>     ParentInheritance1     => this.GetTable<ParentInheritance1>();
		public ITable<ParentInheritanceValue> ParentInheritanceValue => this.GetTable<ParentInheritanceValue>();
		public ITable<Child>                  Child                  => this.GetTable<Child>();
		public ITable<GrandChild>             GrandChild             => this.GetTable<GrandChild>();
		public ITable<GrandChild1>            GrandChild1            => this.GetTable<GrandChild1>();
		public ITable<LinqDataTypes>          Types                  => this.GetTable<LinqDataTypes>();
		public ITable<LinqDataTypes2>         Types2                 => this.GetTable<LinqDataTypes2>();
		public ITable<TestIdentity>           TestIdentity           => this.GetTable<TestIdentity>();
		public ITable<InheritanceParentBase>  InheritanceParent      => this.GetTable<InheritanceParentBase>();
		public ITable<InheritanceChildBase>   InheritanceChild       => this.GetTable<InheritanceChildBase>();

		public ITable<Parent> GetParentByID(int? id)
		{
			throw new NotImplementedException();
		}

		public override void Dispose()
		{
			_onDispose?.Invoke();
			base.Dispose();
		}

		public override ValueTask DisposeAsync()
		{
			_onDispose?.Invoke();
			return base.DisposeAsync();
		}
	}
}
#endif
