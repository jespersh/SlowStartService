using SlowStartService;

// Signal Windows SCM as early as possible
EarlyWindowsService earlyWindowsService = new EarlyWindowsService();
earlyWindowsService.Start();

// Create builder as normal
var webApplicationOptions = new WebApplicationOptions()
{
    ContentRootPath = AppContext.BaseDirectory,
    Args = args,
    ApplicationName = System.Diagnostics.Process.GetCurrentProcess().ProcessName
};
var builder = WebApplication.CreateBuilder(webApplicationOptions);
builder.Host.UseWindowsService();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton(earlyWindowsService);
builder.Services.AddSingleton<IHostLifetime, PrimaryModeServiceLifetime>();

// Simulating a slow service. Not uncommon on boot 
await Task.Delay(TimeSpan.FromMinutes(2));

// Regular example below
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
