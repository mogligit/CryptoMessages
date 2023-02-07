<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class frmMain
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim tabctrlFriend As System.Windows.Forms.TabControl
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(frmMain))
        Me.tabFriendList = New System.Windows.Forms.TabPage()
        Me.lstFriendList = New System.Windows.Forms.ListBox()
        Me.tabFriendRequests = New System.Windows.Forms.TabPage()
        Me.tlpFriendRequests = New System.Windows.Forms.TableLayoutPanel()
        Me.btnAddFriend = New System.Windows.Forms.Button()
        Me.btnLogOff = New System.Windows.Forms.Button()
        Me.btnOpenSettings = New System.Windows.Forms.Button()
        Me.txtMessageInput = New System.Windows.Forms.TextBox()
        Me.btnSendMessage = New System.Windows.Forms.Button()
        Me.lblCurrentChat = New System.Windows.Forms.Label()
        Me.tabctrlChats = New System.Windows.Forms.TabControl()
        Me.btnCloseTab = New System.Windows.Forms.Button()
        tabctrlFriend = New System.Windows.Forms.TabControl()
        tabctrlFriend.SuspendLayout()
        Me.tabFriendList.SuspendLayout()
        Me.tabFriendRequests.SuspendLayout()
        Me.SuspendLayout()
        '
        'tabctrlFriend
        '
        tabctrlFriend.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        tabctrlFriend.Controls.Add(Me.tabFriendList)
        tabctrlFriend.Controls.Add(Me.tabFriendRequests)
        tabctrlFriend.Location = New System.Drawing.Point(12, 12)
        tabctrlFriend.Name = "tabctrlFriend"
        tabctrlFriend.SelectedIndex = 0
        tabctrlFriend.Size = New System.Drawing.Size(344, 307)
        tabctrlFriend.TabIndex = 6
        '
        'tabFriendList
        '
        Me.tabFriendList.BackColor = System.Drawing.Color.Transparent
        Me.tabFriendList.Controls.Add(Me.lstFriendList)
        Me.tabFriendList.ForeColor = System.Drawing.Color.Transparent
        Me.tabFriendList.Location = New System.Drawing.Point(4, 22)
        Me.tabFriendList.Name = "tabFriendList"
        Me.tabFriendList.Padding = New System.Windows.Forms.Padding(3)
        Me.tabFriendList.Size = New System.Drawing.Size(336, 281)
        Me.tabFriendList.TabIndex = 0
        Me.tabFriendList.Text = "Friends"
        Me.tabFriendList.UseVisualStyleBackColor = True
        '
        'lstFriendList
        '
        Me.lstFriendList.BackColor = System.Drawing.Color.White
        Me.lstFriendList.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.lstFriendList.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lstFriendList.FormattingEnabled = True
        Me.lstFriendList.Location = New System.Drawing.Point(3, 3)
        Me.lstFriendList.Name = "lstFriendList"
        Me.lstFriendList.Size = New System.Drawing.Size(330, 275)
        Me.lstFriendList.TabIndex = 0
        '
        'tabFriendRequests
        '
        Me.tabFriendRequests.Controls.Add(Me.tlpFriendRequests)
        Me.tabFriendRequests.ForeColor = System.Drawing.Color.Transparent
        Me.tabFriendRequests.Location = New System.Drawing.Point(4, 22)
        Me.tabFriendRequests.Name = "tabFriendRequests"
        Me.tabFriendRequests.Padding = New System.Windows.Forms.Padding(3)
        Me.tabFriendRequests.Size = New System.Drawing.Size(336, 281)
        Me.tabFriendRequests.TabIndex = 1
        Me.tabFriendRequests.Text = "Friend Requests"
        Me.tabFriendRequests.UseVisualStyleBackColor = True
        '
        'tlpFriendRequests
        '
        Me.tlpFriendRequests.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.[Single]
        Me.tlpFriendRequests.ColumnCount = 3
        Me.tlpFriendRequests.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.tlpFriendRequests.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25.0!))
        Me.tlpFriendRequests.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25.0!))
        Me.tlpFriendRequests.Dock = System.Windows.Forms.DockStyle.Fill
        Me.tlpFriendRequests.Location = New System.Drawing.Point(3, 3)
        Me.tlpFriendRequests.Name = "tlpFriendRequests"
        Me.tlpFriendRequests.RowCount = 1
        Me.tlpFriendRequests.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.tlpFriendRequests.Size = New System.Drawing.Size(330, 275)
        Me.tlpFriendRequests.TabIndex = 0
        '
        'btnAddFriend
        '
        Me.btnAddFriend.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnAddFriend.BackColor = System.Drawing.Color.White
        Me.btnAddFriend.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnAddFriend.ForeColor = System.Drawing.Color.Black
        Me.btnAddFriend.Location = New System.Drawing.Point(16, 327)
        Me.btnAddFriend.Name = "btnAddFriend"
        Me.btnAddFriend.Size = New System.Drawing.Size(75, 22)
        Me.btnAddFriend.TabIndex = 2
        Me.btnAddFriend.Text = "Add Friend"
        Me.btnAddFriend.UseVisualStyleBackColor = False
        '
        'btnLogOff
        '
        Me.btnLogOff.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnLogOff.BackColor = System.Drawing.Color.White
        Me.btnLogOff.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnLogOff.ForeColor = System.Drawing.Color.Black
        Me.btnLogOff.Location = New System.Drawing.Point(302, 327)
        Me.btnLogOff.Name = "btnLogOff"
        Me.btnLogOff.Size = New System.Drawing.Size(50, 23)
        Me.btnLogOff.TabIndex = 3
        Me.btnLogOff.Text = "Log off"
        Me.btnLogOff.UseVisualStyleBackColor = False
        '
        'btnOpenSettings
        '
        Me.btnOpenSettings.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnOpenSettings.BackColor = System.Drawing.Color.White
        Me.btnOpenSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnOpenSettings.ForeColor = System.Drawing.Color.Black
        Me.btnOpenSettings.Location = New System.Drawing.Point(232, 327)
        Me.btnOpenSettings.Name = "btnOpenSettings"
        Me.btnOpenSettings.Size = New System.Drawing.Size(64, 23)
        Me.btnOpenSettings.TabIndex = 4
        Me.btnOpenSettings.Text = "Settings..."
        Me.btnOpenSettings.UseVisualStyleBackColor = False
        '
        'txtMessageInput
        '
        Me.txtMessageInput.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtMessageInput.BackColor = System.Drawing.Color.White
        Me.txtMessageInput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.txtMessageInput.ForeColor = System.Drawing.Color.Black
        Me.txtMessageInput.Location = New System.Drawing.Point(363, 329)
        Me.txtMessageInput.Name = "txtMessageInput"
        Me.txtMessageInput.Size = New System.Drawing.Size(357, 20)
        Me.txtMessageInput.TabIndex = 7
        Me.txtMessageInput.Text = "No chat selected."
        '
        'btnSendMessage
        '
        Me.btnSendMessage.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnSendMessage.BackColor = System.Drawing.Color.White
        Me.btnSendMessage.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnSendMessage.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnSendMessage.ForeColor = System.Drawing.Color.Black
        Me.btnSendMessage.Location = New System.Drawing.Point(726, 327)
        Me.btnSendMessage.Name = "btnSendMessage"
        Me.btnSendMessage.Size = New System.Drawing.Size(46, 23)
        Me.btnSendMessage.TabIndex = 8
        Me.btnSendMessage.Text = "Send"
        Me.btnSendMessage.UseVisualStyleBackColor = False
        '
        'lblCurrentChat
        '
        Me.lblCurrentChat.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblCurrentChat.BackColor = System.Drawing.Color.Transparent
        Me.lblCurrentChat.Font = New System.Drawing.Font("Consolas", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblCurrentChat.ForeColor = System.Drawing.Color.Black
        Me.lblCurrentChat.Location = New System.Drawing.Point(387, 2)
        Me.lblCurrentChat.Name = "lblCurrentChat"
        Me.lblCurrentChat.Size = New System.Drawing.Size(385, 28)
        Me.lblCurrentChat.TabIndex = 9
        Me.lblCurrentChat.Text = "Select chat or add friends..."
        Me.lblCurrentChat.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'tabctrlChats
        '
        Me.tabctrlChats.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.tabctrlChats.Location = New System.Drawing.Point(363, 30)
        Me.tabctrlChats.Name = "tabctrlChats"
        Me.tabctrlChats.SelectedIndex = 0
        Me.tabctrlChats.Size = New System.Drawing.Size(409, 289)
        Me.tabctrlChats.TabIndex = 10
        '
        'btnCloseTab
        '
        Me.btnCloseTab.BackColor = System.Drawing.Color.Transparent
        Me.btnCloseTab.BackgroundImage = Global.Client.My.Resources.Resources.close_square_black_512
        Me.btnCloseTab.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.btnCloseTab.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnCloseTab.Font = New System.Drawing.Font("OCR A Extended", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnCloseTab.ForeColor = System.Drawing.Color.Transparent
        Me.btnCloseTab.Location = New System.Drawing.Point(363, 9)
        Me.btnCloseTab.Margin = New System.Windows.Forms.Padding(1)
        Me.btnCloseTab.Name = "btnCloseTab"
        Me.btnCloseTab.Size = New System.Drawing.Size(20, 20)
        Me.btnCloseTab.TabIndex = 11
        Me.btnCloseTab.UseVisualStyleBackColor = False
        '
        'frmMain
        '
        Me.AcceptButton = Me.btnSendMessage
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
        Me.BackColor = System.Drawing.Color.White
        Me.ClientSize = New System.Drawing.Size(784, 361)
        Me.Controls.Add(Me.lblCurrentChat)
        Me.Controls.Add(Me.btnCloseTab)
        Me.Controls.Add(Me.tabctrlChats)
        Me.Controls.Add(Me.btnSendMessage)
        Me.Controls.Add(Me.txtMessageInput)
        Me.Controls.Add(tabctrlFriend)
        Me.Controls.Add(Me.btnOpenSettings)
        Me.Controls.Add(Me.btnLogOff)
        Me.Controls.Add(Me.btnAddFriend)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "frmMain"
        Me.Text = "CryptoMessages"
        tabctrlFriend.ResumeLayout(False)
        Me.tabFriendList.ResumeLayout(False)
        Me.tabFriendRequests.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents btnAddFriend As System.Windows.Forms.Button
    Friend WithEvents btnLogOff As System.Windows.Forms.Button
    Friend WithEvents btnOpenSettings As System.Windows.Forms.Button
    Friend WithEvents tabFriendList As System.Windows.Forms.TabPage
    Friend WithEvents lstFriendList As System.Windows.Forms.ListBox
    Friend WithEvents tabFriendRequests As System.Windows.Forms.TabPage
    Friend WithEvents txtMessageInput As System.Windows.Forms.TextBox
    Friend WithEvents btnSendMessage As System.Windows.Forms.Button
    Friend WithEvents tlpFriendRequests As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents lblCurrentChat As System.Windows.Forms.Label
    Friend WithEvents tabctrlChats As TabControl
    Friend WithEvents btnCloseTab As Button
End Class
