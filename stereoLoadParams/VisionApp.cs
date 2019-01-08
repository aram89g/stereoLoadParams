//Remote Drone cxOF
using cxOF;
//DiresctShow
using DirectShowLib;
//EMGU
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace stereoLoadParams
{

    public partial class VisionApp : Form
    {
        static bool imageCalibration; // Calibration flag, do new calibration or load pramaters from old one
        static bool debugMode = false; // Debug flag

        public CxOF rmt = new CxOF(); // CX-OF drone remote
        #region Traget coordinates
        double X_target;
        double Y_target;
        double Z_target;
        #endregion

        VideoCapture capLeft;
        VideoCapture capRight;
        Video_Device[] WebCams;
        DateTime baseTime = new DateTime();

        //String imagesPath = "C:\\Users\\Public\\Pictures_test3\\";
        Mat cxFrame = new Mat("cxOF_image.jpg");
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

        #region Calibration variables
        //Calibration variables
        static int bufferLength = 20; // define how many good images needed
        int bufferSavepoint = 0;
        bool patternLeftFound; // True if chessboard found in image
        bool patternRightFound; // True if chessboard found in image
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
        private const int Threshold = 5;

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
        private static Boolean left_camera = true;

        private static Double X_3d = 0.0;
        private static Double Y_3d = 0.0;
        private static Double Z_3d = 0.0;
        //private static Double T_3d = 0.125; // [m]
        //private static Double f = 0.004; // Focal Length = 4mm for "LOGITECH HD WEBCAM C270"
        //private static Double x_pixel_left_camera = 0.0;
        //private static Double x_pixel_right_camera = 0.0;
        //private static Double x_pixels_amount = 640.0;
        //private static Double y_pixels_amount = 480.0;
        //private static Double ox = x_pixels_amount / 2; // width = 1980 => x_pixel_center = 1980/2  assumes pixels start from 0
        //private static Double oy = y_pixels_amount / 2; // height = 1080 => y_pixel_center = 1080/2 assumes pixels start from 0
        //private static Double x_width_length = (2 / (1.732050808)) * f;
        //private static Double y_width_length = x_width_length;
        //private static Double sx = x_width_length / x_pixels_amount;
        //private static Double sy = y_width_length / y_pixels_amount;
        //private static Double x_1 = 0.0;
        //private static Double x_2 = 0.0;
        //private static Double y_1 = 0.0;
        //private static Double y_2 = 0.0; // not in use
        #endregion

        public VisionApp()
        {
            InitializeComponent();
            label1.Text = "Created by:\nAram Gasparian\nRahamim Worknah";
            
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

        }
        // This code runs when you press the Start button
        public void StartButton_Click(object sender, EventArgs e)
        {
            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            try
            {
                ostrm = new FileStream("./Redirect.txt", FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(ostrm);
            }
            catch (Exception er)
            {
                Console.WriteLine("Cannot open Redirect.txt for writing");
                Console.WriteLine(er.Message);
                return;
            }
            Console.SetOut(writer);

            if (capLeft == null) capLeft = new VideoCapture(Camera_Selection_Left.SelectedIndex);
            if (capRight == null) capRight = new VideoCapture(Camera_Selection_Right.SelectedIndex);
            if (capLeft.IsOpened && capRight.IsOpened) // check that both cameras working
            {
                string str1 = "Press ESCAPE key in any image window to close the program.";
                MessageBox.Show(str1);
            }
            // Calibrate cameras
            Calibration();
            // Obtaining and showing first frame of loaded video(used as the base for difference detection)
            backgroundFrame_l = capLeft.QueryFrame();
            backgroundFrame_r = capRight.QueryFrame();
            Mat backgroundLeftRemap = new Mat();
            Mat backgroundRightRemap = new Mat();

            // Apply transformation to rectify background images
            CvInvoke.Remap(backgroundFrame_l, backgroundLeftRemap, rmapx1, rmapy1, Inter.Linear);
            CvInvoke.Remap(backgroundFrame_r, backgroundRightRemap, rmapx2, rmapy2, Inter.Linear);
            Mat backgroundLeftCrop = new Mat(backgroundLeftRemap, Rec1);
            Mat backgroundRightCrop = new Mat(backgroundRightRemap, Rec2);            
            CvInvoke.Imshow(BackgroundFrameWindowName_l, backgroundLeftCrop);
            CvInvoke.Imshow(BackgroundFrameWindowName_r, backgroundRightCrop);

            //backgroundLeftCrop.Save(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\bgLeft.jpg");
            //backgroundRightCrop.Save(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\bgRight.jpg");

            // Connect to drone and takeoff from the ground
            rmt.SendHandShake();
            rmt.Reset();
            Thread cxThread = new Thread(new ThreadStart(rmt.ThreadProc)); // Create thread to keep session alive with drone
            cxThread.Start();
            rmt.Send();
            CvInvoke.Imshow("Press Here", cxFrame);
            CvInvoke.WaitKey(5000);
            rmt.Takeoff();
            CvInvoke.WaitKey(2500);


            //Handling video frames(image processing and contour detection)      
            VideoProcessingLoop(capLeft, backgroundLeftCrop, capRight, backgroundRightCrop, rmapx1, rmapy1, rmapx2, rmapy2, Rec1, Rec2);        
        }

        //partial void Calibration();

        public void VideoProcessingLoop(VideoCapture capture_l, Mat backgroundFrame_l, VideoCapture capture_r, Mat backgroundFrame_r, Mat rmapx1, Mat rmapy1, Mat rmapx2, Mat rmapy2, Rectangle Rec1, Rectangle Rec2)
        {
            var stopwatch = new Stopwatch();// Used to measure video processing performance
            var stopwatch_3d_calculate = new Stopwatch();// Used to measure 3D calculation performance

            int frameNumber = 1;
            int targetCnt = 1;
            StereoPoint3D drone = new StereoPoint3D();
            Point3D Point1 = new Point3D(0.3, 0 ,1.4);
            Point3D Point2 = new Point3D(0.3, 0, 1.9);
            Point3D Point3 = new Point3D(-0.3, 0, 1.9);
            Point3D Point4 = new Point3D(-0.3, 0, 1.4);
            Point3D Point5 = new Point3D(0.3, 0, 1.4);
            Point3D target = Point1;
            while (true)// Loop video
            {
                Console.WriteLine("************************************Start Frame*************************************\n");
                // Getting next frame(null is returned if no further frame exists)
                rawFrame_l = capture_l.QueryFrame();
                rawFrame_r = capture_r.QueryFrame();

                // Rectify frames using rmap and crop according to roi (from calibration)
                CvInvoke.Remap(rawFrame_l, rawFrame_l, rmapx1, rmapy1, Inter.Linear);
                CvInvoke.Remap(rawFrame_r, rawFrame_r, rmapx2, rmapy2, Inter.Linear);
                Mat cropLeft = new Mat(rawFrame_l, Rec1);
                Mat cropRight = new Mat(rawFrame_r, Rec2);
                rawFrame_l = cropLeft;
                rawFrame_r = cropRight;

                if (rawFrame_l != null && rawFrame_r != null)
                {
                    frameNumber++;
                    
                    // Process frame image to find drone location in the frame
                    stopwatch.Restart();// Frame processing calculate - Start
                    ProcessFrame(backgroundFrame_l, backgroundFrame_r, Threshold, ErodeIterations, DilateIterations);
                    stopwatch.Stop();// Frame processing calculate - End

                    // Calculate drone 3D coordinate
                    stopwatch_3d_calculate.Restart(); // 3D calculate - Start
                    drone.CalculateCoordinate3D(point_center_l.X, point_center_r.X, point_center_r.Y);
                    X_3d = drone.GetX3D();
                    Y_3d = drone.GetY3D();
                    Z_3d = drone.GetZ3D();
                    
                    stopwatch_3d_calculate.Stop(); // 3D calculate - End
                    Console.WriteLine($"Frame Number: {frameNumber}\n");
                    Console.WriteLine($"Drone Coordinates:  [X: {X_3d}, Y: {Y_3d}, Z: {Z_3d}]\n");
                    Console.WriteLine("Drone Instructions:\n");
                    // Check drone position accodring to target and update drone command
                    rmt.InstructionCalculate(drone, ref target);
                    if (target.arrived)
                    {
                        System.Media.SystemSounds.Asterisk.Play();
                        targetCnt++;
                        switch (targetCnt)
                        {
                            case 2:
                                target = Point2;
                                break;
                            case 3:
                                target = Point3;
                                break;
                            case 4:
                                target = Point4;
                                break;
                            case 5:
                                target = Point5;                                
                                break;
                            default:
                                rmt.Land();
                                CvInvoke.WaitKey(10000);
                                Environment.Exit(0);
                                break;
                        }
                        
                    }
                    // Write data to Frame
                    WriteFrameInfo(stopwatch.ElapsedMilliseconds, stopwatch_3d_calculate.ElapsedMilliseconds, frameNumber, targetCnt);

                    // Show all frame processing stages
                    ShowWindowsWithImageProcessingStages();


                    //int key = CvInvoke.WaitKey(10);// Wait 10msec between frames to not overload video stream
                    // Close program if Esc key was pressed
                    //if (key == 27)
                    //{
                    //    rmt.Land();
                    //    CvInvoke.WaitKey(5000);
                    //    Environment.Exit(0);
                    //}
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
        private static void ProcessFrame(Mat backgroundFrame_l, Mat backgroundFrame_r, int threshold, int erodeIterations, int dilateIterations)
        {
            //Find difference between background(first) frame and current frame
            CvInvoke.AbsDiff(backgroundFrame_l, rawFrame_l, diffFrame_l);
            CvInvoke.AbsDiff(backgroundFrame_r, rawFrame_r, diffFrame_r);

            //Apply binary threshold to grayscale image(white pixel will mark difference)
            CvInvoke.CvtColor(diffFrame_l, grayscaleDiffFrame_l, ColorConversion.Bgr2Gray);
            CvInvoke.CvtColor(diffFrame_r, grayscaleDiffFrame_r, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(grayscaleDiffFrame_l, binaryDiffFrame_l, threshold, 255, ThresholdType.Binary);
            CvInvoke.Threshold(grayscaleDiffFrame_r, binaryDiffFrame_r, threshold, 255, ThresholdType.Binary);

            //Remove noise with opening operation(erosion followed by dilation)
            CvInvoke.Erode(binaryDiffFrame_l, denoisedDiffFrame_l, null, new Point(-1, -1), erodeIterations, BorderType.Default, new MCvScalar(1));
            CvInvoke.Erode(binaryDiffFrame_r, denoisedDiffFrame_r, null, new Point(-1, -1), erodeIterations, BorderType.Default, new MCvScalar(1));
            CvInvoke.Dilate(denoisedDiffFrame_l, denoisedDiffFrame_l, null, new Point(-1, -1), dilateIterations, BorderType.Default, new MCvScalar(1));
            CvInvoke.Dilate(denoisedDiffFrame_r, denoisedDiffFrame_r, null, new Point(-1, -1), dilateIterations, BorderType.Default, new MCvScalar(1));

            rawFrame_l.CopyTo(finalFrame_l);
            rawFrame_r.CopyTo(finalFrame_r);

            left_camera = true;
            DetectObject(denoisedDiffFrame_l, finalFrame_l);

            left_camera = false;
            DetectObject(denoisedDiffFrame_r, finalFrame_r);
        }
        private static void ShowWindowsWithImageProcessingStages()        
        {
            if (debugMode)
            {
                CvInvoke.Imshow(RawFrameWindowName_l, rawFrame_l);
                CvInvoke.Imshow(RawFrameWindowName_r, rawFrame_r);
                CvInvoke.Imshow(GrayscaleDiffFrameWindowName, grayscaleDiffFrame_l);
                CvInvoke.Imshow(BinaryDiffFrameWindowName, binaryDiffFrame_l);
                CvInvoke.Imshow(DenoisedDiffFrameWindowName, denoisedDiffFrame_l);
            }
            CvInvoke.Imshow(FinalFrameWindowName_l, finalFrame_l);
            CvInvoke.Imshow(FinalFrameWindowName_r, finalFrame_r);
        }
        private static void WriteMultilineText(Mat frame, string[] lines, Point origin)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                int y = i * 10 + origin.Y;// Moving down on each line
                CvInvoke.PutText(frame, lines[i], new Point(origin.X, y), FontFace.HersheyPlain, 0.8, drawingColor);
            }
        }
        private static void DetectObject(Mat detectionFrame, Mat displayFrame)
        {
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                //Build list of contours
                CvInvoke.FindContours(detectionFrame, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

                //Selecting largest contour
                if (contours.Size > 0)
                {
                    double maxArea = 0;
                    int chosen = 0;
                    for (int i = 0; i < contours.Size; i++)
                    {
                        VectorOfPoint contour = contours[i];

                        double area = CvInvoke.ContourArea(contour);
                        if (area > maxArea)
                        {
                            maxArea = area;
                            chosen = i;
                        }
                    }
                    //Draw on a frame
                    MarkDetectedObject(displayFrame, contours[chosen], maxArea);
                }
            }
        }
        private static void MarkDetectedObject(Mat frame, VectorOfPoint contour, double area)
        {
           //Getting minimal rectangle which contains the contour
           Rectangle box = CvInvoke.BoundingRectangle(contour);

            //Drawing contour and box around it
            CvInvoke.Polylines(frame, contour, true, drawingColor);
            CvInvoke.Rectangle(frame, box, drawingColor);

           //Write information next to marked object
           Point center = new Point(box.X + box.Width / 2, box.Y + box.Height / 2);
            if (left_camera == true)
            {
                point_center_l = center;
            }
            else
            {
                point_center_r = center;
            }

            var info = new string[] {
                $"Area: {area}",
                $"Position: {center.X}, {center.Y}",
            };

            WriteMultilineText(frame, info, new Point(box.Right + 5, center.Y));
        }
        private static void WriteFrameInfo(long elapsedMs, long _3d_elapsedMs, int frameNumber, int targetCnt )
        {
            var info = new string[] {
                $"Frame Number: {frameNumber}",
                $"Processing Time (Find the drone in each image): {elapsedMs} [ms]",
                $"3d Processing Time : {_3d_elapsedMs} ms",
                $"Left Camera - Position: {point_center_l.X}, {point_center_l.Y}",
                $"Right Camera - Position: {point_center_r.X}, {point_center_r.Y}",
                $"X_3d : {X_3d} [meter]",
                $"Y_3d : {Y_3d} [meter]",
                $"Z_3d : {Z_3d} [meter]",
                $"Destination: target {targetCnt}"
        };

            // Save frames to PC
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            Directory.CreateDirectory(desktop + "\\savedFrames");
            //finalFrame_l.Save(desktop + "\\savedFrames\\Left_" + frameNumber + ".jpg");
            //finalFrame_r.Save(desktop + "\\savedFrames\\Right_" + frameNumber + ".jpg");
            WriteMultilineText(finalFrame_l, info, new Point(5, 10));
            WriteMultilineText(finalFrame_r, info, new Point(5, 10));
            finalFrame_l.Save(desktop + "\\savedFrames\\" + frameNumber + "Left_withData.jpg");
            finalFrame_r.Save(desktop + "\\savedFrames\\" + frameNumber + "Right_withData.jpg");
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
