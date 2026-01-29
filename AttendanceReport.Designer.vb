<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class AttendanceReport
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(AttendanceReport))
        Me.btnView = New System.Windows.Forms.Button()
        Me.dtpDateTo = New System.Windows.Forms.DateTimePicker()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.dtpDateFrom = New System.Windows.Forms.DateTimePicker()
        Me.dataGridViewEmployee = New System.Windows.Forms.DataGridView()
        Me.btnReset = New System.Windows.Forms.Button()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.groupBox1 = New System.Windows.Forms.GroupBox()
        Me.panel1 = New System.Windows.Forms.Panel()
        Me.label3 = New System.Windows.Forms.Label()
        Me.lblNumCount = New System.Windows.Forms.Label()
        Me.label1 = New System.Windows.Forms.Label()
        Me.txtStudentName = New System.Windows.Forms.TextBox()
        CType(Me.dataGridViewEmployee, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox2.SuspendLayout()
        Me.groupBox1.SuspendLayout()
        Me.panel1.SuspendLayout()
        Me.SuspendLayout()
        '
        'btnView
        '
        Me.btnView.BackColor = System.Drawing.Color.DarkSlateGray
        Me.btnView.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnView.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnView.Font = New System.Drawing.Font("Palatino Linotype", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnView.ForeColor = System.Drawing.Color.White
        Me.btnView.Location = New System.Drawing.Point(398, 49)
        Me.btnView.Name = "btnView"
        Me.btnView.Size = New System.Drawing.Size(86, 26)
        Me.btnView.TabIndex = 3
        Me.btnView.Text = "View Report"
        Me.btnView.UseVisualStyleBackColor = False
        '
        'dtpDateTo
        '
        Me.dtpDateTo.CustomFormat = "dd/MM/yyyy"
        Me.dtpDateTo.Format = System.Windows.Forms.DateTimePickerFormat.Custom
        Me.dtpDateTo.Location = New System.Drawing.Point(221, 50)
        Me.dtpDateTo.Name = "dtpDateTo"
        Me.dtpDateTo.Size = New System.Drawing.Size(162, 24)
        Me.dtpDateTo.TabIndex = 2
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(218, 31)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(30, 17)
        Me.Label2.TabIndex = 13
        Me.Label2.Text = "To :"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(9, 30)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(44, 17)
        Me.Label4.TabIndex = 12
        Me.Label4.Text = "From :"
        '
        'dtpDateFrom
        '
        Me.dtpDateFrom.CustomFormat = "dd/MM/yyyy"
        Me.dtpDateFrom.Format = System.Windows.Forms.DateTimePickerFormat.Custom
        Me.dtpDateFrom.Location = New System.Drawing.Point(12, 49)
        Me.dtpDateFrom.Name = "dtpDateFrom"
        Me.dtpDateFrom.Size = New System.Drawing.Size(162, 24)
        Me.dtpDateFrom.TabIndex = 1
        '
        'dataGridViewEmployee
        '
        Me.dataGridViewEmployee.AllowUserToAddRows = False
        Me.dataGridViewEmployee.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells
        Me.dataGridViewEmployee.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders
        Me.dataGridViewEmployee.BackgroundColor = System.Drawing.Color.White
        Me.dataGridViewEmployee.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.dataGridViewEmployee.ColumnHeadersHeight = 25
        Me.dataGridViewEmployee.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        Me.dataGridViewEmployee.Location = New System.Drawing.Point(17, 112)
        Me.dataGridViewEmployee.Name = "dataGridViewEmployee"
        Me.dataGridViewEmployee.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.[Single]
        Me.dataGridViewEmployee.RowHeadersWidth = 25
        Me.dataGridViewEmployee.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing
        Me.dataGridViewEmployee.Size = New System.Drawing.Size(507, 327)
        Me.dataGridViewEmployee.TabIndex = 350
        '
        'btnReset
        '
        Me.btnReset.BackColor = System.Drawing.Color.Maroon
        Me.btnReset.Cursor = System.Windows.Forms.Cursors.Hand
        Me.btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnReset.Font = New System.Drawing.Font("Palatino Linotype", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnReset.ForeColor = System.Drawing.Color.White
        Me.btnReset.Location = New System.Drawing.Point(490, 49)
        Me.btnReset.Name = "btnReset"
        Me.btnReset.Size = New System.Drawing.Size(82, 26)
        Me.btnReset.TabIndex = 0
        Me.btnReset.Text = "&Reset"
        Me.btnReset.UseVisualStyleBackColor = False
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.btnReset)
        Me.GroupBox2.Controls.Add(Me.btnView)
        Me.GroupBox2.Controls.Add(Me.dtpDateTo)
        Me.GroupBox2.Controls.Add(Me.Label2)
        Me.GroupBox2.Controls.Add(Me.Label4)
        Me.GroupBox2.Controls.Add(Me.dtpDateFrom)
        Me.GroupBox2.Font = New System.Drawing.Font("Palatino Linotype", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox2.Location = New System.Drawing.Point(17, 13)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(588, 93)
        Me.GroupBox2.TabIndex = 351
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Search Student Attendance by Date Range"
        '
        'groupBox1
        '
        Me.groupBox1.Controls.Add(Me.panel1)
        Me.groupBox1.Controls.Add(Me.label1)
        Me.groupBox1.Controls.Add(Me.txtStudentName)
        Me.groupBox1.Font = New System.Drawing.Font("Palatino Linotype", 9.0!, System.Drawing.FontStyle.Bold)
        Me.groupBox1.Location = New System.Drawing.Point(541, 112)
        Me.groupBox1.Name = "groupBox1"
        Me.groupBox1.Size = New System.Drawing.Size(343, 176)
        Me.groupBox1.TabIndex = 352
        Me.groupBox1.TabStop = False
        Me.groupBox1.Text = "View Student Attendance Status"
        Me.groupBox1.Visible = False
        '
        'panel1
        '
        Me.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.panel1.Controls.Add(Me.label3)
        Me.panel1.Controls.Add(Me.lblNumCount)
        Me.panel1.Location = New System.Drawing.Point(13, 77)
        Me.panel1.Name = "panel1"
        Me.panel1.Size = New System.Drawing.Size(312, 68)
        Me.panel1.TabIndex = 342
        Me.panel1.Visible = False
        '
        'label3
        '
        Me.label3.AutoSize = True
        Me.label3.Location = New System.Drawing.Point(22, 22)
        Me.label3.Name = "label3"
        Me.label3.Size = New System.Drawing.Size(135, 17)
        Me.label3.TabIndex = 6
        Me.label3.Text = "Total No. of Attendent:"
        '
        'lblNumCount
        '
        Me.lblNumCount.AutoSize = True
        Me.lblNumCount.Location = New System.Drawing.Point(161, 22)
        Me.lblNumCount.Name = "lblNumCount"
        Me.lblNumCount.Size = New System.Drawing.Size(0, 17)
        Me.lblNumCount.TabIndex = 2
        '
        'label1
        '
        Me.label1.AutoSize = True
        Me.label1.Location = New System.Drawing.Point(10, 40)
        Me.label1.Name = "label1"
        Me.label1.Size = New System.Drawing.Size(89, 17)
        Me.label1.TabIndex = 1
        Me.label1.Text = "Student Name:"
        '
        'txtStudentName
        '
        Me.txtStudentName.Location = New System.Drawing.Point(105, 37)
        Me.txtStudentName.Name = "txtStudentName"
        Me.txtStudentName.Size = New System.Drawing.Size(220, 24)
        Me.txtStudentName.TabIndex = 0
        '
        'AttendanceReport
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(899, 453)
        Me.Controls.Add(Me.dataGridViewEmployee)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.groupBox1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "AttendanceReport"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Attendance Report"
        CType(Me.dataGridViewEmployee, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.groupBox1.ResumeLayout(False)
        Me.groupBox1.PerformLayout()
        Me.panel1.ResumeLayout(False)
        Me.panel1.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents btnView As Button
    Friend WithEvents dtpDateTo As DateTimePicker
    Friend WithEvents Label2 As Label
    Friend WithEvents Label4 As Label
    Friend WithEvents dtpDateFrom As DateTimePicker
    Private WithEvents dataGridViewEmployee As DataGridView
    Friend WithEvents btnReset As Button
    Friend WithEvents GroupBox2 As GroupBox
    Private WithEvents groupBox1 As GroupBox
    Private WithEvents panel1 As Panel
    Private WithEvents label3 As Label
    Private WithEvents lblNumCount As Label
    Private WithEvents label1 As Label
    Private WithEvents txtStudentName As TextBox
End Class
