using ScottPlot.Plottables;

namespace NotionReporter.Core.Services;

using Models;
using ScottPlot;

// This is WET as hell - partly because I'm lazy, partly because I want to be able to tweak plots without messing with others. 
// it could be refactored for sure.... but who's gonna do that? 
public static class PlotService
{
    public static void GeneratePlots(List<Member> members, List<Event> events, string plotFolder)
    {
        var memberPlotData = members
            .Select(m => new MemberPlotData(
                    m.Name ?? "",
                    GetAttendance(m, events, 0, 6),
                    GetAttendance(m, events, 6, 12)
                )
            ).ToList();

        PlotAttendance(memberPlotData, plotFolder);
        PlotChangeInAttendance(memberPlotData, plotFolder);
        PlotMembersMeetingAttendance(events, plotFolder);
        PlotAttendanceByType(events, plotFolder);
        PlotAttendanceByGender(events, plotFolder, members);
    }

    private static void PlotAttendance(List<MemberPlotData> members, string plotFolder)
    {
        members = members
            .OrderByDescending(x => x.PastSixWeeks + x.PrevSixWeeks)
            .ToList();

        var plot = new Plot();

        string[] categoryNames = ["Past 6 Weeks", "Previous 6 Weeks"];
        Color[] categoryColors = {Colors.C0, Colors.C1};

        // Generate Bars
        for (var i = 0; i < members.Count; i++)
        {
            var member = members[i];
            int[] values = [member.PastSixWeeks, member.PrevSixWeeks];
            double nextBarBase = 0;

            for (var j = 0; j < 2; j++)
            {
                Bar bar = new()
                {
                    Value = nextBarBase + values[j], FillColor = categoryColors[j], ValueBase = nextBarBase,
                    Position = i,
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
            LegendItem item = new() {LabelText = categoryNames[i], FillColor = categoryColors[i]};
            plot.Legend.ManualItems.Add(item);
        }

        plot.Legend.Orientation = Orientation.Horizontal;
        plot.ShowLegend(Alignment.UpperRight);

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
            .Where(x => x.PastSixWeeks + x.PrevSixWeeks > 0)
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
            var colorIndex = (int) (normalizedValue * 4);

            var barColor = value < 0 ? amberPalette.GetColor(colorIndex) : frostPalette.GetColor(colorIndex);

            Bar bar = new() {Value = value, Position = i, FillColor = barColor};

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
            .Where(x => x.Tags.Contains("Members Meeting"))
            .Where(x => x.MembersAttended.Count > 0)
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
        var tickGen = new ScottPlot.TickGenerators.NumericAutomatic {IntegerTicksOnly = true};
        plot.Axes.Left.TickGenerator = tickGen;

        var maxAttendance = membersMeetings.Select(x => x.MembersAttended.Count).Max();
        plot.Axes.SetLimits(0, membersMeetings.Count, 0, maxAttendance);

        var formattedDate = DateTime.Now.ToString("yyyy-MM-dd");

        var filePath = Path.Combine(plotFolder, $"members_meeting_{formattedDate}.png");

        plot.SavePng(filePath, 1800, 800);
    }

    private static void PlotAttendanceByType(List<Event> events, string plotFolder)
    {
        var filteredEvents = events.Where(e => e.MembersAttended.Any()
                                               && e.Date > DateTime.Today.AddDays(-90)
                                               && e.Date <= DateTime.Now).ToList();
        var xs = filteredEvents.Select(e => e.Date.ToOADate()).ToArray();
        var ys = filteredEvents.Select(e => (double) e.MembersAttended.Count).ToArray();

        var plot = new ScottPlot.Plot();

        // Helps us to avoid overlapping labels
        var takenXs = new List<Double>();

        for (var i = 0; i < xs.Length; i++)
        {
            var x = xs[i];

            while (takenXs.Contains(x))
            {
                x++;
            }

            takenXs.Add(x);

            var y = ys[i];

            var bar = plot.Add.Bar(x, y);
            bar.Color = TagsToColour(filteredEvents[i].Tags);

            var text = plot.Add.Text(filteredEvents[i].Name ?? "", x - 0.5, y + 1);
            text.LabelRotation = -60;
        }

        AddTagsLegend(plot);

        plot.Axes.DateTimeTicksBottom();

        plot.Title("Event Attendance Over Time");
        plot.YLabel("Number of Attendees");
        plot.XLabel("Date");

        plot.Axes.Margins(bottom: 0, top: .3);

        var formattedDate = DateTime.Now.ToString("yyyy-MM-dd");
        var filePath = Path.Combine(plotFolder, $"events_timeline_{formattedDate}.png");

        plot.SavePng(filePath, 1800, 800);
    }

    private static void PlotAttendanceByGender(List<Event> events, string plotFolder, List<Member> members)
    {
        var filteredEvents = events
            .Where(e => e.MembersAttended.Any()
                                               && e.Date > DateTime.Today.AddDays(-90)
                                               && e.Date <= DateTime.Now)
            .OrderBy(e => e.Date)
            .ToList();
        var xs = filteredEvents.Select(e => e.Date.ToOADate()).ToArray();

        var malesCount = filteredEvents.Select(e => GetAttendanceByGender(e, Gender.Male, members)).ToArray();
        var femalesCount = filteredEvents.Select(e => GetAttendanceByGender(e, Gender.Female, members)).ToArray();
        var nbsCount = filteredEvents.Select(e => GetAttendanceByGender(e, Gender.NonBinary, members)).ToArray();
        var unknownsCount = filteredEvents.Select(e => GetAttendanceByGender(e, Gender.Unknown, members)).ToArray();

        var maleBars = new List<Bar>();
        var femaleBars = new List<Bar>();
        var nbBars = new List<Bar>();
        var unknownBars = new List<Bar>();
        var borderbars = new List<Bar>();
        var labels = new List<Text>();

        var plot = new Plot();

        const double width = 0.4;

        // Helps us to avoid overlapping labels
        var prevx = 0.0;

        for (var i = 0; i < xs.Length; i++)
        {
            var x = xs[i];

            if (x < prevx +2)
            {
                x = prevx + 2;
            }
            
            prevx = x;

            var males = malesCount[i];
            var bar = new Bar
            {
                Position = x - width,
                Value = malesCount[i],
                Size = width,
                FillColor = Colors.C0
            };
            maleBars.Add(bar);

            var females = femalesCount[i];
            bar = new Bar()
            {
                Position = x,
                Value = females,
                Size = width,
                FillColor = Colors.C1
            };
            femaleBars.Add(bar);

            var nbs = nbsCount[i];
            bar = new Bar()
            {
                Position = x + width,
                Value = nbs,
                Size = width,
                FillColor = Colors.C2
            };
            nbBars.Add(bar);

            var unknowns = unknownsCount[i];
            bar = new Bar()
            {
                Position = x + 2 * width,
                Value = unknowns,
                Size = width,
                FillColor = Colors.C3
            };
            unknownBars.Add(bar);

            // get higher of the  bars
            var max = new[] {males, females, nbs, unknowns}.Max();

            bar = new Bar()
            {
                Position = x + 0.15,
                Value = max,
                Size = width * 4,
                FillColor = Colors.Beige
            };
            borderbars.Add(bar);

            var text = new Text
            {
                Location = new Coordinates(x - width, max + 0.5),
                LabelText = filteredEvents[i].Name ?? "",
            };

            labels.Add(text);
        }

        // Plot after everything's been calculated so I can control the layers
        plot.Add.Bars(borderbars);
        plot.Add.Bars(maleBars);
        plot.Add.Bars(femaleBars);
        plot.Add.Bars(nbBars);
        plot.Add.Bars(unknownBars);

        // Wonky, but I'm tired- I can't seem to add a list of labels like I could with the bars
        labels.ForEach(l =>
        {
            var label = plot.Add.Text(l.LabelText, l.Location);
            label.LabelRotation = -60;
        });

        AddGenderLegend(plot);

        plot.Axes.DateTimeTicksBottom();

        plot.Title("Event Attendance Over Time");
        plot.YLabel("Number of Attendees");
        plot.XLabel("Date");

        plot.Axes.Margins(bottom: 0, top: .3);

        var formattedDate = DateTime.Now.ToString("yyyy-MM-dd");
        var filePath = Path.Combine(plotFolder, $"events_timeline_by_gender{formattedDate}.png");

        plot.SavePng(filePath, 1800, 800);
    }

    private static int GetAttendanceByGender(Event ev, Gender gender, List<Member> members)
    {
        return ev.MembersAttended
            .Select(x => members.Single(m => m.Id == x))
            .Count(m => m.Gender == gender);
    }

    private static void FormatAxes(this Plot plot)
    {
        plot.XLabel("Members");

        plot.Axes.Bottom.TickLabelStyle.Rotation = -45;
        plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleRight;
        plot.Axes.Bottom.MinimumSize = 200;

        var tickGen = new ScottPlot.TickGenerators.NumericAutomatic {IntegerTicksOnly = true};
        plot.Axes.Left.TickGenerator = tickGen;
    }

    private static int GetAttendance(Member member, List<Event> events, int weeksAgoStart, int weeksAgoEnd)
    {
        var start = DateTime.Now.AddDays(-(7 * weeksAgoStart));
        var end = DateTime.Now.AddDays(-(7 * weeksAgoEnd));

        return member.EventsAttended?
            .Select(e => events.FirstOrDefault(x => x.Id == e))
            .Count(e => e?.Date >= end && e?.Date < start) ?? 0;
    }

    private static Color TagsToColour(List<string> tags)
    {
        if (tags.Contains("Action"))
        {
            return Colors.C0;
        }

        if (tags.Contains("Outreach"))
        {
            return Colors.C1;
        }

        if (tags.Contains("Meeting"))
        {
            return Colors.C2;
        }

        if (tags.Contains("Social"))
        {
            return Colors.C3;
        }

        if (tags.Contains("Training"))
        {
            return Colors.C4;
        }

        return Colors.C5;
    }

    private static void AddTagsLegend(Plot plot)
    {
        plot.Legend.IsVisible = true;

        List<LegendItem> items =
        [
            new() {LabelText = "Action", FillColor = Colors.C0},
            new() {LabelText = "Outreach", FillColor = Colors.C1},
            new() {LabelText = "Meeting", FillColor = Colors.C2},
            new() {LabelText = "Social", FillColor = Colors.C3},
            new() {LabelText = "Training", FillColor = Colors.C4},
            new() {LabelText = "Other", FillColor = Colors.C5},
        ];

        plot.ShowLegend(items, Alignment.UpperRight);
    }

    private static void AddGenderLegend(Plot plot)
    {
        plot.Legend.IsVisible = true;

        List<LegendItem> items =
        [
            new() {LabelText = "Male", FillColor = Colors.C0},
            new() {LabelText = "Female", FillColor = Colors.C1},
            new() {LabelText = "Non-Binary", FillColor = Colors.C2},
            new() {LabelText = "Unknown", FillColor = Colors.C3},
        ];

        plot.ShowLegend(items, Alignment.UpperRight);
    }
}