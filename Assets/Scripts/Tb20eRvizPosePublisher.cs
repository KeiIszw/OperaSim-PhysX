using System;
using System.Linq;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;

public class Tb20eRvizPosePublisher : MonoBehaviour
{
    public string topic = "/TB20e_0/rviz_link_poses";
    private const string ConfigTopic = "/TB20e_0/rviz_mesh_config";
    private const string RequestTopic = "/TB20e_0/rviz_mesh_config_request";
    private Transform[] links;
    private Transform root;
    private Transform[] targets;
    private ROSConnection ros;
    private float next;
    private static readonly string[] Paths = new string[] {
"00油圧メス_Brade",
"00parts/パーツ ボディー.046",
"00parts/パーツ ボディー.047",
"00parts/パーツ ボディー.048",
"00parts/パーツ ボディー.049",
"00parts/パーツ ボディー.050",
"00parts/パーツ ボディー.051",
"00parts/パーツ ボディー.052",
"00parts/パーツ ボディー.053",
"00parts/パーツ ボディー.054",
"00parts/パーツ ボディー.055",
"00parts/パーツ ボディー.056",
"00parts/パーツ ボディー.057",
"00parts/パーツ ボディー.058",
"00parts/パーツ ボディー.059",
"00parts/パーツ ボディー.060",
"00parts/パーツ ボディー.061",
"00parts/パーツ ボディー.062",
"00parts/パーツ ボディー.063",
"00parts/パーツ ボディー.064",
"00parts/パーツ ボディー.066",
"00parts/パーツ ボディー.067",
"00parts/パーツ ボディー.069",
"00parts/パーツ ボディー.070",
"swing_link/02油圧メス_Swing",
"swing_link/02parts/01Canopy/パーツ ボディー",
"swing_link/02parts/01Canopy/パーツ ボディー.001",
"swing_link/02parts/01Canopy/パーツ ボディー.002",
"swing_link/02parts/01Canopy/パーツ ボディー.003",
"swing_link/02parts/01Canopy/Mesh_148",
"swing_link/02parts/01Canopy/Mesh_149",
"swing_link/02parts/01Canopy/ｿ鬢J1",
"swing_link/02parts/01Canopy/ｿ鬢J1.001",
"swing_link/02parts/01Canopy/パーツ ボディー.004",
"swing_link/02parts/01Canopy/パーツ ボディー.005",
"swing_link/02parts/01Canopy/パーツ ボディー.006",
"swing_link/02parts/01Cover/brep_1",
"swing_link/02parts/01Cover/パーツ ボディー.007",
"swing_link/02parts/01Cover/パーツ ボディー.008",
"swing_link/02parts/01Cover/パーツ ボディー.009",
"swing_link/02parts/01Cover/パーツ ボディー.010",
"swing_link/02parts/01Cover/パーツ ボディー.011",
"swing_link/02parts/01Cover/パーツ ボディー.012",
"swing_link/02parts/01Cover/パーツ ボディー.013",
"swing_link/02parts/01Cover/パーツ ボディー.014",
"swing_link/02parts/01Cover/ボディー.5",
"swing_link/02parts/01Cover/Mesh_11",
"swing_link/02parts/01Cover/Mesh_12",
"swing_link/02parts/01Cover/Mesh_13",
"swing_link/02parts/01Cover/Mesh_14",
"swing_link/02parts/01Cover/Mesh_15",
"swing_link/02parts/01Cover/パーツ ボディー.015",
"swing_link/02parts/01Cover/Mesh_17",
"swing_link/02parts/01Cover/Mesh_18",
"swing_link/02parts/01Cover/Mesh_19",
"swing_link/02parts/01Cover/Mesh_20",
"swing_link/02parts/01Cover/Mesh_21",
"swing_link/02parts/01Cover/Mesh_22",
"swing_link/02parts/01Cover/Mesh_23",
"swing_link/02parts/01Cover/Mesh_24",
"swing_link/02parts/01Cover/Mesh_25",
"swing_link/02parts/01Cover/Mesh_26",
"swing_link/02parts/01Cover/Mesh_27",
"swing_link/02parts/01Cover/Mesh_28",
"swing_link/02parts/01Cover/Mesh_29",
"swing_link/02parts/01Cover/Mesh_30",
"swing_link/02parts/01Cover/Mesh_31",
"swing_link/02parts/01Cover/Mesh_32",
"swing_link/02parts/01Cover/Mesh_33",
"swing_link/02parts/01Cover/Mesh_34",
"swing_link/02parts/01Cover/Mesh_35",
"swing_link/02parts/01Cover/Mesh_36",
"swing_link/02parts/01Cover/Mesh_37",
"swing_link/02parts/01Cover/Mesh_38",
"swing_link/02parts/01Cover/Mesh_39",
"swing_link/02parts/01Cover/Mesh_40",
"swing_link/02parts/01Cover/Mesh_41",
"swing_link/02parts/01Cover/Mesh_42",
"swing_link/02parts/01Cover/Mesh_43",
"swing_link/02parts/01Cover/Mesh_44",
"swing_link/02parts/01Cover/Mesh_45",
"swing_link/02parts/01Cover/Mesh_46",
"swing_link/02parts/01Cover/Mesh_47",
"swing_link/02parts/01Cover/Mesh_48",
"swing_link/02parts/01Cover/Mesh_49",
"swing_link/02parts/01Cover/Mesh_50",
"swing_link/02parts/01Cover/Mesh_51",
"swing_link/02parts/01Cover/Mesh_52",
"swing_link/02parts/01Cover/Mesh_53",
"swing_link/02parts/01Cover/Mesh_54",
"swing_link/02parts/01Cover/Mesh_55",
"swing_link/02parts/01Cover/Mesh_56",
"swing_link/02parts/01Cover/Mesh_57",
"swing_link/02parts/01Cover/Mesh_58",
"swing_link/02parts/01Cover/Mesh_59",
"swing_link/02parts/01Cover/Mesh_60",
"swing_link/02parts/01Cover/Mesh_61",
"swing_link/02parts/01Cover/Mesh_62",
"swing_link/02parts/01Cover/Mesh_63",
"swing_link/02parts/01Cover/Mesh_64",
"swing_link/02parts/01Cover/Mesh_65",
"swing_link/02parts/01Cover/Mesh_66",
"swing_link/02parts/01Cover/Mesh_67",
"swing_link/02parts/01Cover/Mesh_68",
"swing_link/02parts/01Cover/Mesh_69",
"swing_link/02parts/01Cover/Mesh_70",
"swing_link/02parts/01Cover/Mesh_71",
"swing_link/02parts/01Cover/Mesh_72",
"swing_link/02parts/01Cover/Mesh_73",
"swing_link/02parts/01Cover/Mesh_74",
"swing_link/02parts/01Cover/Mesh_75",
"swing_link/02parts/01Cover/Mesh_76",
"swing_link/02parts/01Cover/Mesh_77",
"swing_link/02parts/01Cover/Mesh_78",
"swing_link/02parts/01Cover/Mesh_79",
"swing_link/02parts/01Cover/Mesh_80",
"swing_link/02parts/01Cover/Mesh_81",
"swing_link/02parts/01Cover/Mesh_82",
"swing_link/02parts/01Cover/Mesh_83",
"swing_link/02parts/01Cover/Mesh_84",
"swing_link/02parts/01Cover/Mesh_85",
"swing_link/02parts/01Cover/Mesh_86",
"swing_link/02parts/01Floor/パーツ ボディー.016",
"swing_link/02parts/01Floor/Mesh_220",
"swing_link/02parts/01Floor/Mesh_221",
"swing_link/02parts/01Floor/パーツ ボディー.017",
"swing_link/02parts/01Floor/パーツ ボディー.018",
"swing_link/02parts/01Floor/OPEN_SHELL #241840",
"swing_link/02parts/01Floor/OPEN_SHELL #241840.001",
"swing_link/02parts/01Floor/Mesh_226",
"swing_link/02parts/01Floor/抽出.1",
"swing_link/02parts/01Floor/抽出.002",
"swing_link/02parts/01Floor/NONE",
"swing_link/02parts/01Floor/brep_4",
"swing_link/02parts/01Floor/パーツ ボディー.019",
"swing_link/02parts/01Floor/ReTrace2",
"swing_link/02parts/01Floor/brep_2",
"swing_link/02parts/01Floor/MANIFOLD_SOLID_BREP #32",
"swing_link/02parts/01Floor/brep_1.001",
"swing_link/02parts/01Floor/ReTrace2.001",
"swing_link/02parts/01Floor/ボディー.11",
"swing_link/02parts/01Floor/120706協統",
"swing_link/02parts/01Floor/パーツ ボディー.020",
"swing_link/02parts/01Floor/パーツ ボディー.021",
"swing_link/02parts/01Floor/brep_4.001",
"swing_link/02parts/01Floor/Mesh_242",
"swing_link/02parts/01Floor/BREP_1",
"swing_link/02parts/01レバー/パーツ ボディー.022",
"swing_link/02parts/01レバー/パーツ ボディー.023",
"swing_link/02parts/01レバー/NONE.001",
"swing_link/02parts/01レバー/NONE.002",
"swing_link/02parts/01レバー/パーツ ボディー.024",
"swing_link/02parts/01レバー/パーツ ボディー.025",
"swing_link/02parts/01レバー/パーツ ボディー.026",
"swing_link/02parts/01レバー/パーツ ボディー.027",
"swing_link/02parts/01レバー/パーツ ボディー.028",
"swing_link/02parts/01レバー/パーツ ボディー.029",
"swing_link/02parts/01レバー/NONE.003",
"swing_link/02parts/01レバー/NONE.004",
"swing_link/02parts/01レバー/NONE.005",
"swing_link/02parts/01レバー/NONE.006",
"swing_link/02parts/01レバー/パーツ ボディー.030",
"swing_link/02parts/01レバー/NONE.007",
"swing_link/02parts/01レバー/NONE.008",
"swing_link/02parts/01レバー/パーツ ボディー.031",
"swing_link/02parts/01レバー/パーツ ボディー.032",
"swing_link/02parts/01レバー/パーツ ボディー.033",
"swing_link/02parts/01レバー/パーツ ボディー.034",
"swing_link/02parts/01レバー/パーツ ボディー.035",
"swing_link/02parts/01レバー/パーツ ボディー.036",
"swing_link/02parts/01レバー/パーツ ボディー.037",
"swing_link/02parts/01レバー/パーツ ボディー.038",
"swing_link/02parts/01レバー/パーツ ボディー.039",
"swing_link/02parts/01レバー/パーツ ボディー.040",
"swing_link/02parts/01レバー/パーツ ボディー.041",
"swing_link/02parts/01レバー/パーツ ボディー.042",
"swing_link/02parts/01レバー/パーツ ボディー.043",
"swing_link/02parts/01レバー/パーツ ボディー.044",
"swing_link/02parts/01レバー/パーツ ボディー.045",
"swing_link/02parts/01Hontai/パーツ ボディー.100",
"swing_link/02parts/01Hontai/パーツ ボディー.101",
"swing_link/02parts/01Hontai/パーツ ボディー.102",
"swing_link/02parts/01Hontai/パーツ ボディー.103",
"swing_link/02parts/01Hontai/パーツ ボディー.104",
"swing_link/02parts/01Hontai/Mesh_216",
"swing_link/02parts/01Hontai/Mesh_217",
"swing_link/02parts/01Hontai/パーツ ボディー.105",
"swing_link/02parts/01FloorMat/パーツ ボディー.106",
"swing_link/02parts/01Seat/Part Body",
"swing_link/02parts/01Seat/Part Body.001",
"swing_link/02parts/01Weight/brep_3",
"swing_link/02parts/01Weight/brep_3.001",
"swing_link/02parts/01Weight/brep_3.002",
"swing_link/02parts/01Weight/Mesh_249",
"swing_link/02parts/01Weight/Mesh_250",
"swing_link/03Swing/03油圧オス_Boom",
"swing_link/03Swing/03油圧オス_Swing",
"swing_link/03Swing/03parts/パーツ ボディー.096",
"swing_link/03Swing/03parts/パーツ ボディー.099",
"swing_link/03Swing/boom_link/04油圧メス_Arm",
"swing_link/03Swing/boom_link/04油圧メス_Boom",
"swing_link/03Swing/boom_link/arm_link/05油圧メス_Sam",
"swing_link/03Swing/boom_link/arm_link/05油圧メス_Backet",
"swing_link/03Swing/boom_link/arm_link/05油圧オス_Arm",
"swing_link/03Swing/boom_link/arm_link/05parts/パーツ ボディー.3",
"swing_link/03Swing/boom_link/arm_link/05parts/パーツ ボディー.2",
"swing_link/03Swing/boom_link/arm_link/05parts/NONE.026",
"swing_link/03Swing/boom_link/arm_link/05parts/パーツ ボディー.1",
"swing_link/03Swing/boom_link/arm_link/05parts/パーツ ボディー.088",
"swing_link/03Swing/boom_link/arm_link/05Backet追従/NONE.009",
"swing_link/03Swing/boom_link/arm_link/05Backet追従/NONE.010",
"swing_link/03Swing/boom_link/arm_link/05Backet追従/パーツ ボディー.077",
"swing_link/03Swing/boom_link/arm_link/05Backet追従/パーツ ボディー.078",
"swing_link/03Swing/boom_link/arm_link/05Backet追従/NONE.011",
"swing_link/03Swing/boom_link/arm_link/05Backet追従/NONE.012",
"swing_link/03Swing/boom_link/arm_link/05Backet追従/パーツボディー",
"swing_link/03Swing/boom_link/arm_link/05Backet追従/NONE.023",
"swing_link/03Swing/boom_link/arm_link/05Backet追従/NONE.025",
"swing_link/03Swing/boom_link/arm_link/07Sam/07油圧オス_Sam",
"swing_link/03Swing/boom_link/arm_link/07Sam/07parts/NONE.013",
"swing_link/03Swing/boom_link/arm_link/07Sam/07parts/Solid1",
"swing_link/03Swing/boom_link/arm_link/07Sam/07parts/Solid1.001",
"swing_link/03Swing/boom_link/arm_link/07Sam/07parts/Solid1.002",
"swing_link/03Swing/boom_link/arm_link/bucket_link/06-1追従",
"swing_link/03Swing/boom_link/arm_link/bucket_link/06-1追従/NONE.018",
"swing_link/03Swing/boom_link/arm_link/bucket_link/06-1追従/NONE.019",
"swing_link/03Swing/boom_link/arm_link/bucket_link/06-1追従/NONE.020",
"swing_link/03Swing/boom_link/arm_link/bucket_link/06-1追従/NONE.021",
"swing_link/03Swing/boom_link/arm_link/bucket_link/06-1追従/NONE.022",
"swing_link/03Swing/boom_link/arm_link/bucket_link/06-1追従/NONE.024",
"swing_link/03Swing/boom_link/arm_link/bucket_link/06-1追従/06-1油圧オス_Backet",
"swing_link/03Swing/boom_link/arm_link/bucket_link/06parts/パーツ ボディー.076",
"swing_link/03Swing/boom_link/arm_link/bucket_link/06parts/NONE.014",
"swing_link/03Swing/boom_link/arm_link/bucket_link/06parts/NONE.015",
"swing_link/03Swing/boom_link/arm_link/bucket_link/06parts/NONE.016",
"swing_link/03Swing/boom_link/arm_link/bucket_link/06parts/NONE.017",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_251",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_252",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_253",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_254",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_255",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_256",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_257",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_258",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_259",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_260",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_261",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_262",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_263",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_264",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_265",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_266",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_267",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_268",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_269",
"swing_link/03Swing/boom_link/04Boom_name/Mesh_270",
"swing_link/03Swing/boom_link/04parts/パーツ ボディー.085",
"swing_link/03Swing/boom_link/04parts/パーツ ボディー.086",
"swing_link/03Swing/boom_link/04parts/パーツ２",
"swing_link/03Swing/boom_link/04parts/パーツ ボディー.087",
"swing_link/03Swing/boom_link/04parts/パーツ ボディー.089",
"swing_link/03Swing/boom_link/04parts/パーツ ボディー.093",
"swing_link/03Swing/boom_link/04parts/パーツ",
"01Brade/01油圧オス_Brade",
"01Brade/01parts/パーツ ボディー.071",
"01Brade/01parts/パーツ ボディー.072",
"01Brade/01parts/パーツ ボディー.073",
"01Brade/01parts/パーツ ボディー.074",
"01Brade/01parts/パーツ ボディー.075"
};
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<Tb20eRvizPosePublisher>() == null)
            new GameObject("TB20e RViz Pose Publisher").AddComponent<Tb20eRvizPosePublisher>();
    }
    private static readonly string[] MeshNames = new string[] { "Mesh_142","Mesh_120","Mesh_121","Mesh_122","Mesh_123","Mesh_124","Mesh_125","Mesh_126","Mesh_127","Mesh_128","Mesh_129","Mesh_130","Mesh_131","Mesh_132","Mesh_133","Mesh_134","Mesh_135","Mesh_136","Mesh_137","Mesh_138","Mesh_140","Mesh_141","Mesh_143","Mesh_144","Mesh_209","Mesh_0","Mesh_145","Mesh_146","Mesh_147","Mesh_148","Mesh_149","Mesh_150","Mesh_151","Mesh_152","Mesh_153","Mesh_154","Mesh_1","Mesh_2","Mesh_3","Mesh_4","Mesh_5","Mesh_6","Mesh_7","Mesh_8","Mesh_9","Mesh_10","Mesh_11","Mesh_12","Mesh_13","Mesh_14","Mesh_15","Mesh_16","Mesh_17","Mesh_18","Mesh_19","Mesh_20","Mesh_21","Mesh_22","Mesh_23","Mesh_24","Mesh_25","Mesh_26","Mesh_27","Mesh_28","Mesh_29","Mesh_30","Mesh_31","Mesh_32","Mesh_33","Mesh_34","Mesh_35","Mesh_36","Mesh_37","Mesh_38","Mesh_39","Mesh_40","Mesh_41","Mesh_42","Mesh_43","Mesh_44","Mesh_45","Mesh_46","Mesh_47","Mesh_48","Mesh_49","Mesh_50","Mesh_51","Mesh_52","Mesh_53","Mesh_54","Mesh_55","Mesh_56","Mesh_57","Mesh_58","Mesh_59","Mesh_60","Mesh_61","Mesh_62","Mesh_63","Mesh_64","Mesh_65","Mesh_66","Mesh_67","Mesh_68","Mesh_69","Mesh_70","Mesh_71","Mesh_72","Mesh_73","Mesh_74","Mesh_75","Mesh_76","Mesh_77","Mesh_78","Mesh_79","Mesh_80","Mesh_81","Mesh_82","Mesh_83","Mesh_84","Mesh_85","Mesh_86","Mesh_87","Mesh_220","Mesh_221","Mesh_222","Mesh_223","Mesh_224","Mesh_225","Mesh_226","Mesh_227","Mesh_228","Mesh_229","Mesh_230","Mesh_231","Mesh_232","Mesh_233","Mesh_234","Mesh_235","Mesh_236","Mesh_237","Mesh_238","Mesh_239","Mesh_240","Mesh_241","Mesh_242","Mesh_243","Mesh_88","Mesh_89","Mesh_90","Mesh_91","Mesh_92","Mesh_93","Mesh_94","Mesh_95","Mesh_96","Mesh_97","Mesh_98","Mesh_99","Mesh_100","Mesh_101","Mesh_102","Mesh_103","Mesh_104","Mesh_105","Mesh_106","Mesh_107","Mesh_108","Mesh_109","Mesh_110","Mesh_111","Mesh_112","Mesh_113","Mesh_114","Mesh_115","Mesh_116","Mesh_117","Mesh_118","Mesh_119","Mesh_211","Mesh_212","Mesh_213","Mesh_214","Mesh_215","Mesh_216","Mesh_217","Mesh_218","Mesh_219","Mesh_244","Mesh_245","Mesh_246","Mesh_247","Mesh_248","Mesh_249","Mesh_250","Mesh_204","Mesh_208","Mesh_207","Mesh_210","Mesh_201","Mesh_205","Mesh_184","Mesh_192","Mesh_200","Mesh_161","Mesh_164","Mesh_190","Mesh_193","Mesh_198","Mesh_162","Mesh_163","Mesh_165","Mesh_166","Mesh_167","Mesh_168","Mesh_173","Mesh_181","Mesh_183","Mesh_189","Mesh_170","Mesh_186","Mesh_187","Mesh_188","Mesh_169","Mesh_176","Mesh_177","Mesh_178","Mesh_179","Mesh_180","Mesh_182","Mesh_191","Mesh_160","Mesh_171","Mesh_172","Mesh_174","Mesh_175","Mesh_251","Mesh_252","Mesh_253","Mesh_254","Mesh_255","Mesh_256","Mesh_257","Mesh_258","Mesh_259","Mesh_260","Mesh_261","Mesh_262","Mesh_263","Mesh_264","Mesh_265","Mesh_266","Mesh_267","Mesh_268","Mesh_269","Mesh_270","Mesh_194","Mesh_195","Mesh_196","Mesh_197","Mesh_199","Mesh_203","Mesh_206","Mesh_139","Mesh_155","Mesh_156","Mesh_157","Mesh_158","Mesh_159" };
    private static bool IsVisibleMesh(Transform t)
    {
        var renderer = t.GetComponent<MeshRenderer>();
        return t.gameObject.activeInHierarchy && renderer != null && renderer.enabled;
    }
    private static string FullPath(Transform t)
    {
        return t.parent == null ? t.name : FullPath(t.parent) + "/" + t.name;
    }
    private void Start()
    {
        var roots = FindObjectsOfType<Transform>().Where(t => t.name == "base_link" &&
            t.GetComponentsInChildren<Transform>().Any(c => c.name == "boom_link") &&
            t.GetComponentsInChildren<Transform>().Any(c => c.name == "bucket_link")).ToArray();
        if (roots.Length != 1) { Debug.LogError("RViz pose publisher: expected one TB20e base_link, found " + roots.Length); enabled = false; return; }
        root = roots[0];
        var descendants = root.GetComponentsInChildren<Transform>(true);
        var filters = root.GetComponentsInChildren<MeshFilter>(true);
        targets = new Transform[Paths.Length];
        for (int i = 0; i < Paths.Length; i++)
        {
            targets[i] = root.Find(Paths[i]);
            if (targets[i] != null && !IsVisibleMesh(targets[i])) targets[i] = null;
            if (targets[i] == null)
            {
                string leaf = Paths[i].Substring(Paths[i].LastIndexOf('/') + 1);
                var matches = descendants.Where(t => t.name == leaf && IsVisibleMesh(t)).ToArray();
                if (matches.Length == 1) targets[i] = matches[0];
            }
            if (targets[i] == null)
            {
                var matches = filters.Where(f => f.sharedMesh != null && (f.sharedMesh.name == MeshNames[i] || f.sharedMesh.name == Paths[i].Substring(Paths[i].LastIndexOf('/') + 1)) && IsVisibleMesh(f.transform)).ToArray();
                if (matches.Length == 1) targets[i] = matches[0].transform;
            }
        }
        // These six thumb parts are intentionally hidden in the current Unity scene.
        var hiddenNames = new string[] { "05油圧メス_Sam","07油圧オス_Sam","NONE.013","Solid1","Solid1.001","Solid1.002" };
        var omitted = Enumerable.Range(0, targets.Length).Where(i => hiddenNames.Contains(Paths[i].Substring(Paths[i].LastIndexOf('/') + 1))).ToArray();
        var missing = Enumerable.Range(0, targets.Length).Where(i => targets[i] == null && !omitted.Contains(i)).ToArray();
        if (missing.Length > 0)
        {
            string dump = string.Join("\n", filters.Select(f => f.name + " | " + (f.sharedMesh == null ? "null" : f.sharedMesh.name) + " | visible=" + IsVisibleMesh(f.transform) + " | " + FullPath(f.transform)));
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/Tb20eRvizMeshMapping.txt", dump);
            Debug.LogError("RViz mesh mapping unresolved: " + string.Join(", ", missing.Select(i => Paths[i] + " [" + MeshNames[i] + "]")) + "; see Logs/Tb20eRvizMeshMapping.txt");
            enabled = false; return;
        }
        targets = targets.Where((t, i) => !omitted.Contains(i)).ToArray();
        ros = ROSConnection.GetOrCreateInstance();
        string[] names = { "body_link", "swing_link", "boom_link", "arm_link", "bucket_link" };
        links = names.Select(name => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name)).ToArray();
        if (links.Any(t => t == null)) { Debug.LogError("RViz five-link mapping missing: " + string.Join(",", Enumerable.Range(0,5).Where(i => links[i] == null).Select(i => names[i]))); enabled=false; return; }
        // Only moving ancestors of visible meshes need a streamed pose.
        var moving = descendants.Where(t => t != root && t.gameObject.activeInHierarchy &&
            ((t.GetComponent<ArticulationBody>() != null && t.GetComponent<ArticulationBody>().enabled &&
              t.GetComponent<ArticulationBody>().jointType != ArticulationJointType.FixedJoint) ||
             t.GetComponents<Component>().Any(c => c is UnityEngine.Animations.IConstraint constraint &&
                 c is Behaviour behaviour && behaviour.enabled && constraint.constraintActive && constraint.weight > 0)));
        var ikTargets = root.GetComponentsInChildren<IKFix>().Where(c => c.isActiveAndEnabled && c.target != null)
            .Select(c => c.target).Where(t => t.IsChildOf(root));
        var candidates = links.Concat(moving).Concat(ikTargets).Distinct().ToArray();
        var owners = targets.Select(target => {
            for (var t = target; t != null && t != root; t = t.parent)
                if (candidates.Contains(t)) return t;
            return null;
        }).ToArray();
        links = links.Concat(candidates.Where(t => !links.Contains(t) && owners.Contains(t))
            .OrderBy(t => FullPath(t), StringComparer.Ordinal)).ToArray();
        System.IO.Directory.CreateDirectory("Logs");
        System.IO.File.WriteAllText("Logs/Tb20eRvizMovingLinks.txt",
            string.Join("\n", links.Select((t, i) => i + " | " + FullPath(t))));
        Debug.Log("RViz streaming " + links.Length + " independently moving links; see Logs/Tb20eRvizMovingLinks.txt");
        ros.RegisterPublisher<Float64MultiArrayMsg>(topic);
        ros.RegisterPublisher<Float64MultiArrayMsg>(ConfigTopic);
        ros.Subscribe<BoolMsg>(RequestTopic, msg => { if (msg.data) PublishConfig(); });
        Debug.Log("RViz pose publisher mapped " + targets.Length + " meshes");
    }
    private static void WritePose(double[] data, int k, Transform reference, Transform target, bool meshCorrection)
    {
        RosMessageTypes.Geometry.PointMsg pos = reference.InverseTransformPoint(target.position).To<FLU>();
        RosMessageTypes.Geometry.QuaternionMsg qmsg = (Quaternion.Inverse(reference.rotation) * target.rotation).To<FLU>();
        var q = new Quaternion((float)qmsg.x, (float)qmsg.y, (float)qmsg.z, (float)qmsg.w);
        if (meshCorrection) q *= new Quaternion(0,0,Mathf.Sin(Mathf.PI/4),Mathf.Cos(Mathf.PI/4));
        data[k]=pos.x;data[k+1]=pos.y;data[k+2]=pos.z;
        data[k+3]=q.x;data[k+4]=q.y;data[k+5]=q.z;data[k+6]=q.w;
    }
    private void PublishConfig()
    {
        var data = new double[1 + targets.Length * 8];
        data[0] = links.Length;
        for (int i=0;i<targets.Length;i++)
        {
            int owner=-1;
            for (var t=targets[i];t!=null && t!=root;t=t.parent)
            {
                int index=Array.IndexOf(links,t);
                if(index>=0) {owner=index;break;}
            }
            data[1+i*8]=owner;
            WritePose(data,1+i*8+1,owner<0?root:links[owner],targets[i],true);
        }
        var msg=new Float64MultiArrayMsg();msg.data=data;ros.Publish(ConfigTopic,msg);
    }
    private void LateUpdate()
    {
        if (Time.realtimeSinceStartup < next) return;
        next=Time.realtimeSinceStartup+0.05f;
        var data=new double[links.Length*7];
        for(int i=0;i<links.Length;i++) WritePose(data,i*7,root,links[i],false);
        var msg=new Float64MultiArrayMsg();msg.data=data;ros.Publish(topic,msg);
    }
}
