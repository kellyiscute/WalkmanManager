Public Class DlgCloudMusicLogin

	Public Property Phone() As String
	Public Property Password() As String

	Private Sub TextBoxPhone_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextBoxPhone.TextChanged
		Phone = TextBoxPhone.Text
		If Phone = "" Then
			ButtonLogin.IsEnabled = False
		ElseIf Password <> "" Then
			ButtonLogin.IsEnabled = True
		End If
	End Sub

	Private Sub TextBoxPassword_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextBoxPassword.TextChanged
		Password = TextBoxPassword.Text
		If Password = "" Then
			ButtonLogin.IsEnabled = False
		ElseIf Phone <> "" Then
			ButtonLogin.IsEnabled = True
		End If
	End Sub
End Class
