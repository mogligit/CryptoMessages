Imports PacketType

Public Class frmRegister
    Private Sub btnCancel_Click() Handles btnCancel.Click
        Me.Close()
    End Sub

    Private Async Sub btnConfirm_Click() Handles btnConfirm.Click
        If CheckFields() Then

            Dim Username As String = txtUsername.Text
            Dim FirstName As String = txtFirstName.Text
            Dim Surname As String = txtSurname.Text
            Dim Password As String = txtPassword.Text

            Dim registerResult As Result
            Try
                registerResult = Await PacketManager.Register(Username, FirstName, Surname, Password)
                If registerResult.OK Then
                    MsgBox("Register successful. Please log in now.")
                    Me.Close()
                Else
                    MsgBox(registerResult.Message)
                End If
            Catch ex As Exception
                MsgBox(ex.ToString & " " & ex.Message & vbCrLf & ex.StackTrace)
            End Try
        End If
    End Sub


    Private Function CheckFields() As Boolean
        If CheckUsername() And CheckNames() And CheckPassword() Then
            Return True
        Else
            Return False
        End If
    End Function
    Private Function CheckUsername() As Boolean
        If txtUsername.Text.Length > 0 Then
            If txtUsername.Text.Length <= 15 Then
                Return True
            Else
                MsgBox("Username must not be over 15 characters.")
            End If
        Else
            MsgBox("Username must not be blank.")
        End If
        Return False
    End Function
    Private Function CheckNames() As Boolean
        If txtFirstName.Text.Length > 0 And txtSurname.Text.Length > 0 Then
            If txtFirstName.Text.Length <= 20 And txtSurname.Text.Length <= 20 Then
                Return True
            Else
                MsgBox("First name or surname must not be over 20 characters." & vbCrLf & "Please use an abreviation.")
            End If
        Else
            MsgBox("First name or surname must not be blank.")
        End If
        Return False
    End Function
    Private Function CheckPassword() As Boolean
        If txtPassword.Text = txtPasswordRepeat.Text Then
            If txtPassword.Text.Length >= 4 Then
                If Not txtPassword.Text.Contains(" ") Then
                    Return True
                Else
                    MsgBox("The password cannot contain any spaces.")
                End If
            Else
                MsgBox("Minimum length for the password is 4 characters.")
            End If
        Else
            MsgBox("Passwords do not match.")
        End If
        Return False
    End Function
End Class