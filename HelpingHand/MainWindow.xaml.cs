using System;
using System.Reactive;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reactive.Linq;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Windows.Markup;
using System.Net;

namespace KinectControl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            SensorManager.Instance.PrimarySensor.Subscribe(s =>
                {
                    colorViewer.Kinect = s;
                });

            GestureRecognizer.Instance.Recognized.Subscribe(g =>
                {
                    switch (g)
                    {
                        case GestureRecognizer.Gesture.Left:
                            ((Storyboard)Resources["HandsManLeftStoryboard"]).Begin();
                            break;
                        case GestureRecognizer.Gesture.Right:
                            ((Storyboard)Resources["HandsManRightStoryboard"]).Begin();
                            break;
                        default:
                            break;
                    }
                });

            //IDisposable skeletonSubscription = null;

            //SensorManager.Instance.TrackedSkeletons.NewSkeleton.Subscribe(s =>
            //    {
            //        if (null != skeletonSubscription) skeletonSubscription.Dispose();

            //        skeletonSubscription = s.Subscribe(skeleton => { skeletonViewer1.UpdateSkeleton(skeleton); });
            //    });
            this.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(RequestNavigateHandler));

            var webClient = new WebClient();
            webClient.DownloadStringObservable(new Uri("http://demo.monkeydevelopment.com/HelpingHand/news.xaml"))
                .Catch(Observable.Empty<string>())
                .Subscribe(s =>
                {
                    InfoWrapper.Content = XamlReader.Parse(s);
                });
		}

        void RequestNavigateHandler(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
        }
    }
}
