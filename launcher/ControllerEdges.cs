namespace Hope.Launcher;

// Edge/repeat state is separate from hardware polling so reconnect behavior
// can be verified with disposable input sequences, without a real controller.
public sealed class ControllerEdges
{
    private ushort previous;
    private int held;
    private long repeatAt;
    public int? Device { get; private set; }
    public ushort Update(ushort buttons, long now, int? device)
    {
        if (Device != device)
        {
            Device = device;
            previous = buttons;
            held = buttons & 15;
            repeatAt = now + 420;
            return 0; // Reconnect/handoff establishes a baseline; held A is not a click.
        }
        if (device == null) { previous = 0; held = 0; return 0; }
        var pressed = (ushort)(buttons & ~previous);
        previous = buttons;
        int direction = buttons & 15;
        if (direction != held) { held = direction; repeatAt = now + 420; }
        else if (held != 0 && now >= repeatAt) { pressed |= (ushort)held; repeatAt = now + 110; }
        return pressed;
    }
}
