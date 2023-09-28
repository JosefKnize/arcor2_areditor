md ..\arcor2_AREditor\Assets\Submodules
mklink /j ..\arcor2_AREditor\Assets\Submodules\JSONObject ..\arcor2_AREditor\Submodules\JSONObject
mklink /j ..\arcor2_AREditor\Assets\Submodules\UnityDynamicPanels ..\arcor2_AREditor\Submodules\UnityDynamicPanels\Plugins
mklink /j ..\arcor2_AREditor\Assets\Submodules\UnityRuntimeInspector ..\arcor2_AREditor\Submodules\UnityRuntimeInspector\Plugins
mklink /j ..\arcor2_AREditor\Assets\Submodules\RosSharp ..\arcor2_AREditor\Submodules\RosSharp\Unity3D\Assets\RosSharp
mklink /j ..\arcor2_AREditor\Assets\Submodules\Simple-Side-Menu ..\arcor2_AREditor\Submodules\Simple-Side-Menu
mklink /j ..\arcor2_AREditor\Assets\Submodules\NativeWebSocket ..\arcor2_AREditor\Submodules\NativeWebSocket\NativeWebSocket\Assets\WebSocket
mklink /j ..\arcor2_AREditor\Assets\Submodules\Automation ..\arcor2_AREditor\Submodules\trilleon\client\Assets\Automation
mklink /j ..\arcor2_AREditor\Assets\Submodules\OffScreenIndicator "..\arcor2_AREditor\Submodules\off-screen-indicator\Off Screen Indicator\Assets"

mklink /j ..\arcor2_AREditor\Assets\Submodules\QRTracking ..\arcor2_AREditor\Submodules\QRTracking\SampleQRCodes\Assets

mklink /j "..\arcor2_AREditor\Assets\Submodules\Joystick Pack" "..\arcor2_AREditor\Submodules\arcor2_areditor_private\3rdparty\Joystick Pack"
mklink /j ..\arcor2_AREditor\Assets\Submodules\loadingBar ..\arcor2_AREditor\Submodules\arcor2_areditor_private\3rdparty\loadingBar
mklink /j ..\arcor2_AREditor\Assets\Submodules\LunarConsole ..\arcor2_AREditor\Submodules\arcor2_areditor_private\3rdparty\LunarConsole
mklink /j "..\arcor2_AREditor\Assets\Submodules\Modern UI Pack" "..\arcor2_AREditor\Submodules\arcor2_areditor_private\3rdparty\Modern UI Pack"
mklink /j ..\arcor2_AREditor\Assets\Submodules\plugins ..\arcor2_AREditor\Submodules\arcor2_areditor_private\3rdparty\plugins
mklink /j ..\arcor2_AREditor\Assets\Submodules\SimpleCollada ..\arcor2_AREditor\Submodules\arcor2_areditor_private\3rdparty\SimpleCollada
mklink /j ..\arcor2_AREditor\Assets\Submodules\TriLib ..\arcor2_AREditor\Submodules\arcor2_areditor_private\3rdparty\TriLib
mklink /j ..\arcor2_AREditor\Assets\Submodules\UIGraph ..\arcor2_AREditor\Submodules\arcor2_areditor_private\3rdparty\UIGraph

mklink /j ..\arcor2_AREditor\Assets\Submodules\QuickOutline ..\arcor2_AREditor\Submodules\QuickOutline\QuickOutline

del ..\arcor2_AREditor\Submodules\RosSharp\Unity3D\Assets\RosSharp\Plugins\External\Newtonsoft.Json.dll*
del ..\arcor2_AREditor\Submodules\RosSharp\Unity3D\Assets\RosSharp\Plugins\External\Newtonsoft.Json.xml*

rd /s /q ..\arcor2_AREditor\Submodules\QRTracking\SampleQRCodes\Assets\MRTK
del ..\arcor2_AREditor\Submodules\QRTracking\SampleQRCodes\Assets\MRTK.meta
rd /s /q ..\arcor2_AREditor\Submodules\QRTracking\SampleQRCodes\Assets\MixedRealityToolkit.Generated
del ..\arcor2_AREditor\Submodules\QRTracking\SampleQRCodes\Assets\MixedRealityToolkit.Generated.meta
rd /s /q ..\arcor2_AREditor\Submodules\QRTracking\SampleQRCodes\Assets\XR
del ..\arcor2_AREditor\Submodules\QRTracking\SampleQRCodes\Assets\XR.meta

ren ..\arcor2_AREditor\Submodules\QRTracking\SampleQRCodes\Assets\NuGet\Editor\NugetForUnity.dll NuGetForUnity.dll
ren ..\arcor2_AREditor\Submodules\QRTracking\SampleQRCodes\Assets\NuGet\Editor\NugetForUnity.dll.meta NuGetForUnity.dll.meta

pause
