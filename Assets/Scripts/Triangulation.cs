using System.Collections.Generic;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

/// <summary>Utility that turns landmark range/bearing measurements
/// into an (x , z , heading) pose estimate.</summary>
public class Triangulation
{
    /// <param name="anchors">World‑space positions (+ measured ranges)</param>
    /// <param name="measurements">Agent‑centred range / bearing data</param>
    public Triangulation(IReadOnlyList<TriangulationAnchor> anchors,
                         IReadOnlyList<LandmarkMeasurement> measurements)
    {
        _anchors      = anchors;
        _measurements = measurements;
    }

    /// <returns>(x , z , heading°) or (0,0,NaN) if insufficient data.</returns>
    public Vector3 GetObservation()
    {
        Vector2 loc     = TryTriangulate();
        float   heading = EstimateHeading(loc);
        return new Vector3(loc[0], loc[1], heading);   // y‑slot carries z

    }

    // ────────────────────── internal implementation ────────────
    private readonly IReadOnlyList<TriangulationAnchor> _anchors;
    private readonly IReadOnlyList<LandmarkMeasurement> _measurements;

    private Vector2 TryTriangulate()
    {
        int n = _anchors.Count;
        if (n < 3) return Vector2.zero;

        // exactly three → one solve, otherwise avg. over all triples
        if (n == 3) return SolveSubset(_anchors);

        Vector2 sum = Vector2.zero;
        int     good = 0;
        var triple = new TriangulationAnchor[3];

        for (int i = 0; i < n - 2; ++i)
        for (int j = i + 1; j < n - 1; ++j)
        for (int k = j + 1; k < n; ++k)
        {
            triple[0] = _anchors[i];
            triple[1] = _anchors[j];
            triple[2] = _anchors[k];

            Vector2 est = SolveSubset(triple);
            if (float.IsInfinity(est.x)) continue;

            sum += est;
            ++good;
        }
        return good > 0 ? sum / good : Vector2.zero;
    }

    private Vector2 SolveSubset(IReadOnlyList<TriangulationAnchor> subset)
    {
        int m = subset.Count;
        if (m < 3) return Vector2.positiveInfinity;

        Vector2 p1 = subset[0].pos;
        float   r1 = subset[0].r;

        double a11 = 0, a12 = 0, b1 = 0;
        double a21 = 0, a22 = 0, b2 = 0;

        for (int i = 1; i < m; ++i)
        {
            Vector2 pi = subset[i].pos;  float ri = subset[i].r;

            double Ai = 2 * (pi.x - p1.x);
            double Bi = 2 * (pi.y - p1.y);
            double Ci = pi.x * pi.x - p1.x * p1.x
                      + pi.y * pi.y - p1.y * p1.y
                      + r1 * r1     - ri * ri;

            a11 += Ai * Ai; a12 += Ai * Bi; b1 += Ai * Ci;
            a21 += Bi * Ai; a22 += Bi * Bi; b2 += Bi * Ci;
        }

        double det = a11 * a22 - a12 * a21;
        if (Mathf.Abs((float)det) < 1e-6f) return Vector2.positiveInfinity;

        return new Vector2(
            (float)((b1 * a22 - a12 * b2) / det),
            (float)((a11 * b2 - b1 * a21) / det));
    }

    private float EstimateHeading(Vector2 agentPos)
    {
        int m = _measurements.Count;
        if (m < 2) return float.NaN;

        float sinSum = 0f, cosSum = 0f;

        for (int i = 0; i < m; ++i)
        {
            // anchor world bearing
            Vector2 lmPos = _anchors[i].pos;
            float phiGlobal = Mathf.Atan2(lmPos.y - agentPos.y,
                                          lmPos.x - agentPos.x);

            // θ_i = global − local
            float theta_i = phiGlobal - _measurements[i].bearing;
            theta_i = Mathf.Repeat(theta_i + Mathf.PI, 2f * Mathf.PI) - Mathf.PI;

            sinSum += Mathf.Sin(theta_i);
            cosSum += Mathf.Cos(theta_i);
        }
        float theta = Mathf.Atan2(sinSum / m, cosSum / m);
        if (theta < 0f) theta += 2f * Mathf.PI;
        return theta * Mathf.Rad2Deg;
    }
}
