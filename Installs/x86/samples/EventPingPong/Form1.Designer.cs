namespace Microsoft.Sample.EventPingPong
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.subscribeEventsButton = new System.Windows.Forms.RadioButton();
            this.publishEventsButton = new System.Windows.Forms.RadioButton();
            this.stopPubSubButton = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.roundtripRate = new System.Windows.Forms.TextBox();
            this.numRoundtrips = new System.Windows.Forms.Label();
            this.eventNumberSent = new System.Windows.Forms.TextBox();
            this.quitButton = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.subscribeEventsButton);
            this.groupBox1.Controls.Add(this.publishEventsButton);
            this.groupBox1.Controls.Add(this.stopPubSubButton);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(31, 28);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(228, 118);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Current State";
            // 
            // subscribeEventsButton
            // 
            this.subscribeEventsButton.AutoSize = true;
            this.subscribeEventsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.subscribeEventsButton.Location = new System.Drawing.Point(17, 75);
            this.subscribeEventsButton.Name = "subscribeEventsButton";
            this.subscribeEventsButton.Size = new System.Drawing.Size(179, 24);
            this.subscribeEventsButton.TabIndex = 2;
            this.subscribeEventsButton.TabStop = true;
            this.subscribeEventsButton.Text = "Subscribing to events";
            this.subscribeEventsButton.UseVisualStyleBackColor = true;
            this.subscribeEventsButton.CheckedChanged += new System.EventHandler(this.subscribeEventsButton_CheckedChanged);
            // 
            // publishEventsButton
            // 
            this.publishEventsButton.AutoSize = true;
            this.publishEventsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.publishEventsButton.Location = new System.Drawing.Point(17, 51);
            this.publishEventsButton.Name = "publishEventsButton";
            this.publishEventsButton.Size = new System.Drawing.Size(150, 24);
            this.publishEventsButton.TabIndex = 1;
            this.publishEventsButton.TabStop = true;
            this.publishEventsButton.Text = "Publishing events";
            this.publishEventsButton.UseVisualStyleBackColor = true;
            this.publishEventsButton.CheckedChanged += new System.EventHandler(this.publishEventsButton_CheckedChanged);
            // 
            // stopPubSubButton
            // 
            this.stopPubSubButton.AutoSize = true;
            this.stopPubSubButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.stopPubSubButton.Location = new System.Drawing.Point(17, 28);
            this.stopPubSubButton.Name = "stopPubSubButton";
            this.stopPubSubButton.Size = new System.Drawing.Size(88, 24);
            this.stopPubSubButton.TabIndex = 0;
            this.stopPubSubButton.TabStop = true;
            this.stopPubSubButton.Text = "Stopped";
            this.stopPubSubButton.UseVisualStyleBackColor = true;
            this.stopPubSubButton.CheckedChanged += new System.EventHandler(this.stopPubSubButton_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(31, 294);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(205, 24);
            this.label2.TabIndex = 5;
            this.label2.Text = "Roundtrips per Second";
            // 
            // roundtripRate
            // 
            this.roundtripRate.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.roundtripRate.Location = new System.Drawing.Point(254, 294);
            this.roundtripRate.Name = "roundtripRate";
            this.roundtripRate.ReadOnly = true;
            this.roundtripRate.Size = new System.Drawing.Size(302, 29);
            this.roundtripRate.TabIndex = 6;
            // 
            // numRoundtrips
            // 
            this.numRoundtrips.AutoSize = true;
            this.numRoundtrips.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.numRoundtrips.Location = new System.Drawing.Point(31, 207);
            this.numRoundtrips.Name = "numRoundtrips";
            this.numRoundtrips.Size = new System.Drawing.Size(195, 24);
            this.numRoundtrips.TabIndex = 7;
            this.numRoundtrips.Text = "Number of Roundtrips";
            // 
            // eventNumberSent
            // 
            this.eventNumberSent.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.eventNumberSent.Location = new System.Drawing.Point(254, 207);
            this.eventNumberSent.Name = "eventNumberSent";
            this.eventNumberSent.ReadOnly = true;
            this.eventNumberSent.Size = new System.Drawing.Size(302, 29);
            this.eventNumberSent.TabIndex = 8;
            // 
            // quitButton
            // 
            this.quitButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.quitButton.Location = new System.Drawing.Point(481, 379);
            this.quitButton.Name = "quitButton";
            this.quitButton.Size = new System.Drawing.Size(75, 29);
            this.quitButton.TabIndex = 2;
            this.quitButton.Text = "Quit";
            this.quitButton.UseVisualStyleBackColor = true;
            this.quitButton.Click += new System.EventHandler(this.quitButton_Click);
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(634, 464);
            this.Controls.Add(this.quitButton);
            this.Controls.Add(this.eventNumberSent);
            this.Controls.Add(this.numRoundtrips);
            this.Controls.Add(this.roundtripRate);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.groupBox1);
            this.Name = "Form1";
            this.Text = "Event Ping Pong";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton subscribeEventsButton;
        private System.Windows.Forms.RadioButton publishEventsButton;
        private System.Windows.Forms.RadioButton stopPubSubButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox roundtripRate;
        private System.Windows.Forms.Label numRoundtrips;
        private System.Windows.Forms.TextBox eventNumberSent;
        private System.Windows.Forms.Button quitButton;
        private System.Windows.Forms.Timer timer1;



    }
}

