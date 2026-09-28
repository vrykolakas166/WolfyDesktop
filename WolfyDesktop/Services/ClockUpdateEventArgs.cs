using System;

namespace WolfyDesktop.Services;

public class ClockUpdateEventArgs : EventArgs
{
    public string Hours { get; init; } = "00";
    public string Minutes { get; init; } = "00";
    public string Seconds { get; init; } = "00";
}
