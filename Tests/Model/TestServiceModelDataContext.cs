using System;
using System.ServiceModel;

using LinqToDB;
using LinqToDB.Data.Linq;
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

		public Table<Person>                 Person                 { get { return this.GetTable<Person>();                 } }
		public Table<Patient>                Patient                { get { return this.GetTable<Patient>();                } }
		public Table<Doctor>                 Doctor                 { get { return this.GetTable<Doctor>();                 } }
		public Table<Parent>                 Parent                 { get { return this.GetTable<Parent>();                 } }
		public Table<Parent1>                Parent1                { get { return this.GetTable<Parent1>();                } }
		public Table<IParent>                Parent2                { get { return this.GetTable<IParent>();                } }
		public Table<Parent4>                Parent4                { get { return this.GetTable<Parent4>();                } }
		public Table<Parent5>                Parent5                { get { return this.GetTable<Parent5>();                } }
		public Table<ParentInheritanceBase>  ParentInheritance      { get { return this.GetTable<ParentInheritanceBase>();  } }
		public Table<ParentInheritanceBase2> ParentInheritance2     { get { return this.GetTable<ParentInheritanceBase2>(); } }
		public Table<ParentInheritanceBase3> ParentInheritance3     { get { return this.GetTable<ParentInheritanceBase3>(); } }
		public Table<ParentInheritanceBase4> ParentInheritance4     { get { return this.GetTable<ParentInheritanceBase4>(); } }
		public Table<ParentInheritance1>     ParentInheritance1     { get { return this.GetTable<ParentInheritance1>();     } }
		public Table<ParentInheritanceValue> ParentInheritanceValue { get { return this.GetTable<ParentInheritanceValue>(); } }
		public Table<Child>                  Child                  { get { return this.GetTable<Child>();                  } }
		public Table<GrandChild>             GrandChild             { get { return this.GetTable<GrandChild>();             } }
		public Table<GrandChild1>            GrandChild1            { get { return this.GetTable<GrandChild1>();            } }
		public Table<LinqDataTypes>          Types                  { get { return this.GetTable<LinqDataTypes>();          } }
		public Table<LinqDataTypes2>         Types2                 { get { return this.GetTable<LinqDataTypes2>();         } }
	}
}
