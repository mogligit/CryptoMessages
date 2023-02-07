Imports System.Security.Cryptography
Imports SerializationService.Serialization

Public Class HashService
    Public Overloads Shared Function ComputeHash(ByVal data As Byte()) As Byte()
        Return SHA256.Create.ComputeHash(data)
    End Function
    Public Overloads Shared Function ComputeHash(ParamArray data As Object()) As Byte()
        Dim plainArray As Byte() = ToByte(data)
        Return SHA256.Create.ComputeHash(plainArray)
    End Function
End Class
