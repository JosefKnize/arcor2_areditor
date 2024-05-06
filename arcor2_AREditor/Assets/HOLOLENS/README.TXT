This README is only for HoloLens 2

In order to fix air tap sensitivity you need to remove 4 conditions in ArticulatedHandController on lines 109, 116, 122 and 129 
that ignore HandsAggregator if unity action was already used
NOTE: This is just workaround and in future should be fixed

How to deploy:
In unity select File -> Build Settings (UWP, ARM 64-bit) -> Open the build folder -> and edit file 
..\AREditor\Unity Data.vcxitems
In this delete these two records:

<None Include="$(MSBuildThisFileDirectory)..\Il2CppOutputProject\Source\il2cppOutput\Symbols\il2cppFileRoot.txt">
      <DeploymentContent>true</DeploymentContent>
      <ExcludeFromResourceIndex>true</ExcludeFromResourceIndex>
</None>
<None Include="$(MSBuildThisFileDirectory)..\Il2CppOutputProject\Source\il2cppOutput\Symbols\LineNumberMappings.json">
      <DeploymentContent>true</DeploymentContent>
      <ExcludeFromResourceIndex>true</ExcludeFromResourceIndex>
</None>

After this you can open the solution in build folder
..\AREditor.sln

in Project -> Properties -> Debugging -> Machine name input ip of HoloLens2
Change the run settings to Release ARM64 and run. (this can take from 5 to 30 minutes)
