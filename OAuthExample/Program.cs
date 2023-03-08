using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
    })
    .AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = "685359b0-6d2b-4e65-bf5b-a642b032bfc3";
        options.ClientSecret = "eAN8Q~VmRc_OHdpUykewLIJgpSWcHjSJroPFlb2f";
        options.SaveTokens = true;

        options.ClaimActions.Clear();
        options.ClaimActions.MapJsonKey("id", "id");
        options.ClaimActions.MapJsonKey("name", "displayName");
        options.ClaimActions.MapJsonKey("given_name", "givenName");
        options.ClaimActions.MapJsonKey("surname", "surname");
        options.ClaimActions.MapJsonKey("email", "mail");
    })
    .AddOAuth("github", options =>
    {
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.ClientId = "d3862ae60b7a88ce586f";
        options.ClientSecret = "73227eba312dd04c553495fa5957b83656fec5b4";
        options.CallbackPath = "/github-oauth";
        options.SaveTokens = true;

        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        options.TokenEndpoint = "https://github.com/login/oauth/access_token";

        options.UserInformationEndpoint = "https://api.github.com/user";
        
        options.Scope.Add("read:user");
        
        options.ClaimActions.MapJsonKey("id", "id");
        options.ClaimActions.MapJsonKey("name", "login");
        options.ClaimActions.MapJsonKey("email", "email");
        
        options.Events.OnCreatingTicket += async context =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

            using var result = await context.Backchannel.SendAsync(request);
            var user = await result.Content.ReadFromJsonAsync<JsonElement>();
            context.RunClaimActions(user);
        };
    });

builder.Services.AddAuthorization(options =>
{
    
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/token", async (HttpContext context) =>
{
    return Results.Ok(await context.GetTokenAsync("access_token"));
});

app.MapGet("/", (HttpContext context) =>
{
    return Results.Ok(context.User.Claims.Select(claim => new { claim.Type, claim.Value, claim.Issuer }));
}).RequireAuthorization();

app.MapGet("/github-login", () => Results.Challenge(new AuthenticationProperties
{
    RedirectUri = "https://localhost:5001"
}, new List<string> { "github" }));

app.MapGet("/microsoft-login", () => Results.Challenge(new AuthenticationProperties
{
    RedirectUri = "https://localhost:5001"
}, new List<string> { MicrosoftAccountDefaults.AuthenticationScheme }));

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync();
    return Results.Ok();
});

app.Run();