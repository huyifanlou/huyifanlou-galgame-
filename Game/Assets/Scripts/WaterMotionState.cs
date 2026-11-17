using UnityEngine;

// Integrate velocity; never multiply a changing wind value by total session age.
public struct WaterMotionState
{
    public double Seconds,X,Z;
    public void Step(float delta,float rain)
    {
        double dt=Mathf.Clamp(delta,0,.05f),storm=1+Mathf.Clamp01(rain)*.4;
        Seconds+=dt;X-=.018*storm*dt;Z+=.006*storm*dt;
    }
    public void Publish()
    {Shader.SetGlobalVector("_WaterMotion",new Vector4((float)Seconds,0,0,0));Shader.SetGlobalVector("_WaterFlow",new Vector4((float)X,(float)Z,0,0));}
}
