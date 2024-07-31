# Teaching the Old .NET Remoting New Exploitation Tricks

This repository provides further details and resources on the [CODE WHITE blog post of the same name *Teaching the Old .NET Remoting New Tricks*](https://code-white.com/blog/teaching-old-net-remoting-new-exploitation-tricks/):

- `RemotingServer`: a restricted .NET Remoting server
- `RemotingClient_MBRO`: a client that creates a `MarshalByRefObject` on the server
- `RemotingClient_MBVO`: a client that sends a serializable `MarshalByRefObject` *by value*


## `RemotingServer`

A .NET Remoting server with restrictive configuration:

- `TypeFilterLevel.Low`: causes [CAS code access permission restrictions](https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/h846e9b3(v=vs.100))
- marshaled server type is not `MarshalByRefObject`: renders `--uselease` and `--useobjref` of [*ExploitRemotingService*](https://github.com/tyranid/ExploitRemotingService) unusable
- no existing client channel: also renders `--uselease` and `--useobjref` unusable (due to CAS restrictions)


## `RemotingClient_MBRO`

A client that implements the trick of creating a `MarshalByRefObject` on the server side and coercing the server to serialize it.

It creates a [`WebClient`](https://learn.microsoft.com/en-us/dotnet/api/system.net.webclient?view=netframework-4.8.1) that can remotely read and write files on the server.


## `RemotingClient_MBVO`

A client that implements the trick of sending a serializable `MarshalByRefObject` *by value* instead of *by reference* and coercing the server to serialize it.

It uses the [`SoundPlayer`](https://learn.microsoft.com/en-us/dotnet/api/system.media.soundplayer?view=netframework-4.8.1) to cause a file access by remotely setting its `Location` property.
