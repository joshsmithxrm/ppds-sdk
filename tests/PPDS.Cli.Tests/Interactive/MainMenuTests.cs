using PPDS.Cli.Interactive.Components;
using Xunit;

namespace PPDS.Cli.Tests.Interactive;

/// <summary>
/// Tests for the MainMenu component.
/// </summary>
public class MainMenuTests
{
    [Fact]
    public void MenuAction_Exit_IsDefined()
    {
        // Verify Exit action is defined in the enum
        Assert.True(Enum.IsDefined(typeof(MainMenu.MenuAction), MainMenu.MenuAction.Exit));
    }

    [Fact]
    public void MenuAction_SwitchProfile_IsDefined()
    {
        Assert.True(Enum.IsDefined(typeof(MainMenu.MenuAction), MainMenu.MenuAction.SwitchProfile));
    }

    [Fact]
    public void MenuAction_SwitchEnvironment_IsDefined()
    {
        Assert.True(Enum.IsDefined(typeof(MainMenu.MenuAction), MainMenu.MenuAction.SwitchEnvironment));
    }

    [Fact]
    public void MenuAction_CreateProfile_IsDefined()
    {
        Assert.True(Enum.IsDefined(typeof(MainMenu.MenuAction), MainMenu.MenuAction.CreateProfile));
    }

    [Fact]
    public void MenuAction_DataOperations_IsDefined()
    {
        Assert.True(Enum.IsDefined(typeof(MainMenu.MenuAction), MainMenu.MenuAction.DataOperations));
    }

    [Fact]
    public void MenuAction_PluginManagement_IsDefined()
    {
        Assert.True(Enum.IsDefined(typeof(MainMenu.MenuAction), MainMenu.MenuAction.PluginManagement));
    }

    [Fact]
    public void MenuAction_MetadataExplorer_IsDefined()
    {
        Assert.True(Enum.IsDefined(typeof(MainMenu.MenuAction), MainMenu.MenuAction.MetadataExplorer));
    }

    [Fact]
    public void MenuItem_Label_IsRequired()
    {
        var item = new MainMenu.MenuItem
        {
            Label = "Test",
            Action = MainMenu.MenuAction.Exit
        };

        Assert.Equal("Test", item.Label);
    }

    [Fact]
    public void MenuItem_IsEnabled_DefaultsToTrue()
    {
        var item = new MainMenu.MenuItem
        {
            Label = "Test",
            Action = MainMenu.MenuAction.Exit
        };

        Assert.True(item.IsEnabled);
    }

    [Fact]
    public void MenuItem_IsCategory_DefaultsToFalse()
    {
        var item = new MainMenu.MenuItem
        {
            Label = "Test",
            Action = MainMenu.MenuAction.Exit
        };

        Assert.False(item.IsCategory);
    }

    [Fact]
    public void MenuItem_ToString_ReturnsLabel()
    {
        var item = new MainMenu.MenuItem
        {
            Label = "My Label",
            Action = MainMenu.MenuAction.Exit
        };

        Assert.Equal("My Label", item.ToString());
    }

    [Fact]
    public void MenuItem_CanBeDisabled()
    {
        var item = new MainMenu.MenuItem
        {
            Label = "Test",
            Action = MainMenu.MenuAction.DataOperations,
            IsEnabled = false
        };

        Assert.False(item.IsEnabled);
    }

    [Fact]
    public void MenuItem_CanBeCategory()
    {
        var item = new MainMenu.MenuItem
        {
            Label = "Test",
            Action = MainMenu.MenuAction.DataOperations,
            IsCategory = true
        };

        Assert.True(item.IsCategory);
    }
}
