namespace NotionReporter.Core.Services;

using Models;
using ScottPlot;

public static class PlotService
{
    public static void GeneratePlots(List<Member> members)
    {
        var memberPlotData = members
            .Select(m => new MemberPlotData(
                    m.Name ?? "",
                    GetAttendance(m, 0, 6),
                    GetAttendance(m, 6, 12)
                )
            ).ToList();

        PlotAttendance(memberPlotData);
        PlotChangeInAttendance(memberPlotData);
    }

    private static void PlotAttendance(List<MemberPlotData> members)
    {
        members = members
            .OrderByDescending(x => x.PastSixWeeks + x.PrevSixWeeks)
            .ToList();

        var plot = new Plot();

        string[] categoryNames =["Past 6 Weeks", "Previous 6 Weeks"];
        Color[] categoryColors ={ Colors.C0, Colors.C1 };

        // Generate Bars
        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];
            int[] values =[member.PastSixWeeks, member.PrevSixWeeks];
            double nextBarBase = 0;

            for (var j = 0; j < 2; j++)
            {
                Bar bar = new(){
                    Value = nextBarBase + values[j], FillColor = categoryColors[j], ValueBase = nextBarBase, Position = i,
                };

                plot.Add.Bar(bar);

                nextBarBase += values[j];
            }
        }

        // Populate Axis
        ScottPlot.TickGenerators.NumericManual tickGen = new();
        for (var i = 0; i < members.Count; i++)
        {
            tickGen.AddMajor(i, members[i].Name ?? "");
        }

        plot.Axes.Bottom.TickGenerator = tickGen;
        plot.FormatAxes();


        // display groups in the legend
        for (var i = 0; i < 2; i++)
        {
            LegendItem item = new(){ LabelText = categoryNames[i], FillColor = categoryColors[i] };
            plot.Legend.ManualItems.Add(item);
        }

        plot.Legend.Orientation = Orientation.Horizontal;
        plot.ShowLegend(Alignment.UpperRight);

        // tell the plot to autoscale with no padding beneath the bars
        plot.Axes.Margins(bottom: 0, top: .3);
        
        plot.YLabel("Events Attended");
        plot.Title("Events Attended by Members");

        // ToDo: Appsetting this
        plot.SavePng("C:\\Users\\StephenCripps\\Desktop\\NotionReports\\attendance.png", 1800, 800);
    }

    private static void PlotChangeInAttendance(List<MemberPlotData> members)
    {
        members = members
            .OrderByDescending(x => x.PastSixWeeks - x.PrevSixWeeks)
            .ToList();

        var plot = new Plot();
        IPalette amberPalette = new ScottPlot.Palettes.Amber();
        IPalette frostPalette = new ScottPlot.Palettes.Frost();
        
        double minValue = members.Min(x => x.PastSixWeeks - x.PrevSixWeeks);
        double maxValue = members.Max(x => x.PastSixWeeks - x.PrevSixWeeks);

        // Generate Bars
        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];

            var value = member.PastSixWeeks - member.PrevSixWeeks;

            // Map the value to a color in the palette
            var normalizedValue = (value - minValue) / (maxValue - minValue);
            var colorIndex = (int)(normalizedValue * 4); 
            
            var barColor = value < 0 ? amberPalette.GetColor(colorIndex) : frostPalette.GetColor(colorIndex);
            
            Bar bar = new(){ Value = value, Position = i, FillColor = barColor };

            plot.Add.Bar(bar);
        }

        ScottPlot.TickGenerators.NumericManual tickGen = new();
        for (var i = 0; i < members.Count; i++)
        {
            tickGen.AddMajor(i, members[i].Name ?? "");
        }

        plot.Axes.Bottom.TickGenerator = tickGen;
        plot.FormatAxes();

        plot.Axes.Margins(bottom: .3, top: .3);
        
        plot.YLabel("Change in Attendance");
        plot.Title("Change in Attendance");

        plot.SavePng("C:\\Users\\StephenCripps\\Desktop\\NotionReports\\change.png", 1800, 800);
    }

    private static void FormatAxes(this Plot plot)
    {
        plot.XLabel("Members");

        plot.Axes.Bottom.TickLabelStyle.Rotation = -45;
        plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleRight;
        plot.Axes.Bottom.MinimumSize = 200;
        
        var tickGen = new ScottPlot.TickGenerators.NumericAutomatic{ IntegerTicksOnly = true };
        plot.Axes.Left.TickGenerator  = tickGen;
    }

    private static int GetAttendance(Member member, int weeksAgoStart, int weeksAgoEnd)
    {
        var start = DateTime.Now.AddDays(-(7 * weeksAgoStart));
        var end = DateTime.Now.AddDays(-(7 * weeksAgoEnd));

        return member.EventsAttended?
            .Where(e => e?.Date >= end && e?.Date < start)
            .Count() ?? 0;
    }
}
