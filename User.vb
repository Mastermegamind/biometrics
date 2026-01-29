Imports MySql.Data.MySqlClient
Public Class User
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

    Private Sub User_Shown(sender As Object, e As System.EventArgs) Handles Me.Shown
        loadenrolleddata()
    End Sub
    Private Sub verificationToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles verificationToolStripMenuItem1.Click
        'verificationform.Close()
        'verificationform.ShowDialog()
    End Sub

    Private Sub LogoutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LogoutToolStripMenuItem.Click
        Me.Close()
        Dim login As New Login()
        login.Show()
    End Sub

    Private Sub TimeInToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles TimeInToolStripMenuItem.Click
        verificationform.Close()
        verificationform.ShowDialog()
    End Sub

    Private Sub TimeOutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles TimeOutToolStripMenuItem.Click
        TimeOut.Close()
        TimeOut.ShowDialog()
    End Sub

    Private Sub User_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub
End Class