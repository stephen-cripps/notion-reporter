// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotionReporter.Core.Services;

// ToDo: Get Notion Data
// ToDo: Generate CSVs
// ToDo: Generate Graphs? 
// ToDo: Report Customisation UI? 
// ToDo: DI? 

var serviceProvider = new ServiceCollection()
    .AddSingleton<IConfiguration>(new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build())
    .BuildServiceProvider();

var config = serviceProvider.GetService<IConfiguration>() ?? throw new ArgumentNullException($"Configuration");

var integrationSecret = config.GetValue<string>("IntegrationSecret") ?? throw new ArgumentNullException($"IntegrationSecret");
var membersPageId = config.GetValue<string>("MembersPageId") ?? throw new ArgumentNullException($"MembersPageId");

var notionService = new NotionService(integrationSecret, membersPageId);

var members = await notionService.GetMembers();

foreach (var member in members)
{
    Console.WriteLine(member.Name);
}
