Imports LinqToDB
Imports LinqToDB.Mapping

Imports Tests.Model

Public Module VBTests

	<Table("vb_activity")>
	Public Class GroupByActivity
		<Column, Identity, PrimaryKey> Property activityid As Integer
		<Column, NotNull> Property personid As Integer
		<Column, NotNull> Property added As Date

		<Association(ThisKey:=NameOf(personid), OtherKey:=NameOf(GroupByActivityPerson.personid), CanBeNull:=False)> Public Property Person As GroupByActivityPerson
	End Class

	<Table("vb_activity_person")>
	Public Class GroupByActivityPerson
		<Column, Identity, PrimaryKey> Property personid As Integer
		<Column, NotNull> Property personname As String
		<Association(ThisKey:=NameOf(personid), OtherKey:=NameOf(GroupByActivity.personid), CanBeNull:=False)> Public Property Activties As List(Of GroupByActivity)
	End Class

	Public Function GroupByAnonymousKeyWithGroupMax(ByVal db As IDataContext) As IEnumerable(Of Object)
		Return (From p In db.GetTable(Of GroupByActivity)
				Where p.added >= New Date(2017, 1, 1)
				Group By pp = New With {Key p.Person.personid, Key p.Person.personname} Into ppp = Group
				Select New With
			 {
			  .PersonId = pp.personid,
			  .PersonName = pp.personname,
			  .LastAdded = ppp.Max(Function(f) f.added)
			 }).ToList

	End Function

	Public Function GroupByAnonymousKeyWithIntoMax(ByVal db As IDataContext) As IEnumerable(Of Object)
		Return (From p In db.GetTable(Of GroupByActivity)
				Where p.added >= New Date(2017, 1, 1)
				Group By pp = New With {Key p.Person.personid, Key p.Person.personname} Into LastAdded = Max(p.added)
				Select New With
			 {
			  .PersonId = pp.personid,
			  .PersonName = pp.personname,
			  .LastAdded = LastAdded
			 }).ToList

	End Function

	Public Function GroupByAnonymousKeyMethodSyntax(ByVal db As IDataContext) As IEnumerable(Of Object)
		Return db.GetTable(Of GroupByActivity).
				Where(Function(f) f.added >= New Date(2017, 1, 1)).
				GroupBy(Function(f) New With
				{
					Key .personid = f.Person.personid,
					Key .personname = f.Person.personname
				}).Select(Function(f) New With
				{
					.personid = f.Key.personid,
					.personname = f.Key.personname,
					.LastAdded = f.Max(Function(g) g.added)
				}).ToList

	End Function

	Public Function StringToNullableIntCoercion(ByVal db As IDataContext, SelectedValue As String) As IEnumerable(Of GrandChild1)
		Return db.GetTable(Of GrandChild1).
			Where(Function(w) w.ChildID.HasValue AndAlso w.ChildID.Value = CType(SelectedValue, Integer)).
			ToList

	End Function

End Module
