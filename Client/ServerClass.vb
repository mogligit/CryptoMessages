Imports PacketType
Imports System.Net
Imports System.Threading
Imports System.Security.Cryptography
Imports Elgamal
Imports NetworkInterface
Imports SHAService
Imports SerializationService.Serialization
Imports System.Net.Sockets

'handles all connections to the server
Public Class ServerConnection
    Private connected As Boolean = False    'default is false
    Private _serverEndpoint As IPEndPoint
    Private PORT As Integer = 13000     'default port is 13000
    Private WithEvents _session As SessionManager
    Private TIMEOUT As Integer = 5000 'timeout in miliseconds

    Sub New(ByVal IP As String)
        _serverEndpoint = New IPEndPoint(IPAddress.Parse(IP), PORT)
        'initialise NetInt
        _session = New SessionManager
        connected = ConnectAsync().Result
    End Sub
    Public ReadOnly Property IsConnected As Boolean
        Get
            Return connected
        End Get
    End Property
    Public ReadOnly Property ServerEndpoint As IPEndPoint
        Get
            Return _serverEndpoint
        End Get
    End Property

    ''' <summary>
    ''' This part of the code receives the PacketReceived event and redirects it wherever it needs to go.
    ''' If the program is expecting a response, WaitingForResponse is set to True and the packet will be redirected to the RequestAndResponse method.
    ''' If the program is not expecting a response, WaitingForResponse is set to False and the packet will be forwarded to PacketManager.
    ''' </summary>
    Private WaitingForResponse As Boolean = False
    Private ResponsePacket As Packet
    Private ResponseEndpoint As IPEndPoint
    Private Sub ReceivePacket(ByVal packet As Packet, ByVal endpoint As IPEndPoint) Handles _session.PacketReceived
        If WaitingForResponse Then
            ResponsePacket = packet
            ResponseEndpoint = endpoint
            WaitingForResponse = False
        Else
            PacketManager.ReceivePacket(packet, endpoint)
        End If
    End Sub

    Private Async Function RequestAndResponse(ByVal request As Packet, Optional Timeout As Integer = 2000) As Task(Of Packet)
        If Not IsConnected() Then
            Throw New ServerNotConnectedException()
        End If

        Dim CT As New CancellationTokenSource

        'if timeout is 0, wait indifinitely
        If Not Timeout = 0 Then
            CT.CancelAfter(Timeout)    'set timeout for cancellationtoken
        End If

        request.IsForServer = True
        Debug.WriteLine("Sending request to server: " & request.GetType.ToString & "... ")
        Dim sendTask As Task = _session.SendAsync(ServerEndpoint, request, CT.Token)
        Await sendTask
        Debug.Write("Sent.")

        WaitingForResponse = True
        Debug.WriteLine("Waiting for response... ")
        Dim listeningTask As Task(Of Packet) = WaitForResponseAsync(CT.Token)    'listen for response, 2s timeout
        Await listeningTask
        Debug.Write("Received response from server: " & listeningTask.Result.GetType.ToString)
        WaitingForResponse = False



        If CT.IsCancellationRequested Then
            Throw New ServerTimeoutException
        Else
            Return listeningTask.Result
        End If

    End Function
    Private Async Function WaitForResponseAsync(ct As CancellationToken) As Task(Of Packet)
        Await Task.Run(Sub()
                           Dim IsReceivedYet As Boolean = False

                           Do While Not IsReceivedYet
                               IsReceivedYet = Not (IsNothing(ResponsePacket) OrElse IsNothing(ResponseEndpoint) OrElse WaitingForResponse)
                               Thread.Sleep(10)
                           Loop
                       End Sub, ct)

        Return ResponsePacket
    End Function

    Public Async Function SendPacketAsync(ByVal Data As Packet) As Task(Of Boolean)
        Dim cts As New CancellationTokenSource

        'if timeout is 0, wait indifinitely
        If Not TIMEOUT = 0 Then
            cts.CancelAfter(TIMEOUT)    'set timeout for cancellationtoken
        End If

        Dim successful As Boolean = Await _session.SendAsync(ServerEndpoint, Data, cts.Token)
        Return successful
    End Function

    Public Async Function ConnectAsync() As Task(Of Boolean)
        Dim cts As New CancellationTokenSource
        cts.CancelAfter(TIMEOUT)

        Dim successful As Boolean
        Try
            successful = Await _session.ConnectAsync(ServerEndpoint, cts.Token)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    'Public Async Function ConnectAsync() As Task(Of Boolean)
    '    Dim nPing As New Ping
    '    Dim nRandom As Integer = nPing.Random
    '    Dim response As Packet
    '    Dim pingResult As Ping

    '    response = Await RequestAndResponse(nPing)
    '    If Not IsNothing(response) Then
    '        If response.GetType() = GetType(Ping) Then
    '            pingResult = response
    '            If pingResult.Random = nRandom Then
    '                connected = True
    '                Return True
    '            Else
    '                'if the data in the ping is not the same
    '                Throw New UnknownTypeException
    '            End If
    '        Else
    '            Throw New UnknownTypeException
    '        End If
    '    Else
    '        Throw New NullResponseException
    '    End If
    'End Function
    Public Sub Disconnect()
        _session.Disconnect(ServerEndpoint)
    End Sub
    Private Async Function TryConnect() As Task(Of Result)
        Dim nPing As New Ping
        Dim nRandom As Integer = nPing.Random
        Dim response As Packet
        Dim pingResult As Ping

        Try
            response = Await RequestAndResponse(nPing)
            If Not IsNothing(response) Then
                If response.GetType() = GetType(Ping) Then
                    pingResult = response
                    If pingResult.Random = nRandom Then
                        connected = True
                        Return New Result(True)
                    Else
                        'if the data in the ping is not the same
                        Return New Result(False, "Ping does not match.")
                    End If
                Else
                    Throw New UnknownTypeException
                End If
            Else
                Throw New NullResponseException
            End If
        Catch ex As Exception
            Return New Result(False, ex.Message)
        End Try
    End Function


    Public Async Function FetchServerPublicKey() As Task(Of ElgamalPublicKey)
        If Not IsConnected() Then
            Throw New ServerNotConnectedException()
        End If


        Dim pubKeyRequest As New PublicKeyRequest
        Dim response As Packet

        response = Await RequestAndResponse(pubKeyRequest)
        If Not IsNothing(response) Then
            If response.GetType = GetType(ElgamalPublicKey) Then
                Return DirectCast(response, ElgamalPublicKey)
            Else
                Throw New UnknownTypeException
            End If
        Else
            Throw New NullResponseException
        End If
    End Function
    Public Async Function AttemptLogin(ByVal LoginRequest As LoginAttempt) As Task(Of Result)
        If Not IsConnected() Then
            Throw New ServerNotConnectedException()
        End If

        'fetching a one-time public key from server
        Dim serverPubKey As ElgamalPublicKey = Await FetchServerPublicKey()

        'hash: username + password + one-time key
        Dim myHash As Byte() = HashService.ComputeHash(LoginRequest.User, LoginRequest.Password, serverPubKey)

        'encrypting username and password
        Dim ciphertext As ElgamalCiphertext
        ciphertext = ElgamalService.Encrypt(ToByte(LoginRequest), serverPubKey)


        Debug.WriteLine(String.Format("Attempting login - U:{0}", LoginRequest.User))

        Dim response As Packet
        Dim loginResponse As LoginResponse

        'sending login and waiting for approval in form of the same hash
        response = Await RequestAndResponse(ciphertext, 15000)
        If Not IsNothing(response) Then
            If response.GetType() = GetType(LoginResponse) Then
                loginResponse = DirectCast(response, LoginResponse)
                If loginResponse.IsSuccessful AndAlso loginResponse.Hash.SequenceEqual(myHash) Then
                    Return New Result(True)
                Else
                    Return New Result(False, "Login unsuccessful. Please try again.")
                End If
            Else
                Throw New UnknownTypeException
            End If
        Else
            Throw New NullResponseException
        End If
    End Function
    Public Async Function AttemptRegister(ByVal RegisterRequest As RegisterAttempt) As Task(Of Result)
        If Not IsConnected() Then
            Throw New ServerNotConnectedException()
        End If


        Dim response As Packet
        Dim result As Result
        Dim serverPubKey As ElgamalPublicKey = Await FetchServerPublicKey()

        Dim ciphertext As ElgamalCiphertext = ElgamalService.Encrypt(ToByte(RegisterRequest), serverPubKey)

        response = Await RequestAndResponse(ciphertext, 5000)
        If Not IsNothing(response) Then
            If response.GetType = GetType(Result) Then
                result = DirectCast(response, Result)
                Return result
            Else
                Throw New UnknownTypeException
            End If
        Else
            Throw New NullResponseException
        End If
    End Function
    Public Async Function FetchUserData(ByVal Username As String, ByVal LocalPublicKey As ElgamalPublicKey) As Task(Of ElgamalCiphertext)
        If Not IsConnected() Then
            Throw New ServerNotConnectedException()
        End If

        Dim request As New UserDataRequest(Username, LocalPublicKey)
        Dim response As Packet
        Dim ciphertext As ElgamalCiphertext

        response = Await RequestAndResponse(request)

        If Not IsNothing(response) Then
            If response.GetType = GetType(ElgamalCiphertext) Then
                ciphertext = DirectCast(response, ElgamalCiphertext)
                Return ciphertext
            Else
                Throw New UnknownTypeException
            End If
        Else
            Throw New NullResponseException
        End If
    End Function
    Public Async Function ChangeOnlineStatus(ByVal Notification As UserStatusNotification) As Task(Of Result)
        If Not IsConnected() Then
            Throw New ServerNotConnectedException()
        End If

        Dim response As Packet
        Dim result As Result

        Debug.WriteLine("Changing online status to " & Notification.IsConnected)
        response = Await RequestAndResponse(Notification)

        If Not IsNothing(response) Then
            If response.GetType = GetType(Result) Then
                result = DirectCast(response, Result)
                Debug.WriteLine("Result: " & result.OK)
                Return result
            Else
                Throw New UnknownTypeException
            End If
        Else
            Throw New NullResponseException
        End If
    End Function
    Public Async Function UploadUserData(ByVal data As UserData) As Task(Of Result)
        If Not IsConnected() Then
            Throw New ServerNotConnectedException()
        End If

        Try
            Dim serverKey As ElgamalPublicKey = Await FetchServerPublicKey()

            Debug.WriteLine("Server public key: " & serverKey.y.ToString)
            Dim encryptedData As ElgamalCiphertext = ElgamalService.Encrypt(ToByte(data), serverKey)

            Dim response As Packet = Await RequestAndResponse(encryptedData)
            Debug.WriteLine("Data sent.")
            If Not IsNothing(response) Then
                If response.GetType = GetType(Result) Then
                    Dim result As Result = DirectCast(response, Result)
                    Debug.WriteLine("Result: " & result.OK)
                    Return result
                Else
                    Throw New UnknownTypeException
                End If
            Else
                Throw New NullResponseException
            End If

        Catch ex As Exception
            Return New Result(False, ex.Message)
        End Try
    End Function

End Class

Class UnknownTypeException
    Inherits Exception
    Sub New()
        MyBase.New("Response data type is unknown.")
    End Sub
End Class
Class NullResponseException
    Inherits Exception
    Sub New()
        MyBase.New("Response from server was null.")
    End Sub
End Class
Class ServerTimeoutException
    Inherits Exception
    Sub New()
        MyBase.New("Server timeout.")
    End Sub
End Class
Class ServerNotConnectedException
    Inherits Exception
    Sub New()
        MyBase.New("Client connection not initialised.")
    End Sub
End Class