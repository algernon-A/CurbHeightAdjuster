// <copyright file="WhatsNewMessageListing.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace CurbHeightAdjuster
{
    using System;
    using AlgernonCommons.Notifications;

    /// <summary>
    /// "What's new" update messages.
    /// </summary>
    internal class WhatsNewMessageListing
    {
        /// <summary>
        /// Gets the list of versions and associated update message lines (as translation keys).
        /// </summary>
        internal WhatsNewMessage[] Messages => new WhatsNewMessage[]
        {
            new WhatsNewMessage
            {
                Version = new Version("1.6.0.0"),
                MessagesAreKeys = true,
                Messages = new string[]
                {
                    "CHA_160_0",
                    "CHA_160_1",
                },
            },
            new WhatsNewMessage
            {
                Version = new Version("1.5.2.0"),
                MessagesAreKeys = true,
                Messages = new string[]
                {
                    "CHA_152_0",
                    "CHA_152_1",
                },
            },
            new WhatsNewMessage
            {
                Version = new Version("1.5.1.0"),
                MessagesAreKeys = true,
                Messages = new string[]
                {
                    "CHA_151_0",
                },
            },
            new WhatsNewMessage
            {
                Version = new Version("1.5.0.0"),
                MessagesAreKeys = true,
                Messages = new string[]
                {
                    "CHA_150_0",
                    "CHA_150_1",
                    "CHA_150_2",
                },
            },
            new WhatsNewMessage
            {
                Version = new Version("1.4.0.0"),
                MessagesAreKeys = true,
                Messages = new string[]
                {
                    "CHA_140_0",
                },
            },
            new WhatsNewMessage
            {
                Version = new Version("1.3.1.0"),
                MessagesAreKeys = true,
                Messages = new string[]
                {
                    "CHA_131_0",
                },
            },
            new WhatsNewMessage
            {
                Version = new Version("1.3.0.0"),
                MessagesAreKeys = true,
                Messages = new string[]
                {
                    "CHA_130_0",
                },
            },
        };
    }
}