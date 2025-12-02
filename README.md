# TiltiSlip

Trigger in-game actions based on Tiltify donations.

## Current Actions

The action is chosen based on the last number in the donation amount. (e.g. $5.23 triggers action 3)

You can use the number pad to manually trigger each action for testing.

1. Receive donation message as custom order. Affects the plugin user only
2. Send custom order to all crew with donation message
3. Focus camera on random crew member
4. Focus camera on self
5. Move self to random station on ship
6. Rename the ship to donation message
7. Drop all carried gems


## Requirements

- [Slipstream: Rogue Space (on Steam)](https://playslipstream.com)
- One of the following:
    - [r2modman](https://thunderstore.io/c/slipstream-rogue-space/p/ebkr/r2modman/)
    - [Gale](https://thunderstore.io/c/slipstream-rogue-space/p/Kesomannen/GaleModManager/)
    - [BepInEx manually installed](https://docs.bepinex.dev/articles/user_guide/installation/index.html#where-to-download-bepinex)

## Game Installation

Video guide: https://youtu.be/0xGz3QaRbL8

1) Install Slipstream: Rogue Space from Steam.
2) Install Gale or r2modman.
3) Use the mod manager to select Slipstream as the game.
4) Go to the online mods section and search for "TiltiSlip".
5) Install the mod.
6) Launch the game from the mod manager. (to generate the config files)
7) Exit the game.
8) Use the config tools in the mod manager to set your Tiltify Webhook Key (see below)

## How to setup Tiltify webhooks

There are two parts to this: Online on the Tiltify website, and on your computer.

### On your computer

If you already have a way for Tiltify to send webhooks to your computer, you can use that. If you don't know what that means, just follow these steps:

1) Follow the steps in the [ngrok Getting Started guide](https://ngrok.com/docs/getting-started) to make an account and download ngrok.
2) Claim the free domain from [the Domain Dashboard](https://dashboard.ngrok.com/domains)
3) For later, you'll want to know the url to enter on the Tiltify website. It will be something like: `https://yourdomain.ngrok.io/tiltislip/webhook`

### On the Tiltify website

Firstly, if you haven't already, create a Tiltify account and set up (or join) a campaign.

Then:

1) Go to the Developer Dashboard: https://app.tiltify.com/developers
2) Click `Create Application`
3) Give it a name (any will do), a redirect URI (doesn't matter but I recommend `http://localhost:8001/tiltislip`), and click `Create app`
4) In your app details, click the `Webhooks` tab
5) Click `Add Webhook`
6) Under `Endpoint URL`, enter the URL from earlier.
7) Click `Create webhook`
8) In the settings of the webhook, go to `Subscriptions` and add your campaign using `Add Event` (you'll find your campaign ID in your campaign dashboard under `Setup` -> `Information`)
9) Turn on `direct:donation_updated` and click `Update Event`
10) Go back to the `Setup` tab of your webhook and copy the `Webhook Signing ID` - this is your Webhook Key to enter into the TiltiSlip config file.
11) Save all your changes.

### When You're About to Start

1) Start the ngrok tunnel by opening a terminal/command prompt and run the following command, replacing `yourdomain.ngrok.io` with your ngrok domain: `ngrok http 8001 --url yourdomain.ngrok.io` (You should keep this window open until you quit the game)
2) Launch Slipstream using your mod manager.
3) Go to your webhook list on Tiltify and make sure your webhook is enabled.

### When You're Finished Playing

1) In the terminal/command prompt where ngrok is running, press `Q` to stop the tunnel.
2) Head back to the Tiltify Developer Dashboard and deactivate the webhook. (The `Active` toggle in the webhook list for the application you made earlier)

#### Testing it's working:

You can use the testing tab in your webhook settings to send test events. Make sure to use `donation_updated` events, and be on a ship while testing. They should appear in the logs and trigger the actions in-game!