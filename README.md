# Electric Scooter — Schedule I mod

Adds an **Electric Scooter** to Schedule I (MelonLoader, Il2Cpp). Buy it from the skateboard seller at the Shred Shack; it replaces the skateboard visually and reuses the game's skating system.

- Below the engage speed you push with your foot like on a skateboard.
- Above it, the motor takes over: hold the forward key and the scooter accelerates by itself while the rider stays still.

**Requirements:** Schedule I v0.4.6f13 (Mono not supported — Il2Cpp build), MelonLoader 0.7.3.

## Install
Drop `ElectricScooter.dll` in the game's `Mods` folder.

## Configuration
Settings are in `UserData/MelonPreferences.cfg`, category `[ElectricScooter]`. Press **F9** in game to reload them while riding. Motor settings: `MotorEnabled`, `MotorEngageSpeedKmh`, `MotorTopSpeedKmh`, `MotorAcceleration`. Pose settings (hands, elbows, feet, body) can be tuned the same way.

## Build
Set `GameDir` in `ElectricScooter.csproj`, then `dotnet build -c Release`. The prebuilt asset bundle is in `Bundle/scooter_bundle` and gets embedded in the DLL.

## Known limitations
Not yet visible to other players in multiplayer.
