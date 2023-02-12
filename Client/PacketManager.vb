Imports System.Net
Imports System.Threading
Imports Elgamal
Imports NetworkInterface
Imports PacketType
Imports SerializationService.Serialization

Module PacketManager
    '**********SETTINGS**********
    Const ENCRYPTION_SECURITY_LEVEL As Integer = 64
    '****************************

    Private Server As ServerConnection
    Private WithEvents NetInt As NetworkConnection

    Private WithEvents Elgamal As ElgamalService
    Private LocalPublicKey As ElgamalPublicKey
    Public KeyGenerationThread As New Thread(AddressOf GenerateKeys)    'needs to be public because its activated by ApplicationEvents.vb

    Private ResultCompletionSource As New TaskCompletionSource(Of Result)

    Public ReadOnly Property ServerIP() As String
        Get
            Return Server.ServerIP
        End Get
    End Property

    Public ReadOnly Property PublicKeyAvailable As Boolean
        Get
            Return Not KeyGenerationThread.IsAlive
        End Get
    End Property

    'Public Sub InitialiseNetInt()
    '    NetInt = New NetworkConnection(IPAddress.Parse(Server.ServerIP), PORT)
    '    NetInt.BeginListen()
    'End Sub
    'Public Sub CloseNetInt()
    '    NetInt.EndListen()
    '    NetInt = Nothing
    'End Sub

    '
    'PROCEDURES MANAGING RECEIVED DATA
    '
    Private Sub ReceivePacket(ByVal data As Packet, ByVal origin As String) Handles NetInt.OnPacketReceived
        Select Case data.GetType
            Case GetType(Message)
                ManageMessage(DirectCast(data, Message))
            Case GetType(UserStatusNotification)
                ManageUserStatusNotification(DirectCast(data, UserStatusNotification))
            Case GetType(FriendRequest)
                ManageFriendRequest(DirectCast(data, FriendRequest))
            Case GetType(Result)
                ResultCompletionSource.SetResult(DirectCast(data, Result))
                ResultCompletionSource = New TaskCompletionSource(Of Result)
            Case GetType(FriendResponse)
                Dim response As FriendResponse = DirectCast(data, FriendResponse)
                If response.Accepted Then
                    frmMain.DisplayFriendAccepted(response)
                End If
        End Select
    End Sub
    Private Sub ManageMessage(ByVal data As Message)
        Dim decryptedMessage As String = ToObj(Elgamal.Decrypt(data.Message))
        frmMain.DisplayIncomingMessage(data.DateAndTime, data.Sender, data.Recipient, decryptedMessage)
    End Sub
    Private Sub ManageUserStatusNotification(ByVal notification As UserStatusNotification)
        frmMain.UpdateUserStatus(notification.User, notification.IsConnected, notification.PublicKey)
    End Sub
    Private Sub ManageFriendRequest(ByVal request As FriendRequest)
        frmMain.DisplayFriendRequest(request)
    End Sub
    'FriendResponses are managed in frmMain

    '
    'PROCEDURES SENDING DATA
    '
    Public Async Function TrySendMessage(ByVal Now As Date, ByVal Sender As String, ByVal Recipient As String, ByVal Message As String, ByVal RecipientPublicKey As ElgamalPublicKey) As Task(Of Result)
        Try
            Dim byteMessage As Byte() = ToByte(Message)
            Dim encryptedMessage As ElgamalCiphertext = ElgamalService.Encrypt(byteMessage, RecipientPublicKey)
            Dim messageToSend As New Message(Now, Sender, Recipient, encryptedMessage)

            Await Send(messageToSend)
            Return New Result(True)
        Catch ex As Exception
            Return New Result(False, ex.Message)
        End Try
    End Function
    Public Async Function TrySendFriendRequest(ByVal MeUser As User, ByVal Recipient As String) As Task(Of Result)
        MeUser.PublicKey = LocalPublicKey
        Dim friendRequest As New FriendRequest(MeUser, Recipient)

        Try
            Dim sendTask As Task = Send(friendRequest)
            Dim result As Result = Await ResultCompletionSource.Task  'waiting for Result which is received from ResultTask
            Return result
        Catch ex As Exception
            Return New Result(False, ex.Message)
        End Try
    End Function
    Public Async Function TrySendFriendResponse(ByVal MeUser As User, ByVal Recipient As String, ByVal IsAccepted As Boolean) As Task(Of Result)
        MeUser.PublicKey = LocalPublicKey
        Dim friendResponse As New FriendResponse(IsAccepted, MeUser, Recipient)
        Try
            Await Send(friendResponse)
            Return New Result(True)
        Catch ex As Exception
            Return New Result(False, ex.Message)
        End Try
    End Function

    'general send function
    Private Async Function Send(ByVal data As Packet, Optional Timeout As Integer = 5000) As Task
        Dim CT As New CancellationTokenSource

        'if timeout is 0, wait indifinitely
        If Not Timeout = 0 Then
            CT.CancelAfter(Timeout)    'set timeout for cancellationtoken
        End If

        Await NetInt.SendAsync(data, CT.Token)

        If CT.IsCancellationRequested Then
            Throw New ServerTimeoutException
        End If
    End Function

    Class ServerErrorException
        Inherits Exception
        Sub New(ByVal message As String)
            MyBase.New(message)
        End Sub
    End Class


    '
    'SERVER FUNCTIONS FROM HERE
    '
    Public Function TryConnect(ByVal IP As String) As Result
        Try
            Server = New ServerConnection(IP)
            Return New Result(True, "Connected successfully.")
        Catch ex As Exception
            Return New Result(False, ex.Message)
        End Try
    End Function
    Public Async Function LogOffProtocol(ByVal data As UserData) As Task
        'ignore any exceptions when closing

        Dim mResult As MsgBoxResult = MsgBox(String.Format("Would you like to backup all the messages you sent?{0}Please note that messages are stored{1}unencrypted in the database.", vbCrLf, vbCrLf), MsgBoxStyle.YesNo)
        Dim backupMessages As Boolean = (mResult = MsgBoxResult.Yes)

        Await UploadUserData(data, backupMessages)
        Await ChangeOnlineStatus(False, data.User)


        Server.Disconnect() 'close socket connection to server
    End Function
    Public Async Function Login(ByVal Username As String, ByVal Password As String) As Task(Of Result)
        Dim loginRequest As New LoginAttempt(Username, Password)
        Return Await Server.AttemptLogin(loginRequest)
    End Function
    Public Async Function Register(ByVal Username As String, ByVal FirstName As String, ByVal Surname As String, ByVal Password As String) As Task(Of Result)
        Dim newUser As New User(Username, FirstName, Surname)
        Dim registerRequest As New RegisterAttempt(newUser, Password)

        Return Await Server.AttemptRegister(registerRequest)
    End Function
    Public Async Function FetchUserData(ByVal Username As String) As Task(Of UserData)
        Await WaitForKeys()

        Dim response As ElgamalCiphertext = Await Server.FetchUserData(Username, LocalPublicKey)
        Dim data As Byte() = Elgamal.Decrypt(response)
        Dim userData As UserData = ToObj(data)
        Return userData
    End Function
    Public Async Function ChangeOnlineStatus(ByVal NewStatus As Boolean, ByVal User As User) As Task(Of Result)
        Dim status As New UserStatusNotification(NewStatus, User.Username, LocalPublicKey)
        Return Await Server.ChangeOnlineStatus(status)
    End Function
    Public Async Function UploadUserData(ByVal data As UserData, ByVal BackupMessages As Boolean) As Task(Of Result)
        If Not BackupMessages Then
            data.Messages.Clear()
        End If

        Return Await Server.UploadUserData(data)
    End Function

    Public Async Function WaitForKeys() As Task
        If KeyGenerationThread.IsAlive Then
            Await Task.Run(Sub() KeyGenerationThread.Join())
        End If
    End Function
    'this is called by a separate thread at startup (see ApplicationEvents.vb)
    Public Sub GenerateKeys()
        Elgamal = New ElgamalService(ENCRYPTION_SECURITY_LEVEL)
        LocalPublicKey = Elgamal.GenerateKeys()
    End Sub
End Module
