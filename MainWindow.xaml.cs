using Discord;
using Discord.Rest;
using DotNetEnv;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AmirHelperControlPanel;

public partial class MainWindow : Window
{
    private readonly DiscordRestClient _client = new();

    private readonly string configPath =
        @"C:\Users\360dd\Desktop\Amir's Helper\config.json";

    private ControlPanelUser? CurrentUser;

    public MainWindow()
    {
        InitializeComponent();

        Env.Load(@"C:\Users\360dd\Desktop\Amir's Helper\.env");

        LoadConfig();
        LoginPage.Visibility = Visibility.Visible;
        MainAppGrid.Visibility = Visibility.Collapsed;
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        BotConfig config = GetConfig();

        string discordId = LoginDiscordIdBox.Text.Trim();
        string password = LoginPasswordBox.Password.Trim();

        ControlPanelUser? user = config.ControlPanelUsers
            .FirstOrDefault(x => x.DiscordId == discordId);

        if (user == null)
        {
            LoginStatusText.Text = "Access denied. User not found.";
            return;
        }

        if (user.Status.Equals("Banned", System.StringComparison.OrdinalIgnoreCase) ||
            user.AccessLevel.Equals("Banned", System.StringComparison.OrdinalIgnoreCase))
        {
            LoginStatusText.Text = "You have been banned from the control panel.";
            return;
        }

        if (!password.Equals(user.Password))
        {
            LoginStatusText.Text = "Invalid password.";
            return;
        }

        CurrentUser = user;

        LoginPage.Visibility = Visibility.Collapsed;
        MainAppGrid.Visibility = Visibility.Visible;

        ApplyAccessLevel(user.AccessLevel);
        ShowDashboard();

        LoggedInUserText.Text =
            $"Logged in as {user.Name} | Access Level: {user.AccessLevel}";

        await SendControlPanelLogAsync(
            "Control Panel Login",
            $"**User:** {user.Name}\n" +
            $"**Discord ID:** `{user.DiscordId}`\n" +
            $"**Access Level:** {user.AccessLevel}",
            false
        );
    }

    private void ApplyAccessLevel(string accessLevel)
    {
        DashboardButton.Visibility = Visibility.Visible;

        SettingsButton.Visibility = Visibility.Collapsed;
        RoleSyncButton.Visibility = Visibility.Collapsed;
        ModerationButton.Visibility = Visibility.Collapsed;
        EmbedButton.Visibility = Visibility.Collapsed;
        UsersButton.Visibility = Visibility.Collapsed;

        if (accessLevel.Equals("Owner", System.StringComparison.OrdinalIgnoreCase))
        {
            SettingsButton.Visibility = Visibility.Visible;
            RoleSyncButton.Visibility = Visibility.Visible;
            ModerationButton.Visibility = Visibility.Visible;
            EmbedButton.Visibility = Visibility.Visible;
            UsersButton.Visibility = Visibility.Visible;
        }
        else if (accessLevel.Equals("Admin", System.StringComparison.OrdinalIgnoreCase))
        {
            RoleSyncButton.Visibility = Visibility.Visible;
            ModerationButton.Visibility = Visibility.Visible;
            EmbedButton.Visibility = Visibility.Visible;
        }
        else if (accessLevel.Equals("Restricted", System.StringComparison.OrdinalIgnoreCase))
        {
            EmbedButton.Visibility = Visibility.Visible;
        }
    }

    private void Dashboard_Click(object sender, RoutedEventArgs e)
    {
        ShowDashboard();
    }
    private async void Logout_Click(object sender, RoutedEventArgs e)
{
    if (CurrentUser != null)
    {
        await SendControlPanelLogAsync(
            "Control Panel Logout",
            $"**User:** {CurrentUser.Name}\n" +
            $"**Discord ID:** `{CurrentUser.DiscordId}`",
            false
        );
    }

    CurrentUser = null;

    LoginDiscordIdBox.Text = "";
    LoginPasswordBox.Password = "";
    LoginStatusText.Text = "";

    MainAppGrid.Visibility = Visibility.Collapsed;
    LoginPage.Visibility = Visibility.Visible;
}
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(SettingsPage);
    }

    private void RoleSync_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(RoleSyncPage);
    }

    private void Moderation_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(ModerationPage);
    }

    private void EmbedBuilder_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(EmbedBuilderPage);
    }

    private void Users_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(UsersPage);
        LoadControlPanelUsers();
    }

    private void ShowDashboard()
    {
        ShowPage(DashboardPage);
    }

    private void ShowPage(UIElement page)
    {
        DashboardPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        RoleSyncPage.Visibility = Visibility.Collapsed;
        ModerationPage.Visibility = Visibility.Collapsed;
        EmbedBuilderPage.Visibility = Visibility.Collapsed;
        UsersPage.Visibility = Visibility.Collapsed;

        page.Visibility = Visibility.Visible;
    }

    private BotConfig GetConfig()
    {
        if (!File.Exists(configPath))
        {
            BotConfig defaultConfig = new()
            {
                LogServerId = "1445993907606388769",
                GeneralLogChannelId = "1511940172143530147",
                BanLogChannelId = "1511940713124724786",
                ControlPanelUsers = new List<ControlPanelUser>
                {
                    new ControlPanelUser
                    {
                        Name = "Amir G.",
                        DiscordId = "YOUR_DISCORD_ID",
                        AccessLevel = "Owner",
                        Status = "Active"
                    }
                }
            };

            SaveConfig(defaultConfig);
            return defaultConfig;
        }

        string json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<BotConfig>(json) ?? new BotConfig();
    }

    private void SaveConfig(BotConfig config)
    {
        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(configPath, json);
    }

    private void LoadConfig()
    {
        BotConfig config = GetConfig();

        LogServerIdBox.Text = config.LogServerId;
        GeneralLogChannelBox.Text = config.GeneralLogChannelId;
        BanLogChannelBox.Text = config.BanLogChannelId;

        SettingsStatusText.Text = "Config loaded successfully.";
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        BotConfig config = GetConfig();

        config.LogServerId = LogServerIdBox.Text;
        config.GeneralLogChannelId = GeneralLogChannelBox.Text;
        config.BanLogChannelId = BanLogChannelBox.Text;

        SaveConfig(config);

        SettingsStatusText.Text = "Settings saved.";

        _ = SendControlPanelLogAsync(
            "Settings Updated",
            $"**Updated By:** {CurrentUser?.Name ?? "Unknown"}\n" +
            $"**General Log Channel:** `{config.GeneralLogChannelId}`\n" +
            $"**Ban Log Channel:** `{config.BanLogChannelId}`",
            false
        );
    }

    private async void SendEmbedButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string token = Env.GetString("DISCORD_TOKEN");

            if (string.IsNullOrWhiteSpace(token))
            {
                EmbedStatusText.Text = "Bot token not found.";
                return;
            }

            if (!ulong.TryParse(ChannelIdBox.Text, out ulong channelId))
            {
                EmbedStatusText.Text = "Invalid channel ID.";
                return;
            }

            await _client.LoginAsync(TokenType.Bot, token);

            var channel = await _client.GetChannelAsync(channelId) as IMessageChannel;

            if (channel == null)
            {
                EmbedStatusText.Text = "Channel not found.";
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle(TitleBox.Text)
                .WithDescription(DescriptionBox.Text)
                .WithColor(GetSelectedColor())
                .WithFooter("Sent from Amir's Helper Control Panel")
                .WithCurrentTimestamp();

            if (!string.IsNullOrWhiteSpace(ImageUrlBox.Text))
            {
                embed.WithImageUrl(ImageUrlBox.Text);
            }

            await channel.SendMessageAsync(embed: embed.Build());

            await SendControlPanelLogAsync(
                "Embed Sent From Control Panel",
                $"**Sent By:** {CurrentUser?.Name ?? "Unknown"}\n" +
                $"**Channel ID:** `{channelId}`\n" +
                $"**Title:** {TitleBox.Text}\n" +
                $"**Color:** {((ComboBoxItem)ColorBox.SelectedItem).Content}",
                false
            );

            EmbedStatusText.Text = "Embed sent successfully.";
        }
        catch (System.Exception ex)
        {
            EmbedStatusText.Text = $"Error: {ex.Message}";
        }
    }

    private async void BanUserButton_Click(object sender, RoutedEventArgs e)
    {
        await BanUserFromControlPanelAsync(
            BanUserIdBox.Text,
            BanReasonBox.Text
        );
    }

    private async Task BanUserFromControlPanelAsync(string discordId, string reason)
    {
        if (!ulong.TryParse(discordId, out ulong userId))
        {
            MessageBox.Show("Invalid Discord ID.");
            return;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            reason = "Banned from Amir's Helper Control Panel.";
        }

        string token = Env.GetString("DISCORD_TOKEN");

        if (string.IsNullOrWhiteSpace(token))
        {
            MessageBox.Show("Bot token not found.");
            return;
        }

        await _client.LoginAsync(TokenType.Bot, token);

        int success = 0;
        List<string> failed = new();

        var botGuilds = await _client.GetGuildsAsync();

        foreach (var guild in botGuilds)
        {
            try
            {
                await guild.AddBanAsync(userId, 0, reason);
                success++;
            }
            catch (System.Exception ex)
            {
                failed.Add($"{guild.Name} — {ex.Message}");
            }
        }

        await SendControlPanelLogAsync(
            "Global Ban Issued From Control Panel",
            $"**Issued By:** {CurrentUser?.Name ?? "Unknown"}\n" +
            $"**User ID:** `{discordId}`\n" +
            $"**Reason:** {reason}\n" +
            $"**Successful Servers:** {success}\n" +
            $"**Failed Servers:** {failed.Count}",
            true
        );

        MessageBox.Show($"Ban complete.\nSuccessful: {success}\nFailed: {failed.Count}");
    }

    private void LoadControlPanelUsers()
    {
        BotConfig config = GetConfig();

        ControlPanelUsersGrid.ItemsSource = null;
        ControlPanelUsersGrid.ItemsSource = config.ControlPanelUsers;
    }

    private void AddOrUpdateUserButton_Click(object sender, RoutedEventArgs e)
    {
        BotConfig config = GetConfig();

        string discordId = UserDiscordIdBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(discordId))
        {
            UsersStatusText.Text = "Discord ID is required.";
            return;
        }

        string accessLevel =
            ((ComboBoxItem)UserAccessLevelBox.SelectedItem).Content.ToString() ?? "Restricted";

        ControlPanelUser? existing =
            config.ControlPanelUsers.FirstOrDefault(x => x.DiscordId == discordId);

        if (existing == null)
{
    config.ControlPanelUsers.Add(new ControlPanelUser
    {
        Name = UserNameBox.Text.Trim(),
        DiscordId = discordId,
        Password = UserPasswordBox.Text.Trim(),
        AccessLevel = accessLevel,
        Status = "Active"
    });

    UsersStatusText.Text = "User added.";
}
else
{
    existing.Name = UserNameBox.Text.Trim();
    existing.Password = UserPasswordBox.Text.Trim();
    existing.AccessLevel = accessLevel;
    existing.Status = accessLevel == "Banned" ? "Banned" : "Active";

    UsersStatusText.Text = "User updated.";
}

        SaveConfig(config);
        LoadControlPanelUsers();

        _ = SendControlPanelLogAsync(
            "Control Panel User Updated",
            $"**Updated By:** {CurrentUser?.Name ?? "Unknown"}\n" +
            $"**Name:** {UserNameBox.Text}\n" +
            $"**Discord ID:** `{discordId}`\n" +
            $"**Access Level:** {accessLevel}",
            false
        );
    }

    private void RestrictUserButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateControlPanelUserStatus("Active", "Restricted");
    }

    private void BanControlPanelUserButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateControlPanelUserStatus("Banned", "Banned");
    }

    private void ReactivateUserButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateControlPanelUserStatus("Active", "Restricted");
    }

    private void UpdateControlPanelUserStatus(string status, string accessLevel)
    {
        BotConfig config = GetConfig();

        string discordId = UserDiscordIdBox.Text.Trim();

        ControlPanelUser? user =
            config.ControlPanelUsers.FirstOrDefault(x => x.DiscordId == discordId);

        if (user == null)
        {
            UsersStatusText.Text = "User not found.";
            return;
        }

        user.Status = status;
        user.AccessLevel = accessLevel;

        SaveConfig(config);
        LoadControlPanelUsers();

        UsersStatusText.Text = $"User marked as {accessLevel}.";

        _ = SendControlPanelLogAsync(
            "Control Panel User Status Changed",
            $"**Updated By:** {CurrentUser?.Name ?? "Unknown"}\n" +
            $"**Name:** {user.Name}\n" +
            $"**Discord ID:** `{user.DiscordId}`\n" +
            $"**Access Level:** {user.AccessLevel}\n" +
            $"**Status:** {user.Status}",
            false
        );
    }

    private async Task SendControlPanelLogAsync(
        string title,
        string description,
        bool isBanLog)
    {
        try
        {
            string token = Env.GetString("DISCORD_TOKEN");

            if (string.IsNullOrWhiteSpace(token))
                return;

            BotConfig config = GetConfig();

            ulong channelId = isBanLog
                ? ulong.Parse(config.BanLogChannelId)
                : ulong.Parse(config.GeneralLogChannelId);

            await _client.LoginAsync(TokenType.Bot, token);

            var channel = await _client.GetChannelAsync(channelId) as IMessageChannel;

            if (channel == null)
                return;

            var embed = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(isBanLog ? Color.Red : Color.Blue)
                .WithFooter("Amir's Helper Control Panel")
                .WithCurrentTimestamp()
                .Build();

            await channel.SendMessageAsync(embed: embed);
        }
        catch
        {
        }
    }

    private Color GetSelectedColor()
    {
        string selectedColor =
            ((ComboBoxItem)ColorBox.SelectedItem).Content.ToString() ?? "Blue";

        return selectedColor.ToLower() switch
        {
            "gold" => Color.Gold,
            "red" => Color.Red,
            "green" => Color.Green,
            "purple" => Color.Purple,
            "orange" => Color.Orange,
            _ => Color.Blue
        };
    }
}

public class ControlPanelUser
{
     public string Name { get; set; } = "";
    public string DiscordId { get; set; } = "";
    public string Password { get; set; } = "";
    public string AccessLevel { get; set; } = "Restricted";
    public string Status { get; set; } = "Active";
}

public class BotConfig
{
    public string LogServerId { get; set; } = "";
    public string GeneralLogChannelId { get; set; } = "";
    public string BanLogChannelId { get; set; } = "";

    public List<ControlPanelUser> ControlPanelUsers { get; set; } = new();
}
