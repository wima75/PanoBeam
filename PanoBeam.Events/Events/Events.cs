﻿using System;
using PanoBeam.Events.Data;

namespace PanoBeam.Events.Events
{
    public class ApplicationReady : Event<EventArgs> { }

    public class SettingsChanged : Event<EventArgs> { }

    public class ControlPointsMoved : Event<ControlPointData> { }

    public class CalibrationStarted : Event<EventArgs> { }

    public class CalibrationFinished : Event<EventArgs> { }
}