Imports System.IO
Imports System.Net
Imports System.Numerics
Imports System.Text
Imports system.Security.Cryptography
Imports Newtonsoft.Json


''' <summary>
''' This class is translated from C# by the author
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

	Const Cookie =
		"os=pc;osver=Microsoft-Windows-10-Professional-build-16299.125-64bit;appver=2.0.3.131777;channel=netease;__remember_me=true"

	Const Referer = "http://music.163.com/"
	Private _secretKey As string
	Private _encSecKey As string

#End Region

	Sub new
		_secretKey = CreateSecretKey(16)
		_encSecKey = RSAEncode(_secretKey)
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

	Public Function Prepare(raw As string) As Dictionary(Of string, string)
		Dim data = New Dictionary(Of String, string)
		data("params") = AESEncode(raw, Nonce)
		data("params") = AESEncode(data("params"), _secretKey)
		data("encSecKey") = _encSecKey
		Return data
	End Function

	Public Function BcHexDec(hex As String) As BigInteger
		Dim dec As new BigInteger(0)
		For i = 0 To Hex.Length
			dec += BigInteger.Multiply(New BigInteger(Convert.ToInt32(Hex(i).ToString, 16)),
			                           BigInteger.Pow(New BigInteger(16), Hex.Length - i - 1))
		Next
		Return dec
	End Function

	Public Function RsaEncode(text As String) As String
		Dim srtext = New String(text.Reverse)
		Dim a = BCHexDec(BitConverter.ToString(Encoding.Default.GetBytes(srtext)).Replace("-", ""))
		Dim b = BCHexDec(Pubkey)
		dim c = BCHexDec(Modulus)
		Dim key = BigInteger.ModPow(a, b, c).ToString("x")
		key = key.PadLeft(256, "0")
		If key.Length > 256
			Return key.Substring(key.Length - 256, 256)
		Else
			Return key
		End If
	End Function

	Public Function AesEncode(secretData As String, Optional secret As String = "TA3YiYCfY2dDJQgg") As String
		Dim encrypted() as Byte
		Dim iv() as Byte = Encoding.UTF8.GetBytes(Vi)

		Using aes As Aes = aes.Create()
			aes.Key = Encoding.UTF8.GetBytes(secret)
			aes.IV = IV
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
		Dim encrypted() as Byte
		Dim iv() as Byte = Encoding.UTF8.GetBytes(Vi)

		Using aes As Aes = aes.Create()
			aes.Key = Encoding.UTF8.GetBytes(secret)
			aes.IV = IV
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
	Public Function Curl(url As String, parms As Dictionary(Of String, string), Optional method As String = "POST") _
		As String
		Dim result As String
		Using wc = New WebClient
			wc.Headers.Add(HttpRequestHeader.ContentType, "application/w-www-form-urlencoded")
			wc.Headers.Add(HttpRequestHeader.Referer, Referer)
			wc.Headers.Add(HttpRequestHeader.UserAgent, Useragent)
			wc.Headers.Add(HttpRequestHeader.Cookie, Cookie)
			Dim reqparm = New System.Collections.Specialized.NameValueCollection()
			For Each keyPair As KeyValuePair(Of String,String) In parms
				reqparm.Add(keyPair.Key, keyPair.Value)
			Next

			Dim responsebytes = wc.UploadValues(url, method, reqparm)
			result = Encoding.UTF8.GetString(responsebytes)
		End Using
		Return result
	End Function

	Public Class SearchJson
		Public S As String
		Public Type As Integer
		Public Limit As Integer
		Public Total as String = "true"
		Public Offset As Integer
		Public CsrfToken As String = ""
	End Class

	Public enum SearchType
		Song = 1
		Album = 10
		Artist = 100
		Playlist = 1000
		User = 1002
		Radio = 1009
	End Enum

	Public Function Search(keyword as string, Optional limit As Integer = 30, Optional offset As Integer = 0,
	                       Optional type As SearchType = SearchType.Song) As SearchResult
		Dim url = "http://music.163.com/weapi/cloudsearch/get/web"
		Dim data = New SearchJson With {
			    .s = keyword,
			    .type = type,
			    .limit = limit,
			    .offset = offset
			    }
		Dim raw As String = CURL(url, Prepare(JsonConvert.SerializeObject(data)))
		Dim deserializedObj = JsonConvert.DeserializeObject (Of SearchResult)(raw)
		Return deserializedObj
	End Function
End Class

