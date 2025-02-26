using AmongUs.GameOptions;
using Hazel;
using Il2CppSystem.Linq;
using InnerNet;
using System.Linq;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;
using Mathf = UnityEngine.Mathf;

namespace TOHE.Modules;

public class PlayerGameOptionsSender : GameOptionsSender
{
    public static void SetDirty(PlayerControl player) => SetDirty(player.PlayerId);
    public static void SetDirty(byte playerId) =>
        AllSenders.OfType<PlayerGameOptionsSender>()
        .Where(sender => sender.player.PlayerId == playerId)
        .ToList().ForEach(sender => sender.SetDirty());
    public static void SetDirtyToAll() =>
        AllSenders.OfType<PlayerGameOptionsSender>()
        .ToList().ForEach(sender => sender.SetDirty());

    public override IGameOptions BasedGameOptions =>
        Main.RealOptionsData.Restore(new NormalGameOptionsV07(new UnityLogger().Cast<ILogger>()).Cast<IGameOptions>());
    public override bool IsDirty { get; protected set; }

    public PlayerControl player;

    public PlayerGameOptionsSender(PlayerControl player)
    {
        this.player = player;
    }
    public void SetDirty() => IsDirty = true;

    public override void SendGameOptions()
    {
        if (player.AmOwner)
        {
            var opt = BuildGameOptions();
            foreach (var com in GameManager.Instance.LogicComponents)
            {
                if (com.TryCast<LogicOptions>(out var lo))
                    lo.SetGameOptions(opt);
            }
            GameOptionsManager.Instance.CurrentGameOptions = opt;
        }
        else base.SendGameOptions();
    }

    public override void SendOptionsArray(byte[] optionArray)
    {
        for (byte i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
        {
            if (GameManager.Instance.LogicComponents[i].TryCast<LogicOptions>(out _))
            {
                SendOptionsArray(optionArray, i, player.GetClientId());
            }
        }
    }
    public static void RemoveSender(PlayerControl player)
    {
        var sender = AllSenders.OfType<PlayerGameOptionsSender>()
        .FirstOrDefault(sender => sender.player.PlayerId == player.PlayerId);
        if (sender == null) return;
        sender.player = null;
        AllSenders.Remove(sender);
    }
    public override IGameOptions BuildGameOptions()
    {
        if (Main.RealOptionsData == null)
        {
            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);
        }

        var opt = BasedGameOptions;
        AURoleOptions.SetOpt(opt);
        var state = Main.PlayerStates[player.PlayerId];
        opt.BlackOut(state.IsBlackOut);

        CustomRoles role = player.GetCustomRole();
        switch (role.GetCustomRoleTypes())
        {
            case CustomRoleTypes.Impostor:
                AURoleOptions.ShapeshifterCooldown = Options.DefaultShapeshiftCooldown.GetFloat();
                break;
        }

        switch (role)
        {
            case CustomRoles.Terrorist:
            case CustomRoles.SabotageMaster:
            case CustomRoles.Mario:
                AURoleOptions.EngineerCooldown = 0f;
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.ShapeMaster:
                AURoleOptions.ShapeshifterCooldown = 0f;
                AURoleOptions.ShapeshifterLeaveSkin = false;
                AURoleOptions.ShapeshifterDuration = Options.ShapeMasterShapeshiftDuration.GetFloat();
                break;
            case CustomRoles.Warlock:
                AURoleOptions.ShapeshifterCooldown = Main.isCursed ? 1f : Options.DefaultKillCooldown;
                break;
            case CustomRoles.SerialKiller:
                SerialKiller.ApplyGameOptions(player);
                break;
            case CustomRoles.BountyHunter:
                BountyHunter.ApplyGameOptions();
                break;
            case CustomRoles.Sheriff:
            case CustomRoles.SwordsMan:
            case CustomRoles.Arsonist:
            case CustomRoles.Minimalism:
            case CustomRoles.Innocent:
            case CustomRoles.Pelican:
            case CustomRoles.Revolutionist:
            case CustomRoles.Medicaler:
            case CustomRoles.Provocateur:
                opt.SetVision(false);
                break;
            case CustomRoles.Zombie:
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0.2f);
                break;
            case CustomRoles.Doctor:
                AURoleOptions.ScientistCooldown = 0f;
                AURoleOptions.ScientistBatteryCharge = Options.DoctorTaskCompletedBatteryCharge.GetFloat();
                break;
            case CustomRoles.Mayor:
                AURoleOptions.EngineerCooldown =
                    !Main.MayorUsedButtonCount.TryGetValue(player.PlayerId, out var count) || count < Options.MayorNumOfUseButton.GetInt()
                    ? opt.GetInt(Int32OptionNames.EmergencyCooldown)
                    : 300f;
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Paranoia:
                AURoleOptions.EngineerCooldown =
                    !Main.ParaUsedButtonCount.TryGetValue(player.PlayerId, out var count2) || count2 < Options.ParanoiaNumOfUseButton.GetInt()
                    ? Options.ParanoiaVentCooldown.GetFloat()
                    : 300f;
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Mare:
                Mare.ApplyGameOptions(player.PlayerId);
                break;
            case CustomRoles.EvilTracker:
                EvilTracker.ApplyGameOptions(player.PlayerId);
                break;
            case CustomRoles.Jackal:
                Jackal.ApplyGameOptions(opt);
                break;
            case CustomRoles.Veteran:
                AURoleOptions.EngineerCooldown =
                    !Main.VeteranNumOfUsed.TryGetValue(player.PlayerId, out var count3) || count3 > 0
                    ? Options.VeteranSkillCooldown.GetFloat()
                    : 300f;
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Grenadier:
                AURoleOptions.EngineerCooldown = Options.GrenadierSkillCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.FFF:
                opt.SetVision(true);
                break;
            case CustomRoles.Gamer:
                Gamer.ApplyGameOptions(opt);
                break;
            case CustomRoles.DarkHide:
                DarkHide.ApplyGameOptions(opt);
                break;
            case CustomRoles.Workaholic:
                AURoleOptions.EngineerCooldown = Options.WorkaholicVentCooldown.GetFloat();
                AURoleOptions.EngineerInVentMaxTime = 0f;
                break;
            case CustomRoles.ImperiusCurse:
                AURoleOptions.ShapeshifterCooldown = Options.ImperiusCurseShapeshiftCooldown.GetFloat();
                AURoleOptions.ShapeshifterLeaveSkin = false;
                AURoleOptions.ShapeshifterDuration = Options.ShapeImperiusCurseShapeshiftDuration.GetFloat();
                break;
            case CustomRoles.QuickShooter:
                AURoleOptions.ShapeshifterCooldown = QuickShooter.ShapeshiftCooldown.GetFloat();
                break;
            case CustomRoles.Concealer:
                Concealer.ApplyGameOptions();
                break;
            case CustomRoles.Assassin:
                Assassin.ApplyGameOptions();
                break;
            case CustomRoles.Hacker:
                Hacker.ApplyGameOptions();
                break;
            case CustomRoles.Hangman:
                Hangman.ApplyGameOptions();
                break;
            case CustomRoles.Sunnyboy:
                AURoleOptions.ScientistCooldown = 0f;
                AURoleOptions.ScientistBatteryCharge = 60f;
                break;
            case CustomRoles.BloodKnight:
                BloodKnight.ApplyGameOptions(opt);
                break;
            case CustomRoles.DovesOfNeace:
                AURoleOptions.EngineerCooldown =
                    !Main.DovesOfNeaceNumOfUsed.TryGetValue(player.PlayerId, out var count4) || count4 > 0
                    ? Options.DovesOfNeaceCooldown.GetFloat()
                    : 300f;
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
            case CustomRoles.Rudepeople:
                AURoleOptions.EngineerCooldown =
                    !Main.RudepeopleNumOfUsed.TryGetValue(player.PlayerId, out var count5) || count5 > 0
                    ? Options.RudepeopleSkillCooldown.GetFloat()
                    : 300f;
                AURoleOptions.EngineerInVentMaxTime = 1;
                break;
        }

        // 为迷惑者的凶手
        if (Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Bewilder) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == player.PlayerId && !x.Is(CustomRoles.Hangman)).Count() > 0)
        {
            opt.SetVision(false);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Options.BewilderVision.GetFloat());
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.BewilderVision.GetFloat());
        }
        // 为漫步者的凶手
        if (Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Rambler) && !x.IsAlive() && x.GetRealKiller()?.PlayerId == player.PlayerId).Count() > 0)
        {
            Main.AllPlayerSpeed[player.PlayerId] = Options.RamblerSpeed.GetFloat();
        }

        // 投掷傻瓜蛋啦！！！！！
        if (
            (Main.GrenadierBlinding.Count >= 1 &&
            (player.GetCustomRole().IsImpostor() ||
            (player.GetCustomRole().IsNeutral() && Options.GrenadierCanAffectNeutral.GetBool()))
            ) || (
            Main.MadGrenadierBlinding.Count >= 1 && !player.GetCustomRole().IsImpostorTeam() && !player.Is(CustomRoles.Madmate))
            )
        {
            {
                opt.SetVision(false);
                opt.SetFloat(FloatOptionNames.CrewLightMod, Options.GrenadierCauseVision.GetFloat());
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.GrenadierCauseVision.GetFloat());
            }
        }

        foreach (var subRole in Main.PlayerStates[player.PlayerId].SubRoles)
        {
            switch (subRole)
            {
                case CustomRoles.Watcher:
                    opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                    break;
                case CustomRoles.Flashman:
                    Main.AllPlayerSpeed[player.PlayerId] = Options.FlashmanSpeed.GetFloat();
                    break;
                case CustomRoles.Lighter:
                    opt.SetFloat(FloatOptionNames.CrewLightMod, Options.LighterVision.GetFloat());
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.LighterVision.GetFloat());
                    break;
                case CustomRoles.Bewilder:
                    opt.SetVision(false);
                    opt.SetFloat(FloatOptionNames.CrewLightMod, Options.BewilderVision.GetFloat());
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, Options.BewilderVision.GetFloat());
                    break;
                case CustomRoles.Reach:
                    opt.SetInt(Int32OptionNames.KillDistance, 2);
                    break;
                case CustomRoles.Rambler:
                    Main.AllPlayerSpeed[player.PlayerId] = Options.RamblerSpeed.GetFloat();
                    break;
            }
        }

        // 当工程跳管冷却为0时无法正常显示图标
        AURoleOptions.EngineerCooldown = Mathf.Max(0.01f, AURoleOptions.EngineerCooldown);

        if (Main.AllPlayerKillCooldown.TryGetValue(player.PlayerId, out var killCooldown))
        {
            AURoleOptions.KillCooldown = Mathf.Max(0.01f, killCooldown);
        }

        if (Main.AllPlayerSpeed.TryGetValue(player.PlayerId, out var speed))
        {
            AURoleOptions.PlayerSpeedMod = Mathf.Clamp(speed, Main.MinSpeed, 3f);
        }

        state.taskState.hasTasks = Utils.HasTasks(player.Data, false);
        if (Options.GhostCanSeeOtherVotes.GetBool() && player.Data.IsDead)
            opt.SetBool(BoolOptionNames.AnonymousVotes, false);
        if (Options.AdditionalEmergencyCooldown.GetBool() &&
            Options.AdditionalEmergencyCooldownThreshold.GetInt() <= Utils.AllAlivePlayersCount)
        {
            opt.SetInt(
                Int32OptionNames.EmergencyCooldown,
                Options.AdditionalEmergencyCooldownTime.GetInt());
        }
        if (Options.SyncButtonMode.GetBool() && Options.SyncedButtonCount.GetValue() <= Options.UsedButtonCount)
        {
            opt.SetInt(Int32OptionNames.EmergencyCooldown, 3600);
        }
        MeetingTimeManager.ApplyGameOptions(opt);

        AURoleOptions.ShapeshifterCooldown = Mathf.Max(1f, AURoleOptions.ShapeshifterCooldown);
        AURoleOptions.ProtectionDurationSeconds = 0f;

        return opt;
    }

    public override bool AmValid()
    {
        return base.AmValid() && player != null && !player.Data.Disconnected && Main.RealOptionsData != null;
    }
}