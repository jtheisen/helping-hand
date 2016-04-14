using System.Linq;
using Rxx.Parsers.Linq;
using Rxx.Parsers.Reactive.Linq;
using Microsoft.Kinect;
using System.Diagnostics;
using System;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.ComponentModel;
using System.Reactive;

namespace KinectControl
{
    public class TrackedSkeletons
    {
        public IDisposable AddSensor(KinectSensor sensor)
        {
            sensor.SkeletonFrameReady += sensor_SkeletonFrameReady;

            return Disposable.Create(() => sensor.SkeletonFrameReady -= sensor_SkeletonFrameReady);
        }

        public void RemoveSensor(KinectSensor sensor)
        {
            sensor.SkeletonFrameReady -= sensor_SkeletonFrameReady;
        }

        public readonly Subject<Subject<Skeleton>> NewSkeleton = new Subject<Subject<Skeleton>>();

        public readonly Subject<int> SkeletonCount = new Subject<int>();

        Dictionary<int, Subject<Skeleton>> trackedSkeletons = new Dictionary<int, Subject<Skeleton>>();

        void sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (var frame = e.OpenSkeletonFrame())
            {
                if (null == frame) return;

                var skeletonData = new Skeleton[frame.SkeletonArrayLength];

                frame.CopySkeletonDataTo(skeletonData);

                foreach (var skeleton in skeletonData)
                {
                    if (skeleton.TrackingState != SkeletonTrackingState.Tracked) continue;

                    Subject<Skeleton> subject = null;

                    if (!trackedSkeletons.TryGetValue(skeleton.TrackingId, out subject))
                    {
                        trackedSkeletons[skeleton.TrackingId] = subject = new Subject<Skeleton>();

                        NewSkeleton.OnNext(subject);
                    }

                    subject.OnNext(skeleton);
                }

                var staleIds = trackedSkeletons.Keys.Where(id => skeletonData.FirstOrDefault(s => s.TrackingId == id) == null).ToArray();

                foreach (var id in staleIds)
                {
                    var subject = trackedSkeletons[id];
                    trackedSkeletons.Remove(id);
                    subject.OnCompleted();
                }

                SkeletonCount.OnNext(trackedSkeletons.Count);
            }
        }
    }

    public class SensorManager
    {
        static SensorManager instance;
        public static SensorManager Instance
        {
            get
            {
                if (null == instance)
                {
                    instance = new SensorManager();
                }
                return instance;
            }
        }

        public BehaviorSubject<KinectSensor> PrimarySensor { get; private set; }

        public TrackedSkeletons TrackedSkeletons { get; private set; }

        List<KinectSensor> connected = new List<KinectSensor>();

        public SensorManager()
        {
            TrackedSkeletons = new TrackedSkeletons();
            PrimarySensor = new BehaviorSubject<KinectSensor>(null);

            KinectSensor.KinectSensors.StatusChanged += new EventHandler<StatusChangedEventArgs>(KinectSensors_StatusChanged);

            foreach (var sensor in KinectSensor.KinectSensors.Where(s => s.Status == KinectStatus.Connected))
            {
                AddSensor(sensor);
            }
        }

        void AddSensor(KinectSensor sensor)
        {
            sensor.SkeletonStream.Enable();
            sensor.ColorStream.Enable();
            sensor.Start();
            connected.Add(sensor);
            TrackedSkeletons.AddSensor(sensor);
            PrimarySensor.OnNext(sensor);
        }

        void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (e.Status == KinectStatus.Connected && !connected.Contains(e.Sensor))
            {
                AddSensor(e.Sensor);
            }
            else if (e.Status != KinectStatus.Connected && connected.Contains(e.Sensor))
            {
                connected.Remove(e.Sensor);
                TrackedSkeletons.RemoveSensor(e.Sensor);
            }
        }
    }

    public class GestureRecognizer
    {
        static GestureRecognizer instance;
        public static GestureRecognizer Instance
        {
            get
            {
                if (null == instance)
                {
                    instance = new GestureRecognizer();
                }
                return instance;
            }
        }

        public enum Gesture
        {
            Left,
            Right
        }

        public readonly Subject<Gesture> Recognized = new Subject<Gesture>();

        private GestureRecognizer()
        {
            InitLeftOrRightRecognition(false);
            InitLeftOrRightRecognition(true);
        }

        public void InitLeftOrRightRecognition(bool left)
        {
            SensorManager.Instance.TrackedSkeletons.NewSkeleton.Subscribe(subject =>
                {
                    var positions = from s in subject
                                    let center = s.Joints.Where(j => j.JointType == JointType.ShoulderCenter).Single()
                                    let lr = s.Joints.Where(j => j.JointType == (left ? JointType.HandLeft : JointType.HandRight)).Single()
                                    where lr.TrackingState == JointTrackingState.Tracked
                                    select lr.Position.X - center.Position.X;

                    //.Do(p => { if (left) Debug.WriteLine(left + " " + p); })

                    var recognizer = positions.Timestamp().Parse(parser =>
                        from next in parser
                        let inner = next.Where(p => Math.Abs(p.Value) < .45)
                        let outer = next.Where(p => Math.Abs(p.Value) > .65)
                        let sequence = from i in inner.OneOrMore()
                                       from f in next.Not(outer).NoneOrMore()
                                       from o in outer.OneOrMoreNonGreedy()
                                       let d = o.First().Timestamp - i.Last().Timestamp
                                       //where d.TotalMilliseconds < 1000
                                       select Unit.Default
                        select sequence);

                    recognizer.Subscribe(u => Recognized.OnNext(left ? Gesture.Left : Gesture.Right));
                });
        }
    }
}
