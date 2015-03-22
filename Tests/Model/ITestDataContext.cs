using System;

using LinqToDB;

namespace Tests.Model
{
	public interface ITestDataContext : IDataContext
	{
		ITable<Person>                 Person                 { get; }
        ITable<ComplexPerson>          ComplexPerson          { get; }
		ITable<Patient>                Patient                { get; }
		ITable<Doctor>                 Doctor                 { get; }
		ITable<Parent>                 Parent                 { get; }
		ITable<Parent1>                Parent1                { get; }
		ITable<IParent>                Parent2                { get; }
		ITable<Parent4>                Parent4                { get; }
		ITable<Parent5>                Parent5                { get; }
		ITable<ParentInheritanceBase>  ParentInheritance      { get; }
		ITable<ParentInheritanceBase2> ParentInheritance2     { get; }
		ITable<ParentInheritanceBase3> ParentInheritance3     { get; }
		ITable<ParentInheritanceBase4> ParentInheritance4     { get; }
		ITable<ParentInheritance1>     ParentInheritance1     { get; }
		ITable<ParentInheritanceValue> ParentInheritanceValue { get; }
		ITable<Child>                  Child                  { get; }
		ITable<GrandChild>             GrandChild             { get; }
		ITable<GrandChild1>            GrandChild1            { get; }
		ITable<LinqDataTypes>          Types                  { get; }
		ITable<LinqDataTypes2>         Types2                 { get; }
		ITable<TestIdentity>           TestIdentity           { get; }

		[Sql.TableFunction(Name="GetParentByID")]
		ITable<Parent> GetParentByID(int? id);
	}
}
