Imports Newtonsoft.Json
Imports System.Net
Imports System.Text

Public Class MoreSound

	Const UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.131 Safari/537.36"
	Const ApiUrl = "http://moresound.tk/music/api.php"

	Public Enum SearchMethod
		qq
		wy
		kw
		kg
		bd
	End Enum

	Public Structure SongDetail
		Property albummid As String
		Property albumname As String
		Property songmid As String
		Property songname As String
		Property singers As List(Of SingerDetail)
	End Structure

	Public Structure SingerDetail
		Property singermid As String
		Property singername As String
	End Structure

	Public Structure CoverLyric
		Property Cover As String
		Property Lyric As String
	End Structure

	Private Function InitWebClient() As WebClient
		Dim webCli As New WebClient
		webCli.Headers.Add(HttpRequestHeader.UserAgent, UserAgent)
		Return webCli
	End Function

	''' <summary>
	''' Search From Moresound.tk
	''' </summary>
	''' <param name="method">search method</param>
	''' <param name="query">key words</param>
	''' <param name="page">page number(default = 1)</param>
	''' <returns>return nothing if failed</returns>
	Public Function Search(method As SearchMethod, query As String, Optional page As Integer = 1) As List(Of SongDetail)
		Dim webcli = InitWebClient()

		webcli.QueryString = New Specialized.NameValueCollection From {{"search", method.GetName(GetType(SearchMethod), method)}}
		Dim responseBytes = webcli.UploadValues(ApiUrl, New Specialized.NameValueCollection From {{"w", query}, {"p", page}, {"n", 20}})
		Dim responseString = Encoding.UTF8.GetString(responseBytes)
		Dim deserializedResponse = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(responseString)

		'check if failed
		If deserializedResponse.Keys.Contains("code") Then
			If deserializedResponse("code") <> 0 Then
				Return Nothing
			End If
		Else
			Return Nothing
		End If

		'Convert to object
		Dim songList As Newtonsoft.Json.Linq.JArray = deserializedResponse("song_list")
		Dim result As New List(Of SongDetail)
		For Each itm As Linq.JObject In songList
			Dim t As New SongDetail
			t.albummid = itm("albummid")
			t.albumname = itm("albumname")
			t.songmid = itm("songmid")
			Dim sn As String = itm("songname")
			'process songname, remove <sup> tag
			Dim supTagPos = sn.IndexOf("<sup")
			If supTagPos <> -1 Then
				sn = sn.Substring(0, supTagPos + 1)
			End If
			t.songname = sn

			'note: itm("singer") is JArray
			Dim singers As New List(Of SingerDetail)
			For Each s As Linq.JObject In itm("singer")
				Dim singer As New SingerDetail
				singer.singername = s("name")
				singer.singermid = s("mid")
				singers.Add(singer)
			Next
			t.singers = singers
		Next

		webcli.Dispose()
		Return result
	End Function

	Public Function GetSong(method As SearchMethod, mid As String) As Dictionary(Of String, Object)
		Dim webcli = InitWebClient()
		webcli.QueryString = New Specialized.NameValueCollection From {{"get_song", method.GetName(GetType(SearchMethod), method)}}
		Dim responseBytes = webcli.UploadValues(ApiUrl, New Specialized.NameValueCollection From {{"mid", mid}})
		Dim responseString = Encoding.UTF8.GetString(responseBytes)
		Dim deserializedResponse = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(responseString)

		webcli.Dispose()
		Return deserializedResponse
	End Function

	Public Function GetCoverAndLyric(method As SearchMethod, mid As String) As CoverLyric
		Dim response = GetSong(method, mid)
		Dim result As New CoverLyric
		Dim urls As Linq.JObject = response("url")
		If urls.ContainsKey("专辑封面") Then
			result.Cover = urls("专辑封面")
		End If
		If urls.ContainsKey("lrc") Then
			result.Cover = "http://moresound.tk/music/" & urls("lrc").ToString
		End If

		Return result
	End Function

End Class
