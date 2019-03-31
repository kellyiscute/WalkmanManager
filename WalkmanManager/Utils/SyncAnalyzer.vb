Imports WalkmanManager.Database
Imports WalkmanManager.CloudMusic
Imports System.IO
Imports System.Security.Cryptography
Imports System.Data.SQLite

Public Class SyncAnalyzer
	Public Structure LostFileInfo
		Public Origin As String
		Public Remote As String
	End Structure

	Public Shared Function SyncPlaylist(playlistName As String, musicTracks As List(Of CloudMusicTracks),
										Optional dlg As dlg_progress = Nothing) As List(Of CloudMusicTracks)

		Dim lstFailed As New List(Of CloudMusicTracks)

		If Not IsNothing(dlg) Then
			dlg.Text = "Loading"
		End If

		If CheckPlaylistNameAvailability(playlistName) Then
			'Playlist does not exist locally
			AddPlaylist(playlistName)
		Else
			'Clear
			ClearPlaylist(GetPlaylistIdByName(playlistName))
		End If

		Dim id = GetPlaylistIdByName(playlistName)
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		Dim trans = conn.BeginTransaction()
		cmd.Transaction = trans
		Dim counter = 0
		Dim currentProgress = 0
		For Each track As CloudMusicTracks In musicTracks
			Dim path = SongExists(track.Title, track.Artists, cmd)
			If path.Length = 0 Then
				lstFailed.Add(track)
			Else
				Dim songId = GetSongId(path, cmd)
				AddSongToPlaylist(id, songId, cmd, counter)
				counter += 1
			End If
			currentProgress += 1
			If Not IsNothing(dlg) Then
				dlg.Text = String.Format("{0}/{1}", currentProgress, musicTracks.Count)
			End If
		Next
		trans.Commit()
		conn.Close()

		Return lstFailed
	End Function

	Public Shared Function SyncAllPlaylists(cloudMusicObject As CloudMusic, Optional dlg As dlg_progress = Nothing) _
		As String
		Dim playlists = cloudMusicObject.GetPlaylists()
		If Not IsNothing(dlg) Then
			dlg.Text = "获取歌单"
		End If
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		Dim trans = conn.BeginTransaction()
		cmd.Transaction = trans

		Dim rString As String = ""
		Dim currentProgress = 0

		For i = 0 To playlists.Count - 1

			Dim playlistName = playlists(i)("name")
			Dim lstFailed As New List(Of CloudMusicTracks)
			Dim counter = 0

			Dim id
			If CheckPlaylistNameAvailability(playlistName, cmd) Then
				'Playlist does not exist locally
				AddPlaylist(playlistName, cmd)
				id = GetPlaylistIdByName(playlistName, cmd)
			Else
				'Clear
				id = GetPlaylistIdByName(playlistName, cmd)
				ClearPlaylist(id, cmd)
			End If
			trans.Commit()
			trans = conn.BeginTransaction
			cmd.Transaction = trans

			Dim plDetail = cloudMusicObject.GetPlaylistDetail(playlists(i)("id"))

			If Not IsNothing(dlg) Then
				dlg.Text = String.Format("{0}/{1}", currentProgress, playlists.Count)
			End If

			For Each d As CloudMusicTracks In plDetail("tracks")
				Dim path = SongExists(d.Title, d.Artists, cmd)
				If path.Length = 0 Then
					lstFailed.Add(d)
				Else
					Dim songId = GetSongId(path, cmd)
					Try
						AddSongToPlaylist(id, songId, cmd, counter, True)
					Catch
						Console.WriteLine(String.Format("{0}: {1}", id, songId))
					End Try
					counter += 1
				End If
			Next

			rString += "同步 """ & playlistName & """ 完成" & vbTab & "失败 " & lstFailed.Count & " 个"
			rString += vbNewLine & "============================================"
			For Each itmCloudMusicTracks As CloudMusicTracks In lstFailed
				rString += vbNewLine & itmCloudMusicTracks.Title & " - " & itmCloudMusicTracks.Artists
			Next
			rString += vbNewLine

			currentProgress += 1
		Next

		trans.Commit()
		conn.Close()

		Return rString
	End Function

	Public Shared Function ChangePath(filename As String, newDirPath As String) As String
		filename = filename.Split("\").Last
		If Not My.Computer.FileSystem.DirectoryExists(newDirPath) Then
			Return ""
		End If
		If Not newDirPath.EndsWith("\") Then
			newDirPath += "\"
		End If

		Return newDirPath & filename
	End Function

	Public Shared Function FindDeleted(pathOnRemoteDrive As String, files As ICollection(Of SongInfo)) As List(Of String)
		Dim lstResult As New List(Of String)

		For Each f In My.Computer.FileSystem.GetFiles(pathOnRemoteDrive)
			If (From path In files Where path.Path = f Select path).Count = 0 Then
				'If file is not found in playlist
				lstResult.Add(f)
			End If
		Next

		Return lstResult
	End Function

	Private Shared Function GetFileHash(path As String) As String
		Dim result As String

		Try
			Dim r = New BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read))
			Dim data = r.ReadBytes(r.BaseStream.Length)
			r.Close()
			Dim md5 = New MD5CryptoServiceProvider()
			Dim hashData = md5.ComputeHash(data)
			result = BitConverter.ToString(hashData)
			Dim hash = Nothing
		Catch ex As Exception
			Return ""
		End Try

		Return result
	End Function

	Public Shared Function FindChangedFiles(pathOnRemoteDrive As String, lstSongs As ICollection(Of SongInfo),
											flgMd5Check As Boolean) As ICollection(Of String)
		Dim lstResult As New List(Of String)

		For Each s In lstSongs
			If My.Computer.FileSystem.FileExists(ChangePath(s.Path, pathOnRemoteDrive)) Then
				If flgMd5Check Then
					If Not GetFileHash(ChangePath(s.Path, pathOnRemoteDrive)) = GetFileHash(s.Path) Then
						lstResult.Add(s.Path)
					End If
				End If
			Else
				lstResult.Add(s.Path)
			End If
		Next

		Return lstResult
	End Function
End Class
