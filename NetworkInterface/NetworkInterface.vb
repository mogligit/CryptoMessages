Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports PacketType
Imports SerializationService.Serialization

Public Class NetworkConnection
    Private IP As IPAddress
    Private Port As Integer

    Private cancelListening As CancellationTokenSource
    Private _canSend As Boolean
    Private _isListening As Boolean

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
    End Sub
    Sub New(ByVal Port As Integer)  'listen-only mode
        Me.IP = Nothing
        Me.Port = Port
        _canSend = False
        _isListening = False
    End Sub

    Public Async Function Send(ByVal obj As Object, ByVal CancellationToken As CancellationToken) As Task
        If _canSend Then    'if class is in listen-only mode, it cannot send anything

            Dim taskCompletionSource As New TaskCompletionSource(Of Task)
            CancellationToken.Register(Sub() taskCompletionSource.TrySetCanceled()) 'detects when the token has been cancelled


            Dim connectTask As Task(Of TcpClient)

            connectTask = Task.Run(Function()
                                       Try
                                           Return New TcpClient(IP.MapToIPv4.ToString, Port)
                                       Catch
                                           Return Nothing
                                       End Try
                                   End Function)

            Dim finishedTask As Task = Await Task.WhenAny(connectTask, taskCompletionSource.Task)
            If (Not IsNothing(finishedTask)) AndAlso (finishedTask Is connectTask) Then
                Dim mClient As TcpClient = connectTask.Result

                Dim objToSend As Byte()

                If obj.GetType = GetType(Byte()) Then   'if obj is already in byte() form
                    objToSend = DirectCast(obj, Byte())
                Else            'else convert to byte array
                    objToSend = ToByte(obj)
                End If

                'open stream
                Using stream As NetworkStream = mClient.GetStream()

                    Dim sendTask As Task = stream.WriteAsync(objToSend, 0, objToSend.Length)
                    Await Task.WhenAny(sendTask, taskCompletionSource.Task)

                End Using
                'close stream
            Else
                Throw New ConnectionException
            End If
        End If
    End Function

    Public Overloads Async Function ListenAsync(ByVal CancellationToken As CancellationToken) As Task(Of Packet)
        Dim mListener As New TcpListener(IPAddress.Any, Port)
        Dim mClient As TcpClient
        Dim dataStream As NetworkStream
        Dim bList As New List(Of Byte)

        Dim taskCompletionSource As New TaskCompletionSource(Of TcpClient)
        CancellationToken.Register(Sub() taskCompletionSource.TrySetCanceled())

        mListener.Start()       'start listening

        Dim taskAcceptTcpClientAsync As Task(Of TcpClient) = mListener.AcceptTcpClientAsync() 'starts waiting for message

        Dim completedTask As Task(Of TcpClient) = Await Task.WhenAny(taskAcceptTcpClientAsync, taskCompletionSource.Task)   'race' between cancellation and message

        If completedTask Is taskAcceptTcpClientAsync Then   'if message is received before cancellation then
            mClient = completedTask.Result
            mListener.Stop()    'stop listening
            dataStream = mClient.GetStream  'get data

            Dim currentByte As Integer = dataStream.ReadByte
            If currentByte <> -1 Then
                Do Until currentByte = -1
                    bList.Add(CByte(currentByte))
                    currentByte = dataStream.ReadByte
                Loop
            End If



            Dim receivedPacket As Packet = DirectCast(ToObj(bList.ToArray), Packet)
            'originIP = DirectCast(mClient.Client.RemoteEndPoint, IPEndPoint).Address.MapToIPv4.ToString
            Return receivedPacket

        Else
            mListener.Stop()    'stops listening if task is cancelled
        End If
        Return Nothing
    End Function

    Private ListenCancellationToken As CancellationTokenSource
    Public Async Sub BeginListen()
        Dim mListener As New TcpListener(IPAddress.Any, Port)
        Dim mClient As TcpClient
        Dim originIP As String
        Dim dataStream As NetworkStream
        Dim bList As New List(Of Byte)
        ListenCancellationToken = New CancellationTokenSource

        Dim taskCompletionSource As New TaskCompletionSource(Of TcpClient)
        ListenCancellationToken.Token.Register(Sub() taskCompletionSource.TrySetCanceled()) 'detects when the token has been cancelled

        mListener.Start()       'start listening

        Dim taskAcceptTcpClientAsync As Task(Of TcpClient) = mListener.AcceptTcpClientAsync() 'starts waiting for message

        _isListening = True

        Dim completedTask As Task(Of TcpClient) = Await Task.WhenAny(taskAcceptTcpClientAsync, taskCompletionSource.Task)   'race' between cancellation and message

        If completedTask Is taskAcceptTcpClientAsync Then   'if message is received before cancellation then
            mClient = completedTask.Result
            mListener.Stop()    'stop listening
            dataStream = mClient.GetStream  'get data

            Dim currentByte As Integer = dataStream.ReadByte
            If currentByte <> -1 Then
                Do Until currentByte = -1
                    bList.Add(CByte(currentByte))
                    currentByte = dataStream.ReadByte
                Loop
            End If



            Dim receivedPacket As Packet = DirectCast(ToObj(bList.ToArray), Packet)
            originIP = DirectCast(mClient.Client.RemoteEndPoint, IPEndPoint).Address.MapToIPv4.ToString
            RaiseEvent OnPacketReceived(receivedPacket, originIP)

            BeginListen()
        Else
            mListener.Stop()    'stops listening if task is cancelled
        End If
    End Sub
    Public Sub EndListen()
        ListenCancellationToken.Cancel()
        _isListening = False
    End Sub

    Class ConnectionException
        Inherits Exception
        Sub New()
            MyBase.New("Cannot connect to IP. It is either unreachable or service is not running.")
        End Sub
    End Class

End Class