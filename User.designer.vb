<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class User
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(User))
        Me.StatusStrip2 = New System.Windows.Forms.StatusStrip()
        Me.toolStripStatusLabel4 = New System.Windows.Forms.ToolStripStatusLabel()
        Me.lblUser = New System.Windows.Forms.ToolStripStatusLabel()
        Me.toolStripStatusLabel6 = New System.Windows.Forms.ToolStripStatusLabel()
        Me.lblUserType = New System.Windows.Forms.ToolStripStatusLabel()
        Me.menuStrip3 = New System.Windows.Forms.MenuStrip()
        Me.verificationToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
        Me.TimeInToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.TimeOutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.LogoutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.StatusStrip2.SuspendLayout()
        Me.menuStrip3.SuspendLayout()
        Me.SuspendLayout()
        '
        'StatusStrip2
        '
        Me.StatusStrip2.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.toolStripStatusLabel4, Me.lblUser, Me.toolStripStatusLabel6, Me.lblUserType})
        Me.StatusStrip2.Location = New System.Drawing.Point(0, 449)
        Me.StatusStrip2.Name = "StatusStrip2"
        Me.StatusStrip2.Size = New System.Drawing.Size(971, 22)
        Me.StatusStrip2.TabIndex = 20
        Me.StatusStrip2.Text = "StatusStrip2"
        '
        'toolStripStatusLabel4
        '
        Me.toolStripStatusLabel4.Font = New System.Drawing.Font("Palatino Linotype", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.toolStripStatusLabel4.Name = "toolStripStatusLabel4"
        Me.toolStripStatusLabel4.Size = New System.Drawing.Size(86, 17)
        Me.toolStripStatusLabel4.Text = "Logged in As :"
        '
        'lblUser
        '
        Me.lblUser.Font = New System.Drawing.Font("Palatino Linotype", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblUser.Image = CType(resources.GetObject("lblUser.Image"), System.Drawing.Image)
        Me.lblUser.Name = "lblUser"
        Me.lblUser.Size = New System.Drawing.Size(49, 17)
        Me.lblUser.Text = "User"
        '
        'toolStripStatusLabel6
        '
        Me.toolStripStatusLabel6.Font = New System.Drawing.Font("Palatino Linotype", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.toolStripStatusLabel6.Name = "toolStripStatusLabel6"
        Me.toolStripStatusLabel6.Size = New System.Drawing.Size(11, 17)
        Me.toolStripStatusLabel6.Text = ":"
        '
        'lblUserType
        '
        Me.lblUserType.Font = New System.Drawing.Font("Palatino Linotype", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblUserType.Name = "lblUserType"
        Me.lblUserType.Size = New System.Drawing.Size(62, 17)
        Me.lblUserType.Text = "UserType"
        '
        'menuStrip3
        '
        Me.menuStrip3.BackColor = System.Drawing.Color.White
        Me.menuStrip3.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.menuStrip3.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.verificationToolStripMenuItem1, Me.LogoutToolStripMenuItem})
        Me.menuStrip3.Location = New System.Drawing.Point(0, 0)
        Me.menuStrip3.Name = "menuStrip3"
        Me.menuStrip3.Size = New System.Drawing.Size(971, 77)
        Me.menuStrip3.TabIndex = 19
        Me.menuStrip3.Text = "menuStrip3"
        '
        'verificationToolStripMenuItem1
        '
        Me.verificationToolStripMenuItem1.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.TimeInToolStripMenuItem, Me.TimeOutToolStripMenuItem})
        Me.verificationToolStripMenuItem1.Font = New System.Drawing.Font("Bookman Old Style", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.verificationToolStripMenuItem1.Image = Global.BiometricFingerprintsAttendanceSystemVB.My.Resources.Resources.fingerprint_ver_removebg_preview
        Me.verificationToolStripMenuItem1.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None
        Me.verificationToolStripMenuItem1.Name = "verificationToolStripMenuItem1"
        Me.verificationToolStripMenuItem1.Size = New System.Drawing.Size(109, 73)
        Me.verificationToolStripMenuItem1.Text = "Verification"
        Me.verificationToolStripMenuItem1.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
        '
        'TimeInToolStripMenuItem
        '
        Me.TimeInToolStripMenuItem.Name = "TimeInToolStripMenuItem"
        Me.TimeInToolStripMenuItem.Size = New System.Drawing.Size(149, 24)
        Me.TimeInToolStripMenuItem.Text = "Time-In"
        '
        'TimeOutToolStripMenuItem
        '
        Me.TimeOutToolStripMenuItem.Name = "TimeOutToolStripMenuItem"
        Me.TimeOutToolStripMenuItem.Size = New System.Drawing.Size(149, 24)
        Me.TimeOutToolStripMenuItem.Text = "Time-Out"
        '
        'LogoutToolStripMenuItem
        '
        Me.LogoutToolStripMenuItem.Font = New System.Drawing.Font("Bookman Old Style", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.LogoutToolStripMenuItem.Image = Global.BiometricFingerprintsAttendanceSystemVB.My.Resources.Resources.logout
        Me.LogoutToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None
        Me.LogoutToolStripMenuItem.Name = "LogoutToolStripMenuItem"
        Me.LogoutToolStripMenuItem.Size = New System.Drawing.Size(70, 73)
        Me.LogoutToolStripMenuItem.Text = "Logout"
        Me.LogoutToolStripMenuItem.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText
        '
        'User
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackgroundImage = Global.BiometricFingerprintsAttendanceSystemVB.My.Resources.Resources.bg_FRAttendanceSystem
        Me.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.ClientSize = New System.Drawing.Size(971, 471)
        Me.Controls.Add(Me.StatusStrip2)
        Me.Controls.Add(Me.menuStrip3)
        Me.DoubleBuffered = True
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "User"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Biometric Fingerprints Student Attendance System"
        Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
        Me.StatusStrip2.ResumeLayout(False)
        Me.StatusStrip2.PerformLayout()
        Me.menuStrip3.ResumeLayout(False)
        Me.menuStrip3.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents StatusStrip2 As StatusStrip
    Friend WithEvents toolStripStatusLabel4 As ToolStripStatusLabel
    Friend WithEvents lblUser As ToolStripStatusLabel
    Private WithEvents toolStripStatusLabel6 As ToolStripStatusLabel
    Public WithEvents lblUserType As ToolStripStatusLabel
    Friend WithEvents menuStrip3 As MenuStrip
    Friend WithEvents verificationToolStripMenuItem1 As ToolStripMenuItem
    Friend WithEvents LogoutToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents TimeInToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents TimeOutToolStripMenuItem As ToolStripMenuItem
End Class
