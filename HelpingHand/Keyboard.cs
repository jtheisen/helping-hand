using System.Runtime.InteropServices;
using System;
using WindowsInput;

namespace KinectControl
{
    public static class Keyboard
    {
        public static void Foo()
        {
            InputSimulator.SimulateKeyPress(VirtualKeyCode.RIGHT);
        }
    }
}
