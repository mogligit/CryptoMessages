Public Class frmLoading
    Public Overloads Sub Show(ByVal Message As String)
        Me.Show()
        txtMessage.Text = Message
    End Sub
End Class