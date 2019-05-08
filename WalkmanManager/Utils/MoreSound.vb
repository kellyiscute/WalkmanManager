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

	Private Function InitWebClient() As WebClient
		Dim webCli As New WebClient
		webCli.Headers.Add(HttpRequestHeader.UserAgent, UserAgent)
		Return webCli
	End Function

	Public Function Search(method As SearchMethod, query As String, Optional page As Integer = 1) As Dictionary(Of String, Object)
		Dim webcli = InitWebClient()

		webcli.QueryString = New Specialized.NameValueCollection From {{"search", method.GetName(GetType(SearchMethod), method)}}
		Dim responseBytes = webCli.UploadValues(ApiUrl, New Specialized.NameValueCollection From {{"w", query}, {"p", page}, {"n", 20}})
		Dim responseString = Encoding.UTF8.GetString(responseBytes)
		Dim deserializedResponse = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(responseString)

		webCli.Dispose()
		Return deserializedResponse
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

End Class
