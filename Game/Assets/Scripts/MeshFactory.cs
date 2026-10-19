using System;
using System.Collections.Generic;
using UnityEngine;

// 程序化网格工具（对应 web 版 planeGeometry / coneGeometry / ShapeGeometry / 天空内翻球）
// 缠绕方向说明：Unity 左手系 + 顺时针=正面。以下三角形顺序均已按 cross(v1-v0, v2-v0) 推导过。
public static class MeshFactory
{
    // 水平面片：XZ 平面、法线 +Y；UV 按 uScale/vScale 平铺（地面 repeat 等）
    public static Mesh Quad(float w, float d, float uScale = 1, float vScale = 1)
    {
        var m = new Mesh();
        m.vertices = new[]
        {
            new Vector3(-w / 2, 0, -d / 2),
            new Vector3( w / 2, 0, -d / 2),
            new Vector3( w / 2, 0,  d / 2),
            new Vector3(-w / 2, 0,  d / 2),
        };
        m.uv = new[]
        {
            new Vector2(0, 0), new Vector2(uScale, 0),
            new Vector2(uScale, vScale), new Vector2(0, vScale),
        };
        m.triangles = new[] { 0, 2, 1, 0, 3, 2 };   // 法线朝 +Y（推导见注释文件头）
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    // 竖直面片：XY 平面、法线 +Z（招牌/大门/精灵用；需要朝 -Z 时 rotation Y = π）
    public static Mesh QuadZ(float w, float h)
    {
        var m = new Mesh();
        m.vertices = new[]
        {
            new Vector3(-w / 2, -h / 2, 0),
            new Vector3( w / 2, -h / 2, 0),
            new Vector3( w / 2,  h / 2, 0),
            new Vector3(-w / 2,  h / 2, 0),
        };
        m.uv = new[]
        {
            // U 轴镜像：Unity 左手系下面向 +Z 的观察者看到的世界 +X 在屏幕左侧，
            // 因此 u 必须沿 -X 递增（否则招牌/精灵文字整体镜像）
            new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1),
        };
        m.triangles = new[] { 0, 1, 2, 0, 2, 3 };   // 法线朝 +Z
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    // 世界尺度 UV 的立方体（叶簇/树皮/建筑立面这类贴图要「每面密度恒定」的箱体）
    // 做法：以已验证的 Quad（法线 +Y）/ QuadZ（法线 +Z）为模板，逐面旋转+平移 —— 旋转不改变绕序，
    // 镜像面（-Y/-Z）单独反转三角顺序。UV = 面尺寸 / 贴图覆盖米数 → 每面像素密度恒定。
    // texU/texV：贴图在 U/V 方向上覆盖的世界米数（方形贴图传同一个值）
    public static Mesh BoxUV(Vector3 size, float texU, float texV)
    {
        float hx = size.x * 0.5f, hy = size.y * 0.5f, hz = size.z * 0.5f;
        float ux = size.x / texU, uy = size.y / texV, uz = size.z / texU;
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        void Face(Vector3[] v, float uw, float uh, bool quadOrder)
        {
            int i = verts.Count;
            verts.AddRange(v);
            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(uw, 0)); uvs.Add(new Vector2(uw, uh)); uvs.Add(new Vector2(0, uh));
            if (quadOrder) { tris.Add(i); tris.Add(i + 2); tris.Add(i + 1); tris.Add(i); tris.Add(i + 3); tris.Add(i + 2); }
            else { tris.Add(i); tris.Add(i + 1); tris.Add(i + 2); tris.Add(i); tris.Add(i + 2); tris.Add(i + 3); }
        }

        // 顶 / 底（Quad 模板：0,2,1 / 0,3,2；底面镜像后反转）
        Face(new[] { new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz) }, ux, uz, true);
        Face(new[] { new Vector3(-hx, -hy, -hz), new Vector3(hx, -hy, -hz), new Vector3(hx, -hy, hz), new Vector3(-hx, -hy, hz) }, ux, uz, false);
        // 前 / 后（QuadZ 模板：0,1,2 / 0,2,3；后面镜像后反转）
        Face(new[] { new Vector3(-hx, -hy, hz), new Vector3(hx, -hy, hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz) }, ux, uy, false);
        Face(new[] { new Vector3(-hx, -hy, -hz), new Vector3(hx, -hy, -hz), new Vector3(hx, hy, -hz), new Vector3(-hx, hy, -hz) }, ux, uy, true);
        // 右 / 左（QuadZ 绕 Y 轴 ±90°，保绕序；宽度沿 z）
        Face(new[] { new Vector3(hx, -hy, hz), new Vector3(hx, -hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz) }, uz, uy, false);
        Face(new[] { new Vector3(-hx, -hy, -hz), new Vector3(-hx, -hy, hz), new Vector3(-hx, hy, hz), new Vector3(-hx, hy, -hz) }, uz, uy, false);

        var m = new Mesh();
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    // 方形贴图（树冠/树皮）的简写
    public static Mesh BoxUV(Vector3 size, float texMeters) => BoxUV(size, texMeters, texMeters);

    // 圆锥（松树/柏树用；flat shading = 每面独立顶点）
    // texMeters > 0 时写 UV（u 沿周长展开、v 沿高度），用于叶片类材质
    public static Mesh Cone(float radius, float height, int sides = 6, bool withBottom = true, float texMeters = 0f)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var uvs = new List<Vector2>();
        var apex = new Vector3(0, height, 0);
        float circumference = 2f * Mathf.PI * radius;
        for (int i = 0; i < sides; i++)
        {
            float a0 = i * Mathf.PI * 2f / sides;
            float a1 = (i + 1) * Mathf.PI * 2f / sides;
            var p0 = new Vector3(Mathf.Sin(a0) * radius, 0, Mathf.Cos(a0) * radius);
            var p1 = new Vector3(Mathf.Sin(a1) * radius, 0, Mathf.Cos(a1) * radius);
            int b = verts.Count;
            // 侧面（外法线）
            verts.Add(apex); verts.Add(p0); verts.Add(p1);
            if (texMeters > 0f)
            {
                float v0 = a0 / (Mathf.PI * 2f) * circumference / texMeters;
                float v1 = a1 / (Mathf.PI * 2f) * circumference / texMeters;
                uvs.Add(new Vector2((v0 + v1) * 0.5f, height / texMeters));
                uvs.Add(new Vector2(v0, 0f));
                uvs.Add(new Vector2(v1, 0f));
            }
            else { uvs.Add(Vector2.zero); uvs.Add(Vector2.zero); uvs.Add(Vector2.zero); }
            tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
            if (withBottom)
            {
                // 底面（法线朝下）
                verts.Add(Vector3.zero); verts.Add(p1); verts.Add(p0);
                uvs.Add(Vector2.zero); uvs.Add(Vector2.zero); uvs.Add(Vector2.zero);
                tris.Add(b + 3); tris.Add(b + 4); tris.Add(b + 5);
            }
        }
        var m = new Mesh();
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    // 三角形端封（山墙）：XY 平面、双面（两面缠绕都写入，等价 web 的 DoubleSide）——勿加回单面
    public static Mesh Triangle(float baseW, float height)
    {
        var m = new Mesh();
        m.vertices = new[]
        {
            new Vector3(-baseW / 2, 0, 0),
            new Vector3( baseW / 2, 0, 0),
            new Vector3(0, height, 0),
            new Vector3(-baseW / 2, 0, 0),
            new Vector3( baseW / 2, 0, 0),
            new Vector3(0, height, 0),
        };
        m.uv = new[]
        {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 1),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 1),
        };
        m.triangles = new[] { 0, 1, 2, 3, 5, 4 };   // 正面 + 背面各一组
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    // 世界对齐 UV 的地面片（广场/步道）：多块拼在一起石纹连续
    public static Mesh PlazaQuad(float w, float d, float period, float x, float z, float ry)
    {
        float c = Mathf.Cos(ry), sn = Mathf.Sin(ry);
        void W(float lx, float lz, out float wx, out float wz)
        {
            wx = x + lx * c + lz * sn;
            wz = z - lx * sn + lz * c;
        }
        float x0, z0, x1, z1, x2, z2, x3, z3;
        W(-w / 2, -d / 2, out x0, out z0);
        W(w / 2, -d / 2, out x1, out z1);
        W(w / 2, d / 2, out x2, out z2);
        W(-w / 2, d / 2, out x3, out z3);
        var m = new Mesh();
        m.vertices = new[]
        {
            new Vector3(-w / 2, 0, -d / 2), new Vector3(w / 2, 0, -d / 2),
            new Vector3(w / 2, 0, d / 2), new Vector3(-w / 2, 0, d / 2),
        };
        m.uv = new[]
        {
            new Vector2(x0 / period, z0 / period), new Vector2(x1 / period, z1 / period),
            new Vector2(x2 / period, z2 / period), new Vector2(x3 / period, z3 / period),
        };
        m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    // ── 沿 x 方向的地形条带 / 河岸墙 / 贴地条带（小镇河道地形用）──
    // 条带：zA(x) 为北边界、zB(x) 为南边界；UV = 世界坐标 / period（与整块地面同一平铺基准，接缝连续）
    public static Mesh Strip(Func<float, float> zA, Func<float, float> zB, float x0, float x1, int steps, float period,
        float y = 0f)
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        for (int i = 0; i <= steps; i++)
        {
            float x = Mathf.Lerp(x0, x1, i / (float)steps);
            float za = zA(x), zb = zB(x);
            verts.Add(new Vector3(x, y, za));
            verts.Add(new Vector3(x, y, zb));
            uvs.Add(new Vector2(x / period, za / period));
            uvs.Add(new Vector2(x / period, zb / period));
        }
        for (int i = 0; i < steps; i++)
        {
            int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
            tris.Add(a); tris.Add(d); tris.Add(c);
            tris.Add(a); tris.Add(b); tris.Add(d);
        }
        var m = new Mesh();
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    // 竖直护岸墙：沿 x 方向的折线（z = zAt(x)），从 yTop 到 yBottom；UV 的 v 以墙底为 0（贴图底部勒脚贴水面）
    // 双面时给背面单独一份顶点（否则正反绕序共用顶点 → 法线互相抵消变黑）
    public static Mesh RibbonWall(Func<float, float> zAt, float x0, float x1, int steps,
        float yTop, float yBottom, float period, bool doubleSided = true)
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        float h = yTop - yBottom;
        void Build(bool flip)
        {
            int baseIdx = verts.Count;
            for (int i = 0; i <= steps; i++)
            {
                float x = Mathf.Lerp(x0, x1, i / (float)steps);
                float z = zAt(x);
                verts.Add(new Vector3(x, yTop, z));
                verts.Add(new Vector3(x, yBottom, z));
                uvs.Add(new Vector2(x / period, h / period));
                uvs.Add(new Vector2(x / period, 0f));
            }
            for (int i = 0; i < steps; i++)
            {
                int a = baseIdx + i * 2, b = a + 1, c = a + 2, d = a + 3;   // a=顶, b=底
                if (!flip)
                {
                    tris.Add(a); tris.Add(b); tris.Add(d);           // 朝 +z
                    tris.Add(a); tris.Add(d); tris.Add(c);
                }
                else
                {
                    tris.Add(a); tris.Add(d); tris.Add(b);           // 朝 -z
                    tris.Add(a); tris.Add(c); tris.Add(d);
                }
            }
        }
        Build(false);
        if (doubleSided) Build(true);
        var m = new Mesh();
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    // 椭圆水面（池塘）：UV = 局部坐标 / period（与河水同一密度）；法线 +Y
    public static Mesh Disc(float rx, float rz, int segments, float period)
    {
        var verts = new List<Vector3> { new Vector3(0, 0, 0) };
        var uvs = new List<Vector2> { new Vector2(0, 0) };
        var tris = new List<int>();
        for (int i = 0; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            float x = Mathf.Cos(a) * rx, z = Mathf.Sin(a) * rz;
            verts.Add(new Vector3(x, 0, z));
            uvs.Add(new Vector2(x / period, z / period));
        }
        for (int i = 1; i <= segments; i++)
        {
            tris.Add(0); tris.Add(i + 1); tris.Add(i);
        }
        var m = new Mesh();
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    // 椭圆环（池塘石沿）：内外两圈；UV = 世界弧长/period 近似为局部坐标/period
    public static Mesh Ring(float rxIn, float rzIn, float rxOut, float rzOut, int segments, float period)
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        for (int i = 0; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            verts.Add(new Vector3(ca * rxIn, 0, sa * rzIn));
            verts.Add(new Vector3(ca * rxOut, 0, sa * rzOut));
            uvs.Add(new Vector2(ca * rxIn / period, sa * rzIn / period));
            uvs.Add(new Vector2(ca * rxOut / period, sa * rzOut / period));
        }
        for (int i = 0; i < segments; i++)
        {
            int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
            tris.Add(a); tris.Add(c); tris.Add(d);
            tris.Add(a); tris.Add(d); tris.Add(b);
        }
        var m = new Mesh();
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    // 石拱桥面：带 arches 个半椭圆拱洞的竖面（XY 平面；正/反两面各一份顶点，法线不互相抵消）
    // span/height = 面尺寸；holeW/holeH = 单拱洞宽/高；texW = 石纹贴图覆盖米数（UV 按世界比例）
    public static Mesh ArchFace(float span, float height, int arches, float holeW, float holeH, float texW)
    {
        int cols = Mathf.Max(12, arches * 28);
        float colW = span / cols;
        float bayW = span / arches;
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        float HoleTop(float xLocal)
        {
            float bayU = Mathf.Repeat(xLocal + span / 2f, bayW) - bayW / 2f;
            float r = holeW / 2f;
            if (Mathf.Abs(bayU) >= r) return 0f;
            return holeH * Mathf.Sqrt(1f - (bayU / r) * (bayU / r));
        }

        void Build(bool flip)
        {
            for (int i = 0; i < cols; i++)
            {
                float x0 = -span / 2f + i * colW, x1 = x0 + colW;
                float yb = -height / 2f + HoleTop(x0 + colW * 0.5f);
                float yt = height / 2f;
                if (yt - yb < 0.02f) continue;
                int b = verts.Count;
                verts.Add(new Vector3(x0, yb, 0)); verts.Add(new Vector3(x1, yb, 0));
                verts.Add(new Vector3(x1, yt, 0)); verts.Add(new Vector3(x0, yt, 0));
                uvs.Add(new Vector2((x0 + span / 2f) / texW, (yb + height / 2f) / texW));
                uvs.Add(new Vector2((x1 + span / 2f) / texW, (yb + height / 2f) / texW));
                uvs.Add(new Vector2((x1 + span / 2f) / texW, height / texW));
                uvs.Add(new Vector2((x0 + span / 2f) / texW, height / texW));
                if (!flip)
                {
                    tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                    tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
                }
                else
                {
                    tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                    tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
                }
            }
        }
        Build(false); Build(true);

        var m = new Mesh();
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    // 四棱台（上窄下宽）：钟楼塔身等；UV 按边长/高度世界比例
    public static Mesh TaperedBox(float bw, float bd, float tw, float td, float height, float texMeters)
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u, float v)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(u, 0)); uvs.Add(new Vector2(u, v)); uvs.Add(new Vector2(0, v));
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }
        float hbw = bw / 2f, hbd = bd / 2f, htw = tw / 2f, htd = td / 2f, h = height;
        Quad(new Vector3(-hbw, 0, hbd), new Vector3(hbw, 0, hbd), new Vector3(htw, h, htd), new Vector3(-htw, h, htd), bw / texMeters, h / texMeters);
        Quad(new Vector3(hbw, 0, -hbd), new Vector3(-hbw, 0, -hbd), new Vector3(-htw, h, -htd), new Vector3(htw, h, -htd), bw / texMeters, h / texMeters);
        Quad(new Vector3(hbw, 0, hbd), new Vector3(hbw, 0, -hbd), new Vector3(htw, h, -htd), new Vector3(htw, h, htd), bd / texMeters, h / texMeters);
        Quad(new Vector3(-hbw, 0, -hbd), new Vector3(-hbw, 0, hbd), new Vector3(-htw, h, htd), new Vector3(-htw, h, -htd), bd / texMeters, h / texMeters);
        // 顶面（法线 +Y）
        int top = verts.Count;
        verts.Add(new Vector3(-htw, h, -htd)); verts.Add(new Vector3(htw, h, -htd));
        verts.Add(new Vector3(htw, h, htd)); verts.Add(new Vector3(-htw, h, htd));
        uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(tw / texMeters, 0));
        uvs.Add(new Vector2(tw / texMeters, td / texMeters)); uvs.Add(new Vector2(0, td / texMeters));
        tris.Add(top); tris.Add(top + 2); tris.Add(top + 1);
        tris.Add(top); tris.Add(top + 3); tris.Add(top + 2);
        var mesh = new Mesh();
        mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }

    // 天空穹顶：球面、三角形朝内（供内看）——经纬网格；UV: u=phi/(2π)、v=1 在天顶（与贴图方向一致）
    public static Mesh Dome(float radius, int sectors = 24, int rings = 16)
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        for (int i = 0; i <= rings; i++)
        {
            float theta = i * Mathf.PI / rings;                 // 0 = 天顶
            float sinT = Mathf.Sin(theta), cosT = Mathf.Cos(theta);
            for (int j = 0; j <= sectors; j++)
            {
                float phi = j * Mathf.PI * 2f / sectors;
                verts.Add(new Vector3(sinT * Mathf.Cos(phi), cosT, sinT * Mathf.Sin(phi)) * radius);
                uvs.Add(new Vector2((float)j / sectors, 1f - (float)i / rings));
            }
        }
        int row = sectors + 1;
        for (int i = 0; i < rings; i++)
        {
            for (int j = 0; j < sectors; j++)
            {
                int a = i * row + j, b = (i + 1) * row + j, c = (i + 1) * row + j + 1, d = i * row + j + 1;
                tris.Add(a); tris.Add(b); tris.Add(c);          // 朝内缠绕（推导见注释）
                tris.Add(a); tris.Add(c); tris.Add(d);
            }
        }
        var m = new Mesh();
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
}
