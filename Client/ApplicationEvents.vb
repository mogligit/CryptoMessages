Imports System.Threading

Namespace My
    ' The following events are available for MyApplication:
    ' Startup: Raised when the application starts, before the startup form is created.
    ' Shutdown: Raised after all application forms are closed.  This event is not raised if the application terminates abnormally.
    ' UnhandledException: Raised if the application encounters an unhandled exception.
    ' StartupNextInstance: Raised when launching a single-instance application and the application is already active. 
    ' NetworkAvailabilityChanged: Raised when the network connection is connected or disconnected.
    Partial Friend Class MyApplication
        Private Sub KeyGeneration() Handles MyBase.Startup
            KeyGenerationThread.Start()
        End Sub
        'Private Sub SingleInstanceWarning() Handles MyBase.StartupNextInstance
        '    MsgBox(String.Format("Only one instance of this application{0}can be opened at one time.", vbCrLf))
        'End Sub
    End Class
End Namespace
