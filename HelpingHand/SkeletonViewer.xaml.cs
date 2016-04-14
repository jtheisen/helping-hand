using System;
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
using Microsoft.Kinect;

namespace KinectControl
{
    /// <summary>
    /// Interaction logic for SkeletonViewer.xaml
    /// </summary>
    public partial class SkeletonViewer : UserControl
    {
        public SkeletonViewer()
        {
            InitializeComponent();
        }

        public void UpdateSkeleton(Skeleton skeleton)
        {
            var joints = skeleton.Joints.ToArray();

            for (int i = 0; i < joints.Length; ++i)
            {
                if (Canvas.Children.Count <= i)
                {
                    Canvas.Children.Add(MakeShape());
                }

                Canvas.SetLeft(Canvas.Children[i], joints[i].Position.X);
                Canvas.SetTop(Canvas.Children[i], joints[i].Position.Y);
            }
        }

        FrameworkElement MakeShape()
        {
            return new Ellipse()
            {
                Fill = new SolidColorBrush(Colors.Black),
                Width = .02,
                Height = .02
            };
        }
    }
}
