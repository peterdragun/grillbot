﻿using System.Net.Http;
using GrillBot.Common.Models;
using GrillBot.Data.Models.API;

namespace GrillBot.App.Actions.Api.V1.Auth;

public class ProcessCallback : ApiAction
{
    private IConfiguration Configuration { get; }
    private HttpClient HttpClient { get; }

    public ProcessCallback(ApiRequestContext apiContext, IConfiguration configuration, IHttpClientFactory httpClientFactory) : base(apiContext)
    {
        Configuration = configuration.GetRequiredSection("Auth:OAuth2");
        HttpClient = httpClientFactory.CreateClient();
    }

    public async Task<string> ProcessAsync(string code, string encodedState)
    {
        var state = AuthState.Decode(encodedState);
        var accessToken = await RetrieveAccessTokenAsync(code);
        var returnUrl = GetReturnUrl(state);

        var uriBuilder = new UriBuilder(returnUrl)
        {
            Query = string.Join("&", $"sessionId={accessToken}", $"isPublic={state.IsPublic}")
        };

        return uriBuilder.ToString();
    }

    private async Task<string> RetrieveAccessTokenAsync(string code)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", Configuration["ClientId"] },
                { "client_secret", Configuration["ClientSecret"] },
                { "grant_type", "authorization_code" },
                { "code", code },
                { "scope", "identify" },
                { "redirect_uri", Configuration["RedirectUrl"] }
            })
        };

        using var response = await HttpClient.SendAsync(message);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new WebException(json);

        return JObject.Parse(json)["access_token"]!.ToString();
    }

    private string GetReturnUrl(AuthState state)
        => !string.IsNullOrEmpty(state.ReturnUrl) ? state.ReturnUrl : Configuration[state.IsPublic ? "ClientRedirectUrl" : "AdminRedirectUrl"];
}
