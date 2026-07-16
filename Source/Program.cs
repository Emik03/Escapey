// SPDX-License-Identifier: LicenseRef-PolyForm-Strict
#if ANDROID
[assembly: Android.App.Application(Debuggable = true)]
#else
using EscapeyGame game = new();
game.Run();
#endif
