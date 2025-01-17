﻿using GrillBot.Cache.Services.Managers;
using GrillBot.Common.Extensions;
using GrillBot.Common.Extensions.Discord;
using GrillBot.Common.FileStorage;
using GrillBot.Data.Resources.Peepoangry;
using ImageMagick;

namespace GrillBot.App.Services.Images;

public sealed class PeepoangryRenderer : RendererBase, IDisposable
{
    private MagickImage AngryPeepo { get; }

    public PeepoangryRenderer(FileStorageFactory fileStorageFactory, ProfilePictureManager profilePictureManager)
        : base(fileStorageFactory, profilePictureManager)
    {
        AngryPeepo = new MagickImage(PeepoangryResources.peepoangry);
    }

    public async Task<string> RenderAsync(IUser user, IGuild guild)
    {
        var filename = user.CreateProfilePicFilename(64);
        var file = await Cache.GetFileInfoAsync("Peepoangry", filename);

        if (file.Exists)
            return file.FullName;

        var profilePicture = await ProfilePictureManager.GetOrCreatePictureAsync(user, 64);
        if (profilePicture.IsAnimated && !CanProcessGif(profilePicture, guild))
        {
            filename = Path.ChangeExtension(filename, ".png");
            file = await Cache.GetFileInfoAsync("Peepoangry", filename);
            if (file.Exists)
                return file.FullName;
        }

        using var originalImage = new MagickImageCollection(profilePicture.Data);

        if (file.Extension == ".gif")
        {
            using var collection = new MagickImageCollection();

            foreach (var userFrame in originalImage)
            {
                userFrame.RoundImage();
                var frame = RenderFrame(userFrame);

                frame.AnimationDelay = userFrame.AnimationDelay;
                frame.GifDisposeMethod = GifDisposeMethod.Background;
                collection.Add(frame);
            }

            collection.Coalesce();
            await collection.WriteAsync(file.FullName, MagickFormat.Gif);
        }
        else
        {
            var avatarFrame = originalImage[0];
            avatarFrame.RoundImage();

            using var frame = RenderFrame(avatarFrame);
            await frame.WriteAsync(file.FullName, MagickFormat.Png);
        }

        return file.FullName;
    }

    private IMagickImage<byte> RenderFrame(IMagickImage<byte> avatarFrame)
    {
        var image = new MagickImage(MagickColors.Transparent, 250, 105);

        new Drawables()
            .Composite(20, 10, CompositeOperator.Over, avatarFrame)
            .Composite(115, -5, CompositeOperator.Over, AngryPeepo)
            .Draw(image);

        return image;
    }

    public void Dispose()
    {
        AngryPeepo?.Dispose();
    }
}
