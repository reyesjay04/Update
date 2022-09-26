Imports MySql.Data.MySqlClient
Imports System.Threading
Imports System.Net
Imports System.IO
Public Class Loading
    Property CanBeUpdate As Boolean = False
    Property Thread1 As Thread
    Property ThreadList As List(Of Thread) = New List(Of Thread)
    Property LocalVersion As String = ""
    Property CloudVersion As String = ""
    Property startPath As String = Application.StartupPath & "\update"
    Property Deletepath As String = Application.StartupPath & "\update\Debug"
    Property FTPServer As String = ""
    Property FTPUSername As String = ""
    Property FTPPassword As String = ""
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If My.Settings.UpdateVersion = "" Then
            LabelPOSVersion.Text = "Version 0.0.0"
        Else
            LabelPOSVersion.Text = My.Settings.UpdateVersion
        End If
        Try
            CheckForIllegalCrossThreadCalls = False
            ChangeProgBarColor(ProgressBar1, ProgressBarColor.Yellow)
            If LoadConn(localconnectionpath) = True Then
                CanBeUpdate = True
                ProgressBar1.Maximum = 100
                ProgressBar1.Value = 0
                BackgroundWorker1.WorkerReportsProgress = True
                BackgroundWorker1.WorkerSupportsCancellation = True
                BackgroundWorker1.RunWorkerAsync()
            Else
                CanBeUpdate = False
                ProgressBar1.Maximum = 100
                ProgressBar1.Value = 0
                BackgroundWorker1.WorkerReportsProgress = True
                BackgroundWorker1.WorkerSupportsCancellation = True
                BackgroundWorker1.RunWorkerAsync()
            End If
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub

    Private Function CloudConnectionExist() As Boolean
        Dim bool As Boolean = False
        Try
            Dim Query = "SELECT settings_id FROM loc_settings WHERE settings_id = 1"
            Dim cmd As MySqlCommand = New MySqlCommand(Query, CheckLocalConnection)
            Dim result = cmd.ExecuteScalar
            If result = 1 Then
                bool = True
            Else
                bool = False
            End If
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
        Return bool
    End Function
    Private Sub BackgroundWorker1_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        Try
            For i = 0 To 100
                LabelProgressbarStatus.Text = "Checking for software update. Please wait " & i & "%"
                BackgroundWorker1.ReportProgress(i)
                Thread.Sleep(50)
                If CanBeUpdate = True Then
                    If i = 10 Then
                        If CheckForInternetConnection() Then
                            If LoadConn(localconnectionpath) Then
                                If CloudConnectionExist() Then
                                    Thread1 = New Thread(AddressOf GetCloudCredentials)
                                    Thread1.Start()
                                    ThreadList.Add(Thread1)
                                    For Each t In ThreadList
                                        t.Join()
                                    Next
                                    Thread1 = New Thread(AddressOf CheckVersionLocal)
                                    Thread1.Start()
                                    ThreadList.Add(Thread1)
                                    For Each t In ThreadList
                                        t.Join()
                                    Next
                                    Thread1 = New Thread(AddressOf CheckVersionServer)
                                    Thread1.Start()
                                    ThreadList.Add(Thread1)
                                    For Each t In ThreadList
                                        t.Join()
                                    Next
                                End If
                            End If
                        End If
                    End If
                End If
            Next
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub
    Private Sub BackgroundWorker1_ProgressChanged(sender As Object, e As System.ComponentModel.ProgressChangedEventArgs) Handles BackgroundWorker1.ProgressChanged
        ProgressBar1.Value = e.ProgressPercentage
    End Sub
    Private Sub CreateFolder(Optional ByVal Attributes As System.IO.FileAttributes = IO.FileAttributes.Normal)
        Try
            If My.Computer.FileSystem.DirectoryExists(Application.StartupPath & "\update") Then
                Dim logInfo = My.Computer.FileSystem.GetDirectoryInfo(Application.StartupPath & "\update")
            Else
                My.Computer.FileSystem.CreateDirectory(Application.StartupPath & "\update")
                If Not Attributes = IO.FileAttributes.Normal Then
                    My.Computer.FileSystem.GetDirectoryInfo(Application.StartupPath & "\update").Attributes = Attributes
                End If
            End If
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub
    Private Sub BackgroundWorker1_RunWorkerCompleted(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles BackgroundWorker1.RunWorkerCompleted
        Try
            If CheckForInternetConnection() Then
                If CanBeUpdate Then
                    If LocalVersion <> CloudVersion Then
                        Dim Message = MessageBox.Show("Your POS system is not currently up to date. Would you like to update it now?", "POS System Update", MessageBoxButtons.OKCancel, MessageBoxIcon.Information)
                        If Message = DialogResult.OK Then
                            ProgressBar1.Maximum = 100
                            ProgressBar1.Value = 0
                            BackgroundWorker2.WorkerReportsProgress = True
                            BackgroundWorker2.WorkerSupportsCancellation = True
                            BackgroundWorker2.RunWorkerAsync()

                            ' FTPDownloadFile(Application.StartupPath & "\update\update.zip", "ftp://famousbelgianwaffles.com", "devmaster@dgpos.app", "password2022")

                        Else
                            My.Settings.UpdateVersion = CloudVersion
                            My.Settings.Save()
                            Process.Start(startPath & "\Debug\POS.exe")
                            Application.Exit()
                        End If
                    Else
                        My.Settings.UpdateVersion = CloudVersion
                        My.Settings.Save()
                        Process.Start(startPath & "\Debug\POS.exe")
                        Application.Exit()
                    End If
                Else
                    My.Settings.UpdateVersion = CloudVersion
                    My.Settings.Save()
                    Process.Start(startPath & "\Debug\POS.exe")
                    Application.Exit()
                End If
            Else
                MsgBox("Internet connection is not available")
                Process.Start(startPath & "\Debug\POS.exe")
                Application.Exit()
            End If
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub
    Private Sub CheckVersionLocal()
        Try
            Dim sql = "SELECT S_Update_Version FROM loc_settings WHERE settings_id = 1"
            Dim cmd As MySqlCommand = New MySqlCommand(sql, CheckLocalConnection)
            LocalVersion = cmd.ExecuteScalar
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub
    Private Sub CheckVersionServer()
        Try
            Dim sql = "SELECT S_Update_Version FROM admin_settings_org WHERE settings_id = 1"
            Dim cmd As MySqlCommand = New MySqlCommand(sql, CheckCloudConnection)
            CloudVersion = cmd.ExecuteScalar
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub
    Private Sub GetCloudCredentials()
        Try
            Dim sql = "SELECT `C_Server`, `C_Username`, `C_Password`, `C_Database`, `C_Port` FROM `loc_settings` WHERE `settings_id` = 1"
            Dim cmd As MySqlCommand = New MySqlCommand(sql, CheckLocalConnection)
            Dim da As MySqlDataAdapter = New MySqlDataAdapter(cmd)
            Dim dt As DataTable = New DataTable
            da.Fill(dt)
            CloudServer = dt(0)(0)
            CloudUsername = dt(0)(1)
            CloudPassword = dt(0)(2)
            CloudDatabase = dt(0)(3)
            CloudPort = dt(0)(4)
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub

    Private Sub BackgroundWorker2_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker2.DoWork
        Try
            For i = 0 To 100
                LabelProgressbarStatus.Text = "Getting update package please wait."
                BackgroundWorker2.ReportProgress(i)
                Thread.Sleep(50)
                If i = 10 Then
                    Thread1 = New Thread(AddressOf GetFtpCred)
                    Thread1.Start()
                    ThreadList.Add(Thread1)
                    For Each t In ThreadList
                        t.Join()
                    Next
                    Thread1 = New Thread(AddressOf CreateFolder)
                    Thread1.Start()
                    ThreadList.Add(Thread1)
                    For Each t In ThreadList
                        t.Join()
                    Next
                    Thread1 = New Thread(AddressOf GetNewUpdates)
                    Thread1.Start()
                    ThreadList.Add(Thread1)
                    For Each t In ThreadList
                        t.Join()
                    Next

                    'Thread1 = New Thread(Sub() FTPDownloadFile(Application.StartupPath & "\update\update.zip"), "ftp://famousbelgianwaffles.com", "ftppos@famousbelgianwaffles.com", "password2022")
                    'Thread1.Start()
                    'ThreadList.Add(Thread1)
                    'For Each t In ThreadList
                    '    t.Join()
                    'Next
                    Thread1 = New Thread(AddressOf extract)
                    Thread1.Start()
                    ThreadList.Add(Thread1)
                    For Each t In ThreadList
                        t.Join()
                    Next
                    Thread1 = New Thread(AddressOf UpdateLocalVersion)
                    Thread1.Start()
                    ThreadList.Add(Thread1)
                    For Each t In ThreadList
                        t.Join()
                    Next
                End If
            Next
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub
    Private Sub BackgroundWorker2_ProgressChanged(sender As Object, e As System.ComponentModel.ProgressChangedEventArgs) Handles BackgroundWorker2.ProgressChanged
        ProgressBar1.Value = e.ProgressPercentage
    End Sub
    Private Sub BackgroundWorker2_RunWorkerCompleted(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles BackgroundWorker2.RunWorkerCompleted
        My.Settings.UpdateVersion = CloudVersion
        My.Settings.Save()
        MsgBox("POS System is up to date")
        Process.Start(startPath & "\Debug\POS.exe")
        Application.Exit()
    End Sub

    Private Sub GetFtpCred()
        Try
            Dim sql = "SELECT `C_Ftp_Server`, `C_Ftp_Username`, `C_Ftp_Password` FROM admin_settings_org WHERE settings_id = 1"
            Dim cmd As MySqlCommand = New MySqlCommand(sql, CheckCloudConnection)
            Dim da As MySqlDataAdapter = New MySqlDataAdapter(cmd)
            Dim dt As DataTable = New DataTable
            da.Fill(dt)
            FTPServer = dt(0)(0)
            FTPUSername = dt(0)(1)
            FTPPassword = dt(0)(2)
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub
    Private Sub FTPDownloadFile(ByVal downloadpath As String, ByVal ftpuri As String, ByVal ftpusername As String, ByVal ftppassword As String)
        'Create a WebClient.
        Dim request As New WebClient()

        ' Confirm the Network credentials based on the user name and password passed in.
        request.Credentials = New NetworkCredential(ftpusername, ftppassword)

        'Read the file data into a Byte array
        Dim bytes() As Byte = request.DownloadData(ftpuri)

        Try
            '  Create a FileStream to read the file into
            Dim DownloadStream As FileStream = IO.File.Create(downloadpath)
            '  Stream this data into the file
            DownloadStream.Write(bytes, 0, bytes.Length)
            '  Close the FileStream
            DownloadStream.Close()

        Catch ex As Exception
            MessageBox.Show(ex.Message)
            Exit Sub
        End Try

        MessageBox.Show("Process Complete")

    End Sub
    Private Sub GetNewUpdates()
        Try
            'FTPServer = ConvertB64ToString(FTPServer).ToString
            'FTPUSername = ConvertB64ToString(FTPUSername).ToString
            'FTPPassword = ConvertB64ToString(FTPPassword).ToString

            Dim request As FtpWebRequest = WebRequest.Create(FTPServer & "/software-update/update.zip")
            request.Credentials = New NetworkCredential(FTPUSername, FTPPassword)
            request.Method = WebRequestMethods.Ftp.DownloadFile

            Using ftpStream As Stream = request.GetResponse().GetResponseStream(), fileStream As Stream = File.Create(Application.StartupPath & "\update\update.zip")
                Dim buffer As Byte() = New Byte(10240 - 1) {}
                Dim read As Integer
                Do
                    read = ftpStream.Read(buffer, 0, buffer.Length)
                    If read > 0 Then
                        fileStream.Write(buffer, 0, read)
                        LabelProgressbarStatus.Text = "Downloading file size " & fileStream.Position & " bytes"
                    End If
                Loop While read > 0
            End Using
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub

    Private Sub extract()
        Try
            Dim shObj As Object = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"))
            Dim startPath As String = Application.StartupPath & "\update"
            Dim zipPath As String = Application.StartupPath & "\update\update.zip"
            Dim extractPath As String = Application.StartupPath & "\update"
            DeleteDirectory(startPath & "\Debug")
            IO.Directory.CreateDirectory(extractPath)
            Dim output As Object = shObj.NameSpace((extractPath))
            Dim input As Object = shObj.NameSpace((zipPath))
            output.CopyHere((input.Items), 4)
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub
    Private Sub DeleteDirectory(path As String)
        Try
            If Directory.Exists(path) Then
                'Delete all files from the Directory
                For Each filepath As String In Directory.GetFiles(path)
                    File.Delete(filepath)
                Next
                'Delete all child Directories
                For Each dir As String In Directory.GetDirectories(path)
                    DeleteDirectory(dir)
                Next
                'Delete a Directory
                Directory.Delete(path)
            End If
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub
    Private Sub UpdateLocalVersion()
        Try
            Dim sql = "UPDATE loc_settings SET S_Update_Version = '" & CloudVersion & "' WHERE settings_id = 1"
            Dim cmd As MySqlCommand = New MySqlCommand(sql, CheckLocalConnection)
            cmd.ExecuteNonQuery()
        Catch ex As Exception
            MsgBox(ex.ToString)
        End Try
    End Sub


End Class
