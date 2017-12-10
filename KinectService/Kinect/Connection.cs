using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectService.Kinect
{
    class Connection : IDisposable
    {
        public KinectSensor KSensor { get; set; }

        public Connection(EventHandler<IsAvailableChangedEventArgs> onAvailableChangedCallback)
        {
            this.KSensor = KinectSensor.GetDefault();
            this.KSensor.IsAvailableChanged += onAvailableChangedCallback;
            if (this.KSensor != null)
            {
                if (!this.KSensor.IsOpen)
                {
                    this.KSensor.Open();
                }

            }
        }

        public void Dispose()
        {
            if (KSensor != null)
            {
                /// zamknięcie kontrolera
                KSensor.Close();
                KSensor = null;
            }
        }
    }
}
