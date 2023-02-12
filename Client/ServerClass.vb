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
    Private IP As String
    Private PORT As Integer = 13000     'default port is 13000
    Private NetInt As NetworkConnection

    Sub New(ByVal IP As String)
        Me.IP = IP
        'initialise NetInt
        NetInt = New NetworkConnection(IPAddress.Parse(IP).MapToIPv4, PORT)
        connected = ConnectAsync().Result
    End Sub
    Public ReadOnly Property IsConnected As Boolean
        Get
            Return connected
        End Get
    End Property
    Public ReadOnly Property ServerIP As String
        Get
            Return IP
        End Get
    End Property
    Public Async Function ConnectAsync() As Task(Of Boolean)
        Dim nPing As New Ping
        Dim nRandom As Integer = nPing.Random
        Dim response As Packet
        Dim pingResult As Ping

        response = Await RequestAndResponse(nPing)
        If Not IsNothing(response) Then
            If response.GetType() = GetType(Ping) Then
                pingResult = response
                If pingResult.Random = nRandom Then
                    connected = True
                    Return True
                Else
                    'if the data in the ping is not the same
                    Throw New UnknownTypeException
                End If
            Else
                Throw New UnknownTypeException
            End If
        Else
            Throw New NullResponseException
        End If
    End Function
    Public Function Disconnect() As Boolean
        Return NetInt.TryDisconnect()
    End Function
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
        Dim sendTask As Task = NetInt.SendAsync(request, CT.Token)
        Dim listeningTask As Task(Of Packet) = NetInt.ReceiveAsync(CT.Token)    'listen for response, 2s timeout

        Await sendTask
        Await listeningTask

        If CT.IsCancellationRequested Then
            Throw New ServerTimeoutException
        Else
            Return listeningTask.Result
        End If

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