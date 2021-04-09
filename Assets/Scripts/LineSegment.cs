using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LineSegment : MonoBehaviour
{
    LineRenderer lineRenderer;
    public MusicPiece piece;
    public LineSegment next;
    public Vector3 end;

    public Material noteOnMaterial;
    public Material noteOffMaterial;

    public bool attackDone = false;
    public bool releaseDone = false;
    // Start is called before the first frame update
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public void UpdateMaterial() {
        if(piece.isNote) {
            lineRenderer.material = noteOnMaterial;
        } else {
            lineRenderer.material = noteOffMaterial;
        }
        if(piece.isNote && piece.note != 0) {
            lineRenderer.widthMultiplier = 1;
        } else {
            lineRenderer.widthMultiplier = 0.5f;
        }
    }

    public void MakeArc(float startAngle, float endAngle, float length, float fac = 0) {
        float angDiff = endAngle - startAngle;
        while(angDiff > Mathf.PI) angDiff -= Mathf.PI * 2;
        while(angDiff < -Mathf.PI) angDiff += Mathf.PI * 2;
        endAngle = startAngle + angDiff;
        startAngle = Mathf.Lerp(startAngle, endAngle, fac);
        angDiff = (1 - fac) * angDiff;
        length = (1 - fac) * length;
        if(Mathf.Abs(angDiff) < 0.0001f) {
            MakeLine(startAngle, length);
            return;
        }
        float radius = length / angDiff;
        float circleAngle = startAngle - Mathf.PI / 2;
        Vector3 center = new Vector3(-Mathf.Cos(circleAngle)*radius, -Mathf.Sin(circleAngle)*radius, 0);
        Vector3[] points = new Vector3[16];
        for(int i = 0; i < points.Length; i++) {
            float angle = circleAngle + angDiff * ((float)i / (float)(points.Length-1));
            points[i] = center + new Vector3(Mathf.Cos(angle)*radius, Mathf.Sin(angle)*radius);
        }
        end = points[points.Length-1];
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
    }

    void MakeLine(float angle, float length) {
        Vector3[] points = new Vector3[] {
            new Vector3(0, 0, 0),
            new Vector3(Mathf.Cos(angle)*length, Mathf.Sin(angle)*length)
        };
        lineRenderer.positionCount = 2;
        lineRenderer.SetPositions(points);
        end = points[1];
    }

    // Update is called once per frame
    void Update()
    {
        if(releaseDone) lineRenderer.material = noteOffMaterial;
    }
}
