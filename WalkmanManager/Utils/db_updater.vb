Imports System.Data.SQLite
Imports ATL
Imports ATL.AudioData
Imports Microsoft.VisualBasic.FileIO
Imports WalkmanManager.Database

Public Class DbUpdater

	Public Property ProgIndicator As Integer

	''' <summary>
	''' find songs added to the directory but not in database
	''' </summary>
	''' <param name="songDir">songs directory</param>
	''' <returns>songs added {Song_dir: Song_name}</returns>
	Public Async Function FindNew(songDir As String) As Task(Of List(Of String))
		Console.WriteLine("search Dir: " & songDir)
		Dim songFiles = GetFiles(songDir)
		Dim dbConn = Database.Connect()
		Dim trans = dbConn.BeginTransaction()
		Dim cmd = New SQLiteCommand(dbConn)
		cmd.Transaction = trans
		Dim lstNew As New List(Of String)
		For Each song In songFiles
			Dim t As New Track(song)
			BuildQueryString(cmd, "select count(*) from songs where path = ?", New Object() {song})
			Dim reader = cmd.ExecuteReader()
			reader.Read()
			If reader(0) = 0 Then
				reader.Close()
				AddSong(t.Title, t.Artist, song, cmd)
				Console.WriteLine("added :" & t.Artist & "-" & t.Title)
				lstNew.Add(song)
			End If
			reader.Close()
		Next
		trans.Commit()
		dbConn.Close()
		Return lstNew
	End Function

	Public Shared Function GetFiles(dir As String)
		'start adding file to list
		Dim lst As New List(Of String)
		For Each file In My.Computer.FileSystem.GetFiles(dir, SearchOption.SearchAllSubDirectories)
			If CheckExtention(file) Then
				lst.Add(file)
			End If
		Next
		Return lst
	End Function

	''' <summary>
	''' check if file is a audio file
	''' </summary>
	''' <param name="filename">path of file or the name with extention</param>
	''' <returns></returns>
	Public Shared Function CheckExtention(filename As String) As Boolean
		filename = filename.Split(".")(filename.Split(".").Count - 1)
		Console.WriteLine("[ Ext]" & filename)
		'this list is format that are supported by nw-a45 || http://helpguide.sony.net/dmp/nwa40/v1/zh-cn/contents/TP0001449595.html
		Dim audioExt As String() = {"mp3", "wma", "flac", "wav", "mp4", "m4a", "3gp", "aif", "aiff", "afc", "aifc", "dsf", "dff", "ape", "mqa", "flac"}
		Console.WriteLine("[IsAu]" & audioExt.Contains(filename))
		Return audioExt.Contains(filename)
	End Function

	Public Async Function FindLost() As Task(Of List(Of String))
		Dim lstLost As New List(Of String)
		Dim lstRm As New List(Of String)
		Dim conn = Connect()
		Dim cmd = conn.CreateCommand()
		cmd.CommandText = "select * from songs"
		Dim reader = cmd.ExecuteReader()
		While reader.Read()
			Dim path = reader("path")
			Console.WriteLine("[ Read ] " & path)
			If Not My.Computer.FileSystem.FileExists(path) Then
				lstLost.Add(reader("artists") & " - " & reader("title"))
				lstRm.Add(reader("id"))
			End If
		End While
		reader.Close()

		Dim trans = conn.BeginTransaction()
		cmd.Transaction = trans
		For Each rm In lstRm

			Console.WriteLine("[Remove] " & rm)
			RemoveSongFromLib(rm, cmd)
		Next
		trans.Commit()

		conn.Close()
		Return lstLost
	End Function
End Class
