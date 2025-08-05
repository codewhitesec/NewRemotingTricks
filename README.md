# Teaching the Old .NET Remoting New Exploitation Tricks

This repository provides further details and resources on the [CODE WHITE blog post of the same name *Teaching the Old .NET Remoting New Exploitation Tricks*](https://code-white.com/blog/teaching-the-old-net-remoting-new-exploitation-tricks/):

- `RemotingServer`: a restricted .NET Remoting server
- `RemotingClient_MBRO`: a client that creates a `MarshalByRefObject` on the server using a XAML gadget
- `RemotingClient_MBRO_Lazy`: a client that creates a `MarshalByrefObject` on the server using `Lazy<T>`
- `RemotingClient_MBVO`: a client that sends a serializable `MarshalByRefObject` *by value*


## `RemotingServer`

A .NET Remoting server with restrictive configuration:

- `TypeFilterLevel.Low`: causes [CAS code access permission restrictions](https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/h846e9b3(v=vs.100))
- marshaled server type is not `MarshalByRefObject`: renders `--uselease` and `--useobjref` of [*ExploitRemotingService*](https://github.com/tyranid/ExploitRemotingService) unusable
- no existing client channel: also renders `--uselease` and `--useobjref` unusable (due to CAS restrictions)


## `RemotingClient_MBRO`

A client that implements the trick of creating a `MarshalByRefObject` on the server side and coercing the server to serialize it. This requires the deserialization of a `DataTable` class that results in arbitrary XAML parsing, which creates the `MarshalByRefObject` instance and throws it in an exception retrievable from the response.

It creates a [`WebClient`](https://learn.microsoft.com/en-us/dotnet/api/system.net.webclient?view=netframework-4.8.1) that can remotely read and write files on the server.


## `RemotingClient_MBRO_Lazy`

A client that implements the trick of creating a `MarshalByRefObject` on the server side and coercing the server to serialize it. Opposed to the `RemotingClient_MBRO` above, it only requires the deserialization of a [`System.Lazy<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.lazy-1?view=netframework-4.8.1) object, which creates an instance of the specified type argument `T` during serialization.

It creates a [`WebClient`](https://learn.microsoft.com/en-us/dotnet/api/system.net.webclient?view=netframework-4.8.1) that can remotely read and write files on the server.


## `RemotingClient_MBVO`

A client that implements the trick of sending a serializable `MarshalByRefObject` *by value* instead of *by reference* and coercing the server to serialize it.

It uses the [`SoundPlayer`](https://learn.microsoft.com/en-us/dotnet/api/system.media.soundplayer?view=netframework-4.8.1) to cause a file access by remotely setting its `Location` property.
