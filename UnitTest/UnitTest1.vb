Imports ATL
Imports Microsoft.VisualBasic.FileIO
Imports System.Data.SQLite
Imports WalkmanManager
Imports WalkmanManager.MoreSound
Imports WalkmanManager.CloudMusic

<TestClass()> Public Class UnitTest1

	Async Function async_demo(arg1 As String) As Task(Of String)
		'System.Threading.Thread.Sleep(5000)
		Return arg1 & " sb"
	End Function

	<TestMethod()> Public Sub EncDecTest()
		Dim enc = Database.EncryptString("123456", "456789")
		Console.WriteLine("encrypted: " + enc)
		Dim dec = Database.DecryptString("123456", enc)
		Console.WriteLine("decrypt1: " + dec)
		dec = Database.DecryptString("123", enc)
		Console.WriteLine("decrypt2: " + dec)

	End Sub

	<TestMethod()> Public Async Function TestMethod1() As Task
		Trace.WriteLine("Async method start")
		Dim sb = Await async_demo("Kristen")
		Trace.WriteLine("Function returned")
		Trace.Write(sb)
	End Function

	<TestMethod()> Public Sub ATL_Test()

		For Each file In My.Computer.FileSystem.GetFiles("C:\Users\guoji\music", SearchOption.SearchAllSubDirectories)
			Dim ext = file.Split(".")(file.Split(".").Count - 1)
			'this list is format that are supported by nw-a45 || http://helpguide.sony.net/dmp/nwa40/v1/zh-cn/contents/TP0001449595.html
			Dim audioExt As String() = {"mp3", "wma", "flac", "wav", "mp4", "m4a", "3gp", "aif", "aiff", "afc", "aifc", "dsf", "dff", "ape", "mqa", "flac"}
			'If audioExt.Contains(ext) Then
			Dim tr As New Track(file)
			Dim r As String
			r = String.Format("-----------{0}-----------", Dir(file)) & vbNewLine
			r += "Title: " & tr.Title & vbNewLine
			r += "Artist: " & tr.Artist & vbNewLine
			r += "-----------------------------------------" & vbNewLine
			Console.WriteLine(r)
			'End If
		Next

	End Sub

	<TestMethod> Public Sub ParamQueryTest()
		Dim connstrBuilder = New SQLiteConnectionStringBuilder
		connstrBuilder.DataSource = "data.db"
		Dim conn = New SQLiteConnection(connstrBuilder.ConnectionString)
		conn.Open()
		Dim cmd = conn.CreateCommand()
		cmd.CommandText = "drop table settings"
		cmd.ExecuteNonQuery()
		cmd.CommandText = "create table settings(key text, value text)"
		cmd.ExecuteNonQuery()
		cmd.CommandText = "insert into settings values('sb', 'sb1')"
		cmd.ExecuteNonQuery()
		Dim key As String = "sb"
		Dim value As String = "sb1"
		cmd.BuildQuery("select * from settings where key = ? and value = ?", New Object() {key, value})
		Debug.Print(cmd.CommandText)
		Dim r = cmd.ExecuteReader()
		If r.HasRows Then
			r.Read()
			Console.WriteLine(r(0))
			Console.WriteLine(r(1))
		Else
			Console.WriteLine("Empty")
		End If
		r.Close()
		conn.Close()
	End Sub

	<TestMethod> Public Sub TypeTest()
		Console.WriteLine(3333.GetType())
		Console.WriteLine(2333.GetTypeCode() = TypeCode.Int32)
	End Sub

	<TestMethod()> Public Sub FileExistTest()
		Console.WriteLine(My.Computer.FileSystem.FileExists("ATL.dll"))
	End Sub

	<TestMethod()> Public Sub GetLostFiles()
		Dim upd As New DbUpdater
		Dim lst = upd.FindLost().Result
		Console.WriteLine("---------------------Result---------------------")
		For Each l In lst
			Console.WriteLine(l)
		Next
	End Sub

	<TestMethod> Public Sub FindNewFiles()
		Dim upd As New DbUpdater
		Console.WriteLine(Database.GetSetting("song_dir"))
		Dim lstNew = upd.FindNew(Database.GetSetting("song_dir")).Result
		For Each itm In lstNew
			Console.WriteLine(itm)
		Next
	End Sub

	<TestMethod()> Public Sub CheckExt()
		Dim lst = DbUpdater.GetFiles(Database.GetSetting("song_dir"))
		Console.WriteLine("---------------------Result---------------------")
		For Each itm In lst
			Console.WriteLine(itm)
		Next
	End Sub

	<TestMethod()> Public Sub linqTest()
		Dim lst As String() = {"Burke", "Connor", "Frank",
							   "Everett", "Albert", "George",
							   "Harris", "David"}
		Dim query = From n In lst Where n.Contains(New String() {"e", "g", "o"}) Select n
		For Each itm As String In query
			Console.WriteLine(itm)
		Next
	End Sub

	<TestMethod()> Public Sub MakeDatabase()
		Database.CreateDatabase()
	End Sub

	<TestMethod()> Public Sub GetSongs()
		Dim lstSongs = Database.GetSongs()
		For Each s In lstSongs
			Console.WriteLine("------------------------------------------------------")
			Console.WriteLine("| Id     : " & s.Id)
			Console.WriteLine("| Title  : " & s.Title)
			Console.WriteLine("| Artists: " & s.Artists)
			Console.WriteLine("| Path   : " & s.Path)
			Console.WriteLine("------------------------------------------------------")
		Next
	End Sub

	<TestMethod()> Public Sub String_Extention_ToStringArray()
		Dim str = "abcdefghijklmnopqrstuvwxyz"
		Dim array = str.ToStringArray()
		For Each a In array
			Console.WriteLine(a)
		Next
	End Sub

	<TestMethod()> Public Sub CloudMusicLogin()
		Dim a As New CloudMusic
		Dim r = a.Login("", "")

		For Each k In r.Keys
			Console.WriteLine(String.Format("{0}: {1}", k, r(k)))
		Next
		If r("success") Then
			Console.WriteLine("")
			Dim playlists = a.GetPlaylists()
			For Each p In playlists
				For Each k In p.Keys
					Console.WriteLine(String.Format("{0}: {1}", k, p(k)))
				Next
				Console.WriteLine("==========================================")
			Next
		End If
	End Sub

	<TestMethod()> Public Sub MoreSoundTest()
		Dim ms As New MoreSound
		Dim result = ms.Search(SearchMethod.qq, "asf")
		Console.WriteLine(result)
		For Each r In result
			Console.WriteLine(r.ToString())
		Next
	End Sub

	<TestMethod> Public Sub ThirdPartySearchTest()
		Dim tpApi As New ThridPartyCloudMusicApi
		Dim a = tpApi.Search("哈尼 一口甜")
		For Each b In a
			Console.WriteLine(b.ToString)
		Next
	End Sub

End Class