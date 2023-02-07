Imports PacketType

Public Class frmLogin
    Private Async Sub btnLogin_Click() Handles btnLogin.Click
        Loading = True
        Dim Username As String = txtUsernameInput.Text
        Dim Password As String = txtPasswordInput.Text

        Dim loginTask As Task(Of Result)

        Try
            loginTask = PacketManager.Login(Username, Password)
            Status("Attempting login...")
            Await loginTask

            If loginTask.Result.OK Then
                Await PacketManager.WaitForKeys()
                frmMain.Show()
                frmMain.Initialise(Username)
                Me.Close()
            Else
                MsgBox(loginTask.Result.Message)
            End If
        Catch ex As Exception
            MsgBox(ex.Source.ToString & ex.Message & ex.StackTrace)
        End Try
        frmLogin_Load()
    End Sub

    Private Sub frmLogin_Load() Handles MyBase.Load
        Status("Connected to: " & PacketManager.ServerIP)
        Loading = False
    End Sub

    Public Sub Status(ByVal NewStatus As String)
        lblStatus.Text = NewStatus
    End Sub

    Private WriteOnly Property Loading As Boolean
        Set(ByVal IsLoading As Boolean)
            picLoading.Visible = IsLoading
            btnLogin.Enabled = Not IsLoading
            btnRegister.Enabled = Not IsLoading
        End Set
    End Property

    Private Sub btnRegister_Click(sender As Object, e As EventArgs) Handles btnRegister.Click
        frmRegister.Show()
    End Sub
End Class