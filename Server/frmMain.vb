Public Class frmMain
    Const ENCRYPTION_LEVEL As Integer = 64

    Public Sub frmMain_Load() Handles Me.Load
        AsciiArt()
        DisplayHelp()
        Me.ActiveControl = txtCommandInput
    End Sub
    Private Sub lstLog_Click(sender As Object, e As EventArgs) Handles lstLog.Click
        Me.ActiveControl = txtCommandInput
    End Sub
    Public Sub frmMain_Close() Handles Me.FormClosing
        MessageManager.SHUTDOWN(False)
    End Sub

    Private Sub txtCommandInput_KeyDown(sender As Object, e As KeyPressEventArgs) Handles txtCommandInput.KeyPress
        If e.KeyChar = ChrW(Keys.Enter) Then
            e.Handled = True

            lstLog.Items.Add(">> " & txtCommandInput.Text)

            Dim args As String()
            args = txtCommandInput.Text.ToLower.Split({" "}, StringSplitOptions.RemoveEmptyEntries)
            MessageManager.HandleCommand(args)

            txtCommandInput.Clear()

        End If
    End Sub

    Public Sub ConsoleOutput(ByVal Text As String, ParamArray arg As String())
        Text = String.Format(Text, arg)
        Dim time As Date = TimeOfDay
        Dim timeString As String = String.Format("[{0}:{1}:{2}] > ",
                                                 TimeOfDay.Hour.ToString.PadLeft(2, "0"),
                                                 TimeOfDay.Minute.ToString.PadLeft(2, "0"),
                                                 TimeOfDay.Second.ToString.PadLeft(2, "0"))
        lstLog.Items.Add(timeString & Text)
        lstLog.TopIndex = lstLog.Items.Count - 1       'autoscroll'
    End Sub

    Private Sub AsciiArt()
        With lstLog.Items
            .Add("   ____                  _        __  __                                     ")
            .Add("  / ___|_ __ _   _ _ __ | |_ ___ |  \/  | ___  ___ ___  __ _  __ _  ___  ___ ")
            .Add(" | |   | '__| | | | '_ \| __/ _ \| |\/| |/ _ \/ __/ __|/ _` |/ _` |/ _ \/ __|")
            .Add(" | |___| |  | |_| | |_) | || (_) | |  | |  __/\__ \__ \ (_| | (_| |  __/\__ \")
            .Add("  \____|_|   \__, | .__/ \__\___/|_|  |_|\___||___/___/\__,_|\__, |\___||___/")
            .Add(" / ___|  ___ |___/|_| _____ _ __                             |___/           ")
            .Add(" \___ \ / _ \ '__\ \ / / _ \ '__|                                            ")
            .Add("  ___) |  __/ |   \ V /  __/ |                                               ")
            .Add(" |____/ \___|_|    \_/ \___|_|                                               ")
            .Add("")
        End With
    End Sub

    Public Sub DisplayHelp()
        ConsoleOutput("--------HELP--------")
        ConsoleOutput("COMMAND      OPTIONS                                                            DESCRIPTION")
        ConsoleOutput("AUTO         [-S (security-level)]                                              Starts the server. [-S] specifies size of the cryptographic keys in bits.")
        ConsoleOutput("CLEAR                                                                           Clears log.")
        ConsoleOutput("DB           CONNECT [-IP ip -U username -P password]                           Database command. Enter credentials in dbconnection.txt for [db connect].")
        ConsoleOutput("             DISCONNECT")
        ConsoleOutput("LISTENER     (START [-P port]) | STOP                                           Starts or stops listening for incoming packages.")
        ConsoleOutput("STOP                                                                            Closes all connections and exits the program.")
        ConsoleOutput("HELP                                                                            Displays this menu.")
    End Sub
End Class