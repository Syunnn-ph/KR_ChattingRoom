//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//builder.Services.AddRazorPages();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseRouting();

//app.UseAuthorization();

//app.MapRazorPages();

//app.Run();

using WebApplication1;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddSignalR();

builder.Services.AddHttpClient<OpenAIService>();

builder.Services.AddScoped<DBHelper>();
//builder.Services.AddSingleton<DBHelper>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();


app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.MapControllerRoute(                 // ✅ 一定要有（你現在缺的通常是這個）
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);

app.MapHub<ChatHub>("/chat");
app.MapGet("/healthz", () => Results.Ok("healthy"));

app.Run();
