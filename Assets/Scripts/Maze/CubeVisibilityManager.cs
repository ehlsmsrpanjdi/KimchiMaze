using UnityEngine;
using System.Collections.Generic;

public class CubeVisibilityManager
{
    private static CubeVisibilityManager instance;
    public static CubeVisibilityManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new CubeVisibilityManager();
            }
            return instance;
        }
    }

    private Camera mainCamera;
    private PlayerController player;
    private Dictionary<Vector3Int, GameObject> cubes = new Dictionary<Vector3Int, GameObject>();
    private CameraController.ViewDirection currentCameraDirection = CameraController.ViewDirection.Front;

    private CubeVisibilityManager() { }

    public void Initialize(float speed = 4f, float alpha = 0.2f)
    {
        mainCamera = Camera.main;
    }

    public void RegisterCube(Vector3Int gridPos, GameObject cubeObj)
    {
        cubes[gridPos] = cubeObj;
    }

    public void SetPlayer(PlayerController newPlayer)
    {
        if (player != null)
        {
            player.OnPositionChanged -= OnPlayerMoved;
        }

        player = newPlayer;

        if (player != null)
        {
            player.OnPositionChanged += OnPlayerMoved;
            UpdateVisibility(player.GridPosition);
        }
    }

    public void Clear()
    {
        if (player != null)
        {
            player.OnPositionChanged -= OnPlayerMoved;
            player = null;
        }
        cubes.Clear();
    }

    public void OnCameraRotated(CameraController.ViewDirection newDirection, Vector3Int playerPos)
    {
        currentCameraDirection = newDirection;
        UpdateVisibility(playerPos);
    }

    void OnPlayerMoved(Vector3Int playerGridPos)
    {
        UpdateVisibility(playerGridPos);
    }

    void UpdateVisibility(Vector3Int playerGridPos)
    {
        foreach (var kvp in cubes)
        {
            Vector3Int cubeGridPos = kvp.Key;
            GameObject cubeObj = kvp.Value;

            bool shouldHide = false;

            switch (currentCameraDirection)
            {
                case CameraController.ViewDirection.Front:  // Z- 방향
                    shouldHide = cubeGridPos.z < playerGridPos.z;
                    break;
                case CameraController.ViewDirection.Back:   // Z+ 방향
                    shouldHide = cubeGridPos.z > playerGridPos.z;
                    break;
                case CameraController.ViewDirection.Left:   // X- 방향
                    shouldHide = cubeGridPos.x < playerGridPos.x;
                    break;
                case CameraController.ViewDirection.Right:  // X+ 방향
                    shouldHide = cubeGridPos.x > playerGridPos.x;
                    break;
                case CameraController.ViewDirection.Top:    // Y+ 방향
                    shouldHide = cubeGridPos.y > playerGridPos.y;
                    break;
                case CameraController.ViewDirection.Bottom: // Y- 방향
                    shouldHide = cubeGridPos.y < playerGridPos.y;
                    break;
            }

            cubeObj.SetActive(!shouldHide);
        }
    }
}