Imports System.IO
Imports System.Net
Imports System.Numerics
Imports System.Text
Imports system.Security.Cryptography
Imports Newtonsoft.Json


''' <summary>
''' Partof this class is translated from C# by the author
''' The origional file is written by GEEKiDoS and can be found by this link https://github.com/GEEKiDoS/NeteaseMuiscApi/blob/master/NeteaseCloudMuiscApi.cs
''' The file is licensed under the MIT License, written below
''' 
''' MIT License
''' Copyright (c) 2018 GEEKiDoS
''' Permission is hereby granted, free of charge, to any person obtaining a copy
''' of this software and associated documentation files (the "Software"), to deal
''' in the Software without restriction, including without limitation the rights
''' to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
''' copies of the Software, and to permit persons to whom the Software is
''' furnished to do so, subject to the following conditions
''' The above copyright notice and this permission notice shall be included in all
''' copies or substantial portions of the Software.
''' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS O
''' IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
''' FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL TH
''' AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
''' LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
''' OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
''' SOFTWARE.
'''
''' </summary>

Public Class CloudMusic

#Region "Constants"

	Const Modulus =
		"00e0b509f6259df8642dbc35662901477df22677ec152b5ff68ace615bb7b725152b3ab17a876aea8a5aa76d2e417629ec4ee341f56135fccf695280104e0312ecbda92557c93870114af6c9d05c4f7f0c3685b7a46bee255932575cce10b424d813cfe4875d3e82047b97ddef52741d546b8e289dc6935b3ece0462db0a22b8e7"

	Const Nonce = "0CoJUm6Qyw8W8jud"
	Const Pubkey = "010001"
	Const Vi = "0102030405060708"

	Const Useragent =
		"Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36"

	Dim _cookie As New CookieContainer

	Const Referer = "http://music.163.com/"
	Private ReadOnly _secretKey As String
	Private ReadOnly _encSecKey As String

	Public Playlists

	Public Property UserInfo As Dictionary(Of String, Object)

#End Region

	Sub New()
		_secretKey = CreateSecretKey(16)
		_encSecKey = RsaEncode(_secretKey)
		_cookie.Add(New Cookie With {.Name = "os", .Value = "pc", .Domain = "music.163.com"})
		_cookie.Add(New Cookie With {.Name = "osver", .Value = "Ubuntu 18.04", .Domain = "music.163.com"})
		_cookie.Add(New Cookie With {.Name = "appver", .Value = "2.0.3.131777", .Domain = "music.163.com"})
		_cookie.Add(New Cookie With {.Name = "channel", .Value = "netease", .Domain = "music.163.com"})
	End Sub

	Structure CloudMusicTracks
		Property Title As String
		Property Artists As String
		Property Album As String
	End Structure

	Private Function CreateSecretKey(length As Integer) As String
		Dim str = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
		Dim r = ""
		Dim rnd = New Random()
		For i = 1 To length
			r += str(rnd.Next(0, str.Length - 1))
		Next
		Return r
	End Function

	Private Function Prepare(raw As String) As Dictionary(Of String, String)
		Dim data = New Dictionary(Of String, String)
		data("params") = AesEncode(raw, Nonce)
		data("params") = AesEncode(data("params"), _secretKey)
		data("encSecKey") = _encSecKey
		Return data
	End Function

	Private Function BcHexDec(hex As String) As BigInteger
		Dim dec As New BigInteger(0)
		For i = 0 To hex.Length - 1
			dec += BigInteger.Multiply(New BigInteger(Convert.ToInt32(hex(i).ToString, 16)),
									   BigInteger.Pow(New BigInteger(16), hex.Length - i - 1))
		Next
		Return dec
	End Function

	Private Function RsaEncode(text As String) As String
		Dim srtext = New String(text.Reverse.ToArray())
		Dim a = BcHexDec(BitConverter.ToString(Encoding.Default.GetBytes(srtext)).Replace("-", ""))
		Dim b = BcHexDec(Pubkey)
		Dim c = BcHexDec(Modulus)
		Dim key = BigInteger.ModPow(a, b, c).ToString("x")
		key = key.PadLeft(256, "0")
		If key.Length > 256 Then
			Return key.Substring(key.Length - 256, 256)
		Else
			Return key
		End If
	End Function

	Private Function AesEncode(secretData As String, Optional secret As String = "TA3YiYCfY2dDJQgg") As String
		Dim encrypted() As Byte
		Dim iv() As Byte = Encoding.UTF8.GetBytes(Vi)

		Using aes As Aes = Aes.Create()
			aes.Key = Encoding.UTF8.GetBytes(secret)
			aes.IV = iv
			aes.Mode = CipherMode.CBC
			Using encryptor = aes.CreateEncryptor()
				Using stream = New MemoryStream()
					Using cstream = New CryptoStream(stream, encryptor, CryptoStreamMode.Write)
						Using sw = New StreamWriter(cstream)
							sw.Write(secretData)
						End Using
						encrypted = stream.ToArray()
					End Using
				End Using
			End Using
		End Using
		Return Convert.ToBase64String(encrypted)
	End Function

	Public Function AesDecode(secretData As String, Optional secret As String = "TA3YiYCfY2dDJQgg") As String
		Dim encrypted() As Byte
		Dim iv() As Byte = Encoding.UTF8.GetBytes(Vi)

		Using aes As Aes = Aes.Create()
			aes.Key = Encoding.UTF8.GetBytes(secret)
			aes.IV = iv
			aes.Mode = CipherMode.CBC
			Using encrypt = aes.CreateDecryptor()
				Using stream = New MemoryStream()
					Using cStream = New CryptoStream(stream, encrypt, CryptoStreamMode.Write)
						Using sw = New StreamWriter(cStream)
							sw.Write(secretData)
						End Using
						encrypted = stream.ToArray()
					End Using
				End Using
			End Using
		End Using
		Return Encoding.UTF8.GetString(encrypted)
	End Function

	'fake curl
	Private Function Curl(url As String, parms As Dictionary(Of String, String), Optional method As String = "POST") _
		As String
		Dim result As String
		Using wc = New CookieAwareWebClient
			wc.Headers.Add(HttpRequestHeader.Referer, Referer)
			wc.Headers.Add(HttpRequestHeader.UserAgent, Useragent)
			wc.CookieContainer = _cookie
			Dim reqParam = New Specialized.NameValueCollection()
			For Each keyPair As KeyValuePair(Of String, String) In parms
				reqParam.Add(keyPair.Key, keyPair.Value)
			Next

			Dim responseBytes = wc.UploadValues(url, method, reqParam)

			Debug.Print("=================Cookies=================")
			For i = 0 To wc.ResponseCookies.Count - 1
				_cookie.Add(wc.ResponseCookies.Item(i))
			Next

			result = Encoding.UTF8.GetString(responseBytes)
		End Using
		Return result
	End Function

	Public Function Login(phone As String, password As String) As Dictionary(Of String, Object)
		Dim api = New CloudMusic
		Dim params = New Dictionary(Of String, String)()
		params("phone") = phone
		Dim md5Pwd = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(password))
		Dim b As New StringBuilder
		For i = 0 To md5Pwd.Length - 1
			b.Append(md5Pwd(i).ToString("X2"))
		Next
		params("password") = b.ToString().ToLower
		Dim j = JsonConvert.SerializeObject(params)
		Debug.Print(j)
		Dim r = api.Curl("https://music.163.com/weapi/login/cellphone/", api.Prepare(j))

		Dim returnJson = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(r)
		If returnJson.Keys.Contains("msg") Then
			If returnJson("msg").contains("密码错误") Then
				Dim re = New Dictionary(Of String, Object)
				re("msg") = returnJson("msg")
				re("success") = False
				Return re
			Else
				Dim re = New Dictionary(Of String, Object)
				If returnJson.Keys.Contains("msg") Then
					re("msg") = returnJson("msg")
				Else
					re("msg") = "Unknown Error"
				End If
				re("success") = False
				Return re
			End If
		Else
			Dim re = New Dictionary(Of String, Object)
			re("msg") = ""
			re("success") = True
			re("id") = returnJson("account")("id")
			re("avatarUrl") = returnJson("profile")("avatarUrl")
			re("nickname") = returnJson("profile")("nickname")
			UserInfo = re
			Return re
		End If
	End Function

	Public Overloads Function GetPlaylists(customUid As String, Optional offset As Integer = 0,
										   Optional limit As Integer = 100) As List(Of Dictionary(Of String, Object))
		Dim params = New Dictionary(Of String, String)()
		params("offset") = offset
		params("limit") = limit
		params("uid") = customUid
		Dim r = Curl("https://music.163.com/weapi/user/playlist", Prepare(JsonConvert.SerializeObject(params)))
		Dim cloudMusicDeserialize = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(r)
		Dim result As New List(Of Dictionary(Of String, Object))
		For Each playlist In cloudMusicDeserialize("playlist") 'As Dictionary(Of String, Object)
			Dim p As New Dictionary(Of String, Object)
			p("id") = playlist("id").ToString
			p("name") = playlist("name").ToString
			p("coverImgUrl") = playlist("coverImgUrl").ToString
			result.Add(p)
		Next
		Playlists = result
		Return result
	End Function

	''' <summary>
	''' Get playlists
	''' </summary>
	''' <param name="offset">offset default: 0</param>
	''' <param name="limit">limit default: 100</param>
	''' <returns>
	''' {
	'''		"id"
	'''		"name"
	'''		"coverImgUrl"
	''' }
	''' </returns>
	Public Overloads Function GetPlaylists(Optional offset As Integer = 0, Optional limit As Integer = 100) _
		As List(Of Dictionary(Of String, Object))

		Dim params = New Dictionary(Of String, String)()
		params("offset") = offset
		params("limit") = limit
		params("uid") = UserInfo("id")
		Dim r = Curl("https://music.163.com/weapi/user/playlist", Prepare(JsonConvert.SerializeObject(params)))
		Dim cloudMusicDeserialize = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(r)
		Dim result As New List(Of Dictionary(Of String, Object))
		For Each playlist In cloudMusicDeserialize("playlist") 'As Dictionary(Of String, Object)
			Dim p As New Dictionary(Of String, Object)
			p("id") = playlist("id").ToString
			p("name") = playlist("name").ToString
			p("coverImgUrl") = playlist("coverImgUrl").ToString
			result.Add(p)
		Next
		Playlists = result
		Return result
	End Function

	''' <summary>
	''' Get Songs in Playlist
	''' </summary>
	''' <param name="id">playlist ID</param>
	''' <returns>
	''' {
	'''		"name"
	'''		"coverImgUrl"
	'''		"tracks" : [
	'''			{
	'''			"title"
	'''			"artists"
	'''			"album"
	'''			"picUrl"
	'''			}
	'''		]
	''' }
	''' </returns>
	Public Function GetPlaylistDetail(id As Long) As Dictionary(Of String, Object)
		Dim params = New Dictionary(Of String, String)()
		Dim r = Curl("http://music.163.com/api/playlist/detail?id=" & id.ToString, params)
		Dim cloudMusicDeserialize = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(r)
		Dim result As New Dictionary(Of String, Object)
		'Read Basic Info (in /result: Object)
		result("name") = cloudMusicDeserialize("result")("name")
		result("coverImgUrl") = cloudMusicDeserialize("result")("coverImgUrl")
		'Read Tracks (in /result: Object/tracks: List, list of Objects)
		Dim tracks As New List(Of CloudMusicTracks)
		For Each track In cloudMusicDeserialize("result")("tracks")
			Dim t As CloudMusicTracks
			'Read Track Name
			t.Title = track("name")
			'Read Track Artists
			t.Artists = ""
			For Each artist In track("artists")
				t.Artists += artist("name") & "/"
			Next
			'remove the last slash
			If t.Artists <> "" Then
				t.Artists = Mid(t.Artists, 1, t.Artists.Length - 1)
			End If
			'Read Album Info
			If track("album").Count <> 0 Then
				t.Album = track("album")("name")
			End If
			tracks.Add(t)
		Next
		result("tracks") = tracks
		cloudMusicDeserialize = Nothing
		Return result
	End Function
End Class

Public Class CookieAwareWebClient
	Inherits WebClient

	Public Property CookieContainer As CookieContainer
	Public Property ResponseCookies As CookieCollection

	Sub New()
		CookieContainer = New CookieContainer()
		ResponseCookies = New CookieCollection()
	End Sub

	Protected Overrides Function GetWebRequest(address As Uri) As WebRequest
		Dim request = CType(MyBase.GetWebRequest(address), HttpWebRequest)
		request.CookieContainer = CookieContainer
		Return request
	End Function

	Protected Overrides Function GetWebResponse(request As WebRequest) As WebResponse
		Dim response = CType(MyBase.GetWebResponse(request), HttpWebResponse)
		ResponseCookies = response.Cookies
		Return response
	End Function
End Class