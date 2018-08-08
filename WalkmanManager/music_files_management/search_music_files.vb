''' <summary>
''' Search music files
''' TODO: This Class has NOT been debugged!
''' </summary>
Public Class SearchMusicFiles
    Private Shared ReadOnly ExtensionList As New List(Of String) From {"mp3", "wav", "flac", "ape", "aac"}
    Private Shared _pathList As New List(Of String)
    Private Shared _isInited As Boolean = False

    ''' <summary>
    ''' New class initiazer
    ''' </summary>
    ''' <param name="dir">The target directory to search</param>
    Public Sub Init(dir)
        If Not Search(dir, _pathList) Then
            _isInited = True
        End If
    End Sub

    Public Sub Close()
        MyClass.Close()
    End Sub

    Public Function Get_file_names()
        If _isInited Then
            Dim fileNames As New List(Of String)
            For Each file_path In _pathList
                fileNames.Add(file_path.Split("/")(file_path.Length() - 1))
            Next
            Return fileNames
        Else
            Return 0
        End If
    End Function

    Public Function Get_file_paths()
        If _isInited Then
            Return _pathList
        Else
            Return 0
        End If
    End Function

    ''' <summary>
    ''' A recusion in searching paths of matching files.
    ''' </summary>
    ''' <param name="dir">Target directory path</param>
    ''' <param name="pathList">A container to store mathcing paths</param>
    Private Function Search(ByVal dir As String, ByRef pathList As List(Of String))
        Try
            Dim dirs() As String = System.IO.Directory.GetDirectories(dir)
            Dim files() As String = System.IO.Directory.GetFiles(dir)
            For Each file_path In files
                Dim lenArray As Int16 = files.Length()
                If ExtensionList.Contains(file_path.Split(".")(lenArray - 1)) Then
                    pathList.Add(file_path)
                End If
            Next
            For Each dir_path In dirs
                If Search(dir_path, pathList) Then
                    Return 1
                End If
            Next
        Catch
            Return 1
        End Try
        Return 0
    End Function
End Class
