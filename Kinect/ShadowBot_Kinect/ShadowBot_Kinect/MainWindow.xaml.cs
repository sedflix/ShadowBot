/*
 * Code made by Shwetank Shrey
 * Project - Introduction to Engg Design
 * Shadow Bot
 * Description - This project aims to create Humanoid Robots who will replicate the actions of 
 * a person standing in front of a Kinect sensors.
 * The following code is the WPF application which records joint angles from a Kinect sensor and
 * sends it to the Arduino via Bluetooth.
 */

// Loading basic C# references used
using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
// Loading Kinect references from the Microsoft Kinect SDK
using Microsoft.Kinect;
// Loading default C# serial communication references
using System.IO.Ports;

// Start of Code
namespace ShadowBot_Kinect
{
    // Main Window
    public partial class MainWindow : Window
    {
        // Initialising of Global Variables

        // Kinect Sensor
        private KinectSensor sensor;
        // Serial Port for Bluetooth Communication
        private SerialPort port;
        //For Skeleton Stream
        // Width of output drawing
        private const float RenderWidth = 640.0f;
        // Height of our output drawing
        private const float RenderHeight = 480.0f;
        // Thickness of drawn joint lines
        private const double JointThickness = 3;
        // Thickness of body center ellipse
        private const double BodyCenterThickness = 10;
        // Thickness of clip edge rectangles
        private const double ClipBoundsThickness = 10;
        // Brush used to draw skeleton center point
        private readonly Brush centerPointBrush = Brushes.Blue;
        // Brush used for drawing joints that are currently tracked
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        // Brush used for drawing joints that are currently inferred
        private readonly Brush inferredJointBrush = Brushes.Yellow;
        // Pen used for drawing bones that are currently tracked
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);
        // Pen used for drawing bones that are currently inferred
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);
        // Drawing group for skeleton rendering output
        private DrawingGroup drawingGroup;
        // Drawing image that we will display
        private DrawingImage imageSource;
        // Clock
        DateTime lastTime;
        // Check if first time
        Boolean first;

        public MainWindow()
        {
            InitializeComponent();
        }
        // Actions performed on start of application
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Create the drawing group we'll use for drawing
            drawingGroup = new DrawingGroup();
            // Create an image source that we can use in our image control
            imageSource = new DrawingImage(drawingGroup);
            // Display the drawing using our image control
            Image.Source = imageSource;
            //Initialising Serial Port
            port = new SerialPort();
            port.BaudRate = 9600;
            port.PortName = "COM3";
            if(port.IsOpen)
                port.Close();
            port.Open();
            // Initialising of Kinect
            // For all available Kinect sensors, choose a sensor as the one used
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    sensor = potentialSensor;
                    break;
                }
            }
            // Further code will be only executed if Kinect is available
            if (null != sensor)
            {
                // Enables Skeleton Stream from Kinect sensor
                sensor.SkeletonStream.Enable();
                first = true;
                sensor.SkeletonFrameReady += Sensor_SkeletonFrameReady;
                try
                {
                    sensor.Start();
                }
                catch (IOException)
                {
                    sensor = null;
                }
            }
        }
        // Actions performed on close of application
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Serial port closed
            port.Close();
            // Sensors to be closed if open
            if (null != sensor)
            {
                sensor.Stop();
            }
        }
        // Skeleton Stream Actions
        private void Sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            // Initialise skeleton array to store available skeletons (maximum 6)
            Skeleton[] skeletons = new Skeleton[0];
            // Store skeletons from available skeleton frame to the array
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                {
                    return;
                }
                skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                skeletonFrame.CopySkeletonDataTo(skeletons);
            }
            // Choose the first/nearest skeleton as the main skeleton
            Skeleton skel = (from trackskeleton in skeletons
                             where trackskeleton.TrackingState == SkeletonTrackingState.Tracked
                             select trackskeleton).FirstOrDefault();
            // Code to go ahead if any skeletons are available
            if (skel == null)
            {
                return;
            }
            //Draw Skeleton
            using (DrawingContext dc = drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                RenderClippedEdges(skel, dc);
                if (skel.TrackingState == SkeletonTrackingState.Tracked)
                {
                    DrawBonesAndJoints(skel, dc);
                }
                else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                {
                    dc.DrawEllipse(centerPointBrush, null, SkeletonPointToScreen(skel.Position), BodyCenterThickness, BodyCenterThickness);
                }
                // prevent drawing outside of our render area
                drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
            // If skeletons are being tracked in real time in synchronisation, process angles
            if (skel.Joints[JointType.ShoulderRight].TrackingState == JointTrackingState.Tracked &&
                skel.Joints[JointType.ElbowRight].TrackingState == JointTrackingState.Tracked &&
                skel.Joints[JointType.WristRight].TrackingState == JointTrackingState.Tracked &&
                skel.Joints[JointType.ShoulderLeft].TrackingState == JointTrackingState.Tracked &&
                skel.Joints[JointType.ElbowLeft].TrackingState == JointTrackingState.Tracked &&
                skel.Joints[JointType.WristLeft].TrackingState == JointTrackingState.Tracked)
            {
                // Print angles on the WPF application's labels
                int[] ang = PrintAngles(skel);
                ls1.Content = ang[0];
                rs1.Content = ang[1];
                ls2.Content = ang[2];
                rs2.Content = ang[3];
                le.Content = ang[4];
                re.Content = ang[5];
                lh.Content = ang[6];
                rh.Content = ang[7];
                lk.Content = ang[8];
                rk.Content = ang[9];
                // Print angles on the serial port
                string toSend = "";
                for (int i = 0; i < 6; i++)
                    toSend += (i+"."+ang[i]+":");
                Console.WriteLine(toSend);
                if(first)
                {
                    lastTime = DateTime.UtcNow;
                    port.WriteLine(toSend);
                    return;
                }
                TimeSpan difference = lastTime.Subtract(DateTime.UtcNow);
                if (difference.Seconds > 4)
                {
                    port.WriteLine(toSend);
                    lastTime = DateTime.UtcNow;
                }
            }
        }
        // Draws indicators to show which edges are clipping skeleton data
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(Brushes.Red, null, new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(Brushes.Red, null, new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(Brushes.Red, null, new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(Brushes.Red, null, new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }
        
        // Draws a skeleton's bones and joints
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;
                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        // Maps a SkeletonPoint to lie within our render space and converts to Point
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }
        
        // Draws a bone line between two joints
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }
        // Method to print certain angles in a skeleton
        private int[] PrintAngles(Skeleton skeleton)
        {
            // Unit vectors
            Vector3D XVector = new Vector3D(1.0, 0.0, 0.0);
            Vector3D YVector = new Vector3D(0.0, -1.0, 0.0);
            Vector3D ZVector = new Vector3D(0.0, 0.0, 1.0);
            // Major required vectors within the skeleton
            Vector3D LeftShoulder = new Vector3D(skeleton.Joints[JointType.ShoulderLeft].Position.X, skeleton.Joints[JointType.ShoulderLeft].Position.Y, skeleton.Joints[JointType.ShoulderLeft].Position.Z);
            Vector3D LeftShoulderXY = new Vector3D(skeleton.Joints[JointType.ShoulderLeft].Position.X, skeleton.Joints[JointType.ShoulderLeft].Position.Y, 0);
            Vector3D LeftShoulderYZ = new Vector3D(0, skeleton.Joints[JointType.ShoulderLeft].Position.Y, skeleton.Joints[JointType.ShoulderLeft].Position.Z);
            Vector3D RightShoulder = new Vector3D(skeleton.Joints[JointType.ShoulderRight].Position.X, skeleton.Joints[JointType.ShoulderRight].Position.Y, skeleton.Joints[JointType.ShoulderRight].Position.Z);
            Vector3D RightShoulderXY = new Vector3D(skeleton.Joints[JointType.ShoulderRight].Position.X, skeleton.Joints[JointType.ShoulderRight].Position.Y, 0);
            Vector3D RightShoulderYZ = new Vector3D(0, skeleton.Joints[JointType.ShoulderRight].Position.Y, skeleton.Joints[JointType.ShoulderRight].Position.Z);
            Vector3D LeftElbow = new Vector3D(skeleton.Joints[JointType.ElbowLeft].Position.X, skeleton.Joints[JointType.ElbowLeft].Position.Y, skeleton.Joints[JointType.ElbowLeft].Position.Z);
            Vector3D LeftElbowXY = new Vector3D(skeleton.Joints[JointType.ElbowLeft].Position.X, skeleton.Joints[JointType.ElbowLeft].Position.Y, 0);
            Vector3D LeftElbowYZ = new Vector3D(0, skeleton.Joints[JointType.ElbowLeft].Position.Y, skeleton.Joints[JointType.ElbowLeft].Position.Z);
            Vector3D RightElbow = new Vector3D(skeleton.Joints[JointType.ElbowRight].Position.X, skeleton.Joints[JointType.ElbowRight].Position.Y, skeleton.Joints[JointType.ElbowRight].Position.Z);
            Vector3D RightElbowXY = new Vector3D(skeleton.Joints[JointType.ElbowRight].Position.X, skeleton.Joints[JointType.ElbowRight].Position.Y, 0);
            Vector3D RightElbowYZ = new Vector3D(0, skeleton.Joints[JointType.ElbowRight].Position.Y, skeleton.Joints[JointType.ElbowRight].Position.Z);
            Vector3D LeftWrist = new Vector3D(skeleton.Joints[JointType.WristLeft].Position.X, skeleton.Joints[JointType.WristLeft].Position.Y, skeleton.Joints[JointType.WristLeft].Position.Z);
            Vector3D RightWrist = new Vector3D(skeleton.Joints[JointType.WristRight].Position.X, skeleton.Joints[JointType.WristRight].Position.Y, skeleton.Joints[JointType.WristRight].Position.Z);
            Vector3D LeftHip = new Vector3D(0, skeleton.Joints[JointType.HipLeft].Position.Y, skeleton.Joints[JointType.HipLeft].Position.Z);
            Vector3D RightHip = new Vector3D(0, skeleton.Joints[JointType.HipRight].Position.Y, skeleton.Joints[JointType.HipLeft].Position.Z);
            Vector3D LeftKnee = new Vector3D(0, skeleton.Joints[JointType.KneeLeft].Position.Y, skeleton.Joints[JointType.KneeLeft].Position.Z);
            Vector3D RightKnee = new Vector3D(0, skeleton.Joints[JointType.KneeRight].Position.Y, skeleton.Joints[JointType.KneeRight].Position.Z);
            Vector3D LeftAnkle = new Vector3D(0, skeleton.Joints[JointType.AnkleLeft].Position.Y, skeleton.Joints[JointType.AnkleLeft].Position.Z);
            Vector3D RightAnkle = new Vector3D(0, skeleton.Joints[JointType.AnkleRight].Position.Y, skeleton.Joints[JointType.AnkleRight].Position.Z);
            // Major joint angles
            double AngleLeftShoulder1 = AngleBetweenTwoVectors(LeftShoulderYZ - LeftElbowYZ, YVector);
            double AngleLeftShoulder2 = AngleBetweenTwoVectors(LeftShoulderYZ - LeftElbowYZ, LeftShoulder - LeftElbow);
            double AngleRightShoulder1 = AngleBetweenTwoVectors(RightShoulderYZ - RightElbowYZ, YVector);
            double AngleRightShoulder2 = AngleBetweenTwoVectors(RightShoulderYZ - RightElbowYZ, RightShoulder - RightElbow);
            double AngleLeftElbow = AngleBetweenTwoVectors(LeftShoulder - LeftElbow, LeftElbow - LeftWrist);
            double AngleRightElbow = AngleBetweenTwoVectors(RightShoulder - RightElbow, RightElbow - RightWrist);
            double AngleLeftHip = AngleBetweenTwoVectors(LeftKnee - LeftHip, YVector);
            double AngleRightHip = AngleBetweenTwoVectors(RightKnee - RightHip, YVector);
            double AngleLeftKnee = AngleBetweenTwoVectors(LeftHip - LeftKnee, LeftKnee - LeftAnkle);
            double AngleRightKnee = AngleBetweenTwoVectors(RightHip - RightKnee, RightKnee - RightAnkle);
            // Joint angle array to be returned to the calling statement
            int[] angle = new int[10];
            angle[0] = Round((int)AngleLeftShoulder1);
            angle[1] = Round((int)AngleRightShoulder1);
            angle[2] = Round((int)AngleLeftShoulder2);
            angle[3] = Round((int)AngleRightShoulder2);
            angle[4] = Round((int)AngleLeftElbow);
            angle[5] = Round((int)AngleRightElbow);
            angle[6] = Round((int)AngleLeftHip);
            angle[7] = Round((int)AngleRightHip);
            angle[8] = Round((int)AngleLeftKnee);
            angle[9] = Round((int)AngleRightKnee);
            return angle;
        }
        //Method to round off to nearest ten.
        public static int Round(int value)
        {
            return 10 * (value / 10);
        }
        // Method to calculate angle between two 3D vectors
        private double AngleBetweenTwoVectors(Vector3D vectorA, Vector3D vectorB)
        {
            // Calculates angles by normalising vectors to unit vectors and computing the cosine using dot products
            double angle;
            vectorA.Normalize();
            vectorB.Normalize();
            double dp = Vector3D.DotProduct(vectorA, vectorB);
            angle = (Math.Acos(dp) / Math.PI) * 180;
            return angle;
        }
    }
}