namespace Graph3D6._0
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
			this.picBox = new System.Windows.Forms.PictureBox();
			this.bBody = new System.Windows.Forms.Button();
			this.bLines = new System.Windows.Forms.Button();
			this.bPhong = new System.Windows.Forms.Button();
			this.bGouraud = new System.Windows.Forms.Button();
			this.bPlochy = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.picBox)).BeginInit();
			this.SuspendLayout();
			// 
			// picBox
			// 
			this.picBox.Location = new System.Drawing.Point(0, 33);
			this.picBox.Name = "picBox";
			this.picBox.Size = new System.Drawing.Size(1024, 768);
			this.picBox.TabIndex = 0;
			this.picBox.TabStop = false;
			// 
			// bBody
			// 
			this.bBody.Location = new System.Drawing.Point(0, 4);
			this.bBody.Name = "bBody";
			this.bBody.Size = new System.Drawing.Size(75, 23);
			this.bBody.TabIndex = 1;
			this.bBody.Text = "Body";
			this.bBody.UseVisualStyleBackColor = true;
			this.bBody.Click += new System.EventHandler(this.bBody_Click);
			// 
			// bLines
			// 
			this.bLines.Location = new System.Drawing.Point(81, 4);
			this.bLines.Name = "bLines";
			this.bLines.Size = new System.Drawing.Size(75, 23);
			this.bLines.TabIndex = 2;
			this.bLines.Text = "Ciary";
			this.bLines.UseVisualStyleBackColor = true;
			this.bLines.Click += new System.EventHandler(this.bLines_Click);
			// 
			// bPhong
			// 
			this.bPhong.Location = new System.Drawing.Point(243, 4);
			this.bPhong.Name = "bPhong";
			this.bPhong.Size = new System.Drawing.Size(75, 23);
			this.bPhong.TabIndex = 4;
			this.bPhong.Text = "Phong";
			this.bPhong.UseVisualStyleBackColor = true;
			this.bPhong.Click += new System.EventHandler(this.bPhong_Click);
			// 
			// bGouraud
			// 
			this.bGouraud.Location = new System.Drawing.Point(324, 4);
			this.bGouraud.Name = "bGouraud";
			this.bGouraud.Size = new System.Drawing.Size(75, 23);
			this.bGouraud.TabIndex = 6;
			this.bGouraud.Text = "Gouraud";
			this.bGouraud.UseVisualStyleBackColor = true;
			this.bGouraud.Click += new System.EventHandler(this.bGouraud_Click);
			// 
			// bPlochy
			// 
			this.bPlochy.Location = new System.Drawing.Point(162, 4);
			this.bPlochy.Name = "bPlochy";
			this.bPlochy.Size = new System.Drawing.Size(75, 23);
			this.bPlochy.TabIndex = 7;
			this.bPlochy.Text = "Plochy";
			this.bPlochy.UseVisualStyleBackColor = true;
			this.bPlochy.Click += new System.EventHandler(this.bPlochy_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(984, 801);
			this.Controls.Add(this.bPlochy);
			this.Controls.Add(this.bGouraud);
			this.Controls.Add(this.bPhong);
			this.Controls.Add(this.bLines);
			this.Controls.Add(this.bBody);
			this.Controls.Add(this.picBox);
			this.Name = "Form1";
			this.Text = "Form1";
			((System.ComponentModel.ISupportInitialize)(this.picBox)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private PictureBox picBox;
		private Button bBody;
		private Button bLines;
		private Button bPhong;
		private Button bGouraud;
		private Button bPlochy;
	}
}