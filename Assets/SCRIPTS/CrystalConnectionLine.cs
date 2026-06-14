using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CrystalConnectionLine : MonoBehaviour
{
    [Header("Connection")]
    public CrystalInteract stoneA;
    public CrystalInteract stoneB;
    public float heightOffset = 0.12f;

    [Header("Line Look")]
    public bool revealOnlyWhenEndpointColored = true;
    public float lineWidth = 0.08f;
    public Color waitingColor = new Color(0.8f, 0.9f, 1f, 0.45f);
    public Color validColor = new Color(0f, 1f, 0.18f, 1f);
    public Color conflictColor = new Color(1f, 0f, 0f, 1f);
    public float conflictBaseAlpha = 0.35f;

    [Header("Conflict Dashes")]
    public int dashCount = 7;
    [Range(0.1f, 0.9f)] public float dashFill = 0.55f;

    private LineRenderer solidLine;
    private readonly List<LineRenderer> dashLines = new List<LineRenderer>();
    private Material lineMaterial;

    private void OnEnable()
    {
        CreateVisuals();
        SubscribeToStones();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeFromStones();
    }

    private void OnValidate()
    {
        dashCount = Mathf.Max(1, dashCount);
        lineWidth = Mathf.Max(0.01f, lineWidth);
        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void CreateVisuals()
    {
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            lineMaterial = new Material(shader);
            lineMaterial.name = "Crystal Connection Line";
        }

        if (solidLine == null)
        {
            Transform existing = transform.Find("SolidLine");
            GameObject lineObject = existing != null ? existing.gameObject : new GameObject("SolidLine");
            lineObject.transform.SetParent(transform, false);
            solidLine = lineObject.GetComponent<LineRenderer>();
            if (solidLine == null)
                solidLine = lineObject.AddComponent<LineRenderer>();
        }

        ConfigureLineRenderer(solidLine);
        DestroyOldEndpointMarker("Endpoint_A");
        DestroyOldEndpointMarker("Endpoint_B");
    }

    private void SubscribeToStones()
    {
        if (stoneA != null)
            stoneA.ColorChanged += OnStoneColorChanged;

        if (stoneB != null)
            stoneB.ColorChanged += OnStoneColorChanged;
    }

    private void UnsubscribeFromStones()
    {
        if (stoneA != null)
            stoneA.ColorChanged -= OnStoneColorChanged;

        if (stoneB != null)
            stoneB.ColorChanged -= OnStoneColorChanged;
    }

    private void OnStoneColorChanged(CrystalInteract crystal)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (stoneA == null || stoneB == null)
            return;

        CreateVisuals();

        if (revealOnlyWhenEndpointColored && !stoneA.HasColor && !stoneB.HasColor)
        {
            HideLine();
            return;
        }

        Vector3 start = stoneA.LineAnchorPosition + Vector3.up * heightOffset;
        Vector3 end = stoneB.LineAnchorPosition + Vector3.up * heightOffset;
        bool hasConflict = stoneA.HasColor && stoneB.HasColor && stoneA.SelectedColorIndex == stoneB.SelectedColorIndex;
        Color lineColor = GetLineColor(hasConflict);

        if (hasConflict)
        {
            HideDashLines();
            solidLine.enabled = true;
            solidLine.positionCount = 2;
            solidLine.SetPosition(0, start);
            solidLine.SetPosition(1, end);
            solidLine.startWidth = lineWidth * 1.35f;
            solidLine.endWidth = lineWidth * 1.35f;
            ApplyColor(solidLine, WithAlpha(lineColor, conflictBaseAlpha));
            DrawDashedLine(start, end, lineColor);
        }
        else
        {
            HideDashLines();
            solidLine.enabled = true;
            solidLine.positionCount = 2;
            solidLine.SetPosition(0, start);
            solidLine.SetPosition(1, end);
            solidLine.startWidth = lineWidth;
            solidLine.endWidth = lineWidth;
            ApplyColor(solidLine, lineColor);
        }

    }

    private void HideLine()
    {
        if (solidLine != null)
            solidLine.enabled = false;

        HideDashLines();
    }

    private Color GetLineColor(bool hasConflict)
    {
        if (hasConflict)
            return conflictColor;

        if (stoneA.HasColor && stoneB.HasColor)
            return validColor;

        return waitingColor;
    }

    private void DrawDashedLine(Vector3 start, Vector3 end, Color color)
    {
        int neededDashes = Mathf.Max(1, dashCount);

        while (dashLines.Count < neededDashes)
        {
            GameObject dashObject = new GameObject("ConflictDash");
            dashObject.transform.SetParent(transform, false);
            LineRenderer dash = dashObject.AddComponent<LineRenderer>();
            ConfigureLineRenderer(dash);
            dashLines.Add(dash);
        }

        for (int i = 0; i < dashLines.Count; i++)
        {
            LineRenderer dash = dashLines[i];
            bool active = i < neededDashes;
            dash.enabled = active;

            if (!active)
                continue;

            float segmentStart = i / (float)neededDashes;
            float segmentEnd = Mathf.Min(1f, segmentStart + dashFill / neededDashes);

            dash.positionCount = 2;
            dash.SetPosition(0, Vector3.Lerp(start, end, segmentStart));
            dash.SetPosition(1, Vector3.Lerp(start, end, segmentEnd));
            dash.startWidth = lineWidth * 1.6f;
            dash.endWidth = lineWidth * 1.6f;
            ApplyColor(dash, color);
        }
    }

    private void HideDashLines()
    {
        foreach (LineRenderer dash in dashLines)
        {
            if (dash != null)
                dash.enabled = false;
        }
    }

    private void ConfigureLineRenderer(LineRenderer line)
    {
        line.useWorldSpace = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.positionCount = 2;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.sharedMaterial = lineMaterial;
    }

    private void DestroyOldEndpointMarker(string markerName)
    {
        Transform existing = transform.Find(markerName);
        if (existing == null)
            return;

        if (Application.isPlaying)
            Destroy(existing.gameObject);
        else
            DestroyImmediate(existing.gameObject);
    }

    private void ApplyColor(LineRenderer line, Color color)
    {
        line.startColor = color;
        line.endColor = color;

    }

    private Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
