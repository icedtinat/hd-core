using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using SharpAstrology.Ephemerides;
using SharpAstrology.DataModels;
using SharpAstrology.Enums;   // Channels、Planets

class Program
{
    /* ---------- 9 大能量中心 ---------- */
    static readonly string[] AllCenters =
    {
        "Head", "Ajna", "Throat", "G", "Ego",
        "Solar Plexus", "Sacral", "Spleen", "Root"
    };

    /* ---------- Gate → Center (64 门完整映射) ---------- */
    static readonly Dictionary<int,string> GateCenter = new()
    {
        [ 1]="G",[ 2]="G",[ 3]="Sacral",[ 4]="Ajna",[ 5]="Sacral",[ 6]="Solar Plexus",
        [ 7]="G",[ 8]="Throat",[ 9]="Sacral",[10]="G",[11]="Ajna",[12]="Throat",
        [13]="G",[14]="Sacral",[15]="G",[16]="Throat",[17]="Ajna",[18]="Spleen",
        [19]="Root",[20]="Throat",[21]="Ego",[22]="Solar Plexus",[23]="Throat",[24]="Ajna",
        [25]="G",[26]="Ego",[27]="Sacral",[28]="Spleen",[29]="Sacral",[30]="Solar Plexus",
        [31]="Throat",[32]="Spleen",[33]="Throat",[34]="Sacral",[35]="Throat",[36]="Solar Plexus",
        [37]="Solar Plexus",[38]="Root",[39]="Root",[40]="Ego",[41]="Root",[42]="Sacral",
        [43]="Ajna",[44]="Spleen",[45]="Throat",[46]="G",[47]="Ajna",[48]="Spleen",
        [49]="Solar Plexus",[50]="Spleen",[51]="Ego",[52]="Root",[53]="Root",[54]="Root",
        [55]="Solar Plexus",[56]="Throat",[57]="Spleen",[58]="Root",[59]="Sacral",[60]="Root",
        [61]="Head",[62]="Throat",[63]="Head",[64]="Head"
    };

    static void Main()
    {
        /* ★★★ 改这两行即可换测试对象 ★★★ */
        var localBirth = new DateTime(1999, 5, 2, 14, 0, 0, DateTimeKind.Unspecified);
        const string tzId = "Asia/Shanghai";

        /* ---------- 本地 → UTC ---------- */
        var utc = TimeZoneInfo.ConvertTimeToUtc(
            localBirth,
            TimeZoneInfo.FindSystemTimeZoneById(tzId));

        using var eph = new SwissEphemeridesService("ephe").CreateContext();
        var chart = new HumanDesignChart(utc, eph);

        /* ---------- 基本信息 ---------- */
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"生日（当地） : {localBirth:yyyy-MM-dd HH:mm}  {tzId}");
        Console.WriteLine($"生日（UTC）  : {utc:u}\n");
        Console.ResetColor();

        Console.WriteLine($"类型 (Type)              : {chart.Type}");
        Console.WriteLine($"人生角色 (Profile)        : {chart.Profile}");
        Console.WriteLine($"策略 / 权威 (Strategy)    : {chart.Strategy}");
        Console.WriteLine($"分裂定义 (SplitDef)       : {chart.SplitDefinition}");
        Console.WriteLine($"主十字 (IncarnationCross) : {chart.IncarnationCross}\n");

        /* ---------- 已 / 未定义中心 ---------- */
        var channelRx = new Regex(@"Key(\d{1,2})Key(\d{1,2})");

        // 先映射成 List<string>，再 SelectMany 展平
        var centersPerChannel = chart.ActiveChannels
                                     .Select(ch =>
                                     {
                                         var m = channelRx.Match(ch.ToString());
                                         var list = new List<string>();
                                         if (m.Success)
                                         {
                                             if (GateCenter.TryGetValue(int.Parse(m.Groups[1].Value), out var c1))
                                                 list.Add(c1);
                                             if (GateCenter.TryGetValue(int.Parse(m.Groups[2].Value), out var c2))
                                                 list.Add(c2);
                                         }
                                         return list;
                                     });

        var definedCenters = centersPerChannel
                             .SelectMany(c => c)            // 展平成单序列
                             .Distinct()
                             .OrderBy(c => c)
                             .ToArray();

        var undefinedCenters = AllCenters.Except(definedCenters).OrderBy(c => c);

        Console.WriteLine("✅ 已定义能量中心:");
        Console.WriteLine(string.Join("、", definedCenters));
        Console.WriteLine("\n⬜ 未定义能量中心:");
        Console.WriteLine(string.Join("、", undefinedCenters));
        Console.WriteLine();

        /* ---------- 已定义通道 ---------- */
        Console.WriteLine("🔗 已定义通道 (双向打通):");
        if (chart.ActiveChannels.Any())
        {
            foreach (var ch in chart.ActiveChannels.OrderBy(x => x.ToString()))
            {
                var m = channelRx.Match(ch.ToString());
                if (m.Success)
                    Console.WriteLine($"  • Gate {m.Groups[1].Value} ↔ Gate {m.Groups[2].Value}");
                else
                    Console.WriteLine($"  • {ch}");
            }
        }
        else
            Console.WriteLine("  (无完整通道)");
        Console.WriteLine();

        /* ---------- 行星激活 ---------- */
        var gateLineRx = new Regex(@"^(\d{1,2})\.(\d)");

        void Dump(string title, IDictionary<Planets, Activation> dict)
        {
            Console.WriteLine(title);
            foreach (var (planet, act) in dict)
            {
                var m = gateLineRx.Match(act.ToString());
                string gate = m.Success ? m.Groups[1].Value : "??";
                string line = m.Success ? m.Groups[2].Value : "?";
                Console.WriteLine($"  • {planet,-10} → Gate {gate}.{line}");
            }
            Console.WriteLine();
        }

        Dump("🔴 Design（红）：",      chart.DesignActivation);
        Dump("⚫ Personality（黑）：", chart.PersonalityActivation);

        /* ---------- Incarnation Cross 四基石 Gate ---------- */
        int GateOf(Activation act)
        {
            var m = gateLineRx.Match(act.ToString());
            return m.Success ? int.Parse(m.Groups[1].Value) : -1;
        }

        Console.WriteLine("Incarnation Cross 组成 Gate:");
        Console.WriteLine($"  • 太阳   Personality → Gate {GateOf(chart.PersonalityActivation[Planets.Sun])}");
        Console.WriteLine($"  • 地球   Personality → Gate {GateOf(chart.PersonalityActivation[Planets.Earth])}");
        Console.WriteLine($"  • 太阳   Design      → Gate {GateOf(chart.DesignActivation[Planets.Sun])}");
        Console.WriteLine($"  • 地球   Design      → Gate {GateOf(chart.DesignActivation[Planets.Earth])}");
    }
}






