
namespace t7c_installer
{
    partial class ImportDialog
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
            this.StartImportButton = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.OutputLabel = new System.Windows.Forms.Label();
            this.OutputBtn = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.ImportLabel = new System.Windows.Forms.Label();
            this.SelectImportBtn = new System.Windows.Forms.Button();
            this.InnerForm.ControlContents.SuspendLayout();
            this.SuspendLayout();
            // 
            // InnerForm
            // 
            this.InnerForm.BackColor = System.Drawing.Color.DodgerBlue;
            // 
            // InnerForm.ControlContents
            // 
            this.InnerForm.ControlContents.Controls.Add(this.StartImportButton);
            this.InnerForm.ControlContents.Controls.Add(this.panel2);
            this.InnerForm.ControlContents.Controls.Add(this.OutputLabel);
            this.InnerForm.ControlContents.Controls.Add(this.OutputBtn);
            this.InnerForm.ControlContents.Controls.Add(this.panel1);
            this.InnerForm.ControlContents.Controls.Add(this.ImportLabel);
            this.InnerForm.ControlContents.Controls.Add(this.SelectImportBtn);
            this.InnerForm.ControlContents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InnerForm.ControlContents.Enabled = true;
            this.InnerForm.ControlContents.Location = new System.Drawing.Point(0, 32);
            this.InnerForm.ControlContents.Name = "ControlContents";
            this.InnerForm.ControlContents.Size = new System.Drawing.Size(331, 100);
            this.InnerForm.ControlContents.TabIndex = 1;
            this.InnerForm.ControlContents.Visible = true;
            this.InnerForm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InnerForm.Location = new System.Drawing.Point(0, 0);
            this.InnerForm.Name = "InnerForm";
            this.InnerForm.Size = new System.Drawing.Size(335, 136);
            this.InnerForm.TabIndex = 0;
            this.InnerForm.TitleBarTitle = "Project Migration Utility";
            this.InnerForm.UseTitleBar = true;
            // 
            // StartImportButton
            // 
            this.StartImportButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.StartImportButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.StartImportButton.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.StartImportButton.Location = new System.Drawing.Point(3, 71);
            this.StartImportButton.Name = "StartImportButton";
            this.StartImportButton.Size = new System.Drawing.Size(113, 26);
            this.StartImportButton.TabIndex = 5;
            this.StartImportButton.Text = "Start Migration";
            this.StartImportButton.UseVisualStyleBackColor = true;
            this.StartImportButton.Click += new System.EventHandler(this.StartImportButton_Click);
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.ControlDark;
            this.panel2.Location = new System.Drawing.Point(-1, 66);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(457, 2);
            this.panel2.TabIndex = 4;
            // 
            // OutputLabel
            // 
            this.OutputLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OutputLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OutputLabel.Location = new System.Drawing.Point(122, 41);
            this.OutputLabel.Name = "OutputLabel";
            this.OutputLabel.Size = new System.Drawing.Size(202, 17);
            this.OutputLabel.TabIndex = 3;
            this.OutputLabel.Text = "No Output Folder Selected";
            // 
            // OutputBtn
            // 
            this.OutputBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.OutputBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.OutputBtn.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OutputBtn.Location = new System.Drawing.Point(3, 37);
            this.OutputBtn.Name = "OutputBtn";
            this.OutputBtn.Size = new System.Drawing.Size(113, 26);
            this.OutputBtn.TabIndex = 2;
            this.OutputBtn.Text = "Select Output";
            this.OutputBtn.UseVisualStyleBackColor = true;
            this.OutputBtn.Click += new System.EventHandler(this.OutputBtn_Click);
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ControlDark;
            this.panel1.Location = new System.Drawing.Point(0, 32);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(457, 2);
            this.panel1.TabIndex = 1;
            // 
            // ImportLabel
            // 
            this.ImportLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ImportLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ImportLabel.Location = new System.Drawing.Point(122, 7);
            this.ImportLabel.Name = "ImportLabel";
            this.ImportLabel.Size = new System.Drawing.Size(202, 17);
            this.ImportLabel.TabIndex = 1;
            this.ImportLabel.Text = "No Project Selected";
            // 
            // SelectImportBtn
            // 
            this.SelectImportBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.SelectImportBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.SelectImportBtn.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SelectImportBtn.Location = new System.Drawing.Point(3, 3);
            this.SelectImportBtn.Name = "SelectImportBtn";
            this.SelectImportBtn.Size = new System.Drawing.Size(113, 26);
            this.SelectImportBtn.TabIndex = 0;
            this.SelectImportBtn.Text = "Select Project";
            this.SelectImportBtn.UseVisualStyleBackColor = true;
            this.SelectImportBtn.Click += new System.EventHandler(this.SelectImportButton_Click);
            // 
            // ImportDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(335, 136);
            this.Controls.Add(this.InnerForm);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ImportDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Serious\' MP Tool";
            this.InnerForm.ControlContents.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Refract.UI.Core.Controls.CBorderedForm InnerForm;
        private System.Windows.Forms.Button SelectImportBtn;
        private System.Windows.Forms.Label ImportLabel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label OutputLabel;
        private System.Windows.Forms.Button OutputBtn;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button StartImportButton;
    }
}

