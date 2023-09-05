// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Reflection;
using Xunit.Sdk;

namespace SixLabors.Fonts.Tests;

/// <summary>
/// Sets the AppContext switch <see cref="SwitchName"/> to <see cref="IsEnabled"/> before the test execution
/// and resets the switch after the text execution.
/// </summary>
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
        => AppDomain.CurrentDomain.SetData(this.SwitchName, this.IsEnabled.ToString());

    public override void After(MethodInfo methodUnderTest)
        => AppDomain.CurrentDomain.SetData(this.SwitchName, null);
}
