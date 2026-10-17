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

    // 世界尺度 UV 的立方体（叶簇/树皮这类贴图要「每面密度恒定」的箱体）
    // 做法：以已验证的 Quad（法线 +Y）/ QuadZ（法线 +Z）为模板，逐面旋转+平移 —— 旋转不改变绕序，
    // 镜像面（-Y/-Z）单独反转三角顺序。UV = 面尺寸 / texMeters → 贴图密度恒为 texMeters。
    public static Mesh BoxUV(Vector3 size, float texMeters)
    {
        float hx = size.x * 0.5f, hy = size.y * 0.5f, hz = size.z * 0.5f;
        float ux = size.x / texMeters, uy = size.y / texMeters, uz = size.z / texMeters;
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
