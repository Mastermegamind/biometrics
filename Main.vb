Imports MySql.Data.MySqlClient
Public Class Main
    Dim mysqlconn As MySqlConnection
    Dim dr As MySqlDataReader
    Dim cmd As MySqlCommand
    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

    Sub loadenrolleddata()
        Dim drt As MySqlDataReader
        Dim nm As String
        fptemplist.Clear()
        listofnames.Clear()
        Dim mysqlconn = New MySqlConnection
        mysqlconn.ConnectionString = Module1.con.ConnectionString()

        If mysqlconn.State = ConnectionState.Closed Then
            mysqlconn.Open()
        End If

        nm = "select matricno,fingerdata1,fingerdata2,fingerdata3,fingerdata4,fingerdata5,fingerdata6,fingerdata7,fingerdata8," _
            & "fingerdata9,fingerdata10 from new_enrollment"

        'nm = "select matricno,fingerdata7" _
        '   & "from new_enrollment where length(fingerdata7) > 0 "

        Dim cmd As New MySqlCommand
        cmd.CommandText = nm
        cmd.Connection = mysqlconn
        drt = cmd.ExecuteReader
        'MetroProgressBar1.Maximum = drt.FieldCount
        While drt.Read()
            Dim mstram As IO.MemoryStream

            For i = 1 To 10
                Dim fpbytes As Byte()
                fpbytes = drt("fingerdata" & i)
                mstram = New IO.MemoryStream(fpbytes)
                If fpbytes.Length > 0 Then
                    Dim temp8 As DPFP.Template = New DPFP.Template
                    temp8.DeSerialize(mstram)
                    fptemplist.Add(temp8)
                    listofnames.Add(drt("matricno"))
                End If
            Next
        End While
        drt.Close()
        mysqlconn.Close()
    End Sub

    Private Sub Main_Shown(sender As Object, e As System.EventArgs) Handles Me.Shown
        loadenrolleddata()
    End Sub

    Private Sub RegistrationToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RegistrationToolStripMenuItem.Click
        Dim register As New Register()
        register.Show()
    End Sub

    Private Sub EnrollmentMenuItem1_Click(sender As Object, e As EventArgs) Handles EnrollmentMenuItem1.Click
        Form1.Close()
        Form1.ShowDialog()
    End Sub

    Private Sub LogoutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LogoutToolStripMenuItem.Click
        Me.Close()
        Dim login As New Login()
        login.Show()
    End Sub

    Private Sub verificationToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles verificationToolStripMenuItem1.Click

    End Sub

    Private Sub AddUserToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AddUserToolStripMenuItem.Click
        AdminReg.Close()
        AdminReg.ShowDialog()
    End Sub

    Private Sub ClockInToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ClockInToolStripMenuItem.Click
        verificationform.Close()
        verificationform.ShowDialog()
    End Sub

    Private Sub ClockOutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ClockOutToolStripMenuItem.Click
        TimeOut.Close()
        TimeOut.ShowDialog()
    End Sub

    Private Sub AttendanceReportToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AttendanceReportToolStripMenuItem.Click
        AttendanceReport.Close()
        AttendanceReport.ShowDialog()
    End Sub
End Class