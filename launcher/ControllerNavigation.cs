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
    private readonly ControllerEdges edges = new();

    public ControllerNavigation(Action<ushort> action, Action<bool> connectionChanged)
    {
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        timer.Tick += (_, _) =>
        {
            try
            {
                ushort buttons = 0;
                int? device = null;
                for (uint i = 0; i < 4; i++)
                {
                    if (GetState(i, out var state) != 0) continue;
                    device = (int)i;
                    buttons = state.Pad.Buttons;
                    if (state.Pad.LeftY > 16000) buttons |= 1;
                    if (state.Pad.LeftY < -16000) buttons |= 2;
                    if (state.Pad.LeftX < -16000) buttons |= 4;
                    if (state.Pad.LeftX > 16000) buttons |= 8;
                    break;
                }
                var previousDevice = edges.Device;
                var pressed = edges.Update(buttons, Environment.TickCount64, device);
                if (previousDevice != device) connectionChanged(device != null);
                if (pressed != 0) action(pressed);
            }
            catch (DllNotFoundException) { timer.Stop(); connectionChanged(false); }
            catch (EntryPointNotFoundException) { timer.Stop(); connectionChanged(false); }
        };
        timer.Start();
    }
    public void Dispose() => timer.Stop();
}
