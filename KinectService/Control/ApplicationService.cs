using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace KinectService.Control
{
    class ApplicationService : IDisposable
    {
        private const int OUTPUT_SOCK_PORT = 8726;

        public static ApplicationService _Instance = null;

        private TcpListener OutputListener;
        private StreamWriter OutputStreamWriter;
        private ObservableCollection<Request> RequestQueue;
        private Timer PingTimer = null;

        // Events
        public delegate void ConnectedEventHandler();
        public event ConnectedEventHandler Connected;

        public delegate void DisconnectedEventHandler();
        public event DisconnectedEventHandler Disconnected;

        public static ApplicationService Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new ApplicationService();
                }

                return _Instance;
            }
        }

        private ApplicationService()
        {
            this.RequestQueue = new ObservableCollection<Request>();
            this.RequestQueue.CollectionChanged += OnRequestQueueChange;
        }

        public void StartListening()
        {
            this.InitializeOutputSocket();
            this.WaitForConnection();
        }

        public void Dispose()
        {
            if (this.OutputListener != null)
            {
                this.OutputListener.Stop();
                this.OutputListener = null;
            }
           
            if (this.OutputStreamWriter != null)
            {
                this.OutputStreamWriter.Dispose();
                this.OutputStreamWriter = null;
            }
        }

        private void InitializeOutputSocket()
        {
            IPAddress ipAddress = IPAddress.Parse("192.168.0.20");
            this.OutputListener = new TcpListener(ipAddress, OUTPUT_SOCK_PORT);
            this.OutputListener.Start();

            Utils.LogLine("Nasłuchiwanie na porcie " + OUTPUT_SOCK_PORT);
        }

        private async void WaitForConnection()
        {
            try
            {
                Utils.LogLine("Oczekiwanie na połączenie...");

                TcpClient tcpClient = await OutputListener.AcceptTcpClientAsync();
                Utils.LogLine("Połączono!");

                NetworkStream networkStream = tcpClient.GetStream();

                this.OutputStreamWriter = new StreamWriter(networkStream);

                Connected?.Invoke();

                ProcessRequests();
                StartPinging();
            } catch (Exception)
            {
                Utils.LogLine("Przerwano oczekiwanie na połączenie");
            }
        }

        private void StartPinging()
        {
            this.PingTimer = new Timer();
            this.PingTimer.Elapsed += new ElapsedEventHandler(Ping);
            this.PingTimer.Interval = 10000;
            this.PingTimer.Enabled = true;
        }

        private void Ping(object source, ElapsedEventArgs e)
        {
            if (OutputStreamWriter != null)
            {
                Request request = new Request();
                request.command = "Ping";
                request.parameters = new Hashtable();
                SendRequest(request);
            }
        }

        public void SendRequest(Request request)
        {
            RequestQueue.Add(request);
        }

        private void OnRequestQueueChange(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                ProcessRequests();
            }
        }

        private void ProcessRequests()
        {
            try
            {
                foreach (Request request in RequestQueue.ToArray())
                {
                    string serializedRequest = JsonConvert.SerializeObject(request);

                    this.OutputStreamWriter.WriteLine(Utils.Base64Encode(serializedRequest));
                    this.OutputStreamWriter.Flush();

                    RequestQueue.Remove(request);
                }
            } catch (Exception)
            {
                Utils.LogLine("Błąd podczas wysyłania. Odświeżanie połączenia...");
                RequestQueue.Clear();

                Disconnected?.Invoke();

                OutputStreamWriter = null;
                WaitForConnection();
            }
        }

        public bool IsConnected()
        {
            return OutputStreamWriter != null;
        }

        public void NotifyServiceUp()
        {
            Request request = new Request();
            request.command = "ServiceIsUp";
            request.parameters = new Hashtable();
            request.parameters.Add("serviceName", "IsKinectEnabled");
            SendRequest(request);
        }

        public void NotifyServiceDown()
        {
            Request request = new Request();
            request.command = "ServiceIsDown";
            request.parameters = new Hashtable();
            request.parameters.Add("serviceName", "IsKinectEnabled");
            SendRequest(request);
        }

        public void PullRight(string direction)
        {
            Request request = new Request();
            request.command = "PullRight";
            request.parameters = new Hashtable();
            request.parameters.Add("direction", direction);
            SendRequest(request);
        }

        public void PullLeft(string direction)
        {
            Request request = new Request();
            request.command = "PullLeft";
            request.parameters = new Hashtable();
            request.parameters.Add("direction", direction);
            SendRequest(request);
        }

        public void PullCancel()
        {
            Request request = new Request();
            request.command = "PullCancel";
            request.parameters = new Hashtable();
            SendRequest(request);
        }

        public void Cross()
        {
            Request request = new Request();
            request.command = "Cross";
            request.parameters = new Hashtable();
            SendRequest(request);
        }

        public void ChangeBrightness(float changeBy)
        {
            Request request = new Request();
            request.command = "ChangeBrightness";
            request.parameters = new Hashtable();
            request.parameters.Add("changeBy", changeBy);
            SendRequest(request);
        }

    }
}
