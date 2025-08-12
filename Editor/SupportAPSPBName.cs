
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using Gokoukotori.Bullshit.NDMF;

[assembly: ExportsPlugin(typeof(SupportAPSPBName))]
namespace Gokoukotori.Bullshit.NDMF
{
    /// <summary>
    /// AvaterPoseSystem側が対応するまでの応急処置
    /// </summary>
    public class SupportAPSPBName : Plugin<SupportAPSPBName>
    {
        public override string QualifiedName => "Gokoukotori.Bullshit.NDMF";
        public override string DisplayName => "SupportAPSPBName";
        private bool canExecute;
        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving)
            .BeforePlugin("nadena.dev.modular-avatar")
            .Run("SupportAPSPBName", ctx =>
            {
                canExecute = ctx.AvatarRootObject.GetComponentsInChildren<ZeroFactory.AvatarPoseSystem.NDMF.AvatarPoseSystem>(true).Length != 0;
            });
            InPhase(BuildPhase.Transforming)
            .BeforePlugin("nadena.dev.modular-avatar")
            .BeforePlugin("ZeroFactory.AvatarPoseSystem.NDMF")
            .AfterPlugin("aoyon.facetune")
            .Run("SupportAPSPBName", ctx =>
            {
                if (!canExecute) return;
                var root = ctx.AvatarRootObject;
                var fxControllers = new List<RuntimeAnimatorController>();
                fxControllers.AddRange(root.GetComponent<VRCAvatarDescriptor>().baseAnimationLayers
                    .Where(item => item.type == VRCAvatarDescriptor.AnimLayerType.FX && item.animatorController != null)
                    .Select(item => item.animatorController));
                fxControllers.AddRange(root.GetComponentsInChildren<ModularAvatarMergeAnimator>(true)
                    .Where(item => item.layerType == VRCAvatarDescriptor.AnimLayerType.FX && item.animator != null)
                    .Select(item => item.animator));
                var animationClipGroups = fxControllers.SelectMany(controller => controller.animationClips)
                    .GroupBy(clip => AssetDatabase.GetAssetPath(clip))
                    .ToDictionary(group => group.Key, group => new HashSet<AnimationClip>(group));

                foreach (var group in animationClipGroups)
                {
                    foreach (var clip in group.Value)
                    {
                        var bindings = AnimationUtility.GetCurveBindings(clip);
                        foreach (var binding in bindings)
                        {
                            var newPath = "";
                            //APS適用時にAPS_PBがない場合の解決
                            if (binding.type == typeof(VRCPhysBone) && !Regex.IsMatch(binding.path, @"APS_PB")&& !Regex.IsMatch(binding.path, @"AvatarPoseSystem"))
                            {
                                newPath = binding.path + "/APS_PB";
                                var newBinding = new EditorCurveBinding
                                {
                                    path = newPath,
                                    type = binding.type,
                                    propertyName = binding.propertyName
                                };
                                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                                AnimationUtility.SetEditorCurve(clip, binding, null);
                                AnimationUtility.SetEditorCurve(clip, newBinding, curve);
                            }
                        }
                    }
                }
            });
        }
    }
}