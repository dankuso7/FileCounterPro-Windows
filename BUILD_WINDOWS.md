# How to Build FileCounter Pro for Windows

Since this is a native C# WPF app, it must be compiled on a Windows machine.

## Prerequisites
1. A Windows 10/11 PC.
2. Install **Visual Studio 2022 Community** (Free).
3. During installation, select the **".NET Desktop Development"** workload.

## Build Steps
1. Copy the `FileCounterPro-Windows` folder from your Mac to your Windows PC.
2. Open the `FileCounterPro.csproj` file in Visual Studio.
3. At the top of Visual Studio, ensure the build configuration is set to **Release** (not Debug).
4. Go to the top menu and click **Build -> Build Solution** (or press `Ctrl+Shift+B`).
5. Wait for it to say "Build Succeeded" at the bottom left.

## Where is the App?
After compiling, navigate to:
`FileCounterPro-Windows\bin\Release\net8.0-windows\`

You will find `FileCounterPro_Windows.exe`. This is your fully optimized, native Windows app! You can double-click it to run it, and it will immediately use WMI and the Windows Registry to analyze the PC's hardware and software!
