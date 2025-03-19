// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotionReporter.Core.Services;

// ToDo: UI 
// ToDo: Refine the queries to Notion

var serviceProvider = new ServiceCollection()
    .AddSingleton<IConfiguration>(new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build())
    .BuildServiceProvider();

var config = serviceProvider.GetService<IConfiguration>() ?? throw new ArgumentNullException($"Configuration");

var integrationSecret = config.GetValue<string>("IntegrationSecret") ?? throw new ArgumentNullException($"IntegrationSecret");
var membersPageId = config.GetValue<string>("MembersPageId") ?? throw new ArgumentNullException($"MembersPageId");
var eventsPageId = config.GetValue<string>("EventsPageId") ?? throw new ArgumentNullException($"EventsPageId");
var plotFolder = config.GetValue<string>("PlotFolder") ?? throw new ArgumentNullException($"PlotFolder");

var notionService = new NotionService(integrationSecret, membersPageId, eventsPageId);

var members = await notionService.GetMembers();
var events = await notionService.GetEvents();

PlotService.GeneratePlots(members, events, plotFolder);

Console.WriteLine("Done");


// Rough thing to get counts of crossing campaign attnedance
members.Select(m => new
{
    m.Name,
    EventsCount = m.EventsAttended
        .Select(e => events.FirstOrDefault(ev => ev.Id == e))
        .Count(e => e.Tags.Contains("Crossings Campaign"))
}).Where(e => e.EventsCount > 0)
    .OrderByDescending(m => m.EventsCount)
    .ToList()
    .ForEach(m => Console.WriteLine($"{m.Name} - {m.EventsCount}"));