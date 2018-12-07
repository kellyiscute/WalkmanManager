Imports System.Collections.ObjectModel
Imports System.Windows.Shell
Imports MaterialDesignThemes.Wpf
Imports WalkmanManager.Database
Imports ATL
Imports GongSolutions.Wpf.DragDrop

Class MainWindow
	ReadOnly NETEASE_RED = Media.Color.FromRgb(198, 47, 47)
	ReadOnly DEFAULT_COLOR = Media.Color.FromRgb(103, 58, 183)

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
		If TabRemote.IsSelected Then
			GridRemote.Visibility = Visibility.Visible
		Else
			GridRemote.Visibility = Visibility.Hidden
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
			Dim lbitm = New ListBoxItem() With {.Content = itm, .AllowDrop = True, .Padding = New Thickness(15, 7, 0, 7)}
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
		Try
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
		Catch ex As Exception
			If DlgWindowRoot.IsOpen Then
				DlgWindowRoot.IsOpen = False
				Dim dlg As New DlgMessageDialog("错误", ex.Message)
				DlgWindowRoot.ShowDialog(dlg)
			End If
		End Try
	End Sub

	Private Async Sub RefreshCloudMusicPlaylists(sender As Object, e As EventArgs) Handles ButtonCloudMusicRefresh.Click
		Dim dlg As New dlg_progress
		dlg.ChangeColorTheme(New SolidColorBrush(NETEASE_RED))
		DlgCloudMusic.ShowDialog(dlg)
		Await Task.Run(Sub()
						   _cloudMusic.GetPlaylists()
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

	Private Sub TextBoxSearchLocal_TextChanged(sender As Object, e As TextChangedEventArgs) _
		Handles TextBoxSearchLocal.TextChanged
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
		Dim rString As String
		rString = "同步 """ & plName & """ 完成" & vbTab & "失败 " & failed.Count & " 个"
		rString += vbNewLine & StrDup(rString.Length, "=")
		For Each itmCloudMusicTracks As CloudMusic.CloudMusicTracks In failed
			rString += vbNewLine & itmCloudMusicTracks.Title & " - " & itmCloudMusicTracks.Artists
		Next
		Dim dlgResult As New dlgPlaylistSyncResult(rString)
		DlgWindowRoot.DialogContent = dlgResult
	End Sub

	Private Async Sub ButtonCloudMusicSyncAll_Click(sender As Object, e As RoutedEventArgs) _
		Handles ButtonCloudMusicSyncAll.Click
		Dim dlg As New dlg_progress
		dlg.ChangeColorTheme(New SolidColorBrush(NETEASE_RED))
		DlgWindowRoot.ShowDialog(dlg)
		Dim failed = Await Task.Run(Function()
										Return SyncAnalyzer.SyncAllPlaylists(_cloudMusic, dlg)
									End Function)
		Dim dlgResult As New dlgPlaylistSyncResult(failed)
		DlgWindowRoot.DialogContent = dlgResult
	End Sub

	Private Sub ButtonRefreshDeviceList_Click(sender As Object, e As RoutedEventArgs) Handles ButtonRefreshDeviceList.Click
		ComboBoxDevices.Items.Clear()
		For Each dev In My.Computer.FileSystem.Drives
			If dev.DriveType = IO.DriveType.Removable Then
				If dev.VolumeLabel = "" Then
					ComboBoxDevices.Items.Add(dev.Name & " (没有卷标)")
				Else
					ComboBoxDevices.Items.Add(dev.Name & " (" & dev.VolumeLabel & ")")
				End If

			End If
		Next
	End Sub

	Private Sub ComboBoxDevices_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
		Handles ComboBoxDevices.SelectionChanged
		Dim dir = ComboBoxDevices.SelectedItem.ToString.Substring(0, 3)
		For Each dev In My.Computer.FileSystem.Drives
			If dev.Name.ToUpper = dir.ToUpper Then
				LabelDeviceTotalVolume.Content = "总容量：" & dev.TotalSize / 1024 ^ 2 & " MB"
				LabelDeviceFreeVolume.Content = "剩余空间：" & dev.AvailableFreeSpace / 1024 ^ 2 & " MB"
			End If
		Next
	End Sub

	Private Structure CpInfo
		Public Source As String
		Public Destination As String
		Public Lyric As String
	End Structure

	Private Async Sub ButtonRemoteSync_Click(sender As Object, e As RoutedEventArgs) Handles ButtonRemoteSync.Click
		ProgressBarSyncTotal.IsIndeterminate = True
		ButtonRemoteSync.IsEnabled = False
		Dim drivePath = ComboBoxDevices.SelectedItem.ToString.Trim.Substring(0, 2)
		Dim wmManagedPath = drivePath & "\wmManaged"
		AddSyncLog(LogType.Information, "连接数据库")
		Dim conn = Connect()

		Dim lstCopy As New List(Of CpInfo)
		Dim lstDelete As New List(Of String)
		Dim totalCopySize As Long
		Dim spaceNeeded As Long
		Dim flagCopyLrc As Boolean = Not CheckBoxSyncOptionDoNotCopyLyric.IsChecked

		AddSyncLog(LogType.Information, "检查目录结构")
		Dim lstDirLost = SyncAnalyzer.CheckDirectoryStructure(drivePath)
		If IsNothing(lstDirLost) Then
			'No analysis, copy all files
			AddSyncLog(LogType.Information, "创建文件夹： " & wmManagedPath)
			My.Computer.FileSystem.CreateDirectory(wmManagedPath)
			Dim playlists = GetPlaylists(conn)
			For Each p In playlists
				AddSyncLog(LogType.Information, "创建文件夹: " & p)
				My.Computer.FileSystem.CreateDirectory(wmManagedPath & "\" & p)
			Next
			Dim lstSongs As New List(Of SongInfo)

			Exit Sub
		End If


		'Analyze and copy
		Await Task.Run(Sub()
						   Dim playlists = GetPlaylists(conn)
						   ProgressBarSyncSub.Maximum = playlists.Count + 1
						   For Each pl In playlists
							   Dim p = GetSongsFromPlaylist(pl)
							   AddSyncLog(LogType.Information, "检查列表 " & pl & " 中的文件")
							   Dim lstSongs As New List(Of SongInfo)
							   For Each itm In p
								   lstSongs.Add(GetSongById(itm, conn))
							   Next
							   Dim n = SyncAnalyzer.CheckFiles(wmManagedPath & "\" & pl, lstSongs)
							   For Each itm In n
								   Dim cp As New CpInfo With
										   {.Source = itm.Path, .Destination = wmManagedPath & "\" & pl}
								   lstCopy.Add(cp)
							   Next
							   Dim d = SyncAnalyzer.FindDeleted(wmManagedPath & "\" & pl, lstSongs)
							   For Each itm In d
								   lstDelete.Add(itm)
							   Next
							   ProgressBarSyncSub.AddOne(Me)
						   Next
						   Dim lstNotInPlaylists = SyncAnalyzer.FindNotInPlaylists(conn)
						   Dim extra = SyncAnalyzer.CheckFiles(wmManagedPath, lstNotInPlaylists)
						   For Each itm In extra
							   Dim cp As New CpInfo With
									   {.Source = itm.Path, .Destination = wmManagedPath}
							   lstCopy.Add(cp)
						   Next
						   Dim del = SyncAnalyzer.FindDeleted(wmManagedPath & "\", lstNotInPlaylists)
						   For Each itm In del
							   lstDelete.Add(itm)
						   Next
					   End Sub)

		TextBoxOp.Text = "计算文件大小"
		ProgressBarSyncSub.Maximum = lstCopy.Count + lstDelete.Count
		ProgressBarSyncSub.Value = 0

		Await Task.Run(Sub()
						   For i = 0 To lstCopy.Count
							   If My.Computer.FileSystem.FileExists(lstCopy(i).Source) Then
								   totalCopySize += My.Computer.FileSystem.GetFileInfo(lstCopy(i).Source).Length
								   spaceNeeded += My.Computer.FileSystem.GetFileInfo(lstCopy(i).Source).Length

								   Dim fileInfo = My.Computer.FileSystem.GetFileInfo(lstCopy(i).Source)
								   If flagCopyLrc Then
									   Dim lrc = fileInfo.FullName.Replace(fileInfo.Extension, "lrt")
									   If My.Computer.FileSystem.FileExists(lrc) Then
										   lstCopy(i) = New CpInfo With
									   {.Source = lstCopy(i).Source, .Destination = lstCopy(i).Destination, .Lyric = lrc}
										   spaceNeeded += fileInfo.Length
										   totalCopySize += fileInfo.Length
									   End If
								   End If
								   ProgressBarSyncSub.AddOne(Me)
							   End If
						   Next

						   For Each dl In lstDelete
							   If My.Computer.FileSystem.FileExists(dl) Then
								   spaceNeeded -= My.Computer.FileSystem.GetFileInfo(dl).Length
								   ProgressBarSyncSub.AddOne(Me)
							   End If
						   Next
					   End Sub)

		If Not CheckBoxSyncOptionNoSpaceCheck.IsChecked Then
			If My.Computer.FileSystem.GetDriveInfo(drivePath).AvailableFreeSpace >= spaceNeeded Then
				Await DialogHost.Show("window-root", New DlgMessageDialog("同步时出现错误", "空间不足"))
				Exit Sub
			End If
		End If
		ProgressBarSyncTotal.Maximum = totalCopySize

		Await Task.Run(Sub()
						   'Create Directory
						   Dispatcher.Invoke(Sub()
												 ProgressBarSyncSub.Maximum = lstDirLost.Count
											 End Sub)
						   For Each d In lstDirLost
							   AddSyncLog(LogType.Information, "创建文件夹 " & d)
							   Try
								   My.Computer.FileSystem.CreateDirectory(wmManagedPath & "\" & d)
							   Catch ex As Exception
								   AddSyncLog(LogType.Err, ex.Message)
							   End Try
						   Next

						   'Delete Files
						   For Each d In lstDelete
							   AddSyncLog(LogType.Information, "删除文件 " & d)
							   Try
								   My.Computer.FileSystem.DeleteFile(d)
							   Catch ex As Exception
								   AddSyncLog(LogType.Warning, ex.Message)
							   End Try
						   Next

						   'Copy files
						   Dim sync As New Synchronizer
						   AddHandler sync.Update, AddressOf CopyingDetailUpdateEventHandler
						   For Each c In lstCopy
							   Try
								   AddSyncLog(LogType.Information, "复制文件：" & c.Source)
								   sync.CopyFile(c.Source, c.Destination)
								   If c.Lyric <> "" Then
									   AddSyncLog(LogType.Information, "复制文件：" & c.Lyric)
									   sync.CopyFile(c.Lyric, c.Destination)
								   End If
							   Catch ex As Exception
								   AddSyncLog(LogType.Warning, ex.Message)
							   End Try
						   Next

						   'Create Playlist
						   Dim playlists = GetPlaylists(conn)
						   For Each playlist In playlists
							   Dim lstSongs = My.Computer.FileSystem.GetFiles(wmManagedPath & "\" & playlist)
							   AddSyncLog(LogType.Information, "创建播放列表：" & playlist)
							   sync.CreatePlaylist(lstSongs, wmManagedPath)
							   ProgressBarSyncSub.AddOne()
							   ProgressBarSyncTotal.AddOne()
						   Next
					   End Sub)
	End Sub

	Private Sub CopyingDetailUpdateEventHandler(sender As Object)
		Dispatcher.Invoke(Sub()
						  End Sub)
	End Sub

	Private Enum LogType
		Information
		Warning
		Err
	End Enum

	Private Sub AddSyncLog(type As LogType, message As String, Optional reqDelegate As Boolean = True,
							Optional dispOnCpDetail As Boolean = True)
		Dim dispColor As Color

		Select Case type
			Case LogType.Information
				dispColor = Colors.Black
			Case LogType.Warning
				dispColor = Colors.DarkOrange
			Case LogType.Err
				dispColor = Colors.Red
		End Select

		If reqDelegate Then
			ListBoxSyncEventLog.Items.Add(New ListBoxItem() With {.Content =
											String.Format("[{0}][{1}]: {2}", Now.ToString, type.ToString, message),
											.Foreground = New SolidColorBrush(dispColor)})
		Else
			Me.Dispatcher.Invoke(Sub()
									 ListBoxSyncEventLog.Items.Add(New ListBoxItem() With {.Content =
																	 String.Format("[{0}][{1}]: {2}", Now.ToString, type.ToString, message),
																	 .Foreground = New SolidColorBrush(dispColor)})
									 If dispOnCpDetail Then
										 TextBoxOp.Text = message
									 End If
								 End Sub)
		End If
	End Sub
End Class
