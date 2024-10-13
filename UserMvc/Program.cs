using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Serilog;
using UserMvc.Jobs;
using UserMvc.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
// Bind SmtpOptions from appsettings.json
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("SmtpOptions"));
// Add Serilog logging
builder.Host.UseSerilog((context, config) =>
{
    config.WriteTo.Console();
    config.WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);
});
// Configure Quartz job
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();
    var jobKey = new JobKey("EmailJob", "group1");
    q.AddJob<EmailJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey) // Link to the EmailJob
        .WithIdentity("EmailJob-trigger", "group1") // Trigger name
        .StartNow() // Start immediately
        .WithSimpleSchedule(x => x
            .WithIntervalInMinutes(3) // Runs every 3 minutes
            .RepeatForever()));

    // Add job listener to track job status
    q.AddJobListener<JobTrackingListener>();
});

// Quartz Hosted service
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Lifetime.ApplicationStarted.Register(async () =>
{
    var schedulerFactory = app.Services.GetRequiredService<ISchedulerFactory>();
    var scheduler = await schedulerFactory.GetScheduler();
    await scheduler.Start();
});

app.Run();
