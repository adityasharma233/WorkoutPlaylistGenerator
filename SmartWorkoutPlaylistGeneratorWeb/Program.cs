using SmartWorkoutPlaylistGenerator; 
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<PlaylistManager>(sp => 
    new PlaylistManager(
        "353fa9bbdd0d4fac8fd986eaff5d05d2",
        "4b3e977b71fa491888d15d742a97b90c",
        "AQDgvCAd0NHlyDaIakh3Er4IkVk_gTvVtTYubcDHTWXh-TM8ZsTxy5dtlj6dwh5ArREmPEb9Z3E_1-qtDPGjXDnak_auIdO9eZlINlfeOb2k36qQ0Kecb-AnK-tC8VJaEPE"
    )
);

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.Run();
