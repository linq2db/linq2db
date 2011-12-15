using System;
using System.Reflection;
using System.Text;

using LinqToDB.Data;
using LinqToDB.Data.Linq;
using LinqToDB.Data.Sql;

namespace Tests.Model
{
	public class TestDbManager : DbManager, ITestDataContext
	{
		public TestDbManager(string configString)
			: base(configString)
		{
		}

		public TestDbManager()
			: base("Sql2008")
		{
		}

		public Table<Person>                 Person                 { get { return GetTable<Person>();                 } }
		public Table<Patient>                Patient                { get { return GetTable<Patient>();                } }
		public Table<Doctor>                 Doctor                 { get { return GetTable<Doctor>();                 } }
		public Table<Parent>                 Parent                 { get { return GetTable<Parent>();                 } }
		public Table<Parent1>                Parent1                { get { return GetTable<Parent1>();                } }
		public Table<IParent>                Parent2                { get { return GetTable<IParent>();                } }
		public Table<Parent4>                Parent4                { get { return GetTable<Parent4>();                } }
		public Table<Parent5>                Parent5                { get { return GetTable<Parent5>();                } }
		public Table<ParentInheritanceBase>  ParentInheritance      { get { return GetTable<ParentInheritanceBase>();  } }
		public Table<ParentInheritanceBase2> ParentInheritance2     { get { return GetTable<ParentInheritanceBase2>(); } }
		public Table<ParentInheritanceBase3> ParentInheritance3     { get { return GetTable<ParentInheritanceBase3>(); } }
		public Table<ParentInheritanceBase4> ParentInheritance4     { get { return GetTable<ParentInheritanceBase4>(); } }
		public Table<ParentInheritance1>     ParentInheritance1     { get { return GetTable<ParentInheritance1>();     } }
		public Table<ParentInheritanceValue> ParentInheritanceValue { get { return GetTable<ParentInheritanceValue>(); } }
		public Table<Child>                  Child                  { get { return GetTable<Child>();                  } }
		public Table<GrandChild>             GrandChild             { get { return GetTable<GrandChild>();             } }
		public Table<GrandChild1>            GrandChild1            { get { return GetTable<GrandChild1>();            } }
		public Table<LinqDataTypes>          Types                  { get { return GetTable<LinqDataTypes>();          } }
		public Table<LinqDataTypes2>         Types2                 { get { return GetTable<LinqDataTypes2>();         } }

		[TableFunction(Name="GetParentByID")]
		public Table<Parent> GetParentByID(int? id)
		{
			return GetTable<Parent>(this, (MethodInfo)MethodBase.GetCurrentMethod(), id);
		}

		public string GetSqlText(SqlQuery sql)
		{
			var provider = ((IDataContext)this).CreateSqlProvider();

			//provider.SqlQuery = sql;

			sql = provider.Finalize(sql);

			var cc = provider.CommandCount(sql);
			var sb = new StringBuilder();

			var commands = new string[cc];

			for (var i = 0; i < cc; i++)
			{
				sb.Length = 0;

				provider.BuildSql(i, sql, sb, 0, 0, false);
				commands[i] = sb.ToString();
			}

			return string.Join("\n\n", commands);
		}
	}
}
