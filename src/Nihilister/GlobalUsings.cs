// global using System.Collections.Concurrent;
global using NonBlocking;

// packages
global using Serilog;

// nadekobot
global using Nihilister;
global using Nihilister.Db;
global using Nihilister.Services;
global using Nihilister.Common;
global using Nihilister.Common.Attributes;
global using Nihilister.Extensions;

// discord
global using Discord;
global using Discord.Commands;
global using Discord.Net;
global using Discord.WebSocket;

// aliases
global using GuildPerm = Discord.GuildPermission;
global using ChannelPerm = Discord.ChannelPermission;
global using BotPermAttribute = Discord.Commands.RequireBotPermissionAttribute;
global using LeftoverAttribute = Discord.Commands.RemainderAttribute;
global using TypeReaderResult = Nihilister.Common.TypeReaders.TypeReaderResult;

// non-essential
global using JetBrains.Annotations;

// source gen
global using Cloneable;
