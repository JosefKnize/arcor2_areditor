// TODO: [Optional] Add copyright and license statement(s).

using MixedReality.Toolkit;
using MixedReality.Toolkit.Subsystems;
using UnityEngine;
using UnityEngine.Scripting;

namespace VUTFIT.MRTK3.Subsystems
{
    [Preserve]
    [MRTKSubsystem(
        Name = "vutfit.mrtk3.subsystems",
        DisplayName = "VUTFIT NewSubsystem",
        Author = "VUTFIT",
        ProviderType = typeof(VUTFITNewSubsystemProvider),
        SubsystemTypeOverride = typeof(VUTFITNewSubsystem),
        ConfigType = typeof(BaseSubsystemConfig))]
    public class VUTFITNewSubsystem : NewSubsystem
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            // Fetch subsystem metadata from the attribute.
            var cinfo = XRSubsystemHelpers.ConstructCinfo<VUTFITNewSubsystem, NewSubsystemCinfo>();

            if (!VUTFITNewSubsystem.Register(cinfo))
            {
                Debug.LogError($"Failed to register the {cinfo.Name} subsystem.");
            }
        }

        [Preserve]
        class VUTFITNewSubsystemProvider : Provider
        {

            #region INewSubsystem implementation

            // TODO: Add the provider implementation.

            #endregion NewSubsystem implementation
        }
    }
}
