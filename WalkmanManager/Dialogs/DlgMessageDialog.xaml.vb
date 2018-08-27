Public Class DlgMessageDialog
	Public Sub New(title As String, content As String)

		' 此调用是设计器所必需的。
		InitializeComponent()

		' 在 InitializeComponent() 调用之后添加任何初始化。
		TextBlockTitle.Text = title
		LabelContent.Content = content
	End Sub

	Public Sub ChangeColorTheme(brush As SolidColorBrush)
		ColorZoneTitle.Background = brush
		ButtonClose.Foreground = brush
		ButtonOk.Foreground = brush
	End Sub

End Class
