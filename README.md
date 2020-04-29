# Protobuf: testing multiple message types in a wrapper (C#)

Examples covering three options:

* `oneof`, including a quick backwards compatibility check
* `google.protobuf.Any`, using Any.Pack and doing it manually
* Custom wrapper that works like `Any`

Pretty simple. Uses Grpc.Tools for compiling the protos.

