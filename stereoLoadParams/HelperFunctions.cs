// EMGU
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
// System
using System;
using System.Drawing;
using System.IO;

namespace stereoLoadParams
{
    public partial class VisionApp
    {
        private static void ProcessFrame(int threshold)
        {
            CvInvoke.CvtColor(rawFrame_l, grayscaleDiffFrame_l, ColorConversion.Bgr2Gray);
            CvInvoke.CvtColor(rawFrame_r, grayscaleDiffFrame_r, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(grayscaleDiffFrame_l, binaryDiffFrame_l, threshold, 255, ThresholdType.Binary);
            CvInvoke.Threshold(grayscaleDiffFrame_r, binaryDiffFrame_r, threshold, 255, ThresholdType.Binary);

            rawFrame_l.CopyTo(finalFrame_l);
            rawFrame_r.CopyTo(finalFrame_r);

            left_camera = true;
            DetectObject(binaryDiffFrame_l, finalFrame_l);

            left_camera = false;
            DetectObject(binaryDiffFrame_r, finalFrame_r);
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
        private static void WriteFrameInfo(long elapsedMs, int frameNumber, int targetCnt)
        {
            var info = new string[] {
                $"Frame Number: {frameNumber}",
                $"Processing Time (Find the drone in each image): {elapsedMs} [ms]",
                $"Left Camera - Position: {point_center_l.X}, {point_center_l.Y}",
                $"Right Camera - Position: {point_center_r.X}, {point_center_r.Y}",
                $"X_3d : {X_3d} [m]",
                $"Y_3d : {Y_3d} [m]",
                $"Z_3d : {Z_3d} [m]",
                $"Destination: target {targetCnt}"
        };

            // Save frames to PC
            bool saveFramesEnable = false;
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            Directory.CreateDirectory(desktop + "\\savedFrames");
            finalFrame_l.Save(desktop + "\\savedFrames\\Left_" + frameNumber + ".jpg");
            finalFrame_r.Save(desktop + "\\savedFrames\\Right_" + frameNumber + ".jpg");
            WriteMultilineText(finalFrame_l, info, new Point(5, 10));
            WriteMultilineText(finalFrame_r, info, new Point(5, 10));
            if (saveFramesEnable == true)
            {
                finalFrame_l.Save(desktop + "\\savedFrames\\" + frameNumber + "Left_withData.jpg");
                finalFrame_r.Save(desktop + "\\savedFrames\\" + frameNumber + "Right_withData.jpg");
            }
        }
        private static void WriteMultilineText(Mat frame, string[] lines, Point origin)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                int y = i * 10 + origin.Y;// Moving down on each line
                CvInvoke.PutText(frame, lines[i], new Point(origin.X, y), FontFace.HersheyPlain, 0.8, drawingColor);
            }
        }
    }
}
