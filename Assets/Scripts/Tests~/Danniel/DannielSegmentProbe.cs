#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public sealed class DannielSegmentProbe : MonoBehaviour
{
    private int checks;
    private static void Set(object o,string field,object value) => o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
    private static object Get(object o,string field) => o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(o);
    private static object Call(object o,string method,params object[] args) => o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
    private void Check(bool value,string message) { if(!value) throw new Exception("SEGMENTPROBE: "+message); checks++; }
    private IEnumerator Start()
    {
        IEnumerator run=Run();
        while(true)
        {
            object next;
            try { if(!run.MoveNext()) break; next=run.Current; }
            catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
            yield return next;
        }
        Debug.Log($"SEGMENTPROBE PASSED: {checks} checks; Android/desktop budgets, earlier recycling and uninterrupted anchors through 300 recycles.");
        EditorApplication.Exit(0);
    }
    private GameObject Template(string name,float start,float end)
    {
        var root=new GameObject(name);
        root.SetActive(false);
        var a=new GameObject("Start").transform; a.SetParent(root.transform,false); a.localPosition=Vector3.forward*start;
        var b=new GameObject("End").transform; b.SetParent(root.transform,false); b.localPosition=Vector3.forward*end;
        var anchors=root.AddComponent<SegmentAnchors>(); Set(anchors,"startPoint",a); Set(anchors,"endPoint",b);
        root.SetActive(true);
        return root;
    }
    private IEnumerator Run()
    {
        var normal=Template("Normal",-5.5f,35.49f);
        var rotated=Template("Rotated",-4.995001f,34.99f);
        for(int scenario=0;scenario<3;scenario++)
        {
            bool android=scenario!=0;
            int androidCount=scenario==2?2:3;
            int expected=android?androidCount:4;
            var root=new GameObject("SegmentPool");
            var pool=root.AddComponent<SegmentPool>();
            Set(pool,"normalSegmentPrefab",normal); Set(pool,"rotatedSegmentPrefab",rotated);
            Set(pool,"maxActiveSegments",4); Set(pool,"useAndroidSegmentSettings",true);
            Set(pool,"androidMaxActiveSegments",androidCount); Set(pool,"androidRecycleAdvanceSegments",1);
            Set(pool,"secondSegmentDelay",0f); Set(pool,"speed",0f);
            Check(pool.ResolveSegmentLimit(RuntimePlatform.Android)==androidCount,"Android count");
            Check(pool.ResolveSegmentLimit(RuntimePlatform.WindowsEditor)==4,"Editor preserves desktop count");
            Check(pool.ResolveSegmentLimit(RuntimePlatform.WindowsPlayer)==4,"Quest Link preserves desktop count");
            // Traverse nested preload routine here so exceptions reach the test runner.
            var preload=pool.PreloadForPlatform(android?RuntimePlatform.Android:RuntimePlatform.WindowsPlayer);
            while(preload.MoveNext()) yield return preload.Current;
            Check(pool.ActiveSegmentCount==1 && pool.SegmentLimit==expected,"preload only activates first segment");
            int instances=root.GetComponentsInChildren<SegmentAnchors>(true).Length;
            Check(instances==2*((expected+1)/2),"prewarm only reserves required alternating types");
            pool.BeginExperience();
            // Fill synchronously; gameplay's coroutine sees the completed count next frame.
            for(int i=1;i<expected;i++) Call(pool,"AddInitialSegment");
            Set(pool,"initialFillComplete",true);
            Check(pool.ActiveSegmentCount==expected,"correct active segment budget");
            for(int cycle=0;cycle<100;cycle++)
            {
                var segments=(Array)Get(pool,"segments");
                int oldest=(int)Get(pool,"oldestIndex");
                object old=segments.GetValue(oldest);
                var oldTransform=(Transform)Get(old,"Transform");
                float cutoff=(float)Call(pool,"GetRecycleZ",old);
                Check(android?cutoff>-80:Mathf.Approximately(cutoff,-80),"only Android recycles earlier");
                float delta=cutoff-0.01f-oldTransform.position.z;
                for(int i=0;i<expected;i++) ((Transform)Get(segments.GetValue(i),"Transform")).position+=Vector3.forward*delta;
                Check(((SegmentAnchors)Get(old,"Anchors")).EndPoint.position.z<0,"recycled segment already behind play area");
                Call(pool,"Update");
                oldest=(int)Get(pool,"oldestIndex");
                for(int i=1;i<expected;i++)
                {
                    var previous=(SegmentAnchors)Get(segments.GetValue((oldest+i-1)%expected),"Anchors");
                    var current=(SegmentAnchors)Get(segments.GetValue((oldest+i)%expected),"Anchors");
                    Check(Vector3.Distance(previous.EndPoint.position,current.StartPoint.position)<0.001f,"segments stay connected");
                    Check((bool)Get(segments.GetValue((oldest+i-1)%expected),"IsRotated")!=(bool)Get(segments.GetValue((oldest+i)%expected),"IsRotated"),"segment types alternate");
                }
                Check(root.GetComponentsInChildren<SegmentAnchors>(false).Length==expected,"no extra active distant segment");
                Check(root.GetComponentsInChildren<SegmentAnchors>(true).Length==instances,"no allocations during recycling");
            }
            pool.EndExperience(); Destroy(root);
            yield return null;
        }
        Destroy(normal); Destroy(rotated);
    }
}
#endif
