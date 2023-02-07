Imports PacketType

Public Class frmSettings
    Private ColourSelectEventHandler As New EventHandler(AddressOf ColourSelected)

    Private MeClientSettings As ClientSettings
    Dim ColourSqrList As List(Of Label)

    'This boolean is here because the control NumericUpAndDown raises the event ValueChanged
    'when the form first starts, without letting it initialise first
    Private IsFormLoaded As Boolean = False

    Public Sub Initialise(ByVal MeClientSettings As ClientSettings)

        Me.MeClientSettings = MeClientSettings

        ColourSqrList = grpColour.Controls.Cast(Of Label).ToList

        'displaying current settings
        Dim currentColour As Label = ColourSqrList.Find(Function(label As Label) label.BackColor = MeClientSettings.BackgroundColour)
        ColourSelected(currentColour, Nothing)
        nudTextSize.Value = MeClientSettings.TextSize
        txtTextSizePreview.Font = New Font(txtTextSizePreview.Font.FontFamily, MeClientSettings.TextSize)

        For Each colourSqr In ColourSqrList
            AddHandler DirectCast(colourSqr, Label).Click, ColourSelectEventHandler
        Next
        IsFormLoaded = True
    End Sub
    Private Sub frmSettings_FormClosing(sender As Object, e As FormClosingEventArgs)
        If e.CloseReason = CloseReason.UserClosing Then
            Dim Apply As MsgBoxResult
            Apply = MsgBox("Would you like to apply the settings?", MsgBoxStyle.YesNo)

            If Apply = MsgBoxResult.Yes Then
                ApplySettings()
            End If
        End If
    End Sub
    Private Sub ApplySettings() Handles btnApply.Click
        frmMain.UpdateClientSettings(MeClientSettings)
        Me.Close()
    End Sub
    Private Sub Cancel() Handles btnCancel.Click
        Me.Close()
    End Sub

    Private Sub ColourSelected(ByVal sender As Label, ByVal e As EventArgs) 'called by ColourSelectEventHandler
        For Each colour In ColourSqrList
            colour.Text = ""
        Next
        sender.Text = "x"
        Dim brightness As Integer = sender.BackColor.GetBrightness() * 100
        If brightness >= 50 Then
            sender.ForeColor = Color.Black
        Else
            sender.ForeColor = Color.White
        End If

        MeClientSettings.BackgroundColour = sender.BackColor
    End Sub

    Private Sub nudTextSize_ValueChanged() Handles nudTextSize.ValueChanged
        If IsFormLoaded Then
            Dim txtSize As Integer = CInt(nudTextSize.Value)
            txtTextSizePreview.Font = New Font(txtTextSizePreview.Font.FontFamily, txtSize)

            MeClientSettings.TextSize = txtSize
        End If

    End Sub
End Class