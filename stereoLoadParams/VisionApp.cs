using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

//EMGU
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

//DiresctShow
using DirectShowLib;

//Remote Drone cxOF
using cxOF;

namespace stereoLoadParams
{
    static public class TargetCoordinate
    {
        static public double X_target = 0;
        static public double Y_target = 0;
        static public double Z_target = 0.8;

        //static double getX()
        //{
        //    return X_target;
        //}
        //static double getY()
        //{
        //    return Y_target;
        //}
        //static double getZ()
        //{
        //    return Z_target;
        //}
    }

    public partial class VisionApp : Form
    {
        bool csharpCalib = true; // for debug
        bool debugMode = false; // for debug

        CxOF rmt = new CxOF();
        #region Traget coordinates
        double X_target;
        double Y_target;
        double Z_target;
        #endregion

        VideoCapture capLeft;
        VideoCapture capRight;
        Video_Device[] WebCams;
        TimeSpan diffLeft;
        TimeSpan diffRight;
        DateTime baseTime = new DateTime();

        String imagesPath = "C:\\Users\\Public\\Pictures_3011\\";

        #region Frame matrices
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
        //Mat disparity = new Mat();
        #endregion

        #region Calibration variables
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

        #region  Chessboard Size
        const int width = 9; //width of chessboard no. squares in width - 1
        const int height = 6; // height of chess board no. squares in heigth - 1
        Size patternSize = new Size(width, height); //size of chess board to be detected
        #endregion

        #region Cameras Paramemters
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
        Rectangle Rec3 = new Rectangle(); //Rectangle Caliubrated in camera 2
        Rectangle Rec4 = new Rectangle(); //Rectangle Caliubrated in camera 2

        Mat rmapx1 = new Mat();
        Mat rmapx2 = new Mat();
        Mat rmapy1 = new Mat();
        Mat rmapy2 = new Mat();
        #endregion

        #region Rahamim
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

        private static Double T_3d = 0.125; // [m]
        private static Double f = 0.004; // Focal Length = 4mm for "LOGITECH HD WEBCAM C270"
        private static Double X_3d = 0.0;
        private static Double Y_3d = 0.0;
        private static Double Z_3d = 0.0;
        private static Double x_pixel_left_camera = 0.0;
        private static Double x_pixel_right_camera = 0.0;
        private static Double x_pixels_amount = 640.0;
        private static Double y_pixels_amount = 480.0;
        private static Double ox = x_pixels_amount / 2; // width = 1980 => x_pixel_center = 1980/2  assumes pixels start from 0
        private static Double oy = y_pixels_amount / 2; // height = 1080 => y_pixel_center = 1080/2 assumes pixels start from 0
        private static Double x_width_length = (2 / (1.732050808)) * f;
        private static Double y_width_length = x_width_length;
        private static Double sx = x_width_length / x_pixels_amount;
        private static Double sy = y_width_length / y_pixels_amount;
        private static Double x_1 = 0.0;
        private static Double x_2 = 0.0;
        private static Double y_1 = 0.0;
        //private static Double y_2 = 0.0; // not in use
        #endregion

        public VisionApp()
        {
            InitializeComponent();
            baseTime = DateTime.Now;
            #region Find systems cameras with DirectShow.Net dll
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
        // Not relevent - used for debug
        private void ProcessFrame2(object sender, EventArgs e)
        {

            #region Frame Aquasition   
            capLeft.Retrieve(frameLeft, 0);
            diffLeft = DateTime.Now - baseTime;
            capRight.Retrieve(frameRight, 0);
            diffRight = DateTime.Now - baseTime;
            //CvInvoke.CvtColor(frameLeft, grayLeft, ColorConversion.Bgr2Gray);
            //CvInvoke.CvtColor(frameRight, grayRight, ColorConversion.Bgr2Gray);
            #endregion

            leftUnrectified = frameLeft.Clone();
            rightUnrectified = frameRight.Clone();
            // Add horizontal lines to unrectified images
            //for (int j = 0; j < 480 / 20; j++) 
            //{
            //    Rectangle line = new Rectangle(0, j * 20, 640, 1);
            //    CvInvoke.Rectangle(leftUnrectified, line, new MCvScalar(0, 0, 255), 1, LineType.EightConnected, 0);
            //    CvInvoke.Rectangle(rightUnrectified, line, new MCvScalar(0, 0, 255), 1, LineType.EightConnected, 0);
            //}
            if (debugMode == true)
            {
                // Check that frames corosspond in time (The number is milliseconds)
                CvInvoke.PutText(leftUnrectified, diffLeft.Milliseconds.ToString(), new Point(10, 10), FontFace.HersheyPlain, 1.0, new Bgr(0, 0, 255).MCvScalar);
                CvInvoke.PutText(rightUnrectified, diffRight.Milliseconds.ToString(), new Point(10, 10), FontFace.HersheyPlain, 1.0, new Bgr(0, 0, 255).MCvScalar);
            }
            CvInvoke.Imshow("Unrectified Left", leftUnrectified);
            CvInvoke.Imshow("Unrectified Right", rightUnrectified);

            // Apply transformation to rectify images
            CvInvoke.Remap(frameLeft, frameLeftRectified, rmapx1, rmapy1, Inter.Linear);
            CvInvoke.Remap(frameRight, frameRightRectified, rmapx2, rmapy2, Inter.Linear);


            // Add horizontal lines to rectified images
            //for (int j = 0; j < 480 / 20; j++)
            //{
            //    Rectangle line = new Rectangle(0, j * 20, 640, 1);
            //    CvInvoke.Rectangle(frameLeftRectified,  line, new MCvScalar(0, 255, 0), 1, LineType.EightConnected, 0);
            //    CvInvoke.Rectangle(frameRightRectified, line, new MCvScalar(0, 255, 0), 1, LineType.EightConnected, 0);
            //}

            CvInvoke.Imshow("Rectified Left", frameLeftRectified);
            CvInvoke.Imshow("Rectified Right", frameRightRectified);



            CvInvoke.WaitKey(0);

        }
   
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        // This code runs when you press the Start button
        public void startButton_Click(object sender, EventArgs e)
        {
            if (capLeft == null) capLeft = new VideoCapture(Camera_Selection_Left.SelectedIndex);
            if (capRight == null) capRight = new VideoCapture(Camera_Selection_Right.SelectedIndex);
            if (capLeft.IsOpened && capRight.IsOpened)
            {
                string str1 = "Press ESCAPE key in any image window to close the program.";
                string str2 = "Press other key in any image window to move to next frame.";
                MessageBox.Show(str1 + "\n" + str2);
            }

            Calibration();
            // Obtaining and showing first frame of loaded video(used as the base for difference detection)
            backgroundFrame_l = capLeft.QueryFrame();
            backgroundFrame_r = capRight.QueryFrame();
            Mat backgroundLeftRemap = new Mat();
            Mat backgroundRightRemap = new Mat();
            //CvInvoke.Undistort(backgroundFrame_l, backgroundLeftCrop, camMat1, dist1);
            //CvInvoke.Undistort(backgroundFrame_r, backgroundRightCrop, camMat2, dist2);

            // Apply transformation to rectify images
            CvInvoke.Remap(backgroundFrame_l, backgroundLeftRemap, rmapx1, rmapy1, Inter.Linear);
            CvInvoke.Remap(backgroundFrame_r, backgroundRightRemap, rmapx2, rmapy2, Inter.Linear);
            Mat backgroundLeftCrop = new Mat(backgroundLeftRemap, Rec1);
            Mat backgroundRightCrop = new Mat(backgroundRightRemap, Rec2);            
            //Mat backgroundLeftCrop = new Mat(backgroundFrame_l, RecCrop);
            //Mat backgroundRightCrop = new Mat(backgroundFrame_r, RecCrop);
            CvInvoke.Imshow(BackgroundFrameWindowName_l, backgroundLeftCrop);
            CvInvoke.Imshow(BackgroundFrameWindowName_r, backgroundRightCrop);
            //CvInvoke.Imshow(BackgroundFrameWindowName_l, backgroundFrame_l);
            //CvInvoke.Imshow(BackgroundFrameWindowName_r, backgroundFrame_r);

            rmt.SendHandShake();
            rmt.Takeoff();
            Thread.Sleep(5000);
            TimerCallback tmCallback = rmt.TimerDelegate;
            System.Threading.Timer timer = new System.Threading.Timer(tmCallback, null, 1000, 100);

            //Handling video frames(image processing and contour detection)      
            VideoProcessingLoop(capLeft, backgroundLeftCrop, capRight, backgroundRightCrop, rmapx1, rmapy1, rmapx2, rmapy2, Rec1, Rec2 );
            //capLeft.ImageGrabbed += ProcessFrame;
            //capLeft.Start();
        }

        private void Calibration()
        {
            if (csharpCalib != true)
            {
                FileStorage fs = new FileStorage("stereo.yml", FileStorage.Mode.Read);
                fs["camMat1"].ReadMat(camMat1);
                fs["camMat2"].ReadMat(camMat2);
                fs["dist1"].ReadMat(dist1);
                fs["dist2"].ReadMat(dist2);
                //fs["R"].ReadMat(R);
                //fs["T"].ReadMat(T);
            }
            if (csharpCalib == true)
            {
                for (int i = 0; i < bufferLength * 2; i++)
                {
                    chessFrameL = CvInvoke.Imread(imagesPath + "camera1\\image_" + i.ToString() + ".jpg");
                    chessFrameR = CvInvoke.Imread(imagesPath + "camera2\\image_" + i.ToString() + ".jpg");

                    patternLeftFound = CvInvoke.FindChessboardCorners(chessFrameL, patternSize, cornersVecLeft, CalibCbType.NormalizeImage | CalibCbType.AdaptiveThresh);
                    patternRightFound = CvInvoke.FindChessboardCorners(chessFrameR, patternSize, cornersVecRight, CalibCbType.NormalizeImage | CalibCbType.AdaptiveThresh);

                    if (patternLeftFound && patternRightFound)
                    {
                        CvInvoke.CvtColor(chessFrameL, grayLeft, ColorConversion.Bgr2Gray);
                        CvInvoke.CvtColor(chessFrameR, grayRight, ColorConversion.Bgr2Gray);
                        //CvInvoke.CornerSubPix(grayLeft, cornersVecLeft, new Size(11, 11), new Size(-1, -1), new MCvTermCriteria(30, 0.1));
                        //CvInvoke.CornerSubPix(grayRight, cornersVecRight, new Size(11, 11), new Size(-1, -1), new MCvTermCriteria(30, 0.1));

                        //draw the results
                        CvInvoke.DrawChessboardCorners(chessFrameL, patternSize, cornersVecLeft, patternLeftFound);
                        CvInvoke.DrawChessboardCorners(chessFrameR, patternSize, cornersVecRight, patternRightFound);

                        CvInvoke.Imshow("Calibration image left", chessFrameL);
                        CvInvoke.Imshow("Calibration image right", chessFrameR);
                        CvInvoke.WaitKey(10);
                        imagePoints1[bufferSavepoint] = cornersVecLeft.ToArray();
                        imagePoints2[bufferSavepoint] = cornersVecRight.ToArray();
                        bufferSavepoint++;
                        if (bufferSavepoint == bufferLength)
                            break;
                    }

                }
                CvInvoke.DestroyAllWindows();
                //fill the MCvPoint3D32f with correct mesurments
                for (int k = 0; k < bufferLength; k++)
                {
                    //Fill our objects list with the real world mesurments for the intrinsic calculations
                    List<MCvPoint3D32f> object_list = new List<MCvPoint3D32f>();
                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            object_list.Add(new MCvPoint3D32f(j * 20.0F, i * 20.0F, 0.0F));
                        }
                    }
                    cornersObjectPoints[k] = object_list.ToArray();
                }
                
                CvInvoke.CalibrateCamera(cornersObjectPoints, imagePoints1, chessFrameL.Size, camMat1, dist1, CalibType.Default, new MCvTermCriteria(100, 1e-5), out rvecs, out tvecs);
                CvInvoke.CalibrateCamera(cornersObjectPoints, imagePoints2, chessFrameL.Size, camMat2, dist2, CalibType.Default, new MCvTermCriteria(100, 1e-5), out rvecs, out tvecs);
                CvInvoke.StereoCalibrate(cornersObjectPoints, imagePoints1, imagePoints2, camMat1, dist1, camMat2, dist2, chessFrameL.Size,
                                                                  R, T, essential, fundamental, CalibType.FixAspectRatio | CalibType.ZeroTangentDist| CalibType.SameFocalLength| CalibType.RationalModel| CalibType.UseIntrinsicGuess| CalibType.FixK3|CalibType.FixK4|CalibType.FixK5, new MCvTermCriteria(100, 1e-5));

                CvInvoke.StereoRectify(camMat1, dist1, camMat2, dist2, chessFrameL.Size, R, T, R1, R2, P1, P2, Q, StereoRectifyType.CalibZeroDisparity, 0,
                             chessFrameL.Size, ref Rec1, ref Rec2);

                newCamMat1 = CvInvoke.GetOptimalNewCameraMatrix(camMat1, dist1, chessFrameL.Size, 1, chessFrameL.Size, ref Rec3);
                newCamMat2 = CvInvoke.GetOptimalNewCameraMatrix(camMat2, dist2, chessFrameL.Size, 1, chessFrameL.Size, ref Rec4);

                CvInvoke.InitUndistortRectifyMap(camMat1, dist1, R1, P1, chessFrameL.Size, DepthType.Cv32F, rmapx1, rmapy1);
                CvInvoke.InitUndistortRectifyMap(camMat2, dist2, R2, P2, chessFrameL.Size, DepthType.Cv32F, rmapx2, rmapy2);

                ////This will Show us the usable area from each camera
                MessageBox.Show("Left: " + Rec1.ToString() + " \nRight: " + Rec2.ToString());
            }
            MessageBox.Show("Calibration has ended");
        }
        public void VideoProcessingLoop(VideoCapture capture_l, Mat backgroundFrame_l, VideoCapture capture_r, Mat backgroundFrame_r, Mat rmapx1, Mat rmapy1, Mat rmapx2, Mat rmapy2, Rectangle Rec1, Rectangle Rec2)
        {
            var stopwatch = new Stopwatch();// Used for measuring video processing performance
            var stopwatch_3d_calculate = new Stopwatch();

            int frameNumber = 1;
            while (true)// Loop video
            {
                rawFrame_l = capture_l.QueryFrame();// Getting next frame(null is returned if no further frame exists)
                rawFrame_r = capture_r.QueryFrame();// Getting next frame(null is returned if no further frame exists)

                CvInvoke.Remap(rawFrame_l, rawFrame_l, rmapx1, rmapy1, Inter.Linear);
                CvInvoke.Remap(rawFrame_r, rawFrame_r, rmapx2, rmapy2, Inter.Linear);
                Mat cropLeft = new Mat(rawFrame_l, Rec1);
                Mat cropRight = new Mat(rawFrame_r, Rec2);

                rawFrame_l = cropLeft;
                rawFrame_r = cropRight;

                if (rawFrame_l != null && rawFrame_r != null)
                {
                    frameNumber++;

                    stopwatch.Restart();
                    ProcessFrame(backgroundFrame_l, backgroundFrame_r, Threshold, ErodeIterations, DilateIterations);
                    stopwatch.Stop();

                    //3d calculate - start

                    stopwatch_3d_calculate.Restart();

                    x_pixel_left_camera = point_center_r.X;
                    x_pixel_right_camera = point_center_l.X;
                    x_1 = (x_pixel_left_camera - ox) * sx;
                    x_2 = (x_pixel_right_camera - ox) * sx;
                    y_1 = (point_center_r.Y - oy) * sy;
                    Z_3d = (T_3d * f) / (x_1 - x_2);
                    X_3d = x_1 * (Z_3d / f);
                    Y_3d = y_1 * (Z_3d / f);

                    stopwatch_3d_calculate.Stop();

                    //3d calculate - end
                    rmt.InstructionCalculate(X_3d, Y_3d, Z_3d);
                    WriteFrameInfo(stopwatch.ElapsedMilliseconds, stopwatch_3d_calculate.ElapsedMilliseconds, frameNumber);
                    ShowWindowsWithImageProcessingStages();

                    int key = CvInvoke.WaitKey(0);// Wait indefinitely until key is pressed

                    //Close program if Esc key was pressed(any other key moves to next frame)
                    if (key == 27)
                        Environment.Exit(0);
                }
                else
                {
                    capture_l.SetCaptureProperty(CapProp.PosFrames, 0);// Move to first frame
                    capture_r.SetCaptureProperty(CapProp.PosFrames, 0);// Move to first frame
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
            CvInvoke.Imshow(RawFrameWindowName_l, rawFrame_l);
            CvInvoke.Imshow(RawFrameWindowName_r, rawFrame_r);
            CvInvoke.Imshow(GrayscaleDiffFrameWindowName, grayscaleDiffFrame_l);
            CvInvoke.Imshow(BinaryDiffFrameWindowName, binaryDiffFrame_l);
            CvInvoke.Imshow(DenoisedDiffFrameWindowName, denoisedDiffFrame_l);
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
        private static void WriteFrameInfo(long elapsedMs, long _3d_elapsedMs, int frameNumber)
        {
            var info = new string[] {
                $"Frame Number: {frameNumber}",
                $"Processing Time (Find the drone in each image): {elapsedMs} [ms]",
                $"3d Processing Time : {_3d_elapsedMs} ms",
                $"Left Camera - Position: {point_center_l.X}, {point_center_l.Y}",
                $"Right Camera - Position: {point_center_r.X}, {point_center_r.Y}",
                $"X_3d : {X_3d} [meter]",
                $"Y_3d : {Y_3d} [meter]",
                $"Z_3d : {Z_3d} [meter]"
        };
            finalFrame_l.Save("C:\\Users\\Public\\Pictures_3011\\Left_" + frameNumber + ".jpg");
            finalFrame_r.Save("C:\\Users\\Public\\Pictures_3011\\Right_" + frameNumber + ".jpg");
            WriteMultilineText(finalFrame_l, info, new Point(5, 10));
            WriteMultilineText(finalFrame_r, info, new Point(5, 10));
            finalFrame_l.Save("C:\\Users\\Public\\Pictures_3011\\Left_withData" + frameNumber + ".jpg");
            finalFrame_r.Save("C:\\Users\\Public\\Pictures_3011\\Right_withData" + frameNumber + ".jpg");


        }
    }
}
