'TODO: The Class has NOT been debugged!
'[Warmings]: This Class is depent on the Class, [Database.vb](./Database.vb)
Public Class RefreshDatabase
    Private Structure Song
        Public Id As Integer
        Public Title As String
        '[Warnings]: song_artist here can be a Nothing
        Public Artist As String
        Public Path As String
    End Structure

    Private Shared _localSongList As New List(Of Song)
    Private Shared _databaseSongList As New List(Of Song)

    ''' <summary>
    ''' [Warnings]: song_artist here can be a Nothing
    ''' </summary>
    ''' <param name="songName"></param>
    ''' <param name="path"></param>
    ''' <param name="songArtist"></param>
    Public Sub Append(ByVal songName As String, ByVal path As String, Optional ByVal songArtist As String = Nothing)
        Dim song As Song
        song.title = songName
        song.path = path
        song.artist = songArtist
        song.id = Nothing
        _localSongList.Add(song)
    End Sub

    Public Sub Close()
        MyClass.Close()
    End Sub
    ''' <summary>
    ''' A built-in Database Manager to
    ''' Initialize Database to database_song_list
    ''' </summary>
    Public Sub ReadDatabase()
        _databaseSongList.Clear()
        Dim song As Song

        Dim connstrBuilder = New System.Data.SQLite.SQLiteConnectionStringBuilder
        connstrBuilder.DataSource = "data.db"
        Dim conn = New System.Data.SQLite.SQLiteConnection(connstrBuilder.ConnectionString)
        conn.Open()
        Dim cmd = New System.Data.SQLite.SQLiteCommand(conn)
        cmd.CommandText = String.Format("select id, title, artists, path from songs")
        Dim r = cmd.ExecuteReader
        'If r.HasRows Then
        '    For i = 0 To r.StepCount() - 1
        '        r.Read()
        '        song.id = r("id")
        '        song.title = r("title")
        '        song.artist = r("artists")
        '        song.path = r("path")
        '        database_song_list.Add(song)
        '    Next
        'Else
        '    conn.Close()
        '    Return False
        'End If
        'conn.Close()
        'Return True
        While r.Read()
            song.id = r("id")
            song.title = r("title")
            song.artist = r("artists")
            song.path = r("path")
            _databaseSongList.Add(song)
        End While
        conn.Close()
    End Sub

    ''' <summary>
    ''' Refreshs songs' path and database_song_list, and delects excess songs in database
    ''' </summary>
    Public Sub RefreshDatabase()
        Dim isExist As Boolean = False
        For Each database_song In _databaseSongList
            For Each local_song In _localSongList
                'mark down the both existing songs
                If database_song.title = local_song.title Then
                    If Not local_song.artist Or database_song.artist = local_song.artist Then
                        IsExist = True
                    End If
                    'refresh the path
                    If database_song.artist = local_song.artist And Not database_song.path = local_song.path Then
                        'maybe unnecessary
                        database_song.artist = local_song.artist
                        Database.UpdatePath(database_song.id, local_song.path)
                    End If
                    Exit For
                End If
            Next
            'delect excess songs in database
            If IsExist Then
                IsExist = False
            Else
                Database.RemoveSongFromLib(database_song.id)
            End If
        Next
        'refresh database_song_list
        ReadDatabase()
    End Sub

    '''' <returns>State that whether the function is still processing</returns>
    ''' <summary>
    ''' Add local songs to database
    ''' </summary>
    Public Sub AddSongsToDatabase()
        For Each local_song In _localSongList
            If Not Database.IsExist(local_song.title) Then
                If vbOK = MessageBox.Show(String.Format("Do you want to add the song *{0}*, which is composed by {1} and lies in path {2}, to database?", local_song.title, local_song.artist, local_song.path), "Decision Making", vbYesNo) Then
                    Database.AddSong(local_song.title, local_song.artist, local_song.path)
                End If
            End If
        Next
        'refresh database_song_list
        ReadDatabase()
        'Return True
    End Sub
    '''' <summary>
    '''' [Warnings]: The Function shares the same process of the same-name function above
    '''' </summary>
    '''' <returns></returns>
    'Public Function RefreshDatabase()
    '    Dim matched_song_id As New List(Of Integer)
    '    For Each local_song In local_song_list
    '        Dim tmp_matched_id_list As List(Of Integer) = Database.GetSong_id(local_song.title)
    '        For Each id In tmp_matched_id_list
    '            If Not matched_song_id.Contains(id) Then
    '                matched_song_id.Add(id)
    '            End If
    '        Next
    '    Next
    '    Dim database_id_list As List(Of Integer) = Database.GetSong_id()
    '    'reverse the local_song_list in the limitation of matched_song_id
    '    For Each database_id In database_id_list
    '        If matched_song_id.Contains(database_id) Then
    '            matched_song_id.Remove(database_id)
    '        Else
    '            matched_song_id.Add(database_id)
    '        End If
    '    Next
    '    For Each deleting_id In matched_song_id
    '        Database.RemoveSongFromLib(deleting_id)
    '    Next
    '    Return True
    'End Function

End Class
