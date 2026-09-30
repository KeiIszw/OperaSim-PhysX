using System;
using System.Reflection;
using UnityEngine;

// Standalone controller tests with Unity API doubles; no PhysX simulation.
public static class Tb20eNeutralHoldTests
{
    static int checks;
    static void Near(float actual, float expected, string label)
    {
        checks++;
        if (Math.Abs(actual - expected) > .001f) throw new Exception(label + ": " + actual);
    }
    static void Invoke(string name, object target, params object[] args) =>
        typeof(Tb20eLeverController).GetMethod(name, BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance).Invoke(target, args);
    public static void Main()
    {
        var controller = new Tb20eLeverController();
        foreach (var axis in new[] {controller.boom, controller.arm, controller.bucket, controller.swing})
        {
            if (!axis.neutralHoldEnabled) throw new Exception("All axes must default to holding");
            axis.targetArticulationBody = new ArticulationBody {
                xDrive = new ArticulationDrive {lowerLimit=-180, upperLimit=180, forceLimit=1234},
                twistLock=ArticulationDofLock.LimitedMotion};
            var body=axis.targetArticulationBody;
            body.jointPosition[0]=30 / Mathf.Rad2Deg;
            Invoke("InvalidateCommand", null, axis);
            Near(body.xDrive.target,30,"startup hold");
            Near(body.xDrive.forceLimit,1234,"preserve force cap");
            body.jointPosition[0]=31 / Mathf.Rad2Deg;
            Invoke("InvalidateCommand", null, axis);
            Near(body.xDrive.target,30,"repeated fault must not recapture");
            axis.hasReceivedMessage=true; axis.lastMessageTime=1; axis.latestLeverInput=50;
            Invoke("ApplyAxis",controller,axis,1.0);
            Near(body.xDrive.stiffness,0,"release hold during motion");
            if (body.xDrive.targetVelocity==0) throw new Exception("motion must resume");
            body.jointPosition[0]=40 / Mathf.Rad2Deg;
            axis.latestLeverInput=0;
            Invoke("ApplyAxis",controller,axis,1.01);
            Near(body.xDrive.target,40,"neutral recaptures new pose");
            Near(body.xDrive.targetVelocity,0,"neutral zero speed");
            Near(body.xDrive.stiffness,axis.neutralHoldStiffness,"holding gain");
            body.jointPosition[0]=42 / Mathf.Rad2Deg;
            Invoke("ApplyAxis",controller,axis,1.02);
            Near(body.xDrive.target,40,"neutral target remains fixed");
            // Timeout while moving captures once and clears delayed flow.
            axis.latestLeverInput=50;
            Invoke("ApplyAxis",controller,axis,1.03);
            Invoke("ApplyAxis",controller,axis,2.0);
            Near(body.xDrive.target,42,"timeout captures pose");
            body.jointPosition[0]=43 / Mathf.Rad2Deg;
            Invoke("ApplyAxis",controller,axis,2.1);
            Near(body.xDrive.target,42,"timeout target remains fixed");
            axis.neutralHoldEnabled=false;
            Invoke("InvalidateCommand",null,axis);
            Near(body.xDrive.stiffness,0,"hold disabled");
            axis.neutralHoldEnabled=true;
            axis.deadbandPercent=10;axis.hasReceivedMessage=true;axis.lastMessageTime=3;axis.latestLeverInput=5;
            Invoke("ApplyAxis",controller,axis,3.0);
            Near(body.xDrive.target,43,"deadband engages hold");
            // Re-enable captures current pose, rather than driving back to old target.
            Invoke("OnDisable",controller);
            body.jointPosition[0]=45 / Mathf.Rad2Deg;
            Invoke("ApplyAxis",controller,axis,4.0);
            Near(body.xDrive.target,45,"recapture after disable");
        }
        Console.WriteLine("PASS: " + checks + " neutral hold checks");
    }
}
