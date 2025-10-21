
namespace ImportXml
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.aprifile = new System.Windows.Forms.Button();
            this.openxml = new System.Windows.Forms.OpenFileDialog();
            this.importanag = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // aprifile
            // 
            this.aprifile.Location = new System.Drawing.Point(225, 132);
            this.aprifile.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.aprifile.Name = "aprifile";
            this.aprifile.Size = new System.Drawing.Size(943, 171);
            this.aprifile.TabIndex = 0;
            this.aprifile.Text = "Import Fattura da xml";
            this.aprifile.UseVisualStyleBackColor = true;
            this.aprifile.Click += new System.EventHandler(this.aprifile_Click);
            // 
            // openxml
            // 
            this.openxml.InitialDirectory = "C:\\xml_file";
            // 
            // importanag
            // 
            this.importanag.Location = new System.Drawing.Point(225, 392);
            this.importanag.Margin = new System.Windows.Forms.Padding(6);
            this.importanag.Name = "importanag";
            this.importanag.Size = new System.Drawing.Size(943, 171);
            this.importanag.TabIndex = 1;
            this.importanag.Text = "Importa Anagrafiche";
            this.importanag.UseVisualStyleBackColor = true;
            this.importanag.Click += new System.EventHandler(this.importanag_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1486, 960);
            this.Controls.Add(this.importanag);
            this.Controls.Add(this.aprifile);
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.Name = "Form1";
            this.Text = "Import da file";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button aprifile;
        private System.Windows.Forms.OpenFileDialog openxml;
        private System.Windows.Forms.Button importanag;
    }
}

