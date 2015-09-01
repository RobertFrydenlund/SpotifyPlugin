using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using SpotifyPlugin;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SpotifyPluginDebugger
{
    public partial class DebugForm : Form
    {
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        public Measure[] measures;

        public Measure trackNameMeasure;
        public Measure albumNameMeasure;
        public Measure artistNameMeasure;
        public Measure lengthMeasure;
        public Measure progressMeasure;
        public Measure artMeasure;

        public DebugForm()
        {
#if DEBUG
            InitializeComponent();

            //InfoGatherer sAPI = new InfoGatherer(InfoGatherer.GetOAuth());
            //StatusControl.Restart();
            Thread.Sleep(200);
            Measure trackNameMeasure = new Measure();
            Measure albumNameMeasure = new Measure();
            Measure artistNameMeasure = new Measure();
            Measure lengthMeasure = new Measure();
            Measure progressMeasure = new Measure();
            artMeasure = new Measure();

            double d = 0.0;
            trackNameMeasure.Reload("0", "trackname", ref d);
            albumNameMeasure.Reload("0", "artistname", ref d);
            artistNameMeasure.Reload("0", "albumname", ref d);
            lengthMeasure.Reload("0", "length", ref d);
            progressMeasure.Reload("0", "progress", ref d);
            artMeasure.Reload("300", "albumart", ref d);
            measures = new Measure[]{ trackNameMeasure, albumNameMeasure, artistNameMeasure, lengthMeasure, progressMeasure, artMeasure };

            // Remember this class!
            //timer.Tick += new EventHandler(this.GetData);
            EventHandler what = new EventHandler((sender, e) => this.GetData(sender, e, measures));
            timer.Tick += what;
            timer.Interval = 200;
            timer.Start();
#endif
        }

        int i = 0;

        public void GetData(object sender, EventArgs e, Measure[] measures)
        {
            //Thread.Sleep(10);
            string toShow;
            toShow = "\n" + "Track: " + measures[0].GetString();
            toShow += "\n" + "Album: " + measures[1].GetString();
            toShow += "\n" + "Artist: " + measures[2].GetString();
            toShow += "\n";
            toShow += "\n" + "Length: " + measures[3].GetString();
            toShow += "\n" + "Progress: " + measures[4].GetString(); //.Update();
            toShow += "\n";
            //toShow += "\n" + String.Format("Subscribers ({0}): {1}", measures.Length, sAPI.subscribers);
            toShow += "\n" + StatusControl.getData();
            toShow += "\n" + measures[5].GetString();

           
            labDebug.Text = toShow;


            
            if (i > 5)
            {
                picDebug.ImageLocation = measures[5].GetString();
                i = 0;
            }
            i++;
        }
    }
}
