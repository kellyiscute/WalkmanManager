Imports System.Collections.ObjectModel
Imports System.Data.SQLite
Imports System.Security.Cryptography
Imports System.Text
Imports ATL

Public Class Database
	'Database Structure
	'-settings
	'	--key
	'	--value
	'-songs
	'	--id
	'	--title
	'	--artists
	'	--path
	'-playlists
	'	--name 
	'-playlist_detail
	'	--playlist_id
	'	--song_id
	'	--song_order

#Region "Structure Definition"

	Public Structure SongInfo
		Public Property Id As Integer

		Public Property Title As String

		Public Property Artists As String

		Public Property Path As String
	End Structure

	Public Structure PlaylistDetail
		Public PlaylistId As Integer
		' ids of songs, already ordered by field "song_order"
		Public SongIds As List(Of Integer)
	End Structure

	Public Structure Playlists
		Public Id As Integer
		Public PlaylistName As String
	End Structure

#End Region

	Public Shared Function Connect() As SQLiteConnection
		Dim connstrBuilder = New SQLiteConnectionStringBuilder
		'connstr_builder.Uri = New Uri(AppDomain.CurrentDomain.BaseDirectory & "data.db").AbsoluteUri
		connstrBuilder.DataSource = "data.db"
		Dim conn = New SQLiteConnection(connstrBuilder.ConnectionString)
		conn.Open()
		Return conn
	End Function


	Public Shared Sub CreateDatabase()
		Dim conn = Connect()
		Dim cmd = New SQLiteCommand(conn)
		Dim trans = conn.BeginTransaction()
		cmd.Transaction = trans
		cmd.CommandText = "create table settings(key text, value text)"
		cmd.ExecuteNonQuery()
		cmd.CommandText = "create table songs(id Integer PRIMARY KEY, title text, artists text, path text)"
		cmd.ExecuteNonQuery()
		cmd.CommandText = "create table playlists(id Integer PRIMARY KEY, name text)"
		cmd.ExecuteNonQuery()
		cmd.CommandText = "create table playlist_detail(playlist_id Integer, song_id Integer, song_order Integer)"
		cmd.ExecuteNonQuery()
		cmd.CommandText = "create table bindings(local_id Integer, cloudmusic_id Integer)"
		cmd.ExecuteNonQuery()
		trans.Commit()
		conn.Close()
		Dim encryptKey As String
		Dim p As New Random()
		For i = 1 To 64
			encryptKey += Chr(p.Next(33, 126))
		Next
		SaveSetting("enc_key", encryptKey)
	End Sub

	Public Shared Function EncryptString(key As String, data As String) As String
		Dim dataByte = Encoding.ASCII.GetBytes(data)
		Dim keyByte = Encoding.ASCII.GetBytes(key)
		Dim encrypted(dataByte.Count) As Byte
		Dim result = ""

		For i = 0 To dataByte.Count - 1
			encrypted(i) = dataByte(i)
			For Each k In keyByte
				encrypted(i) = (encrypted(i) Xor k)
			Next
		Next

		'Checking char, use to check if it decrypts correctly
		encrypted(dataByte.Count) = Encoding.ASCII.GetBytes("P")(0)
		For Each k In keyByte
			encrypted(dataByte.Count) = (encrypted(dataByte.Count) Xor k)
		Next

		For Each b In encrypted
			result += b.ToString("X02")
		Next

		Return result
	End Function

	Public Shared Function DecryptString(key As String, data As String) As String
		If data = "" Then
			Return ""
		End If

		Dim dataByte((data.Length / 2) - 1) As Byte
		Dim c = 0
		For i = 0 To data.Length - 1 Step 2
			dataByte(c) = Convert.ToByte(data.Substring(i, 2), 16)
			c += 1
		Next
		Dim result = ""

		Dim keyByte = Encoding.ASCII.GetBytes(key)
		Dim decrypted(dataByte.Count - 1) As Byte

		For i = 0 To dataByte.Count - 1
			decrypted(i) = dataByte(i)
			For Each k In keyByte
				decrypted(i) = (decrypted(i) Xor k)
			Next
		Next

		result = Encoding.ASCII.GetString(decrypted)

		If result.Last = "P" Then
			Return result.Substring(0, result.Count - 1)
		Else
			Return ""
		End If
	End Function

	''' <summary>
	''' add a song to database
	''' </summary>
	''' <param name="title">song title, read from file</param>
	''' <param name="artists">artists</param>
	''' <param name="path">file path</param>
	Public Overloads Shared Function AddSong(title As String, artists As String, path As String) As Integer
		Dim conn = Connect()
		Dim cmd = New SQLiteCommand(conn)
		cmd.BuildQuery("insert into songs (title, artists, path) values (?, ?, ?)",
					   New Object() {title, artists, path})
		cmd.ExecuteNonQuery()
		Dim id = GetSongId(path, cmd)
		conn.Close()
		Return id
	End Function

	''' <summary>
	''' this is a overload of add_song method, which allows you to pass a SQLiteConnection Object instead of creating a new one
	''' However, NOTICE that you HAVE TO close the connection YOURSELF
	''' </summary>
	''' <param name="title">song title, readed from file</param>
	''' <param name="artists">artists</param>
	''' <param name="path">file path</param>
	''' <param name="conn">SQLiteConnection Object</param>
	Public Overloads Shared Function AddSong(title As String, artists As String, path As String, conn As SQLiteConnection) As Integer
		Dim cmd = New SQLiteCommand(conn)
		cmd.BuildQuery("insert into songs (title, artists, path) calues (?, ?, ?)",
					   New Object() {title, artists, path})
		cmd.ExecuteNonQuery()
		Dim id = GetSongId(path, cmd)
		Return id
	End Function

	''' <summary>
	''' this is a overload of add_song method, which allows you to pass a SQLiteCommand Object instead of creating a new connection and a new command object
	''' </summary>
	''' <param name="title">song title, readed from file</param>
	''' <param name="artists">artists</param>
	''' <param name="path">file path</param>
	''' <param name="cmd">SQLiteCommand Object</param>
	Public Overloads Shared Function AddSong(title As String, artists As String, path As String, cmd As SQLiteCommand) As Integer
		cmd.BuildQuery("insert into songs (title, artists, path) values (?, ?, ?)",
					   New Object() {title, artists, path})
		cmd.ExecuteNonQuery()
		Dim id = GetSongId(path, cmd)
		Return id
	End Function

	''' <summary>
	''' create a new playlist in the database
	''' </summary>
	''' <param name="name">name of playlist</param>
	Public Overloads Shared Sub AddPlaylist(name As String)
		Dim conn = Connect()
		Dim cmd = New SQLiteCommand(conn)
		cmd.BuildQuery("insert into playlists (name) VALUES (?)", New Object() {name})
		cmd.ExecuteNonQuery()
		conn.Close()
	End Sub

	''' <summary>
	''' this is a overload method of AddPlaylist, which allows you to use your own SQLiteConnection object to operate the database
	''' However, NOTICE that you HAVE TO close the connection YOURSELF
	''' </summary>
	''' <param name="name">name of the playlist</param>
	''' <param name="conn">SQLiteConnection Object</param>
	Public Overloads Shared Sub AddPlaylist(name As String, conn As SQLiteConnection)
		Dim cmd = New SQLiteCommand(conn)
		cmd.BuildQuery("insert into playlists (name) VALUES (?)", New Object() {name})
		cmd.ExecuteNonQuery()
	End Sub

	''' <summary>
	''' Remove a Playlist
	''' </summary>
	''' <param name="id">playlist id</param>
	Public Overloads Shared Sub RemovePlaylist(id As Integer)
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		Dim trans = conn.BeginTransaction()
		cmd.Transaction = trans
		cmd.BuildQuery("delete from playlists where id = ?", New Object() {id})
		cmd.ExecuteNonQuery()
		cmd.BuildQuery("delete from playlist_detail where playlist_id = ?", New Object() {id})
		cmd.ExecuteNonQuery()
		trans.Commit()
		conn.Close()
	End Sub

	Public Overloads Shared Sub RemovePlaylist(id As Integer, conn As SQLiteConnection)
		Dim cmd = conn.CreateCommand()
		Dim trans = conn.BeginTransaction()
		cmd.Transaction = trans
		cmd.BuildQuery("delete from playlists where id = ?", New Object() {id})
		cmd.ExecuteNonQuery()
		cmd.BuildQuery("delete from playlist_detail where playlist_id = ?", New Object() {id})
		cmd.ExecuteNonQuery()
		trans.Commit()
	End Sub

	Public Overloads Shared Sub RemovePlaylist(id As Integer, cmd As SQLiteCommand)
		cmd.BuildQuery("delete from playlists where id = ?", New Object() {id})
		cmd.ExecuteNonQuery()
		cmd.BuildQuery("delete from playlist_detail where playlist_id = ?", New Object() {id})
		cmd.ExecuteNonQuery()
	End Sub

	''' <summary>
	''' this is a overload method of AddPlaylist, which allows you to use your own SQliteConnection object to operate the database
	''' </summary>
	''' <param name="name">name of the playlist</param>
	''' <param name="cmd">SQLiteCommand Object</param>
	Public Overloads Shared Sub AddPlaylist(name As String, cmd As SQLiteCommand)
		cmd.BuildQuery("insert into playlists (name) VALUES (?)", New Object() {name})
		cmd.ExecuteNonQuery()
	End Sub

	''' <summary>
	''' Add a song to a playlist
	''' </summary>
	''' <param name="playlistId">id of the playlist</param>
	''' <param name="songId">id of the song</param>
	''' <param name="order">order, start from ZERO</param>
	Public Overloads Shared Sub AddSongToPlaylist(playlistId As Integer, songId As Integer, Optional order As Integer = -1)
		Dim conn = Connect()
		Dim cmd = New SQLiteCommand(conn)
		cmd.BuildQuery("select count(*) from playlist_detail WHERE playlist_id = ? and song_id = ?",
					   New Object() {playlistId, songId})
		Dim r = cmd.ExecuteReader
		r.Read()
		If r(0) = 0 Then
			r.Close()
			If order = -1 Then
				cmd.BuildQuery("select count(*) from playlist_detail where playlist_id = ?", New Object() {playlistId})
				r = cmd.ExecuteReader()
				r.Read()
				Dim counter = r(0)
				r.Close()
				cmd.BuildQuery("insert into playlist_detail values (?, ?, ?)", New Object() {playlistId, songId, counter + 1})
			Else
				cmd.BuildQuery("insert into playlist_detail values (?, ?, ?)", New Object() {playlistId, songId, order})
			End If
			cmd.ExecuteNonQuery()
			conn.Close()
		Else
			r.Close()
			conn.Close()
			Throw New Exception("Already Exist")
		End If
	End Sub

	''' <summary>
	''' this is a overload method of AddSongToPlaylist, which allows you to use your own SQliteConnection object to operate the database
	''' </summary>
	''' <param name="playlistId">id of the playlist</param>
	''' <param name="songId">id of the song</param>
	''' <param name="conn">SQLiteConnection Object</param>
	''' <param name="order">order, start from ZERO</param>
	Public Overloads Shared Sub AddSongToPlaylist(playlistId As Integer, songId As Integer, conn As SQLiteConnection,
												  Optional order As Integer = -1)
		Dim cmd = New SQLiteCommand(conn)
		cmd.BuildQuery("select count(*) from playlist_detail WHERE playlist_id = ? and song_id = ?",
					   New Object() {playlistId, songId})
		Dim r = cmd.ExecuteReader
		r.Read()
		If r(0) = 0 Then
			r.Close()
			If order = -1 Then
				cmd.BuildQuery("select count(*) from playlist_detail where playlist_id = ?", New Object() {playlistId})
				r = cmd.ExecuteReader()
				r.Read()
				Dim counter = r(0)
				r.Close()
				cmd.BuildQuery("insert into playlist_detail values (?, ?, ?)", New Object() {playlistId, songId, counter + 1})
			Else
				cmd.BuildQuery("insert into playlist_detail values (?, ?, ?)", New Object() {playlistId, songId, order})
			End If
		Else
			r.Close()
			Throw New Exception("Already Exist")
		End If
	End Sub

	''' <summary>
	''' this is a overload method of AddSongToPlaylist, which allows you to use your own SQLiteCommand object to operate the database
	''' </summary>
	''' <param name="playlistId">id of the playlist</param>
	''' <param name="songId">id of the song</param>
	''' <param name="order">order, start from ZERO</param>
	''' <param name="cmd">SQLiteCommand Object</param>
	Public Overloads Shared Sub AddSongToPlaylist(playlistId As Integer, songId As Integer,
												  cmd As SQLiteCommand, Optional order As Integer = -1, Optional ignoreDuplicate As Boolean = False)

		cmd.BuildQuery("select count(*) from playlist_detail WHERE playlist_id = ? and song_id = ?",
					   New Object() {playlistId, songId})
		Dim r = cmd.ExecuteReader
		r.Read()
		If r(0) = 0 Or ignoreDuplicate Then
			r.Close()
			If order = -1 Then
				cmd.BuildQuery("select count(*) from playlist_detail where playlist_id = ?", New Object() {playlistId})
				r = cmd.ExecuteReader()
				r.Read()
				Dim counter = r(0)
				r.Close()
				cmd.BuildQuery("insert into playlist_detail values (?, ?, ?)", New Object() {playlistId, songId, counter + 1})
			Else
				cmd.BuildQuery("insert into playlist_detail values (?, ?, ?)", New Object() {playlistId, songId, order})
			End If
			cmd.ExecuteNonQuery()
		Else
			r.Close()
			Throw New Exception("Already Exist")
		End If
	End Sub

	''' <summary>
	''' remove a song from a playlist
	''' </summary>
	''' <param name="playlistId">id of the playlist</param>
	''' <param name="songId">id of the song</param>
	Public Overloads Shared Sub RemoveSongFromPlaylist(playlistId As Integer, songId As Integer)
		Dim conn = Connect()
		Dim cmd = New SQLiteCommand(conn)
		cmd.BuildQuery("delete from playlist_detail where playlist_id = ? and song_id = ?", New Object() {playlistId,
																										  songId})
		cmd.ExecuteNonQuery()
		conn.Close()
	End Sub

	''' <summary>
	''' this is a overload method of RemoveSongFromPlaylist, which allows you to use your own SQLiteCommand object to operate the database
	''' </summary>
	''' <param name="playlistId">the id of the playlist</param>
	''' <param name="songId">the id of the song</param>
	''' <param name="conn">SQLiteConnection Object</param>
	Public Overloads Shared Sub RemoveSongFromPlaylist(playlistId As Integer, songId As Integer, conn As SQLiteConnection)
		Dim cmd = New SQLiteCommand(conn)
		cmd.BuildQuery("delete from playlist_detail where playlist_id = ? and song_id = ?", New Object() {playlistId,
																										  songId})
		cmd.ExecuteNonQuery()
	End Sub

	''' <summary>
	''' this is a overload method of RemoveSongFromPlaylist, which allows you to use your own SQLiteCommand object to operate the database
	''' </summary>
	''' <param name="playlistId">the id of the playlist</param>
	''' <param name="songId">the id of the song</param>
	''' <param name="cmd">SQLiteConnection Object</param>
	Public Overloads Shared Sub RemoveSongFromPlaylist(playlistId As Integer, songId As Integer, cmd As SQLiteCommand)
		cmd.BuildQuery("delete from playlist_detail where playlist_id = ? and song_id = ?", New Object() {playlistId,
																										  songId})
		cmd.ExecuteNonQuery()
	End Sub

	''' <summary>
	''' remove a song from database
	''' </summary>
	''' <param name="songId">id of the song</param>
	Public Overloads Shared Sub RemoveSongFromLib(songId As Integer)
		Dim conn = Connect()
		Dim cmd = New SQLiteCommand(conn)
		cmd.BuildQuery("delete from songs where id = ?", New Object() {songId})
		cmd.ExecuteNonQuery()
		cmd.BuildQuery("delete from playlist_detail where song_id = ?", New Object() {songId})
		cmd.ExecuteNonQuery()
		conn.Close()
	End Sub

	''' <summary>
	''' this is a overload method of RemoveSongFromPlaylist, which allows you to use your own SQLiteCommand object to operate the database
	''' </summary>
	''' <param name="songId">id of the song</param>
	''' <param name="conn">SQLiteConnection Object</param>
	Public Overloads Shared Sub RemoveSongFromLib(songId As Integer, conn As SQLiteConnection)
		Dim cmd = New SQLiteCommand(conn)
		cmd.BuildQuery("delete from songs where id = ?", New Object() {songId})
		cmd.ExecuteNonQuery()
		cmd.BuildQuery("delete from playlist_detail where song_id = ?", New Object() {songId})
		cmd.ExecuteNonQuery()
	End Sub

	''' <summary>
	''' this is a overload method of RemoveSongFromPlaylist, which allows you to use your own SQLiteCommand object to operate the database
	''' </summary>
	''' <param name="songId">id of the song</param>
	''' <param name="cmd">SQLiteCommand Object</param>
	Public Overloads Shared Sub RemoveSongFromLib(songId As Integer, cmd As SQLiteCommand)
		cmd.BuildQuery("delete from songs where id = ?", New Object() {songId})
		cmd.ExecuteNonQuery()
		cmd.BuildQuery("delete from playlist_detail where song_id = ?", New Object() {songId})
		cmd.ExecuteNonQuery()
	End Sub

	''' <summary>
	''' get user setting from database
	''' </summary>
	''' <param name="key">key of setting</param>
	''' <param name="defaultValue">default value to return if not found(default = Nothing)</param>
	''' <returns>setting value</returns>
	Public Shared Function GetSetting(key As String, Optional defaultValue As String = Nothing) As String
		Dim conn = Connect()
		Dim cmd = New SQLiteCommand(conn)
		cmd.BuildQuery("select * from settings where key = ?", New Object() {key})
		Dim r = cmd.ExecuteReader
		If r.HasRows Then
			r.Read()
			Dim val = r("value")
			r.Close()
			conn.Close()
			Return val
		Else
			Return defaultValue
		End If
	End Function

	''' <summary>
	''' save a setting to database
	''' </summary>
	''' <param name="key">key of setting</param>
	''' <param name="value">value</param>
	Public Shared Sub SaveSetting(key As String, value As String)
		If IsNothing(GetSetting(key)) Then
			Dim conn = Connect()
			Dim cmd = New SQLiteCommand(conn)
			cmd.BuildQuery("insert into Settings Values(?, ?)", New Object() {key, value})
			cmd.ExecuteNonQuery()
			conn.Close()
		Else
			Dim conn = Connect()
			Dim cmd As New SQLiteCommand(conn)
			cmd.BuildQuery("update Settings set value = ? where key = ?", New Object() {value, key})
			cmd.ExecuteNonQuery()
			conn.Close()
		End If
	End Sub

	''' <summary>
	''' Get song Id with path
	''' </summary>
	''' <param name="path">song path</param>
	''' <returns>song id or Nothing if not found</returns>
	Public Overloads Shared Function GetSongId(path As String) As Integer
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("select * from songs where path = ?", New Object() {path})
		Dim reader = cmd.ExecuteReader()
		If reader.Read() Then
			Dim id = reader(0)
			reader.Close()
			conn.Close()
			Return id
		Else
			reader.Close()
			conn.Close()
			Return Nothing
		End If
	End Function

	''' <summary>
	''' Get song with path
	''' </summary>
	''' <param name="path"></param>
	''' <param name="conn"></param>
	''' <returns></returns>
	Public Overloads Shared Function GetSongId(path As String, conn As SQLiteConnection) As Integer
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("select * from songs where path = ?", New Object() {path})
		Dim reader = cmd.ExecuteReader()
		If reader.Read() Then
			Dim id = reader(0)
			reader.Close()
			Return id
		Else
			reader.Close()
			Return Nothing
		End If
	End Function

	Public Overloads Shared Function GetSongId(path As String, cmd As SQLiteCommand) As Integer
		cmd.BuildQuery("select * from songs where path = ?", New Object() {path})
		Dim reader = cmd.ExecuteReader()
		If reader.Read() Then
			Dim id = reader(0)
			reader.Close()
			Return id
		Else
			reader.Close()
			Return Nothing
		End If
	End Function

	''' <summary>
	''' Get song list from database
	''' </summary>
	''' <returns></returns>
	Public Overloads Shared Function GetSongs() As ObservableCollection(Of SongInfo)
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("select * from songs order by title")
		Dim reader = cmd.ExecuteReader()
		If reader.HasRows Then
			Dim lst As New ObservableCollection(Of SongInfo)
			While reader.Read()
				Dim t As New SongInfo
				t.Id = reader("id")
				t.Title = reader("title")
				t.Artists = reader("artists")
				t.Path = reader("path")
				lst.Add(t)
			End While
			reader.Close()
			conn.Close()
			Return lst
		Else
			reader.Close()
			conn.Close()
			Return New ObservableCollection(Of SongInfo)
		End If
	End Function

	''' <summary>
	''' this is a overload method of GetSongs, which allows you to use your own SQLiteConnection object to operate the database
	''' </summary>
	''' <param name="conn">SQLiteConnection Object</param>
	''' <returns></returns>
	Public Overloads Shared Function GetSongs(conn As SQLiteConnection) As ObservableCollection(Of SongInfo)
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("select * from songs order by title")
		Dim reader = cmd.ExecuteReader()
		If reader.HasRows Then
			Dim lst As New ObservableCollection(Of SongInfo)
			While reader.Read()
				Dim t As New SongInfo
				t.Id = reader("id")
				t.Title = reader("title")
				t.Artists = reader("artists")
				t.Path = reader("path")
				lst.Add(t)
			End While
			reader.Close()
			Return lst
		Else
			reader.Close()
			Return Nothing
		End If
	End Function

	''' <summary>
	''' this is a overload method of GetSongs, which allows you to use your own SQLiteConnection object to operate the database
	''' </summary>
	''' <param name="cmd">SQLiteCommand Object</param>
	''' <returns></returns>
	Public Overloads Shared Function GetSongs(cmd As SQLiteCommand) As ObservableCollection(Of SongInfo)
		cmd.BuildQuery("select * from songs order by title")
		Dim reader = cmd.ExecuteReader()
		If reader.HasRows Then
			Dim lst As New ObservableCollection(Of SongInfo)
			While reader.Read()
				Dim t As New SongInfo
				t.Id = reader("id")
				t.Title = reader("title")
				t.Artists = reader("artists")
				t.Path = reader("path")
				lst.Add(t)
			End While
			reader.Close()
			Return lst
		Else
			reader.Close()
			Return Nothing
		End If
	End Function

	''' <summary>
	''' update song detail in dastabase
	''' </summary>
	''' <param name="path">path of the song</param>
	Public Shared Sub UpdateInfo(path As String)
		Dim conn = Connect()
		Dim trans = conn.BeginTransaction()
		Dim cmd = conn.CreateCommand()
		cmd.Transaction = trans
		cmd.BuildQuery("select count(*) from songs where path = ?", New Object() {path})
		Dim reader = cmd.ExecuteReader()
		reader.Read()
		If reader(0) > 0 Then
			reader.Close()
			Dim t As New Track(path)
			cmd.BuildQuery("update songs set title = ?, artists = ? where path = ?", New Object() {t.Title, t.Artist, path})
			cmd.ExecuteNonQuery()
			trans.Commit()
			conn.Close()
		Else
			reader.Close()
			conn.Close()
		End If
	End Sub

	''' <summary>
	''' check if playlist name is used
	''' </summary>
	''' <param name="playlistName">name</param>
	''' <returns>True if not used</returns>
	Public Overloads Shared Function CheckPlaylistNameAvailability(playlistName As String) As Boolean
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("select count(*) from playlists where name = ?", New Object() {playlistName})
		Dim reader = cmd.ExecuteReader()
		reader.Read()
		If reader(0) = 0 Then
			reader.Close()
			conn.Close()
			Return True
		Else
			reader.Close()
			conn.Close()
			Return False
		End If
	End Function

	Public Overloads Shared Function CheckPlaylistNameAvailability(playlistName As String, cmd As SQLiteCommand) As Boolean
		cmd.BuildQuery("select count(*) from playlists where name = ?", New Object() {playlistName})
		Dim reader = cmd.ExecuteReader()
		reader.Read()
		If reader(0) = 0 Then
			reader.Close()
			Return True
		Else
			reader.Close()
			Return False
		End If
	End Function

	''' <summary>
	''' Get all playlists
	''' </summary>
	''' <returns></returns>
	Public Shared Function GetPlaylists() As List(Of String)
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("select * from playlists")
		Dim reader = cmd.ExecuteReader()
		Dim result As New List(Of String)
		While reader.Read()
			result.Add(reader(1))
		End While
		reader.Close()
		conn.Close()
		Return result
	End Function

	Public Shared Function GetPlaylists(conn As SQLiteConnection) As List(Of String)
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("select * from playlists")
		Dim reader = cmd.ExecuteReader()
		Dim result As New List(Of String)
		While reader.Read()
			result.Add(reader(1))
		End While
		reader.Close()
		Return result
	End Function

	''' <summary>
	''' get song ids of a playlist
	''' </summary>
	''' <param name="id">playlist id</param>
	''' <returns>song ids</returns>
	Public Overloads Shared Function GetSongsFromPlaylist(id As Integer) As List(Of Integer)
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("select * from playlist_detail where playlist_id = ? order by song_order", New Object() {id})
		Dim reader = cmd.ExecuteReader()
		Dim result As New List(Of Integer)
		While reader.Read
			result.Add(reader(1))
		End While
		reader.Close()
		conn.Close()
		Return result
	End Function

	Public Overloads Shared Function GetSongsFromPlaylist(name As String) As List(Of Integer)
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("SELECT * FROM (playlist_detail INNER JOIN playlists ON playlist_detail.playlist_id = playlists.id) WHERE name = ?;", New Object() {name})
		Dim reader = cmd.ExecuteReader()
		Dim result As New List(Of Integer)
		While reader.Read
			result.Add(reader(1))
		End While
		reader.Close()
		conn.Close()
		Return result
	End Function

	Public Overloads Shared Function GetSongsFromPlaylist(id As Integer, conn As SQLiteConnection) As List(Of Integer)
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("select * from playlist_detail where playlist_id = ? orderby song_order", New Object() {id})
		Dim reader = cmd.ExecuteReader()
		Dim result As New List(Of Integer)
		While reader.Read
			result.Add(1)
		End While
		reader.Close()
		Return result
	End Function

	Public Overloads Shared Function GetSongsFromPlaylist(name As String, conn As SQLiteConnection) As List(Of Integer)
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("SELECT * FROM (playlist_detail INNER JOIN playlists ON playlist_detail.playlist_id = playlists.id) WHERE name = ?;", New Object() {name})
		Dim reader = cmd.ExecuteReader()
		Dim result As New List(Of Integer)
		While reader.Read
			result.Add(1)
		End While
		reader.Close()
		Return result
	End Function

	Public Overloads Shared Function GetSongsFromPlaylist(id As Integer, cmd As SQLiteCommand) As List(Of Integer)
		cmd.BuildQuery("select * from playlist_detail where playlist_id = ? orderby song_order", New Object() {id})
		Dim reader = cmd.ExecuteReader()
		Dim result As New List(Of Integer)
		While reader.Read
			result.Add(1)
		End While
		reader.Close()
		Return result
	End Function

	Public Overloads Shared Function GetSongsFromPlaylist(name As String, cmd As SQLiteCommand) As List(Of Integer)
		cmd.BuildQuery("SELECT * FROM (playlist_detail INNER JOIN playlists ON playlist_detail.playlist_id = playlists.id) WHERE name = ?;", New Object() {name})
		Dim reader = cmd.ExecuteReader()
		Dim result As New List(Of Integer)
		While reader.Read
			result.Add(1)
		End While
		reader.Close()
		Return result
	End Function

	''' <summary>
	''' get song info by id
	''' </summary>
	''' <param name="id">song id</param>
	''' <returns>song detail info</returns>
	Public Overloads Shared Function GetSongById(id As Integer) As SongInfo
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("select * from songs where id = ?", New Object() {id})
		Dim reader = cmd.ExecuteReader()
		If reader.HasRows Then
			Dim t As New SongInfo
			reader.Read()
			t.Id = reader("id")
			t.Title = reader("title")
			t.Artists = reader("artists")
			t.Path = reader("path")
			reader.Close()
			conn.Close()
			Return t
		Else
			reader.Close()
			conn.Close()
			Return Nothing
		End If
	End Function

	Public Overloads Shared Function GetSongById(id As Integer, conn As SQLiteConnection) As SongInfo
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("select * from songs where id = ?", New Object() {id})
		Dim reader = cmd.ExecuteReader()
		If reader.HasRows Then
			Dim t As New SongInfo
			reader.Read()
			t.Id = reader("id")
			t.Title = reader("title")
			t.Artists = reader("artists")
			t.Path = reader("path")
			reader.Close()
			Return t
		Else
			reader.Close()
			Return Nothing
		End If
	End Function

	Public Overloads Shared Function GetSongById(id As Integer, cmd As SQLiteCommand) As SongInfo
		cmd.BuildQuery("select * from songs where id = ?", New Object() {id})
		Dim reader = cmd.ExecuteReader()
		If reader.HasRows Then
			Dim t As New SongInfo
			reader.Read()
			t.Id = reader("id")
			t.Title = reader("title")
			t.Artists = reader("artists")
			t.Path = reader("path")
			reader.Close()
			Return t
		Else
			reader.Close()
			Return Nothing
		End If
	End Function

	Public Overloads Shared Function GetPlaylistIdByName(playlistName As String) As Integer
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("select id from playlists where name = ?", New Object() {playlistName})
		Dim reader = cmd.ExecuteReader()
		If reader.HasRows Then
			reader.Read()
			Dim id = reader(0)
			reader.Close()
			conn.Close()
			Return id
		Else
			reader.Close()
			conn.Close()
			Return -1
		End If
	End Function

	Public Overloads Shared Function GetPlaylistIdByName(playlistName As String, ByRef cmd As SQLiteCommand) As Integer
		cmd.BuildQuery("select id from playlists where name = ?", New Object() {playlistName})
		Dim reader = cmd.ExecuteReader()
		If reader.HasRows Then
			reader.Read()
			Dim id = reader(0)
			reader.Close()
			Return id
		Else
			reader.Close()
			Return -1
		End If
	End Function

	Public Overloads Shared Sub ClearPlaylist(playlistId As Integer)
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("delete from playlist_detail where playlist_id = ?", New Object() {playlistId})
		cmd.ExecuteNonQuery()
		conn.Close()
	End Sub

	Public Overloads Shared Sub ClearPlaylist(playlistId As Integer, cmd As SQLiteCommand)
		cmd.BuildQuery("delete from playlist_detail where playlist_id = ?", New Object() {playlistId})
		cmd.ExecuteNonQuery()
	End Sub

	''' <summary>
	''' To check if there is an identical song in the database
	''' </summary>
	''' <param name="title">song title</param>
	''' <param name="artists">song artists</param>
	''' <returns>return path if found, empty string if not found</returns>
	Public Overloads Shared Function SongExists(title As String, artists As String) As String
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("select * from songs where title = ? and artists = ?", New Object() {title, artists})
		Dim r = cmd.ExecuteReader()
		If r.HasRows Then
			r.Read()
			Dim result = r("path")
			r.Close()
			conn.Close()
			Return result
		Else
			r.Close()
			conn.Close()
			Return ""
		End If
	End Function

	Public Overloads Shared Function SongExists(title As String, artists As String, conn As SQLiteConnection) As String
		Dim cmd = conn.CreateCommand()
		cmd.BuildQuery("select * from songs where title = ? and artists = ?", New Object() {title, artists})
		Dim r = cmd.ExecuteReader()
		If r.HasRows Then
			r.Read()
			Dim result = r("path")
			r.Close()
			Return result
		Else
			r.Close()
			Return ""
		End If
	End Function

	Public Overloads Shared Function SongExists(title As String, artists As String, cmd As SQLiteCommand) As String
		cmd.BuildQuery("select * from songs where title = ? and artists = ?", New Object() {title, artists})
		Dim r = cmd.ExecuteReader()
		If r.HasRows Then
			r.Read()
			Dim result = r("path")
			r.Close()
			Return result
		Else
			r.Close()
			Return ""
		End If

	End Function

	Public Overloads Shared Function FindSong(title As String, artists As String) As List(Of Integer)
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand
		cmd.BuildQuery("select id from songs where title = ? and artists = ?", New Object() {title, artists})
		Dim r = cmd.ExecuteReader()
		Dim result As New List(Of Integer)
		If r.HasRows Then
			While r.Read
				result.Add(r(0))
			End While
		End If
		r.Close()
		conn.Close()
		Return result
	End Function

	Public Shared Sub RenamePlaylist(name As String, newName As String)
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand
		cmd.BuildQuery("update playlists set name = ? where name = ?", New Object() {newName, name})
		cmd.ExecuteNonQuery()
		conn.Close()
		cmd.Dispose()
		conn.Dispose()
	End Sub

	Public Shared Sub InitPlaylists()
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand
		cmd.CommandText = "DELETE FROM playlists"
		cmd.ExecuteNonQuery()
		cmd.CommandText = "DELETE FROM playlist_detail"
		cmd.ExecuteNonQuery()
		conn.Close()
	End Sub

	Public Shared Sub InitSongLib()
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand
		cmd.CommandText = "DELETE FROM songs"
		cmd.ExecuteNonQuery()
		conn.Close()
	End Sub

	Public Shared Sub ClearDatabase()
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand
		cmd.CommandText = "DROP TABLE playlists"
		cmd.ExecuteNonQuery()
		cmd.CommandText = "DROP TABLE playlist_detail"
		cmd.ExecuteNonQuery()
		cmd.CommandText = "DROP TABLE songs"
		cmd.ExecuteNonQuery()
		cmd.CommandText = "DROP TABLE settings"
		cmd.ExecuteNonQuery()
		cmd.CommandText = "DROP TABLE bindings"
		cmd.ExecuteNonQuery()
		conn.Close()
		CreateDatabase()
	End Sub

	Public Overloads Shared Function FindCloudmusicBinding(cloudmusicId As Integer) As Integer
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand
		Dim result = FindCloudmusicBinding(cloudmusicId, cmd)
		conn.Close()
		Return result
	End Function

	Public Overloads Shared Function FindCloudmusicBinding(cloudmusicId As Integer, cmd As SQLiteCommand) As Integer
		cmd.CommandText = "SELECT local_id FROM bindings WHERE cloudmusic_id = " & cloudmusicId
		Dim r = cmd.ExecuteReader()
		If r.HasRows Then
			r.Read()
			Dim result = r(0)
			r.Close()
			Return result
		Else
			Return 0
		End If
	End Function

	Public Shared Sub BindSong(cloudmusicId As Integer, localId As Integer)
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand
		cmd.CommandText = $"INSERT INTO bindings VALUES({localId}, {cloudmusicId})"
		cmd.ExecuteNonQuery()
		conn.Close()
	End Sub
End Class