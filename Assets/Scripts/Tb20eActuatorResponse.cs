using System;
using System.Collections.Generic;

/// <summary>Open-loop lever/flow approximation; no position or measured velocity feedback.</summary>
public sealed class Tb20eActuatorResponse
{
    private readonly Queue<KeyValuePair<double, double>> pending =
        new Queue<KeyValuePair<double, double>>();
    private double delayedLever;
    private double speed;

    public void Reset()
    {
        pending.Clear();
        delayedLever = 0;
        speed = 0;
    }

    // Caller validates parameters and calls once per physics step.
    public double Step(double now, double dt, double lever, double deadband,
        double delay, double timeConstant, double fullSpeed, double negativeRatio)
    {
        pending.Enqueue(new KeyValuePair<double, double>(now, lever));
        while (pending.Count > 0 && pending.Peek().Key <= now - delay)
            delayedLever = pending.Dequeue().Value;

        double magnitude = Math.Max(0, Math.Min(100, Math.Abs(delayedLever)) - deadband)
            / (100 - deadband);
        double requested = Math.Sign(delayedLever) * magnitude * fullSpeed
            * (delayedLever < 0 ? negativeRatio : 1);
        double alpha = timeConstant <= 0 ? 1 : 1 - Math.Exp(-dt / timeConstant);
        speed += alpha * (requested - speed);
        return speed;
    }
}
