Imports System.Net
Imports NetworkInterface
Imports PacketType
Imports System.Numerics
Imports Elgamal
Imports SHAService
Imports System.Threading
Imports Serializationservice.Serialization
Imports System.Net.Sockets

Module MessageManager
    'SETTINGS
    Const DEFAULT_LOCAL_PORT As Integer = 13000

    '
    'DICTIONARIES
    '
    'dictionary to map IP addresses to local cryptographic keys
    Private EndpointKeyMap As New Dictionary(Of IPEndPoint, ElgamalKeyPair)
    'dictionary to map logged-in users to their respective IP address
    Private UserEndpointMap As New Dictionary(Of String, IPEndPoint)
    'dictionary to map logged-in users to their public key
    Private UserPublicKeyMap As New Dictionary(Of String, ElgamalPublicKey)


    'list to index all users that are online
    Private OnlineUsers As New List(Of String)

    Private LocalPublicKey As ElgamalPublicKey

    Private ListenCT As New CancellationTokenSource
    Private WithEvents sessions As New SessionManager

    Public Sub STARTUP(Optional SecurityLevel As Integer = 64)
        frmMain.ConsoleOutput("Generating {0}-bit cryptographic keys...", SecurityLevel)
        Dim Elgamal As New ElgamalService(SecurityLevel)
        LocalPublicKey = Elgamal.GenerateKeys()
        frmMain.ConsoleOutput("Done.")

        DatabaseInterface.StartConnection()

        StartListenAsync()
        frmMain.ConsoleOutput("Ready.")
    End Sub

    Public Sub SHUTDOWN(ByVal CloseWindow As Boolean)
        If sessions.IsListening Then
            StopListen()
        End If
        If DatabaseInterface.IsConnected Then
            DatabaseInterface.StopConnection()
        End If
        If CloseWindow Then
            frmMain.Close()
        End If
    End Sub

    Private Sub StartListenAsync(Optional PORT As Integer = DEFAULT_LOCAL_PORT)
        If sessions.IsListening Then
            frmMain.ConsoleOutput("Interface is already running.")
        Else
            Try
                sessions.StartListen(PORT, ListenCT.Token)
                frmMain.ConsoleOutput("Socket now listening on port {0}.", PORT)
            Catch ex As Exception
                frmMain.ConsoleOutput("Could not start listening.", PORT)
            End Try
        End If
    End Sub
    Private Sub OnNewConnection(ByVal endpoint As IPEndPoint) Handles sessions.NewConnection
        frmMain.ConsoleOutput("New connection from {0}.", endpoint.ToString)
    End Sub
    Private Sub StopListen()
        If Not ListenCT.IsCancellationRequested Then
            ListenCT.Cancel()
            frmMain.ConsoleOutput("Socket closed and no longer accepting connections.")
        Else
            frmMain.ConsoleOutput("Socket is already closed.")
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
                                DatabaseInterface.StartConnection(args(Array.IndexOf(args, "-ip") + 1))
                            End If
                        Case "disconnect"
                            DatabaseInterface.StopConnection()
                    End Select
                Case "listener"
                    Select Case args(1)
                        Case "start"
                            If args.Length = 2 Then
                                StartListenAsync()
                            Else
                                StartListenAsync(args(Array.IndexOf(args, "-p") + 1))
                            End If
                        Case "stop"
                            StopListen()
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
            frmMain.ConsoleOutput("Failed to execute command. Reason: " & ex.Message)
        End Try

    End Sub

    Private Sub PacketReceived(ByVal packet As Packet, ByVal endpoint As IPEndPoint) Handles sessions.PacketReceived
        frmMain.ConsoleOutput("{0} RECEIVED FROM {1}", packet.GetType.Name.ToUpper, endpoint.ToString)

        If packet.IsForServer AndAlso packet.GetType = GetType(ElgamalCiphertext) Then
            packet = Decrypt(DirectCast(packet, ElgamalCiphertext), endpoint)
        End If

        Select Case packet.GetType
            Case GetType(Ping)
                ManagePing(DirectCast(packet, Ping), endpoint)
            Case GetType(LoginAttempt)
                ManageLoginAttempt(DirectCast(packet, LoginAttempt), endpoint)
            Case GetType(RegisterAttempt)
                ManageRegisterAttempt(DirectCast(packet, RegisterAttempt), endpoint)
            Case GetType(PublicKeyRequest)
                ManagePublicKeyRequest(DirectCast(packet, PublicKeyRequest), endpoint)
            Case GetType(UserDataRequest)
                ManageUserDataRequest(DirectCast(packet, UserDataRequest), endpoint)
            Case GetType(Message)
                ManageMessage(DirectCast(packet, Message), endpoint)
            Case GetType(FriendRequest)
                ManageFriendRequest(DirectCast(packet, FriendRequest), endpoint)
            Case GetType(FriendResponse)
                ManageFriendResponse(DirectCast(packet, FriendResponse), endpoint)
            Case GetType(UserStatusNotification)
                ManageUserStatusNotification(DirectCast(packet, UserStatusNotification), endpoint)
            Case GetType(UserData)
                ManageUserDataUpload(DirectCast(packet, UserData), endpoint)
        End Select
    End Sub

    Private Sub ManagePing(ByVal data As Ping, ByVal origin As IPEndPoint)
        Send(data, origin)
    End Sub
    Private Sub ManageLoginAttempt(ByVal attempt As LoginAttempt, ByVal origin As IPEndPoint)
        frmMain.ConsoleOutput("     Attempt to log in with credentials: [{0}:{1}].", attempt.User, attempt.Password)

        Dim realPasswordHash As Byte()
        Dim attemptPasswordHash As Byte()
        Try
            realPasswordHash = DatabaseInterface.GetUserHash(attempt.User)
            attemptPasswordHash = HashService.ComputeHash(ToByte(attempt.Password))

            If (Not IsNothing(realPasswordHash)) AndAlso realPasswordHash.SequenceEqual(attemptPasswordHash) Then
                frmMain.ConsoleOutput("     Hash from database and user match.")

                'getting this IP's assigned public key
                Dim userKeyPair As ElgamalKeyPair = EndpointKeyMap.Item(origin)

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
    Private Sub ManageRegisterAttempt(ByVal attempt As RegisterAttempt, ByVal origin As IPEndPoint)
        frmMain.ConsoleOutput("     New user to be added to database:")
        frmMain.ConsoleOutput("         Username: {0}", attempt.User.Username)
        frmMain.ConsoleOutput("         First name: {0}", attempt.User.FirstName)
        frmMain.ConsoleOutput("         Surname: {0}", attempt.User.Surname)
        frmMain.ConsoleOutput("         Password: {0}", attempt.Password)

        Dim passwordHash As Byte() = HashService.ComputeHash(ToByte(attempt.Password))

        Try
            DatabaseInterface.CreateNewUser(attempt.User.Username, attempt.User.FirstName, attempt.User.Surname, passwordHash)
            Send(New Result(True), origin)

        Catch ex As Exception
            frmMain.ConsoleOutput("     New user not successfully created. Exception message:")
            frmMain.ConsoleOutput("         {0}", ex.Message)
            Send(New Result(False) With {.Message = ex.Message}, origin)
        End Try
    End Sub
    Private Sub ManagePublicKeyRequest(ByVal request As PublicKeyRequest, ByVal origin As IPEndPoint)
        Dim newKeyPair As ElgamalKeyPair
        'generating a new public+private key pair for each IP address that requests it
        newKeyPair = ElgamalService.GenerateNewKeyPair(LocalPublicKey.p, LocalPublicKey.g)
        If Not EndpointKeyMap.ContainsKey(origin) Then   'if IP is not registered
            EndpointKeyMap.Add(origin, newKeyPair)   'creating new entry in dictionary
        Else
            EndpointKeyMap.Item(origin) = newKeyPair
        End If
        frmMain.ConsoleOutput("     New key pair generated. Associated to {0}", origin.ToString)
        frmMain.ConsoleOutput("         Pr: {0}", newKeyPair.PrivateKey.ToString)
        frmMain.ConsoleOutput("         Pb: {0}", newKeyPair.PublicKey.y.ToString)

        'returning public key only
        Send(newKeyPair.PublicKey, origin)
    End Sub
    Private Sub ManageUserDataRequest(ByVal request As UserDataRequest, ByVal origin As IPEndPoint)
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
    Private Sub ManageMessage(ByVal data As Message, ByVal origin As IPEndPoint)    'relays incoming messages to the right user
        Dim destination As IPEndPoint = UserEndpointMap(data.Recipient)
        Send(data, destination)
        frmMain.ConsoleOutput("     Message forwarded from {0} to {1}.", origin.ToString, destination.ToString)
    End Sub
    Private Sub ManageFriendRequest(ByVal request As FriendRequest, ByVal origin As IPEndPoint)
        frmMain.ConsoleOutput("     {0} sent a friend request to {1}.", request.Requester.Username, request.PersonBeingRequested)
        Try
            'this is getting the actual username for this person. it allows friend requests to be non-case-sensitive
            Dim personBeingRequested As String
            personBeingRequested = DatabaseInterface.GetSingleUserDetails(request.PersonBeingRequested).Username
            If OnlineUsers.Contains(personBeingRequested) Then
                Dim destination As IPEndPoint = UserEndpointMap(personBeingRequested)
                Send(request, destination)
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
    Private Sub ManageFriendResponse(ByVal response As FriendResponse, ByVal origin As IPEndPoint)
        If OnlineUsers.Contains(response.Requester) Then    'relays response to the requester
            response.PersonBeingRequested.IsOnline = True   'tells requester that the person being requested is online
            Dim destination As IPEndPoint = UserEndpointMap(response.Requester)
            Send(response, destination)
        End If

        'requester does not need to be online because friendship can be added to the database
        If response.Accepted Then
            'add friendship to the database
            DatabaseInterface.CreateNewFriendPair(response.Requester, response.PersonBeingRequested.Username)
            frmMain.ConsoleOutput("     Friend response added to database. {0} and {1} are now friends.", response.Requester, response.PersonBeingRequested.Username)
        End If
    End Sub
    Private Sub ManageUserStatusNotification(ByVal notification As UserStatusNotification, ByVal origin As IPEndPoint)
        If notification.IsConnected Then
            'this is checked in case a user closed the program without logging out
            'data just gets overwritten
            If OnlineUsers.Contains(notification.User) Then
                OnlineUsers.Remove(notification.User)
                UserEndpointMap.Remove(notification.User)
                UserPublicKeyMap.Remove(notification.User)
            End If
            OnlineUsers.Add(notification.User)
            'associate logged in user to their IP
            UserEndpointMap.Add(notification.User, origin)
            'store user's public key in order to share it with other users
            UserPublicKeyMap.Add(notification.User, notification.PublicKey)

            'no need to store the public key, as it was already stored in ManageUserDataRequest()
            frmMain.ConsoleOutput("     {0} is now ONLINE.", notification.User)
        Else
            OnlineUsers.Remove(notification.User)
            UserEndpointMap.Remove(notification.User)
            UserPublicKeyMap.Remove(notification.User)
            frmMain.ConsoleOutput("     {0} is now OFFLINE.", notification.User)
        End If

        Dim friendList As String() = DatabaseInterface.GetFriendList(notification.User)
        'forwards the notification to all this user's friends
        For Each username In friendList
            If OnlineUsers.Contains(username) Then
                Dim destination As IPEndPoint = UserEndpointMap(username)
                Send(notification, destination)
            End If
        Next

        Send(New Result(True), origin)
    End Sub
    Private Sub ManageUserDataUpload(ByVal data As UserData, ByVal origin As IPEndPoint)
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

    Private Function Decrypt(ByVal data As ElgamalCiphertext, ByVal origin As IPEndPoint) As Packet
        Try
            Dim userKeyPair As ElgamalKeyPair = EndpointKeyMap(origin)

            Dim bArray As Byte() = ElgamalService.Decrypt(data, userKeyPair)
            Return DirectCast(ToObj(bArray), Packet)
        Catch ex As Exception
            frmMain.ConsoleOutput("ERROR. DATA FROM {0} CANNOT BE DECRYPTED.", origin.ToString)
            Return data
        End Try
    End Function

    Private Sub Send(ByVal Data As Object, ByVal Destination As IPEndPoint)
        Dim CT As New CancellationTokenSource
        CT.CancelAfter(2000)

        Try
            sessions.SendAsync(Destination, Data, CT.Token)
            frmMain.ConsoleOutput("     {0} sent to {1} successfully.", Data.GetType.Name, Destination.ToString)
        Catch ex As Exception
            frmMain.ConsoleOutput("     ERROR: {0} could NOT be sent to {1}.", Data.GetType.Name, Destination.ToString)
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
