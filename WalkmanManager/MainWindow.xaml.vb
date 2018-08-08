Class MainWindow
	Private Sub czTitle_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) Handles CzTitle.MouseLeftButtonDown
		DragMove()
	End Sub

	Private Sub btn_window_minimize_Click(sender As Object, e As RoutedEventArgs) Handles BtnWindowMinimize.Click
		Me.WindowState = WindowState.Minimized
	End Sub

	Private Sub btn_window_close_Click(sender As Object, e As RoutedEventArgs) Handles BtnWindowClose.Click
		Environment.Exit(0)
	End Sub

	Private Async Sub MainWindow_ContentRendered(sender As Object, e As EventArgs) Handles Me.ContentRendered
		If Not My.Computer.FileSystem.FileExists("data.db") Then
			Database.CreateDatabase()
		End If
		Dim dlg As New dlg_progress
		MaterialDesignThemes.Wpf.DialogHost.Show(dlg, "window-root")
		Dim upd As New DbUpdater
		Dim songDir = Database.GetSetting("song_dir")
		If IsNothing(songDir) Then
			My.Computer.FileSystem.CreateDirectory("SongLib")
			Database.SaveSetting("song_dir", "SongLib")
		End If
		Try
			Await Task.Run(Function()
							   Dim lstNew = upd.FindNew(Database.GetSetting("song_dir"))
							   Dim lstLost = upd.FindLost()
							   Return New Object() {lstNew, lstLost}
						   End Function)
		Catch ex As TaskCanceledException
		Catch unknown As Exception
		End Try


		'Console.WriteLine(lstNew)
		DlgWindowRoot.IsOpen = False
	End Sub

End Class
