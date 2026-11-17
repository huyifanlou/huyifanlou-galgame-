using UnityEngine;

// Bounded local impact particles; ray hits place ripples on the actual top surface, not through roofs.
public sealed class RainSurfaceDetail:MonoBehaviour
{
    Weather weather;WorldBuilder world;Transform player;ParticleSystem rings,drops;float budget;
    public void Init(Weather w,WorldBuilder builder,Transform follow)
    {
        weather=w;world=builder;player=follow;
        var go=new GameObject("RainSurfaceRipples");go.transform.SetParent(transform,false);rings=go.AddComponent<ParticleSystem>();
        var main=rings.main;main.playOnAwake=false;main.startSpeed=0;main.startLifetime=.55f;main.startSize=.1f;main.maxParticles=260;main.simulationSpace=ParticleSystemSimulationSpace.World;
        var em=rings.emission;em.enabled=false;var shape=rings.shape;shape.enabled=false;
        var size=rings.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.15f,1,1));
        var fade=rings.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.65f,0),new GradientAlphaKey(0,1)});fade.color=gradient;
        var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);texture.name="RainRippleSoftRing";texture.wrapMode=TextureWrapMode.Clamp;
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){float r=new Vector2((x-31.5f)/31.5f,(y-31.5f)/31.5f).magnitude;float a=Mathf.Clamp01(1-Mathf.Abs(r-.72f)/.095f);texture.SetPixel(x,y,new Color(.82f,.88f,.9f,a*.8f));}texture.filterMode=FilterMode.Bilinear;texture.Apply();
        var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.material=MatLib.Fade("snow_flake",new Color(.58f,.63f,.68f,.5f));renderer.material.mainTexture=texture;renderer.renderMode=ParticleSystemRenderMode.HorizontalBillboard;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;rings.Play();
        var splash=new GameObject("RainImpactDrops");splash.transform.SetParent(transform,false);drops=splash.AddComponent<ParticleSystem>();
        var dm=drops.main;dm.playOnAwake=false;dm.startSpeed=0;dm.startLifetime=.22f;dm.gravityModifier=.8f;dm.maxParticles=150;dm.simulationSpace=ParticleSystemSimulationSpace.World;
        var de=drops.emission;de.enabled=false;var ds=drops.shape;ds.enabled=false;var dr=splash.GetComponent<ParticleSystemRenderer>();dr.material=MatLib.Fade("snow_flake",new Color(.65f,.69f,.73f,.6f));dr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;drops.Play();
    }
    void Update()
    {
        if(weather==null||player==null)return;
        if(weather.Indoor){rings.Clear();drops.Clear();budget=0;return;}
        budget+=Time.deltaTime*weather.RainAmt*165;
        int count=Mathf.Min(12,Mathf.FloorToInt(budget));budget-=count;
        for(int i=0;i<count;i++)
        {
            var offset=Random.insideUnitCircle*8;float x=player.position.x+offset.x,z=player.position.z+offset.y;
            float y=world.GroundY(x,z);var origin=new Vector3(x,player.position.y+12,z);
            if(Physics.Raycast(origin,Vector3.down,out var hit,30,~0,QueryTriggerInteraction.Ignore)){if(hit.normal.y<.65f)continue;y=Mathf.Max(y,hit.point.y);}
            var tint=Color.Lerp(new Color(.78f,.85f,.9f,.65f),new Color(.40f,.48f,.60f,.50f),TimeOfDay.NightMixAt(GetComponent<TimeOfDay>().Hour));
            var emit=new ParticleSystem.EmitParams{position=new Vector3(x,y+.025f,z),startSize=Random.Range(.22f,.52f),startLifetime=Random.Range(.38f,.65f),startColor=tint};rings.Emit(emit,1);
            if(i%2==0)drops.Emit(new ParticleSystem.EmitParams{position=emit.position,velocity=new Vector3(Random.Range(-.2f,.2f),Random.Range(.6f,1.1f),Random.Range(-.2f,.2f)),startLifetime=.20f,startSize=Random.Range(.025f,.045f),startColor=tint},2);
        }
    }
}
