using UnityEngine;

// 实时河面：沿河道的高度场（波动方程 + 顺流平流 + 两岸/桥墩反射 + 雨滴扰动）
// 顶点高度每帧写回网格；法线由高度差解析求得（不调用 RecalculateNormals）。
// 提供 SampleHeight / SampleCurrent 给浮力物件（小船、漂木）用。
public class WaterSim : MonoBehaviour
{
    public static WaterSim I;

    public RiverDef River;
    public float Level = -1.95f;        // 静水面高度
    public float Current = 0.32f;       // 顺流速度（m/s，河道中心；缓流）
    public float WaveSpeed = 1.2f;      // 波速（m/s）
    public float Damping = 0.995f;
    const float StepX = 1f;             // 顺河采样间距
    const float StepT = 0.5f;           // 横河采样间距

    int _nx, _nt;
    float[] _h, _hPrev, _foam, _staticFoam;
    bool[] _block;
    Vector3[] _verts;
    Vector3[] _normals;
    Color[] _colors;
    Mesh _mesh;
    Weather _weather;
    float _t;

    float RZ(float x) => River.amp * Mathf.Sin((x + River.phase) / River.freq) + River.offset;

    public void Build(RiverDef river)
    {
        I = this;
        River = river;
        _nx = Mathf.CeilToInt((river.to - river.from) / StepX) + 1;
        _nt = Mathf.CeilToInt(river.width / StepT) + 1;
        int n = _nx * _nt;
        _h = new float[n];
        _hPrev = new float[n];
        _foam = new float[n];
        _staticFoam = new float[n];
        _block = new bool[n];
        _verts = new Vector3[n];
        _normals = new Vector3[n];
        _colors = new Color[n];

        // 桥墩遮蔽 + 桥墩/岸边泡沫
        float span = river.width + 1.6f;
        foreach (var b in river.bridges)
        {
            if (b.covered) continue;
            float bay = span / b.arches;
            for (int k = 1; k < b.arches; k++)
            {
                float tz = -span / 2f + k * bay;
                for (int i = 0; i < _nx; i++)
                {
                    float x = river.from + i * StepX;
                    if (Mathf.Abs(x - b.x) > b.width / 2f + 0.6f) continue;
                    for (int j = 0; j < _nt; j++)
                    {
                        float t = -river.width / 2f + j * StepT;
                        float d = Mathf.Sqrt((x - b.x) * (x - b.x) + (t - tz) * (t - tz));
                        int idx = i * _nt + j;
                        if (d < 1.3f) { _block[idx] = true; _h[idx] = 0f; }
                        if (d < 2.6f) _staticFoam[idx] = Mathf.Max(_staticFoam[idx], Mathf.Lerp(0.45f, 0f, (d - 1.3f) / 1.3f));
                    }
                }
            }
        }
        // 岸边泡沫
        for (int i = 0; i < _nx; i++)
            for (int j = 0; j < _nt; j++)
            {
                float t = -river.width / 2f + j * StepT;
                float edge = Mathf.Abs(t) / (river.width / 2f);
                _staticFoam[i * _nt + j] = Mathf.Max(_staticFoam[i * _nt + j],
                    Mathf.SmoothStep(0f, 1f, (edge - 0.88f) / 0.12f) * 0.22f);
            }

        // 初始扰动：几组正弦叠加，避免死水面
        var rnd = new System.Random(20261021);
        for (int i = 0; i < _nx; i++)
            for (int j = 0; j < _nt; j++)
            {
                float x = river.from + i * StepX;
                float t = -river.width / 2f + j * StepT;
                _h[i * _nt + j] = 0.008f * Mathf.Sin(x * 0.35f + t * 0.8f) + 0.005f * Mathf.Sin(x * 0.9f - t * 0.3f)
                                  + (float)(rnd.NextDouble() - 0.5) * 0.01f;
            }

        // 网格
        var uvs = new Vector2[n];
        for (int i = 0; i < _nx; i++)
            for (int j = 0; j < _nt; j++)
            {
                int idx = i * _nt + j;
                float x = river.from + i * StepX;
                float t = -river.width / 2f + j * StepT;
                _verts[idx] = new Vector3(x, Level, RZ(x) + t);
                uvs[idx] = new Vector2(x / 4f, t / 4f);
                _colors[idx] = Color.black;
            }
        var tris = new int[(_nx - 1) * (_nt - 1) * 6];
        int o = 0;
        for (int i = 0; i < _nx - 1; i++)
            for (int j = 0; j < _nt - 1; j++)
            {
                int a = i * _nt + j, b = a + 1, c = a + _nt, d = c + 1;
                tris[o++] = a; tris[o++] = d; tris[o++] = c;
                tris[o++] = a; tris[o++] = b; tris[o++] = d;
            }
        _mesh = new Mesh { name = "RiverSim" };
        _mesh.MarkDynamic();
        _mesh.vertices = _verts;
        _mesh.uv = uvs;
        _mesh.triangles = tris;
        _mesh.colors = _colors;
        _mesh.RecalculateNormals();
        _mesh.bounds = new Bounds(new Vector3((river.from + river.to) / 2f, Level, 0f),
            new Vector3(river.to - river.from + 4f, 4f, 60f));
        gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
        var mr = gameObject.AddComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Campus/Water"));
        mat.mainTexture = MatLib.Tex("water", false, true);
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        _weather = FindObjectOfType<Weather>();
    }

    void Update()
    {
        if (_mesh == null) return;
        if (_weather == null) _weather = FindObjectOfType<Weather>();
        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
        Simulate(dt);
        UpdateMesh();
    }

    void Simulate(float dt)
    {
        // 波动方程（显式）+ 顺流平流（上风差分）+ 阻尼
        float c2 = WaveSpeed * WaveSpeed * dt * dt / (StepT * StepT);
        float u = Current * dt / StepX;
        float rain = _weather != null ? _weather.RainAmt : 0f;
        for (int i = 0; i < _nx; i++)
        {
            bool wrapPrev = i == 0, wrapNext = i == _nx - 1;
            for (int j = 0; j < _nt; j++)
            {
                int idx = i * _nt + j;
                float cur = _h[idx];
                if (_block[idx]) { _hPrev[idx] = 0f; _h[idx] = 0f; continue; }
                float hL = _h[(i > 0 ? i - 1 : _nx - 2) * _nt + j];
                float hR = _h[(i < _nx - 1 ? i + 1 : 1) * _nt + j];
                float hU = _h[i * _nt + (j > 0 ? j - 1 : j + 1)];
                float hD = _h[i * _nt + (j < _nt - 1 ? j + 1 : j - 1)];
                float lap = hL + hR + hU + hD - 4f * cur;
                float next = 2f * cur - _hPrev[idx] + c2 * lap;
                next += u * (hL - cur);              // 顺流（i-1 为上风）
                next *= Damping;

                // 上游来波：河源持续小扰动
                if (i < 6 && (j & 3) == 0)
                    next += Mathf.Sin(_t * 2.3f + j * 0.7f) * 0.00035f;
                // 全域细浪（风的扰动）：稀疏随机脉冲，保证整条河都在动
                int hsh = (i * 92821 + j * 68917 + (int)(_t * 60f) * 31337) & 1023;
                if (hsh < 4) next += ((hsh & 1) == 0 ? 1f : -1f) * 0.0011f;
                // 雨滴扰动
                if (rain > 0.01f && ((i * 7 + j * 13 + (int)(_t * 37f)) % 97) == 0)
                    next -= 0.004f * rain;

                _hPrev[idx] = cur;
                _h[idx] = Mathf.Clamp(next, -0.035f, 0.035f);
            }
        }
        _t += dt;
    }

    void UpdateMesh()
    {
        float inv2x = 1f / (2f * StepX), inv2t = 1f / (2f * StepT);
        for (int i = 0; i < _nx; i++)
            for (int j = 0; j < _nt; j++)
            {
                int idx = i * _nt + j;
                float h = _h[idx];
                float x = River.from + i * StepX;
                float t = -River.width / 2f + j * StepT;
                _verts[idx].y = Level + h;
                float hL = _h[(i > 0 ? i - 1 : _nx - 2) * _nt + j];
                float hR = _h[(i < _nx - 1 ? i + 1 : 1) * _nt + j];
                float hU = _h[i * _nt + (j > 0 ? j - 1 : j + 1)];
                float hD = _h[i * _nt + (j < _nt - 1 ? j + 1 : j - 1)];
                _normals[idx] = new Vector3(-(hR - hL) * inv2x, 1f, -(hD - hU) * inv2t).normalized;
                float dyn = Mathf.Clamp01(Mathf.Abs(h - _hPrev[idx]) * 3f);
                float foam = Mathf.Clamp01(_staticFoam[idx] + dyn + Mathf.Abs(h) * 0.18f);
                _colors[idx] = new Color(foam, 0f, 0f, 1f);
            }
        _mesh.vertices = _verts;
        _mesh.normals = _normals;
        _mesh.colors = _colors;
    }

    // 水面高度（世界坐标；河道外返回静水面）
    public float SampleHeight(float x, float z)
    {
        if (River == null) return Level;
        float fx = (x - River.from) / StepX;
        float t = z - RZ(x);
        float fz = (t + River.width / 2f) / StepT;
        if (fx < 0 || fx >= _nx - 1 || fz < 0 || fz >= _nt - 1) return Level;
        int i = (int)fx, j = (int)fz;
        float ax = fx - i, az = fz - j;
        float h0 = Mathf.Lerp(_h[i * _nt + j], _h[(i + 1) * _nt + j], ax);
        float h1 = Mathf.Lerp(_h[i * _nt + j + 1], _h[(i + 1) * _nt + j + 1], ax);
        return Level + Mathf.Lerp(h0, h1, az);
    }

    // 水流速度（沿河道切线；近岸略慢）
    public Vector3 SampleCurrent(float x, float z)
    {
        if (River == null) return Vector3.zero;
        float slope = River.amp / River.freq * Mathf.Cos((x + River.phase) / River.freq);
        var dir = new Vector3(1f, 0f, slope).normalized;
        float t = Mathf.Abs(z - RZ(x)) / (River.width / 2f);
        float speed = Current * Mathf.Clamp01(1f - 0.55f * t * t);
        return dir * speed;
    }
}
