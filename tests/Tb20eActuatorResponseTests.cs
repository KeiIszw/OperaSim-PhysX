using System;

// Standalone tests: compile this with Assets/Scripts/Tb20eActuatorResponse.cs.
public static class Tb20eActuatorResponseTests
{
    private static int checks;
    private static void Near(double actual, double expected, string label)
    {
        checks++;
        if (Math.Abs(actual - expected) > 1e-8)
            throw new Exception(label + ": expected " + expected + ", got " + actual);
    }

    public static void Main()
    {
        var model = new Tb20eActuatorResponse();
        // Constant lever must not decay as an accumulated position target catches up.
        for (int i = 0; i < 2000; i++)
            Near(model.Step(i * .005, .005, 50, 0, 0, 0, 80, 1), 40, "steady lever");
        Near(model.Step(10, .005, 0, 0, 0, 0, 80, 1), 0, "neutral");
        Near(model.Step(10.005, .005, -100, 0, 0, 0, 80, .5), -40, "reverse/asymmetry");
        Near(model.Step(10.01, .005, 200, 0, 0, 0, 80, 1), 80, "saturation");
        foreach (double lever in new[] { -10.0, 0, 10 })
            Near(model.Step(11, .005, lever, 10, 0, 0, 50, 1), 0, "deadband");
        Near(model.Step(11, .005, 55, 10, 0, 0, 50, 1), 25, "deadband rescaling");
        Near(model.Step(11, .005, -55, 10, 0, 0, 50, 1), -25, "negative rescaling");

        model.Reset();
        Near(model.Step(0, .005, 100, 0, .1, 0, 50, 1), 0, "dead time start");
        Near(model.Step(.099, .005, -100, 0, .1, 0, 50, 1), 0, "before delay");
        Near(model.Step(.1, .005, -100, 0, .1, 0, 50, 1), 50, "delayed positive");
        Near(model.Step(.201, .005, -100, 0, .1, 0, 50, 1), -50, "delayed reverse");
        // A fault clears both delayed input and flow state; stale motion cannot return.
        model.Reset();
        Near(model.Step(.205, .005, 0, 0, .1, .2, 50, 1), 0, "fault reset");
        Near(model.Step(.4, .005, 0, 0, .1, .2, 50, 1), 0, "no stale replay");

        model.Reset();
        double speed = 0;
        for (int i = 0; i < 40; i++)
            speed = model.Step(i * .005, .005, 100, 0, 0, .2, 50, 1);
        Near(speed, 50 * (1 - Math.Exp(-1)), "one time constant");
        var coarse = new Tb20eActuatorResponse();
        double coarseSpeed = 0;
        for (int i = 0; i < 10; i++)
            coarseSpeed = coarse.Step(i * .02, .02, 100, 0, 0, .2, 50, 1);
        Near(coarseSpeed, speed, "timestep independence");
        Near(model.Step(.2, .2, 0, 0, 0, .2, 50, 1), speed * Math.Exp(-1), "neutral decay");
        Console.WriteLine("PASS: " + checks + " actuator response checks");
    }
}
