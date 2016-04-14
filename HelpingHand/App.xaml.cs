using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using WindowsInput;
using System.IO;
using Microsoft.Kinect;
using System.Reflection;

namespace KinectControl
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.Startup += new StartupEventHandler(App_Startup);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!IsKinectRuntimeInstalled)
            {
                MessageBox.Show("The Microsoft Kinect Runtime is not installed.");
                throw new Exception();
            }
            base.OnStartup(e);
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            GestureRecognizer.Instance.Recognized.Subscribe(g =>
            {
                switch (g)
                {
                    case GestureRecognizer.Gesture.Left:
                        InputSimulator.SimulateKeyPress(VirtualKeyCode.LEFT);
                        break;
                    case GestureRecognizer.Gesture.Right:
                        InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
                        break;
                    default:
                        break;
                }
            });
        }

        public bool IsKinectRuntimeInstalled
        {
            get
            {
                bool isInstalled;
                try
                {
                    TestForKinectTypeLoadException();
                    isInstalled = true;
                }
                catch (Exception)
                {
                    isInstalled = false;
                }
                return isInstalled;
            }
        }

        // This Microsoft.Kinect.dll based type, must be isolated in its own method
        // as the CLR will attempt to load the Microsoft.Kinect.dll assembly it when this method is executed.
        private void TestForKinectTypeLoadException()
        {
#pragma warning disable 219 //ignore the fact that status is unused code after this set.
            var status = KinectStatus.Disconnected;
            var sensors = KinectSensor.KinectSensors;
#pragma warning restore 219
        }
    }
}
