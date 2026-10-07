namespace CreatorFlow.Api.Models.Auth;

public sealed record UserAvatar(byte[] ImageData, int Width, int Height);

public enum AvatarChange { Keep, Url, Upload, Remove }
