// EMGU
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
// System
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace stereoLoadParams
{
    public partial class VisionApp
    {
        #region Calibration variables
        //Calibration variables
        static int bufferLength = 10; // define how many good images needed
        int bufferSavepoint = 0;
        bool patternLeftFound; // True if chessboard found in image
        bool patternRightFound; // True if chessboard found in image
        #endregion
        public void Calibration()
        {
            string imagesPath = calibrationPath.Text;
            if (imageCalibration != true)
            {
                try
                {
                    FileStorage fs = new FileStorage(calibrationPath.Text, FileStorage.Mode.Read);
                    fs["rmapx1"].ReadMat(rmapx1);
                    fs["rmapy1"].ReadMat(rmapy1);
                    fs["rmapx2"].ReadMat(rmapx2);
                    fs["rmapy2"].ReadMat(rmapy2);

                    string rec1Str = fs["Rec1"].ReadString();
                    string rec2Str = fs["Rec2"].ReadString();

                    int idx1 = rec1Str.IndexOf('=');
                    int idx2 = rec1Str.IndexOf(',');
                    int x = Convert.ToInt32(rec1Str.Substring(idx1 + 1, idx2 - idx1 - 1));
                    idx1 = rec1Str.IndexOf('=',idx2);
                    idx2 = rec1Str.IndexOf(',',idx1);
                    int y = Convert.ToInt32(rec1Str.Substring(idx1 + 1, idx2 - idx1 - 1));
                    idx1 = rec1Str.IndexOf('=', idx2);
                    idx2 = rec1Str.IndexOf(',', idx1);
                    int w = Convert.ToInt32(rec1Str.Substring(idx1 + 1, idx2 - idx1 - 1));
                    idx1 = rec1Str.IndexOf('=', idx2);
                    idx2 = rec1Str.Length;
                    int h = Convert.ToInt32(rec1Str.Substring(idx1 + 1, idx2 - idx1 - 1));

                    Rec1 = new Rectangle(x,y,w,h);

                    idx1 = rec2Str.IndexOf('=');
                    idx2 = rec2Str.IndexOf(',');
                    x = Convert.ToInt32(rec2Str.Substring(idx1 + 1, idx2 - idx1 - 1));
                    idx1 = rec2Str.IndexOf('=', idx2);
                    idx2 = rec2Str.IndexOf(',', idx1);
                    y = Convert.ToInt32(rec2Str.Substring(idx1 + 1, idx2 - idx1 - 1));
                    idx1 = rec2Str.IndexOf('=', idx2);
                    idx2 = rec2Str.IndexOf(',', idx1);
                    w = Convert.ToInt32(rec2Str.Substring(idx1 + 1, idx2 - idx1 - 1));
                    idx1 = rec2Str.IndexOf('=', idx2);
                    idx2 = rec2Str.Length;
                    h = Convert.ToInt32(rec2Str.Substring(idx1 + 1, idx2 - idx1 - 1));

                    Rec2 = new Rectangle(x,y,w,h);

                    MessageBox.Show("Transformation maps loaded successfully");
                }
                catch (Exception)
                {
                    MessageBox.Show("Error: Problem loading Transformation maps");
                    Environment.Exit(1);
                }
            }
            if (imageCalibration == true)
            {
                for (int i = 0; i < bufferLength * 2; i++)
                {
                    chessFrameL = CvInvoke.Imread(imagesPath + "\\camera1\\image_" + i.ToString() + ".jpg");
                    chessFrameR = CvInvoke.Imread(imagesPath + "\\camera2\\image_" + i.ToString() + ".jpg");

                    patternLeftFound = CvInvoke.FindChessboardCorners(chessFrameL, patternSize, cornersVecLeft, CalibCbType.NormalizeImage | CalibCbType.AdaptiveThresh);
                    patternRightFound = CvInvoke.FindChessboardCorners(chessFrameR, patternSize, cornersVecRight, CalibCbType.NormalizeImage | CalibCbType.AdaptiveThresh);

                    if (patternLeftFound && patternRightFound)
                    {
                        CvInvoke.CvtColor(chessFrameL, grayLeft, ColorConversion.Bgr2Gray);
                        CvInvoke.CvtColor(chessFrameR, grayRight, ColorConversion.Bgr2Gray);
                        //CvInvoke.CornerSubPix(grayLeft, cornersVecLeft, new Size(11, 11), new Size(-1, -1), new MCvTermCriteria(30, 0.1));
                        //CvInvoke.CornerSubPix(grayRight, cornersVecRight, new Size(11, 11), new Size(-1, -1), new MCvTermCriteria(30, 0.1));

                        CvInvoke.DrawChessboardCorners(chessFrameL, patternSize, cornersVecLeft, patternLeftFound);
                        CvInvoke.DrawChessboardCorners(chessFrameR, patternSize, cornersVecRight, patternRightFound);

                        CvInvoke.Imshow("Calibration image left", chessFrameL);
                        CvInvoke.Imshow("Calibration image right", chessFrameR);
                        //chessFrameL.Save(desktop + "\\Left_" + i + ".jpg");
                        //chessFrameR.Save(desktop + "\\Right" + i + ".jpg");

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
                                                                  R, T, essential, fundamental, CalibType.FixAspectRatio | CalibType.ZeroTangentDist | CalibType.SameFocalLength | CalibType.RationalModel | CalibType.UseIntrinsicGuess | CalibType.FixK3 | CalibType.FixK4 | CalibType.FixK5, new MCvTermCriteria(100, 1e-5));

                CvInvoke.StereoRectify(camMat1, dist1, camMat2, dist2, chessFrameL.Size, R, T, R1, R2, P1, P2, Q, StereoRectifyType.CalibZeroDisparity, 0,
                             chessFrameL.Size, ref Rec1, ref Rec2);

                // Create transformation maps 
                CvInvoke.InitUndistortRectifyMap(camMat1, dist1, R1, P1, chessFrameL.Size, DepthType.Cv32F, rmapx1, rmapy1);
                CvInvoke.InitUndistortRectifyMap(camMat2, dist2, R2, P2, chessFrameL.Size, DepthType.Cv32F, rmapx2, rmapy2);
                MessageBox.Show("Calibration has ended\n Connect to TELLO access point");
                try
                {
                    FileStorage fs = new FileStorage(calibrationPath.Text + "\\calibMaps.xml", FileStorage.Mode.Write);
                    fs.Write(rmapx1, "rmapx1");
                    fs.Write(rmapy1, "rmapy1");
                    fs.Write(rmapx2, "rmapx2");
                    fs.Write(rmapy2, "rmapy2");
                    fs.Write(Rec1.ToString(), "Rec1");
                    fs.Write(Rec2.ToString(), "Rec2");
                    MessageBox.Show("Transformation maps saved successfully");                   
                }
                catch (Exception)
                {
                    MessageBox.Show("Error: Problem saving Transformation maps");
                    Environment.Exit(1);
                }
            }
        }
    }
    
}