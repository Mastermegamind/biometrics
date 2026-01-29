Imports MySql.Data.MySqlClient
Imports System.IO

Public Class verificationform

    Dim mysqlconn As MySqlConnection
    Dim dr As MySqlDataReader
    Dim cmd As MySqlCommand

    Sub OnComplete(ByVal Control As Object, ByVal FeatureSet As DPFP.FeatureSet, ByRef EventHandlerStatus As DPFP.Gui.EventHandlerStatus) Handles VerificationControl.OnComplete
        Dim ver As New DPFP.Verification.Verification()
        Dim res As New DPFP.Verification.Verification.Result()
        Dim plo As Integer = 0

        For Each template As DPFP.Template In fptemplist   ' Compare feature set with all stored templates:
            If Not template Is Nothing Then                     '   Get template from storage.
                ver.Verify(FeatureSet, template, res)           '   Compare feature set with particular template.
                'Data.IsFeatureSetMatched = res.Verified         '   Check the result of the comparison
                FalseAcceptRate.Text = res.FARAchieved.ToString          '   Determine the current False Accept Rate
                If res.Verified Then
                    EventHandlerStatus = DPFP.Gui.EventHandlerStatus.Success
                    PictureBox2.Image = My.Resources.check_mark_symbol_transparent_background_22
                    Dim name As String = listofnames(plo)
                    TextBox1.Text = listofnames(plo)
                    autoselect(name)
                    TimeIn()

                    Exit For ' success
                    Reset()
                End If
            End If

            plo += 1
        Next
        If Not res.Verified Then
            EventHandlerStatus = DPFP.Gui.EventHandlerStatus.Failure
            PictureBox2.Image = My.Resources.question_mark_PNG66
            TextBox1.Text = ""
            'TextBox2.Text = ""
            txtName.Text = ""
        End If

    End Sub

    Sub autoselect(ByVal identity As String)
        Try
            Dim arrImage() As Byte = Nothing
            Dim dr As MySqlDataReader
            Dim nm As String
            Dim name As String = ""
            Dim dept As String = ""
            Dim jobtitle As String = ""
            Dim mysqlconn = New MySqlConnection
            mysqlconn.ConnectionString = Module1.con.ConnectionString()

            Dim name2 As String = identity
            Dim dept2 As String = ""

            If mysqlconn.State = ConnectionState.Closed Then
                mysqlconn.Open()
            End If

            nm = "select name,passport from students where matricno ='" & identity & "'"
            Dim cmd As New MySqlCommand
            cmd.CommandText = nm
            cmd.Connection = mysqlconn
            dr = cmd.ExecuteReader
            If (dr.Read() = True) Then
                'jobtitle = dr("JobTitle")
                name = dr("Name")

                Dim pByte As Byte()
                pByte = dr("passport")
                Dim MS As MemoryStream = New MemoryStream(pByte)
                PictureBox1.Image = New Bitmap(MS)
            End If

            dr.Close()
            mysqlconn.Close()
            cmd.Dispose()
            If String.IsNullOrWhiteSpace(name) Then
                'jobtitle = "No Job title"
                name = "No Name"
            End If

            'Invoke(New FunctionCall(AddressOf setjobtitle), jobtitle)

            Invoke(New FunctionCall(AddressOf setname), name)

        Catch ex As MySqlException
            MsgBox(ex.ToString, MsgBoxStyle.Exclamation, "")
        End Try


    End Sub

    'Private Sub setjobtitle(ByVal jobtitle As String)
    '    TextBox2.Text = jobtitle
    'End Sub

    Private Sub setname(ByVal name As String)
        txtName.Text = name
    End Sub
    Public Sub TimeIn()
        Try
            'Connection String
            Dim constr As String = Module1.con.ConnectionString()
            Using cnn As MySqlConnection = New MySqlConnection(constr)
                Using com As MySqlCommand = New MySqlCommand("SELECT matricno FROM attendance WHERE matricno = '" & TextBox1.Text & "' and date = '" & System.DateTime.Now.ToShortDateString() & "'")
                    com.CommandType = CommandType.Text
                    com.Connection = cnn
                    cnn.Open()
                    Using sdr As MySqlDataReader = com.ExecuteReader()
                        If sdr.Read() Then
                            'MsgBox("Duplicate Time-In Detected", MsgBoxStyle.Information, "Time and Attendance System")
                            cnn.Close()
                        Else

                            Dim comd As New MySqlCommand
                            Dim cn As New MySqlConnection
                            cn.ConnectionString = Module1.con.ConnectionString()
                            comd.CommandType = System.Data.CommandType.Text

                            comd.CommandText = "Insert into attendance (matricno,name,date,day,timein) Values 
                            ('" & TextBox1.Text & "','" & txtName.Text & "','" & DateClockIn.Text & "','" & System.DateTime.Now.ToString("dddd") & "','" & System.DateTime.Now.ToShortTimeString() & "')"
                            'comd.Parameters.AddWithValue("@pic", picc)
                            comd.Connection = cn
                            cn.Open()

                            'pic.Image.Save(AppDomain.CurrentDomain.BaseDirectory + txtUserId.Text + ".jpg")

                            'Run Query
                            comd.ExecuteNonQuery()
                            cn.Close()
                            ListView1.Items.Clear()
                            BackgroundWorker1.RunWorkerAsync()
                            'Reset()
                            'MsgBox("Time-In Attendance Recorded", MsgBoxStyle.Information, "Time and Attendance System")

                        End If

                    End Using
                    'con.Close()
                End Using
            End Using


        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub
    'Public Sub Reset()
    '    TextBox1.Text = Nothing
    '    'TextBox2.Text = Nothing
    '    txtName.Text = Nothing
    '    PictureBox1.Image = My.Resources.photo
    'End Sub

    Private Sub CloseButton_Click(sender As System.Object, e As System.EventArgs)
        Me.Close()
    End Sub

    Private Sub verificationform_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load
        Control.CheckForIllegalCrossThreadCalls = False
    End Sub

    Private Sub BackgroundWorker1_DoWork(sender As System.Object, e As System.ComponentModel.DoWorkEventArgs) Handles BackgroundWorker1.DoWork
        Dim mysqlconn = New MySqlConnection
        mysqlconn.ConnectionString = Module1.con.ConnectionString()
        mysqlconn.Open()
        Dim females As Integer = 0
        Dim males As Integer = 0
        Dim sqlstr As String = "select matricno,name,date,day,timein,timeout from attendance where date = '" & DateClockIn.Text & "'"
        Dim cmd = New MySqlCommand(sqlstr, mysqlconn)
        Dim sqlReader As MySqlDataReader = cmd.ExecuteReader
        For i As Integer = 1 To sqlReader.FieldCount
            Dim li As ListViewItem
            ListView1.BeginUpdate()
            While sqlReader.Read
                li = ListView1.Items.Add(sqlReader("matricno").ToString())
                li.SubItems.Add(sqlReader("name").ToString())
                'li.SubItems.Add(sqlReader("JobTitle").ToString())
                li.SubItems.Add(sqlReader("date").ToString())
                li.SubItems.Add(sqlReader("day").ToString())
                li.SubItems.Add(sqlReader("timein").ToString())
                li.SubItems.Add(sqlReader("timeout").ToString())
                If li.Index Mod 3 = 0 Then
                    li.BackColor = Color.LightBlue
                End If
            End While
            ListView1.EndUpdate()
        Next
        mysqlconn.Close()
        cmd.Dispose()
        sqlReader.Dispose()
        Label4.Text = ListView1.Items.Count

    End Sub

    Public Sub verificationform_Shown(sender As Object, e As System.EventArgs) Handles Me.Shown
        BackgroundWorker1.RunWorkerAsync()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub
    Public Sub Reset()
        TextBox1.Text = Nothing
        txtName.Text = Nothing
        PictureBox1.Image = Nothing
    End Sub

End Class