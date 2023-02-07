Imports System.Net
Imports NetworkInterface
Imports PacketType
Imports System.Numerics
Imports Elgamal
Imports SHAService
Imports System.Threading
Imports Serializationservice.serialization

Module PacketManager
    'SETTINGS
    Const defPORT As Integer = 13000
    'dictionary to map IP addresses to local cryptographic keys
    Private IPKeyMap As New Dictionary(Of String, ElgamalKeyPair)

    '
    'DICTIONARIES
    '
    'dictionary to map logged-in users to their respective IP address
    Private UserIPAddressMap As New Dictionary(Of String, String)
    'dictionary to map logged-in users to their public key
    Private UserPublicKeyMap As New Dictionary(Of String, ElgamalPublicKey)
    'list to index all users that are online
    Private OnlineUsers As New List(Of String)

    Private LocalPublicKey As ElgamalPublicKey

    Private WithEvents NetListener As NetworkConnection

    Public Sub STARTUP(Optional SecurityLevel As Integer = 64)
        frmMain.ConsoleOutput("Generating {0}-bit cryptographic keys...", SecurityLevel)
        Dim Elgamal As New ElgamalService(SecurityLevel)
        LocalPublicKey = Elgamal.GenerateKeys()
        frmMain.ConsoleOutput("Done.")

        DatabaseInterface.StartConnection()
        StartListener()
        frmMain.ConsoleOutput("Ready.")
    End Sub

    Public Sub SHUTDOWN(ByVal CloseWindow As Boolean)
        If (Not IsNothing(NetListener)) AndAlso NetListener.IsListening Then
            StopListener()
        End If
        If DatabaseInterface.IsConnected Then
            DatabaseInterface.StopConnection()
        End If
        If CloseWindow Then
            frmMain.Close()
        End If
    End Sub

    Private Sub StartListener(Optional PORT As Integer = defPORT)
        If (Not IsNothing(NetListener)) AndAlso NetListener.IsListening Then
            frmMain.ConsoleOutput("Listener is already running.")
        Else
            NetListener = New NetworkConnection(defPORT)
            NetListener.BeginListen()
            frmMain.ConsoleOutput("Listener is now listening at port {0}.", defPORT)
        End If
    End Sub
    Private Sub StopListener()
        If (Not IsNothing(NetListener)) AndAlso NetListener.IsListening Then
            NetListener.EndListen()
            frmMain.ConsoleOutput("Listener stopped successfully.")
        Else
            frmMain.ConsoleOutput("Listener is already stopped.")
        End If
    End Sub

    Public Sub HandleCommand(ByVal args As String())
        Try
            Select Case args(0) 'first word
                Case "auto"
                    If args.Length = 1 Then
                        STARTUP()
                    Else
                        STARTUP(args(Array.IndexOf(args, "-s") + 1))
                    End If
                Case "clear"
                    frmMain.lstLog.Items.Clear()
                    frmMain.ConsoleOutput("Log cleared.")
                Case "db"
                    Select Case args(1)
                        Case "connect"
                            If args.Length = 2 Then
                                DatabaseInterface.StartConnection()
                            Else
                                DatabaseInterface.StartConnection(args(Array.IndexOf(args, "-ip") + 1), args(Array.IndexOf(args, "-u") + 1), args(Array.IndexOf(args, "-p") + 1))
                            End If
                        Case "disconnect"
                            DatabaseInterface.StopConnection()
                    End Select
                Case "listener"
                    Select Case args(1)
                        Case "start"
                            If args.Length = 2 Then
                                StartListener()
                            Else
                                StartListener(args(Array.IndexOf(args, "-p") + 1))
                            End If
                        Case "stop"
                            StopListener()
                    End Select
                Case "help"
                    frmMain.DisplayHelp()
                Case "stop"
                    If args.Length = 1 Then
                        SHUTDOWN(True)
                    End If
                Case Else
                    frmMain.ConsoleOutput("Command not recognised. Please try again.")
            End Select
        Catch ex As Exception
            frmMain.ConsoleOutput("Command not valid. Please try again.")
        End Try

    End Sub

    Private Sub PacketReceived(ByVal data As Packet, ByVal origin As String) Handles NetListener.OnPacketReceived
        frmMain.ConsoleOutput("{0} RECEIVED FROM {1}", data.GetType.Name.ToUpper, origin)

        If data.IsForServer AndAlso data.GetType = GetType(ElgamalCiphertext) Then
            data = Decrypt(DirectCast(data, ElgamalCiphertext), origin)
        End If

        Select Case data.GetType
            Case GetType(Ping)
                ManagePing(DirectCast(data, Ping), origin)
            Case GetType(LoginAttempt)
                ManageLoginAttempt(DirectCast(data, LoginAttempt), origin)
            Case GetType(RegisterAttempt)
                ManageRegisterAttempt(DirectCast(data, RegisterAttempt), origin)
            Case GetType(PublicKeyRequest)
                ManagePublicKeyRequest(DirectCast(data, PublicKeyRequest), origin)
            Case GetType(UserDataRequest)
                ManageUserDataRequest(DirectCast(data, UserDataRequest), origin)
            Case GetType(Message)
                ManageMessage(DirectCast(data, Message), origin)
            Case GetType(FriendRequest)
                ManageFriendRequest(DirectCast(data, FriendRequest), origin)
            Case GetType(FriendResponse)
                ManageFriendResponse(DirectCast(data, FriendResponse), origin)
            Case GetType(UserStatusNotification)
                ManageUserStatusNotification(DirectCast(data, UserStatusNotification), origin)
            Case GetType(UserData)
                ManageUserDataUpload(DirectCast(data, UserData), origin)
        End Select
    End Sub

    Private Sub ManagePing(ByVal data As Ping, ByVal origin As String)
        Send(data, origin)
    End Sub
    Private Sub ManageLoginAttempt(ByVal attempt As LoginAttempt, ByVal origin As String)
        frmMain.ConsoleOutput("     Attempt to log in with credentials: [{0}, {1}].", attempt.User, attempt.Password)

        Dim realPasswordHash As Byte()
        Dim attemptPasswordHash As Byte()
        Try
            realPasswordHash = DatabaseInterface.GetUserHash(attempt.User)
            attemptPasswordHash = HashService.ComputeHash(ToByte(attempt.Password))

            If (Not IsNothing(realPasswordHash)) AndAlso realPasswordHash.SequenceEqual(attemptPasswordHash) Then
                frmMain.ConsoleOutput("     Hash from database and user match.")

                'getting this IP's assigned public key
                Dim userKeyPair As ElgamalKeyPair = IPKeyMap.Item(origin)

                'hash = username + password + one-time cryptokey
                Dim response As New LoginResponse(HashService.ComputeHash(attempt.User, attempt.Password, userKeyPair.PublicKey))
                Send(response, origin)
                frmMain.ConsoleOutput("     Login successful.")
            Else
                Send(New LoginResponse(False), origin)
                frmMain.ConsoleOutput("     Hash from database and user DO NOT match.")
                frmMain.ConsoleOutput("     Login unsuccessful.")
            End If
        Catch ex As Exception
            frmMain.ConsoleOutput("     Database exception. {0}", ex.Message)
            frmMain.ConsoleOutput("     Login unsuccessful.")
            Send(New LoginResponse(False), origin)
        End Try
    End Sub
    Private Sub ManageRegisterAttempt(ByVal attempt As RegisterAttempt, ByVal origin As String)
        frmMain.ConsoleOutput("     New user to be added to database:")
        frmMain.ConsoleOutput("         Username: {0}", attempt.User.Username)
        frmMain.ConsoleOutput("         First name: {0}", attempt.User.FirstName)
        frmMain.ConsoleOutput("         Surname: {0}", attempt.User.Surname)
        frmMain.ConsoleOutput("         Password: {0}", attempt.Password)

        Dim passwordHash As Byte() = HashService.ComputeHash(ToByte(attempt.Password))
        Dim NetInt As New NetworkConnection(IPAddress.Parse(origin), defPORT)

        Try
            DatabaseInterface.CreateNewUser(attempt.User.Username, attempt.User.FirstName, attempt.User.Surname, passwordHash)
            Send(New Result(True), origin)

        Catch ex As Exception
            frmMain.ConsoleOutput("     New user not successfully created. Exception message:")
            frmMain.ConsoleOutput("         {0}", ex.Message)
            Send(New Result(False) With {.Message = ex.Message}, origin)
        End Try
    End Sub
    Private Sub ManagePublicKeyRequest(ByVal request As PublicKeyRequest, ByVal origin As String)
        Dim newKeyPair As ElgamalKeyPair
        'generating a new public+private key pair for each IP address that requests it
        newKeyPair = ElgamalService.GenerateNewKeyPair(LocalPublicKey.p, LocalPublicKey.g)
        If Not IPKeyMap.ContainsKey(origin) Then   'if IP is not registered
            IPKeyMap.Add(origin, newKeyPair)   'creating new entry in dictionary
        Else
            IPKeyMap.Item(origin) = newKeyPair
        End If
        frmMain.ConsoleOutput("     New key pair generated. Associated to {0}", origin)
        frmMain.ConsoleOutput("         Pr: {0}", newKeyPair.PrivateKey.ToString)
        frmMain.ConsoleOutput("         Pb: {0}", newKeyPair.PublicKey.y.ToString)

        'returning public key only
        Send(newKeyPair.PublicKey, origin)
    End Sub
    Private Sub ManageUserDataRequest(ByVal request As UserDataRequest, ByVal origin As String)
        frmMain.ConsoleOutput("     UserData requested by {0}.", request.Username)
        Dim userPubKey As ElgamalPublicKey = request.PublicKey
        Dim userData As UserData
        userData = DatabaseInterface.GetUserData(request.Username)
        For Each user In userData.FriendList
            If OnlineUsers.Contains(user.Username) Then
                user.PublicKey = UserPublicKeyMap(user.Username)    'if user is online, send their public key to the new logged in user
                user.IsOnline = True
            Else
                user.IsOnline = False
            End If
        Next
        frmMain.ConsoleOutput("     UserData gathered from database.")

        Dim encryptedUserData As ElgamalCiphertext
        encryptedUserData = ElgamalService.Encrypt(ToByte(userData), request.PublicKey)

        Send(encryptedUserData, origin)
    End Sub
    Private Sub ManageMessage(ByVal data As Message, ByVal origin As String)    'relays incoming messages to the right user
        Dim destinationIP As String = UserIPAddressMap(data.Recipient)
        Send(data, destinationIP)
        frmMain.ConsoleOutput("     Message forwarded from {0} to {1}.", origin, destinationIP)
    End Sub
    Private Sub ManageFriendRequest(ByVal request As FriendRequest, ByVal origin As String)
        frmMain.ConsoleOutput("     {0} sent a friend request to {1}.", request.Requester.Username, request.PersonBeingRequested)
        Try
            'this is getting the actual username for this person. it allows friend requests to be non-case-sensitive
            Dim personBeingRequested As String
            personBeingRequested = DatabaseInterface.GetSingleUserDetails(request.PersonBeingRequested).Username
            If OnlineUsers.Contains(PersonBeingRequested) Then
                Dim destinationIP As String = UserIPAddressMap(PersonBeingRequested)
                Send(request, destinationIP)
                Send(New Result(True), origin)
            Else
                frmMain.ConsoleOutput("     {0} was not online. Cannot send friend request.", request.PersonBeingRequested)
                Send(New Result(False, "This user is not online."), origin)
            End If
        Catch ex As Exception
            frmMain.ConsoleOutput("     Database exception. {0}", ex.Message)
            Send(New Result(False, ex.Message), origin)
        End Try
    End Sub
    Private Sub ManageFriendResponse(ByVal response As FriendResponse, ByVal origin As String)
        If OnlineUsers.Contains(response.Requester) Then    'relays response to the requester
            response.PersonBeingRequested.IsOnline = True   'tells requester that the person being requested is online
            Dim destinationIP As String = UserIPAddressMap(response.Requester)
            Send(response, destinationIP)
        End If

        'requester does not need to be online because friendship can be added to the database
        If response.Accepted Then
            'add friendship to the database
            DatabaseInterface.CreateNewFriendPair(response.Requester, response.PersonBeingRequested.Username)
            frmMain.ConsoleOutput("     Friend response added to database. {0} and {1} are now friends.", response.Requester, response.PersonBeingRequested.Username)
        End If
    End Sub
    Private Sub ManageUserStatusNotification(ByVal notification As UserStatusNotification, ByVal origin As String)
        If notification.IsConnected Then
            'this is checked in case a user closed the program without logging out
            'data just gets overwritten
            If OnlineUsers.Contains(notification.User) Then
                OnlineUsers.Remove(notification.User)
                UserIPAddressMap.Remove(notification.User)
                UserPublicKeyMap.Remove(notification.User)
            End If
            OnlineUsers.Add(notification.User)
            'associate logged in user to their IP
            UserIPAddressMap.Add(notification.User, origin)
            'store user's public key in order to share it with other users
            UserPublicKeyMap.Add(notification.User, notification.PublicKey)

            'no need to store the public key, as it was already stored in ManageUserDataRequest()
            frmMain.ConsoleOutput("     {0} is now ONLINE.", notification.User)
        Else
            OnlineUsers.Remove(notification.User)
            UserIPAddressMap.Remove(notification.User)
            UserPublicKeyMap.Remove(notification.User)
            frmMain.ConsoleOutput("     {0} is now OFFLINE.", notification.User)
        End If

        Dim friendList As String() = DatabaseInterface.GetFriendList(notification.User)
        'forwards the notification to all this user's friends
        For Each username In friendList
            If OnlineUsers.Contains(username) Then
                Dim destinationIP As String = UserIPAddressMap(username)
                Send(notification, destinationIP)
            End If
        Next

        Send(New Result(True), origin)
    End Sub
    Private Sub ManageUserDataUpload(ByVal data As UserData, ByVal origin As String)
        Dim user As User = data.User
        Dim clientSettings As ClientSettings = data.ClientSettings
        Dim messages As UnencryptedMessage() = data.Messages.ToArray

        Try
            DatabaseInterface.UpdateUserData(user, clientSettings, messages)
            frmMain.ConsoleOutput("     UserData ({0}) successfully updated.", user.Username)
            Send(New Result(True), origin)
        Catch ex As Exception
            Send(New Result(False, ex.Message), origin)
        End Try
    End Sub

    Private Function Decrypt(ByVal data As ElgamalCiphertext, ByVal origin As String) As Packet
        Try
            Dim userKeyPair As ElgamalKeyPair = IPKeyMap(origin)

            Dim bArray As Byte() = ElgamalService.Decrypt(data, userKeyPair)
            Return DirectCast(ToObj(bArray), Packet)
        Catch ex As Exception
            frmMain.ConsoleOutput("ERROR. DATA FROM {0} CANNOT BE DECRYPTED.", origin)
            Return data
        End Try
    End Function

    Private Sub Send(ByVal Data As Object, ByVal Destination As String)
        Dim NetInt As New NetworkConnection(IPAddress.Parse(Destination), defPORT)

        Dim CT As New CancellationTokenSource
        CT.CancelAfter(2000)

        Try
            NetInt.Send(Data, CT.Token)
            frmMain.ConsoleOutput("     {0} sent to {1} successfully.", Data.GetType.Name, Destination)
        Catch ex As Exception
            frmMain.ConsoleOutput("     ERROR: {0} could NOT be sent to {1}.", Data.GetType.Name, Destination)
            frmMain.ConsoleOutput("     {0}", ex.Message)
        End Try
    End Sub

    Class NotDecryptableException
        Inherits Exception
        Sub New()
            MyBase.New("The private key for this ciphertext cannot be found.")
        End Sub
    End Class
End Module
