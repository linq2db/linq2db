using System;

using LinqToDB;

namespace Tests.Model
{
	public interface ITestDataContext : IDataContext
	{
		Table<Person>                 Person                 { get; }
		Table<Patient>                Patient                { get; }
		Table<Doctor>                 Doctor                 { get; }
		Table<Parent>                 Parent                 { get; }
		Table<Parent1>                Parent1                { get; }
		Table<IParent>                Parent2                { get; }
		Table<Parent4>                Parent4                { get; }
		Table<Parent5>                Parent5                { get; }
		Table<ParentInheritanceBase>  ParentInheritance      { get; }
		Table<ParentInheritanceBase2> ParentInheritance2     { get; }
		Table<ParentInheritanceBase3> ParentInheritance3     { get; }
		Table<ParentInheritanceBase4> ParentInheritance4     { get; }
		Table<ParentInheritance1>     ParentInheritance1     { get; }
		Table<ParentInheritanceValue> ParentInheritanceValue { get; }
		Table<Child>                  Child                  { get; }
		Table<GrandChild>             GrandChild             { get; }
		Table<GrandChild1>            GrandChild1            { get; }
		Table<LinqDataTypes>          Types                  { get; }
		Table<LinqDataTypes2>         Types2                 { get; }
		Table<TestIdentity>           TestIdentity           { get; }
	}
}
