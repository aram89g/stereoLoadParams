// EMGU
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
// System
using System;
using System.Drawing;
using System.IO;



namespace stereoLoadParams
{
    public partial class VisionApp
    {
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
        private static void WriteFrameInfo(long elapsedMs, long _3d_elapsedMs, int frameNumber, int targetCnt)
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
    }
}
