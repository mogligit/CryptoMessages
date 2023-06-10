Imports System.IO.Ports
Imports System.Net
Imports System.Net.Sockets
Imports System.Net.WebSockets
Imports System.Security.Cryptography
Imports System.Threading
Imports PacketType
Imports SerializationService.Serialization


'This class is an interface between application and transport layer (TCP)
Public Class SessionManager
    Private cancelListening As CancellationTokenSource
    Private _isListening As Boolean

    Private _sessions As New Dictionary(Of IPEndPoint, Socket)
    Private _cancelTokens As New Dictionary(Of IPEndPoint, CancellationTokenSource)

    Public Event PacketReceived(ByVal message As Packet, remoteEndpoint As IPEndPoint)
    Public Event NewConnection(ByVal endpoint As IPEndPoint)

    Public ReadOnly Property IsListening As Boolean
        Get
            Return _isListening
        End Get
    End Property

    Public Sub New()
        _isListening = False
    End Sub

    Public Sub Disconnect(ByVal endpoint As IPEndPoint)
        If Not _sessions.ContainsKey(endpoint) Then
            Throw New SessionNotFoundException
        End If

        Dim thisSocket As Socket = _sessions(endpoint)
        If Not thisSocket.Connected Then
            Throw New SessionNotFoundException
        End If

        Dim thisCT As CancellationTokenSource = _cancelTokens(endpoint)

        thisCT.Cancel()
        thisSocket.Close(1)
        thisSocket.Dispose()

    End Sub

    ''' <summary>
    ''' Creates a new connection with server. Only used by Client.
    ''' </summary>
    ''' <param name="endpoint"></param>
    ''' <param name="ct"></param>
    ''' <returns></returns>
    Public Async Function ConnectAsync(ByVal endpoint As IPEndPoint, ByVal ct As CancellationToken) As Task(Of Boolean)
        Dim socket As New Socket(SocketType.Stream, ProtocolType.Tcp)

        Dim taskCancellation As New TaskCompletionSource(Of List(Of Byte))
        ct.Register(Sub() taskCancellation.TrySetCanceled())

        Dim connectTask As Task = socket.ConnectAsync(endpoint)
        Dim completedTask As Task = Await Task.WhenAny(connectTask, taskCancellation.Task)

        If completedTask Is connectTask Then
            _sessions.Add(endpoint, socket)
            _cancelTokens.Add(endpoint, New CancellationTokenSource)

            StartReceive(endpoint, _cancelTokens(endpoint).Token)
            Return True
        Else
            Return False
        End If
    End Function

    ''' <summary>
    ''' Listens for incoming connections. Only used by Server.
    ''' </summary>
    ''' <param name="port"></param>
    ''' <param name="ct"></param>
    Public Async Sub StartListen(ByVal port As Integer, ByVal ct As CancellationToken)
        _isListening = True
        Do While Not ct.IsCancellationRequested
            Dim listenTask As Task(Of IPEndPoint)
            listenTask = ListenAsync(port, ct)

            Await listenTask
            If Not ct.IsCancellationRequested Then
                StartReceive(listenTask.Result, _cancelTokens(listenTask.Result).Token)
                RaiseEvent NewConnection(listenTask.Result)
            End If
        Loop
        _isListening = False
    End Sub

    ''' <summary>
    ''' Listens for incoming connections on <paramref name="port"/>. Raises the NewConnection event when a new connection is accepted.
    ''' </summary>
    ''' <param name="ct">Cancellation token to stop listening.</param>
    ''' <param name="port">Port to listen on.</param>
    ''' <remarks>This method will raise the event NewConnection indefinitely until the Cancellation Token is cancelled.</remarks>
    Public Async Function ListenAsync(ByVal port As Integer, ByVal ct As CancellationToken) As Task(Of IPEndPoint)
        Using socket As New Socket(SocketType.Stream, ProtocolType.Tcp)
            Dim localEndPoint As New IPEndPoint(IPAddress.Any, port)
            Dim remoteEndPoint As IPEndPoint
            Dim newSocket As Socket

            socket.Bind(localEndPoint)
            socket.Listen(1000)

            'Dim listeningConnection As Task(Of Socket)
            'listeningConnection = Task.Run(Function() As Socket
            '                                   socket.Listen(100)
            '                                   Return socket.Accept()
            '                               End Function, ct)



            Try
                newSocket = Await socket.AcceptAsync()

                remoteEndPoint = DirectCast(newSocket.RemoteEndPoint, IPEndPoint)
                _sessions.Add(remoteEndPoint, newSocket)
                _cancelTokens.Add(remoteEndPoint, New CancellationTokenSource)

                Return remoteEndPoint
            Catch ex As Exception
                Throw New SocketException
            End Try
        End Using



    End Function

    Public Async Function SendAsync(ByVal endpoint As IPEndPoint, ByVal obj As Object, ByVal ct As CancellationToken) As Task(Of Boolean)
        If Not _sessions.ContainsKey(endpoint) Then
            Throw New SessionNotFoundException
        End If

        Dim socket As Socket = _sessions(endpoint)
        If Not socket.Connected Then
            Throw New SessionNotFoundException
        End If

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

        Dim taskSend As Task(Of Integer) = socket.SendAsync(objArray, SocketFlags.None)
        Dim bytesSent As Integer = Await Task.WhenAny(taskSend, taskCancellation.Task).Result

        Return objArray.Count = bytesSent   'returns true if all bytes were sent

    End Function

    Public Async Sub StartReceive(ByVal endpoint As IPEndPoint, ByVal ct As CancellationToken)
        Do While Not ct.IsCancellationRequested
            Dim listenTask As Task(Of Packet)
            listenTask = ReceiveAsync(endpoint, ct)

            Await listenTask
            If Not ct.IsCancellationRequested Then
                RaiseEvent PacketReceived(listenTask.Result, endpoint)
            End If
        Loop
    End Sub

    Public Async Function ReceiveAsync(ByVal endpoint As IPEndPoint, ByVal ct As CancellationToken) As Task(Of Packet)

        If Not _sessions.ContainsKey(endpoint) Then
            Throw New SessionNotFoundException
        End If

        Dim thisSocket As Socket = _sessions(endpoint)
        If Not thisSocket.Connected Then
            Throw New SessionNotFoundException
        End If

        Dim taskCancellation As New TaskCompletionSource(Of Integer)
        ct.Register(Sub() taskCancellation.TrySetCanceled())


        Dim taskWaitForData As Task(Of Integer) = Task.Run(Function() As Integer
                                                               Do While Not thisSocket.Available > 0
                                                                   Thread.Sleep(10)
                                                               Loop
                                                               Return thisSocket.Available
                                                           End Function, ct)

        Await taskWaitForData   'wait for data to become available
        Debug.WriteLine("Data available: " & taskWaitForData.Result)

        If ct.IsCancellationRequested Then
            Return Nothing
        End If

        Dim listBytesReceived As New List(Of Byte)     'total message
        Dim bufferArray(thisSocket.Available - 1) As Byte   'create buffer array of whatever the size of the data is
        Dim buffer As New ArraySegment(Of Byte)(bufferArray)

        Dim taskCompleted As Task(Of Integer)
        Dim numberBytesReceived As Integer

        Dim taskReceive As Task(Of Integer)

        taskReceive = thisSocket.ReceiveAsync(buffer, SocketFlags.None) 'gets data and put it in buffer
        taskCompleted = Await Task.WhenAny(taskReceive, taskCancellation.Task)
        Debug.WriteLine("Task status: " & taskCompleted.Status.ToString)

        If taskCompleted Is taskReceive And taskReceive.Result > 0 Then   'if message is received before cancellation then
            numberBytesReceived += taskCompleted.Result
            listBytesReceived = listBytesReceived.Concat(buffer.ToList).ToList

            Debug.WriteLine("Attempt to deserialize")
            Dim receivedMessage As Packet = DirectCast(ToObj(listBytesReceived.ToArray), Packet)

            Debug.WriteLine("Message received of type: " & receivedMessage.GetType.ToString)
            ', DirectCast(socket.RemoteEndPoint, IPEndPoint))

            Return receivedMessage
        End If
        Return Nothing

    End Function

    Class ConnectionException
        Inherits Exception
        Sub New()
            MyBase.New("Connection could not be established.")
        End Sub
    End Class

    Class SessionNotFoundException
        Inherits Exception
        Sub New()
            MyBase.New("Could not find an open session for this endpoint. Unable to send data.")
        End Sub
    End Class
End Class