namespace SpriteChat
{
    partial class SpriteChatClient
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
            this.SuspendLayout();
            // 
            // CivilTerrorClient
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "CivilTerrorClient";
            this.Size = new System.Drawing.Size(400, 400);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.CivilTerrorClient_Paint);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.CivilTerrorClient_KeyUp);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
