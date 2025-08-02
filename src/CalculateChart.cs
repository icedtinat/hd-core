using SharpAstrology.Ephemerides;
using SharpAstrology.DataModels;
using SharpAstrology.ExtensionMethods;
using SharpAstrology.Enums;
using System;
using System.Linq;

public static class CalculateChart
{
    public static ChartDTO Calculate(DateTime localTime, string tzId)
    {
        // ① 本地 ➜ UTC
        var tz  = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var utc = TimeZoneInfo.ConvertTimeToUtc(localTime, tz);

        // ② 计算人类图
        using var eph = new SwissEphemeridesService("ephe").CreateContext();
        var chart = new HumanDesignChart(utc, eph);

        // ③ 将 Gates enum 转 int (枚举值本身就是门号)
        int[] gateNumbers = chart.ActiveGates.Select(g => (int)g).OrderBy(n => n).ToArray();

        return new ChartDTO(
            chart.Type.ToString(),
            chart.Profile.ToString(),
            gateNumbers,
            chart.ActiveChannels.Select(c => c.ToString()).ToArray(),
            Array.Empty<string>()   // 先留空，后续可用 Gate→Center 字典转换
        );
    }
}
