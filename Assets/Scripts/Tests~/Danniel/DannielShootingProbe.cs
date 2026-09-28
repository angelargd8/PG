#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public sealed class DannielShootingProbe : MonoBehaviour
{
    private int checks;
    private static void Set(object o, string field, object value) =>
        o.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(o, value);
    private static void Call(object o, string method, params object[] args) =>
        o.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(o, args);
    private void Check(bool value, string message)
    {
        if (!value) throw new Exception("DANNIELPROBE: " + message);
        checks++;
    }
    private IEnumerator Start()
    {
        IEnumerator run = Run();
        while (true)
        {
            object next;
            try { if (!run.MoveNext()) break; next = run.Current; }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
            yield return next;
        }
        Debug.Log($"DANNIELPROBE PASSED: {checks} checks, owner collisions, reuse, expiry, pool overflow and 500 rhythm turns.");
        EditorApplication.Exit(0);
    }
    private IEnumerator Run()
    {
        var root = new GameObject("PoolRoot");
        root.SetActive(false);
        var template = new GameObject("BulletTemplate");
        template.SetActive(false);
        template.AddComponent<SphereCollider>().isTrigger = true;
        template.AddComponent<Rigidbody>().useGravity = false;
        var prefab = template.AddComponent<PooledBullet>();
        var pool = root.AddComponent<BulletPool>();
        Set(pool, "bulletPrefab", prefab);
        Set(pool, "prewarmCount", 2);
        Set(pool, "defaultCapacity", 2);
        Set(pool, "maxSize", 4);
        root.SetActive(true);
        var shooter = new GameObject("Owner");
        var gun = new GameObject("GunCollider");
        gun.transform.SetParent(shooter.transform, false);
        gun.AddComponent<BoxCollider>();
        var bullet = pool.Spawn(Vector3.zero, Quaternion.identity, 0, 10, shooter.transform);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Check(bullet.gameObject.activeSelf && pool.ActiveBulletCount == 1, "bullet survives actual physics contact with owner's child collider");
        bullet.Despawn();
        var otherShooter = new GameObject("OtherOwner");
        otherShooter.transform.position = Vector3.right * 10;
        var reused = pool.Spawn(Vector3.zero, Quaternion.identity, 0, 10, otherShooter.transform);
        Check(reused == bullet, "bullet reused from same pool");
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Check(!reused.gameObject.activeSelf && pool.ActiveBulletCount == 0, "previous owner is no longer exempt on reuse");
        bullet = pool.Spawn(new Vector3(0,100,0), Quaternion.identity, 0, 0.02f);
        yield return new WaitForSeconds(0.08f);
        Check(pool.ActiveBulletCount == 0, "expiry returns an untouched projectile");
        for (int i=0; i<80; i++) pool.Spawn(new Vector3(i*3,100,0), Quaternion.identity, 0, 10);
        Check(pool.ActiveBulletCount == 80, "maxSize is not a concurrent bullet or ammunition cap");
        pool.EndExperience();
        Check(pool.ActiveBulletCount == 0 && pool.InactiveBulletCount == 4, "overflow trimmed on return, pool remains usable");
        yield return null;
        var director = new GameObject("Rhythm").AddComponent<DannielRhythmDirector>();
        var beatMap = ScriptableObject.CreateInstance<BeatMapSO>();
        var times = new List<double>();
        for (int i=0;i<1002;i++) times.Add(i*0.384);
        beatMap.SetData(null,156.25f,times);
        Set(director,"_beatMap",beatMap);
        Set(director,"<IsRunning>k__BackingField",true);
        var target = new GameObject("Target").transform;
        target.position = new Vector3(3,1000,10);
        var enemies = new List<EnemyShooter>();
        for(int i=0;i<24;i++)
        {
            var go = new GameObject("Enemy"+i);
            go.SetActive(false);
            go.transform.position = new Vector3(i*0.2f,1000,0);
            var enemy = go.AddComponent<EnemyShooter>();
            Set(enemy,"bulletPoint",go.transform);
            enemy.Configure(target,pool,director);
            go.SetActive(true);
            enemies.Add(enemy);
        }
        for(int turn=0;turn<500;turn++)
        {
            if(turn%7==0)
            {
                var enemy=enemies[turn%enemies.Count];
                enemy.gameObject.SetActive(false);
                enemy.Configure(target,pool,director);
                enemy.gameObject.SetActive(true);
            }
            Call(director,"PrepareAttackers",turn*2);
            Call(director,"FirePendingAttackers",turn*2+1);
            Check(pool.ActiveBulletCount==1,"exactly one actual projectile at rhythm turn "+turn);
            pool.EndExperience();
        }
        root.SetActive(false);
        enemies[0].ShowRhythmCue();
        Check(!enemies[0].TryShootOnBeat(),"inactive pool cannot be reported as a successful shot");
        root.SetActive(true);
        enemies[0].ShowRhythmCue();
        Check(enemies[0].TryShootOnBeat(),"shooting resumes with available pool");
        pool.EndExperience();
        var boundaryEnemy = enemies[0];
        boundaryEnemy.transform.position = target.position + Vector3.back * 14.9f;
        Check(boundaryEnemy.CanShootOnBeat, "enemy inside range can announce a shot");
        boundaryEnemy.ShowRhythmCue();
        boundaryEnemy.transform.position = target.position + Vector3.back * 16f;
        Check(!boundaryEnemy.CanShootOnBeat, "moving platform takes shooter beyond acquisition range");
        Check(boundaryEnemy.TryShootOnBeat() && pool.ActiveBulletCount == 1,
            "announced shot still fires when moving platform crosses range boundary");
        Check(!boundaryEnemy.TryShootOnBeat(), "one cue cannot fire twice");
        pool.EndExperience();
        boundaryEnemy.ShowRhythmCue();
        Check(!boundaryEnemy.TryShootOnBeat(), "out of range enemy cannot acquire a new shot");
        boundaryEnemy.transform.position = target.position + Vector3.back * 14;
        boundaryEnemy.ShowRhythmCue();
        boundaryEnemy.gameObject.SetActive(false);
        boundaryEnemy.gameObject.SetActive(true);
        Check(!boundaryEnemy.TryShootOnBeat(), "recycled enemy cannot keep an old announced shot");
    }
}
#endif
