Public Module StringExtensionMethods
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
End Module