Imports MySql.Data.MySqlClient
Imports System.Net
Module PublicFunctions
    Declare Auto Function SendMessage Lib "user32.dll" (ByVal hWnd As IntPtr, ByVal msg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As Integer
    Enum ProgressBarColor
        Green = &H1
        Red = &H2
        Yellow = &H3
    End Enum
    Public Sub ChangeProgBarColor(ByVal ProgressBar_Name As System.Windows.Forms.ProgressBar, ByVal ProgressBar_Color As ProgressBarColor)
        SendMessage(ProgressBar_Name.Handle, &H410, ProgressBar_Color, 0)
    End Sub
    Public Function ConvertB64ToString(str As String)
        Dim b As Byte() = Convert.FromBase64String(str)
        Dim byt2 = System.Text.Encoding.UTF8.GetString(b)
        Return byt2
    End Function
    Public Function RemoveCharacter(ByVal stringToCleanUp, ByVal characterToRemove)
        ' replace the target with nothing
        ' Replace() returns a new String and does not modify the current one
        Return stringToCleanUp.Replace(characterToRemove, "")
    End Function
    Public Function LoadConn(path As String) As Boolean
        Dim RetMe As Boolean = False
        Try
            If path <> "" Then
                If System.IO.File.Exists(path) Then
                    Dim CreateConnString As String = ""
                    Dim filename As String = String.Empty
                    Dim TextLine As String = ""
                    Dim objReader As New System.IO.StreamReader(path)
                    Dim lineCount As Integer
                    Do While objReader.Peek() <> -1
                        TextLine = objReader.ReadLine()
                        If lineCount = 0 Then
                            LocalServer = ConvertB64ToString(RemoveCharacter(TextLine, "server="))
                        End If
                        If lineCount = 1 Then
                            LocalUsername = ConvertB64ToString(RemoveCharacter(TextLine, "user id="))
                        End If
                        If lineCount = 2 Then
                            LocalPassword = ConvertB64ToString(RemoveCharacter(TextLine, "password="))
                        End If
                        If lineCount = 3 Then
                            LocalDatabase = ConvertB64ToString(RemoveCharacter(TextLine, "database="))
                        End If
                        If lineCount = 4 Then
                            LocalPort = ConvertB64ToString(RemoveCharacter(TextLine, "port="))
                        End If
                        lineCount = lineCount + 1
                    Loop
                    objReader.Close()
                    RetMe = True
                Else
                    RetMe = False
                End If
            Else
                RetMe = False
            End If
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
        Return RetMe
    End Function
    Public Function CheckLocalConnection() As MySqlConnection
        Dim Con As MySqlConnection = New MySqlConnection
        Try
            Con.ConnectionString = "server=" & Trim(LocalServer) &
            ";user id= " & Trim(LocalUsername) &
            ";password=" & Trim(LocalPassword) &
            ";database=" & Trim(LocalDatabase) &
            ";port=" & Trim(LocalPort)
            Con.Open()
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
        Return Con
    End Function
    Public Function CheckCloudConnection() As MySqlConnection
        Dim Con As MySqlConnection = New MySqlConnection
        Dim str As String = "server=" & Trim(ConvertB64ToString(CloudServer)) &
            ";user id= " & Trim(ConvertB64ToString(CloudUsername)) &
            ";password=" & Trim(ConvertB64ToString(CloudPassword)) &
            ";database=" & Trim(ConvertB64ToString(CloudDatabase)) &
            ";port=" & Trim(ConvertB64ToString(CloudPort))
        Try
            Con.ConnectionString = str
            Con.Open()
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
        Return Con
    End Function
    Public Function CheckForInternetConnection() As Boolean
        Try
            Using client = New WebClient()
                Using stream = client.OpenRead("http://www.google.com")
                    Return True
                End Using
            End Using
        Catch
            Return False
        End Try
    End Function
End Module
