using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using CreatorFlow.Api.Authentication;
using CreatorFlow.Api.Endpoints;
using CreatorFlow.Api.ErrorHandling;
using CreatorFlow.Api.Models.Auth;
using CreatorFlow.Api.Repositories.Auth;
using CreatorFlow.Api.Services.Auth;
using CreatorFlow.Api.Tests.Services;
using CreatorFlow.Contracts.Auth;
using CreatorFlow.Contracts.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.Api;

[TestClass]
[TestCategory("AuthApiIntegration")]
public sealed class AuthRoutingTests
{
    [TestMethod]
    public async Task AvatarUploadReplaceAndRemove_AreProtectedAndReturnNormalizedPixels()
    {
        await using var fixture = await Fixture.CreateAsync(); fixture.Repository.User = VerifiedUser();
        var login = await fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("fixture@example.test", "Original1!"));
        var issued = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;
        async Task<HttpResponseMessage> Save(string action, byte[]? bytes = null)
        {
            using var form = new MultipartFormDataContent { { new StringContent("Saved"), "displayName" }, { new StringContent(action), "avatarAction" } };
            if (bytes is not null) form.Add(new ByteArrayContent(bytes), "image", "misleading.txt");
            using var request = new HttpRequestMessage(HttpMethod.Put, "/api/users/me/profile") { Content = form };
            request.Headers.Authorization = new("Bearer", issued.AccessToken);
            return await fixture.Client.SendAsync(request);
        }
        byte[] input = AvatarFixtures.WithMetadata(AvatarFixtures.Encode(1024, 512), false);
        using var saved = await Save("Upload", input); Assert.AreEqual(HttpStatusCode.OK, saved.StatusCode);
        Assert.AreEqual("Saved", (await saved.Content.ReadFromJsonAsync<ProfileResponse>())!.DisplayName);
        Assert.AreEqual(0L, fixture.Repository.User!.TokenVersion);
        using var read = new HttpRequestMessage(HttpMethod.Get, "/api/users/me/avatar"); read.Headers.Authorization = new("Bearer", issued.AccessToken);
        using var downloaded = await fixture.Client.SendAsync(read); Assert.AreEqual("image/png", downloaded.Content.Headers.ContentType!.MediaType);
        byte[] pixels = await downloaded.Content.ReadAsByteArrayAsync(); Assert.IsTrue(pixels.AsSpan().IndexOf(AvatarFixtures.Marker) < 0);
        using var decoded = SkiaSharp.SKBitmap.Decode(pixels); Assert.AreEqual(512, decoded.Width); Assert.AreEqual(256, decoded.Height);
        using var spoofed = await Save("Upload", "not an image"u8.ToArray()); Assert.AreEqual(HttpStatusCode.BadRequest, spoofed.StatusCode);
        CollectionAssert.AreEqual(pixels, fixture.Repository.Avatar!.ImageData);
        using var removed = await Save("Remove"); Assert.AreEqual(HttpStatusCode.OK, removed.StatusCode); Assert.IsNull(fixture.Repository.Avatar);
    }
    [TestMethod]
    public async Task LoginProtectedProfile_RejectsRevokedLockedAndRestoredOldToken()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Repository.User = VerifiedUser();
        var login = await fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("FIXTURE@example.test", "Original1!"));
        Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);
        var response = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;
        Assert.AreEqual(42L, response.CurrentUser.UserId);
        Assert.IsFalse((await login.Content.ReadAsStringAsync()).Contains("passwordHash", StringComparison.OrdinalIgnoreCase));
        using var me = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        me.Headers.Authorization = new("Bearer", response.AccessToken);
        Assert.AreEqual(HttpStatusCode.OK, (await fixture.Client.SendAsync(me)).StatusCode);
        fixture.Repository.User = VerifiedUser(AccountStatus.Locked, 1);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await fixture.GetWithToken(response.AccessToken)).StatusCode);
        fixture.Repository.User = VerifiedUser(AccountStatus.Active, 2);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await fixture.GetWithToken(response.AccessToken)).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await fixture.Client.GetAsync("/api/users/me")).StatusCode);
    }

    [TestMethod]
    public async Task RegisterVerification_NoAutoTokenAndCrossPurposeRejected()
    {
        await using var fixture = await Fixture.CreateAsync();
        var registered = await fixture.Client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Person", "fixture@example.test", "Original1!", "Original1!"));
        Assert.AreEqual(HttpStatusCode.Created, registered.StatusCode);
        Assert.IsNull(fixture.Repository.User!.EmailVerifiedAt);
        Assert.IsTrue(BCrypt.Net.BCrypt.Verify("Original1!", fixture.Repository.User.PasswordHash));
        var pending = await fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("fixture@example.test", "Original1!"));
        Assert.AreEqual(HttpStatusCode.Forbidden, pending.StatusCode);
        Assert.IsFalse((await pending.Content.ReadAsStringAsync()).Contains("accessToken", StringComparison.OrdinalIgnoreCase));
        var requested = await fixture.Client.PostAsJsonAsync("/api/auth/email-verification/requests", new EmailCodeRequest("fixture@example.test"));
        var id = (await requested.Content.ReadFromJsonAsync<VerificationRequestResponse>())!.RequestId;
        var crossed = await fixture.Client.PostAsJsonAsync("/api/auth/password-reset/confirm", new ResetPasswordRequest(id, fixture.Sender.VerificationCode, "Newpass2!", "Newpass2!"));
        Assert.AreEqual(HttpStatusCode.BadRequest, crossed.StatusCode);
        var verified = await fixture.Client.PostAsJsonAsync("/api/auth/email-verification/confirm", new VerifyEmailRequest(id, fixture.Sender.VerificationCode));
        Assert.AreEqual(HttpStatusCode.OK, verified.StatusCode);
        Assert.IsFalse((await verified.Content.ReadAsStringAsync()).Contains("accessToken", StringComparison.OrdinalIgnoreCase));
        var reused = await fixture.Client.PostAsJsonAsync("/api/auth/email-verification/confirm", new VerifyEmailRequest(id, fixture.Sender.VerificationCode));
        Assert.AreEqual(HttpStatusCode.BadRequest, reused.StatusCode);
    }

    [TestMethod]
    public async Task WrongLoginAndUnknownEmail_ShareError_AndSeedHashCannotLogin()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Repository.User = VerifiedUser();
        var wrong = await fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("fixture@example.test", "Wrongpass1!"));
        var unknown = await fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("unknown@example.test", "Wrongpass1!"));
        Assert.AreEqual(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, unknown.StatusCode);
        using var wrongJson = JsonDocument.Parse(await wrong.Content.ReadAsStringAsync());
        using var unknownJson = JsonDocument.Parse(await unknown.Content.ReadAsStringAsync());
        Assert.AreEqual(wrongJson.RootElement.GetProperty("title").GetString(), unknownJson.RootElement.GetProperty("title").GetString());
        fixture.Repository.User = new User { UserId = 42, DisplayName = "Fixture", Email = "fixture@example.test", PasswordHash = "DEV_HASH_FIXTURE", AccountStatus = AccountStatus.Active, EmailVerifiedAt = DateTimeOffset.UtcNow };
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("fixture@example.test", "Original1!"))).StatusCode);
    }

    [TestMethod]
    public async Task ChangePassword_RevokesOldToken_AndRejectsUserIdOverride()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Repository.User = VerifiedUser();
        var login = await fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("fixture@example.test", "Original1!"));
        var issued = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/password")
        { Content = JsonContent.Create(new ChangePasswordRequest("Original1!", "Newpass2!", "Newpass2!")) };
        request.Headers.Authorization = new("Bearer", issued.AccessToken);
        Assert.AreEqual(HttpStatusCode.OK, (await fixture.Client.SendAsync(request)).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await fixture.GetWithToken(issued.AccessToken)).StatusCode);
        var freshLogin = await fixture.Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("fixture@example.test", "Newpass2!"));
        var fresh = (await freshLogin.Content.ReadFromJsonAsync<LoginResponse>())!;
        using var profile = new HttpRequestMessage(HttpMethod.Put, "/api/users/me/profile") { Content = new MultipartFormDataContent
        { { new StringContent("Other"), "displayName" }, { new StringContent("Keep"), "avatarAction" }, { new StringContent("999"), "userId" } } };
        profile.Headers.Authorization = new("Bearer", fresh.AccessToken);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await fixture.Client.SendAsync(profile)).StatusCode);
        Assert.AreEqual(42L, fixture.Repository.User!.UserId);
    }

    private static User VerifiedUser(AccountStatus status = AccountStatus.Active, long version = 0) => new()
    { UserId = 42, Email = "fixture@example.test", DisplayName = "Fixture", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Original1!"), EmailVerifiedAt = DateTimeOffset.UtcNow, AccountStatus = status, TokenVersion = version };

    private sealed class Fixture : IAsyncDisposable
    {
        public required WebApplication App { get; init; }
        public required HttpClient Client { get; init; }
        public required UserAccountFake Repository { get; init; }
        public required TestEmailSender Sender { get; init; }
        public static async Task<Fixture> CreateAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            var repository = new UserAccountFake();
            var sender = new TestEmailSender();
            var protector = new EmailOtpCodeProtector(RandomNumberGenerator.GetBytes(32));
            builder.Services.AddCreatorFlowAuthentication(JwtTests.Configuration());
            builder.Services.AddSingleton<IUserRepository>(repository);
            builder.Services.AddScoped(services => new EmailVerificationService(repository, services.GetRequiredService<CurrentAuthenticatedUser>(), protector, sender, new ImmediateClock()));
            builder.Services.AddScoped(services => new PasswordResetService(repository, services.GetRequiredService<CurrentAuthenticatedUser>(), protector, sender, new ImmediateClock()));
            builder.Services.AddScoped<AuthService>(); builder.Services.AddScoped<UserService>();
            builder.Services.AddSingleton<IAvatarImageProcessor, AvatarImageProcessor>();
            builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow);
            builder.Services.AddProblemDetails(); builder.Services.AddExceptionHandler<ApiExceptionHandler>();
            var app = builder.Build();
            app.UseExceptionHandler(); app.UseStatusCodePages(); app.UseAuthentication(); app.UseAuthorization();
            app.MapAuthEndpoints(); app.MapUserEndpoints();
            await app.StartAsync();
            return new() { App = app, Client = app.GetTestClient(), Repository = repository, Sender = sender };
        }
        public Task<HttpResponseMessage> GetWithToken(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
            request.Headers.Authorization = new("Bearer", token);
            return Client.SendAsync(request);
        }
        public async ValueTask DisposeAsync() { Client.Dispose(); await App.DisposeAsync(); }
    }
}
