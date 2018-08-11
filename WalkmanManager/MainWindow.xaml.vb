Imports System.Windows.Shell
Imports MaterialDesignThemes.Wpf
Imports WalkmanManager.Database

Class MainWindow
	Private Sub czTitle_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) _
		Handles CzTitle.MouseLeftButtonDown
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
			CreateDatabase()
		End If
		Dim dlg As New dlg_progress
		DialogHost.Show(dlg, "window-root")
		Dim upd As New DbUpdater
		Dim songDir = GetSetting("song_dir")
		If IsNothing(songDir) Then
			My.Computer.FileSystem.CreateDirectory("SongLib")
			SaveSetting("song_dir", "SongLib")
		End If
		Dim songs As List(Of SongInfo)

		Dim newLost = Await Task.Run(Async Function()
										 Dim lstNew = Await upd.FindNew(GetSetting("song_dir"))
										 Dim lstLost = Await upd.FindLost()
										 songs = GetSongs()
										 Dim SyncResult As String = ""
										 If lstNew.Count > 0 Then
											 SyncResult = "=========================发现以下新项目=========================" & vbNewLine
											 For Each NewItem In lstNew
												 SyncResult += NewItem & vbNewLine
											 Next
										 End If
										 If lstLost.Count > 0 Then
											 SyncResult += "=========================发现已删除项目=========================" & vbNewLine
											 For Each LostItem In lstLost
												 SyncResult += LostItem & vbNewLine
											 Next
										 End If
										 Return SyncResult
									 End Function)
		DatSongList.ItemsSource = songs
		DlgWindowRoot.IsOpen = False
		If newLost <> "" Then
			Dim DlgSyncResult = New dlgDirSyncResult(newLost)
			Await DialogHost.Show(DlgSyncResult, "window-root")
		End If
	End Sub

	Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
		WindowChrome.SetWindowChrome(Me,
									New WindowChrome() _
										With {.GlassFrameThickness = New Thickness(0),
										.UseAeroCaptionButtons = False, .ResizeBorderThickness = New Thickness(5), .CornerRadius = New CornerRadius(0),
										.CaptionHeight = 0})
	End Sub
End Class
