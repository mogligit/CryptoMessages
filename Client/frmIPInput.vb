Imports PacketType

Public Class frmIPInput
    Private Sub btnConnect_Click() Handles btnConnect.Click
        Dim ip As New Net.IPAddress(0)
        If Net.IPAddress.TryParse(txtServerIP.Text, ip) Then
            Loading = True

            Dim connectionResult As Result
            connectionResult = PacketManager.TryConnect(ip.MapToIPv4.ToString)

            If connectionResult.OK Then
                frmLogin.Show()
                Me.Close()
            Else
                Loading = False
                MsgBox(connectionResult.Message)
            End If
        Else
            MsgBox("IP is not valid. Please try again.")
        End If
    End Sub

    Private WriteOnly Property Loading As Boolean
        Set(ByVal IsLoading As Boolean)
            picLoading.Visible = IsLoading
            btnConnect.Enabled = Not IsLoading
        End Set
    End Property

    Private Sub frmInputServerIP_Load() Handles MyBase.Load
        Loading = False
        Me.ActiveControl = txtServerIP
    End Sub
End Class