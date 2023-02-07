Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary

Public Class Serialization
    Public Shared Function ToByte(ByVal obj As Object) As Byte()
        If IsNothing(obj) Then
            Dim emptyArray(0) As Byte
            Return emptyArray
        Else
            Dim binForm As New BinaryFormatter
            Using memStr As New MemoryStream
                binForm.Serialize(memStr, obj)
                Return memStr.ToArray
            End Using
        End If
    End Function
    Public Shared Function ToObj(ByVal bArray() As Byte) As Object
        If IsNothing(bArray) Then
            Dim emptyArray(0) As Byte
            Return emptyArray
        Else
            Dim obj As Object
            Dim binForm As New BinaryFormatter
            Using memStr As New MemoryStream
                memStr.Write(bArray, 0, bArray.Length)
                memStr.Seek(0, SeekOrigin.Begin)
                obj = binForm.Deserialize(memStr)
                Return obj
            End Using
        End If
    End Function
End Class
