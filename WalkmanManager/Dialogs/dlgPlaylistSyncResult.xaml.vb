Public Class dlgPlaylistSyncResult
	Public Sub New(result As String)

		' 此调用是设计器所必需的。
		InitializeComponent()

		' 在 InitializeComponent() 调用之后添加任何初始化。
		TextBoxResult.Text = result
	End Sub
End Class
