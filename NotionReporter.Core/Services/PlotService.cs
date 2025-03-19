namespace NotionReporter.Core.Services;

using Models;
using ScottPlot;

public static class PlotService
{
    public static void GeneratePlots(List<Member> members, List<Event> events,  string plotFolder)
    {
        var memberPlotData = members
            .Select(m => new MemberPlotData(
                    m.Name ?? "",
                    GetAttendance(m, events,0, 6),
                    GetAttendance(m, events, 6, 12)
                )
            ).ToList();

        PlotAttendance(memberPlotData, plotFolder);
        PlotChangeInAttendance(memberPlotData, plotFolder);
        PlotMembersMeetingAttendance(events, plotFolder);
    }

    private static void PlotAttendance(List<MemberPlotData> members, string plotFolder)
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

        var formattedDate = DateTime.Now.ToString("yyyy-MM-dd");
        
        var filePath = Path.Combine(plotFolder, $"attendance_{formattedDate}.png");
        
        plot.SavePng(filePath, 1800, 800);
    }

    private static void PlotChangeInAttendance(List<MemberPlotData> members, string plotFolder)
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
        
        var formattedDate = DateTime.Now.ToString("yyyy-MM-dd");
        
        var filePath = Path.Combine(plotFolder, $"change_{formattedDate}.png");

        plot.SavePng(filePath, 1800, 800);
    }

    private static void PlotMembersMeetingAttendance(List<Event> events, string plotFolder)
    {
        var plot = new Plot();
        
        // ToDo - should use a tag for this
        var membersMeetings = events
            .Where(x => x.Name?.Contains("Members Meeting") == true)
            .Where(x => x.MembersAttended.Count >0)
            .OrderByDescending(x => x.Date)
            .Reverse()
            .ToList();
        
        var membersAttended = membersMeetings
            .Select((x, i) => new Coordinates(i, x.MembersAttended.Count))
            .ToList();
        
        var ticks = membersMeetings
            .Select((x, i) => new Tick(i, x.Date.ToString("MMM yyyy")))
            .ToArray();
        
        plot.Add.Scatter(membersAttended);
        
        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
        
        plot.XLabel("Month");
        plot.YLabel("Members Attended");
        plot.Title("Members Meeting Attendance");
        
        // Y Axis whole numbers only and start from 0   
        var tickGen = new ScottPlot.TickGenerators.NumericAutomatic{ IntegerTicksOnly = true };
        plot.Axes.Left.TickGenerator = tickGen;
        
        var maxAttendance = membersMeetings.Select(x => x.MembersAttended.Count).Max();
        plot.Axes.SetLimits(0, membersMeetings.Count, 0, maxAttendance);
        
        var formattedDate = DateTime.Now.ToString("yyyy-MM-dd");
        
        var filePath = Path.Combine(plotFolder, $"members_meeting_{formattedDate}.png");
        
        plot.SavePng(filePath, 1800, 800);
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

    private static int GetAttendance(Member member, List<Event> events, int weeksAgoStart, int weeksAgoEnd)
    {
        var start = DateTime.Now.AddDays(-(7 * weeksAgoStart));
        var end = DateTime.Now.AddDays(-(7 * weeksAgoEnd));

        return member.EventsAttended?
            .Select(e => events.FirstOrDefault(x => x.Id == e))
            .Count(e => e?.Date >= end && e?.Date < start) ?? 0;
    }
}
