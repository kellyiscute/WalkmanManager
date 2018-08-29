Imports System.Collections.ObjectModel
Imports System.Windows.Shell
Imports MaterialDesignThemes.Wpf
Imports WalkmanManager.Database
Imports ATL
Imports GongSolutions.Wpf.DragDrop

Class MainWindow
	ReadOnly NETEASE_RED = Color.FromRgb(198, 47, 47)
	ReadOnly DEFAULT_COLOR = Color.FromRgb(103, 58, 183)

	Dim _lstSongs As ObservableCollection(Of SongInfo)
	Dim _isRightClickSelect As Boolean = False
	Dim _isCloudMusicLoggedIn As Boolean = False
	Dim _cloudMusic As New CloudMusic

	Private Sub czTitle_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) _
		Handles CzTitle.MouseLeftButtonDown
		Try
			DragMove()
		Catch ex As Exception

		End Try
	End Sub

	Private Sub btn_window_minimize_Click(sender As Object, e As RoutedEventArgs) Handles BtnWindowMinimize.Click
		SystemCommands.MinimizeWindow(Me)
	End Sub

	Private Sub btn_window_close_Click(sender As Object, e As RoutedEventArgs) Handles BtnWindowClose.Click
		Environment.Exit(0)
	End Sub

	Private Async Sub PageSwitcher(sender As Object, e As RoutedEventArgs) Handles LstTopbar.SelectionChanged
		If IsNothing(GridLocal) Then
			Exit Sub
		End If
		If TabCloudMusic.IsSelected Then
			CzTitle.Background = New SolidColorBrush(NETEASE_RED)
		Else
			CzTitle.Background = New SolidColorBrush(DEFAULT_COLOR)
		End If
		If TabLocal.IsSelected Then
			GridLocal.Visibility = Visibility.Visible
			RefreshPlaylists()
		Else
			GridLocal.Visibility = Visibility.Hidden
		End If
		If TabCloudMusic.IsSelected Then
			If _isCloudMusicLoggedIn Then
				GridCloudMusic.Visibility = Visibility.Visible
			Else
				Dim lastPhone As String = ""
				While True
					Dim dlgLogin As New DlgCloudMusicLogin(lastPhone)
					Dim login As Boolean = Await DlgWindowRoot.ShowDialog(dlgLogin)
					If login Then
						Dim dlgProg = New dlg_progress
						dlgProg.ChangeColorTheme(New SolidColorBrush(NETEASE_RED))
						DlgWindowRoot.ShowDialog(dlgProg)
						Dim result = Await UiCloudMusicLogin(dlgLogin, dlgProg)
						If result = "" Then
							For Each p In _cloudMusic.Playlists
								ListBoxCloudMusicPlaylists.Items.Add(
									New ListBoxItem() With {.Content = p("name"), .Padding = New Thickness(20, 10, 0, 10)})
							Next
							Try
								Dim img As New BitmapImage
								img.BeginInit()
								Console.WriteLine(_cloudMusic.UserInfo("avatarUrl"))
								img.UriSource = New Uri(_cloudMusic.UserInfo("avatarUrl"))
								img.CacheOption = BitmapCacheOption.OnLoad
								img.EndInit()
								ImageCloudMusicAvatar.Source = img
								LabelCloudMusicNickName.Content = _cloudMusic.UserInfo("nickname")
							Catch ex As Exception

							End Try

							GridCloudMusic.Visibility = Visibility.Visible
							_isCloudMusicLoggedIn = True
							DlgWindowRoot.IsOpen = False
							If ListBoxCloudMusicPlaylists.Items.Count > 0 Then
								ListBoxCloudMusicPlaylists.SelectedIndex = 0
							End If
							Exit While
						Else
							lastPhone = dlgLogin.TextBoxPhone.Text
							Dim dlgFail As New DlgMessageDialog("登录失败", result)
							dlgFail.ChangeColorTheme(New SolidColorBrush(NETEASE_RED))
							_isCloudMusicLoggedIn = False
							DlgWindowRoot.IsOpen = False
							Await DlgWindowRoot.ShowDialog(dlgFail)
						End If
					Else
						TabCloudMusic.IsSelected = False
						TabLocal.IsSelected = True
						Exit While
					End If
				End While
			End If
		Else
			GridCloudMusic.Visibility = Visibility.Hidden
		End If
	End Sub

	Private Async Function UiCloudMusicLogin(dlgLogin As DlgCloudMusicLogin, dlgProg As dlg_progress) As Task(Of String)
		Dim result = Await Task.Run(Function()
										Try
											dlgProg.Text = "登录中"
											Dim loginResult = _cloudMusic.Login(dlgLogin.Phone, dlgLogin.Password)
											If loginResult("success") Then
												dlgProg.Text = "获取歌单"
												_cloudMusic.GetPlaylists()
												Return ""
											Else
												Return loginResult("msg")
											End If
										Catch
											Return "Unknown Error"
										End Try
									End Function)
		Return result
	End Function

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

		PageSwitcher(Nothing, Nothing)

		Dim newLost = Await Task.Run(Async Function()
										 Dim lstNew = Await upd.FindNew(GetSetting("song_dir"))
										 Dim lstLost = Await upd.FindLost()
										 _lstSongs = GetSongs()
										 Dim syncResult As String = ""
										 If lstNew.Count > 0 Then
											 syncResult = "=========================发现以下新项目=========================" & vbNewLine
											 For Each NewItem In lstNew
												 syncResult += NewItem & vbNewLine
											 Next
										 End If
										 If lstLost.Count > 0 Then
											 syncResult += "=========================发现已删除项目=========================" & vbNewLine
											 For Each LostItem In lstLost
												 syncResult += LostItem & vbNewLine
											 Next
										 End If
										 Return syncResult
									 End Function)
		RefreshPlaylists()
		DatSongList.ItemsSource = _lstSongs
		DlgWindowRoot.IsOpen = False
		If newLost <> "" Then
			Dim dlgSyncResult = New dlgDirSyncResult(newLost)
			Await DialogHost.Show(dlgSyncResult, "window-root")
		End If
	End Sub

	Private Async Sub RefreshPlaylists()
		Dim lstPlaylist = Await Task.Run(Function()
											 Dim r = GetPlaylists()
											 Return r
										 End Function)
		Dim addNew = ListBoxPlaylist.Items(ListBoxPlaylist.Items.Count - 1)
		ListBoxPlaylist.Items.Clear()

		For Each itm In lstPlaylist
			Dim lbitm = New ListBoxItem() With {.Content = itm, .AllowDrop = True}
			AddHandler lbitm.Drop, AddressOf ListBoxItem_Drop
			ListBoxPlaylist.Items.Add(lbitm)
		Next
		ListBoxPlaylist.Items.Add(addNew)
	End Sub

	Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
		WindowChrome.SetWindowChrome(Me,
									 New WindowChrome() _
										With {.GlassFrameThickness = New Thickness(1),
										.UseAeroCaptionButtons = False, .ResizeBorderThickness = New Thickness(5),
										.CornerRadius = New CornerRadius(10),
										.CaptionHeight = 34})
	End Sub

	Private Async Sub DatSongList_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) _
		Handles DatSongList.MouseDoubleClick
		If DatSongList.SelectedIndex <> -1 Then
			DlgWindowRoot.CloseOnClickAway = True
			Dim detailDialog As New DlgSongDetail(DatSongList.SelectedItem.path)
			Await DlgWindowRoot.ShowDialog(detailDialog)

			If Not detailDialog.IsDeleted Then
				If detailDialog.IsChanged Then
					UpdateInfo(DatSongList.SelectedItem.path)
					Dim t As New Track(DatSongList.SelectedItem.path)

					Dim index = _lstSongs.IndexOf(DatSongList.SelectedItem)
					_lstSongs(index) = New SongInfo _
						With {.Id = DatSongList.SelectedItem.Id, .Title = t.Title, .Artists = t.Artist, .Path =
							DatSongList.SelectedItem.path}
				End If
			Else
				_lstSongs.RemoveAt(DatSongList.SelectedIndex)
			End If
		End If
		DlgWindowRoot.CloseOnClickAway = False
	End Sub

	Private Sub ListBoxItem_Drop(sender As Object, e As DragEventArgs)
		If sender.Content.GetType.ToString = "System.Windows.Controls.WrapPanel" Then
			Exit Sub
		End If
		Try
			Dim songInf As SongInfo = e.Data.GetData(DragDrop.DataFormat.Name)
			'if it is 0(initial number of integer), it is probably not dragged from the list
			If songInf.Id = 0 Then
				Exit Sub
			End If
			Dim playlistId = GetPlaylistIdByName(sender.Content)
			AddSongToPlaylist(playlistId, songInf.Id)
		Catch ex As Exception
			If ex.Message = "Already Exist" Then
				Exit Sub
			End If
			Try
				e.Data.GetData("GongSolutions.Wpf.DragDrop")
			Catch
			End Try
			Dim a = e.Data.GetData("GongSolutions.Wpf.DragDrop")
			Dim playlistId = GetPlaylistIdByName(sender.Content)
			Dim conn = Connect()
			Dim trans = conn.BeginTransaction()
			Dim cmd = conn.CreateCommand()
			cmd.Transaction = trans
			For Each itm As SongInfo In a
				Try
					AddSongToPlaylist(playlistId, itm.Id, cmd)
				Catch exc As Exception
					If exc.Message <> "Already Exist" Then
						Console.WriteLine("Unexpected Error : " & exc.Message)
					End If
				End Try
			Next
			trans.Commit()
			conn.Close()
		End Try
	End Sub

	Private Async Sub ListItem_SelectionChange(sender As ListBox, e As EventArgs)
		If sender.SelectedIndex <> -1 And Not _isRightClickSelect Then
			If sender.SelectedItem.Tag = "NewPlaylist" Then
				'if is button "NewPlaylist"
				Dim dlg As New DlgNewPlaylist
				Dim result = Await DlgWindowRoot.ShowDialog(dlg)
				If result Then
					AddPlaylist(dlg.PlaylistName)
					Dim lbitm = New ListBoxItem() With {.Content = dlg.PlaylistName, .AllowDrop = True}
					AddHandler lbitm.Drop, AddressOf ListBoxItem_Drop
					ListBoxPlaylist.Items.Insert(ListBoxPlaylist.Items.Count - 1, lbitm)
				End If
				sender.SelectedIndex = -1
			Else
				ButtonMusic.IsSelected = False
				DatSongList.CanUserSortColumns = False
				ButtonSaveSorting.Visibility = Visibility.Visible
				PanelSearchLocalMusic.Visibility = Visibility.Collapsed
				Dim dlg As New dlg_progress
				DatSongList.ItemsSource = Nothing
				DialogHost.Show(dlg, "window-root")
				Dim playlistName = sender.SelectedItem.Content
				Dim lstSongs = Await Task.Run(Function()
												  Dim songIds = GetSongsFromPlaylist(GetPlaylistIdByName(playlistName))
												  Dim conn = Connect()
												  Dim trans = conn.BeginTransaction()
												  Dim cmd = conn.CreateCommand()
												  cmd.Transaction = trans
												  Dim songs As New ObservableCollection(Of SongInfo)
												  For Each itm In songIds
													  songs.Add(GetSongById(itm, cmd))
												  Next
												  trans.Commit()
												  conn.Close()
												  Return songs
											  End Function)
				DatSongList.ItemsSource = lstSongs
				DlgWindowRoot.IsOpen = False
			End If
		End If
		_isRightClickSelect = False
	End Sub

	Private Sub BlockRightClickSelectoin(sender As Object, e As MouseButtonEventArgs)
		_isRightClickSelect = True
	End Sub

	Private Sub ButtonMusic_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) _
		Handles ButtonMusic.MouseLeftButtonUp
		DatSongList.ItemsSource = Nothing
		DatSongList.ItemsSource = _lstSongs
		ListBoxPlaylist.SelectedIndex = -1
		DatSongList.CanUserSortColumns = False
		PanelSearchLocalMusic.Visibility = Visibility.Visible
		ButtonSaveSorting.Visibility = Visibility.Collapsed
	End Sub

	'Private Sub ButtonMusic_Selected(sender As Object, e As RoutedEventArgs) Handles ButtonMusic.Selected
	'	On Error Resume Next
	'	ListBoxPlaylist.SelectedIndex = -1
	'	DatSongList.CanUserSortColumns = True
	'End Sub

	Private Async Sub DeletePlaylist_Click(sender As Object, e As EventArgs) Handles MenuDeletePlaylist.Click
		If ListBoxPlaylist.SelectedIndex <> -1 And ListBoxPlaylist.SelectedIndex <> ListBoxPlaylist.Items.Count - 1 Then
			Dim dlg As New DlgYesNoDialog("删除播放列表", "要删除播放列表 """ & ListBoxPlaylist.SelectedItem.Content & """ 吗")
			Dim r = Await DlgWindowRoot.ShowDialog(dlg)
			If r = True Then
				Dim id = GetPlaylistIdByName(ListBoxPlaylist.SelectedItem.Content)
				RemovePlaylist(id)
				ListBoxPlaylist.Items.Remove(ListBoxPlaylist.SelectedItem)
				ButtonMusic_MouseLeftButtonUp(Nothing, Nothing)
			End If
		End If
	End Sub

	Private Sub ButtonDelete_Click(sender As Object, e As RoutedEventArgs) Handles ButtonDelete.Click
		If Not ButtonMusic.IsSelected Then
			If DatSongList.SelectedItems.Count <> 0 Then
				Dim conn = Connect()
				Dim trans = conn.BeginTransaction()
				Dim cmd = conn.CreateCommand()
				cmd.Transaction = trans
				Dim removeList As New List(Of Object)
				Dim playlistId = GetPlaylistIdByName(ListBoxPlaylist.SelectedItem.Content, cmd)
				For Each itm In DatSongList.SelectedItems
					Dim itmLocal As Object = itm
					RemoveSongFromPlaylist(playlistId, itm.id, cmd)
					removeList.Add(itmLocal)
				Next
				cmd.Transaction.Commit()
				conn.Close()
				For Each o As Object In removeList
					Dim source = CType(DatSongList.ItemsSource, ObservableCollection(Of SongInfo))
					source.Remove(o)
				Next
			End If
		Else
			'Remove from whole library
			If DatSongList.SelectedIndex <> -1 Then
				Dim conn = Connect()
				Dim cmd = conn.CreateCommand()
				Dim trans = conn.BeginTransaction()
				cmd.Transaction = trans
				Dim removeList As New List(Of Object)
				For Each itm In DatSongList.SelectedItems
					If My.Computer.FileSystem.FileExists(itm.path) Then
						My.Computer.FileSystem.DeleteFile(itm.path)
					End If
					removeList.Add(itm)
					RemoveSongFromLib(itm.id, cmd)
				Next
				trans.Commit()
				conn.Close()
				For Each o As Object In removeList
					Dim source = CType(DatSongList.ItemsSource, ObservableCollection(Of SongInfo))
					source.Remove(o)
				Next
			End If
		End If
	End Sub

	Private Sub LocalMusicTabKeyHandler(sender As Object, e As KeyEventArgs) Handles DatSongList.KeyUp
		If e.Key = Key.Delete Then
			ButtonDelete_Click(Nothing, Nothing)
		End If
	End Sub

	Private Async Sub SavePlaylistSorting(sender As Object, e As EventArgs) Handles ButtonSaveSorting.Click
		If ListBoxPlaylist.SelectedIndex <> -1 Then
			Dim conn = Connect()
			Dim cmd = conn.CreateCommand()
			Dim id = GetPlaylistIdByName(ListBoxPlaylist.SelectedItem.Content)
			ClearPlaylist(id)

			Dim trans = conn.BeginTransaction()
			cmd.Transaction = trans
			Dim lst As ObservableCollection(Of SongInfo) = DatSongList.ItemsSource
			DatSongList.ItemsSource = Nothing
			Dim dlg As New dlg_progress
			DlgWindowRoot.Show(dlg)
			Await Task.Run(Sub()
							   For Each songInfo As SongInfo In lst
								   AddSongToPlaylist(id, songInfo.Id, cmd)
							   Next
						   End Sub)
			trans.Commit()
			conn.Close()
			DatSongList.ItemsSource = lst
			DlgWindowRoot.IsOpen = False
		End If
	End Sub

	Private Sub ButtonCloudMusicLogout_Click(sender As Object, e As RoutedEventArgs) Handles ButtonCloudMusicLogout.Click
		_cloudMusic = Nothing
		_cloudMusic = New CloudMusic
		_isCloudMusicLoggedIn = False
		ImageCloudMusicAvatar.Source = Nothing
		TabCloudMusic.IsSelected = False
		TabLocal.IsSelected = True
	End Sub

	Private Async Sub ListBoxCloudMusicPlaylists_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
		Handles ListBoxCloudMusicPlaylists.SelectionChanged
		If ListBoxCloudMusicPlaylists.SelectedIndex <> -1 Then
			Dim id = _cloudMusic.Playlists(ListBoxCloudMusicPlaylists.SelectedIndex)("id")
			Dim dlg As New dlg_progress
			dlg.ChangeColorTheme(New SolidColorBrush(NETEASE_RED))
			DlgCloudMusic.ShowDialog(dlg)

			Dim detail = Await Task.Run(Function()
											Dim r = _cloudMusic.GetPlaylistDetail(id)
											Return r
										End Function)
			DataGridCloudMusic.ItemsSource = detail("tracks")
			Dim img As New BitmapImage
			img.BeginInit()
			img.UriSource = New Uri(detail("coverImgUrl"))
			img.CacheOption = BitmapCacheOption.OnLoad
			img.EndInit()
			LabelCloudMusicPlaylistName.Content = detail("name")
			ImagePlaylistCover.Source = img
			DlgCloudMusic.IsOpen = False
		End If
		GC.Collect()
	End Sub

	Private Async Sub RefreshCloudMusicPlaylists(sender As Object, e As EventArgs) Handles ButtonCloudMusicRefresh.Click
		Dim dlg As New dlg_progress
		dlg.ChangeColorTheme(New SolidColorBrush(NETEASE_RED))
		DlgCloudMusic.ShowDialog(dlg)
		Await Task.Run(Sub()
						   _cloudmusic.GetPlaylists()
					   End Sub)
		ListBoxCloudMusicPlaylists.Items.Clear()
		For Each p In _cloudMusic.Playlists
			ListBoxCloudMusicPlaylists.Items.Add(
				New ListBoxItem() With {.Content = p("name"), .Padding = New Thickness(20, 10, 0, 10)})
		Next
		DlgCloudMusic.IsOpen = False
	End Sub

	Private Sub BtnSearchSong_Click(sender As Object, e As RoutedEventArgs) Handles BtnSearchSong.Click
		If TextBoxSearchLocal.Text = "" Then
			DatSongList.ItemsSource = _lstSongs
		Else
			Dim r = Searching.SearchSongs(_lstSongs, TextBoxSearchLocal.Text)
			DatSongList.ItemsSource = r
		End If
	End Sub

	Private Sub TextBoxSearchLocal_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextBoxSearchLocal.TextChanged
		If _lstSongs.Count < 1300 Then
			BtnSearchSong_Click(sender, Nothing)
		End If
	End Sub

	Private Sub TextBoxSearchLocal_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBoxSearchLocal.KeyDown
		If e.Key = Key.Enter Or Key.Return Then
			BtnSearchSong_Click(sender, Nothing)
		End If
	End Sub

	Private Async Sub ButtonSyncCurrent_Click(sender As Object, e As RoutedEventArgs) Handles ButtonSyncCurrent.Click
		Dim dlg As New dlg_progress
		dlg.ChangeColorTheme(New SolidColorBrush(NETEASE_RED))
		DlgWindowRoot.ShowDialog(dlg)
		Dim plName = ListBoxCloudMusicPlaylists.SelectedItem.Content
		Dim lst = DataGridCloudMusic.ItemsSource
		Dim failed = Await Task.Run(Function()
										Return SyncAnalyzer.SyncPlaylist(plName, lst, dlg)
									End Function)
		DlgWindowRoot.IsOpen = False
	End Sub
End Class
