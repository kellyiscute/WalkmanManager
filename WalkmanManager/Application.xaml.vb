Imports System.Windows.Threading

Class Application
	Private Sub Application_DispatcherUnhandledException(sender As Object, e As DispatcherUnhandledExceptionEventArgs) Handles Me.DispatcherUnhandledException
		Dim a = My.Computer.FileSystem.OpenTextFileWriter("Error.log", False)
		a.WriteLine(e.Exception.Message)
		a.WriteLine("")
		a.WriteLine(e.Exception.TargetSite)
		a.WriteLine("")
		a.WriteLine(e.Exception.Source)
		a.WriteLine("")
		a.WriteLine(e.Exception.InnerException)
		a.Flush()
		a.Close()
	End Sub

	Private Sub Application_LoadCompleted(sender As Object, e As NavigationEventArgs) Handles Me.LoadCompleted
		LibVLCSharp.Shared.Core.Initialize()
	End Sub

	' 应用程序级事件(例如 Startup、Exit 和 DispatcherUnhandledException)
	' 可以在此文件中进行处理。

End Class
