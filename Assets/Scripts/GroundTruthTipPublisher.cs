// 刃先3点とバケットの有効コライダー頂点から、TerrainColliderへの鉛直距離を配信する。
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;

/// <summary>LiDAR推定に依存しない刃先・バケット形状真値。参照は未設定なら階層から解決する。</summary>
public class GroundTruthTipPublisher : MonoBehaviour
{
    public Transform tip;
    public Transform robotRoot;
    public Transform bucketRoot;
    public TerrainCollider[] terrains;
    public string topicName = "/tb20e/ground/truth";
    [Min(0.1f)] public float publishFrequency = 10f;
    [Min(0.1f)] public float rayAbove = 10f;
    [Min(1f)] public float rayLength = 100f;

    // 推定側のunity_geometry.yamlと同じBoxColliderの外側長辺（local y=-0.5、厚み中心）。親の非等方scaleもTransformPointで反映する。
    public Vector3[] tipLocalPoints = { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) };
    private ROSConnection ros;
    private double lastPublish;
    private readonly List<ColliderPoints> colliderPoints = new List<ColliderPoints>();

    private class ColliderPoints
    {
        public Collider collider;
        public Vector3[] points;
        // 頂点を読めない凸MeshCollider。ClosestPointの支持点で下側を近似する。
        public bool support;
    }

    // 支持点を取る方向（真下からの傾き[deg]）。斜面でも最下点に近い点を拾うため前後左右へ振る。
    private static readonly float[] supportTilts = { -40f, -20f, 0f, 20f, 40f };
    private const float supportReach = 50f;

    /// <summary>凸コライダーの、真下寄りの各方向で最も外側にある表面点（支持点の近似）を返す。</summary>
    private static List<Vector3> SupportPoints(Collider collider)
    {
        // 遠方の点に最も近い表面点は、その方向の支持点に近い。凸形状なので表面上の実在点になる。
        Vector3 center = collider.bounds.center;
        var result = new List<Vector3>();
        foreach (float pitch in supportTilts)
            foreach (float roll in supportTilts)
            {
                Vector3 direction = Quaternion.Euler(pitch, 0f, roll) * Vector3.down;
                result.Add(collider.ClosestPoint(center + direction * supportReach));
            }
        return result;
    }

    /// <summary>OBBの空の角を距離に使わず、メッシュ実頂点とBoxの8頂点をキャッシュする。</summary>
    private void CacheColliderPoints()
    {
        // Tip直近のArticulationBodyがbucket_link。Inspector未設定でもシーンを変更せず解決する。
        if (bucketRoot == null)
        {
            ArticulationBody body = tip.GetComponentInParent<ArticulationBody>();
            if (body != null) bucketRoot = body.transform;
        }
        if (bucketRoot == null || !bucketRoot.IsChildOf(robotRoot) || !tip.IsChildOf(bucketRoot))
        {
            Debug.LogError("GroundTruthTipPublisher: cannot resolve bucket ArticulationBody.", this);
            enabled = false;
            return;
        }
        foreach (Collider collider in bucketRoot.GetComponentsInChildren<Collider>(true))
        {
            Vector3[] points = null;
            MeshCollider mesh = collider as MeshCollider;
            BoxCollider box = collider as BoxCollider;
            bool support = false;
            if (mesh != null && mesh.sharedMesh != null && mesh.sharedMesh.isReadable)
            {
                // 重複した描画頂点を除き、親の非等方scaleは配信時のTransformPointで反映する。
                points = new List<Vector3>(new HashSet<Vector3>(mesh.sharedMesh.vertices)).ToArray();
            }
            else if (mesh != null && mesh.sharedMesh != null && mesh.convex)
            {
                // バケットのコライダーメッシュはRead/Write無効で、Play中は頂点を読めない（Mesh.verticesも
                // AcquireReadOnlyMeshDataも不可）。インポート設定を変えず、物理形状への最近点で代える。
                support = true;
            }
            else if (box != null)
            {
                points = new Vector3[8];
                int index = 0;
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                            points[index++] = box.center + Vector3.Scale(box.size * 0.5f, new Vector3(x, y, z));
            }
            // 未対応/読取不可を箱で代用すると過小距離になるため、有効時はunknownにする。
            colliderPoints.Add(new ColliderPoints { collider = collider, points = points, support = support });
        }
    }

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
        CacheColliderPoints();
        if (!enabled) return;
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
        double[] data = new double[26];
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
        // 従来の17要素は刃先のまま保存。v2を追加: version, shape_min, world xyz,
        // collider instance ID, local vertex index, known count, total count。
        double shapeMinimum;
        Vector3 minimumPoint = Vector3.zero;
        int minimumCollider = 0;
        int minimumVertex = -1;
        int known = 0;
        int total = 3;
        int activeColliders = 0;
        bool complete = true;
        // 欠測を別の有限点で隠さず、診断用の既知部分最小位置は残す。
        for (int i = 0; i < 3; ++i)
        {
            if (double.IsNaN(data[2 + i])) complete = false;
            else ++known;
        }
        shapeMinimum = double.PositiveInfinity;
        for (int i = 0; i < 3; ++i)
            if (!double.IsNaN(data[2 + i]) && data[2 + i] < shapeMinimum)
            {
                shapeMinimum = data[2 + i];
                minimumPoint = tip.TransformPoint(tipLocalPoints[i]);
                minimumVertex = i;
            }
        foreach (ColliderPoints entry in colliderPoints)
        {
            Collider collider = entry.collider;
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy || collider.isTrigger)
                continue;
            ++activeColliders;
            List<Vector3> worldPoints = null;
            if (entry.support)
                worldPoints = SupportPoints(collider);
            else if (entry.points != null && entry.points.Length > 0)
            {
                worldPoints = new List<Vector3>(entry.points.Length);
                foreach (Vector3 localPoint in entry.points)
                    worldPoints.Add(collider.transform.TransformPoint(localPoint));
            }
            if (worldPoints == null)
            {
                complete = false;
                ++total;
                continue;
            }
            for (int vertex = 0; vertex < worldPoints.Count; ++vertex)
            {
                ++total;
                Vector3 point = worldPoints[vertex];
                double height = TerrainHeight(point);
                if (double.IsNaN(height)) { complete = false; continue; }
                ++known;
                double clearance = point.y - height;
                if (clearance < shapeMinimum)
                {
                    shapeMinimum = clearance;
                    minimumPoint = point;
                    minimumCollider = collider.GetInstanceID();
                    minimumVertex = vertex;
                }
            }
        }
        var shapeFlu = minimumPoint.To<FLU>();
        data[17] = 2;
        data[18] = complete && activeColliders > 0 && known == total ? shapeMinimum : double.NaN;
        data[19] = known > 0 ? shapeFlu.x : double.NaN;
        data[20] = known > 0 ? shapeFlu.y : double.NaN;
        data[21] = known > 0 ? shapeFlu.z : double.NaN;
        data[22] = minimumCollider;
        data[23] = minimumVertex;
        data[24] = known;
        data[25] = total;
        var message = new Float64MultiArrayMsg();
        message.data = data;
        ros.Publish(topicName, message);
        lastPublish = now;
    }
}
