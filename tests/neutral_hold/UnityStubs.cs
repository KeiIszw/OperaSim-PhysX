using System;
namespace UnityEngine {
public class MonoBehaviour { public bool isActiveAndEnabled=true; public GameObject gameObject=new GameObject(); }
public class GameObject {}
public class TooltipAttribute:Attribute {public TooltipAttribute(string s){}}
public class MinAttribute:Attribute {public MinAttribute(float x){}}
public class RangeAttribute:Attribute {public RangeAttribute(float a,float b){}}
public class HeaderAttribute:Attribute {public HeaderAttribute(string s){}}
public static class Mathf {public const float Rad2Deg=180/(float)Math.PI; public static float Abs(float x)=>Math.Abs(x);public static float Clamp(float x,float a,float b)=>Math.Clamp(x,a,b);}
public static class Time {public static double timeAsDouble;public static float fixedDeltaTime=.02f;}
public static class Debug {public static void LogWarning(string s,object o){}}
public enum ArticulationDofLock {LimitedMotion,FreeMotion}
public enum ArticulationDriveType {Force}
public struct ArticulationDrive {public float target,targetVelocity,stiffness,damping,lowerLimit,upperLimit,forceLimit;public ArticulationDriveType driveType;}
public class ArticulationBody {public ArticulationDrive xDrive;public float linearDamping,angularDamping,jointFriction;public int dofCount=1;public float[] jointPosition=new float[1];public ArticulationDofLock twistLock;public T[] GetComponentsInParent<T>()=>Array.Empty<T>();}
}
namespace RosMessageTypes.Std {public class Float64Msg {public double data;}}
namespace Unity.Robotics.ROSTCPConnector {public class ROSConnection {public static ROSConnection GetOrCreateInstance()=>new ROSConnection();public void Subscribe<T>(string s,Action<T> f){}}}
public class EmergencyStop {public bool isEmergencyStop;public static EmergencyStop GetEmergencyStop(UnityEngine.GameObject o)=>null;}
public static class Utils {public static string PreprocessNamespace(UnityEngine.GameObject o,string s)=>s;}
