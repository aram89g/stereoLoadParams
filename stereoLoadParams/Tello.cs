using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.CvEnum;
/*---------------------------------------------------------------------------------------------------
 * This class represents the Tello drone control system, It send's commands using UDP.
 ---------------------------------------------------------------------------------------------------*/
public class Tello
{
    string tello_host = "192.168.10.1";
    string local_host = "0.0.0.0";
    int tello_port = 8889;
    int local_state_port = 8890;
    int tello_video_port = 11111;

    bool recvFlag = false;
    IPEndPoint tello_endpoint;
    UdpClient tello_socket;

    // Video Recieve thread
    Mat frame;
    bool is_new_frame_to_process;
    string video_stream_url;
    VideoCapture cap;
    Thread video_receive_thread;

    //controls
    public int left_right = 0;
    public int down_up = 0;
    public int back_forward = 0;
    public int yaw = 0;

    // Battery thread
    byte[] response;
    Thread keep_alive_thread; // Create thread to keep session alive with drone

    // State Recieve thread
    IPEndPoint state_endpoint;
    UdpClient state_socket;
    Thread state_receive_thread;
    static void OnUdpData(IAsyncResult result)
    {
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);
        // get the actual message and fill out the source:
        byte[] response = socket.EndReceive(result, ref source);
        // do what you'd like with `message` here:
        Console.WriteLine(Encoding.UTF8.GetString(response));
        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnUdpData), socket);
    }

    public void send_command(string command)
    {
        byte[] cmd = Encoding.UTF8.GetBytes(command);
        tello_socket.Send(cmd, cmd.Length);
    }
    public void send_rc_control()
    {
        send_command($"rc {left_right} {back_forward} {down_up} {yaw}");
    }

    public Tello()
    {
        tello_endpoint = new IPEndPoint(IPAddress.Parse(tello_host), tello_port);
        state_endpoint = new IPEndPoint(IPAddress.Parse(local_host), local_state_port);
        tello_socket = new UdpClient(tello_port);
        tello_socket.Connect(tello_endpoint);
        tello_socket.BeginReceive(new AsyncCallback(OnUdpData), tello_socket);
        send_command("command");
        keep_alive_thread = new Thread(new ThreadStart(keepAliveProc));

        cap = new VideoCapture($"udp://" + tello_host + ":{tello_port}");
        video_receive_thread = new Thread(new ThreadStart(videoReceiveProc));
        video_receive_thread.Start();

        Socket state_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        state_socket.Bind(state_endpoint);
        state_receive_thread = new Thread(new ThreadStart(stateReceiveProc));
        state_receive_thread.Start();
    }

    public void keepAliveProc()
    {
        send_command("battery?");
        Thread.Sleep(9000);
    }

    public void videoReceiveProc()
    {
        while(true)
        {
            cap.Read(frame);
            is_new_frame_to_process = true;
        }
    }
    public bool Is_new_frame_ready()
    {
        return is_new_frame_to_process;
    }

    public Mat get_last_frame()
    {
        is_new_frame_to_process = false;
        return frame;
    }
    public void stateReceiveProc()
    {

    }

    public void InstructionCalculate(StereoPoint3D drone, ref Point3D target)
    {
        double delta = 0.1;
        int stepX = 20;
        int stepY = 20;
        int stepZ = 20;

        double X = drone.GetX3D();
        double Y = drone.GetY3D();
        double Z = drone.GetZ3D();

        bool xValid = false;
        bool yValid = false;
        bool zValid = false;

        Console.WriteLine($"Drone Coordinates:  [X: {drone.GetX3D()}, Y: {drone.GetY3D()}, Z: {drone.GetZ3D()}]\n");
        Console.WriteLine($"Target Coordinates:  [X: {target.GetX()}, Y: {target.GetY()}, Z: {target.GetZ()}]\n");

        // Horizontal axis
        if ((X < (target.GetX() + delta)) && (X > (target.GetX() - delta)))
        {
            left_right = 0;
            xValid = true;
        }
        else if (X < target.GetX())
        {
            left_right = stepX;
            Console.WriteLine("goes right");
        }
        else
        {
            left_right = -stepX;
            Console.WriteLine("goes left");
        }
        // Vertical axis
        if ((Y < (target.GetY() + delta)) && (Y > (target.GetY() - delta)))
        {
            down_up = 0;
            yValid = true;
        }
        else if (Y < target.GetY())
        {
            down_up = -stepY;
            Console.WriteLine("goes down");
        }
        else
        {
            down_up = stepY;
            Console.WriteLine("goes up");
        }
        //Depth axis
        if ((Z < (target.GetZ() + delta)) && (Z > (target.GetZ() - delta)))
        {
            back_forward = 0;
            zValid = true;
        }
        else if (Z < target.GetZ())
        {
            back_forward = stepZ;
            Console.WriteLine("goes forward");
        }
        else
        {
            back_forward = -stepZ;
            Console.WriteLine("goes backwards");
        }
        if (xValid && yValid && zValid)
        {
            target.arrived = true;
        }

        // Send the RC command to the drone
        send_rc_control();
    }

    public void Takeoff()
    {
        send_command("takeoff");
    }

    public void Land()
    {
        send_command("land");
    }

    public void Set_video_stream_on()
    {
        send_command("streamon");
    }

    public void Set_video_stream_off()
    {
        send_command("streamoff");
    }


}