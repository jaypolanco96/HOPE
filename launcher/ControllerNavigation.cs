using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace Hope.Launcher;

internal sealed class ControllerNavigation : IDisposable
{
    [StructLayout(LayoutKind.Sequential)] private struct Gamepad
    {
        public ushort Buttons;
        public byte LeftTrigger, RightTrigger;
        public short LeftX, LeftY, RightX, RightY;
    }
    [StructLayout(LayoutKind.Sequential)] private struct State { public uint Packet; public Gamepad Pad; }
    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")] private static extern uint GetState(uint userIndex, out State state);
    private readonly DispatcherTimer timer;
    private ushort previous;
    private int held;
    private long repeatAt;

    public ControllerNavigation(Action<ushort> action)
    {
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        timer.Tick += (_, _) =>
        {
            try
            {
                ushort buttons = 0;
                for (uint i = 0; i < 4; i++)
                {
                    if (GetState(i, out var state) != 0) continue;
                    buttons = state.Pad.Buttons;
                    if (state.Pad.LeftY > 16000) buttons |= 1;
                    if (state.Pad.LeftY < -16000) buttons |= 2;
                    if (state.Pad.LeftX < -16000) buttons |= 4;
                    if (state.Pad.LeftX > 16000) buttons |= 8;
                    break;
                }
                var pressed = (ushort)(buttons & ~previous);
                previous = buttons;
                int direction = buttons & 15;
                if (direction != held) { held = direction; repeatAt = Environment.TickCount64 + 420; }
                else if (held != 0 && Environment.TickCount64 >= repeatAt)
                {
                    pressed |= (ushort)held;
                    repeatAt = Environment.TickCount64 + 110;
                }
                if (pressed != 0) action(pressed);
            }
            catch (DllNotFoundException) { timer.Stop(); }
            catch (EntryPointNotFoundException) { timer.Stop(); }
        };
        timer.Start();
    }
    public void Dispose() => timer.Stop();
}
