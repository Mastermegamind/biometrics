Imports MySql.Data.MySqlClient

Public Class AttendanceReport
    Private Sub btnView_Click(sender As Object, e As EventArgs) Handles btnView.Click
        SelectGridView()
        groupBox1.Visible = True
    End Sub

    Sub SelectGridView()
        Try

            Dim con As MySqlConnection
            con = New MySqlConnection
            con.ConnectionString = Module1.con.ConnectionString()

            Dim cmd As MySqlCommand = New MySqlCommand("select name as'Student Name', matricno as'Matric No.', date, day, timein as'Time-In',timeout as'Time-Out' From attendance where date BETWEEN '" + dtpDateFrom.Text + "' AND '" + dtpDateTo.Text + "'", con)



            con.Open()
            Dim da As MySqlDataAdapter = New MySqlDataAdapter()
            da.SelectCommand = cmd
            Dim dt As DataTable = New DataTable()
            da.Fill(dt)
            Dim bs As BindingSource = New BindingSource()
            bs.DataSource = dt
            dataGridViewEmployee.DataSource = dt
            da.Update(dt)
            con.Close()
            'btnExportCSV.Enabled = True;
        Catch ex As Exception

        End Try
    End Sub

    Public Sub Reset()
        dataGridViewEmployee.DataSource = ""
        txtStudentName.Text = ""
        lblNumCount.Text = ""
    End Sub

    Private Sub btnReset_Click(sender As Object, e As EventArgs) Handles btnReset.Click
        Reset()
    End Sub

    Public Sub AutoDisplayName()
        Try
            Dim con As MySqlConnection = Nothing
            con = New MySqlConnection(Module1.con.ConnectionString)
            con.Open()
            Dim cmd As MySqlCommand = New MySqlCommand("SELECT name FROM attendance", con)
            Dim ds As DataSet = New DataSet()
            Dim da As New MySqlDataAdapter(cmd)
            da.Fill(ds, "list") 'list can be any name u want

            Dim col As New AutoCompleteStringCollection
            Dim i As Integer
            For i = 0 To ds.Tables(0).Rows.Count - 1
                col.Add(ds.Tables(0).Rows(i)("name").ToString())  'columnname same As In query

            Next

            txtStudentName.AutoCompleteSource = AutoCompleteSource.CustomSource
            txtStudentName.AutoCompleteCustomSource = col
            txtStudentName.AutoCompleteMode = AutoCompleteMode.Suggest

            con.Close()
        Catch ex As MySqlException
            MsgBox(ex.ToString, MsgBoxStyle.Exclamation, "")
        End Try

    End Sub

    Private Sub AttendanceReport_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        AutoDisplayName()
    End Sub

    Public Sub CalculateAttendance()
        Try
            Dim constring As MySqlConnection
            constring = New MySqlConnection()
            constring.ConnectionString = Module1.con.ConnectionString()
            constring.Open()
            Dim comd As MySqlCommand = New MySqlCommand("SELECT COUNT(*) as Count FROM attendance where name='" & txtStudentName.Text & "' AND date BETWEEN '" & dtpDateFrom.Text & "' AND '" & dtpDateTo.Text & "'", constring)
            comd.CommandType = CommandType.Text

            Dim o As Object = comd.ExecuteScalar()

            If (Not o Is Nothing) Then
                lblNumCount.Text = o.ToString()
                panel1.Visible = True
                constring.Close()
            End If

        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    Private Sub txtStudentName_TextChanged(sender As Object, e As EventArgs) Handles txtStudentName.TextChanged
        CalculateAttendance()
    End Sub
End Class