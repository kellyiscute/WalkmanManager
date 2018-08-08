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
End Module