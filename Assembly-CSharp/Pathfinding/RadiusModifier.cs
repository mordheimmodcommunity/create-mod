using Pathfinding.Util;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    [AddComponentMenu("Pathfinding/Modifiers/Radius Offset")]
    [HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_radius_modifier.php")]
    public class RadiusModifier : MonoModifier
    {
        [Flags]
        private enum TangentType
        {
            OuterRight = 0x1,
            InnerRightLeft = 0x2,
            InnerLeftRight = 0x4,
            OuterLeft = 0x8,
            Outer = 0x9,
            Inner = 0x6
        }

        public float radius = 1f;

        public float detail = 10f;

        private float[] radi = new float[10];

        private float[] a1 = new float[10];

        private float[] a2 = new float[10];

        private bool[] dir = new bool[10];

        public override int Order => 41;

        private bool CalculateCircleInner(Vector3 p1, Vector3 p2, float r1, float r2, out float a, out float sigma)
        {
            float magnitude = (p1 - p2).magnitude;
            if (r1 + r2 > magnitude)
            {
                a = 0f;
                sigma = 0f;
                return false;
            }
            a = (float)Math.Acos((r1 + r2) / magnitude);
            sigma = (float)Math.Atan2(p2.z - p1.z, p2.x - p1.x);
            return true;
        }

        private bool CalculateCircleOuter(Vector3 p1, Vector3 p2, float r1, float r2, out float a, out float sigma)
        {
            float magnitude = (p1 - p2).magnitude;
            if (Math.Abs(r1 - r2) > magnitude)
            {
                a = 0f;
                sigma = 0f;
                return false;
            }
            a = (float)Math.Acos((r1 - r2) / magnitude);
            sigma = (float)Math.Atan2(p2.z - p1.z, p2.x - p1.x);
            return true;
        }

        private TangentType CalculateTangentType(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            bool flag = VectorMath.RightOrColinearXZ(p1, p2, p3);
            bool flag2 = VectorMath.RightOrColinearXZ(p2, p3, p4);
            return (TangentType)(1 << (flag ? 2 : 0) + (flag2 ? 1 : 0));
        }

        private TangentType CalculateTangentTypeSimple(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            bool flag = VectorMath.RightOrColinearXZ(p1, p2, p3);
            bool flag2 = flag;
            return (TangentType)(1 << (flag2 ? 2 : 0) + (flag ? 1 : 0));
        }

        private void DrawCircleSegment(Vector3 p1, float rad, Color col, float start = 0f, float end = MathF.PI * 2f)
        {
            Vector3 start2 = new Vector3((float)Math.Cos(start), 0f, (float)Math.Sin(start)) * rad + p1;
            for (float num = start; num < end; num += MathF.PI / 100f)
            {
                Vector3 vector = new Vector3((float)Math.Cos(num), 0f, (float)Math.Sin(num)) * rad + p1;
                Debug.DrawLine(start2, vector, col);
                start2 = vector;
            }
            if ((double)end == Math.PI * 2.0)
            {
                Vector3 end2 = new Vector3((float)Math.Cos(start), 0f, (float)Math.Sin(start)) * rad + p1;
                Debug.DrawLine(start2, end2, col);
            }
        }

        public override void Apply(Path p)
        {
            List<Vector3> vectorPath = p.vectorPath;
            List<Vector3> list = Apply(vectorPath);
            if (list != vectorPath)
            {
                ListPool<Vector3>.Release(p.vectorPath);
                p.vectorPath = list;
            }
        }

        public List<Vector3> Apply(List<Vector3> vs)
        {
            if (vs == null || vs.Count < 3)
            {
                return vs;
            }
            if (radi.Length < vs.Count)
            {
                radi = new float[vs.Count];
                a1 = new float[vs.Count];
                this.a2 = new float[vs.Count];
                dir = new bool[vs.Count];
            }
            for (int i = 0; i < vs.Count; i++)
            {
                radi[i] = radius;
            }
            radi[0] = 0f;
            radi[vs.Count - 1] = 0f;
            int num = 0;
            for (int j = 0; j < vs.Count - 1; j++)
            {
                num++;
                if (num > 2 * vs.Count)
                {
                    Debug.LogWarning("Could not resolve radiuses, the path is too complex. Try reducing the base radius");
                    break;
                }
                TangentType tangentType = (j == 0) ? CalculateTangentTypeSimple(vs[j], vs[j + 1], vs[j + 2]) : ((j != vs.Count - 2) ? CalculateTangentType(vs[j - 1], vs[j], vs[j + 1], vs[j + 2]) : CalculateTangentTypeSimple(vs[j - 1], vs[j], vs[j + 1]));
                float a2;
                float sigma2;
                if ((tangentType & TangentType.Inner) != 0)
                {
                    if (!CalculateCircleInner(vs[j], vs[j + 1], radi[j], radi[j + 1], out float a, out float sigma))
                    {
                        float magnitude = (vs[j + 1] - vs[j]).magnitude;
                        radi[j] = magnitude * (radi[j] / (radi[j] + radi[j + 1]));
                        radi[j + 1] = magnitude - radi[j];
                        radi[j] *= 0.99f;
                        radi[j + 1] *= 0.99f;
                        j -= 2;
                    }
                    else if (tangentType == TangentType.InnerRightLeft)
                    {
                        this.a2[j] = sigma - a;
                        a1[j + 1] = sigma - a + MathF.PI;
                        dir[j] = true;
                    }
                    else
                    {
                        this.a2[j] = sigma + a;
                        a1[j + 1] = sigma + a + MathF.PI;
                        dir[j] = false;
                    }
                }
                else if (!CalculateCircleOuter(vs[j], vs[j + 1], radi[j], radi[j + 1], out a2, out sigma2))
                {
                    if (j == vs.Count - 2)
                    {
                        radi[j] = (vs[j + 1] - vs[j]).magnitude;
                        radi[j] *= 0.99f;
                        j--;
                    }
                    else
                    {
                        if (radi[j] > radi[j + 1])
                        {
                            radi[j + 1] = radi[j] - (vs[j + 1] - vs[j]).magnitude;
                        }
                        else
                        {
                            radi[j + 1] = radi[j] + (vs[j + 1] - vs[j]).magnitude;
                        }
                        radi[j + 1] *= 0.99f;
                    }
                    j--;
                }
                else if (tangentType == TangentType.OuterRight)
                {
                    this.a2[j] = sigma2 - a2;
                    a1[j + 1] = sigma2 - a2;
                    dir[j] = true;
                }
                else
                {
                    this.a2[j] = sigma2 + a2;
                    a1[j + 1] = sigma2 + a2;
                    dir[j] = false;
                }
            }
            List<Vector3> list = ListPool<Vector3>.Claim();
            list.Add(vs[0]);
            if (detail < 1f)
            {
                detail = 1f;
            }
            float num2 = MathF.PI * 2f / detail;
            for (int k = 1; k < vs.Count - 1; k++)
            {
                float num3 = a1[k];
                float num4 = this.a2[k];
                float d = radi[k];
                if (dir[k])
                {
                    if (num4 < num3)
                    {
                        num4 += MathF.PI * 2f;
                    }
                    for (float num5 = num3; num5 < num4; num5 += num2)
                    {
                        list.Add(new Vector3((float)Math.Cos(num5), 0f, (float)Math.Sin(num5)) * d + vs[k]);
                    }
                    continue;
                }
                if (num3 < num4)
                {
                    num3 += MathF.PI * 2f;
                }
                for (float num6 = num3; num6 > num4; num6 -= num2)
                {
                    list.Add(new Vector3((float)Math.Cos(num6), 0f, (float)Math.Sin(num6)) * d + vs[k]);
                }
            }
            list.Add(vs[vs.Count - 1]);
            return list;
        }
    }
}
