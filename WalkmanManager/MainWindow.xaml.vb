Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Threading
Imports System.Windows.Shell
Imports ATL
Imports GongSolutions.Wpf.DragDrop
Imports LibVLCSharp.Shared
Imports LibVLCSharp.WPF
Imports MaterialDesignThemes.Wpf
Imports WalkmanManager.Database
Imports WalkmanManager.CloudMusic
Imports System.ComponentModel
Imports System.Security.Cryptography
Imports TagLib.IFD.Entries

Class MainWindow
    ReadOnly NETEASE_RED = Color.FromRgb(198, 47, 47)
    ReadOnly DEFAULT_COLOR = Color.FromRgb(103, 58, 183)

    Dim _lstSongs As ObservableCollection(Of SongInfo)
    Dim _isRightClickSelect As Boolean = False
    Dim _isCloudMusicLoggedIn As Boolean = False
    Dim _cloudMusic As New CloudMusic.CloudMusic
    Dim _syncRemoteDeviceContent As Object
    Dim _flgSyncStop As Boolean
    Dim _flgUSBRefreshPause As Boolean = False
    Dim _encryptKey As String
    Dim _toolWindowConvertNcm As DlgConvertNcm
    Dim _usbWatcher As UsbWatcher
    Dim _sbMessageQueue As New SnackbarMessageQueue
    Dim _remoteActionSyncContent As WrapPanel
    Dim _remoteActionTakeOverContent As WrapPanel
    Dim LibV As LibVLC
    Dim LibVlcMediaPlayer As MediaPlayer
    Dim _nowPlaying As SongInfo
    Dim fileChangeNotifyIgnoreCount As Integer
    Dim _searchOnType As Integer = 1300
    Dim _lyricPreLoad As Integer = 3
    Dim _flgFindRepeatRunning As Boolean = False

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

                Dim dlgLogin As New DlgCloudMusicLogin()
                Dim cmPhone = DecryptString(_encryptKey, GetSetting("CloudMusicPhone", ""))
                Dim cmPassword = DecryptString(_encryptKey, GetSetting("CloudMusicPwd", ""))
                Dim flgAutoLogin = Boolean.Parse(GetSetting("CloudMusicAutoLogin", "False"))

                dlgLogin.Phone = cmPhone
                dlgLogin.Password = cmPassword
                Dim login = True

                While True
                    If Not flgAutoLogin Or cmPhone = "" Or cmPassword = "" Then
                        login = Await DlgWindowRoot.ShowDialog(dlgLogin)
                    End If

                    If login Then
                        Dim dlgProg = New DlgProgress
                        dlgProg.ChangeColorTheme(New SolidColorBrush(NETEASE_RED))
                        DlgWindowRoot.ShowDialog(dlgProg)
                        Dim result = Await UiCloudMusicLogin(dlgLogin.Phone, dlgLogin.Password, dlgProg)
                        If result = "" Then
                            If dlgLogin.ChkRememberPwd.IsChecked Then
                                SaveSetting("CloudMusicPhone", EncryptString(_encryptKey, dlgLogin.Phone))
                                SaveSetting("CloudMusicPwd", EncryptString(_encryptKey, dlgLogin.Password))
                            End If
                            If dlgLogin.ChkAutoLogin.IsChecked Then
                                SaveSetting("CloudMusicAutoLogin", "True")
                            Else
                                SaveSetting("CloudMusicAutoLogin", "False")
                            End If

                            For Each p In _cloudMusic.Playlists
                                ListBoxCloudMusicPlaylists.Items.Add(
                                    New ListBoxItem() _
                                                                        With {.Content = p("name"),
                                                                        .Padding = New Thickness(20, 10, 0, 10)})
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
                            Dim dlgFail As New DlgMessageDialog("登录失败", result)
                            dlgFail.ChangeColorTheme(New SolidColorBrush(NETEASE_RED))
                            _isCloudMusicLoggedIn = False
                            DlgWindowRoot.IsOpen = False
                            Await DlgWindowRoot.ShowDialog(dlgFail)
                            flgAutoLogin = False
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

    ''' <summary>
    ''' UI login method, return an empty string when success, error message when failed
    ''' </summary>
    ''' <param name="dlgProg">progress dialog ref, for changing prompt</param>
    ''' <returns></returns>
    Private Async Function UiCloudMusicLogin(phone As String, pwd As String, dlgProg As DlgProgress) As Task(Of String)
        Dim result = Await Task.Run(Function()
                                        Try
                                            dlgProg.Text = "登录中"
                                            Dim loginResult = _cloudMusic.Login(phone, pwd)
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
        Dim dlg As New DlgProgress
        DialogHost.Show(dlg, "window-root")
        Dim upd As New DbUpdater
        Dim songDir = GetSetting("song_dir")
        _searchOnType = GetSetting("searchOnType", 1300)
        _lyricPreLoad = GetSetting("lyricPreLoad", 3)
        If IsNothing(songDir) Then
            My.Computer.FileSystem.CreateDirectory("SongLib")
            SaveSetting("song_dir", "SongLib")
            songDir = "SongLib"
        End If
        _encryptKey = GetSetting("enc_key")

        PageSwitcher(Nothing, Nothing)

        If Not My.Computer.FileSystem.DirectoryExists(songDir) Then
            My.Computer.FileSystem.CreateDirectory(songDir)
        End If

        Await Task.Run(Sub()
                           Core.Initialize()
                           LibV = New LibVLC()
                           LibVlcMediaPlayer = New MediaPlayer(LibV)
                           AddHandler LibVlcMediaPlayer.TimeChanged, AddressOf MediaPlayer_TimeChanged
                           AddHandler LibVlcMediaPlayer.LengthChanged, AddressOf MediaPlayer_LengthChanged
                       End Sub)

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
        _syncRemoteDeviceContent = ButtonRemoteAction.Content
        'Init USB Watcher
        _usbWatcher = New UsbWatcher
        AddHandler _usbWatcher.Unplugged, AddressOf Device_Unplugged
        AddHandler _usbWatcher.PluggedIn, AddressOf Device_PluggedIn
        _usbWatcher.Start()
        'Init Device List
        ButtonRefreshDeviceList_Click(Nothing, Nothing)
        'Monitor File changes
        Dim w As New FileSystemWatcher(GetSetting("song_dir"))
        AddHandler w.Created, AddressOf File_Created
        AddHandler w.Deleted, AddressOf File_Deleted
        w.EnableRaisingEvents = True
        'Set Message Queue
        SnackbarInfoMessage.MessageQueue = _sbMessageQueue
        'Set Wrap panel content
        _remoteActionTakeOverContent = ButtonRemoteChangeAction.Content
        _remoteActionSyncContent = ButtonRemoteAction.Content
    End Sub

    Private Sub File_Created(sender As Object, e As FileSystemEventArgs)
        If DbUpdater.CheckExtention(e.FullPath) Then
            'if is audio, add to db
            Dim FullPath = My.Computer.FileSystem.GetFileInfo(e.FullPath).FullName
            Dim t As New Track(FullPath)
            Dim id = AddSong(t.Title, t.Artist, FullPath)
            'add to list
            Dispatcher.Invoke(
                Sub() _
                                 _lstSongs.Add(New SongInfo _
                                                  With {.Title = t.Title, .Artists = t.Artist, .Path = FullPath,
                                                  .Id = id}))
            t = Nothing
            If fileChangeNotifyIgnoreCount = 0 Then
                _sbMessageQueue.Enqueue($"新文件：{FullPath}")
            Else
                fileChangeNotifyIgnoreCount -= 1
            End If
        End If
    End Sub

    Private Sub File_Deleted(sender As Object, e As FileSystemEventArgs)
        If DbUpdater.CheckExtention(e.FullPath) Then
            'if is audio, check if in db
            Dim id = GetSongId(e.FullPath)
            Dim FullPath = My.Computer.FileSystem.GetFileInfo(e.FullPath).FullName
            If Not IsNothing(id) Then
                'remove from db
                RemoveSongFromLib(id)
                'remove from list
                Dispatcher.Invoke(
                    Sub() _lstSongs.Remove((From itm In _lstSongs Where itm.Path = FullPath Select itm)(0)))
            End If
            If fileChangeNotifyIgnoreCount = 0 Then
                _sbMessageQueue.Enqueue($"文件删除：{FullPath}")
            Else
                fileChangeNotifyIgnoreCount -= 1
            End If
        End If
    End Sub

    Private Sub Device_Unplugged(sender As Object, d As DriveInfoMem)
        Dim selDevName, selDevLabel As String
        selDevName = Dispatcher.Invoke(Of String)(Function()
                                                      Try
                                                          Return Mid(ComboBoxDevices.SelectedValue, 1, 3)
                                                      Catch
                                                          Return ""
                                                      End Try
                                                  End Function)
        selDevLabel = Dispatcher.Invoke(Of String)(Function()
                                                       Try
                                                           Return ComboBoxDevices.SelectedValue.Substring(5, ComboBoxDevices.SelectedValue.length - 6)
                                                       Catch
                                                           Return ""
                                                       End Try
                                                   End Function)

        For Each itm In ComboBoxDevices.Items
            If itm = $"{d.Name} ({d.VolumeLabel})" Then
                Dispatcher.Invoke(Sub() ComboBoxDevices.Items.Remove(itm))
                Exit For
            End If
        Next
        If selDevLabel = d.VolumeLabel And selDevName = d.Name Then
            Dispatcher.Invoke(Sub()
                                  If ComboBoxDevices.Items.Count > 0 Then
                                      ComboBoxDevices.SelectedIndex = 0
                                  Else
                                      ComboBoxDevices.SelectedIndex = -1
                                  End If
                              End Sub)
        End If

        _sbMessageQueue.Enqueue($"设备拔出：{d.Name} ({d.VolumeLabel})")
    End Sub

    Private Sub Device_PluggedIn(sender As Object, d As DriveInfoMem)
        Dispatcher.Invoke(Sub() ComboBoxDevices.Items.Add($"{d.Name} ({d.VolumeLabel})"))
        _sbMessageQueue.Enqueue($"设备插入：{d.Name} ({d.VolumeLabel})")
    End Sub

    Private Async Sub RefreshPlaylists()
        Dim lstPlaylist = Await Task.Run(Function()
                                             Dim r = GetPlaylists()
                                             Return r
                                         End Function)
        ListBoxPlaylist.Items.Clear()

        For Each itm In lstPlaylist
            Dim lbitm = New ListBoxItem() _
                    With {.Content = itm, .AllowDrop = True, .Padding = New Thickness(15, 7, 0, 7)}
            AddHandler lbitm.Drop, AddressOf ListBoxItem_Drop
            ListBoxPlaylist.Items.Add(lbitm)
        Next
    End Sub

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        WindowChrome.SetWindowChrome(Me,
                                     New WindowChrome() _
                                        With {.GlassFrameThickness = New Thickness(1),
                                        .UseAeroCaptionButtons = False, .ResizeBorderThickness = New Thickness(5),
                                        .CornerRadius = New CornerRadius(10),
                                        .CaptionHeight = 34})
        'Init ActionButton Content
        _remoteActionSyncContent = New WrapPanel
        With _remoteActionSyncContent
            .Children.Add(New PackIcon With {.Kind = PackIconKind.Sync})
            .Children.Add(New TextBlock With {.Text = "同步", .Height = 29, .Width = 29})
        End With

        _remoteActionTakeOverContent = New WrapPanel
        With _remoteActionTakeOverContent
            .Children.Add(New PackIcon With {.Kind = PackIconKind.Undo})
            .Children.Add(New TextBlock With {.Text = "接管", .Height = 29, .Width = 29})
        End With

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

    'Playlist Selected
    Private Async Sub ListItem_SelectionChange(sender As ListBox, e As EventArgs) _
        Handles ListBoxPlaylist.SelectionChanged
        If sender.SelectedIndex <> -1 And Not _isRightClickSelect Then

            'if not editing
            If ListBoxPlaylist.SelectedItem.Content.GetType = GetType(String) Then
                ButtonMusic.IsSelected = False
                DatSongList.CanUserSortColumns = False
                ButtonSaveSorting.Visibility = Visibility.Visible
                PanelSearchLocalMusic.Visibility = Visibility.Collapsed
                Dim dlg As New DlgProgress
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
                ListBoxPlaylist.Focus()

            End If
        Else
            DatSongList.CanUserSortColumns = True
        End If
        _isRightClickSelect = False
    End Sub

    Private Sub BlockRightClickSelection(sender As Object, e As MouseButtonEventArgs)
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

    Private Sub ButtonMusic_Selected(sender As Object, e As RoutedEventArgs) Handles ButtonMusic.Selected
        On Error Resume Next
        ListBoxPlaylist.SelectedIndex = -1
        DatSongList.CanUserSortColumns = True
    End Sub

    Private Async Sub DeletePlaylist_Click(sender As Object, e As EventArgs) Handles MenuDeletePlaylist.Click
        If ListBoxPlaylist.SelectedIndex <> -1 Then _
            'And ListBoxPlaylist.SelectedIndex <> ListBoxPlaylist.Items.Count - 1
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
                    _lstSongs.Remove(o)
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
                    _lstSongs.Remove(o)
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
            Dim dlg As New DlgProgress
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

    Private Sub ButtonCloudMusicLogout_Click(sender As Object, e As RoutedEventArgs) _
        Handles ButtonCloudMusicLogout.Click
        SaveSetting("CloudMusicAutoLogin", "False")
        _cloudMusic = Nothing
        _cloudMusic = New CloudMusic.CloudMusic
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
                Dim dlg As New DlgProgress
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
        Dim dlg As New DlgProgress
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
        If _lstSongs.Count < _searchOnType Then
            BtnSearchSong_Click(sender, Nothing)
        End If
    End Sub

    Private Sub TextBoxSearchLocal_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBoxSearchLocal.KeyDown
        If e.Key = Key.Enter Or Key.Return Then
            BtnSearchSong_Click(sender, Nothing)
        End If
    End Sub

    Private Async Sub ButtonSyncCurrent_Click(sender As Object, e As RoutedEventArgs) Handles ButtonSyncCurrent.Click
        Dim dlg As New DlgProgress
        dlg.ChangeColorTheme(New SolidColorBrush(NETEASE_RED))
        DlgWindowRoot.ShowDialog(dlg)
        Dim plName = ListBoxCloudMusicPlaylists.SelectedItem.Content
        Dim lst = DataGridCloudMusic.ItemsSource
        Dim failed = Await Task.Run(Function()
                                        Return SyncAnalyzer.SyncPlaylist(plName, lst, dlg)
                                    End Function)
        '        Dim rString As String
        '        rString = "同步 """ & plName & """ 完成" & vbTab & "失败 " & failed.Count & " 个"
        '        rString += vbNewLine & StrDup(rString.Length, "=")
        '        For Each itmCloudMusicTracks As CloudMusic.CloudMusic.CloudMusicTracks In failed
        '            rString += vbNewLine & itmCloudMusicTracks.Title & " - " & itmCloudMusicTracks.Artists
        '        Next
        '        Dim dlgResult As New dlgPlaylistSyncResult(rString)
        '        DlgWindowRoot.DialogContent = dlgResult
        If failed.Count = 0 Then
            Dim dlgMsg As New DlgMessageDialog("同步当前歌单", "同步成功")
            DlgWindowRoot.IsOpen = False
            Await DlgWindowRoot.ShowDialog(dlgMsg)
            Exit Sub
        End If

        Dim dlgSyncResult As New DlgCloudMusicPlaylistSyncResult(_lstSongs, failed, GetPlaylistIdByName(plName))
        DlgWindowRoot.DialogContent = dlgSyncResult
    End Sub

    Private Async Sub ButtonCloudMusicSyncAll_Click(sender As Object, e As RoutedEventArgs) _
        Handles ButtonCloudMusicSyncAll.Click
        Dim dlg As New DlgProgress
        dlg.ChangeColorTheme(New SolidColorBrush(NETEASE_RED))
        DlgWindowRoot.ShowDialog(dlg)
        Dim failed = Await Task.Run(Function()
                                        Return SyncAnalyzer.SyncAllPlaylists(_cloudMusic, dlg)
                                    End Function)
        Dim dlgResult As New dlgPlaylistSyncResult(failed)
        DlgWindowRoot.DialogContent = dlgResult
    End Sub

    Private Sub ButtonRefreshDeviceList_Click(sender As Object, e As RoutedEventArgs) _
        Handles ButtonRefreshDeviceList.Click
        Dispatcher.Invoke(Sub() ComboBoxDevices.Items.Clear())
        For Each dev In My.Computer.FileSystem.Drives
            If dev.DriveType = IO.DriveType.Removable And dev.IsReady Then
                If dev.VolumeLabel = "" Then
                    Dispatcher.Invoke(Sub() ComboBoxDevices.Items.Add(dev.Name & " (没有卷标)"))
                Else
                    Dispatcher.Invoke(Sub() ComboBoxDevices.Items.Add(dev.Name & " (" & dev.VolumeLabel & ")"))
                End If

            End If
        Next

        If ComboBoxDevices.Items.Count > 0 Then
            Dispatcher.Invoke(Sub() ComboBoxDevices.SelectedIndex = 0)
        End If
    End Sub

    Private Sub ComboBoxDevices_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
        Handles ComboBoxDevices.SelectionChanged
        If sender.SelectedIndex = -1 Then
            Exit Sub
        End If
        Dim dir = ComboBoxDevices.SelectedItem.ToString.Substring(0, 3)
        For Each dev In My.Computer.FileSystem.Drives
            If dev.Name.ToUpper = dir.ToUpper Then
                LabelDeviceTotalVolume.Content = "总容量：" & dev.TotalSize / 1024 ^ 2 & " MB"
                LabelDeviceFreeVolume.Content = "剩余空间：" & dev.AvailableFreeSpace / 1024 ^ 2 & " MB"
            End If
        Next
    End Sub

    Private Sub ButtonRemoteSync_Click(sender As Object, e As RoutedEventArgs) Handles ButtonRemoteAction.Click
        _syncRemoteDeviceContent = ButtonRemoteAction.Content
        If ComboBoxDevices.SelectedIndex <> -1 Then
            If ButtonRemoteAction.Content Is _remoteActionSyncContent Then
                _flgSyncStop = False
                _usbWatcher.FlgPause = True
                ButtonRemoteChangeAction.IsEnabled = False
                StartSync()
            ElseIf ButtonRemoteAction.Content Is _remoteActionTakeOverContent Then
                _flgSyncStop = False
                _usbWatcher.FlgPause = True
                ButtonRemoteChangeAction.IsEnabled = False
                ButtonRemoteAction.Content = "取消"
                StartTakeOver()
            ElseIf ButtonRemoteAction.Content = "取消" Then
                ButtonRemoteAction.Content = "正在取消"
                ButtonRemoteAction.IsEnabled = False
                _flgSyncStop = True
            End If
        Else
            SnackbarInfoMessage.MessageQueue.Enqueue("未选择设备")
        End If
    End Sub

    Private Structure CpInfo
        Public Source As String
        Public Destination As String
        Public Lyric As String
    End Structure

    Private Async Sub StartSync()

        ProgressBarSyncTotal.IsIndeterminate = True
        ButtonRemoteAction.Content = "取消"
        Dim drivePath = ComboBoxDevices.SelectedItem.ToString.Trim.Substring(0, 2)
        Dim wmManagedPath = drivePath & "\MUSIC"
        AddSyncLog(LogType.Information, "连接数据库")
        '        Dim conn = Connect()

        If Not My.Computer.FileSystem.DirectoryExists(wmManagedPath) Then
            My.Computer.FileSystem.CreateDirectory(wmManagedPath)
        End If

        Dim lstCopy As New List(Of CpInfo)
        Dim lstDelete As New List(Of String)
        Dim totalCopySize As Long
        Dim spaceNeeded As Long
        Dim flagCopyLrc As Boolean = Not CheckBoxSyncOptionDoNotCopyLyric.IsChecked
        Dim lstSongs = GetSongs()
        Dim progressSubscriber() As Long = {0, 0}
        Dim progressUpdateThread As Thread
        Dim flgProgressUpdateThreadStop = False
        Dim flgProgressUpdateThreadPause = False
        Dim flgHashCheck = CheckBoxSyncOptionHashCheck.IsChecked

        ProgressBarSyncSub.Maximum = 2
        ProgressBarSyncSub.IsIndeterminate = False
        ProgressBarSyncSub.Value = 0

        If IsNothing(lstSongs) Then
            GoTo Complete
        End If

        progressUpdateThread = New Thread(Sub()
                                              Do
                                                  Dispatcher.Invoke(Sub()
                                                                        If flgProgressUpdateThreadPause Then
                                                                            Exit Sub
                                                                        End If
                                                                        If progressSubscriber(1) <> 0 Then
                                                                            ProgressBarSyncSub.IsIndeterminate = False
                                                                            ProgressBarSyncSub.Maximum = progressSubscriber(1)
                                                                            ProgressBarSyncSub.Value = progressSubscriber(0)
                                                                        Else
                                                                            ProgressBarSyncSub.IsIndeterminate = True
                                                                        End If
                                                                    End Sub)
                                                  Thread.Sleep(300)
                                                  If flgProgressUpdateThreadStop Then
                                                      Exit Do
                                                  End If
                                              Loop
                                          End Sub)
        progressUpdateThread.IsBackground = True
        progressUpdateThread.Start()

        AddSyncLog(LogType.Information, "查找删除项目", False)
        lstDelete = Await Task.Run(Function()
                                       Return SyncAnalyzer.FindDeleted(wmManagedPath, lstSongs, progressSubscriber, _flgSyncStop)
                                   End Function)
        ProgressBarSyncSub.AddOne()
        If _flgSyncStop Then
            GoTo Complete
        End If

        AddSyncLog(LogType.Information, "发现需要删除的项目：" & lstDelete.Count, False)
        AddSyncLog(LogType.Information, "查找需要复制/覆盖的项目", False)
        Dim lstChanged = Await Task.Run(Function()
                                            Return _
                                           SyncAnalyzer.FindChangedFiles(wmManagedPath, lstSongs, flgHashCheck,
                                                                         progressSubscriber, _flgSyncStop)
                                        End Function)
        If _flgSyncStop Then
            GoTo Complete
        End If
        AddSyncLog(LogType.Information, "发现需要复制/覆盖的项目：" & lstChanged.Count, False)
        ProgressBarSyncSub.Value = 0

        AddSyncLog(LogType.Information, "正在计算所需空间...", False)
        ProgressBarSyncSub.Maximum = lstChanged.Count + lstDelete.Count

        ProgressBarSyncSub.IsIndeterminate = False
        flgProgressUpdateThreadStop = True
        Await Task.Run(Sub()
                           For Each itm In lstChanged
                               If My.Computer.FileSystem.FileExists(itm) Then
                                   Dim fInfo = My.Computer.FileSystem.GetFileInfo(itm)
                                   totalCopySize += fInfo.Length
                                   Dim sCopy As New CpInfo
                                   sCopy.Source = itm
                                   sCopy.Destination = wmManagedPath & "\" & My.Computer.FileSystem.GetFileInfo(itm).Name
                                   If _
                          flagCopyLrc And
                          My.Computer.FileSystem.FileExists(fInfo.DirectoryName & "\" & fInfo.NameWithoutExtention & ".lrc") Then
                                       sCopy.Lyric = fInfo.DirectoryName & "\" & fInfo.NameWithoutExtention & ".lrc"
                                       totalCopySize +=
                          My.Computer.FileSystem.GetFileInfo(fInfo.DirectoryName & "\" & fInfo.NameWithoutExtention & ".lrc").Length
                                   End If

                                   lstCopy.Add(sCopy)
                               End If
                               ProgressBarSyncSub.AddOne(Me)

                               If _flgSyncStop Then
                                   Exit Sub
                               End If
                           Next
                           spaceNeeded = totalCopySize

                           For Each itm In lstDelete
                               spaceNeeded -= My.Computer.FileSystem.GetFileInfo(itm).Length
                               ProgressBarSyncSub.AddOne(Me)

                               If _flgSyncStop Then
                                   Exit Sub
                               End If
                           Next
                       End Sub)

        If _
            spaceNeeded > My.Computer.FileSystem.GetDriveInfo(drivePath).TotalFreeSpace And
            Not CheckBoxSyncOptionNoSpaceCheck.IsChecked Then

            Dim errorDlg As New DlgMessageDialog("同步失败", "磁盘空间不足")
            Await DialogHost.Show(errorDlg, "window-root")
            ProgressBarSyncTotal.Value = 0
            ProgressBarSyncTotal.Maximum = 0
            ProgressBarSyncTotal.IsIndeterminate = False
            ProgressBarSyncSub.Value = 0
            ProgressBarSyncSub.Maximum = 0
            ProgressBarSyncSub.IsIndeterminate = False
            ProgressBarSyncSub.Value = 0
            ProgressBarSyncTotal.Value = 0
            ButtonRemoteAction.Content = _syncRemoteDeviceContent
            Return
        End If

        ProgressBarSyncTotal.Maximum = lstCopy.Count + lstDelete.Count
        ProgressBarSyncTotal.IsIndeterminate = False

        Dim cp As New Synchronizer(GetSetting("chunkSize", 1024))
        AddHandler cp.Update, AddressOf CopyingDetailUpdateEventHandler

        Await Task.Run(Sub()
                           For Each itm In lstDelete
                               AddSyncLog(LogType.Information, "删除文件：" & itm)
                               Try
                                   My.Computer.FileSystem.DeleteFile(itm)
                               Catch ex As Exception
                                   AddSyncLog(LogType.Err, "删除文件时出现错误：" & ex.Message)
                                   Exit Sub
                               End Try
                               ProgressBarSyncTotal.AddOne(Me)

                               If _flgSyncStop Then
                                   Exit Sub
                               End If
                           Next

                           For Each itm In lstCopy
                               Try
                                   AddSyncLog(LogType.Information, "写入：" & itm.Destination)
                                   cp.CopyFile(itm.Source, itm.Destination)
                                   If itm.Lyric <> "" Then
                                       AddSyncLog(LogType.Information, "写入：" & SyncAnalyzer.ChangePath(itm.Lyric, wmManagedPath))
                                       cp.CopyFile(itm.Lyric, SyncAnalyzer.ChangePath(itm.Lyric, wmManagedPath))
                                   End If
                               Catch ex As Exception
                                   AddSyncLog(LogType.Err, "写入文件时出现错误：" & ex.Message)
                               End Try
                               ProgressBarSyncTotal.AddOne(Me)

                               If _flgSyncStop Then
                                   Exit Sub
                               End If
                           Next
                       End Sub)

        ' Write playlist files
        Await Task.Run(Sub()
                           Dim lstPlaylists = GetPlaylists()
                           For Each p In lstPlaylists
                               Try
                                   AddSyncLog(LogType.Information, "写入：" & wmManagedPath & "\" & p & ".m3u")
                                   Dim playlistFile = My.Computer.FileSystem.OpenTextFileWriter(wmManagedPath & "\" & p & ".m3u", False,
                                                                                 Text.Encoding.UTF8)
                                   For Each s In GetSongsFromPlaylist(p)
                                       Dim sInfo = GetSongById(s)
                                       playlistFile.WriteLine(My.Computer.FileSystem.GetFileInfo(sInfo.Path).Name)
                                       playlistFile.Flush()
                                   Next
                                   playlistFile.Close()
                               Catch ex As Exception
                                   AddSyncLog(LogType.Err, ex.Message)
                               End Try
                           Next
                       End Sub)

Complete:
        ProgressBarSyncSub.Value = 0
        ProgressBarSyncSub.IsIndeterminate = False
        ProgressBarSyncTotal.Value = 0
        ButtonRemoteAction.Content = _syncRemoteDeviceContent
        AddSyncLog(LogType.Information, "同步完成")
        Dim msgDlg As New DlgMessageDialog("", "同步完成")
        If Not IsNothing(progressUpdateThread) Then
            If progressUpdateThread.IsAlive Then
                progressUpdateThread.Abort()
            End If
        End If
        Await DlgWindowRoot.ShowDialog(msgDlg)
        ButtonRemoteAction.IsEnabled = True
        ButtonRemoteChangeAction.IsEnabled = True
        '        Await Task.Run(Sub() Thread.Sleep(1000))
        _usbWatcher.FlgPause = False
    End Sub

    Private Async Sub StartTakeOver()
        Dim lstCopyInfo As New List(Of CpInfo)
        Dim lstCopy As New List(Of String)
        Dim spaceNeeded As Long
        Dim songDir = GetSetting("song_dir")
        Dim progressSubscriber() As Long = {0, 0}
        Dim flgProgressUpdaterPause As Boolean = False
        Dim progressUpdateThread As Thread

        ProgressBarSyncSub.IsIndeterminate = True

        progressUpdateThread = New Thread(Sub()
                                              Do
                                                  Dispatcher.Invoke(Sub()
                                                                        If flgProgressUpdaterPause Then
                                                                            Exit Sub
                                                                        End If
                                                                        If progressSubscriber(1) <> 0 Then
                                                                            ProgressBarSyncSub.IsIndeterminate = False
                                                                            ProgressBarSyncSub.Maximum = progressSubscriber(1)
                                                                            ProgressBarSyncSub.Value = progressSubscriber(0)
                                                                        Else
                                                                            ProgressBarSyncSub.IsIndeterminate = True
                                                                        End If
                                                                    End Sub)
                                                  Thread.Sleep(300)
                                                  If flgProgressUpdaterPause Then
                                                      Exit Do
                                                  End If
                                              Loop
                                          End Sub)
        progressUpdateThread.IsBackground = True
        progressUpdateThread.Start()

        Dim drivePath = ComboBoxDevices.SelectedItem.ToString.Trim.Substring(0, 2)
        Dim wmManagedPath = drivePath & "\MUSIC"

        'Find files need to be copied
        AddSyncLog(LogType.Information, "查找需要复制/覆盖的项目", False)
        lstCopy = Await Task.Run(Function()
                                     Return SyncAnalyzer.FindDeleted(wmManagedPath, _lstSongs, progressSubscriber, _flgSyncStop)
                                 End Function)
        If _flgSyncStop Then
            GoTo Complete
        End If
        AddSyncLog(LogType.Information, "发现需要复制/覆盖的项目：" & lstCopy.Count, False)
        fileChangeNotifyIgnoreCount = lstCopy.Count

        Dim c As Integer
        AddSyncLog(LogType.Information, "正在计算所需空间", False)
        Await Task.Run(Sub()
                           While Not _flgSyncStop And c < lstCopy.Count
                               Dim fInfo = My.Computer.FileSystem.GetFileInfo(lstCopy(c))
                               Dim itmCopyInfo As New CpInfo
                               spaceNeeded += fInfo.Length
                               itmCopyInfo.Source = lstCopy(c)
                               itmCopyInfo.Destination = songDir & "\" & fInfo.Name

                               If My.Computer.FileSystem.FileExists(fInfo.DirectoryName & "\" & fInfo.NameWithoutExtention & ".lrc") Then
                                   itmCopyInfo.Lyric = fInfo.DirectoryName & "\" & fInfo.Name & ".lrc"
                                   spaceNeeded +=
                    My.Computer.FileSystem.GetFileInfo(fInfo.DirectoryName & "\" & fInfo.NameWithoutExtention & ".lrc").Length
                               End If
                               lstCopyInfo.Add(itmCopyInfo)

                               c += 1
                           End While
                       End Sub)
        If _flgSyncStop Then
            GoTo Complete
        End If

        Dim cp As New Synchronizer(GetSetting("chunkSize", 1024))
        AddHandler cp.Update, AddressOf CopyingDetailUpdateEventHandler

        ProgressBarSyncSub.IsIndeterminate = False
        ProgressBarSyncSub.Value = 0
        ProgressBarSyncTotal.Value = 0
        ProgressBarSyncTotal.Maximum = lstCopy.Count

        Await Task.Run(Sub()
                           For Each itm In lstCopyInfo
                               Try
                                   AddSyncLog(LogType.Information, "写入：" & itm.Destination)
                                   cp.CopyFile(itm.Source, itm.Destination)
                                   If itm.Lyric <> "" Then
                                       AddSyncLog(LogType.Information, "写入：" & SyncAnalyzer.ChangePath(itm.Lyric, songDir))
                                       cp.CopyFile(itm.Lyric, SyncAnalyzer.ChangePath(itm.Lyric, songDir))
                                   End If
                               Catch ex As Exception
                                   AddSyncLog(LogType.Err, "写入文件时出现错误：" & ex.Message)
                                   Exit Sub
                               End Try
                               ProgressBarSyncTotal.AddOne(Me)

                               If _flgSyncStop Then
                                   Exit Sub
                               End If
                           Next
                       End Sub)

Complete:
        'Stop progressUpdate Thread
        If Not IsNothing(progressUpdateThread) Then
            If progressUpdateThread.IsAlive Then
                progressUpdateThread.Abort()
            End If
        End If
        ProgressBarSyncSub.Value = 0
        ProgressBarSyncSub.IsIndeterminate = False
        ProgressBarSyncTotal.Value = 0
        ButtonRemoteAction.Content = _syncRemoteDeviceContent
        AddSyncLog(LogType.Information, "同步完成")
        Dim msgDlg As New DlgMessageDialog("", "同步完成")
        Await DlgWindowRoot.ShowDialog(msgDlg)
        ButtonRemoteAction.IsEnabled = True
        ButtonRemoteChangeAction.IsEnabled = True
        _usbWatcher.FlgPause = False
    End Sub

    Private Sub CopyingDetailUpdateEventHandler(sender As Synchronizer)
        Dispatcher.Invoke(Sub()
                              ProgressBarSyncSub.Maximum = sender.TotalLength
                              ProgressBarSyncSub.Value = sender.CopiedLength
                              TextBoxTotal.Text = sender.TotalLength
                              TextBoxComplete.Text = sender.CopiedLength
                              TextBoxBlock.Text = sender.ChunkSize
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

        If Not reqDelegate Then
            ListBoxSyncEventLog.Items.Add(New ListBoxItem() With {.Content =
                                             String.Format("[{0}][{1}]: {2}", Now.ToString, type.ToString, message),
                                             .Foreground = New SolidColorBrush(dispColor)})
            If dispOnCpDetail Then
                TextBoxOp.Text = message
            End If
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

    Private Sub MenuRenamePlaylist_Click(sender As Object, e As RoutedEventArgs) Handles MenuRenamePlaylist.Click
        If ListBoxPlaylist.SelectedIndex <> -1 Then
            If ListBoxPlaylist.SelectedItem.Content.GetType = GetType(String) Then
                Dim textBoxRenamePlaylist = New TextBox _
                        With {.Tag = New Object() {ListBoxPlaylist.SelectedItem, ListBoxPlaylist.SelectedItem.Content},
                        .Width = ListBoxPlaylist.Width - 20,
                        .Text = ListBoxPlaylist.SelectedItem.Content}
                AddHandler textBoxRenamePlaylist.KeyDown, AddressOf TextBoxRenamePlaylist_KeyDown
                ListBoxPlaylist.SelectedItem.Content = textBoxRenamePlaylist
                textBoxRenamePlaylist.Focus()
            End If
        End If
    End Sub

    Private Async Sub TextBoxRenamePlaylist_KeyDown(sender As TextBox, e As KeyEventArgs)
        If e.Key = Key.Escape Then
            sender.Tag(0).Content = sender.Tag(1)
            sender = Nothing
        ElseIf e.Key = Key.Enter Or e.Key = Key.Return Then
            If CheckPlaylistNameAvailability(sender.Text) Then
                RenamePlaylist(sender.Tag(1), sender.Text)
                sender.Tag(0).Content = sender.Text
                sender = Nothing
            Else
                Dim dlg As New DlgMessageDialog("重命名歌单失败", "有重名的歌单")
                Await DlgWindowRoot.ShowDialog(dlg)
                sender.Tag(0).Content = sender.Tag(1)
                sender = Nothing
            End If
        End If
    End Sub

    Private Sub ListBoxPlaylist_KeyDown(sender As Object, e As KeyEventArgs) Handles ListBoxPlaylist.KeyDown
        If e.Key = Key.F2 Then
            MenuRenamePlaylist_Click(sender, Nothing)
        End If
    End Sub

    Private Async Sub BtnSettings_Click(sender As Object, e As RoutedEventArgs) Handles BtnSettings.Click
        Dim dlg = New DlgSettings
        dlg.Init()
        Try
            Await DlgWindowRoot.ShowDialog(dlg)
            If dlg.FlgForceRestart Then
                Process.Start(Application.ResourceAssembly.Location)
                Environment.Exit(0)
            End If
            'reload settings
            _searchOnType = GetSetting("searchOnType", 1300)
            _lyricPreLoad = GetSetting("lyricPreLoad", 3)
        Catch
        End Try
    End Sub

    Private Sub BtnConvertNcm_Click(sender As Object, e As RoutedEventArgs) Handles BtnConvertNcm.Click
        If IsNothing(_toolWindowConvertNcm) Then
            _toolWindowConvertNcm = New DlgConvertNcm(GetSetting("song_dir"))
            _toolWindowConvertNcm.HorizontalAlignment = HorizontalAlignment.Center
            _toolWindowConvertNcm.VerticalAlignment = VerticalAlignment.Center
            AddHandler _toolWindowConvertNcm.Close, AddressOf dlgConvertNcm_Close
            AddHandler _toolWindowConvertNcm.Minimize, AddressOf dlgConvertNcm_Minimize

            GridRoot.Children.Add(_toolWindowConvertNcm)
            _toolWindowConvertNcm.Init()
        Else
            _toolWindowConvertNcm.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub dlgConvertNcm_Minimize()
        _toolWindowConvertNcm.Visibility = Visibility.Collapsed
    End Sub

    Private Sub dlgConvertNcm_Close()
        GridRoot.Children.Remove(_toolWindowConvertNcm)
        _toolWindowConvertNcm.Dispose()
        _toolWindowConvertNcm = Nothing
    End Sub

    Private Async Sub MenuImportPlaylist_Click(sender As Object, e As RoutedEventArgs) Handles MenuImportPlaylist.Click
        Dim dlgOpen As New System.Windows.Forms.OpenFileDialog
        dlgOpen.Filter = "兼容的播放列表 |*.m3u; *.m3u8"
        dlgOpen.Multiselect = False
        Dim r = dlgOpen.ShowDialog()
        If r = Forms.DialogResult.OK And My.Computer.FileSystem.FileExists(dlgOpen.FileName) Then
            Dim files As New List(Of String)
            Dim playlistName As String
            playlistName = My.Computer.FileSystem.GetFileInfo(dlgOpen.FileName).Name
            playlistName = playlistName.Replace(My.Computer.FileSystem.GetFileInfo(dlgOpen.FileName).Extension, "")

            Dim reader = My.Computer.FileSystem.OpenTextFileReader(dlgOpen.FileName, Text.Encoding.UTF8)
            Do Until reader.EndOfStream
                Dim t = reader.ReadLine
                If Not t.StartsWith("#") Then
                    files.Add(t)
                End If
            Loop

            'Check path
            For i = 0 To files.Count - 1
                files(i) = IIf(My.Computer.FileSystem.FileExists(files(i)), files(i),
                               My.Computer.FileSystem.CombinePath(
                                   My.Computer.FileSystem.GetFileInfo(dlgOpen.FileName).DirectoryName,
                                   files(i)))
                If Not My.Computer.FileSystem.FileExists(files(i)) Then
                    files(i) = ""
                End If
            Next

            'Copy to library & add to database
            Dim libDir = GetSetting("song_dir")
            Dim localDir As String
            Dim dlgWait As New DlgProgress()
            DlgWindowRoot.ShowDialog(dlgWait)
            dlgWait.Text = "0/" & files.Count
            Await Task.Run(Sub()
                               'Add playlist, if there is one with the same name, merge
                               If CheckPlaylistNameAvailability(playlistName) Then
                                   'Create
                                   AddPlaylist(playlistName)
                               End If
                               Dim playlistId = GetPlaylistIdByName(playlistName)

                               For i = 0 To files.Count - 1
                                   Dim audioInfo As New Track(files(i))
                                   Dim songId As Integer
                                   If SongExists(audioInfo.Title, audioInfo.Artist) <> "" Then
                                       'if there is same song, dont copy
                                       songId = FindSong(audioInfo.Title, audioInfo.Artist)(0) 'only take the first one
                                   Else
                                       'copy and add to db
                                       localDir = My.Computer.FileSystem.CombinePath(libDir,
                                                                      My.Computer.FileSystem.GetFileInfo(files(i)).Name)
                                       My.Computer.FileSystem.CopyFile(files(i), localDir, True)
                                       songId = AddSong(audioInfo.Title, audioInfo.Artist, localDir)
                                       dlgWait.Text = (i + 1) & "/" & files.Count
                                   End If
                                   'add to playlist
                                   AddSongToPlaylist(playlistId, songId)

                                   audioInfo = Nothing
                               Next
                               dlgWait.Text = "更新列表..."
                               _lstSongs = GetSongs()
                               'DatSongList.ItemsSource = _lstSongs
                               Dispatcher.Invoke(Sub() RefreshPlaylists())
                           End Sub)
            DlgWindowRoot.IsOpen = False
        End If
    End Sub

    Private Async Sub DatSongList_Drop(sender As Object, e As DragEventArgs) Handles DatSongList.Drop
        If e.Data.GetFormats.Contains("FileNameW") Then
            Dim filename() As String = e.Data.GetData("FileNameW")
            Dim dlg As New dlgDragImport()
            DlgWindowRoot.ShowDialog(dlg)
            dlg.Max = filename.Count
            Await Task.Run(Sub()
                               Dim libDir = GetSetting("song_dir")
                               Dim localDir As String

                               For i = 0 To filename.Count - 1
                                   'Check extension
                                   If Not DbUpdater.CheckExtention(filename(i)) Then
                                       Continue For
                                   End If
                                   localDir = My.Computer.FileSystem.CombinePath(libDir,
                                                                  My.Computer.FileSystem.GetFileInfo(filename(i)).Name)
                                   Dim audioInfo As New Track(localDir)
                                   If SongExists(audioInfo.Title, audioInfo.Artist) = "" Then
                                       My.Computer.FileSystem.CopyFile(filename(i), localDir, True)
                                       AddSong(audioInfo.Title, audioInfo.Artist, localDir)
                                       Dispatcher.Invoke(Sub() dlg.Progress += 1)
                                   End If
                                   audioInfo = Nothing
                               Next

                               Dispatcher.Invoke(Sub() dlg.ProgressBar.IsIndeterminate = True)
                               _lstSongs = GetSongs()
                           End Sub)
            DlgWindowRoot.IsOpen = False
        End If
    End Sub

    Private Sub DatSongList_DragOver(sender As Object, e As DragEventArgs) Handles DatSongList.DragOver
        If e.Data.GetFormats.Contains("FileNameW") Or e.Data.GetFormats.Contains("GongSolutions.Wpf.DragDrop") Then
            e.Effects = DragDropEffects.All
        Else
            e.Effects = DragDropEffects.None
        End If
        e.Handled = True
    End Sub

    Private Async Sub ButtonAddPlaylist_Click(sender As Object, e As RoutedEventArgs) Handles ButtonAddPlaylist.Click
        Dim dlg As New DlgNewPlaylist
        Dim result = Await DlgWindowRoot.ShowDialog(dlg)
        If result Then
            AddPlaylist(dlg.PlaylistName)
            RefreshPlaylists()
        End If
        ButtonMusic.IsSelected = True
        ListBoxPlaylist.SelectedIndex = -1
    End Sub

    Private Async Sub ButtonMatchLyric_Click(sender As Object, e As RoutedEventArgs) Handles ButtonMatchLyric.Click
        If DatSongList.SelectedIndex <> -1 Then
            DlgWindowRoot.ShowDialog(New DlgProgress)
            Dim itm As SongInfo = DatSongList.SelectedItem
            Dim dir = My.Computer.FileSystem.GetFileInfo(itm.Path).Directory.FullName & "\"
            Dim filename = My.Computer.FileSystem.GetFileInfo(itm.Path).NameWithoutExtention() & ".lrc"
            filename = dir & filename
            If Not My.Computer.FileSystem.FileExists(filename) Then
                Dim tpApi As New ThirdPartyCloudMusicApi
                Dim r As List(Of ThirdPartyCloudMusicApi.SearchResult) = Nothing
                Await Task.Run(Sub()
                                   r = tpApi.Search(itm.Title & itm.Artists)
                               End Sub)
                If IsNothing(r) Then
                    DlgWindowRoot.IsOpen = False
                    Await DlgWindowRoot.ShowDialog(New DlgMessageDialog("匹配歌词", "匹配失败"))
                    Exit Sub
                End If
                If r(0).Artist = itm.Artists And r(0).Name = itm.Title Then
                    Dim lyric = ""
                    Await Task.Run(Sub()
                                       lyric = tpApi.GetLyric(r(0).Id)
                                   End Sub)
                    If lyric <> "" Then
                        Dim writer As New StreamWriter(File.OpenWrite(filename))
                        writer.Write(lyric)
                        writer.Flush()
                        writer.Close()
                    Else
                        DlgWindowRoot.IsOpen = False
                        Dim dlgR As Boolean = Await DlgWindowRoot.ShowDialog(New DlgYesNoDialog("匹配歌词", "自动匹配失败，手动匹配吗？"))
                        If dlgR Then
                            Dim dlg As New DlgChooseLyric(LibV, LibVlcMediaPlayer, r, itm, _lyricPreLoad)
                            Dim cancelled = Await DlgWindowRoot.ShowDialog(dlg)
                            If Not cancelled Then
                                Dim writer As New StreamWriter(File.OpenWrite(filename))
                                writer.Write(dlg.lrc)
                                writer.Flush()
                                writer.Close()
                            End If
                        End If
                        Exit Sub
                    End If
                    DlgWindowRoot.IsOpen = False
                    Await DlgWindowRoot.ShowDialog(New DlgMessageDialog("匹配歌词", "匹配完成"))
                Else
                    DlgWindowRoot.IsOpen = False
                    Dim dlgR As Boolean = Await DlgWindowRoot.ShowDialog(New DlgYesNoDialog("匹配歌词", "自动匹配失败，手动匹配吗？"))
                    If dlgR Then
                        Dim dlg As New DlgChooseLyric(LibV, LibVlcMediaPlayer, r, itm, _lyricPreLoad)
                        Dim cancelled = Await DlgWindowRoot.ShowDialog(dlg)
                        If cancelled Then
                            Exit Sub
                        End If
                        Dim writer As New StreamWriter(File.OpenWrite(filename))
                        writer.Write(dlg.lrc)
                        writer.Flush()
                        writer.Close()
                    End If
                End If
            End If
        End If
    End Sub

    Private Async Sub ButtonChooseLyric_Click(sender As Object, e As RoutedEventArgs)
        If DatSongList.SelectedIndex = -1 Then
            Exit Sub
        End If

        Dim itm As SongInfo = DatSongList.SelectedItem
        Dim dir = My.Computer.FileSystem.GetFileInfo(itm.Path).Directory.FullName & "\"
        Dim filename = My.Computer.FileSystem.GetFileInfo(itm.Path).NameWithoutExtention() & ".lrc"
        filename = dir & filename

        Dim tpApi As New ThirdPartyCloudMusicApi
        DlgWindowRoot.ShowDialog(New DlgProgress)
        Dim searchResults As List(Of ThirdPartyCloudMusicApi.SearchResult) = Nothing
        Dim searchString As String = $"{DatSongList.SelectedItem.Title} {DatSongList.SelectedItem.Artists}"
        Dim errMsg As String = ""
        Await Task.Run(Sub()
                           Try
                               searchResults = tpApi.Search(searchString)
                           Catch ex As Exception
                               errMsg = ex.Message
                           End Try
                       End Sub)
        If IsNothing(searchResults) Then
            DlgWindowRoot.IsOpen = False
            Await DlgWindowRoot.ShowDialog(New DlgMessageDialog("获取失败", errMsg))
            Exit Sub
        End If
        Dim dlg As New DlgChooseLyric(LibV, LibVlcMediaPlayer, searchResults, DatSongList.SelectedItem, _lyricPreLoad)
        DlgWindowRoot.IsOpen = False
        Await DlgWindowRoot.ShowDialog(dlg)

        If dlg.Cancelled Then
            Exit Sub
        End If

        If My.Computer.FileSystem.FileExists(filename) Then
            Dim override As Boolean = Await DlgWindowRoot.ShowDialog(New DlgYesNoDialog("覆盖文件", "文件已存在，覆盖？"))
            If Not override Then
                Exit Sub
            End If
        End If
        Dim writer As New StreamWriter(File.OpenWrite(filename))
        writer.Write(dlg.lrc)
        writer.Flush()
        writer.Close()
    End Sub

    Private Sub MenuPlayMusic(sender As Object, e As RoutedEventArgs)
        If DatSongList.SelectedIndex = -1 Then
            Exit Sub
        End If

        Dim m = New Media(LibV, DatSongList.SelectedItem.Path, FromType.FromPath)
        _nowPlaying = DatSongList.SelectedItem
        LibVlcMediaPlayer.Play(m)
        LabelNowPlaying.Content = "正在播放：" & DatSongList.SelectedItem.title
        '        LabelTotalPlayTime.Content = DlgChooseLyric.MsToTime(LibVlcMediaPlayer.Length)
    End Sub

    Private Sub ButtonPlayerPlayPause_Click(sender As Object, e As RoutedEventArgs) Handles ButtonPlayerPlayPause.Click
        LibVlcMediaPlayer.Pause()
    End Sub

    Private Sub MediaPlayer_TimeChanged(sender As Object, e As MediaPlayerTimeChangedEventArgs)

    End Sub

    Private Sub MediaPlayer_LengthChanged(sender As Object, e As MediaPlayerLengthChangedEventArgs)

    End Sub

    Private Sub MediaPlayer_Stopped(sender As Object, e As EventArgs)
        LabelNowPlaying.Content = ""
    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        Environment.Exit(0)
    End Sub

    Private Sub ButtonPlayerPrev_Click(sender As Object, e As RoutedEventArgs) Handles ButtonPlayerPrev.Click
        If Not IsNothing(_nowPlaying) And _lstSongs.Count > 0 Then
            Dim index = _lstSongs.IndexOf(_nowPlaying)
            If index > 0 Then
                Dim m = New Media(LibV, _lstSongs(index - 1).Path, FromType.FromPath)
                _nowPlaying = _lstSongs(index - 1)
                LibVlcMediaPlayer.Play(m)
                LabelNowPlaying.Content = "正在播放：" & _lstSongs(index - 1).Title
            Else
                SnackbarInfoMessage.MessageQueue.Enqueue("已经是第一首啦")
            End If
        End If
    End Sub

    Private Sub ButtonPlayerNext_Click(sender As Object, e As RoutedEventArgs) Handles ButtonPlayerNext.Click
        If Not IsNothing(_nowPlaying) And _lstSongs.Count > 0 Then
            Dim index = _lstSongs.IndexOf(_nowPlaying)
            If index <> _lstSongs.Count - 1 Then
                Dim m = New Media(LibV, _lstSongs(index + 1).Path, FromType.FromPath)
                _nowPlaying = _lstSongs(index + 1)
                LibVlcMediaPlayer.Play(m)
                LabelNowPlaying.Content = "正在播放：" & _lstSongs(index + 1).Title
            Else
                SnackbarInfoMessage.MessageQueue.Enqueue("已经是最后一首啦")
            End If
        End If
    End Sub

    Private Sub ButtonRemoteChangeAction_Click(sender As Object, e As RoutedEventArgs) Handles ButtonRemoteChangeAction.Click
        If sender.content Is _remoteActionTakeOverContent Then
            sender.content = _remoteActionSyncContent
            ButtonRemoteAction.Content = _remoteActionTakeOverContent
        Else
            sender.content = _remoteActionTakeOverContent
            ButtonRemoteAction.Content = _remoteActionSyncContent
        End If
    End Sub

    Private Class FindRepeatFileInfo
        Property Filename As String
        Property Size As Long
        Property Hash As Byte()

        Shared Operator =(v1 As FindRepeatFileInfo, v2 As FindRepeatFileInfo)
            If v1.Filename = v2.Filename Then
                Return False
            End If

            If v1.Hash.Count <> v2.Hash.Count Then
                Return False
            End If

            For i = 0 To v1.Hash.Count - 1
                If v1.Hash(i) <> v2.Hash(i) Then
                    Return False
                End If
            Next
            Return True
        End Operator

        Shared Operator <>(v1 As FindRepeatFileInfo, v2 As FindRepeatFileInfo)
            Return Not v1 = v2
        End Operator

        Public Overrides Function Equals(obj As Object) As Boolean
            If IsNothing(obj) Then
                Return False
            End If
            If TypeOf obj Is FindRepeatFileInfo Then
                If Me.Filename = obj.Filename Then
                    Return True
                Else
                    Return False
                End If
            Else
                Return False
            End If

        End Function
    End Class

    Private Async Sub ButtonFindRepeat_Click(sender As Object, e As RoutedEventArgs) Handles ButtonFindRepeat.Click
        If _flgFindRepeatRunning Then
            Exit Sub
        End If

        _flgFindRepeatRunning = True
        ProgressFindRepeat.Visibility = Visibility.Visible
        ProgressFindRepeat.Value = 0
        Dim lstFiles As New List(Of FindRepeatFileInfo)

        Await Task.Run(Sub()
                           For Each file In My.Computer.FileSystem.GetFiles(GetSetting("song_dir", ""))
                               If DbUpdater.CheckExtention(file) Then
                                   Dim itm As New FindRepeatFileInfo
                                   itm.Filename = file
                                   itm.Size = My.Computer.FileSystem.GetFileInfo(file).Length
                                   lstFiles.Add(itm)
                               End If
                           Next
                       End Sub)

        ProgressFindRepeat.Maximum = lstFiles.Count
        Dim lstSame As New List(Of FindRepeatFileInfo)

        Await Task.Run(Sub()
                           For i = 0 To lstFiles.Count - 1
                               For j = i + 1 To lstFiles.Count - 1
                                   If lstFiles(i).Size = lstFiles(j).Size Then
                                       If IsNothing(lstFiles(i).Hash) Then
                                           Try
                                               Dim r = New BinaryReader(File.Open(lstFiles(i).Filename, FileMode.Open,
                                                                                  FileAccess.Read))
                                               Dim data = r.ReadBytes(r.BaseStream.Length)
                                               r.Close()
                                               Dim md5 = New MD5CryptoServiceProvider()
                                               Dim hashData = md5.ComputeHash(data)
                                               Dim itm As New FindRepeatFileInfo
                                               itm.Filename = lstFiles(j).Filename
                                               itm.Size = lstFiles(j).Size
                                               itm.Hash = hashData
                                               lstFiles(i) = itm
                                           Catch
                                           End Try
                                       End If

                                       'Check if is able to continue hash check
                                       If IsNothing(lstFiles(i).Hash) Then
                                           'Check Song title
                                           Dim t As New Track(lstFiles(i).Filename)
                                           Dim t2 As New Track(lstFiles(j).Filename)
                                           If t.Title <> t2.Title Then
                                               Continue For
                                           End If
                                           'Title Matches
                                           If Not lstSame.Contains(lstFiles(i)) Then
                                               lstSame.Add(lstFiles(i))
                                           End If
                                           lstSame.Add(lstFiles(j))
                                           Continue For
                                       End If

                                       Try
                                           Dim r = New BinaryReader(File.Open(lstFiles(j).Filename, FileMode.Open, FileAccess.Read))
                                           Dim data = r.ReadBytes(r.BaseStream.Length)
                                           r.Close()
                                           Dim md5 = New MD5CryptoServiceProvider()
                                           Dim hashData = md5.ComputeHash(data)
                                           Dim itm As New FindRepeatFileInfo
                                           itm.Filename = lstFiles(j).Filename
                                           itm.Size = lstFiles(j).Size
                                           itm.Hash = hashData
                                           lstFiles(j) = itm
                                       Catch
                                       End Try

                                       'unable to hash check
                                       If IsNothing(lstFiles(j).Hash) Then
                                           'Check Song title
                                           Dim t As New Track(lstFiles(i).Filename)
                                           Dim t2 As New Track(lstFiles(j).Filename)
                                           If t.Title <> t2.Title Then
                                               Continue For
                                           End If
                                           'Assume is the same
                                           If Not lstSame.Contains(lstFiles(i)) Then
                                               lstSame.Add(lstFiles(i))
                                           End If
                                           If Not lstSame.Contains(lstFiles(j)) Then
                                               lstSame.Add(lstFiles(j))
                                           End If
                                           Continue For
                                       End If

                                       'hash check
                                       If lstFiles(i) = lstFiles(j) Then
                                           If Not lstSame.Contains(lstFiles(i)) Then
                                               lstSame.Add(lstFiles(i))
                                           End If
                                           If Not lstSame.Contains(lstFiles(j)) Then
                                               lstSame.Add(lstFiles(j))
                                           End If
                                       End If
                                   End If
                               Next
                               ProgressFindRepeat.AddOne(Me)
                           Next

                       End Sub)
        _flgFindRepeatRunning = False
        ProgressFindRepeat.Visibility = Visibility.Collapsed
    End Sub
End Class
