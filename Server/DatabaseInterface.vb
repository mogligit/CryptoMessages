Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Data.SQLite
Imports PacketType

Class DatabaseInterface
    Private Shared dbConnection As SQLiteConnection
    Private Shared _IsConnected As Boolean
    Private Const DEFAULT_DB_CONNECTION_STRING = "Data Source=cryptomessages.db"
    Private Const DEFAULT_DB_SCHEMA = "BEGIN TRANSACTION;
CREATE TABLE If Not EXISTS ""_Message"" (
	""DateAndTime""	INTEGER,
	""Sender""	TEXT,
	""Recipient""	TEXT,
	""Message""	TEXT,
	PRIMARY KEY(""DateAndTime"", ""Sender"", ""Recipient""),
    FOREIGN KEY(""Sender"") REFERENCES ""_User""(""Username""),
	FOREIGN KEY(""Recipient"") REFERENCES ""_User""(""Username"")
);
CREATE TABLE If Not EXISTS ""_Friend"" (
	""Friend1""	TEXT,
	""Friend2""	TEXT,
	PRIMARY KEY(""Friend1"", ""Friend2""),
    FOREIGN KEY(""Friend2"") REFERENCES ""_User""(""Username""),
	FOREIGN KEY(""Friend1"") REFERENCES ""_User""(""Username"")
);
CREATE TABLE If Not EXISTS ""_User"" (
	""Username""	TEXT,
	""FirstName""	TEXT,
	""Surname""	TEXT,
	""Passwd""	BLOB,
	""ClientTextSize""	INTEGER,
	""ClientColour""	TEXT,
	PRIMARY KEY(""Username"")
);
COMMIT;"


    Public Shared ReadOnly Property IsConnected As Boolean
        Get
            Return _IsConnected
        End Get
    End Property

    Public Overloads Shared Function StartConnection(Optional connectionString As String = Nothing) As Boolean
        If _IsConnected Then
            frmMain.ConsoleOutput("Connection to the database is already open.")
            Return True
        Else
            frmMain.ConsoleOutput("Connecting to SQLite database...")
            dbConnection = New SQLiteConnection

            If IsNothing(connectionString) Then     'check whether user specified a connection string
                connectionString = ReadCredentialsFromFile()
            Else
                connectionString = DEFAULT_DB_CONNECTION_STRING
            End If

            If Not IsNothing(connectionString) Then
                dbConnection.ConnectionString = connectionString
                Try
                    dbConnection.Open()
                    'restores default schema if it doesn't exist
                    Dim sqlCommand As New SQLiteCommand(DEFAULT_DB_SCHEMA, dbConnection)
                    sqlCommand.ExecuteNonQuery()
                    frmMain.ConsoleOutput("Connection to database established successfully.")
                    _IsConnected = True
                    Return True
                Catch ex As Exception
                    frmMain.ConsoleOutput("Error while connecting to database. Please check connection string.")
                    Return False
                End Try
            Else
                Return False
            End If
        End If
    End Function

    Public Shared Function ReadCredentialsFromFile() As String
        Dim credentials As String
        If File.Exists("dbconnection.txt") Then
            Using reader As New StreamReader("dbconnection.txt")
                credentials = reader.ReadLine()
            End Using
            Return credentials
        Else
            Return "Data Source=cryptomessages.db"
            frmMain.ConsoleOutput("     The file dbconnection.txt could not be found. Trying default string 'Data Source=cryptomessages.db'...")
            'frmMain.ConsoleOutput("     The file dbconnection.txt could not be found. Cannot connect to database.")
            'frmMain.ConsoleOutput("     Please make sure it is in the same directory as the executing path of this program.")
        End If
        Return Nothing
    End Function

    'Public Overloads Shared Function StartConnection(ByVal IP As String, ByVal Username As String, ByVal Password As String) As Boolean
    '    If _IsConnected Then
    '        frmMain.ConsoleOutput("Connection to the database is already open.")
    '        Return True
    '    Else
    '        dbConnection = New SQLiteConnection
    '        Dim credentials As String = String.Format("server={0}; userid={1};password={2}; database=B9296_CryptoMessages;", IP, Username, Password)
    '        dbConnection.ConnectionString = credentials
    '        Try
    '            dbConnection.Open()
    '            frmMain.ConsoleOutput("Connection with database established successfully.")
    '            _IsConnected = True
    '            Return True
    '        Catch ex As Exception
    '            frmMain.ConsoleOutput("Error while connecting to database. Please check credentials.")
    '            Return False
    '        End Try
    '    End If
    'End Function
    Public Shared Function StopConnection() As Boolean
        If _IsConnected Then
            Try
                dbConnection.Close()
                dbConnection.Dispose()
                frmMain.ConsoleOutput("Connection with database closed successfully.")
                _IsConnected = False
                Return True
            Catch ex As Exception
                frmMain.ConsoleOutput("Error while closing connection with database. Please try again.")
                Return False
            End Try
        Else
            frmMain.ConsoleOutput("Connection to the database is already closed.")
            Return True
        End If
    End Function

    Public Shared Sub CreateNewUser(ByVal Username As String, ByVal FirstName As String, ByVal Surname As String, ByVal Password As Byte())
        Dim rex As New Regex("[\w]*")   'just to make sure they are not SQL commands (to avoid SQL injection)
        If rex.IsMatch(Username) And rex.IsMatch(FirstName) And rex.IsMatch(Surname) Then
            Dim newUserSql As String
            newUserSql = "INSERT INTO _User (Username, FirstName, Surname, Passwd, ClientTextSize, ClientColour) VALUES (@Username, @FirstName, @Surname, @Passwd, @ClientTextSize, @ClientColour);"

            Dim newUserCommand As New SQLiteCommand(newUserSql, dbConnection)
            newUserCommand.Parameters.AddWithValue("@Username", Username)
            newUserCommand.Parameters.AddWithValue("@FirstName", FirstName)
            newUserCommand.Parameters.AddWithValue("@Surname", Surname)
            newUserCommand.Parameters.AddWithValue("@Passwd", Password)
            newUserCommand.Parameters.AddWithValue("@ClientTextSize", 8)   'default text size is 8
            newUserCommand.Parameters.AddWithValue("@ClientColour", "White")    'default colour is white


            newUserCommand.ExecuteNonQuery()    'throws exception if Username already exists
        Else
            Throw New NonValidFieldsException
        End If

    End Sub
    Public Shared Function GetUserHash(ByVal Username As String) As Byte()
        Dim hashSql As String = "SELECT Passwd FROM _User WHERE Username=@Username;"
        Dim hashCommand As New SQLiteCommand(hashSql, dbConnection)

        hashCommand.Parameters.AddWithValue("@Username", Username)

        Dim passwordHash As Byte()
        Using sqlDataReader As SQLiteDataReader = hashCommand.ExecuteReader
            If sqlDataReader.Read() Then
                passwordHash = sqlDataReader("Passwd")
                Return passwordHash
            Else
                Throw New UserDoesNotExistException(Username)
            End If
        End Using
    End Function
    Public Shared Function GetUserData(ByVal Username As String) As UserData

        'get user data
        Dim dataSql As String = "SELECT Username, FirstName, Surname, ClientTextSize, ClientColour FROM _User WHERE Username=@Username;"
        Dim dataCommand As New SQLiteCommand(dataSql, dbConnection)

        dataCommand.Parameters.AddWithValue("@Username", Username)

        Dim meUser As User
        Dim userSettings As ClientSettings
        Using userDataRd As SQLiteDataReader = dataCommand.ExecuteReader
            userDataRd.Read()

            meUser = New User(userDataRd("Username"), userDataRd("FirstName"), userDataRd("Surname"))
            meUser.IsOnline = True
            userSettings = New ClientSettings(Color.FromName(userDataRd("ClientColour")), userDataRd("ClientTextSize"))
        End Using

        'get all messages related to the user (either as sender or receiver)
        Dim messageSql As String = "SELECT * FROM _Message WHERE Sender=@Username OR Recipient=@Username ORDER BY DateAndTime ASC;"
        Dim messageCommand As New SQLiteCommand(messageSql, dbConnection)
        messageCommand.Parameters.AddWithValue("@Username", Username)

        Dim messageList As New List(Of UnencryptedMessage)
        Using messageDataRd As SQLiteDataReader = messageCommand.ExecuteReader
            'cycle through all rows of message and put them into a UnencryptedMessage object
            Do While messageDataRd.Read()
                messageList.Add(New UnencryptedMessage(messageDataRd("DateAndTime"), messageDataRd("Sender"), messageDataRd("Recipient"), messageDataRd("Message")))
            Loop
        End Using

        'query that outputs all friends of username @me
        Dim friendsSql As String = "SELECT IIF(Friend1=@me, Friend2, Friend1), FirstName, Surname FROM _Friend, _User WHERE (Friend1=@me OR Friend2=@me) AND IIF(Friend1=@me, Friend2, Friend1)=Username;"
        'IF(condition, if_true, if_false)
        'IF(Friend1=@me, Friend2, Friend1) checks if Friend1 contains @me
        'If it does, Then @Me's friend username must be in the column Friend2, so output Friend2. Else output Friend1
        'IF statement is used twice because I need to refer to the friend's username again in the Where clause, in order to get their data too
        Dim friendCommand As New SQLiteCommand(friendsSql, dbConnection)

        friendCommand.Parameters.AddWithValue("@me", meUser.Username)


        Dim friendList As New List(Of User)
        Using friendDataRd As SQLiteDataReader = friendCommand.ExecuteReader
            Do While friendDataRd.Read
                friendList.Add(New User(friendDataRd(friendDataRd.GetName(0)), friendDataRd("FirstName"), friendDataRd("Surname")))
            Loop
        End Using

        Dim data As New UserData(meUser, friendList, userSettings, messageList)

        Return data
    End Function
    Public Shared Function GetFriendList(ByVal Username As String) As String()

        Dim friendsSql As String = "SELECT IIF(Friend1=@me, Friend2, Friend1) FROM _Friend WHERE (Friend1=@me OR Friend2=@me);"

        Dim friendCommand As New SQLiteCommand(friendsSql, dbConnection)

        friendCommand.Parameters.AddWithValue("@me", Username)


        Dim friendList As New List(Of String)
        Using friendDataRd As SQLiteDataReader = friendCommand.ExecuteReader
            Do While friendDataRd.Read
                friendList.Add(friendDataRd(friendDataRd.GetName(0)))
            Loop
        End Using
        Return friendList.ToArray
    End Function
    Public Shared Sub CreateNewFriendPair(ByVal Friend1 As String, ByVal Friend2 As String)
        Dim newFriendSql As String = "INSERT INTO _Friend VALUES (@Friend1, @Friend2);"
        Dim newFriendCommand As New SQLiteCommand(newFriendSql, dbConnection)

        With newFriendCommand.Parameters
            .AddWithValue("@Friend1", Friend1)
            .AddWithValue("@Friend2", Friend2)
        End With


        newFriendCommand.ExecuteNonQuery()
    End Sub
    Public Shared Sub UpdateUserData(ByVal User As User, ByVal Settings As ClientSettings, ByVal Messages As UnencryptedMessage())
        Dim settingsSql As String = "UPDATE _User SET ClientTextSize=@TextSize, ClientColour=@Colour WHERE Username=@Username;"
        Dim settingCommand As New SQLiteCommand(settingsSql, dbConnection)

        With settingCommand.Parameters
            .AddWithValue("@TextSize", Settings.TextSize)
            .AddWithValue("@Colour", Settings.BackgroundColour.Name)
            .AddWithValue("@Username", User.Username)
        End With
        settingCommand.ExecuteNonQuery()

        If Messages.Count > 0 Then
            Dim messageSql As String = ""
            Dim messageCommand As New SQLiteCommand

            Dim count As Integer = 0
            For Each Message In Messages

                messageSql = messageSql & String.Format("INSERT INTO _Message VALUES (@Timestamp{0}, @Sender{1}, @Recipient{2}, @Message{3});", count, count, count, count)

                Dim timestamp As Long = (Message.DateAndTime - New DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds    'store as Unix time due to SQLite limitations

                With messageCommand.Parameters
                    .AddWithValue("@Timestamp" & count, timestamp)
                    .AddWithValue("@Sender" & count, Message.Sender)
                    .AddWithValue("@Recipient" & count, Message.Recipient)
                    .AddWithValue("@Message" & count, Message.Message)
                End With

                count += 1
            Next

            messageCommand.Connection = dbConnection
            messageCommand.CommandText = messageSql

            messageCommand.ExecuteNonQuery()
        End If

    End Sub
    Public Shared Function GetSingleUserDetails(ByVal Username As String) As User
        Dim userDetails As User
        Dim friendsSql As String = "SELECT Username, FirstName, Surname FROM _User WHERE Username=@Username;"
        Dim userCommand As New SQLiteCommand(friendsSql, dbConnection)
        userCommand.Parameters.AddWithValue("@Username", Username)
        Using userRd As SQLiteDataReader = userCommand.ExecuteReader
            If userRd.Read() Then
                userDetails = New User(Username, userRd("FirstName"), userRd("Surname"))
                Return userDetails
            Else
                Throw New UserDoesNotExistException(Username)
            End If
        End Using
    End Function
End Class

Class NonValidFieldsException
    Inherits Exception
    Sub New()
        MyBase.New("User fields are not valid.")
    End Sub
End Class
Class UserDoesNotExistException
    Inherits Exception
    Sub New(ByVal username As String)
        MyBase.New(String.Format("A user was not found with username {0}.", username))
    End Sub
End Class