Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports PacketType
Imports SerializationService.Serialization


'This class is an interface between application and transport layer (TCP)
Public Class NetworkConnection
    Private IP As IPAddress
    Private Port As Integer

    Private Const TIMEOUT As Integer = 1000     'default timeout in ms

    Private cancelListening As CancellationTokenSource
    Private _canSend As Boolean
    Private _isListening As Boolean
    Private _isEstablished As Boolean

    Private _socket As Socket

    Public Event OnPacketReceived(ByVal Packet As Packet, ByVal Origin As String)

    Public ReadOnly Property CanSend As Boolean
        Get
            Return _canSend
        End Get
    End Property
    Public ReadOnly Property IsListening As Boolean
        Get
            Return _isListening
        End Get
    End Property

    Sub New(ByVal IP As IPAddress, ByVal Port As Integer)
        Me.IP = IP
        Me.Port = Port
        _canSend = True
        _isListening = False
        _isEstablished = False
    End Sub
    Sub New(ByVal Port As Integer)  'listen-only mode
        Me.IP = IPAddress.Any
        Me.Port = Port
        _canSend = False
        _isListening = False
        _isEstablished = False
    End Sub

    Public Function TryConnect() As Boolean
        Try
            ConnectAsync(New CancellationToken).Wait()
        Catch ex As Exception
            Return False
        End Try

        Return True
    End Function

    Public Async Function ConnectAsync(ByVal ct As CancellationToken) As Task
        Dim socket As New Socket(SocketType.Stream, ProtocolType.Tcp)

        Dim taskCancellation As New TaskCompletionSource(Of List(Of Byte))
        ct.Register(Sub() taskCancellation.TrySetCanceled())

        Dim connectTask As Task = socket.ConnectAsync(IP, Port)
        Dim completedTask As Task = Await Task.WhenAny(connectTask, taskCancellation.Task)

        If completedTask Is connectTask Then
            _isEstablished = True
            Me._socket = socket
        Else
            Throw New ConnectionException
        End If
    End Function

    Public Async Function ListenAsync(ByVal ct As CancellationToken) As Task 'true when connected
        Dim socket As New Socket(SocketType.Stream, ProtocolType.Tcp)
        Dim endpoint As New IPEndPoint(IPAddress.Any, 13000)

        socket.Bind(endpoint)

        Dim connectedSocket As Socket

        connectedSocket = Await Task.Run(Function() As Socket
                                             socket.Listen(100)
                                             Return socket.Accept()
                                         End Function, ct)

        If connectedSocket.Connected Then
            _isEstablished = True
            Me._socket = connectedSocket
        Else
            Throw New ConnectionException
        End If

    End Function

    Public Async Function SendAsync(ByVal obj As Object, ByVal ct As CancellationToken) As Task(Of Boolean)
        If _isEstablished Then    'if class is in listen-only mode, it cannot send anything

            Dim taskCancellation As New TaskCompletionSource(Of Integer)
            ct.Register(Sub() taskCancellation.TrySetCanceled()) 'detects when the token has been cancelled

            'serialise obj into byte array
            Dim objBytes As Byte()
            Dim objArray As ArraySegment(Of Byte)

            If obj.GetType = GetType(Byte()) Then   'if obj is already in byte() form
                objBytes = DirectCast(obj, Byte())
            Else            'else convert to byte array
                objBytes = ToByte(obj)
            End If

            objArray = New ArraySegment(Of Byte)(objBytes)

            Dim taskSend As Task(Of Integer) = _socket.SendAsync(objArray, SocketFlags.None)
            Dim bytesSent As Integer = Await Task.WhenAny(taskSend, taskCancellation.Task).Result

            Return objArray.Count = bytesSent   'returns true if all bytes were sent
        Else
            Throw New ConnectionNotEstablishedException
        End If
    End Function

    Public Overloads Async Function ReceiveAsync(ByVal ct As CancellationToken) As Task(Of Packet)

        Dim taskCancellation As New TaskCompletionSource(Of List(Of Byte))
        ct.Register(Sub() taskCancellation.TrySetCanceled())

        'mListener.Start()       'start listening


        Dim taskReceive As Task(Of List(Of Byte)) = Task.Run(Async Function() As Task(Of List(Of Byte))
                                                                 Dim byteArray As New List(Of Byte)     'total message
                                                                 Dim bufferArray(1023) As Byte
                                                                 Dim buffer As New ArraySegment(Of Byte)(bufferArray)    'create 1kb buffer array

                                                                 Dim bytesReceived As Integer
                                                                 Dim totalBytesReceived As Integer

                                                                 'Each loop has a timeout of 50ms
                                                                 Do
                                                                     Dim cts As New CancellationTokenSource
                                                                     Dim tcs As New TaskCompletionSource(Of Integer)
                                                                     cts.CancelAfter(50)
                                                                     cts.Token.Register(Sub() tcs.SetResult(0))

                                                                     Dim receive As Task(Of Integer)
                                                                     receive = _socket.ReceiveAsync(buffer, SocketFlags.None) 'starts waiting for message

                                                                     receive = Await Task.WhenAny(receive, tcs.Task) 'assigns whichever was there first to taskReceive
                                                                     bytesReceived = receive.Result

                                                                     If bytesReceived > 0 Then
                                                                         totalBytesReceived += bytesReceived
                                                                         byteArray = byteArray.Concat(buffer.ToList).ToList
                                                                     End If
                                                                 Loop While bytesReceived > 0

                                                                 Return byteArray
                                                             End Function, ct)  'still includes normal cancellation token



        Dim completedTask As Task(Of List(Of Byte)) = Await Task.WhenAny(taskReceive, taskCancellation.Task)   'race' between cancellation and message
        If completedTask Is taskReceive Then   'if message is received before cancellation then

            Dim receivedPacket As Packet = DirectCast(ToObj(completedTask.Result.ToArray), Packet)
            Return receivedPacket

        End If
        Return Nothing
    End Function

    'Private ListenCancellationToken As CancellationTokenSource
    'Public Async Sub BeginListen()
    '    Dim mListener As New TcpListener(IPAddress.Any, RemotePort)
    '    Dim mClient As TcpClient
    '    Dim originIP As String
    '    Dim dataStream As NetworkStream
    '    Dim bList As New List(Of Byte)
    '    ListenCancellationToken = New CancellationTokenSource

    '    Dim taskCompletionSource As New TaskCompletionSource(Of TcpClient)
    '    ListenCancellationToken.Token.Register(Sub() taskCompletionSource.TrySetCanceled()) 'detects when the token has been cancelled

    '    mListener.Start()       'start listening

    '    Dim taskAcceptTcpClientAsync As Task(Of TcpClient) = mListener.AcceptTcpClientAsync() 'starts waiting for message

    '    _isListening = True

    '    Dim completedTask As Task(Of TcpClient) = Await Task.WhenAny(taskAcceptTcpClientAsync, taskCompletionSource.Task)   'race' between cancellation and message

    '    If completedTask Is taskAcceptTcpClientAsync Then   'if message is received before cancellation then
    '        mClient = completedTask.Result
    '        mListener.Stop()    'stop listening
    '        dataStream = mClient.GetStream  'get data

    '        Dim currentByte As Integer = dataStream.ReadByte
    '        If currentByte <> -1 Then
    '            Do Until currentByte = -1
    '                bList.Add(CByte(currentByte))
    '                currentByte = dataStream.ReadByte
    '            Loop
    '        End If



    '        Dim receivedPacket As Packet = DirectCast(ToObj(bList.ToArray), Packet)
    '        originIP = DirectCast(mClient.Client.RemoteEndPoint, IPEndPoint).Address.MapToIPv4.ToString
    '        RaiseEvent OnPacketReceived(receivedPacket, originIP)

    '        BeginListen()
    '    Else
    '        mListener.Stop()    'stops listening if task is cancelled
    '    End If
    'End Sub
    'Public Sub EndListen()
    '    ListenCancellationToken.Cancel()
    '    _isListening = False
    'End Sub

    Class ConnectionException
        Inherits Exception
        Sub New()
            MyBase.New("Connection could not be established. Endpoint is unreachable.")
        End Sub
    End Class

    Class ConnectionNotEstablishedException
        Inherits Exception
        Sub New()
            MyBase.New("Connection must be established first before sending any data.")
        End Sub
    End Class

End Class