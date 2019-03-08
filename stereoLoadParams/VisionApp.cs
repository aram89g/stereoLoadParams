// DiresctShow
using DirectShowLib;
// EMGU
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
// System
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;


namespace stereoLoadParams
{
    public partial class VisionApp : Form
    {
        static bool imageCalibration; // Calibration flag, do new calibration or load pramaters from old one
        static bool debugMode = false; // Debug flag

        public Tello rmt;
        static public string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        DateTime baseTime = new DateTime();
        Mat telloFrame = new Mat("tello.jpeg");

        #region Flight plan
        public string[] flightPlanFile = File.ReadAllLines(desktop + @"\flightPlan.txt");
        Queue<Point3D> flightPlan = new Queue<Point3D>();
        #endregion

        #region Target coordinates
        double X_target;
        double Y_target;
        double Z_target;
        #endregion

        #region Cameras
        VideoCapture capLeft;
        VideoCapture capRight;
        Video_Device[] WebCams;
        #endregion

        #region Video recorder
        VideoWriter videoWrite;
        string videoPath = desktop + @"\drone_recording.mp4";
        int videoFourcc;
        int videoWidth;
        int videoHeight;
        int videoFps;        
        #endregion

        #region Frame matrices
        //Frame matrices
        Mat frameLeft = new Mat();
        Mat frameRight = new Mat();
        Mat frameLeftRectified = new Mat();
        Mat frameRightRectified = new Mat();
        Mat leftUnrectified = new Mat();
        Mat rightUnrectified = new Mat();
        Mat grayLeft = new Mat();
        Mat grayRight = new Mat();
        Mat chessFrameL = new Mat();
        Mat chessFrameR = new Mat();
        #endregion

        #region Points Arrays
        VectorOfVectorOfPoint3D32F objectPointsV = new VectorOfVectorOfPoint3D32F();
        VectorOfPoint3D32F obj = new VectorOfPoint3D32F();

        VectorOfPointF cornersVecLeft = new VectorOfPointF();
        VectorOfPointF cornersVecRight = new VectorOfPointF();

        MCvPoint3D32f[][] cornersObjectPoints = new MCvPoint3D32f[bufferLength][];
        PointF[][] imagePoints1 = new PointF[bufferLength][];
        PointF[][] imagePoints2 = new PointF[bufferLength][];
        #endregion

        #region Chessboard Size
        const int width = 9; //width of chessboard no. squares in width - 1
        const int height = 6; // height of chess board no. squares in heigth - 1
        Size patternSize = new Size(width, height); //size of chess board to be detected
        #endregion

        #region Camera Matrices
        Mat newCamMat1 = new Mat(3, 3, DepthType.Cv64F, 1);
        Mat newCamMat2 = new Mat(3, 3, DepthType.Cv64F, 1);
        Mat camMat1 = new Mat(3, 3, DepthType.Cv64F, 1);
        Mat camMat2 = new Mat(3, 3, DepthType.Cv64F, 1);
        Mat dist1 = new Mat(1, 5, DepthType.Cv64F, 1);
        Mat dist2 = new Mat(1, 5, DepthType.Cv64F, 1);
        Mat essential = new Mat(3, 3, DepthType.Cv64F, 1);
        Mat fundamental = new Mat(3, 3, DepthType.Cv64F, 1);
        Mat R = new Mat(3, 3, DepthType.Cv64F, 1);
        Mat T = new Mat(3, 1, DepthType.Cv64F, 1);

        Mat R1 = new Mat(3, 3, DepthType.Cv64F, 1);
        Mat R2 = new Mat(3, 3, DepthType.Cv64F, 1);
        Mat P1 = new Mat(3, 3, DepthType.Cv64F, 1);
        Mat P2 = new Mat(3, 3, DepthType.Cv64F, 1);
        Mat _P1 = new Mat();
        Mat _P2 = new Mat();
        Mat Q = new Mat();

        Mat[] rvecs = new Mat[3];
        Mat[] tvecs = new Mat[3];
        #endregion

        #region Rectification variables
        Rectangle Rec1 = new Rectangle(); //Rectangle Calibrated in camera 1
        Rectangle Rec2 = new Rectangle(); //Rectangle Caliubrated in camera 2

        Mat rmapx1 = new Mat();
        Mat rmapx2 = new Mat();
        Mat rmapy1 = new Mat();
        Mat rmapy2 = new Mat();
        #endregion

        #region Image Processing Variables
        //Determines boundary of brightness while turning grayscale image to binary(black-white) image
        private const int Threshold = 200;

        //Erosion to remove noise(reduce white pixel zones)
        private const int ErodeIterations = 3;

        //Dilation to enhance erosion survivors(enlarge white pixel zones)
        private const int DilateIterations = 3;
        //Window names used in CvInvoke.Imshow calls
        private const string BackgroundFrameWindowName_l = "Left Camera - Background Frame";
        private const string BackgroundFrameWindowName_r = "Right Camera - Background Frame";
        private const string RawFrameWindowName_l = "Left Camera - Raw Frame";
        private const string RawFrameWindowName_r = "Right Camera - Raw Frame";
        private const string GrayscaleDiffFrameWindowName = "Grayscale Difference Frame";
        private const string BinaryDiffFrameWindowName = "Binary Difference Frame";
        private const string DenoisedDiffFrameWindowName = "Denoised Difference Frame";
        private const string FinalFrameWindowName_l = "Left Camera - Final Frame";
        private const string FinalFrameWindowName_r = "Right Camera - Right Camera - Final Frame";

        private static Mat rawFrame_l = new Mat();// Frame as obtained from video
        private static Mat rawFrame_r = new Mat();// Frame as obtained from video
        private static Mat backgroundFrame_l = new Mat();// Frame used as base for change detection
        private static Mat backgroundFrame_r = new Mat();// Frame used as base for change detection
        private static Mat diffFrame_l = new Mat();// Image showing differences between background and raw frame
        private static Mat diffFrame_r = new Mat();// Image showing differences between background and raw frame
        private static Mat grayscaleDiffFrame_l = new Mat();// Image showing differences in 8-bit color depth
        private static Mat grayscaleDiffFrame_r = new Mat();// Image showing differences in 8-bit color depth
        private static Mat binaryDiffFrame_l = new Mat();// Image showing changed areas in white and unchanged in black
        private static Mat binaryDiffFrame_r = new Mat();// Image showing changed areas in white and unchanged in black
        private static Mat denoisedDiffFrame_l = new Mat();// Image with irrelevant changes removed with opening operation
        private static Mat denoisedDiffFrame_r = new Mat();// Image with irrelevant changes removed with opening operation
        private static Mat finalFrame_l = new Mat();// Video frame with detected object marked
        private static Mat finalFrame_r = new Mat();// Video frame with detected object marked

        private static MCvScalar drawingColor = new Bgr(Color.Red).MCvScalar;
        private static Point point_center_l = new Point(0, 0);
        private static Point point_center_r = new Point(0, 0);
        private static bool left_camera = true;

        private static double X_3d = 0.0;
        private static double Y_3d = 0.0;
        private static double Z_3d = 0.0;
        #endregion

        public VisionApp()
        {
            InitializeComponent();
            label1.Text = "Created by:\nAram Gasparian\nRahamim Workenech";
            
            baseTime = DateTime.Now;
            #region Find systems cameras with DirectShow.Net dll
            // Find systems cameras with DirectShow.Net dll
            DsDevice[] _SystemCamereas = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            WebCams = new Video_Device[_SystemCamereas.Length];
            for (int i = 0; i < _SystemCamereas.Length; i++)
            {
                WebCams[i] = new Video_Device(i, _SystemCamereas[i].Name); //fill web cam array
                Camera_Selection_Left.Items.Add(WebCams[i].ToString());
                Camera_Selection_Right.Items.Add(WebCams[i].ToString());
            }
            if (Camera_Selection_Left.Items.Count > 0)
            {
                Camera_Selection_Left.SelectedIndex = 0; //Set the selected device the default
                Camera_Selection_Right.SelectedIndex = 1; //Set the selected device the default
            }
            #endregion
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            calibrationPath.Text = @"C:\Users\aram8\Desktop\calib";
        }
        // This code runs when you press the Start button
        public void StartButton_Click(object sender, EventArgs e)
        {
            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            try
            {
                File.Delete(desktop + "/EventLog.txt");
                ostrm = new FileStream(desktop + "/EventLog.txt", FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(ostrm);
            }
            catch (Exception er)
            {
                Console.WriteLine("Cannot open EventLog.txt for writing");
                Console.WriteLine(er.Message);
                return;
            }
            Console.SetOut(writer);

            if (capLeft == null) capLeft = new VideoCapture(Camera_Selection_Left.SelectedIndex);
            if (capRight == null) capRight = new VideoCapture(Camera_Selection_Right.SelectedIndex);
            if (capLeft.IsOpened && capRight.IsOpened) // check that both cameras working
            {
                // set video parameters
                videoFourcc = (int)capLeft.GetCaptureProperty(CapProp.FourCC);
                videoWidth  = 2*(int)capLeft.GetCaptureProperty(CapProp.FrameWidth);
                videoHeight = 2 * (int)capLeft.GetCaptureProperty(CapProp.FrameHeight);
                videoFps = (int)capLeft.GetCaptureProperty(CapProp.Fps);
                videoWrite = new VideoWriter(videoPath, videoFourcc, videoFps, new Size(videoWidth, videoHeight), true);                
                MessageBox.Show("Press ESCAPE key to close the program");
            }
            else
            {
                throw new Exception("Error opening a camera");
            }

            // Calibrate cameras
            Calibration();

            rmt = new Tello();

            // Obtaining and showing first frame of loaded video(used as the base for difference detection)
            backgroundFrame_l = capLeft.QueryFrame();
            backgroundFrame_r = capRight.QueryFrame();
            
            Mat backgroundLeftRemap = new Mat();
            Mat backgroundRightRemap = new Mat();

            // Apply transformation to rectify background images
            CvInvoke.Remap(backgroundFrame_l, backgroundLeftRemap, rmapx1, rmapy1, Inter.Linear);
            CvInvoke.Remap(backgroundFrame_r, backgroundRightRemap, rmapx2, rmapy2, Inter.Linear);
            //Mat backgroundLeftCrop = new Mat(backgroundLeftRemap, Rec1);
            //Mat backgroundRightCrop = new Mat(backgroundRightRemap, Rec2);

            Mat backgroundLeftCrop = backgroundLeftRemap.Clone();
            Mat backgroundRightCrop = backgroundRightRemap.Clone();

            //CvInvoke.Imshow(BackgroundFrameWindowName_l, backgroundLeftCrop);
            //CvInvoke.Imshow(BackgroundFrameWindowName_r, backgroundRightCrop);

            // Drone takeoff from the ground            
            CvInvoke.Imshow("Press Here", telloFrame);
            rmt.Takeoff();
            CvInvoke.WaitKey(5000);
            System.Media.SystemSounds.Exclamation.Play();

            //Handling video frames(image processing and contour detection)      
            VideoProcessingLoop(capLeft, backgroundLeftCrop, capRight, backgroundRightCrop, rmapx1, rmapy1, rmapx2, rmapy2, Rec1, Rec2);        
        }

        public void VideoProcessingLoop(VideoCapture capture_l, Mat backgroundFrame_l, VideoCapture capture_r, Mat backgroundFrame_r, Mat rmapx1, Mat rmapy1, Mat rmapx2, Mat rmapy2, Rectangle Rec1, Rectangle Rec2)
        {
            // Statistics timers
            var stopwatch = new Stopwatch();// Used to measure video processing performance
            var stopwatch_3d_calculate = new Stopwatch();// Used to measure 3D calculation performance
            
            int frameNumber = 1;
            Mat videoFrame = new Mat();

            #region Flight Plan
            int targetCnt = 1;
            StereoPoint3D drone = new StereoPoint3D();
            Point3D target;

            foreach (string line in flightPlanFile)
            {
                string[] coordinate = line.Split(',');
                double x = Convert.ToDouble(coordinate[0]);
                double y = Convert.ToDouble(coordinate[1]);
                double z = Convert.ToDouble(coordinate[2]);
                flightPlan.Enqueue(new Point3D(x, y, z));
            }
            target = flightPlan.Dequeue();
            #endregion
            
            while (true)// Loop video
            {
                Console.WriteLine("************************************Start Frame*************************************\n");
                // Getting next frame(null is returned if no further frame exists)
                rawFrame_l = capture_l.QueryFrame();
                rawFrame_r = capture_r.QueryFrame();
                
                // Rectify frames using rmap and crop according to roi (from calibration)
                CvInvoke.Remap(rawFrame_l, rawFrame_l, rmapx1, rmapy1, Inter.Linear);
                CvInvoke.Remap(rawFrame_r, rawFrame_r, rmapx2, rmapy2, Inter.Linear);
                //Mat cropLeft = new Mat(rawFrame_l, Rec1);
                //Mat cropRight = new Mat(rawFrame_r, Rec2);
                Mat cropLeft = rawFrame_l.Clone();
                Mat cropRight = rawFrame_r.Clone();
                rawFrame_l = cropLeft;
                rawFrame_r = cropRight;

                if (rawFrame_l != null && rawFrame_r != null)
                {                    
                    frameNumber++;
                    CvInvoke.HConcat(rawFrame_l, rawFrame_r, videoFrame);
                    videoWrite.Write(videoFrame);
                    // Process frame image to find drone location in the frame
                    stopwatch.Restart();// Frame processing calculate - Start
                    ProcessFrame(backgroundFrame_l, backgroundFrame_r, Threshold, ErodeIterations, DilateIterations);
                    stopwatch.Stop();// Frame processing calculate - End

                    // Calculate drone 3D coordinate
                    drone.CalculateCoordinate3D(point_center_l.X, point_center_r.X, point_center_r.Y);
                    X_3d = drone.GetX3D();
                    Y_3d = drone.GetY3D();
                    Z_3d = drone.GetZ3D();                    
                    Console.WriteLine($"Frame Number: {frameNumber}\n");

                    // Check drone position accodring to target and update drone command                    
                    rmt.InstructionCalculate(drone, ref target);

                    // check and update if needed target coordinate
                    if (target.arrived)
                    {                        
                        System.Media.SystemSounds.Beep.Play();
                        targetCnt++;
                        if(flightPlan.Count > 0)
                            target = flightPlan.Dequeue();
                        else
                        {
                            rmt.SendCommand("land");
                            CvInvoke.WaitKey(10000);
                            
                            Environment.Exit(0);
                        }                        
                    }
                    // Write data to Frame
                    WriteFrameInfo(stopwatch.ElapsedMilliseconds, frameNumber, targetCnt);

                    // Show all frame processing stages
                    ShowWindowsWithImageProcessingStages();

                    // Enable program exit from keyboard
                    int key = CvInvoke.WaitKey(5);
                    // Close program if Esc key was pressed
                    if (key == 27)
                    {
                        videoWrite.Dispose();
                        rmt.SendCommand("land");
                        CvInvoke.WaitKey(5000);
                        Environment.Exit(0);
                    }
                    Console.WriteLine("************************************End Frame*************************************\n\n");
                }
                else
                {
                    // Move to first frame
                    capture_l.SetCaptureProperty(CapProp.PosFrames, 0);
                    capture_r.SetCaptureProperty(CapProp.PosFrames, 0);
                    frameNumber = 0;
                }
            }
        }
        private void newCalib_CheckedChanged(object sender, EventArgs e)
        {
            if (newCalib.Checked)
            {
                imageCalibration = newCalib.Checked;
                loadCalib.Enabled = false;
            }
            else
            {
                loadCalib.Enabled = true;
            }
        }
        private void loadCalib_CheckedChanged(object sender, EventArgs e)
        {
            if (loadCalib.Checked)
            {
                imageCalibration = !(loadCalib.Checked);
                newCalib.Enabled = false;
            }
            else
            {
                newCalib.Enabled = true;
            }
        }
        private void browseBtn_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK && (loadCalib.Checked || newCalib.Checked))
            {
                calibrationPath.Text = folderBrowserDialog1.SelectedPath;
            }

            //if (openFileDialog1.ShowDialog() == DialogResult.OK && (loadCalib.Checked || newCalib.Checked))
            //{
            //    calibrationPath.Text = openFileDialog1.;
            //}
        }
    }
}
