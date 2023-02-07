Imports PacketType

Public Class frmMain
    Private MeUser As User
    Private MeUserData As UserData

    Private UserChatLogList As New List(Of ListBox)
    Private FriendRequestList As New List(Of User)
    Private SelectedRecipient As User

    Private FrndRqstBtnEventHandler As New EventHandler(AddressOf SendFriendResponse)

    Private CustomisableControls As List(Of Control)


    'INITIAL PROCEDURES
    Public Async Sub Initialise(ByVal Username As String)
        Me.Enabled = False

        frmLoading.Show("Fetching your data...")
        'we need to fetch user data first
        Dim data As UserData = Await PacketManager.FetchUserData(Username)
        UpdateUserData(data)
        'notify server that this user is online
        Await PacketManager.ChangeOnlineStatus(True, MeUser)

        PacketManager.InitialiseNetInt()
        frmLoading.Close()
        Me.Enabled = True
        Me.BringToFront()
        txtMessageInput.Enabled = False
        btnSendMessage.Enabled = False
    End Sub
    Private Sub UpdateUserData(ByVal NewData As UserData)
        MeUserData = NewData
        MeUser = NewData.User
        UpdateClientSettings(NewData.ClientSettings)
        UpdateFriendList()
        UpdateChatLogs()
    End Sub


    '
    'NETWORK RELATED PROCEDURES
    '
    Private Async Sub SendMessage() Handles btnSendMessage.Click
        If Not IsNothing(SelectedRecipient) Then
            Dim messageToSend As String = txtMessageInput.Text

            If Not (messageToSend.Length = 0 Or messageToSend.Trim.Length = 0) Then
                txtMessageInput.Text = "Sending..."

                Dim Now As Date = DateAndTime.Now
                Dim result As Result
                result = Await PacketManager.TrySendMessage(Now, MeUser.Username, SelectedRecipient.Username, messageToSend, SelectedRecipient.PublicKey)

                If result.OK Then
                    ChatLogOutput(Now, SelectedRecipient.Username, "Me > {0}", messageToSend)
                    'saves to MeUserData to be backed up later
                    MeUserData.Messages.Add(New UnencryptedMessage(Now, MeUser.Username, SelectedRecipient.Username, messageToSend))
                    txtMessageInput.Clear()
                Else
                    MsgBox(result.Message)
                End If

            End If
        End If
    End Sub
    Private Async Sub SendFriendRequest() Handles btnAddFriend.Click
        Dim recipient As String

        recipient = InputBox("Please input the Username of the user you want to add.", "Add friend")

        If recipient.Trim.Length <> 0 Then
            If MeUserData.FriendList.Exists(Function(user As User) user.Username = recipient) Then
                MsgBox("This user is your friend already.")
            Else
                Dim result As Result = Await PacketManager.TrySendFriendRequest(MeUser, recipient)
                If result.OK Then
                    MsgBox(String.Format("Request sent successfully{0}to {1}.", vbCrLf, recipient))
                Else
                    MsgBox(result.Message)
                End If
            End If
        End If
    End Sub
    Private Async Sub SendFriendResponse(ByVal sender As Object, ByVal e As EventArgs)    'called by FrndRqstBtnEventHandler
        Dim buttonName As String = DirectCast(sender, Button).Name
        Dim Reply As Boolean = buttonName.StartsWith("btnAccepted")
        Dim Username As String = buttonName.Substring(11)   'btnAccepted and btnDeclined have the same length of 10

        Dim RequesterUser As User = FriendRequestList.Find(Function(user As User) user.Username = Username)
        RequesterUser.IsOnline = True
        If Reply = True Then
            MeUserData.FriendList.Add(RequesterUser)
            UpdateFriendList()
            UpdateChatLogs()
        End If
        FriendRequestList.Remove(RequesterUser)

        Dim result As Result
        result = Await PacketManager.TrySendFriendResponse(MeUser, Username, Reply)

        If result.OK Then
            'code to delete a row from tlpFriendRequests
            'it deletes all controls from that row and then moves all controls
            'below that row up a row
            With tlpFriendRequests
                Dim rowNumber As Integer = .GetPositionFromControl(sender).Row
                For column = 0 To 2
                    .Controls.Remove(tlpFriendRequests.GetControlFromPosition(column, rowNumber))

                    For row = rowNumber To .RowCount - 3
                        .Controls.Add(.GetControlFromPosition(column, row + 1), column, row)
                    Next

                    .Controls.Remove(tlpFriendRequests.GetControlFromPosition(column, .RowCount - 2))
                Next

                .RowStyles.RemoveAt(rowNumber)
                .RowCount -= 1
            End With
        Else
            MsgBox(result.Message)
        End If
    End Sub



    '
    'UI RELATED PROCEDURES
    '
    Public Sub DisplayIncomingMessage(ByVal DateAndTime As Date, ByVal Sender As String, ByVal Recipient As String, ByVal Message As String)
        ChatLogOutput(DateAndTime, Sender, "{0} > {1}", Sender, Message)
        If Sender = MeUser.Username Then
            MeUserData.Messages.Add(New UnencryptedMessage(DateAndTime, Sender, Recipient, Message))
        End If
    End Sub
    Public Sub DisplayFriendRequest(ByVal request As FriendRequest)
        FriendRequestList.Add(request.Requester)

        Dim AcceptBtnKey As String = "btnAccepted" & request.Requester.Username
        Dim DeclineBtnKey As String = "btnDeclined" & request.Requester.Username

        'add a row in tlpFriendRequests (because its a TableLayoutPanel)
        'it's adding a row in .RowCount - 2 because TableLayoutPanels initialise with 1 empty row (it doesn't go any lower)
        'so it's placing items from the first row and creates a new blank one at the botton to fill up the empty space
        tlpFriendRequests.RowCount += 1
        tlpFriendRequests.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))
        tlpFriendRequests.Controls.Add(New Label With {.Text = request.Requester.Username, .TextAlign = ContentAlignment.MiddleCenter, .Dock = DockStyle.Fill, .Font = New Font(Me.Font.FontFamily, MeUserData.ClientSettings.TextSize), .ForeColor = Color.Black}, 0, tlpFriendRequests.RowCount - 2)
        tlpFriendRequests.Controls.Add(New Button With {.Name = AcceptBtnKey, .Text = "Accept", .Dock = DockStyle.Fill, .Font = New Font(Me.Font.FontFamily, MeUserData.ClientSettings.TextSize), .FlatStyle = FlatStyle.Flat, .ForeColor = Color.Black}, 1, tlpFriendRequests.RowCount - 2)
        tlpFriendRequests.Controls.Add(New Button With {.Name = DeclineBtnKey, .Text = "Decline", .Dock = DockStyle.Fill, .Font = New Font(Me.Font.FontFamily, MeUserData.ClientSettings.TextSize), .FlatStyle = FlatStyle.Flat, .ForeColor = Color.Black}, 2, tlpFriendRequests.RowCount - 2)

        AddHandler DirectCast(tlpFriendRequests.Controls.Find(AcceptBtnKey, False).First, Button).Click, FrndRqstBtnEventHandler
        AddHandler DirectCast(tlpFriendRequests.Controls.Find(DeclineBtnKey, False).First, Button).Click, FrndRqstBtnEventHandler
    End Sub
    Public Sub DisplayFriendAccepted(ByVal response As FriendResponse)
        MeUserData.FriendList.Add(response.PersonBeingRequested)
        UpdateFriendList()
        UpdateChatLogs()
    End Sub
    Private Sub ChangeSelectedRecipient() Handles lstFriendList.SelectedIndexChanged

        If Not IsNothing(lstFriendList.SelectedItem) Then
            'set selected recipient
            SelectedRecipient = MeUserData.FriendList(lstFriendList.SelectedIndex)

            If SelectedRecipient.IsOnline Then
                txtMessageInput.Text = "Type something and press enter..."
            Else
                txtMessageInput.Text = "This user is not online. Please try again later."
            End If

            txtMessageInput.Enabled = SelectedRecipient.IsOnline
            btnSendMessage.Enabled = SelectedRecipient.IsOnline

            Dim chatTabKey As String = "tabChat" & SelectedRecipient.Username

            lblCurrentChat.Text = String.Format("{0} {1}", SelectedRecipient.FirstName, SelectedRecipient.Surname)

            If tabctrlChats.TabPages.ContainsKey(chatTabKey) Then
                tabctrlChats.SelectTab(tabctrlChats.TabPages.IndexOfKey(chatTabKey))
            Else
                'Creating new tab for new chat
                tabctrlChats.TabPages.Add(chatTabKey, SelectedRecipient.FirstName)
                tabctrlChats.TabPages(tabctrlChats.TabCount - 1).BackColor = Color.White
                'Selecting new tab (opening it)
                tabctrlChats.SelectTab(tabctrlChats.TabPages.IndexOfKey(chatTabKey))

                'Finding user list and placing it inside new tab
                Dim userList As ListBox
                userList = UserChatLogList.Where(Function(lstBox) lstBox.Name = "lstChat" & SelectedRecipient.Username).First
                'Placing it inside the tab
                userList.Parent = tabctrlChats.SelectedTab
                userList.Dock = DockStyle.Fill
            End If
        End If

    End Sub

    Private Sub UpdateControlColours(ByVal colour As Color)

        Dim buttonList As List(Of Button) = {btnAddFriend, btnCloseTab, btnLogOff, btnOpenSettings, btnSendMessage}.ToList

        'special case for white because I think it looks better like this
        If colour = Color.White Then
            Me.BackColor = Color.White
            For Each btn In buttonList
                btn.ForeColor = Color.Black
                btn.BackColor = Color.Transparent
            Next
            txtMessageInput.BackColor = Color.White
            lblCurrentChat.ForeColor = Color.Black
        Else    'all other colours
            Dim textColour As Color
            Dim secondaryShade As Color
            If colour.GetBrightness * 100 >= 50 Then
                textColour = Color.Black
            Else
                textColour = Color.White
            End If

            'makes a darker shade of the colour
            secondaryShade = Color.FromArgb(colour.R * 2 / 3,
                                            colour.G * 2 / 3,
                                            colour.B * 2 / 3)


            Me.BackColor = colour

            lblCurrentChat.ForeColor = textColour
            txtMessageInput.ForeColor = textColour
            txtMessageInput.BackColor = secondaryShade
            For Each btn In buttonList
                btn.BackColor = secondaryShade
                'dark buttons and white text looks awesome
                btn.ForeColor = Color.White
            Next
        End If

    End Sub
    Public Sub UpdateClientSettings(ByVal NewSettings As ClientSettings)
        MeUserData.ClientSettings = NewSettings

        lstFriendList.Font = New Font(lstFriendList.Font.FontFamily, NewSettings.TextSize)
        For Each lstChat In UserChatLogList
            lstChat.Font = New Font(lstFriendList.Font.FontFamily, NewSettings.TextSize)
        Next
        tabctrlChats.Font = New Font(tabctrlChats.Font.FontFamily, NewSettings.TextSize)

        UpdateControlColours(NewSettings.BackgroundColour)
    End Sub



    '
    'PROCEDURES RELATED TO GENERAL FUNCTIONS OF THIS FORM
    '
    Private Sub UpdateFriendList()
        lstFriendList.Items.Clear()

        For Each user In MeUserData.FriendList
            Dim userEntry As String = String.Format("{0} ({1} {2})", user.Username, user.FirstName, user.Surname)

            If user.IsOnline Then
                userEntry = userEntry.Insert(0, "[ONLINE] ")
            End If
            lstFriendList.Items.Add(userEntry)
        Next
    End Sub
    Private Sub UpdateChatLogs()
        For Each user In MeUserData.FriendList
            'this is the expected name of the particular ListBox
            Dim chatLogName As String = "lstChat" & user.Username

            'if there isn't a chat list with their name
            If Not UserChatLogList.Exists(Function(lstChat As ListBox) lstChat.Name = chatLogName) Then
                Dim newChatList As New ListBox With {.Name = chatLogName, .HorizontalScrollbar = True, .SelectionMode = SelectionMode.None, .BorderStyle = BorderStyle.None}
                UserChatLogList.Add(newChatList)

                Dim currentUserMessageList As New List(Of UnencryptedMessage)
                currentUserMessageList = MeUserData.Messages.Where(Function(message As UnencryptedMessage) message.Sender = user.Username Or message.Recipient = user.Username).ToList

                For Each textMessage In currentUserMessageList
                    If textMessage.Sender = MeUser.Username Then
                        ChatLogOutput(textMessage.DateAndTime, textMessage.Recipient, "Me > {0}", textMessage.Message)
                    Else
                        ChatLogOutput(textMessage.DateAndTime, textMessage.Sender, "{0} > {1}", textMessage.Sender, textMessage.Message)
                    End If
                Next
            End If
        Next

        'Removes all already-existant messages, so the server knows that whatever
        'messages are here, they are new. It's to avoid duplicates in the Message table
        MeUserData.Messages.Clear()
    End Sub
    Public Sub UpdateUserStatus(ByVal Username As String, ByVal IsOnline As Boolean, ByVal NewPublicKey As ElgamalPublicKey)
        'NewPublicKey because once a user comes online, they will send their public key to every online user
        Dim user As User = MeUserData.FriendList.Find(Function(friendUser As User) friendUser.Username = Username)
        user.IsOnline = IsOnline

        'IMPORTANT
        'if there is a public key attached, store it.
        If Not IsNothing(NewPublicKey) Then
            user.PublicKey = NewPublicKey
        End If

        Dim indexOfUsername As Integer = MeUserData.FriendList.IndexOf(user)
        Dim currentString As String = lstFriendList.Items.Item(indexOfUsername)
        If IsOnline Then
            currentString = currentString.Insert(0, "[ONLINE] ")
        Else
            currentString = currentString.Remove(0, 9) 'remove [ONLINE] bit from string
        End If
        lstFriendList.Items.Item(indexOfUsername) = currentString
    End Sub
    Private Sub ChatLogOutput(ByVal DateAndTime As Date, ByVal Username As String, ByVal Text As String, ParamArray arg As String())
        'Getting the right list to output to
        Dim userListBox As ListBox = UserChatLogList.Find(Function(list As ListBox) list.Name = "lstChat" & Username)

        Text = String.Format(Text, arg)
        Dim timeString As String = String.Format("[{0}/{1}; {2}:{3}] : ",
                                                 DateAndTime.Day.ToString.PadLeft(2, "0"),
                                                 DateAndTime.Month.ToString.PadLeft(2, "0"),
                                                 DateAndTime.Hour.ToString.PadLeft(2, "0"),
                                                 DateAndTime.Minute.ToString.PadLeft(2, "0"))
        userListBox.Items.Add(timeString & Text)
        userListBox.TopIndex = userListBox.Items.Count - 1
    End Sub



    '
    'PROCEDURES THAT HANDLE USER INPUT
    '
    Private Sub tabctrlChats_Selected(sender As Object, e As TabControlEventArgs) Handles tabctrlChats.Selected
        If Not IsNothing(e.TabPage) Then        'the event .Selected is raised even when there is no tabs open
            'takes the username from the tabname and finds it in the friend list
            'to then select it in the lstFriendList to raise the event ListBox.SelectedIndexChanged
            'and to give the user a sense of uniformity across the UI
            Dim username As String = e.TabPage.Name.Remove(0, 7)
            Dim index As Integer = MeUserData.FriendList.FindIndex(Function(user As User) user.Username = username)
            lstFriendList.SelectedIndex = index
        End If
    End Sub
    Private Sub btnCloseTab_Click(sender As Object, e As EventArgs) Handles btnCloseTab.Click
        If tabctrlChats.TabCount > 0 Then
            If tabctrlChats.TabCount = 1 Then
                lblCurrentChat.Text = "Select chat or add friends..."
                txtMessageInput.Text = "No chat selected."
            End If
            tabctrlChats.TabPages.Remove(tabctrlChats.SelectedTab)
        End If
    End Sub
    Private Sub txtMessageInput_Click() Handles txtMessageInput.Click
        txtMessageInput.Clear()
    End Sub
    Private Sub btnOpenSettings_Click(sender As Object, e As EventArgs) Handles btnOpenSettings.Click
        frmSettings.Show()
        frmSettings.Initialise(MeUserData.ClientSettings)
    End Sub
    Private Async Sub frmMain_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        Me.Visible = False
        frmLoading.Show("Loging off...")
        Await LogOffProtocol(MeUserData)
        frmLoading.Close()
    End Sub
    Private Sub btnLogOff_Click(sender As Object, e As EventArgs) Handles btnLogOff.Click
        frmLogin.Show()
        Me.Close()
    End Sub

End Class
