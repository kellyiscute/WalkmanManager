Imports System.Data
Imports System.Data.SQLite
Imports System.Net

Public Module ExtensionMethods
	Sub New()
	End Sub
	<System.Runtime.CompilerServices.Extension>
	Public Function Contains(ByVal str As String, ByVal values As String()) As Boolean
		Dim result As Integer
		For Each value In values
			If str.Contains(value) Then
				result += 1
			End If
		Next
		Return result = values.Count()
	End Function

	<System.Runtime.CompilerServices.Extension>
	Public Function ToStringArray(str As String) As String()
		Dim array(str.Length - 1) As String
		For i = 0 To str.Length - 1
			array(i) = str(i)
		Next
		Return array
	End Function

	<System.Runtime.CompilerServices.Extension>
	Public Function AllToLower(lst As IEnumerable(Of String)) As List(Of String)
		Dim result As New List(Of String)
		For Each s As String In lst
			result.Add(s.ToLower())
		Next
		Return result
	End Function

	<System.Runtime.CompilerServices.Extension>
	Public Function NameWithoutExtention(file As IO.FileInfo) As String
		Dim result = file.Name
		Dim sp = result.Split(".")
		result = ""
		For i = 0 To sp.Count-2
			result += sp(i) + "."
		Next
		result = result.Substring(0,result.Length-2)
		Return result
	End Function

	''' <summary>
	''' this method is to create a python-ish command builder, which is easier to use
	''' </summary>
	''' <param name="cmd">SQLiteCommand Object</param>
	''' <param name="sql">SQL Query string</param>
	''' <param name="params">SQL Query arguments</param>
	<System.Runtime.CompilerServices.Extension>
	Public Sub BuildQuery(ByRef cmd As SQLiteCommand, sql As String, Optional params As Object() = Nothing)
		'if no params was passed, set CommandText directly
		cmd.Parameters.Clear()
		If IsNothing(params) Then
			cmd.CommandText = sql
			Return
		End If

		Dim counter = 0
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

	<System.Runtime.CompilerServices.Extension>
	Public Function ToCookieString(lst As List(Of Cookie)) As String
		Dim r = ""
		For Each c In lst
			r += c.Name & "=" & c.Value & ";"
		Next
		Return r
	End Function

	<System.Runtime.CompilerServices.Extension>
	Public Sub AddOne(prog As ProgressBar)
		If prog.Value.Equals(prog.Maximum) Then
			prog.Value += 1
		End If
	End Sub

	<System.Runtime.CompilerServices.Extension>
	Public Sub AddOne(prog As ProgressBar, window As MainWindow)
		window.Dispatcher.Invoke(Sub()
									 If Not prog.Value = prog.Maximum Then
										 prog.Value += 1
									 End If
								 End Sub)
	End Sub

End Module