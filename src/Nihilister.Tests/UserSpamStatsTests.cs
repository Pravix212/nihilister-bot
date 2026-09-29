using System.Collections.Generic;
using Discord;
using Nihilister.Modules.Administration;
using NSubstitute;
using NUnit.Framework;

namespace Nihilister.Tests;

public class UserSpamStatsTests
{
    private static IUserMessage CreateMessage(string content, params (string Filename, int Size)[] attachments)
    {
        var msg = Substitute.For<IUserMessage>();
        msg.Content.Returns(content);

        var attaches = new List<IAttachment>();
        foreach (var (filename, size) in attachments)
        {
            var att = Substitute.For<IAttachment>();
            att.Filename.Returns(filename);
            att.Size.Returns(size);
            attaches.Add(att);
        }

        msg.Attachments.Returns(attaches);
        return msg;
    }

    [Test]
    public void RepeatedTextSpam_IncrementsCount()
    {
        var msg = CreateMessage("spam");
        var stats = new UserSpamStats(msg);

        stats.ApplyNextMessage(CreateMessage("spam"));
        stats.ApplyNextMessage(CreateMessage("spam"));

        Assert.That(stats.Count, Is.EqualTo(3));
    }

    [Test]
    public void RepeatedAttachmentSpam_IncrementsCount()
    {
        var msg = CreateMessage("", ("image.png", 1024));
        var stats = new UserSpamStats(msg);

        stats.ApplyNextMessage(CreateMessage("", ("image.png", 1024)));
        stats.ApplyNextMessage(CreateMessage("", ("image.png", 1024)));

        Assert.That(stats.Count, Is.EqualTo(3));
    }

    [Test]
    public void DifferentAttachments_ResetsCount()
    {
        var msg = CreateMessage("", ("image.png", 1024));
        var stats = new UserSpamStats(msg);

        stats.ApplyNextMessage(CreateMessage("", ("image.png", 1024)));
        stats.ApplyNextMessage(CreateMessage("", ("other.jpg", 2048)));

        Assert.That(stats.Count, Is.EqualTo(1));
    }
}
