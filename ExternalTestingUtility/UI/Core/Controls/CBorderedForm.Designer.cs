
namespace Refract.UI.Core.Controls
{
    partial class CBorderedForm
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.MainPanel = new System.Windows.Forms.Panel();
            this.TitleBar = new Refract.UI.Core.Controls.CTitleBar();
            this.DesignerContents = new System.Windows.Forms.Panel();
            this.MainPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainPanel
            // 
            this.MainPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MainPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.MainPanel.Controls.Add(this.DesignerContents);
            this.MainPanel.Controls.Add(this.TitleBar);
            this.MainPanel.Location = new System.Drawing.Point(2, 2);
            this.MainPanel.Name = "MainPanel";
            this.MainPanel.Size = new System.Drawing.Size(696, 496);
            this.MainPanel.TabIndex = 0;
            // 
            // TitleBar
            // 
            this.TitleBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(36)))), ((int)(((byte)(36)))));
            this.TitleBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.TitleBar.Location = new System.Drawing.Point(0, 0);
            this.TitleBar.Name = "TitleBar";
            this.TitleBar.Size = new System.Drawing.Size(696, 32);
            this.TitleBar.TabIndex = 0;
            // 
            // DesignerContents
            // 
            this.DesignerContents.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DesignerContents.Location = new System.Drawing.Point(0, 32);
            this.DesignerContents.Name = "DesignerContents";
            this.DesignerContents.Size = new System.Drawing.Size(696, 464);
            this.DesignerContents.TabIndex = 1;
            // 
            // CBorderedForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DodgerBlue;
            this.Controls.Add(this.MainPanel);
            this.Name = "CBorderedForm";
            this.Size = new System.Drawing.Size(700, 500);
            this.MainPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel MainPanel;
        private CTitleBar TitleBar;
        private System.Windows.Forms.Panel DesignerContents;
    }
}
