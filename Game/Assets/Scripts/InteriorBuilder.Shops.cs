using UnityEngine;

// 小镇公共室内（单层）：图书馆 / 餐厅 / 超市
// 参考现代公共空间：
//   图书馆 —— 暖木、木格栅吊顶、整墙书墙、长桌 + 休闲阅读角、绿植
//   餐厅   —— 卡座、开放厨房/吧台、吊灯、酒架、暖色木作
//   超市   —— 生鲜/烘焙/冷藏/货架/收银分区，成列货架与清晰动线
public static partial class InteriorBuilder
{
    const float LIB_X = 1220f, REST_X = 1300f, MART_X = 1380f;

    // ── 通用小件 ──────────────────────────────────────────

    // 沿 X 的靠墙书架：face=+1 书脊朝 +z（靠北墙），face=-1 朝 -z（靠南墙）
    static void ShelfWallX(Transform p, Interior it, float z, float x0, float x1, int face,
                           Material frame, Material[] books, float h = 2.10f, float productScale = 1f)
    {
        const float d = 0.50f;
        float len = x1 - x0, cx = (x0 + x1) * 0.5f;
        var trim = Darker(frame, 0.76f);
        // 背板/顶板/底板/隔板全部倒角：卡通描边把每层格子分开
        PartC(p, new Vector3(cx, h * 0.5f, z - face * (d * 0.5f - 0.02f)),
              new Vector3(len, h, 0.04f), frame, 0.010f, 1f);
        PartC(p, new Vector3(x0 + 0.03f, 0.06f, z), new Vector3(0.06f, 0.12f, d), frame, 0.010f, 0.7f);
        PartC(p, new Vector3(x1 - 0.03f, 0.06f, z), new Vector3(0.06f, 0.12f, d), frame, 0.010f, 0.7f);
        PartC(p, new Vector3(cx, h - 0.03f, z), new Vector3(len, 0.06f, d), frame, 0.012f, 1f);
        PartC(p, new Vector3(cx, 0.06f, z), new Vector3(len, 0.12f, d), frame, 0.012f, 1f);
        // 顶部压条：比顶板外凸 1cm，书架顶沿在卡通渲染里有一条独立亮线
        Part(p, new Vector3(cx, h + 0.005f, z), new Vector3(len + 0.02f, 0.03f, d + 0.02f), trim, 1f);
        for (int r = 1; r <= 3; r++)
            PartC(p, new Vector3(cx, 0.12f + r * 0.48f, z),
                  new Vector3(len - 0.12f, 0.04f, d - 0.06f), trim, 0.008f, 1f);

        float step = 0.17f * (productScale < 0.9f ? 0.76f : 1f);
        int n = Mathf.FloorToInt((len - 0.24f) / step);
        for (int r = 0; r < 4; r++)
        {
            float y = r == 0 ? 0.12f : 0.12f + r * 0.48f + 0.02f;
            for (int i = 0; i < n; i++)
            {
                if ((i + r * 5) % 11 == 7) continue;
                float bw = (0.10f + ((i * 7 + r * 3) % 4) * 0.014f) * productScale;
                float bh = (0.28f + ((i * 5 + r) % 4) * 0.022f) * productScale;
                float bx = x1 - 0.14f - i * step - bw * 0.5f;
                Part(p, new Vector3(bx, y + bh * 0.5f + 0.002f, z + face * 0.14f),
                     new Vector3(bw, bh, 0.24f * productScale), books[(i + r * 2) % books.Length], 0.5f);
            }
        }
        Reg(it, cx, z, len, d);
    }

    // 沿 Z 的靠墙书架：face=+1 书脊朝 +x（靠西墙），face=-1 朝 -x（靠东墙）
    static void ShelfWallZ(Transform p, Interior it, float x, float z0, float z1, int face,
                           Material frame, Material[] books, float h = 2.10f, float productScale = 1f)
    {
        const float d = 0.50f;
        float len = z1 - z0, cz = (z0 + z1) * 0.5f;
        var trim = Darker(frame, 0.76f);
        PartC(p, new Vector3(x - face * (d * 0.5f - 0.02f), h * 0.5f, cz),
              new Vector3(0.04f, h, len), frame, 0.010f, 1f);
        PartC(p, new Vector3(x, 0.06f, z0 + 0.03f), new Vector3(d, 0.12f, 0.06f), frame, 0.010f, 0.7f);
        PartC(p, new Vector3(x, 0.06f, z1 - 0.03f), new Vector3(d, 0.12f, 0.06f), frame, 0.010f, 0.7f);
        PartC(p, new Vector3(x, h - 0.03f, cz), new Vector3(d, 0.06f, len), frame, 0.012f, 1f);
        PartC(p, new Vector3(x, 0.06f, cz), new Vector3(d, 0.12f, len), frame, 0.012f, 1f);
        Part(p, new Vector3(x, h + 0.005f, cz), new Vector3(d + 0.02f, 0.03f, len + 0.02f), trim, 1f);
        for (int r = 1; r <= 3; r++)
            PartC(p, new Vector3(x, 0.12f + r * 0.48f, cz),
                  new Vector3(d - 0.06f, 0.04f, len - 0.12f), trim, 0.008f, 1f);

        float step = 0.17f * (productScale < 0.9f ? 0.76f : 1f);
        int n = Mathf.FloorToInt((len - 0.24f) / step);
        for (int r = 0; r < 4; r++)
        {
            float y = r == 0 ? 0.12f : 0.12f + r * 0.48f + 0.02f;
            for (int i = 0; i < n; i++)
            {
                if ((i + r * 5) % 11 == 7) continue;
                float bw = (0.10f + ((i * 7 + r * 3) % 4) * 0.014f) * productScale;
                float bh = (0.28f + ((i * 5 + r) % 4) * 0.022f) * productScale;
                float bz = z1 - 0.14f - i * step - bw * 0.5f;
                Part(p, new Vector3(x + face * 0.14f, y + bh * 0.5f + 0.002f, bz),
                     new Vector3(0.24f * productScale, bh, bw), books[(i + r * 2) % books.Length], 0.5f);
            }
        }
        Reg(it, x, cz, d, len);
    }

    static void BookStack(Transform p, Vector3 pos, int n = 3)
    {
        // 卡通书堆：高饱和六色轮换（原 #8c4a3c / #3f5c6b / #a9a08a 一堆灰褐）
        var cols = new[] { C("#e0554a"), C("#4a9ac8"), C("#f0b03c"),
                           C("#6cc06a"), C("#a86ad8"), C("#e08a4a") };
        for (int i = 0; i < n; i++)
            Part(p, pos + new Vector3(0f, i * 0.055f, 0f),
                 new Vector3(0.42f, 0.05f, 0.30f), cols[i % cols.Length], 0.5f);
    }

    static void TableLamp(Transform p, Vector3 pos, string shade = "#f0d8a8")
    {
        PartC(p, pos, new Vector3(0.18f, 0.04f, 0.18f), CB("#c98a4e"), 0.008f, 0.5f);
        Part(p, pos + new Vector3(0f, 0.18f, 0f), new Vector3(0.035f, 0.34f, 0.035f), C("#c8d0d8"), 1f);
        PartC(p, pos + new Vector3(0f, 0.38f, 0f), new Vector3(0.30f, 0.18f, 0.30f), CB(shade), 0.014f, 1f);
        Part(p, pos + new Vector3(0f, 0.30f, 0f), new Vector3(0.32f, 0.02f, 0.32f), CB("#e08a4a"), 1f);
    }

    // 吊灯（暖光）：y 是灯罩中心，吊杆自动接到天花板底，且不会把灯罩埋进吊顶
    static void Pendant(Transform p, float x, float y, float z, float drop, string shade = "#f0d0a0")
    {
        if (_ceilY > 0f) y = Mathf.Min(y, _ceilY - 0.75f);
        y = HangY(y, 0.10f);
        float rodBottom = y + 0.10f;
        float rodTop = _ceilY > rodBottom + 0.05f ? _ceilY : rodBottom + Mathf.Max(drop, 0.2f);
        Part(p, new Vector3(x, (rodBottom + rodTop) * 0.5f, z),
             new Vector3(0.025f, rodTop - rodBottom, 0.025f), CB("#5a626e"), 1f);
        // 灯罩倒角成梯形感：卡通吊灯是一个大色块 + 一圈亮描边
        PartC(p, new Vector3(x, y, z), new Vector3(0.54f, 0.22f, 0.54f), CB(shade), 0.040f, 1f);
        // 发光面朝下并嵌进灯罩底 1cm（原来放在罩顶 1.5cm 处，看着像灯罩上又多扣了一块白板）
        Part(p, new Vector3(x, y - 0.115f, z), new Vector3(0.44f, 0.03f, 0.44f), C("#fff2d6"), 1f);
        var go = new GameObject("pendantlight");
        go.transform.SetParent(p, false);
        go.transform.localPosition = new Vector3(x, y - 0.10f, z);
        var li = go.AddComponent<Light>();
        li.type = LightType.Point;
        li.range = 6.5f;
        li.intensity = 0.9f;
        li.color = MatLib.Hex("#ffdcae");
        li.shadows = LightShadows.None;
    }

    static void Stool(Transform p, Interior it, float x, float z, Material seat)
    {
        // 圆凳：倒角坐面 + 中柱 + 底盘（原来是一块 0.40 见方的坐板 + 一根细杆 + 一个小盘）
        PartC(p, new Vector3(x, 0.62f, z), new Vector3(0.42f, 0.07f, 0.42f), seat, 0.026f, 0.8f);
        Part(p, new Vector3(x, 0.575f, z), new Vector3(0.36f, 0.03f, 0.36f), Darker(seat, 0.78f), 1f);
        Part(p, new Vector3(x, 0.31f, z), new Vector3(0.07f, 0.62f, 0.07f), CB("#a8b0b8"), 1f);
        PartC(p, new Vector3(x, 0.04f, z), new Vector3(0.32f, 0.07f, 0.32f), CB("#8a949e"), 0.016f, 1f);
        Reg(it, x, z, 0.42f, 0.42f);
    }

    // 墙牌：face=-1 装在 z=max 的南墙（朝室内 -z），face=+1 装在北墙
    static void WallSign(Transform p, float x, float y, float wallZ, int face, string label,
                         float w = 2.2f, float h = 0.62f, Material plate = null, float textH = 0.030f)
    {
        plate ??= CB("#3f9a6a");
        float z = wallZ + face * 0.16f;
        PartC(p, new Vector3(x, y, z), new Vector3(w, h, 0.05f), plate, 0.016f, 1f);
        Part(p, new Vector3(x, y, z + face * 0.035f),
             new Vector3(w - 0.20f, h - 0.14f, 0.012f), C("#f2fbf4"), 1f);
        SignText(p, new Vector3(x, y, z + face * 0.055f), face > 0 ? 180f : 0f,
                 label, textH, MatLib.Hex("#12402a"));
    }

    // 吊牌（吊杆接天花板，朝 face 方向）
    static void HangingSign(Transform p, float x, float y, float z, int face, string label,
                            float w = 1.7f, float h = 0.42f, Material plate = null, float textH = 0.028f)
    {
        plate ??= CB("#3f9a6a");
        y = HangY(y, h * 0.5f);
        PartC(p, new Vector3(x, y, z), new Vector3(w, h, 0.05f), plate, 0.014f, 1f);
        Part(p, new Vector3(x, y, z + face * 0.035f),
             new Vector3(w - 0.16f, h - 0.12f, 0.012f), C("#f2fbf4"), 1f);
        SignText(p, new Vector3(x, y, z + face * 0.055f), face > 0 ? 180f : 0f,
                 label, textH, MatLib.Hex("#12402a"));
        HangRods(p, x, y + h * 0.5f, z, w);
    }

    // 墙纸 + 踢脚线（doorSide：0 北墙、1 南墙、2 西墙、3 东墙）
    static void PaperRoom(Transform p, float x1, float x2, float z1, float z2, float h,
                          Material paper, int doorSide, float dA, float dB)
    {
        var hx = new[] { (dA, dB) };
        var hz = new[] { (dA, dB) };
        WallDeco(p, x1 + 0.11f, x2 - 0.11f, z1 + 0.11f, 0.02f, h, 0, 0.05f, paper, 1.7f,
                 doorSide == 0 ? hx : null);
        WallDeco(p, x1 + 0.11f, x2 - 0.11f, z2 - 0.11f, 0.02f, h, 0, 0.05f, paper, 1.7f,
                 doorSide == 1 ? hx : null);
        WallDeco(p, z1 + 0.11f, z2 - 0.11f, x1 + 0.11f, 0.02f, h, 1, 0.05f, paper, 1.7f,
                 doorSide == 2 ? hz : null);
        WallDeco(p, z1 + 0.11f, z2 - 0.11f, x2 - 0.11f, 0.02f, h, 1, 0.05f, paper, 1.7f,
                 doorSide == 3 ? hz : null);

        void RunX(float a0, float a1, float z, bool door)
        {
            if (!door) { Skirt(p, a0, z, a1, z, 0.02f); return; }
            if (dA - 0.08f > a0) Skirt(p, a0, z, dA - 0.08f, z, 0.02f);
            if (dB + 0.08f < a1) Skirt(p, dB + 0.08f, z, a1, z, 0.02f);
        }
        void RunZ(float a0, float a1, float x, bool door)
        {
            if (!door) { Skirt(p, x, a0, x, a1, 0.02f); return; }
            if (dA - 0.08f > a0) Skirt(p, x, a0, x, dA - 0.08f, 0.02f);
            if (dB + 0.08f < a1) Skirt(p, x, dB + 0.08f, x, a1, 0.02f);
        }
        RunX(x1 + 0.16f, x2 - 0.16f, z1 + 0.16f, doorSide == 0);
        RunX(x1 + 0.16f, x2 - 0.16f, z2 - 0.16f, doorSide == 1);
        RunZ(z1 + 0.16f, z2 - 0.16f, x1 + 0.16f, doorSide == 2);
        RunZ(z1 + 0.16f, z2 - 0.16f, x2 - 0.16f, doorSide == 3);
    }

    static Material _exitMarker;
    static void DoorFrame(Transform p,Interior it,float x,float z,float halfW,float h,int face)
    {
        var root=new GameObject("PrecisionExitDoor").transform;root.SetParent(p,false);root.localPosition=new Vector3(x,0,z);root.localRotation=Quaternion.Euler(0,face>0?0:180,0);
        var batch=new RegionMeshBatch();var frame=InteriorMood.DoorFrame;var gasket=DetailSurface.Mat("rubber","#343e3d");var stone=DetailSurface.Mat("stone","#b8b1a3");
        foreach(int side in new[]{-1,1})
        {
            batch.Bevel(frame,new Vector3(side*(halfW+.09f),h/2,0),new Vector3(.18f,h+.08f,.36f),.018f);
            batch.Bevel(gasket,new Vector3(side*(halfW-.014f),h/2,.12f),new Vector3(.035f,h,.05f),.006f);
            batch.Bevel(frame,new Vector3(side*(halfW+.16f),h/2,.20f),new Vector3(.08f,h+.16f,.06f),.012f);
        }
        batch.Bevel(frame,new Vector3(0,h+.06f,.08f),new Vector3(halfW*2+.42f,.18f,.36f),.02f);
        batch.Bevel(stone,new Vector3(0,.045f,.46f),new Vector3(halfW*2-.08f,.05f,.86f),.012f);
        if(_exitMarker==null){_exitMarker=new Material(DetailSurface.Mat("paint","#34594a")){name="PrecisionExitMarker"};_exitMarker.EnableKeyword("_EMISSION");_exitMarker.SetColor("_EmissionColor",new Color(.04f,.10f,.06f));}
        batch.Bevel(_exitMarker,new Vector3(0,h+.065f,.23f),new Vector3(.65f,.18f,.07f),.014f);
        SignText(root,new Vector3(0,h+.065f,.272f),180,"出口",.022f,MatLib.Hex("#e6eddf"));
        batch.Box(gasket,new Vector3(0,h/2,.025f),new Vector3(halfW*2+.02f,h,.05f));
        float central=halfW>1.05f?Mathf.Min(halfW,1.26f):halfW;
        if(halfW-central>.05f)
        {
            var glass=DetailSurface.Mat("glass","#a6bdc0");float width=halfW-central;
            foreach(int side in new[]{-1,1})
            {float cx=side*(central+width/2);batch.Bevel(glass,new Vector3(cx,h*.56f,.12f),new Vector3(width-.075f,h*.82f,.05f),.008f);
             foreach(int edge in new[]{-1,1})batch.Bevel(frame,new Vector3(cx+edge*(width/2-.018f),h/2,.15f),new Vector3(.055f,h,.09f),.009f);
             batch.Bevel(frame,new Vector3(cx,.22f,.15f),new Vector3(width,.38f,.09f),.012f);}
        }
        batch.Flush(root);
        var collision=root.gameObject.AddComponent<BoxCollider>();collision.center=new Vector3(0,h/2,.085f);collision.size=new Vector3(halfW*2+.03f,h,.21f);
        it.Solids.Add(new Solid(x-halfW,z+face*.085f-.105f,x+halfW,z+face*.085f+.105f,true));
        if(halfW>1.05f){DoorLeaf(p,it,x-central/2,z,central-.018f,h,face,1);DoorLeaf(p,it,x+central/2,z,central-.018f,h,face,-1);}
        else DoorLeaf(p,it,x,z,central*2-.035f,h,face,1);
        var lamp=new GameObject("exit_warm_light");lamp.transform.SetParent(root,false);lamp.transform.localPosition=new Vector3(0,h-.1f,.45f);var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.color=MatLib.Hex("#ffe6c3");light.range=4.2f;light.intensity=.65f;light.shadows=LightShadows.None;
    }
    static void DoorLeaf(Transform p,Interior it,float cx,float z,float w,float h,int face,int handleSide)
    {
        var root=new GameObject("PrecisionDoorLeaf").transform;root.SetParent(p,false);root.localPosition=new Vector3(cx,0,z+face*.12f);root.localRotation=Quaternion.Euler(0,face>0?0:180,0);
        var body=InteriorMood.DoorBody;var trim=InteriorMood.DoorFrame;var metal=DetailSurface.Mat("metal",InteriorMood.Current.Metal);var glass=DetailSurface.Mat("glass","#7b979b");var batch=new RegionMeshBatch();
        batch.Bevel(body,new Vector3(0,h/2,0),new Vector3(w,h,.12f),.024f,0,3);
        foreach(float y in new[]{h*.72f,h*.28f})
        {
            float ph=h*(y>h*.5f?.31f:.27f),pw=w*.62f;
            batch.Bevel(trim,new Vector3(0,y,.069f),new Vector3(pw+.11f,ph+.11f,.035f),.012f);
            batch.Bevel(y>h*.5f?glass:body,new Vector3(0,y,.091f),new Vector3(pw,ph,.018f),.006f);
        }
        batch.Bevel(metal,new Vector3(0,.18f,.077f),new Vector3(w*.85f,.20f,.025f),.006f);
        int localSide=handleSide*face;float hx=localSide*(w/2-.19f);batch.Bevel(metal,new Vector3(hx,1.04f,.095f),new Vector3(.10f,.23f,.045f),.018f);
        batch.Cylinder(metal,new Vector3(hx,1.07f,.10f),new Vector3(hx,1.07f,.17f),.025f);
        batch.Cylinder(metal,new Vector3(hx,1.07f,.17f),new Vector3(hx-localSide*.16f,1.07f,.17f),.018f);
        foreach(float hy in new[]{.33f,h*.5f,h-.33f})batch.Cylinder(metal,new Vector3(-localSide*(w/2-.025f),hy-.07f,.02f),new Vector3(-localSide*(w/2-.025f),hy+.07f,.02f),.026f);
        batch.Flush(root);
    }

    // 收银台（长轴沿 X；face=+1 顾客在 +z 侧）
    static void Checkout(Transform p, Interior it, float x, float z, int face)
    {
        var white = C("#fdfaf0");
        var stone = CB("#8fb0c8");
        PartC(p, new Vector3(x, 0.48f, z), new Vector3(2.4f, 0.92f, 0.85f), white, 0.028f, 1f);
        PartC(p, new Vector3(x, 0.96f, z), new Vector3(2.5f, 0.07f, 0.9f), stone, 0.014f, 1f);
        Part(p, new Vector3(x - face * 0.12f, 1.00f, z + face * 0.20f),
             new Vector3(1.5f, 0.035f, 0.28f), C("#4a5260"), 1f);
        PartC(p, new Vector3(x + 0.82f, 1.20f, z - face * 0.10f),
              new Vector3(0.46f, 0.30f, 0.30f), CB("#4a5260"), 0.014f, 1f);
        Part(p, new Vector3(x + 0.82f, 1.38f, z - face * 0.22f),
             new Vector3(0.38f, 0.22f, 0.02f), C("#a8e0f0"), 1f);
        Part(p, new Vector3(x - 0.78f, 1.04f, z - face * 0.15f),
             new Vector3(0.24f, 0.08f, 0.18f), C("#b8c4d0"), 1f);
        Part(p, new Vector3(x - 0.78f, 1.14f, z - face * 0.15f),
             new Vector3(0.16f, 0.12f, 0.02f), C("#e0f4f8"), 1f);
        Reg(it, x, z, 2.5f, 0.9f);
    }

    // 生鲜台：木台 + 三个斜口货箱 + 彩色果蔬块
    static void ProduceTable(Transform p, Interior it, float x, float z, float w, float d)
    {
        var wood = CB("#c98a4e");
        PartC(p, new Vector3(x, 0.42f, z), new Vector3(w, 0.84f, d), wood, 0.030f, 1f);
        PartC(p, new Vector3(x, 0.87f, z), new Vector3(w + 0.08f, 0.06f, d + 0.08f), CB("#e0a860"), 0.012f, 1f);
        // 卡通果蔬：高饱和五色（原 #7a3c32 / #5f7a3f 这些在 cel shading 下像烂菜）
        var cols = new[] { C("#e0554a"), C("#6cc06a"), C("#f0b03c"), C("#a86ad8"), C("#ff8a3c") };
        int n = Mathf.Max(2, Mathf.FloorToInt(w / 0.95f));
        for (int i = 0; i < n; i++)
        {
            float cx = x - w * 0.5f + (i + 0.5f) * w / n;
            PartC(p, new Vector3(cx, 0.96f, z), new Vector3(w / n - 0.10f, 0.16f, d - 0.16f), CB("#b8743c"), 0.014f, 0.7f);
            PartC(p, new Vector3(cx, 1.06f, z), new Vector3(w / n - 0.18f, 0.08f, d - 0.26f), cols[i % cols.Length], 0.012f, 0.4f);
            for (int k = 0; k < 3; k++)
                Part(p, new Vector3(cx - 0.18f + k * 0.18f, 1.14f, z + (k % 2 == 0 ? -0.10f : 0.10f)),
                     new Vector3(0.13f, 0.13f, 0.13f), cols[(i + k) % cols.Length], 0.3f);
        }
        Reg(it, x, z, w, d);
    }

    // 冷藏柜（沿东墙，正面朝 -x）
    static void Cooler(Transform p, Interior it, float x, float z, float w = 1.7f)
    {
        var white = C("#f0f8fc");
        var frame = CB("#8fb0c8");
        // 开放式冷柜：背板 + 顶底 + 侧板，前方留空露商品，避免实心白盒/不透明玻璃
        PartC(p, new Vector3(x + 0.32f, 1.10f, z), new Vector3(0.06f, 2.20f, w), white, 0.012f, 1f);
        PartC(p, new Vector3(x, 2.18f, z), new Vector3(0.70f, 0.06f, w), white, 0.012f, 1f);
        PartC(p, new Vector3(x, 0.02f, z), new Vector3(0.70f, 0.06f, w), white, 0.012f, 1f);
        foreach (float ez in new[] { z - w * 0.5f + 0.03f, z + w * 0.5f - 0.03f })
            PartC(p, new Vector3(x, 1.10f, ez), new Vector3(0.70f, 2.20f, 0.06f), white, 0.012f, 1f);
        Part(p, new Vector3(x - 0.34f, 2.10f, z), new Vector3(0.05f, 0.08f, w - 0.10f), frame, 1f);
        Part(p, new Vector3(x - 0.34f, 0.30f, z), new Vector3(0.05f, 0.08f, w - 0.10f), frame, 1f);
        foreach (float ez in new[] { z - w * 0.5f + 0.08f, z + w * 0.5f - 0.08f })
            Part(p, new Vector3(x - 0.34f, 1.20f, ez), new Vector3(0.05f, 1.75f, 0.05f), frame, 1f);
        var cols = new[] { C("#e0554a"), C("#4a9ac8"), C("#6cc06a"), C("#f0b03c"), C("#a86ad8"), C("#ff8a3c") };
        for (int r = 0; r < 4; r++)
        {
            float y = 0.30f + r * 0.44f;
            PartC(p, new Vector3(x - 0.18f, y - 0.03f, z), new Vector3(0.30f, 0.03f, w - 0.22f), CB("#c8d8e4"), 0.008f, 1f);
            for (int i = 0; i < 7; i++)
            {
                float bz = z - w * 0.5f + 0.16f + i * (w - 0.32f) / 6f;
                Part(p, new Vector3(x - 0.10f, y + 0.17f, bz),
                     new Vector3(0.16f, 0.30f, 0.13f), cols[(i + r) % cols.Length], 0.3f);
            }
        }
        Reg(it, x, z, 0.8f, w + 0.1f);
    }

}
