REM Build both debug and release versions of Axiom3D
..\..\..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Axiom.Demos.build debug build.all

..\..\..\BuildSupport\NAnt\bin\NAnt -buildfile:..\Axiom.Demos.build release build.all 