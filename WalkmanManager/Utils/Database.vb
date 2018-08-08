Imports System.Data
Imports System.Data.SQLite

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

	Public Shared Function Connect() As SQLiteConnection
		Dim connstrBuilder = New SQLiteConnectionStringBuilder
		'connstr_builder.Uri = New Uri(AppDomain.CurrentDomain.BaseDirectory & "data.db").AbsoluteUri
		connstrBuilder.DataSource = "data.db"
		Dim conn = New SQLiteConnection(connstrBuilder.ConnectionString)
		conn.Open()
		Return conn
	End Function

	''' <summary>
	''' this method is to create a python-ish command builder, which is easier to use
	''' </summary>
	''' <param name="cmd">SQLiteCommand Object</param>
	''' <param name="sql">SQL Query string</param>
	''' <param name="params">SQL Query arguments</param>
	Public Shared Sub BuildQueryString(ByRef cmd As SQLiteCommand, sql As String, params As Object())
		Dim counter As Integer = 0
		'convert all question mark to the form of @param#
		While InStr(sql, "?") <> 0
			sql = Replace(sql, "?", "@param" & counter, 1, 1)
			counter += 1
		End While
		counter = 0

		'get types of params
		Dim types As New List(Of DbType)
		For Each param In params
			Select Case param.GetTypeCode()
				Case TypeCode.Int32
					types.Add(DbType.Int32)
				Case TypeCode.Int16
					types.Add(DbType.Int16)
				Case TypeCode.Int64
					types.Add(DbType.Int64)
				Case TypeCode.String
					types.Add(DbType.String)
				Case TypeCode.DateTime
					types.Add(DbType.DateTime)
			End Select
		Next

		'process done
		cmd.CommandText = sql

		'add params to command object
		For Each param In params
			cmd.Parameters.Add("@param" & counter, types(counter)).Value = param
			counter += 1
		Next
	End Sub

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
		trans.Commit()
		conn.Close()
	End Sub

	''' <summary>
	''' add a song to database
	''' </summary>
	''' <param name="title">song title, readed from file</param>
	''' <param name="artists">artists</param>
	''' <param name="path">file path</param>
	Public Overloads Shared Sub AddSong(title As String, artists As String, path As String)
		Dim conn = Connect()
		Dim cmd = New SQLiteCommand(conn)
		BuildQueryString(cmd, "insert into songs (title, artists, path) calues (?, ?, ?)",
										New Object() {title, artists, path})
		cmd.ExecuteNonQuery()
		conn.Close()
	End Sub

	''' <summary>
	''' this is a overload of add_song method, which allows you to pass a SQLiteConnection Object instead of creating a new one
	''' However, NOTICE that you HAVE TO close the connection YOURSELF
	''' </summary>
	''' <param name="title">song title, readed from file</param>
	''' <param name="artists">artists</param>
	''' <param name="path">file path</param>
	''' <param name="conn">SQLiteConnection Object</param>
	Public Overloads Shared Sub AddSong(title As String, artists As String, path As String, conn As SQLiteConnection)
		Dim cmd = New SQLiteCommand(conn)
		BuildQueryString(cmd, "insert into songs (title, artists, path) calues (?, ?, ?)",
						 New Object() {title, artists, path})
		cmd.ExecuteNonQuery()
	End Sub

	''' <summary>
	''' this is a overload of add_song method, which allows you to pass a SQLiteCommand Object instead of creating a new connection and a new command object
	''' </summary>
	''' <param name="title">song title, readed from file</param>
	''' <param name="artists">artists</param>
	''' <param name="path">file path</param>
	''' <param name="cmd">SQLiteCommand Object</param>
	Public Overloads Shared Sub AddSong(title As String, artists As String, path As String, cmd As SQLiteCommand)
		BuildQueryString(cmd, "insert into songs (title, artists, path) values (?, ?, ?)",
						 New Object() {title, artists, path})
		cmd.ExecuteNonQuery()
	End Sub
	''' <summary>
	''' create a new playlist in the database
	''' </summary>
	''' <param name="name">name of playlist</param>
	Public Overloads Shared Sub AddPlaylist(name As String)
		Dim conn = Connect()
		Dim cmd = New SQLiteCommand(conn)
		BuildQueryString(cmd, "insert into playlists (name) VALUES (?)", New Object() {name})
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
		BuildQueryString(cmd, "insert into playlists (name) VALUES (?)", New Object() {name})
		cmd.ExecuteNonQuery()
	End Sub

	''' <summary>
	''' this is a overload method of AddPlaylist, which allows you to use your own SQliteConnection object to operate the database
	''' </summary>
	''' <param name="name">name of the playlist</param>
	''' <param name="cmd">SQLiteCommand Object</param>
	Public Overloads Shared Sub AddPlaylist(name As String, cmd As SQLiteCommand)
		BuildQueryString(cmd, "insert into playlists (name) VALUES (?)", New Object() {name})
		cmd.ExecuteNonQuery()
	End Sub

	''' <summary>
	''' Add a song to a playlist
	''' </summary>
	''' <param name="playlistId">id of the playlist</param>
	''' <param name="songId">id of the song</param>
	''' <param name="order">order, start from ZERO</param>
	Public Overloads Shared Sub AddSongToPlaylist(playlistId As Integer, songId As Integer, order As Integer)
		Dim conn = Connect()
		Dim cmd = New SQLiteCommand(conn)
		BuildQueryString(cmd, "select count(*) from playlist_detail WHERE playlist_id = ? and song_id = ?", New Object() {playlistId, songId})
		Dim r = cmd.ExecuteReader
		r.NextResult()
		If r(0) = 0 Then
			r.Close()
			BuildQueryString(cmd, "insert into playlist_detail values (?, ?, ?)", New Object() {playlistId, songId, order})
			cmd.ExecuteNonQuery()
			conn.Close()
		Else
			Throw New Exception("Already Exist")
		End If
	End Sub

	''' <summary>
	''' this is a overload method of AddSongToPlaylist, which allows you to use your own SQliteConnection object to operate the database
	''' </summary>
	''' <param name="playlistId">id of the playlist</param>
	''' <param name="songId">id of the song</param>
	''' <param name="order">order, start from ZERO</param>
	''' <param name="conn">SQLiteConnection Object</param>
	Public Overloads Shared Sub AddSongToPlaylist(playlistId As Integer, songId As Integer, order As Integer, conn As SQLiteConnection)
		Dim cmd = New SQLiteCommand(conn)
		BuildQueryString(cmd, "select count(*) from playlist_detail WHERE playlist_id = ? and song_id = ?", New Object() {playlistId, songId})
		Dim r = cmd.ExecuteReader
		r.NextResult()
		If r(0) = 0 Then
			r.Close()
			BuildQueryString(cmd, "insert into playlist_detail values (?, ?, ?)", New Object() {playlistId, songId, order})
			cmd.ExecuteNonQuery()
		Else
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
	Public Overloads Shared Sub AddSongToPlaylist(playlistId As Integer, songId As Integer, order As Integer, cmd As SQLiteCommand)
		BuildQueryString(cmd, "select count(*) from playlist_detail WHERE playlist_id = ? and song_id = ?", New Object() {playlistId, songId})
		Dim r = cmd.ExecuteReader
		r.NextResult()
		If r(0) = 0 Then
			r.Close()
			BuildQueryString(cmd, "insert into playlist_detail values (?, ?, ?)", New Object() {playlistId, songId, order})
			cmd.ExecuteNonQuery()
		Else
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
		BuildQueryString(cmd, "delete from playlist_detail where playlist_id = ? and song_id = ?", New Object() {playlistId,
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
		BuildQueryString(cmd, "delete from playlist_detail where playlist_id = ? and song_id = ?", New Object() {playlistId,
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
		BuildQueryString(cmd, "delete from playlist_detail where playlist_id = ? and song_id = ?", New Object() {playlistId,
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
		BuildQueryString(cmd, "delete from songs where id = ?", New Object() {songId})
		cmd.ExecuteNonQuery()
		BuildQueryString(cmd, "delete from playlist_detail where song_id = ?", New Object() {songId})
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
		BuildQueryString(cmd, "delete from songs where id = ?", New Object() {songId})
		cmd.ExecuteNonQuery()
		BuildQueryString(cmd, "delete from playlist_detail where song_id = ?", New Object() {songId})
		cmd.ExecuteNonQuery()
	End Sub

	''' <summary>
	''' this is a overload method of RemoveSongFromPlaylist, which allows you to use your own SQLiteCommand object to operate the database
	''' </summary>
	''' <param name="songId">id of the song</param>
	''' <param name="cmd">SQLiteCommand Object</param>
	Public Overloads Shared Sub RemoveSongFromLib(songId As Integer, cmd As SQLiteCommand)
		BuildQueryString(cmd, "delete from songs where id = ?", New Object() {songId})
		cmd.ExecuteNonQuery()
		BuildQueryString(cmd, "delete from playlist_detail where song_id = ?", New Object() {songId})
		cmd.ExecuteNonQuery()
	End Sub

	''' <summary>
	''' get user setting from database
	''' </summary>
	''' <param name="key">key of setting</param>
	''' <param name="defaultValue">default value to return if not found(default = Nothing)</param>
	''' <returns>setting value</returns>
	Public Shared Function GetSetting(key As String, Optional defaultValue As String = Nothing)
		Dim conn = Connect()
		Dim cmd = New SQLiteCommand(conn)
		BuildQueryString(cmd, "select * from settings where key = ?", New Object() {key})
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
			BuildQueryString(cmd, "insert into Settings Values(?, ?)", New Object() {key, value})
			cmd.ExecuteNonQuery()
			conn.Close()
		Else
			Dim conn = Connect()
			Dim cmd As New SQLiteCommand(conn)
			BuildQueryString(cmd, "update Settings set value = ? where key = ?", New Object() {value, key})
		End If

	End Sub

	'TODO: This Function has NOT been debugged!
	Public Shared Function IsExist(ByVal songName As String, Optional ByVal songArtist As String = Nothing) As Boolean
		Dim conn = Connect()
		Dim cmd = New SQLiteCommand(conn)
		If Not IsNothing(songArtist) Then
			cmd.CommandText = String.Format("select count(*) from songs where title = '{0}' and artists = '?'", songName, songArtist)
		Else
			cmd.CommandText = String.Format("select count(*) from songs where title = '{0}'", songName)
		End If
		Dim r = cmd.ExecuteReader
		If Int(r.Read()) Then
			Return True
		Else
			Return False
		End If
		'conn.Close()
	End Function

	''' <summary>
	''' Get song Id with path
	''' </summary>
	''' <param name="path">song path</param>
	''' <returns>song id or Nothing if not found</returns>
	Public Overloads Shared Function GetSongId(path As String) As Integer
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		BuildQueryString(cmd, "select * from songs where path = ?", New Object() {path})
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

	Public Overloads Shared Function GetSongId(path As String, conn As SQLiteConnection) As Integer
		Dim cmd = conn.CreateCommand()
		BuildQueryString(cmd, "select * from songs where path = ?", New Object() {path})
		Dim reader = cmd.ExecuteReader()
		If reader.Read() Then
			Dim id = reader(0)
			reader.Close()
			conn.Close()
			Return id
		Else
			reader.Close()
			Return Nothing
		End If
	End Function

	'TODO: This Function has NOT been debugged!
	Public Shared Function GetSong_id(Optional ByVal songName As String = Nothing, Optional ByVal songArtist As String = Nothing) As List(Of Integer)
		Dim lstId As New List(Of Integer)
		Dim conn = Connect()
		Dim cmd = New SQLiteCommand(conn)
		If Not songArtist = Nothing And Not songName = Nothing Then
			cmd.CommandText = String.Format("select id from songs where title = '{0}' and artists = '{1}'", songName, songArtist)
		ElseIf songArtist = Nothing And Not songName = Nothing Then
			cmd.CommandText = String.Format("select id from songs where title = '{0}'", songName)
		ElseIf songArtist = Nothing And songName = Nothing Then
			cmd.CommandText = String.Format("select id from songs")
		End If
		Dim r = cmd.ExecuteReader
		While r.Read()
			lstId.Add(Int(r("id")))
		End While
		conn.Close()
		Return lstId
	End Function

	'TODO: This Sub has NOT been debugged!
	Public Shared Sub UpdatePath(ByVal songId As Integer, ByVal songPath As String)
		Dim conn = Connect()
		Dim cmd = New SQLiteCommand(conn)
		cmd.CommandText = String.Format("update songs set path='{0}' where song_id={1}", songPath, songId)
	End Sub

	'TODO: This Function has NOT been debugged!
	Public Shared Function GetRowsCount()
		Dim conn = Connect()
		Dim cmd = New SQLiteCommand(conn)
		cmd.CommandText = String.Format("select count(*) from songs")
		Dim r = cmd.ExecuteReader
		Return Int(r.Read())
	End Function

End Class