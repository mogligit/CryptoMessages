Imports System.Numerics
Imports System.Drawing
Imports System.Net

<Serializable>
Public MustInherit Class Packet
    Public Property IsForServer As Boolean = False
End Class

<Serializable>
Public Class PublicKeyRequest
    Inherits Packet
End Class

<Serializable>
Public Class ElgamalPublicKey
    Inherits Packet
    Public Property p As BigInteger
    Public Property g As BigInteger
    Public Property y As BigInteger

    Sub New(ByVal p As BigInteger,
            ByVal g As BigInteger,
            ByVal y As BigInteger)
        Me.p = p
        Me.g = g
        Me.y = y
    End Sub
End Class

<Serializable>
Public Class ElgamalCiphertext
    Inherits Packet
    Public Property C1 As BigInteger
    Public Property C2 As BigInteger()

    Sub New(ByVal C1 As BigInteger,
            ParamArray ByVal C2 As BigInteger())
        Me.C1 = C1
        Me.C2 = C2
    End Sub
End Class

<Serializable>
Public Class Message
    Inherits Packet
    Public Property DateAndTime As Date
    Public Property Sender As String
    Public Property Recipient As String
    Public Property Message As ElgamalCiphertext

    Sub New(ByVal DateAndTime As Date,
            ByVal Sender As String,
            ByVal Recipient As String,
            ByVal Message As ElgamalCiphertext)
        Me.DateAndTime = DateAndTime
        Me.Sender = Sender
        Me.Recipient = Recipient
        Me.Message = Message
    End Sub
End Class
<Serializable>
Public Class UnencryptedMessage
    Inherits Packet
    Public Property DateAndTime As Date
    Public Property Sender As String
    Public Property Recipient As String
    Public Property Message As String

    Sub New(ByVal DateAndTime As Date,
            ByVal Sender As String,
            ByVal Recipient As String,
            ByVal Message As String)
        Me.DateAndTime = DateAndTime
        Me.Sender = Sender
        Me.Recipient = Recipient
        Me.Message = Message
    End Sub
End Class

<Serializable>
Public Class User
    Inherits Packet
    Public Property Username As String
    Public Property FirstName As String
    Public Property Surname As String
    'These are optional to add
    Public Property PublicKey As ElgamalPublicKey
    Public Property IsOnline As Boolean

    Sub New(ByVal Username As String,
            ByVal FirstName As String,
            ByVal Surname As String)
        Me.Username = Username
        Me.FirstName = FirstName
        Me.Surname = Surname
    End Sub
End Class

<Serializable>
Public Class UserStatusNotification
    Inherits Packet
    Public Property IsConnected As Boolean
    Public Property User As String
    Public Property PublicKey As ElgamalPublicKey

    Sub New(ByVal IsConnected As Boolean,
            ByVal User As String,
            ByVal PublicKey As ElgamalPublicKey)
        Me.IsConnected = IsConnected
        Me.User = User
        Me.PublicKey = PublicKey
    End Sub
End Class

<Serializable>
Public Class RegisterAttempt
    Inherits Packet
    Public Property User As User
    Public Property Password As String

    Sub New(ByVal User As User,
            ByVal Password As String)
        Me.User = User
        Me.Password = Password
    End Sub
End Class

<Serializable>
Public Class Result
    Inherits Packet
    Public Property OK As Boolean
    Public Property Message As String

    Sub New(ByVal OK As Boolean)
        Me.OK = OK
    End Sub
    Sub New(ByVal OK As Boolean, ByVal Message As String)
        Me.OK = OK
        Me.Message = Message
    End Sub
End Class

<Serializable>
Public Class LoginAttempt
    Inherits Packet
    Public Property User As String
    Public Property Password As String

    Sub New(ByVal User As String,
            ByVal Password As String)

        Me.User = User
        Me.Password = Password
    End Sub
End Class

<Serializable>
Public Class LoginResponse
    Inherits Packet
    Public Property IsSuccessful As Boolean
    Public Property Hash As Byte()

    Sub New(ByVal Hash As Byte())
        IsSuccessful = True
        Me.Hash = Hash
    End Sub
    Sub New(ByVal IsSuccessful As Boolean)
        Me.IsSuccessful = IsSuccessful
    End Sub
End Class

<Serializable>
Public Class UserData
    Inherits Packet
    Public Property User As User
    Public Property FriendList As List(Of User)
    Public Property ClientSettings As ClientSettings
    Public Property Messages As List(Of UnencryptedMessage) 'It's a list so more messages can be added later on.

    Sub New(ByVal User As User,
            ByVal ConnectedUsers As List(Of User),
            ByVal ClientSettings As ClientSettings,
            ByVal Messages As List(Of UnencryptedMessage))
        Me.User = User
        Me.FriendList = ConnectedUsers
        Me.ClientSettings = ClientSettings
        Me.Messages = Messages
    End Sub
End Class
<Serializable>
Public Class UserDataRequest
    Inherits Packet
    Public Property Username As String
    Public Property PublicKey As ElgamalPublicKey

    Sub New(ByVal Username As String,
            ByVal PublicKey As ElgamalPublicKey)
        Me.Username = Username
        Me.PublicKey = PublicKey
    End Sub
End Class

<Serializable>
Public Class FriendRequest
    Inherits Packet
    Public Property Requester As User
    Public Property PersonBeingRequested As String

    Sub New(ByVal Sender As User,
            ByVal PersonBeingRequested As String)
        Me.Requester = Sender
        Me.PersonBeingRequested = PersonBeingRequested
    End Sub
End Class
<Serializable>
Public Class FriendResponse
    Inherits Packet
    Public Property Accepted As Boolean
    Public Property PersonBeingRequested As User
    Public Property Requester As String

    Sub New(ByVal Accepted As Boolean,
            ByVal PersonBeingRequested As User,
            ByVal Requester As String)
        Me.Accepted = Accepted
        Me.PersonBeingRequested = PersonBeingRequested
        Me.Requester = Requester
    End Sub
End Class

<Serializable>
Public Class ClientSettings
    Public Property BackgroundColour As Color
    Public Property TextSize As Integer

    Sub New(ByVal BackgroundColour As Color,
            ByVal TextSize As Integer)
        Me.BackgroundColour = BackgroundColour
        Me.TextSize = TextSize
    End Sub
End Class

<Serializable>
Public Class Ping
    Inherits Packet
    Public Property Random As Integer
    Sub New()
        Random = New Random().Next(1, 100)
    End Sub
End Class