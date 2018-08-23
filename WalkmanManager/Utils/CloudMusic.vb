Imports System.IO
Imports System.Net
Imports System.Numerics
Imports System.Text
Imports system.Security.Cryptography
Imports Newtonsoft.Json


''' <summary>
''' Partof this class is translated from C# by the author
''' The origional file is written by GEEKiDoS and can be found by this link https://github.com/GEEKiDoS/NeteaseMuiscApi/blob/master/NeteaseCloudMuiscApi.cs
''' The file is licensed under the MIT License, written as below
''' MIT License
'''Copyright (c) 2018 GEEKiDoS
'''Permission is hereby granted, free of charge, to any person obtaining a copy
'''of this software and associated documentation files (the "Software"), to deal
'''in the Software without restriction, including without limitation the rights
'''to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
'''copies of the Software, and to permit persons to whom the Software is
'''furnished to do so, subject to the following conditions
'''The above copyright notice and this permission notice shall be included in all
'''copies or substantial portions of the Software.
'''THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS O
'''IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
'''FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL TH
'''AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
'''LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
'''OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
'''SOFTWARE.
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

	Dim Cookie As New CookieContainer

	Const Referer = "http://music.163.com/"
	Private _secretKey As String
	Private _encSecKey As String

#End Region

	Sub New()
		_secretKey = CreateSecretKey(16)
		_encSecKey = RsaEncode(_secretKey)
		Cookie.Add(New Cookie With {.Name = "os", .Value = "pc", .Domain = "music.163.com"})
		Cookie.Add(New Cookie With {.Name = "osver", .Value = "Ubuntu 18.04", .Domain = "music.163.com"})
		Cookie.Add(New Cookie With {.Name = "appver", .Value = "2.0.3.131777", .Domain = "music.163.com"})
		Cookie.Add(New Cookie With {.Name = "channel", .Value = "netease", .Domain = "music.163.com"})
	End Sub

	Public Function CreateSecretKey(length As Integer) As String
		Dim str = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"
		Dim r = ""
		Dim rnd = New Random()
		For i = 1 To length
			r += str(rnd.Next(0, str.Length - 1))
		Next
		Return r
	End Function

	Public Function Prepare(raw As String) As Dictionary(Of String, String)
		Dim data = New Dictionary(Of String, String)
		data("params") = AesEncode(raw, Nonce)
		data("params") = AesEncode(data("params"), _secretKey)
		data("encSecKey") = _encSecKey
		Return data
	End Function

	Public Function BcHexDec(hex As String) As BigInteger
		Dim dec As New BigInteger(0)
		For i = 0 To hex.Length - 1
			dec += BigInteger.Multiply(New BigInteger(Convert.ToInt32(hex(i).ToString, 16)),
										BigInteger.Pow(New BigInteger(16), hex.Length - i - 1))
		Next
		Return dec
	End Function

	Public Function RsaEncode(text As String) As String
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

	Public Function AesEncode(secretData As String, Optional secret As String = "TA3YiYCfY2dDJQgg") As String
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
			Using encryptor = aes.CreateDecryptor()
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
		Return Encoding.UTF8.GetString(encrypted)
	End Function

	'fake curl
	Public Function Curl(url As String, parms As Dictionary(Of String, String), Optional method As String = "POST") _
		As String
		Dim result As String
		Using wc = New CookieAwareWebClient
			wc.Headers.Add(HttpRequestHeader.Referer, Referer)
			wc.Headers.Add(HttpRequestHeader.UserAgent, Useragent)
			wc.CookieContainer = Cookie
			Dim reqparm = New System.Collections.Specialized.NameValueCollection()
			For Each keyPair As KeyValuePair(Of String, String) In parms
				reqparm.Add(keyPair.Key, keyPair.Value)
			Next

			Dim responsebytes = wc.UploadValues(url, method, reqparm)

			Debug.Print("=================Cookies=================")
			For i = 0 To wc.ResponseCookies.Count - 1
				'Cookie += ";" & wc.ResponseCookies.Item(i).Name & "="
				'Cookie += wc.ResponseCookies.Item(i).Value
				Cookie.Add(wc.ResponseCookies.Item(i))
			Next
			'Debug.Print(Cookie)

			result = Encoding.UTF8.GetString(responsebytes)
		End Using
		Return result
	End Function

	Public Class SearchJson
		Public S As String
		Public Type As Integer
		Public Limit As Integer
		Public Total As String = "true"
		Public Offset As Integer
		Public CsrfToken As String = ""
	End Class

	Public Enum SearchType
		Song = 1
		Album = 10
		Artist = 100
		Playlist = 1000
		User = 1002
		Radio = 1009
	End Enum

	Public Function Search(keyword As String, Optional limit As Integer = 30, Optional offset As Integer = 0,
							Optional type As SearchType = SearchType.Song) As SearchResult
		Dim url = "http://music.163.com/weapi/cloudsearch/get/web"
		Dim data = New SearchJson With {
				.S = keyword,
				.Type = type,
				.Limit = limit,
				.Offset = offset
				}
		Dim raw As String = Curl(url, Prepare(JsonConvert.SerializeObject(data)))
		Dim deserializedObj = JsonConvert.DeserializeObject(Of SearchResult)(raw)
		Return deserializedObj
	End Function

	Public Function Login(phone As String, password As String) 'As Dictionary(Of String, Object)
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

		'Return JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(r)
		Return r
	End Function

	Public Function GetPlaylists(uid As String, Optional offset As Integer = 0, Optional limit As Integer = 100)
		Dim params = New Dictionary(Of String, String)()
		params("offset") = offset
		params("limit") = limit
		params("uid") = uid
		Dim r = Curl("https://music.163.com/weapi/user/playlist", Prepare(JsonConvert.SerializeObject(params)))
		Return JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(r)
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