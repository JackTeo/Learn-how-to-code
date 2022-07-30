using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video.DirectShow;
using AForge.Video;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing.Drawing2D;

namespace RobotShootLaserIntoEyes
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            serialPort1.Open();
        }

        FilterInfoCollection filter;
        VideoCaptureDevice device;

        private void Form1_Load(object sender, EventArgs e)
        {
            filter = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            ComboBox cboDevice = new ComboBox();
            foreach (FilterInfo device in filter)
            {
                cboDevice.Items.Add(device.Name);
            }
            cboDevice.SelectedIndex = 0;

            device = new VideoCaptureDevice(filter[cboDevice.SelectedIndex].MonikerString);
            device.NewFrame += Device_NewFrame;
            device.Start();

        }

        static readonly CascadeClassifier cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_alt_tree.xml");

        private void Device_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // Get Screen size
            int fullScreenY = picBox.Height;
            int fullScreenX = picBox.Width;
            int halfScreenY = fullScreenY / 2;
            int halfScreenX = fullScreenX / 2;

            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
            Image<Bgr, byte> grayImage = new Image<Bgr, byte>(bitmap);
            Rectangle[] rectangles = cascadeClassifier.DetectMultiScale(grayImage, 1.2, 1);
            foreach(Rectangle rectangle in rectangles)
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    using(Pen pen = new Pen(Color.Red, 1))
                    {
                        graphics.DrawRectangle(pen, rectangle);

                        int xpos = rectangle.X + (rectangle.Width / 2);
                        int ypos = rectangle.Y + (rectangle.Height / 2);

                        // Getting the center of the detected area
                        int x = halfScreenX - xpos;
                        int y = halfScreenY - ypos;

                        // Debug use
                        // Drawing small rectangle to make sure I picked up the center of detection
                        Rectangle smallRec = new Rectangle();
                        smallRec.X = xpos;
                        smallRec.Y = ypos;
                        smallRec.Width = 5;
                        smallRec.Height = 5;
                        // Draw rectangle
                        graphics.DrawRectangle(pen, smallRec);

                        // Getting angle using formula
                        int degreeX = (int)(90 + tanCal(x));
                        int degreeY = (int)(90 + tanCal(y));

                        // Sending angle as output to Arduino
                        serialPort1.Write(degreeY + "y");
                        serialPort1.Write(degreeX + "x");


                        //Console.WriteLine(degreeX);
                        //Console.WriteLine(degreeY);

                        //Console.WriteLine(tanCal(x));
                        //Console.WriteLine(tanCal(y));

                        //Console.WriteLine();
                    }
                }
            }
            picBox.Image = bitmap;
        }

        // Convert Distance to Angle formula
        private double tanCal(double x)
        {
            // 690 is a static approximate distance from to my face in mm
            // Change the value if your room is bigger or smaller
            return Math.Atan(x/720) * 180 / Math.PI;
        }
    }
}
