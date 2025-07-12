// SPDX-License-Identifier: MPL-2.0
#if ANDROID
[assembly: Android.App.Application(Debuggable = true)]
#else
EscapeyGame.Go();
#endif
