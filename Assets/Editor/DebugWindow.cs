using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class DebugWindow : EditorWindow
{
    GameStarter target;

    int iterations = 100;
    bool buildInSceneEachIteration = false; // true면 매번 Instantiate/삭제(느림)
    bool logFailDetails = true;
    int logFailLimit = 20;

    // 랜덤 시드 생성용
    int randomSeedMin = int.MinValue;
    int randomSeedMax = int.MaxValue;

    Vector2 scroll;
    string lastReport = "";

    [MenuItem("Tools/Debug Window")]
    public static void ShowWindow()
    {
        GetWindow<DebugWindow>("Debug Window");
    }

    void OnGUI()
    {
        EditorGUILayout.Space(6);

        // 타겟 자동 탐색
        if (target == null)
        {
            var go = Selection.activeGameObject;
            if (go != null)
                target = go.GetComponentInParent<GameStarter>() ?? go.GetComponentInChildren<GameStarter>();
        }

        target = (GameStarter)EditorGUILayout.ObjectField("Target(GameStarter)", target, typeof(GameStarter), true);

        if (target == null)
        {
            EditorGUILayout.HelpBox("씬에 GameStarter 컴포넌트가 필요합니다.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reset (Delete Puzzle)", GUILayout.Height(28)))
            {
                Undo.RegisterFullObjectHierarchyUndo(target.gameObject, "Reset Puzzle");
                target.ClearPuzzle();
                MarkDirty(target);
                lastReport = "Reset done.";
            }

            if (GUILayout.Button("Regen (Today Seed)", GUILayout.Height(28)))
            {
                Undo.RegisterFullObjectHierarchyUndo(target.gameObject, "Generate Today Puzzle");
                target.GenerateTodayAndBuild();
                MarkDirty(target);
                var v = target.ValidateCurrentMaze();
                lastReport = $"Today regen done. Validate: {v.ok} / {v.msg} / goal={v.goal} / dist={v.dist}";
            }

            if (GUILayout.Button("Regen (Random Seed)", GUILayout.Height(28)))
            {
                Undo.RegisterFullObjectHierarchyUndo(target.gameObject, "Generate Random Puzzle");
                int seed = new System.Random(Environment.TickCount).Next(randomSeedMin, randomSeedMax);
                target.GenerateRandomAndBuild(seed);
                MarkDirty(target);
                var v = target.ValidateCurrentMaze();
                lastReport = $"Random regen seed={seed}. Validate: {v.ok} / {v.msg} / goal={v.goal} / dist={v.dist}";
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Batch Validate", EditorStyles.boldLabel);

        iterations = EditorGUILayout.IntField("Iterations (N)", Mathf.Max(1, iterations));
        buildInSceneEachIteration = EditorGUILayout.ToggleLeft("Build in Scene each iteration (slow)", buildInSceneEachIteration);
        logFailDetails = EditorGUILayout.ToggleLeft("Log fail details", logFailDetails);
        logFailLimit = EditorGUILayout.IntField("Fail log limit", Mathf.Clamp(logFailLimit, 1, 200));

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Run N Random Seeds Validate", GUILayout.Height(28)))
            {
                RunBatchRandomSeeds(target, iterations);
            }

            if (GUILayout.Button("Run N Today-Based Seeds Validate", GUILayout.Height(28)))
            {
                // 날짜 시드의 “변형” 테스트: 오늘 시드를 기준으로 i를 섞어서 테스트
                RunBatchTodayVariantSeeds(target, iterations);
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Last Report", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(220));
        EditorGUILayout.TextArea(lastReport, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    void RunBatchRandomSeeds(GameStarter t, int n)
    {
        int size = t.MazeSize;
        int margin = t.GoalMargin;
        Vector3Int entrance = t.Entrance; // (1,1,0)
        Vector3Int start = new Vector3Int(entrance.x, entrance.y, entrance.z + 1); // (1,1,1)

        var gen = new MazeGenerator();
        var rng = new System.Random(Environment.TickCount);

        int ok = 0, fail = 0;
        List<string> failLines = new List<string>();

        for (int i = 1; i <= n; i++)
        {
            int seed = rng.Next(randomSeedMin, randomSeedMax);

            bool[,,] maze = gen.Generate(size, seed, entrance, t.LoopChance);

            if (!MazeDebugUtility.TrySelectGoalAndPath(
                    maze, size, start, margin, // start = (1,1,1)
                    out var goal, out var path, out var reachableOpen, out var reason))
            {
                fail++;
                if (logFailDetails && failLines.Count < logFailLimit)
                    failLines.Add($"#{i} seed={seed} SELECT_FAIL reason={reason}");
                continue;
            }

            var vr = MazeDebugUtility.Validate(maze, size, start, goal, margin);
            if (!vr.ok)
            {
                fail++;
                if (logFailDetails && failLines.Count < logFailLimit)
                    failLines.Add($"#{i} seed={seed} VALIDATE_FAIL {vr.message} goal={goal}");
            }
            else
            {
                ok++;
            }

            if (buildInSceneEachIteration)
            {
                Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Batch Build Iteration");
                t.GenerateRandomAndBuild(seed);
                t.ClearPuzzle();
            }
        }

        lastReport = BuildReport("RandomSeeds", size, margin, n, ok, fail, failLines);
        Debug.Log(lastReport);
    }

    void RunBatchTodayVariantSeeds(GameStarter t, int n)
    {
        int size = t.MazeSize;
        int margin = t.GoalMargin;
        Vector3Int entrance = t.Entrance;
        Vector3Int start = new Vector3Int(entrance.x, entrance.y, entrance.z + 1);

        var gen = new MazeGenerator();

        int baseSeed = int.Parse(DateTime.Now.ToString("yyyyMMdd"));

        int ok = 0, fail = 0;
        List<string> failLines = new List<string>();

        for (int i = 1; i <= n; i++)
        {
            int seed = unchecked(baseSeed * 73856093 ^ i * 19349663);

            bool[,,] maze = gen.Generate(size, seed, entrance, t.LoopChance);

            if (!MazeDebugUtility.TrySelectGoalAndPath(
                    maze, size, start, margin,
                    out var goal, out var path, out var reachableOpen, out var reason))
            {
                fail++;
                if (logFailDetails && failLines.Count < logFailLimit)
                    failLines.Add($"#{i} seed={seed} SELECT_FAIL reason={reason}");
                continue;
            }

            var vr = MazeDebugUtility.Validate(maze, size, start, goal, margin);
            if (!vr.ok)
            {
                fail++;
                if (logFailDetails && failLines.Count < logFailLimit)
                    failLines.Add($"#{i} seed={seed} VALIDATE_FAIL {vr.message} goal={goal}");
            }
            else
            {
                ok++;
            }

            if (buildInSceneEachIteration)
            {
                Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Batch Build Iteration");
                t.GenerateRandomAndBuild(seed);
                t.ClearPuzzle();
            }
        }

        lastReport = BuildReport("TodayVariantSeeds", size, margin, n, ok, fail, failLines);
        Debug.Log(lastReport);
    }

    static string BuildReport(string title, int size, int margin, int n, int ok, int fail, List<string> failLines)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(2048);
        sb.AppendLine($"[{title}] size={size} margin={margin} N={n} OK={ok} FAIL={fail}");
        if (failLines != null && failLines.Count > 0)
        {
            sb.AppendLine("---- Fail Samples ----");
            foreach (var line in failLines)
                sb.AppendLine(line);
        }
        return sb.ToString();
    }

    static void MarkDirty(GameStarter t)
    {
        EditorUtility.SetDirty(t);
        EditorSceneManagerMarkDirtySafe();
        SceneView.RepaintAll();
    }

    static void EditorSceneManagerMarkDirtySafe()
    {
#if UNITY_2020_1_OR_NEWER
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
#endif
    }
}
