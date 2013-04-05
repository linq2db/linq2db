using System;
using System.ServiceModel;

using LinqToDB;
using LinqToDB.ServiceModel;

namespace Tests.Model
{
	public class TestServiceModelDataContext : ServiceModelDataContext, ITestDataContext
	{
		public TestServiceModelDataContext(int ip) : base(
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
			new EndpointAddress("net.tcp://localhost:" + ip + "/LinqOverWCF"))
		{
			((NetTcpBinding)Binding).ReaderQuotas.MaxStringContentLength = 1000000;
		}

		public ITable<Person>                 Person                 { get { return this.GetTable<Person>();                 } }
		public ITable<Patient>                Patient                { get { return this.GetTable<Patient>();                } }
		public ITable<Doctor>                 Doctor                 { get { return this.GetTable<Doctor>();                 } }
		public ITable<Parent>                 Parent                 { get { return this.GetTable<Parent>();                 } }
		public ITable<Parent1>                Parent1                { get { return this.GetTable<Parent1>();                } }
		public ITable<IParent>                Parent2                { get { return this.GetTable<IParent>();                } }
		public ITable<Parent4>                Parent4                { get { return this.GetTable<Parent4>();                } }
		public ITable<Parent5>                Parent5                { get { return this.GetTable<Parent5>();                } }
		public ITable<ParentInheritanceBase>  ParentInheritance      { get { return this.GetTable<ParentInheritanceBase>();  } }
		public ITable<ParentInheritanceBase2> ParentInheritance2     { get { return this.GetTable<ParentInheritanceBase2>(); } }
		public ITable<ParentInheritanceBase3> ParentInheritance3     { get { return this.GetTable<ParentInheritanceBase3>(); } }
		public ITable<ParentInheritanceBase4> ParentInheritance4     { get { return this.GetTable<ParentInheritanceBase4>(); } }
		public ITable<ParentInheritance1>     ParentInheritance1     { get { return this.GetTable<ParentInheritance1>();     } }
		public ITable<ParentInheritanceValue> ParentInheritanceValue { get { return this.GetTable<ParentInheritanceValue>(); } }
		public ITable<Child>                  Child                  { get { return this.GetTable<Child>();                  } }
		public ITable<GrandChild>             GrandChild             { get { return this.GetTable<GrandChild>();             } }
		public ITable<GrandChild1>            GrandChild1            { get { return this.GetTable<GrandChild1>();            } }
		public ITable<LinqDataTypes>          Types                  { get { return this.GetTable<LinqDataTypes>();          } }
		public ITable<LinqDataTypes2>         Types2                 { get { return this.GetTable<LinqDataTypes2>();         } }
		public ITable<TestIdentity>           TestIdentity           { get { return this.GetTable<TestIdentity>();           } }

		public ITable<Parent> GetParentByID(int? id)
		{
			throw new NotImplementedException();
		}
	}
}
