Imports System.Collections.Generic
Imports System.Linq

Imports Tests.Model

Public Module CompilerServices

    Public Function CompareString(ByVal db As ITestDataContext) As IEnumerable(Of Person)
        Return From p In db.Person Where p.FirstName = "John" Select p
    End Function

End Module
