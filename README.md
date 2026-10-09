# Electric Scooter — Schedule I mod

Adds an **Electric Scooter** to Schedule I (MelonLoader, Il2Cpp). Buy it from the skateboard seller at the Shred Shack; it replaces the skateboard visually and reuses the game's skating system.

- Below the engage speed you push with your foot like on a skateboard.
- Above it, the motor takes over: hold the forward key and the scooter accelerates by itself while the rider stays still.

**Requirements:** Schedule I v0.4.6f13 (Mono not supported — Il2Cpp build), MelonLoader 0.7.3.

## Install
Drop `ElectricScooter.dll` in the game's `Mods` folder.

## Settings
Pose and motor settings are built into the mod (no config file). Motor: engages at 16 km/h, top speed 40 km/h, acceleration 5 m/s². Change them in `ScooterConfig.cs` and rebuild.

## Build
Set `GameDir` in `ElectricScooter.csproj`, then `dotnet build -c Release`. The prebuilt asset bundle is in `Bundle/scooter_bundle` and gets embedded in the DLL.

## Known limitations
Not yet visible to other players in multiplayer.
