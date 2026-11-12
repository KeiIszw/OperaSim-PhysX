// Tipの3点とTerrainColliderだけへの鉛直レイから評価用真値を配信する。シーンの変更は行わない。
using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;

/// <summary>LiDAR推定に依存しないバケット刃先真値。InspectorでTB20eのTipとrobotRootを指定する。</summary>
public class GroundTruthTipPublisher : MonoBehaviour
{
    public Transform tip;
    public Transform robotRoot;
    public TerrainCollider[] terrains;
    public string topicName = "/tb20e/ground/truth";
    [Min(0.1f)] public float publishFrequency = 10f;
    [Min(0.1f)] public float rayAbove = 10f;
    [Min(1f)] public float rayLength = 100f;

    // 推定側のunity_geometry.yamlと同じBoxColliderの外側長辺（local y=-0.5、厚み中心）。親の非等方scaleもTransformPointで反映する。
    public Vector3[] tipLocalPoints = { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) };
    private ROSConnection ros;
    private double lastPublish;

    private void Start()
    {
        // Inspectorの参照設定を省けるよう、未設定なら取付先をrootとし、その配下のBoxCollider付き「Tip」を使う。
        if (robotRoot == null) robotRoot = transform;
        if (tip == null)
        {
            foreach (BoxCollider box in robotRoot.GetComponentsInChildren<BoxCollider>(true))
                if (box.name == "Tip") { tip = box.transform; break; }
        }
        if (tip == null || robotRoot == null || tipLocalPoints == null || tipLocalPoints.Length != 3 ||
            !tip.IsChildOf(robotRoot) || publishFrequency <= 0f)
        {
            Debug.LogError("GroundTruthTipPublisher: set TB20e robotRoot, child Tip, and exactly three samples.", this);
            enabled = false;
            return;
        }
        // TerrainColliderのみを直接照会するので、Convexの作業機コライダーにレイを遮られない。
        if (terrains == null || terrains.Length == 0)
            terrains = FindObjectsOfType<TerrainCollider>();
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<Float64MultiArrayMsg>(topicName);
        lastPublish = Time.time;
    }

    /// <summary>刃先より上から照射し、地面へ食い込んだ負の距離も測る。地形未命中はNaN。</summary>
    private double TerrainHeight(Vector3 point)
    {
        float highest = float.NegativeInfinity;
        Ray ray = new Ray(point + Vector3.up * rayAbove, Vector3.down);
        foreach (TerrainCollider terrain in terrains)
        {
            if (terrain == null || !terrain.enabled || !terrain.gameObject.activeInHierarchy || terrain.transform.IsChildOf(robotRoot))
                continue;
            RaycastHit hit;
            if (terrain.Raycast(ray, out hit, rayLength))
                highest = Mathf.Max(highest, hit.point.y);
        }
        return float.IsNegativeInfinity(highest) ? double.NaN : highest;
    }

    private void FixedUpdate()
    {
        double now = Time.time;
        if (now < lastPublish) lastPublish = now;
        if (now - lastPublish < 1.0 / publishFrequency) return;
        // data = stamp, min_clearance, clearance[3], (tip ROS world xyz, ground world z)[3]。
        // Time.timeはUnitySensorsのsensor.timeと共通。ROS-TCP受信時刻を真値時刻にしない。
        double[] data = new double[17];
        data[0] = now;
        double minimum = double.PositiveInfinity;
        for (int i = 0; i < 3; ++i)
        {
            Vector3 point = tip.TransformPoint(tipLocalPoints[i]);
            double height = TerrainHeight(point);
            double clearance = point.y - height;
            data[2 + i] = clearance;
            minimum = Math.Min(minimum, clearance); // 一点でもNaNなら全体もunknown。
            var flu = point.To<FLU>();
            data[5 + 4 * i] = flu.x;
            data[6 + 4 * i] = flu.y;
            data[7 + 4 * i] = flu.z;
            data[8 + 4 * i] = height;
        }
        data[1] = minimum;
        var message = new Float64MultiArrayMsg();
        message.data = data;
        ros.Publish(topicName, message);
        lastPublish = now;
    }
}
