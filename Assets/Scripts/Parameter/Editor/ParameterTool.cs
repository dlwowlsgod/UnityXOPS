using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityXOPS;

namespace UnityXOPS.Editor
{
    /// <summary>
    /// 에디터에서 ScriptableObject화한 Parameter를 json으로 저장하기 위한 클래스입니다.
    /// </summary>
    /// <remarks>
    /// Menubar의 UnityXOPS/Parameter에 있습니다.
    /// </remarks>
    public class ParameterTool
    {
        [MenuItem("UnityXOPS/Parameter/Save Human Parameter to JSON")]
        private static void SaveHumanParameter()
        {
            SaveParameter(
                manager => manager.weaponParameters,
                so => new WeaponParameterWrapper
                {
                    finalName = so.finalName,
                    damage = so.damage,
                    penetration = so.penetration,
                    fireRate = so.fireRate,
                    velocity = so.velocity,
                    capacity = so.capacity,
                    reloadTime = so.reloadTime,
                    recoil = so.recoil,
                    errorRange = so.errorRange,
                    staticModels = so.staticModels,
                    muzzleFlash = so.muzzleFlash,
                    muzzleFlashScale = so.muzzleFlashScale,
                    muzzleFlashTexturePath = so.muzzleFlashTexturePath,
                    muzzleFlashPosition = so.muzzleFlashPosition,
                    shellEjection = so.shellEjection,
                    shellEjectionDelayTime = so.shellEjectionDelayTime,
                    shellTexturePath = so.shellTexturePath,
                    shellScale = so.shellScale,
                    shellEjectionPosition = so.shellEjectionPosition,
                    shellEjectionDirection = so.shellEjectionDirection,
                    burst = so.burst,
                    bulletIndex = so.bulletIndex,
                    scopeIndex = so.scopeIndex,
                    silent = so.silent,
                    weaponSoundPath = so.weaponSoundPath,
                    soundVolume = so.soundVolume,
                    soundRadius = so.soundRadius,
                    handIndex = so.handIndex,
                    nextWeaponIndex = so.nextWeaponIndex,
                    nextWeaponSwitchTime = so.nextWeaponSwitchTime,
                    swapTime = so.swapTime
                },
                dataList => new WeaponParameterList { items = dataList },
                "weapon.json",
                "Weapon"
            );

        }

        [MenuItem("UnityXOPS/Parameter/Save HumanType Parameter to JSON")]
        private static void SaveHumanTypeParameter()
        {
            SaveParameter(
                manager => manager.humanTypeParameters,
                so => new HumanTypeParameterWrapper
                {
                    finalName = so.finalName,
                    startZombie = so.startZombie,
                    infected = so.infected,
                    resurrectionChance = so.resurrectionChance,
                    resurrectionTime = so.resurrectionTime,
                    pickupWeapon = so.pickupWeapon,
                    headDamageMult = so.headDamageMult,
                    bodyDamageMult = so.bodyDamageMult,
                    legDamageMult = so.legDamageMult,
                    speedMult = so.speedMult,
                    smokeOnDeath = so.smokeOnDeath,
                    clampNoBloodHp = so.clampNoBloodHp,
                    clampFallDamage = so.clampFallDamage
                },
                dataList => new HumanTypeParameterList { items = dataList },
                "human_type.json",
                "HumanType"
            );
        }
        
        [MenuItem("UnityXOPS/Parameter/Save HumanAI Parameter to JSON")]
        private static void SaveHumanAIParameter()
        {
            SaveParameter(
                manager => manager.humanAIParameters,
                so => new HumanAIParameterWrapper
                {
                    finalName = so.finalName,
                    fireFrequency = so.fireFrequency,
                    searchFrequency = so.searchFrequency,
                    normalSearchView = so.normalSearchView,
                    cautionSearchView = so.cautionSearchView,
                    aimFire = so.aimFire
                },
                dataList => new HumanAIParameterList { items = dataList },
                "human_ai.json",
                "HumanAI"
            );
        }
        
        [MenuItem("UnityXOPS/Parameter/Save HumanArm Parameter to JSON")]
        private static void SaveHumanArmParameter()
        {
            SaveParameter(
                manager => manager.humanArmParameters,
                so => new HumanArmParameterWrapper
                {
                    finalName = so.finalName,
                    armMeshPaths = so.armMeshPaths
                },
                dataList => new HumanArmParameterList { items = dataList },
                "human_arm.json",
                "HumanArm"
            );
        }
        
        [MenuItem("UnityXOPS/Parameter/Save HumanLeg Parameter to JSON")]
        private static void SaveHumanLegParameter()
        {
            SaveParameter(
                manager => manager.humanLegParameters,
                so => new HumanLegParameterWrapper
                {
                    finalName = so.finalName,
                    idleLegPath = so.idleLegPath,
                    walkLegPath = so.walkLegPath,
                    runLegPath = so.runLegPath
                },
                dataList => new HumanLegParameterList { items = dataList },
                "human_leg.json",
                "HumanLeg"
            );
        }
        
        [MenuItem("UnityXOPS/Parameter/Save Weapon Parameter to JSON")]
        private static void SaveWeaponParameter()
        {
            SaveParameter(
                manager => manager.weaponParameters,
                so => new WeaponParameterWrapper
                {
                    finalName = so.finalName,
                    flags = so.flags,
                    damage = so.damage,
                    penetration = so.penetration,
                    fireRate = so.fireRate,
                    velocity = so.velocity,
                    capacity = so.capacity,
                    reloadTime = so.reloadTime,
                    recoil = so.recoil,
                    errorRange = so.errorRange,
                    staticModels = so.staticModels,
                    muzzleFlash = so.muzzleFlash,
                    muzzleFlashScale = so.muzzleFlashScale,
                    muzzleFlashTexturePath = so.muzzleFlashTexturePath,
                    muzzleFlashPosition = so.muzzleFlashPosition,
                    shellEjection = so.shellEjection,
                    shellEjectionDelayTime = so.shellEjectionDelayTime,
                    shellTexturePath = so.shellTexturePath,
                    shellScale = so.shellScale,
                    shellEjectionPosition = so.shellEjectionPosition,
                    shellEjectionDirection = so.shellEjectionDirection,
                    burst = so.burst,
                    bulletIndex = so.bulletIndex,
                    scopeIndex = so.scopeIndex,
                    silent = so.silent,
                    weaponSoundPath = so.weaponSoundPath,
                    soundVolume = so.soundVolume,
                    soundRadius = so.soundRadius,
                    handIndex = so.handIndex,
                    nextWeaponIndex = so.nextWeaponIndex,
                    nextWeaponSwitchTime = so.nextWeaponSwitchTime,
                    swapTime = so.swapTime,
                    magazineCount = so.magazineCount
                },
                dataList => new WeaponParameterList { items = dataList },
                "weapon.json",
                "Weapon"
            );
        }
        
        [MenuItem("UnityXOPS/Parameter/Save Bullet Parameter to JSON")]
        private static void SaveBulletParameter()
        {
            SaveParameter(
                manager => manager.bulletParameters,
                so => new BulletParameterWrapper
                {
                    finalName = so.finalName,
                    meshPath = so.meshPath,
                    texturePath = so.texturePath,
                    explodeAfterTimer = so.explodeAfterTimer,
                    explodeAfterWallHit = so.explodeAfterWallHit,
                    explodeAfterColliderHit = so.explodeAfterColliderHit,
                    mass = so.mass,
                    explodeSize = so.explodeSize,
                    colliderSize = so.colliderSize,
                    timer = so.timer,
                    wallHitPack = so.wallHitPack,
                    explodePack = so.explodePack
                },
                dataList => new BulletParameterList { items = dataList },
                "bullet.json",
                "Bullet"
            );
        }
        
        [MenuItem("UnityXOPS/Parameter/Save Scope Parameter to JSON")]
        private static void SaveScopeParameter()
        {
            SaveParameter(
                manager => manager.scopeParameters,
                so => new ScopeParameterWrapper
                {
                    finalName = so.finalName,
                    staticCrosshair = so.staticCrosshair,
                    dynamicCrosshair = so.dynamicCrosshair,
                    zoom = so.zoom,
                    zoomFov = so.zoomFov,
                    zoomTexturePath = so.zoomTexturePath,
                    zoomStaticCrosshair = so.zoomStaticCrosshair,
                    zoomDynamicCrosshair = so.zoomDynamicCrosshair
                },
                dataList => new ScopeParameterList { items = dataList },
                "scope.json",
                "Scope"
            );
        }
        
        [MenuItem("UnityXOPS/Parameter/Save Object Parameter to JSON")]
        private static void SaveObjectParameter()
        {
            SaveParameter(
                manager => manager.objectParameters,
                so => new ObjectParameterWrapper
                {
                    finalName = so.finalName,
                    meshPath = so.meshPath,
                    texturePath = so.texturePath,
                    soundPath = so.soundPath,
                    colliderType = so.colliderType,
                    colliderSize = so.colliderSize,
                    through = so.through,
                    hp = so.hp,
                    bounce = so.bounce
                },
                dataList => new ObjectParameterList { items = dataList },
                "object.json",
                "Object"
            );
        }

        [MenuItem("UnityXOPS/Parameter/Save Demo Parameter to JSON")]
        private static void SaveDemoParameter()
        {
            SaveParameter(
                manager => manager.demoParameters,
                so => new DemoParameterWrapper
                {
                    finalName = so.finalName,
                    bd1Path = so.bd1Path,
                    pd1Path = so.pd1Path,
                    skyIndex = so.skyIndex
                },
                dataList => new DemoParameterList { items = dataList },
                "demo.json",
                "Demo"
            );
        }

        [MenuItem("UnityXOPS/Parameter/Save Sky Parameter to JSON")]
        private static void SaveSkyParameter()
        {
            SaveParameter(
                manager => manager.skyParameters,
                so => new SkyParameterWrapper
                {
                    finalName = so.finalName,
                    skyTexturePath = so.skyTexturePath,
                    billboardTexturePath = so.billboardTexturePath,
                    cloudTexturePath = so.cloudTexturePath,
                    lightTexturePath = so.lightTexturePath,
                    light = so.light,
                    lightStrength = so.lightStrength,
                    lightColor = so.lightColor,
                    lightDirection = so.lightDirection
                },
                dataList => new SkyParameterList { items = dataList },
                "sky.json",
                "Sky"
            );
        }

        [MenuItem("UnityXOPS/Parameter/Save Official Mission Parameter to JSON")]
        private static void SaveOfficialMissionParameter()
        {
            SaveParameter(
                manager => manager.officialMissionParameters,
                so => new OfficialMissionParameterWrapper
                {
                    finalName = so.finalName,
                    longName = so.longName,
                    bd1Path = so.bd1Path,
                    pd1Path = so.pd1Path,
                    txtPath = so.txtPath,
                    adjustCollision = so.adjustCollision,
                    darkScreen = so.darkScreen
                },
                dataList => new OfficialMissionParameterList { items = dataList },
                "official_mission.json",
                "Official mission"
            );
        }

        private static void SaveParameter<TSo, TData>(
            Func<ParameterManager, List<TSo>> getParameters,
            Func<TSo, TData> createDataWrapper,
            Func<List<TData>, object> createListWrapper,
            string fileName,
            string logName) where TSo : ScriptableObject
        {
            ParameterManager manager = UnityEngine.Object.FindFirstObjectByType<ParameterManager>();
            if (!manager)
            {
                Debug.LogError("[ParameterTool] Detected parameter manager instance null.");
                return;
            }

            var parameterPath = Path.Combine(Application.streamingAssetsPath, "common", "parameter");
            Directory.CreateDirectory(parameterPath);

            var parameterList = getParameters(manager);
            if (parameterList == null)
            {
                Debug.LogWarning($"[ParameterTool] Parameter list for {logName} is null.");
                return;
            }

            var dataList = parameterList.Select(createDataWrapper).ToList();
            var listWrapper = createListWrapper(dataList);
            var json = JsonUtility.ToJson(listWrapper, true);
            var fullPath = Path.Combine(parameterPath, fileName);
            File.WriteAllText(fullPath, json);
            Debug.Log($"[ParameterTool] {logName} parameter saved to {fullPath}");

            AssetDatabase.Refresh();
        }
    }
}