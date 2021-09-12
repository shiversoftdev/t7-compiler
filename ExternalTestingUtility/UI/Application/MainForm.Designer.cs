
namespace t7c_installer
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.InnerForm = new Refract.UI.Core.Controls.CBorderedForm();
            this.CreateDefaultProject = new System.Windows.Forms.Button();
            this.JoinDiscord = new System.Windows.Forms.Button();
            this.InstallVSCExt = new System.Windows.Forms.Button();
            this.ConvertProj = new System.Windows.Forms.Button();
            this.UpdateButton = new System.Windows.Forms.Button();
            this.InnerForm.ControlContents.SuspendLayout();
            this.SuspendLayout();
            // 
            // InnerForm
            // 
            this.InnerForm.BackColor = System.Drawing.Color.DodgerBlue;
            // 
            // InnerForm.ControlContents
            // 
            this.InnerForm.ControlContents.Controls.Add(this.CreateDefaultProject);
            this.InnerForm.ControlContents.Controls.Add(this.JoinDiscord);
            this.InnerForm.ControlContents.Controls.Add(this.InstallVSCExt);
            this.InnerForm.ControlContents.Controls.Add(this.ConvertProj);
            this.InnerForm.ControlContents.Controls.Add(this.UpdateButton);
            this.InnerForm.ControlContents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InnerForm.ControlContents.Enabled = true;
            this.InnerForm.ControlContents.Location = new System.Drawing.Point(0, 32);
            this.InnerForm.ControlContents.Name = "ControlContents";
            this.InnerForm.ControlContents.Size = new System.Drawing.Size(246, 168);
            this.InnerForm.ControlContents.TabIndex = 1;
            this.InnerForm.ControlContents.Visible = true;
            this.InnerForm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InnerForm.Location = new System.Drawing.Point(0, 0);
            this.InnerForm.Name = "InnerForm";
            this.InnerForm.Size = new System.Drawing.Size(250, 204);
            this.InnerForm.TabIndex = 0;
            this.InnerForm.TitleBarTitle = "Compiler Utility";
            this.InnerForm.UseTitleBar = true;
            // 
            // CreateDefaultProject
            // 
            this.CreateDefaultProject.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.CreateDefaultProject.Cursor = System.Windows.Forms.Cursors.Hand;
            this.CreateDefaultProject.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CreateDefaultProject.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CreateDefaultProject.Location = new System.Drawing.Point(3, 69);
            this.CreateDefaultProject.Name = "CreateDefaultProject";
            this.CreateDefaultProject.Size = new System.Drawing.Size(240, 30);
            this.CreateDefaultProject.TabIndex = 4;
            this.CreateDefaultProject.Text = "Create Default Project";
            this.CreateDefaultProject.UseVisualStyleBackColor = true;
            this.CreateDefaultProject.Click += new System.EventHandler(this.CreateDefaultProject_Click);
            // 
            // JoinDiscord
            // 
            this.JoinDiscord.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.JoinDiscord.Cursor = System.Windows.Forms.Cursors.Hand;
            this.JoinDiscord.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.JoinDiscord.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.JoinDiscord.Location = new System.Drawing.Point(3, 135);
            this.JoinDiscord.Name = "JoinDiscord";
            this.JoinDiscord.Size = new System.Drawing.Size(240, 30);
            this.JoinDiscord.TabIndex = 3;
            this.JoinDiscord.Text = "Join Discord Server";
            this.JoinDiscord.UseVisualStyleBackColor = true;
            this.JoinDiscord.Click += new System.EventHandler(this.JoinDiscord_Click);
            // 
            // InstallVSCExt
            // 
            this.InstallVSCExt.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.InstallVSCExt.Cursor = System.Windows.Forms.Cursors.Hand;
            this.InstallVSCExt.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.InstallVSCExt.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InstallVSCExt.Location = new System.Drawing.Point(3, 36);
            this.InstallVSCExt.Name = "InstallVSCExt";
            this.InstallVSCExt.Size = new System.Drawing.Size(240, 30);
            this.InstallVSCExt.TabIndex = 2;
            this.InstallVSCExt.Text = "Install VSC Extension";
            this.InstallVSCExt.UseVisualStyleBackColor = true;
            this.InstallVSCExt.Click += new System.EventHandler(this.InstallVSCExt_Click);
            // 
            // ConvertProj
            // 
            this.ConvertProj.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ConvertProj.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ConvertProj.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ConvertProj.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ConvertProj.Location = new System.Drawing.Point(3, 102);
            this.ConvertProj.Name = "ConvertProj";
            this.ConvertProj.Size = new System.Drawing.Size(240, 30);
            this.ConvertProj.TabIndex = 1;
            this.ConvertProj.Text = "Auto-port T7 IL Project";
            this.ConvertProj.UseVisualStyleBackColor = true;
            this.ConvertProj.Click += new System.EventHandler(this.ConvertProj_Click);
            // 
            // UpdateButton
            // 
            this.UpdateButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.UpdateButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.UpdateButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.UpdateButton.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UpdateButton.Location = new System.Drawing.Point(3, 3);
            this.UpdateButton.Name = "UpdateButton";
            this.UpdateButton.Size = new System.Drawing.Size(240, 30);
            this.UpdateButton.TabIndex = 0;
            this.UpdateButton.Text = "Install Compiler";
            this.UpdateButton.UseVisualStyleBackColor = true;
            this.UpdateButton.Click += new System.EventHandler(this.UpdateButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(250, 204);
            this.Controls.Add(this.InnerForm);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Serious\' MP Tool";
            this.InnerForm.ControlContents.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Refract.UI.Core.Controls.CBorderedForm InnerForm;
        private System.Windows.Forms.Button UpdateButton;
        private System.Windows.Forms.Button ConvertProj;
        private System.Windows.Forms.Button InstallVSCExt;
        private System.Windows.Forms.Button JoinDiscord;
        private System.Windows.Forms.Button CreateDefaultProject;
    }
}

