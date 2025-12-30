---
description: "Guide for integrating FeatBit OpenFeature Node.js SDK in applications."
---

# FeatBit Feature Flag OpenFeature provider for Node.js SDK

Source: https://github.com/featbit/openfeature-provider-node-server/edit/main/README.md

Description: The FeatBit OpenFeature provider is designed primarily for use in multi-user systems such as web servers and applications. It is not intended for use in desktop and embedded systems applications.

## Supported Node versions

This version of the FeatBit OpenFeature provider is compatible with Node.js versions 16 and above.

## Getting started

### Installation

```bash
npm install @openfeature/server-sdk
npm install @featbit/node-server-sdk
npm install @featbit/openfeature-provider-node-server
```

### Quick start

```js
import { OpenFeature, ProviderEvents } from '@openfeature/server-sdk';
import { FbProvider } from '@featbit/openfeature-provider-node-server';

const provider = new FbProvider({
    sdkKey: '<your-sdk-key>',
    streamingUri: '<your-streaming-uri>',
    eventsUri: '<your-events-uri>'
});

OpenFeature.setProvider(provider);

// If you need access to the FbClient, then you can use provider.getClient()

// Evaluations before the provider indicates it is ready may get default values with a
// CLIENT_NOT_READY reason.
OpenFeature.addHandler(ProviderEvents.Ready, (eventDetails) => {
    console.log(`Changed ${eventDetails.flagsChanged}`);
});


// The FeatBit provider supports the ProviderEvents.ConfigurationChanged event.
// The provider will emit this event for any flag key that may have changed (each event will contain
// a single key in the `flagsChanged` field).
OpenFeature.addHandler(ProviderEvents.ConfigurationChanged, async (eventDetails) => {
    const client = OpenFeature.getClient();
    const value = await client.getBooleanValue('ff1', false, {targetingKey: 'my-key'});
    console.log({...eventDetails, value});
});

// (async () => {
//     // When the FeatBit provider is closed it will flush the events on the FbClient instance.
//     // This can be useful for short lived processes.
//     await OpenFeature.close();
// })();
```