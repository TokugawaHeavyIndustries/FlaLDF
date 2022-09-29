# FlaLDF
CUDA-accelerated FLAC encoder modified for Laserdisc RF files

# Building

1) Install IDE and dependencies:  
1a) Microsoft Visual Studio 2017 or newer (.NET Desktop Development option)  
1b) Add in .NET Framework 4.7 SDK and Targeting
1c) Ensure .NET Framework 3.5 is enabled in Windows Optional Features
2) run `dotnet restore` in both `CUETools.Codecs.Flake` and `CUETools.Codecs`
3) Open folder in VC
4) Select `CUETools.FLACCL.cmd.csproj` in startup project dropdown
5) Build
