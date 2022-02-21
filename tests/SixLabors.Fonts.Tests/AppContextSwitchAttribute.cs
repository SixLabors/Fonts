// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Reflection;
using Xunit.Sdk;

namespace SixLabors.Fonts.Tests;

/// <summary>
/// Sets the AppContext switch <see cref="SwitchName"/> to <see cref="IsEnabled"/> before the test execution
/// and resets the switch after the text execution.
/// </summary>
/// <remarks>
/// On .NET Core 3.1, the switch is actually reset to its default value (i.e. <c>null</c>) after execution.
/// On .NET Core 2.1, the switch is set to <c>!IsEnabled</c> after execution.
/// </remarks>
public class AppContextSwitchAttribute : BeforeAfterTestAttribute
{
    public string SwitchName { get; }

    public bool IsEnabled { get; }

    public AppContextSwitchAttribute(string switchName, bool isEnabled)
    {
        this.SwitchName = switchName;
        this.IsEnabled = isEnabled;
    }

    public override void Before(MethodInfo methodUnderTest)
    {
#if NETCOREAPP3_1_OR_GREATER
        AppDomain.CurrentDomain.SetData(this.SwitchName, this.IsEnabled.ToString());
#else
        AppContext.SetSwitch(this.SwitchName, this.IsEnabled);
#endif
    }

    public override void After(MethodInfo methodUnderTest)
    {
#if NETCOREAPP3_1_OR_GREATER
        AppDomain.CurrentDomain.SetData(this.SwitchName, null);
#else
        AppContext.SetSwitch(this.SwitchName, !this.IsEnabled);
#endif
    }
}
