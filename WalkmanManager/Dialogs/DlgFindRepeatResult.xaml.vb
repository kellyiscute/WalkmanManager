Imports MaterialDesignThemes.Wpf

Public Class DlgFindRepeatResult
    Public Sub New(info As Dictionary(Of String, List(Of MainWindow.FindRepeatFileInfo)))

        ' 此调用是设计器所必需的。
        InitializeComponent()

        ' 在 InitializeComponent() 调用之后添加任何初始化。
        For Each itm In info.Values
            Dim g = BuildGroup(itm)
            StackPanelContents.Children.Add(g)
        Next

    End Sub

    Private Function BuildGroup(song As List(Of MainWindow.FindRepeatFileInfo)) As Card
        Dim result As New Card With {.Margin = New Thickness(5, 30, 5, 5)}
        Dim resultContentStack As New StackPanel
        result.Content = resultContentStack
        For Each s In song
            Dim e As New Expander
            Dim fileInfo = My.Computer.FileSystem.GetFileInfo(s.SongInfo.Path)
            e.Header = $"{s.SongInfo.Artists} - {s.SongInfo.Title} ({fileInfo.Name})"
            Dim contentStack As New StackPanel With {.Margin = New Thickness(40, 5, 5, 5)}
            'Title
            contentStack.Children.Add(New Label _
                                         With {.Content = $"标题: {s.SongInfo.Title}", .Margin = New Thickness(5),
                                         .VerticalContentAlignment = VerticalAlignment.Center})
            'Artists
            contentStack.Children.Add(New Label _
                                         With {.Content = $"艺术家: {s.SongInfo.Artists}", .Margin = New Thickness(5),
                                         .VerticalContentAlignment = VerticalAlignment.Center})
            'File type
            contentStack.Children.Add(New Label _
                                         With {.Content = $"文件类型: { fileInfo.Extension}", .Margin = New Thickness(5),
                                         .VerticalContentAlignment = VerticalAlignment.Center})
            'Filename
            contentStack.Children.Add(New Label _
                                         With {.Content = $"文件路径: { fileInfo.FullName}", .Margin = New Thickness(5),
                                         .VerticalContentAlignment = VerticalAlignment.Center})
            'Size
            contentStack.Children.Add(New Label _
                                         With {.Content = $"文件大小: {fileInfo.Length}", .Margin = New Thickness(5),
                                         .VerticalContentAlignment = VerticalAlignment.Center})
            'HASH
            contentStack.Children.Add(New Label _
                                         With {.Content = $"散列值: {BitConverter.ToString(s.Hash)}", .Margin = New Thickness(5),
                                         .VerticalContentAlignment = VerticalAlignment.Center})
            e.Content = contentStack
            resultContentStack.Children.Add(e)
            resultContentStack.Children.Add(New Border With {.Height = 1, .BorderThickness = New Thickness(1), .BorderBrush = New SolidColorBrush(Colors.LightGray)})
        Next
        Return result

    End Function
End Class
